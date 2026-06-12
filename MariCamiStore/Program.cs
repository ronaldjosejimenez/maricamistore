    /*using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;*/
using MariCamiStore;
using MariCamiStore.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web.UI;
using NLog;
using NLog.Web;
using MariCamiStore.Extensions;
using Microsoft.AspNetCore.Mvc.Razor;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{

    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for Dependency injection
    // builder.Logging.ClearProviders(); // Comentado para version desarrollo y mostrar logs en consola en formato original.
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    builder.Services.Configure<BaseSettings>(builder.Configuration);

    // Add services to the container.

    /*builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddAuthorization(options =>
    {
        // By default, all incoming requests will be authorized according to the default policy.
        options.FallbackPolicy = options.DefaultPolicy;
    });*/

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddRazorPages()
        .AddMicrosoftIdentityUI()
        .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
        .AddDataAnnotationsLocalization()
        .AddNewtonsoftJson();

    builder.Services.AddServices();

    builder.Services.AddDbContext<MariCamiStoreContext>(options =>
    {
        options.UseSqlServer(builder.Configuration["ConnectionString"]);
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseSession();
    app.UseRouting();
        
    app.UseAuthorization();

    app.MapRazorPages();
    
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    //NLog: catch setup errors
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}