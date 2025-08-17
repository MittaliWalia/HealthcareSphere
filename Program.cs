using HealthSphere_CapstoneProject.Models;
using Microsoft.EntityFrameworkCore;

using Rotativa.AspNetCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseSession();

RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
