using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stripe;
using Stripe.Checkout;
using System.Data;

namespace HealthSphere_CapstoneProject.Controllers
{
    [Route("[controller]/[action]")]
    public class PaymentsController : Controller
    {
        private readonly string cc;
        private readonly IConfiguration _cfg;
        
        public PaymentsController(IConfiguration cfg)
        {
            StripeConfiguration.ApiKey = cfg["Stripe:SecretKey"];
            _cfg = cfg;
            cc = cfg.GetConnectionString("MyConnectionString");
        }

        
        [HttpGet]
        public IActionResult Key() => Json(new { key = _cfg["Stripe:PublishableKey"] });

        [HttpPost]
        public IActionResult CreateCheckoutSession(int billId)
        {
        
            int patientId; decimal amount; string status;
            string patientEmail = "patient@example.com";
            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 b.PatientId, b.Amount, b.Status, u.Email
                FROM Bills b
                JOIN Users u ON b.PatientId = u.UserID
                WHERE b.Id = @Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", billId);
                con.Open();
                using var dr = cmd.ExecuteReader();
                if (!dr.Read()) return NotFound("Bill not found.");
                patientId = (int)dr["PatientId"];
                amount = (decimal)dr["Amount"];
                status = dr["Status"].ToString();
                if (dr["Email"] != DBNull.Value) patientEmail = dr["Email"].ToString();
            }

            if (string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Bill already paid.");

            
            var domain = $"{Request.Scheme}://{Request.Host}";
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                CustomerEmail = patientEmail, 
                ClientReferenceId = billId.ToString(),
                SuccessUrl = $"{domain}/Payments/Success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Payments/Cancel?billId={billId}",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd", 
                            UnitAmount = (long)decimal.Round(amount * 100m, 0), // cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Medical Bill #{billId}"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["billId"] = billId.ToString(),
                    ["patientId"] = patientId.ToString()
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(
                "UPDATE Bills SET StripeSessionId=@sid WHERE Id=@id", con))
            {
                cmd.Parameters.AddWithValue("@sid", session.Id);
                cmd.Parameters.AddWithValue("@id", billId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Json(new { id = session.Id, url = session.Url });
        }

        [HttpGet]
        public IActionResult Success(string session_id)
        {
            
            ViewBag.SessionId = session_id;
            return View(); 
        }

        [HttpGet]
        public IActionResult Cancel(int billId)
        {
            ViewBag.BillId = billId;
            return View(); 
        }

        
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Webhook()
        {
            var json = new StreamReader(Request.Body).ReadToEnd();
            var signatureHeader = Request.Headers["Stripe-Signature"];
            var webhookSecret = _cfg["Stripe:WebhookSecret"];

            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = Stripe.EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
            }
            catch (Exception ex)
            {
                return BadRequest($"Webhook signature verification failed: {ex.Message}");
            }

            if (string.Equals(stripeEvent.Type, "checkout.session.completed", StringComparison.OrdinalIgnoreCase))
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

               
                var billIdStr = session?.ClientReferenceId ?? session?.Metadata?["billId"];
                if (int.TryParse(billIdStr, out var billId))
                {
                    var paymentIntentId = session?.PaymentIntentId;

                    using (var con = new SqlConnection(cc))
                    using (var cmd = new SqlCommand(@"
                UPDATE Bills
                   SET Status='Paid',
                       StripePaymentIntentId=@pi,
                       PaidAt=GETDATE()
                 WHERE Id=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@pi", (object?)paymentIntentId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", billId);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
         
            else if (string.Equals(stripeEvent.Type, "payment_intent.succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var intent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                var billIdStr = intent?.Metadata?["billId"];
                if (int.TryParse(billIdStr, out var billId))
                {
                    using (var con = new SqlConnection(cc))
                    using (var cmd = new SqlCommand(@"
                UPDATE Bills
                   SET Status='Paid',
                       StripePaymentIntentId=@pi,
                       PaidAt=GETDATE()
                 WHERE Id=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@pi", intent?.Id ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", billId);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return Ok();
        }

    }
}
