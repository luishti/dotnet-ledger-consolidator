using Consolidator.Api;
using Consolidator.Api.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Banco de dados
builder.Services.AddDbContext<ConsolidatorDbContext>(options =>
{
    var cs = configuration.GetConnectionString("Default");

    options.UseNpgsql(cs, npg =>
    {
        npg.MigrationsHistoryTable("__EFMigrationsHistory", "public");
    });
});

// Redis (StackExchange.Redis)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var host = configuration["Redis:Host"] ?? "localhost";
    var port = configuration["Redis:Port"] ?? "6379";
    // Conecta ao Redis usando host e porta da configuração
    return ConnectionMultiplexer.Connect($"{host}:{port}");
});

await Task.Delay(TimeSpan.FromSeconds(10));

// MassTransit e consumidor com transporte RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Registra todos os consumidores do namespace do EntryRecordedConsumer
    x.AddConsumersFromNamespaceContaining<EntryRecordedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = configuration["RabbitMq:Host"] ?? "localhost";
        var username = configuration["RabbitMq:Username"] ?? "guest";
        var password = configuration["RabbitMq:Password"] ?? "guest";
        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Cria fila automática e associa consumidores
        cfg.ConfigureEndpoints(context);
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// OpenTelemetry: instrumenta ASP.NET Core, clientes HTTP, Redis e captura spans de MassTransit
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService("Consolidator.Api"))
    .WithTracing(tracerBuilder =>
    {
        tracerBuilder.AddAspNetCoreInstrumentation();
        tracerBuilder.AddHttpClientInstrumentation();
        tracerBuilder.AddRedisInstrumentation();
        // Ouve spans do MassTransit via ActivitySource
        tracerBuilder.AddSource("MassTransit");
        tracerBuilder.AddConsoleExporter();
    });

var app = builder.Build();

// Aplica migrações de banco de dados
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ConsolidatorDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Erro ao aplicar migrações: " + ex.Message);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

// Endpoint para consultar saldo diário
app.MapGet("/daily-balances", async (DateTime? date, ConsolidatorDbContext db, IConnectionMultiplexer redis) =>
{
    DateOnly targetDate = date.HasValue ? DateOnly.FromDateTime(date.Value) : DateOnly.FromDateTime(DateTime.UtcNow);
    var redisKey = $"dailyBalances:{targetDate:yyyy-MM-dd}";
    var redisDb = redis.GetDatabase();
    var cached = await redisDb.StringGetAsync(redisKey);
    if (cached.HasValue)
    {
        var cachedResult = JsonSerializer.Deserialize<List<DailyBalanceDto>>(cached!);
        return Results.Ok(cachedResult);
    }
    var balances = await db.DailyBalances
        .AsNoTracking()
        .Where(b => b.Date == targetDate)
        .Select(b => new DailyBalanceDto(b.MerchantId, b.Date, b.TotalAmount))
        .ToListAsync();
    // Armazena no cache por 5 minutos
    await redisDb.StringSetAsync(redisKey, JsonSerializer.Serialize(balances), TimeSpan.FromMinutes(5));
    return Results.Ok(balances);
});

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

// DTO para retorno dos saldos
public record DailyBalanceDto(string MerchantId, DateOnly Date, decimal TotalAmount);