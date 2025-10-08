using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ledger.Domain.Entities;
using Ledger.Infrastructure;
using Ledger.Infrastructure.Repositories;
using MediatR;
using Shared.Messaging.Events;

namespace Ledger.Application.Commands
{
    /// <summary>
    /// Manipulador do comando de criação de lançamento. Persistirá o lançamento e
    /// adicionará um registro na outbox para publicação assíncrona do evento.
    /// </summary>
    public class CreateEntryCommandHandler : IRequestHandler<CreateEntryCommand, Guid>
    {
        private readonly LedgerDbContext _context;
        private readonly LedgerRepository _repository;

        public CreateEntryCommandHandler(LedgerDbContext context, LedgerRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<Guid> Handle(CreateEntryCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.MerchantId))
                throw new ArgumentException("MerchantId é obrigatório");
            if (request.Amount <= 0)
                throw new ArgumentException("Amount deve ser maior que zero");

            // Converte a string Type para o enum EntryType
            if (!Enum.TryParse<EntryType>(request.Type, ignoreCase: true, out var entryType))
                throw new ArgumentException($"Tipo de lançamento inválido: {request.Type}");

            // Cria a entidade de lançamento
            var entry = new LedgerEntry
            {
                MerchantId = request.MerchantId,
                Amount = request.Amount,
                Type = entryType,
                CreatedAt = DateTime.UtcNow
            };

            // Persiste no banco
            await _repository.AddAsync(entry, cancellationToken);

            // Cria o evento de domínio e grava na outbox
            var eventPayload = new EntryRecorded(
                entry.Id,
                entry.MerchantId,
                entry.Amount,
                entry.Type.ToString(),
                entry.CreatedAt
            );
            var outbox = new OutboxMessage
            {
                Type = typeof(EntryRecorded).AssemblyQualifiedName ?? typeof(EntryRecorded).FullName!,
                Content = JsonSerializer.Serialize(eventPayload),
                CreatedAt = DateTime.UtcNow
            };
            _context.OutboxMessages.Add(outbox);
            await _context.SaveChangesAsync(cancellationToken);

            return entry.Id;
        }
    }
}