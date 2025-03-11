using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Infra.Database.DbContexts;
using DiplomaticMailBot.Infra.Entities;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.Repositories.Implementations.Extensions;
using DiplomaticMailBot.Infra.ServiceModels.DiplomaticRelation;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Infra.Repositories.Implementations;

public sealed class DiplomaticRelationRepository : IDiplomaticRelationRepository
{
    private readonly ILogger<DiplomaticRelationRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;
    private readonly TimeProvider _timeProvider;

    public DiplomaticRelationRepository(
        ILogger<DiplomaticRelationRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<Either<DiplomaticRelationsInfoSm, Error>> EstablishRelationsAsync(long sourceChatId, string targetChatAlias, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Establishing relations between {SourceChatId} and {TargetChatAlias}", sourceChatId, targetChatAlias);

        ArgumentOutOfRangeException.ThrowIfZero(sourceChatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetChatAlias);

        await using var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var chats = await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .Where(x => x.ChatId == sourceChatId || x.ChatAlias == targetChatAlias)
            .ToListAsync(cancellationToken);

        var sourceChat = chats.OrderBy(x => x.Id).FirstOrDefault(x => x.ChatId == sourceChatId);
        var targetChat = chats.OrderBy(x => x.Id).FirstOrDefault(x => x.ChatAlias == targetChatAlias);

        if (sourceChat is null)
        {
            _logger.LogInformation("Source chat not found: {SourceChatId}", sourceChatId);
            return new DomainError(EventCode.SourceChatNotFound.ToInt(), "Source chat not found");
        }

        if (targetChat is null)
        {
            _logger.LogInformation("Target chat not found: {TargetChatAlias}", targetChatAlias);
            return new DomainError(EventCode.TargetChatNotFound.ToInt(), "Target chat not found");
        }

        if (sourceChat.IsSameAs(targetChat))
        {
            _logger.LogInformation("Can not establish relations with self");
            return new DomainError(EventCode.CanNotEstablishRelationsWithSelf.ToInt(), "Can not establish relations with self");
        }

        var relations = await applicationDbContext.DiplomaticRelations
            .TagWithCallSite()
            .Where(x => (x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id) || (x.SourceChatId == targetChat.Id && x.TargetChatId == sourceChat.Id))
            .ToListAsync(cancellationToken);

        var outgoingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id);
        var incomingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == targetChat.Id && x.TargetChatId == sourceChat.Id);

        if (outgoingRelation is not null)
        {
            _logger.LogInformation("Relation already exists: {SourceChatId} -> {TargetChatAlias}", sourceChatId, targetChatAlias);
            return new DomainError(EventCode.OutgoingRelationAlreadyExists.ToInt(), "Relation already exists");
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        outgoingRelation = new DiplomaticRelation
        {
            CreatedAt = utcNow,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        applicationDbContext.DiplomaticRelations.Add(outgoingRelation);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Relations established between {SourceChatId} and {TargetChatAlias}", sourceChatId, targetChatAlias);

        return new DiplomaticRelationsInfoSm
        {
            IsOutgoingRelationPresent = true,
            IsIncomingRelationPresent = incomingRelation is not null,
            SourceChatId = sourceChat.ChatId,
            SourceChatTitle = sourceChat.ChatTitle,
            SourceChatAlias = sourceChat.ChatAlias,
            TargetChatId = targetChat.ChatId,
            TargetChatTitle = targetChat.ChatTitle,
            TargetChatAlias = targetChat.ChatAlias,
        };
    }

    public async Task<Either<DiplomaticRelationsInfoSm, Error>> BreakOffRelationsAsync(long sourceChatId, string targetChatAlias, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Breaking off relations between {SourceChatId} and {TargetChatAlias}", sourceChatId, targetChatAlias);

        ArgumentOutOfRangeException.ThrowIfZero(sourceChatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetChatAlias);

        await using var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var chats = await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .Where(x => x.ChatId == sourceChatId || x.ChatAlias == targetChatAlias)
            .ToListAsync(cancellationToken);

        var sourceChat = chats.OrderBy(x => x.Id).FirstOrDefault(x => x.ChatId == sourceChatId);
        var targetChat = chats.OrderBy(x => x.Id).FirstOrDefault(x => x.ChatAlias == targetChatAlias);

        if (sourceChat is null)
        {
            _logger.LogInformation("Source chat not found: {SourceChatId}", sourceChatId);
            return new DomainError(EventCode.SourceChatNotFound.ToInt(), "Source chat not found");
        }

        if (targetChat is null)
        {
            _logger.LogInformation("Target chat not found: {TargetChatAlias}", targetChatAlias);
            return new DomainError(EventCode.TargetChatNotFound.ToInt(), "Target chat not found");
        }

        if (sourceChat.IsSameAs(targetChat))
        {
            _logger.LogInformation("Can not break off relations with self");
            return new DomainError(EventCode.CanNotBreakOffRelationsWithSelf.ToInt(), "Can not break off relations with self");
        }

        var relations = await applicationDbContext.DiplomaticRelations
            .TagWithCallSite()
            .Where(x => (x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id) || (x.SourceChatId == targetChat.Id && x.TargetChatId == sourceChat.Id))
            .ToListAsync(cancellationToken);

        var outgoingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id);
        var incomingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == targetChat.Id && x.TargetChatId == sourceChat.Id);

        if (outgoingRelation is null)
        {
            _logger.LogInformation("Relation does not exist: {SourceChatId} -> {TargetChatAlias}", sourceChatId, targetChatAlias);
            return new DomainError(EventCode.OutgoingRelationDoesNotExist.ToInt(), "Relation does not exist");
        }

        applicationDbContext.DiplomaticRelations.Remove(outgoingRelation);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return new DiplomaticRelationsInfoSm
        {
            IsOutgoingRelationPresent = false,
            IsIncomingRelationPresent = incomingRelation is not null,
            SourceChatId = sourceChat.ChatId,
            SourceChatTitle = sourceChat.ChatTitle,
            SourceChatAlias = sourceChat.ChatAlias,
            TargetChatId = targetChat.ChatId,
            TargetChatTitle = targetChat.ChatTitle,
            TargetChatAlias = targetChat.ChatAlias,
        };
    }
}
