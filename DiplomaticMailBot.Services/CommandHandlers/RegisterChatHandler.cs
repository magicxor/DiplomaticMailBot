using System.Text.RegularExpressions;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Domain;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.ServiceModels.RegisteredChat;
using DiplomaticMailBot.TelegramInterop.Extensions;
using DiplomaticMailBot.TelegramInterop.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Services.CommandHandlers;

public sealed partial class RegisterChatHandler
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly TelegramInfoService _telegramInfoService;
    private readonly RegisteredChatRepository _registeredChatRepository;
    private readonly PreviewGenerator _previewGenerator;

    public RegisterChatHandler(
        ITelegramBotClient telegramBotClient,
        TelegramInfoService telegramInfoService,
        RegisteredChatRepository registeredChatRepository,
        PreviewGenerator previewGenerator)
    {
        _telegramBotClient = telegramBotClient;
        _telegramInfoService = telegramInfoService;
        _registeredChatRepository = registeredChatRepository;
        _previewGenerator = previewGenerator;
    }

    public async Task HandleListChatsAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        var registeredChats = await _registeredChatRepository.ListRegisteredChatsAsync(cancellationToken);

        var registeredChatsString = registeredChats.Any()
            ? string.Join(
                "\n",
                registeredChats
                    .OrderBy(chat => chat.ChatAlias)
                    .ThenBy(chat => chat.ChatTitle)
                    .ThenBy(chat => chat.CreatedAt)
                    .ThenBy(chat => chat.Id)
                    .Select(chat => string.Join(
                        " - ",
                        StringExtensions.GetNonEmpty(
                            chat.CreatedAt?.ToString("yyyy-MM-dd"),
                            _previewGenerator.GetChatDisplayString(chat.ChatAlias, chat.ChatTitle))
                        )))
            : "Нет зарегистрированных чатов";

        await _telegramBotClient.SendMessage(userCommand.Chat.Id, registeredChatsString, replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
    }

    public async Task HandleRegisterChatAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
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

    public async Task HandleDeregisterChatAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        var userCommandText = userCommand.Text ?? string.Empty;

        var match = DeregisterChatRegex().Match(userCommandText);
        if (match.Success)
        {
            if (await _telegramInfoService.IsSentByChatAdminAsync(userCommand, cancellationToken))
            {
                var deregisteredChatAlias = match.Groups["alias"].Value.ToLowerInvariant();

                var deleteResult = await _registeredChatRepository.DeleteAsync(userCommand.Chat.Id, deregisteredChatAlias, cancellationToken);

                await deleteResult.MatchAsync(
                    async err =>
                    {
                        return err.Code switch
                        {
                            (int)EventCode.RegisteredChatNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Чат не найден", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            (int)EventCode.RegisteredChatAliasMismatch => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Введённый алиас чата не совпадает с реальным алиасом текущего чата", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
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
