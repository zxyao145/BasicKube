using BasicKube.Api;
using BasicKube.Api.Filters;

var builder = WebApplication.CreateBuilder(args);
DiUtil.Configuration = builder.Configuration;
builder.Services.AddControllers(options =>
{
    options.Filters.AddAppFilters();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiResult()
    .ScanService()
    .AddK8sService(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "allowAll",
        policy =>
        {
            policy.AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod();
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

app.UseCors("allowAll");
app.UseAuthorization();
app.UseWebSockets();
app.MapControllers();

app.Run();