using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// Carrega configurações do proxy a partir do appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();