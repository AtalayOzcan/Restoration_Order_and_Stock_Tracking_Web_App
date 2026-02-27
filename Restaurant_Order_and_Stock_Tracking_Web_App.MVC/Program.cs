using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Data;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Models;
using Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Services;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── DbContext ────────────────────────────────────────────────
            builder.Services.AddDbContext<RestaurantDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ── Identity ─────────────────────────────────────────────────
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;

                options.Lockout.AllowedForNewUsers = false;
            })
            .AddEntityFrameworkStores<RestaurantDbContext>()
            .AddDefaultTokenProviders();

            // ── Cookie / Oturum Ayarları ─────────────────────────────────
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Name = "RestaurantOS.Auth";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

            // ── Security Stamp (Validation Interval Buraya Taşındı) ──────
            builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.FromSeconds(30);
            });

            // ── Background Service ───────────────────────────────────────
            builder.Services.AddHostedService<ReservationCleanupService>();

            // ── MVC ──────────────────────────────────────────────────────
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // ── Middleware Pipeline ──────────────────────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();   // UseAuthorization'dan ÖNCE
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            //// ── İlk Admin Seed ───────────────────────────────────────────────
            //using (var scope = app.Services.CreateScope())
            //{
            //    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            //    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            //    // Rolleri oluştur
            //    string[] roles = { "Admin", "Waiter", "Kitchen" };
            //    foreach (var role in roles)
            //    {
            //        if (!await roleManager.RoleExistsAsync(role))
            //            await roleManager.CreateAsync(new IdentityRole(role));
            //    }

            //    // Admin kullanıcısı yoksa oluştur
            //    var adminUser = await userManager.FindByNameAsync("admin");
            //    if (adminUser == null)
            //    {
            //        adminUser = new ApplicationUser
            //        {
            //            UserName = "admin",
            //            Email = "admin@restaurant.com",
            //            FullName = "Admin",
            //            CreatedAt = DateTime.UtcNow
            //        };
            //        await userManager.CreateAsync(adminUser, "admin123");
            //        await userManager.AddToRoleAsync(adminUser, "Admin");
            //    }
            //}
            //// ────────────────────────────────────────────────────────────────

            app.Run();
        }
    }
}