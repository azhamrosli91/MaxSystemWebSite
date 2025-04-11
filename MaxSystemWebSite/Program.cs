using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MaxSys.Interface;
using MaxSys.Helpers;
using BaseWebApi.Interface;
using BaseSQL.Interface;
using BaseWebApi.Repository;
using Component_TableListing.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using MaxSys.Helpers;
using System.Globalization;
using Component_TableListing.Services;
using MaxSys.Interface;
using E_Template.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IWebApi, WebApi>();
builder.Services.AddScoped<IDapper, BaseSQL.Repository.BaseSQL>();
builder.Services.AddScoped<ISQL, BaseSQL.Repository.BaseSQL>();
builder.Services.AddScoped<IDapper_Oracle, BaseSQL.Repository.BaseOracle>();
builder.Services.AddScoped<IErrorLog, BaseSQL.Repository.BaseSQL>();
builder.Services.AddScoped<IJWTToken, JwtToken>();
builder.Services.AddScoped<ITable, TableService>();
builder.Services.AddScoped<IAuthenticator, BaseSQL.Repository.BaseSQL>();
builder.Services.AddSingleton<IConfiguration>(provider => builder.Configuration);
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<CommonMethod>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionExpireAttribute>();
});
// Register EmailService
builder.Services.AddSingleton<IEmailService, EmailService_STMP>();


builder.Services.AddScoped<SessionExpireAttribute>(); // Register the custom filter
builder.Services.AddHttpContextAccessor();

var provider = builder.Services.BuildServiceProvider();
var _configuration = provider.GetRequiredService<IConfiguration>();

////JWT Token
//builder.Services.AddAuthentication(
//    options =>
//    {
//        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//    }
//)
//.AddJwtBearer(options =>
//{
//    options.Events = new JwtBearerEvents
//    {
//        OnMessageReceived = context =>
//        {
//            var token = context.HttpContext.Request.Cookies["jwt"];
//            if (!string.IsNullOrEmpty(token))
//            {
//                context.Token = token;
//            }
//            return Task.CompletedTask;
//        },
//        OnAuthenticationFailed = context =>
//        {
//            Console.WriteLine("Authentication failed: " + context.Exception.Message);
//            return Task.CompletedTask;
//        },
//        OnTokenValidated = context =>
//        {
//            Console.WriteLine("Token is valid.");
//            return Task.CompletedTask;
//        }
//    };

//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
//        ValidAudience = builder.Configuration["JwtSettings:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JwtSettings").GetValue<string>("Key").ToString()))
//    };
//});
//JWT Token
builder.Services.AddAuthentication(
    options =>
    {
       // options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
       //// options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // For sign-in
       // options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
       // options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    }
)
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.HttpContext.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token is valid.");
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JwtSettings").GetValue<string>("Key").ToString()))
    };
}).AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                  .RequireAuthenticatedUser()
                  .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});


// Set default culture to "en-US"
var defaultCulture = new CultureInfo("en-US");
var cultureInfo = new CultureInfo("en-US")
{
    DateTimeFormat = { ShortDatePattern = "dd/MM/yyyy", LongDatePattern = "dd/MM/yyyy HH:mm:ss" }
};
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;



builder.Services.AddAuthorization();

var app = builder.Build();

//app.Use(async (context, next) =>
//{
//    await next();

//    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
//    {
//        context.Response.Redirect("/Errors/NotFound");
//    }
//    else if (context.Response.StatusCode == 401 && !context.Response.HasStarted)
//    {
//        context.Response.Redirect("/Errors/Unauthorize");
//    }
//});


app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo),
    SupportedCultures = new List<CultureInfo> { cultureInfo },
    SupportedUICultures = new List<CultureInfo> { cultureInfo }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
