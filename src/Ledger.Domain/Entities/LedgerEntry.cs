using System;

namespace Ledger.Domain.Entities
{
    /// <summary>
    /// Representa um lançamento de débito ou crédito registrado pelo comerciante.
    /// </summary>
    public class LedgerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MerchantId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public EntryType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Calcula o valor positivo ou negativo do lançamento, conforme o tipo.
        /// Créditos incrementam o saldo; débitos decrementam.
        /// </summary>
        public decimal SignedAmount => Type == EntryType.Credit ? Amount : -Amount;
    }
}