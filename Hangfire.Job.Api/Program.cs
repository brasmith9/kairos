using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Job.Api.Security;
using Hangfire.Job.Api.Services;
using Hangfire.Job.Api.Services.Providers;
using Hangfire.PostgreSql;

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

builder.Services.AddHttpClient();
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
app.MapControllers();

app.UseHangfireDashboard(dashboardOptions.Path, new DashboardOptions
{
    DashboardTitle = "Hangfire Job API",
    Authorization = [new DashboardBasicAuthFilter(dashboardOptions)]
});

app.Run();
