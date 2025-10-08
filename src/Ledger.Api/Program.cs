using Ledger.Application.Commands;
using Ledger.Application.Queries;
using Ledger.Infrastructure;
using Ledger.Infrastructure.Repositories;
using Ledger.Infrastructure.Services;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configurações
var configuration = builder.Configuration;

// Banco de dados
builder.Services.AddDbContext<LedgerDbContext>(options =>
{
    var cs = configuration.GetConnectionString("Default");

    options.UseNpgsql(cs, npg =>
    {
        npg.MigrationsHistoryTable("__EFMigrationsHistory", "public");
    });
});

// Repositório
builder.Services.AddScoped<LedgerRepository>();

// MediatR para comandos e queries
// A versão 11.x utiliza o registro simples baseado em assemblies
builder.Services.AddMediatR(typeof(CreateEntryCommand).Assembly);

await Task.Delay(TimeSpan.FromSeconds(10));

// MassTransit com transporte RabbitMQ
builder.Services.AddMassTransit(x =>
{
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
    });
});

// Hosted service para publicar mensagens da outbox
builder.Services.AddHostedService<OutboxWorker>();

// Swagger para documentação
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// OpenTelemetry – instrumenta ASP.NET Core e clientes HTTP. Para o MassTransit,
// as atividades são emitidas via ActivitySource "MassTransit".
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService("Ledger.Api"))
    .WithTracing(builder =>
    {
        builder.AddAspNetCoreInstrumentation();
        builder.AddHttpClientInstrumentation();
        // Habilita coleta de spans do MassTransit
        builder.AddSource("MassTransit");
        builder.AddConsoleExporter();
    });

var app = builder.Build();

// Executa migrações automáticas (para criar tabelas se necessário)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        db.Database.Migrate();
    }
    catch (System.Exception ex)
    {
        Console.WriteLine("Erro ao aplicar migrações: " + ex.Message);
    }
}

// Configuração do pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Endpoints
app.MapPost("/entries", async (CreateEntryCommand command, IMediator mediator) =>
{
    var id = await mediator.Send(command);
    return Results.Created($"/entries/{id}", new { id });
});

app.MapGet("/entries/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var entry = await mediator.Send(new GetEntryByIdQuery(id));
    return entry is null ? Results.NotFound() : Results.Ok(entry);
});

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();