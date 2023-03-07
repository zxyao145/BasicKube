using BasicKube.Api;
using BasicKube.Api.Domain;
using BasicKube.Api.Filters;
using KubeClient;

var builder = WebApplication.CreateBuilder(args);

DiUtil.Configuration = builder.Configuration;
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    options.Filters.Add<IamIdAndNsNameFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApiResult()
    .AddBasicKubeServices();

var c = K8sConfig.Load();
var cname = c.CurrentContextName;
KubeClientOptions clientOptions = K8sConfig.Load().ToKubeClientOptions(
    kubeContextName: cname,
    defaultKubeNamespace: "default"
// loggerFactory: builder.Services.g
);

builder.Services.AddKubeClient(clientOptions);

// Load kubernetes configuration
var kubernetesClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
//kubernetesClientConfig = new KubernetesClientConfiguration { Host = "http://127.0.0.1:8001" };
builder.Services.AddSingleton<IKubernetes>(new Kubernetes(kubernetesClientConfig));

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "cors",
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

app.UseCors("cors");
app.UseAuthorization();
app.UseWebSockets();
app.MapControllers();

app.Run();