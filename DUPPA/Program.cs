using System.Globalization;
using DUPPA;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Localization
// ----------------------
var supportedCultures = new[] { new CultureInfo("pl-PL") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("pl-PL");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // force secure cookies
});
// ----------------------
// Authentication
// ----------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:CLIENT_ID"];
    options.ClientSecret = builder.Configuration["Google:CLIENT_SECRET"];
    options.Scope.Add("profile");
    options.Scope.Add("email");
});

// ----------------------
// Authorization & services
// ----------------------
builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // important for HTTPS behind proxy
});
builder.Services.AddHttpContextAccessor();

AppConfiguration.Configuration = builder.Configuration;

// ----------------------
// Build app
// ----------------------
var app = builder.Build();

// ----------------------
// Environment-specific config
// ----------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();

    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    app.Urls.Add($"http://*:{port}");
}

// ----------------------
// Middleware pipeline
// ----------------------

// Forwarded headers (for Render proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { }, // empty means trust all networks (okay for Render)
    KnownProxies = { }   // empty means trust all proxies (okay for Render)
});


// Localization
app.UseRequestLocalization();

// HTTPS redirect
app.UseHttpsRedirection();

// Routing
app.UseRouting();

// Session must come BEFORE authentication
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Static assets & Razor Pages
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// Run the app
app.Run();
