using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.DataAccess.Repository;
using Microsoft.AspNetCore.Identity.UI.Services;
using BestBooks.Utility;
using Stripe;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using BestBooks.DataAccess.DbInitializer;

namespace BestBooks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // MySql database Connection
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));

            // AddDefaultIdentity does not have support for identity role.
            // Using AddIdentity will also need AddDefaultTokenProviders
            services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // initialize database
            services.AddScoped<IDbInitializer, DbInitializer>();

            services.AddSingleton<IEmailSender, EmailSender>();

            // temp data allows us to hold a value for just one request
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();

            // Third Party
            services.Configure<EmailOptions>(Configuration);
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));
            services.Configure<TwilioSettings>(Configuration.GetSection("Twilio"));
            services.Configure<FacebookSettings>(Configuration.GetSection("Facebook"));
            services.Configure<GoogleSettings>(Configuration.GetSection("Google"));
            services.Configure<BrainTreeSettings>(Configuration.GetSection("BrainTree"));

            services.AddSingleton<IBrainTree, BrainTree>();

            services.AddControllersWithViews().AddRazorRuntimeCompilation();

            services.AddRazorPages();

            // cookie configuration
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
            });

            // facebook authentication - appId and AppSecret found on fb dev app page
            services.AddAuthentication().AddFacebook(opt =>
            {
                opt.AppId = Configuration.GetSection("Facebook")["AppId"];
                opt.AppSecret = Configuration.GetSection("Facebook")["AppSecret"];
            });

            // google authentication - appId and AppSecret found on google dev app page
            services.AddAuthentication().AddGoogle(opt =>
            {
                opt.ClientId = Configuration.GetSection("Google")["ClientId"];
                opt.ClientSecret = Configuration.GetSection("Google")["ClientSecret"];
            });

            // Session Configuration
            services.AddSession(opt =>
            {
                opt.IdleTimeout = TimeSpan.FromMinutes(30);
                opt.Cookie.HttpOnly = true;
                opt.Cookie.IsEssential = true;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IDbInitializer dbInitializer)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            StripeConfiguration.ApiKey = Configuration.GetSection("Stripe")["SecretKey"];

            app.UseSession();

            app.UseAuthentication();

            app.UseAuthorization();

            // initalize database
            dbInitializer.InitalizeDb();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
