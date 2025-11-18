using ETICARET.Business.Abstract;
using ETICARET.Business.Concrete;
using ETICARET.DataAccess.Abstract;
using ETICARET.DataAccess.Concrete;
using ETICARET.WebUI.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ETICAREt.WebUI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection"));
            });

            #region Configure Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
                .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
                .AddDefaultTokenProviders();
            #endregion

            #region Configure Cookie Settings
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.SlidingExpiration = true;
                options.Cookie = new CookieBuilder()
                {
                    HttpOnly = true,
                    Name = "ETICARET.Security.Cookie",
                    SameSite = SameSiteMode.Strict,
                };
            });

            #endregion

            #region Servisler
            builder.Services.AddScoped<IProductDal, EfCoreProductDal>();
            builder.Services.AddScoped<IProductService, ProductManager>();
            builder.Services.AddScoped<ICartDal, EfCoreCartDal>();
            builder.Services.AddScoped<ICartService, CartManager>();
            builder.Services.AddScoped<ICommentDal, EfCoreCommentDal>();
            builder.Services.AddScoped<ICommentService, CommentManager>();
            builder.Services.AddScoped<ICategoryDal, EfCoreCategoryDal>();
            builder.Services.AddScoped<ICategoryService, CategoryManager>();
            builder.Services.AddScoped<IOrderDal, EfCoreOrderDal>();
            builder.Services.AddScoped<IOrderService, OrderManager>();
            #endregion


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();



            #region Endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}");

                endpoints.MapControllerRoute(
                    name: "products",
                    pattern: "products/{category?}",
                    defaults: new { controller = "Shop", action = "List" }
                );
                endpoints.MapControllerRoute(
                    name: "adminProducts",
                    pattern: "admin/products",
                    defaults: new { controller = "Admin", action = "ProductList" }
                );
                endpoints.MapControllerRoute(
                    name: "adminProducts",
                    pattern: "admin/products/{id}",
                    defaults: new { controller = "Admin", action = "EditProduct" }
                );

                endpoints.MapControllerRoute(
                   name: "adminProducts",
                   pattern: "admin/deleteproduct",
                   defaults: new { controller = "Admin", action = "DeleteProduct" }
               );

                endpoints.MapControllerRoute(
                    name: "adminCategory",
                    pattern: "admin/category",
                    defaults: new { controller = "Admin", action = "CategoryList" }
                );
                endpoints.MapControllerRoute(
                    name: "adminCategory",
                    pattern: "admin/categories/{id}",
                    defaults: new { controller = "Admin", action = "EditCategory" }
                );

                endpoints.MapControllerRoute(
                  name: "adminCategory",
                  pattern: "admin/deletecategory",
                  defaults: new { controller = "Admin", action = "DeleteCategory" }
              );
                endpoints.MapControllerRoute(
                    name: "cart",
                    pattern: "cart",
                    defaults: new { controller = "Cart", action = "Index" }
                );
                endpoints.MapControllerRoute(
                    name: "checkout",
                    pattern: "checkout",
                    defaults: new { controller = "Cart", action = "Checkout" }
                );
                endpoints.MapControllerRoute(
                    name: "orders",
                    pattern: "orders",
                    defaults: new { controller = "Cart", action = "GetOrders" }
                );
            });
            #endregion

            #region SeedDatabase
            SeedDatabase.Seed();
            #endregion

            #region Seed Identity roles and admin user
            using(var scope = app.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                await SeedIdentity.Seed(userManager, roleManager, app.Configuration);
            }
            #endregion

            app.Run();
        }
    }
}
