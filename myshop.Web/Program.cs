using Microsoft.EntityFrameworkCore;
using myshop.DataAccess;
using myshop.DataAccess.Implementation;
using myshop.Entities.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using myshop.Utilities;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
));

builder.Services.Configure<StripeData>(builder.Configuration.GetSection("stripe"));

// تكوين Identity مع سياسات القفل والأدوار
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(4);
    options.SignIn.RequireConfirmedAccount = false; // إلغاء تأكيد البريد إذا لم يكن ضرورياً
})
.AddDefaultTokenProviders()
.AddDefaultUI()
.AddEntityFrameworkStores<ApplicationDbContext>();

// تكوين سياسات الصلاحيات
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(SD.AdminRole));
});

builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("stripe:Secretkey").Get<string>();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// إنشاء الأدوار والمستخدم الإداري تلقائياً (فقط في بيئة التطوير)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // إنشاء الأدوار إذا لم تكن موجودة
        string[] roles = { SD.AdminRole, SD.EditorRole, SD.CustomerRole };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // إنشاء مستخدم إداري إذا لم يكن موجوداً
        string adminEmail = "admin@example.com";
        string adminPassword = "Admin@1234"; // كلمة مرور قوية
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, SD.AdminRole);
            }
        }
    }
}

// تعديل مسارات الروتنج لتجنب المشاكل
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area=Admin}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "Customer",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // مسار افتراضي بدون منطقة

app.MapRazorPages();
app.Run();