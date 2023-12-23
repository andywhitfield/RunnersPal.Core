using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RunnersPal.Core.Controllers;
using RunnersPal.Core.Data;
using RunnersPal.Core.Data.Caching;
using RunnersPal.Core.ViewModels.Binders;

namespace RunnersPal.Core;

public class Startup(IConfiguration configuration)
{
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
                o.Cookie.MaxAge = TimeSpan.FromDays(150);
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.IsEssential = true;
                o.ExpireTimeSpan = TimeSpan.FromDays(150);
                o.SlidingExpiration = true;
            });

        services.AddRazorPages();
        services.AddDistributedMemoryCache();
        services
            .AddSession()
            .AddFido2(options =>
            {
                options.ServerName = "runners:pal";
                options.ServerDomain = configuration.GetValue<string>("FidoDomain");
                options.Origins = [configuration.GetValue<string>("FidoOrigins")];
            });
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
            logging.SetMinimumLevel(LogLevel.Trace);
        });

        services
            .AddTransient<IDataCache, SimpleDataCache>()
            .AddTransient<IAuthorisationHandler, AuthorisationHandler>();

        MassiveDB.Configure(configuration);
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
