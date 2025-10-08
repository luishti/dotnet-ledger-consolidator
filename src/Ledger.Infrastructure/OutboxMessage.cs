using System;

namespace Ledger.Infrastructure
{
    /// <summary>
    /// Representa uma mensagem a ser publicada no broker. Utilizado pelo padrão Outbox.
    /// </summary>
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}