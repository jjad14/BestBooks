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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));

            // AddDefaultIdentity does not have support for identity role.
            // Using AddIdentity will also need AddDefaultTokenProviders
            services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddSingleton<IEmailSender, EmailSender>();

            services.Configure<EmailOptions>(Configuration);

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddControllersWithViews().AddRazorRuntimeCompilation();

            services.AddRazorPages();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
            });

            // facebook authentication - appId and AppSecret found on fb dev app page
            services.AddAuthentication().AddFacebook(opt =>
            {
                opt.AppId = "375606090415758";
                opt.AppSecret = "ea128dc56b6b872d81c872142335464c";
            });

            // google authentication - appId and AppSecret found on google dev app page
            services.AddAuthentication().AddGoogle(opt =>
            {
                opt.ClientId = "492806799469-fnoch2jod0s79nqq1a5g9l3qsumqg6ev.apps.googleusercontent.com";
                opt.ClientSecret = "R5UUoQ2UQlTNLkRPQcAiNk2v";
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            app.UseSession();

            app.UseAuthentication();

            app.UseAuthorization();

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


//static async Task Execute()
//{
//    var apiKey = Environment.GetEnvironmentVariable("NAME_OF_THE_ENVIRONMENT_VARIABLE_FOR_YOUR_SENDGRID_KEY");
//    var client = new SendGridClient(apiKey);
//    var from = new EmailAddress("test@example.com", "Example User");
//    var subject = "Sending with SendGrid is Fun";
//    var to = new EmailAddress("test@example.com", "Example User");
//    var plainTextContent = "and easy to do anywhere, even with C#";
//    var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
//    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
//    var response = await client.SendEmailAsync(msg);
//}