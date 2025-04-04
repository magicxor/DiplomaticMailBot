﻿using System.Globalization;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Infra.Database.DbContexts;
using DiplomaticMailBot.Infra.Entities;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;
using DiplomaticMailBot.Infra.ServiceModels.SlotTemplate;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Infra.Repositories.Implementations;

public sealed class RegisteredChatRepository : IRegisteredChatRepository
{
    private readonly ILogger<RegisteredChatRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;
    private readonly TimeProvider _timeProvider;

    public RegisteredChatRepository(
        ILogger<RegisteredChatRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyCollection<RegisteredChatSm>> ListRegisteredChatsAsync(CancellationToken cancellationToken = default)
    {
        await using var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        return await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .Select(x => new RegisteredChatSm
            {
                Id = x.Id,
                ChatId = x.ChatId,
                ChatAlias = x.ChatAlias,
                ChatTitle = x.ChatTitle,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SlotTemplateSm?> GetChatSlotTemplateByTelegramChatIdAsync(long telegramChatId, CancellationToken cancellationToken = default)
    {
        await using var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        return await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .Include(chat => chat.SlotTemplate)
            .Where(chat => chat.ChatId == telegramChatId && chat.SlotTemplate != null)
            .Select(chat => chat.SlotTemplate!)
            .Select(template => new SlotTemplateSm
            {
                Id = template.Id,
                VoteStartAt = template.VoteStartAt,
                VoteEndAt = template.VoteEndAt,
                Number = template.Number,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Either<RegisteredChatCreateOrUpdateResultSm, Error>> CreateOrUpdateAsync(RegisteredChatCreateOrUpdateRequestSm registeredChatCreateOrUpdateRequestSm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registeredChatCreateOrUpdateRequestSm);
        ArgumentOutOfRangeException.ThrowIfZero(registeredChatCreateOrUpdateRequestSm.ChatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(registeredChatCreateOrUpdateRequestSm.ChatAlias);
        ArgumentException.ThrowIfNullOrWhiteSpace(registeredChatCreateOrUpdateRequestSm.ChatTitle);

        _logger.LogTrace("Creating or updating registered chat {Alias}", registeredChatCreateOrUpdateRequestSm.ChatAlias);

        await using var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var isAliasTaken = await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .AnyAsync(x =>
                x.ChatAlias == registeredChatCreateOrUpdateRequestSm.ChatAlias
                && x.ChatId != registeredChatCreateOrUpdateRequestSm.ChatId,
                cancellationToken);

        if (isAliasTaken)
        {
            _logger.LogInformation("Alias {Alias} is already taken", registeredChatCreateOrUpdateRequestSm.ChatAlias);
            return new DomainError(EventCode.AliasIsTaken.ToInt(), "Alias is already taken");
        }

        var registeredChat = await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .IgnoreQueryFilters()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.ChatId == registeredChatCreateOrUpdateRequestSm.ChatId, cancellationToken);

        var isCreated = false;
        var isUpdated = false;
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var chatRegistrationUpdateCooldown = TimeSpan.FromDays(30);

        if (registeredChat is not null
            && registeredChat.UpdatedAt.HasValue
            && registeredChat.UpdatedAt.Value.Add(chatRegistrationUpdateCooldown) > utcNow)
        {
            _logger.LogInformation("Chat registration update rate limit exceeded for chat {ChatId}", registeredChatCreateOrUpdateRequestSm.ChatId);
            return new DomainError(EventCode.ChatRegistrationUpdateRateLimitExceeded.ToInt(), "Chat registration update rate limit exceeded");
        }
        else if (registeredChat is not null)
        {
            _logger.LogInformation("Updating registered chat {ChatId}. New alias: {Alias}, new title: {Title}",
                registeredChat.ChatId,
                registeredChatCreateOrUpdateRequestSm.ChatAlias,
                registeredChatCreateOrUpdateRequestSm.ChatTitle);

            registeredChat.ChatAlias = registeredChatCreateOrUpdateRequestSm.ChatAlias;
            registeredChat.ChatTitle = registeredChatCreateOrUpdateRequestSm.ChatTitle;
            registeredChat.UpdatedAt = utcNow;
            registeredChat.IsDeleted = false;

            isUpdated = true;
        }
        else
        {
            _logger.LogInformation("Creating registered chat {ChatId}, alias: {Alias}, title: {Title}",
                registeredChatCreateOrUpdateRequestSm.ChatId,
                registeredChatCreateOrUpdateRequestSm.ChatAlias,
                registeredChatCreateOrUpdateRequestSm.ChatTitle);

            var defaultSlotTemplateId = await applicationDbContext.SlotTemplates
                .TagWithCallSite()
                .OrderBy(slotTemplate => slotTemplate.Id)
                .Select(slotTemplate => slotTemplate.Id)
                .FirstOrDefaultAsync(cancellationToken);

            registeredChat = new RegisteredChat
            {
                ChatId = registeredChatCreateOrUpdateRequestSm.ChatId,
                ChatTitle = registeredChatCreateOrUpdateRequestSm.ChatTitle,
                ChatAlias = registeredChatCreateOrUpdateRequestSm.ChatAlias,
                CreatedAt = utcNow,
                SlotTemplateId = defaultSlotTemplateId,
            };

            applicationDbContext.RegisteredChats.Add(registeredChat);

            isCreated = true;
        }

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogTrace("Registered chat {Alias} created or updated", registeredChatCreateOrUpdateRequestSm.ChatAlias);

        return new RegisteredChatCreateOrUpdateResultSm
        {
            IsCreated = isCreated,
            IsUpdated = isUpdated,
            ChatId = registeredChatCreateOrUpdateRequestSm.ChatId,
            ChatTitle = registeredChatCreateOrUpdateRequestSm.ChatTitle,
            ChatAlias = registeredChatCreateOrUpdateRequestSm.ChatAlias,
        };
    }

    public async Task<Either<bool, Error>> DeleteAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(chatId, string.Empty, checkAlias: false, cancellationToken);
    }

    public async Task<Either<bool, Error>> DeleteAsync(long chatId, string chatAlias, bool checkAlias = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting registered chat {ChatId}", chatId);

        await using var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var registeredChat = await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.ChatId == chatId, cancellationToken);

        if (registeredChat is null)
        {
            _logger.LogInformation("Chat {ChatId} not found", chatId);
            return new DomainError(EventCode.ChatNotFound.ToInt(), "Chat not found");
        }

        if (checkAlias && !registeredChat.ChatAlias.EqualsIgnoreCase(chatAlias))
        {
            _logger.LogInformation("Chat {ChatId} alias mismatch; expected: {ExpectedAlias}, actual: {ActualAlias}. Won't delete",
                chatId,
                chatAlias,
                registeredChat.ChatAlias);

            return new DomainError(EventCode.ChatAliasMismatch.ToInt(), "Chat alias mismatch");
        }

        registeredChat.ChatAlias = Guid.NewGuid().ToString("d", CultureInfo.InvariantCulture);
        registeredChat.IsDeleted = true;
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Chat {ChatId} deleted", chatId);

        return true;
    }
}
