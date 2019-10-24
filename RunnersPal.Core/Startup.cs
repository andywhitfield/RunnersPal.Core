using System;
using AspNet.Security.OpenId;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RunnersPal.Core.Data;
using RunnersPal.Core.ViewModels.Binders;

namespace RunnersPal.Core
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
            services
                .AddAuthentication(o =>
                {
                    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(o =>
                {
                    o.LoginPath = "/login";
                    o.LogoutPath = "/logout";
                    o.Cookie.HttpOnly = true;
                    o.ExpireTimeSpan = TimeSpan.FromDays(150);
                })
                .AddOpenId("SmallId", "SmallId", o =>
                {
                    o.CallbackPath = "/signin-smallid";
                    o.Configuration = new OpenIdAuthenticationConfiguration
                    {
                        AuthenticationEndpoint = "https://smallid.nosuchblogger.com/openid/provider"
                    };
                });
            
            services.AddRazorPages();
            services.AddDistributedMemoryCache();
            services.AddSession();
            services
                .AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                    options.ModelBinderProviders.Insert(0, new CustomModelsBinderProvider());
                })
                .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });
            
            MassiveDB.Configure(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/home/error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

            app.UseMvcWithDefaultRoute();
        }
    }
}
