using MediatR;
using System;

namespace Ledger.Application.Commands
{
    /// <summary>
    /// Comando para criar um lançamento no serviço de Ledger.
    /// </summary>
    public record CreateEntryCommand(string MerchantId, decimal Amount, string Type) : IRequest<Guid>;
}