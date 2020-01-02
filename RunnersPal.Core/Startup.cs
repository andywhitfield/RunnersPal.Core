using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using AspNet.Security.OpenId;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RunnersPal.Core.Data;
using RunnersPal.Core.Data.Caching;
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
                .AddOpenIdConnect(options =>
                {
                    options.ClientId = "runnerspal";
                    options.ClientSecret = "4b301164-0c33-454c-969d-050907f11d55";

                    options.RequireHttpsMetadata = false;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                    options.Authority = "https://smallauth.nosuchblogger.com/";
                    options.Scope.Add("roles");

                    options.SecurityTokenValidator = new JwtSecurityTokenHandler
                    {
                        InboundClaimTypeMap = new Dictionary<string, string>()
                    };

                    options.TokenValidationParameters.NameClaimType = "name";
                    options.TokenValidationParameters.RoleClaimType = "role";

                    options.AccessDeniedPath = "/";
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

            services.AddTransient<IDataCache, SimpleDataCache>();
            
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

            app.UseCookiePolicy();
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
