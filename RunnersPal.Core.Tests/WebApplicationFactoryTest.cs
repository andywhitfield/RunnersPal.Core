using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests;

public class WebApplicationFactoryTest : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SqliteDataContext> _options;
    private readonly Action<IServiceCollection>? _configureTestServices;

    public WebApplicationFactoryTest(Action<IServiceCollection>? configureTestServices = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<SqliteDataContext>().UseSqlite(_connection).Options;
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services
                .Replace(ServiceDescriptor.Scoped(_ => new SqliteDataContext(_options)))
                .AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestStubAuthHandler>("Test", null);
            _configureTestServices?.Invoke(services);
        });

    public HttpClient CreateClient(bool isAuthorized, bool allowAutoRedirect = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowAutoRedirect });
        if (isAuthorized)
            client.DefaultRequestHeaders.Authorization = new("Test");
        return client;
    }

    public async Task ConfigureDbContextAsync(Func<SqliteDataContext, Task> contextAction)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        await contextAction(context);
        await context.SaveChangesAsync();
    }

    public static string GetFormValidationToken(string responseContent, string? formAction = null)
    {
        var validationToken = responseContent[responseContent.IndexOf(formAction == null ? "<form " : $"action=\"{formAction}\"")..];
        validationToken = validationToken[validationToken.IndexOf("__RequestVerificationToken")..];
        validationToken = validationToken[(validationToken.IndexOf("value=\"") + 7)..];
        validationToken = validationToken[..validationToken.IndexOf('"')];
        return validationToken;
    }
}
