using BasicKube.Api.Common.Components.Logger;
using BasicKube.Api.Domain.Prom;
using BasicKube.Api.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;


var builder = WebApplication.CreateBuilder(args);
var logger = builder.Host.AddSerilog();
logger.Information("app starting..");
DiUtil.Configuration = builder.Configuration;
builder.Services.AddControllers(options =>
{
    options.Filters.AddAppFilters();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
)
.AddCookie();


builder.Services.AddApiResult()
    .ScanService()
    .AddK8sService(builder.Configuration)
    .AddHttpClient<PromHttpClient>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "allowAll",
        policy =>
        {
            policy
            //.AllowAnyOrigin()
            .WithOrigins("http://localhost:5103")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

var app = builder.Build();
DiUtil.ServiceProvider = app.Services;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("allowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();
app.MapControllers();

app.Run();