using System.Globalization;
using System.Text.RegularExpressions;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Domain.Contracts;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;
using DiplomaticMailBot.Infra.Telegram.Contracts;
using DiplomaticMailBot.Infra.Telegram.Implementations.Extensions;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Services.CommandHandlers;

public sealed partial class RegisterChatHandler
{
    private readonly ILogger<RegisterChatHandler> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly ITelegramInfoService _telegramInfoService;
    private readonly IRegisteredChatRepository _registeredChatRepository;
    private readonly IPreviewGenerator _previewGenerator;

    public RegisterChatHandler(
        ILogger<RegisterChatHandler> logger,
        ITelegramBotClient telegramBotClient,
        ITelegramInfoService telegramInfoService,
        IRegisteredChatRepository registeredChatRepository,
        IPreviewGenerator previewGenerator)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _telegramInfoService = telegramInfoService;
        _registeredChatRepository = registeredChatRepository;
        _previewGenerator = previewGenerator;
    }

    public async Task HandleListChatsAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var registeredChats = await _registeredChatRepository.ListRegisteredChatsAsync(cancellationToken);

        var registeredChatsString = registeredChats.Count > 0
            ? string.Join(
                '\n',
                registeredChats
                    .OrderBy(chat => chat.ChatAlias)
                    .ThenBy(chat => chat.ChatTitle)
                    .ThenBy(chat => chat.CreatedAt)
                    .ThenBy(chat => chat.Id)
                    .Select(chat => string.Join(
                        " - ",
                        StringExtensions.FilterNonEmpty(
                            chat.CreatedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            _previewGenerator.GetChatDisplayString(chat.ChatAlias, chat.ChatTitle))
                        )))
            : "Нет зарегистрированных чатов";
        registeredChatsString = registeredChatsString.TryLeft(Defaults.NormalMessageMaxChars);

        await _telegramBotClient.SendMessage(userCommand.Chat.Id, registeredChatsString, replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
    }

    public async Task HandleRegisterChatAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var userCommandText = userCommand.Text ?? string.Empty;
        var chatTitle = userCommand.Chat.Title ?? string.Empty;

        var match = RegisterChatRegex().Match(userCommandText);
        if (match.Success)
        {
            if (await _telegramInfoService.IsSentByChatAdminAsync(userCommand, cancellationToken))
            {
                var registeredChatAlias = match.Groups["alias"].Value.ToLowerInvariant();

                var createOrUpdateResult = await _registeredChatRepository.CreateOrUpdateAsync(new RegisteredChatCreateOrUpdateRequestSm
                {
                    ChatId = userCommand.Chat.Id,
                    ChatTitle = chatTitle,
                    ChatAlias = registeredChatAlias,
                }, cancellationToken);

                await createOrUpdateResult.MatchAsync(
                    async err =>
                    {
                        return err.Code switch
                        {
                            (int)EventCode.ChatRegistrationUpdateRateLimitExceeded => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Алиас чата можно обновлять не чаще раза в месяц", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            (int)EventCode.AliasIsTaken => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Алиас '{registeredChatAlias}' уже занят", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось зарегистрировать чат: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        };
                    },
                    async result =>
                    {
                        return result switch
                        {
                            { IsCreated: true } => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Чат зарегистрирован в боте. Алиас: {result.ChatAlias}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            { IsUpdated: true } => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Чат обновлён в боте. Новый алиас: {result.ChatAlias}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось зарегистрировать чат: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        };
                    });
            }
            else
            {
                await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Только администраторы могут регистрировать чаты", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ожидаемый формат команды: {BotCommands.RegisterChat} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
    }

    public async Task HandleDeregisterExitedChatAsync(User bot, Chat chat, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(chat);

        _logger.LogDebug("Deregistering chat {ChatId} ({ChatType}, {ChatTitle}) because the bot was kicked or left or restricted",
            chat.Id,
            chat.Type,
            chat.Title);

        await _registeredChatRepository.DeleteAsync(chat.Id, cancellationToken);
    }

    public async Task HandleDeregisterChatAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var userCommandText = userCommand.Text ?? string.Empty;

        var match = DeregisterChatRegex().Match(userCommandText);
        if (match.Success)
        {
            if (await _telegramInfoService.IsSentByChatAdminAsync(userCommand, cancellationToken))
            {
                var deregisteredChatAlias = match.Groups["alias"].Value.ToLowerInvariant();

                var deleteResult = await _registeredChatRepository.DeleteAsync(userCommand.Chat.Id, deregisteredChatAlias, cancellationToken: cancellationToken);

                await deleteResult.MatchAsync(
                    async err =>
                    {
                        return err.Code switch
                        {
                            (int)EventCode.ChatNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Чат не найден", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            (int)EventCode.ChatAliasMismatch => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Введённый алиас чата не совпадает с реальным алиасом текущего чата", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось дерегистрировать чат: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        };
                    },
                    async result =>
                    {
                        return result switch
                        {
                            true => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Чат удалён из бота", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось дерегистрировать чат: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        };
                    });
            }
            else
            {
                await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Только администраторы могут дерегистрировать чаты", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ожидаемый формат команды: {BotCommands.DeregisterChat} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
    }

    [GeneratedRegex(@$"^{BotCommands.RegisterChat}(?:@(?<botname>[A-Za-z0-9_]+))?\s+(?<alias>[A-Za-z0-9_]+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, 500)]
    private static partial Regex RegisterChatRegex();

    [GeneratedRegex(@$"^{BotCommands.DeregisterChat}(?:@(?<botname>[A-Za-z0-9_]+))?\s+(?<alias>[A-Za-z0-9_]+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, 500)]
    private static partial Regex DeregisterChatRegex();
}
