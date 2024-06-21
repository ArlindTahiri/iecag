using WebApp.Components;
using WebApp.Services;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebApp.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddApplicationInsightsTelemetry(options => {
    options.ConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "auth_token";
        options.LoginPath = "/login";
        options.Cookie.MaxAge = TimeSpan.FromMinutes(30);
        options.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<DataFetcherService>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    var httpClient = sp.GetRequiredService<HttpClient>();
    var hubContext = sp.GetRequiredService<IHubContext<PricesHub>>();
    return new DataFetcherService(httpClient, hubContext, connectionString);
});

builder.Services.AddHttpClient();

builder.Services.AddSingleton<NotificationService>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    var logger = sp.GetRequiredService<ILogger<NotificationService>>();
    var telemetryClient = sp.GetRequiredService<TelemetryClient>();
    return new NotificationService(connectionString, logger, telemetryClient);
});

builder.Services.AddSingleton<UserService>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    var logger = sp.GetRequiredService<ILogger<UserService>>();
    var telemetryClient = sp.GetRequiredService<TelemetryClient>();
    return new UserService(connectionString, logger, telemetryClient);
});

builder.Services.AddSingleton<TransactionService>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    var logger = sp.GetRequiredService<ILogger<TransactionService>>();
    var telemetryClient = sp.GetRequiredService<TelemetryClient>();
    return new TransactionService(connectionString, logger, telemetryClient);
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
          new[] { "application/octet-stream" });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseResponseCompression();

app.MapHub<PricesHub>("/priceshub");


app.Run();
