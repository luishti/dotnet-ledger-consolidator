using System;
using System.Threading;
using System.Threading.Tasks;
using Ledger.Domain.Entities;
using Ledger.Infrastructure.Repositories;
using MediatR;

namespace Ledger.Application.Queries
{
    /// <summary>
    /// Manipulador da consulta de lançamento por ID.
    /// </summary>
    public class GetEntryByIdQueryHandler : IRequestHandler<GetEntryByIdQuery, LedgerEntry?>
    {
        private readonly LedgerRepository _repository;

        public GetEntryByIdQueryHandler(LedgerRepository repository)
        {
            _repository = repository;
        }

        public async Task<LedgerEntry?> Handle(GetEntryByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}