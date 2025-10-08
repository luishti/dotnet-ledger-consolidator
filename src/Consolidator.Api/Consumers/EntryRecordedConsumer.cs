using System;
using System.Threading.Tasks;
using Consolidator.Api.Entities;
using Ledger.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Shared.Messaging.Events;
using Npgsql;

namespace Consolidator.Api.Consumers
{
    /// <summary>
    /// Consumidor de mensagens EntryRecorded. Atualiza o saldo diário do comerciante no banco e limpa o cache.
    /// </summary>
    public class EntryRecordedConsumer : IConsumer<EntryRecorded>
    {
        private readonly ConsolidatorDbContext _db;
        private readonly IDatabase _redis;

        private readonly ILogger<EntryRecordedConsumer> _logger;

        public EntryRecordedConsumer(ConsolidatorDbContext db, IConnectionMultiplexer redis)
        {
            _db = db;
            _redis = redis.GetDatabase();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<EntryRecordedConsumer>();
        }

        public async Task Consume(ConsumeContext<EntryRecorded> context)
        {
            var message = context.Message;
            _logger.LogInformation("EntryRecordedConsumer.Receive → Merchant={Merchant}, Amount={Amt}, Type={T}, CreatedAt={C}",
                message.MerchantId, message.Amount, message.Type, message.CreatedAt);

            DateOnly date = DateOnly.FromDateTime(message.CreatedAt);

            decimal signedAmount = string.Equals(message.Type, "Credit", StringComparison.OrdinalIgnoreCase)
                ? message.Amount
                : -message.Amount;

            // Comando SQL upsert via interpolated string
            // A interpolação vai gerar parâmetros automaticamente
            FormattableString sql = $@"
                INSERT INTO public.""DailyBalances"" (""Id"", ""MerchantId"", ""Date"", ""TotalAmount"", ""UpdatedAtUtc"")
                VALUES (gen_random_uuid(), {message.MerchantId}, {date}, {signedAmount}, NOW())
                ON CONFLICT (""MerchantId"", ""Date"")
                DO UPDATE SET
                  ""TotalAmount"" = public.""DailyBalances"".""TotalAmount"" + EXCLUDED.""TotalAmount"",
                  ""UpdatedAtUtc"" = NOW();
                ";

            int affected = await _db.Database.ExecuteSqlInterpolatedAsync(sql, context.CancellationToken);

            _logger.LogInformation("Upsert executed: {Affected} rows affected for Merchant={Merchant}, Date={Date}, Delta={Delta}",
                affected, message.MerchantId, date, signedAmount);

            // Invalidação de cache
            var listKey = $"dailyBalances:{date:yyyy-MM-dd}";
            await _redis.KeyDeleteAsync(listKey);
            _logger.LogInformation("Cache invalidated for key {Key}", listKey);
        }
    }
}