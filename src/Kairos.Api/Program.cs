using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using Kairos.Api.Security;
using Kairos.Api.Services;
using Kairos.Api.Services.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.AddControllers(o =>
{
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
}).AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    o.JsonSerializerOptions.WriteIndented = true;
});

var dashboardOptions = builder.Configuration.GetSection(DashboardAuthOptions.SectionName)
    .Get<DashboardAuthOptions>() ?? new DashboardAuthOptions();
var callbackOptions = builder.Configuration.GetSection(CallbackOptions.SectionName)
    .Get<CallbackOptions>() ?? new CallbackOptions();
var apiAuthOptions = builder.Configuration.GetSection(ApiAuthOptions.SectionName)
    .Get<ApiAuthOptions>() ?? new ApiAuthOptions();

builder.Services.AddHttpClient();
builder.Services.AddSingleton(callbackOptions);
builder.Services.AddSingleton(new CallbackUrlValidator(callbackOptions));
builder.Services.AddTransient<DynamicCallbackJob>();

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_110)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var apiKeyAuthenticator = new ApiKeyAuthenticator(apiAuthOptions);
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), branch =>
    branch.Use(async (context, next) =>
    {
        if (!apiKeyAuthenticator.IsAuthorized(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Message = "Missing or invalid API key." });
            return;
        }

        await next();
    }));

app.MapControllers();

app.UseHangfireDashboard(dashboardOptions.Path, new DashboardOptions
{
    DashboardTitle = "Kairos",
    Authorization = [new DashboardBasicAuthFilter(dashboardOptions)]
});

app.Run();
