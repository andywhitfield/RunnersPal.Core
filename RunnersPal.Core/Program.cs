using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using RunnersPal.Core.Geolib;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
#if DEBUG
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
#endif
builder.Services.AddControllers();
builder.Services.AddDbContext<SqliteDataContext>((serviceProvider, options) =>
{
    var sqliteConnectionString = builder.Configuration.GetConnectionString("runnerspal");
    serviceProvider.GetRequiredService<ILogger<Program>>().LogInformation("Using connection string: {SqliteConnectionString}", sqliteConnectionString);
    options.UseSqlite(sqliteConnectionString);
#if DEBUG
    options.EnableSensitiveDataLogging();
#endif
});
builder.Services
    .AddTransient<IGeoCalculator, GeoCalculator>()
    .AddScoped(sp => (ISqliteDataContext)sp.GetRequiredService<SqliteDataContext>())
    .AddScoped<IUserService, UserService>()
    .AddScoped<IUserAccountRepository, UserAccountRepository>()
    .AddScoped<IRouteRepository, RouteRepository>()
    .AddScoped<IRunLogRepository, RunLogRepository>()
    .AddScoped<IUserRouteService, UserRouteService>()
    .AddScoped<IPaceService, PaceService>()
    .AddScoped<IElevationService, ElevationService>()
    .AddScoped<IOpenElevationClient, OpenElevationClient>();

builder.Services
    .AddHttpClient(nameof(OpenElevationClient), (sp, cfg) => cfg.BaseAddress = new(sp.GetRequiredService<IConfiguration>().GetValue("OpenElevationBaseUri", "http://localhost:50000/")));

builder.Services
    .AddHttpContextAccessor()
    .ConfigureApplicationCookie(c => c.Cookie.Name = "runnerspal")
    .AddAuthentication(o => o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/signin";
        o.LogoutPath = "/signout";
        o.Cookie.Name = "runnerspal";
        o.Cookie.HttpOnly = true;
        o.Cookie.MaxAge = TimeSpan.FromDays(7);
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.IsEssential = true;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
    });
builder.Services
    .AddDataProtection()
    .SetApplicationName(typeof(Program).Namespace ?? "")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
builder.Services.Configure<CookiePolicyOptions>(o =>
{
    o.CheckConsentNeeded = context => false;
    o.MinimumSameSitePolicy = SameSiteMode.None;
});
builder.Services
    .AddSession(options => options.IdleTimeout = TimeSpan.FromMinutes(5))
    .AddFido2(options =>
    {
        options.ServerName = "runners:pal";
        options.ServerDomain = builder.Configuration.GetValue<string>("FidoDomain");
        options.Origins = [builder.Configuration.GetValue<string>("FidoOrigins")];
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
    scope.ServiceProvider.GetRequiredService<ISqliteDataContext>().Migrate();

app.Run();

public partial class Program { }