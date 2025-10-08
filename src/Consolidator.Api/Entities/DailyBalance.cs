using System;

namespace Consolidator.Api.Entities
{
    /// <summary>
    /// Representa o saldo diário consolidado de um comerciante.
    /// </summary>
    public class DailyBalance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MerchantId { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}