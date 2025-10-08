using System;
using Ledger.Domain.Entities;
using MediatR;

namespace Ledger.Application.Queries
{
    /// <summary>
    /// Consulta um lançamento pelo seu identificador.
    /// </summary>
    public record GetEntryByIdQuery(Guid Id) : IRequest<LedgerEntry?>;
}