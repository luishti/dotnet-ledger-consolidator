using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ledger.Infrastructure.Services
{
    /// <summary>
    /// Worker que publica mensagens da tabela outbox para o broker usando MassTransit.
    /// Implementa o padrão Outbox, garantindo consistência entre gravação no banco e publicação.
    /// </summary>
    public class OutboxWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxWorker> _logger;

        public OutboxWorker(IServiceProvider serviceProvider, ILogger<OutboxWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox worker iniciado");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    var messages = await db.OutboxMessages
                        .Where(m => m.ProcessedAt == null)
                        .OrderBy(m => m.CreatedAt)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in messages)
                    {
                        if (string.IsNullOrWhiteSpace(msg.Type))
                            continue;

                        try
                        {
                            // Converte o conteúdo JSON para o tipo apropriado
                            var messageType = Type.GetType(msg.Type);
                            if (messageType == null)
                            {
                                _logger.LogWarning("Tipo de mensagem desconhecido: {Type}", msg.Type);
                                continue;
                            }
                            var payload = JsonSerializer.Deserialize(msg.Content, messageType);
                            if (payload != null)
                            {
                                await publisher.Publish(payload, messageType, stoppingToken);
                                msg.ProcessedAt = DateTime.UtcNow;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao publicar mensagem {MessageId}", msg.Id);
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no worker da outbox");
                }

                // Aguarda antes de tentar novamente
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}