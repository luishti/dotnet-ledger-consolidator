using System;

namespace Shared.Messaging.Events
{
    /// <summary>
    /// Evento publicado quando um lançamento é registrado no serviço de Ledger.
    /// Inclui informações essenciais para o consolidado calcular o saldo diário.
    /// </summary>
    public record EntryRecorded(
        Guid Id,
        string MerchantId,
        decimal Amount,
        string Type,
        DateTime CreatedAt
    );
}