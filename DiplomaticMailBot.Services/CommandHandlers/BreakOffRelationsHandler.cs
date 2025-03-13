using System.Text.RegularExpressions;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Domain.Contracts;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.Telegram.Contracts;
using DiplomaticMailBot.Infra.Telegram.Implementations.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Services.CommandHandlers;

public sealed partial class BreakOffRelationsHandler
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly ITelegramInfoService _telegramInfoService;
    private readonly IDiplomaticRelationRepository _diplomaticRelationRepository;
    private readonly IPreviewGenerator _previewGenerator;

    public BreakOffRelationsHandler(
        ITelegramBotClient telegramBotClient,
        ITelegramInfoService telegramInfoService,
        IDiplomaticRelationRepository diplomaticRelationRepository,
        IPreviewGenerator previewGenerator)
    {
        _telegramBotClient = telegramBotClient;
        _telegramInfoService = telegramInfoService;
        _diplomaticRelationRepository = diplomaticRelationRepository;
        _previewGenerator = previewGenerator;
    }

    public async Task HandleBreakOffRelationsAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var userCommandText = userCommand.Text ?? string.Empty;

        var match = BreakOffRelationsRegex().Match(userCommandText);
        if (match.Success)
        {
            if (await _telegramInfoService.IsSentByChatAdminAsync(userCommand, cancellationToken))
            {
                var targetChatAlias = match.Groups["alias"].Value.ToLowerInvariant();

                var breakOffRelationsResult = await _diplomaticRelationRepository.BreakOffRelationsAsync(userCommand.Chat.Id, targetChatAlias, cancellationToken);

                await breakOffRelationsResult.MatchAsync(async err =>
                {
                    List<Message> sentTelegramMessages = err.Code switch
                    {
                        (int)EventCode.SourceChatNotFound => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Текущий чат не зарегистрирован, используйте команду {BotCommands.RegisterChat} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                        (int)EventCode.TargetChatNotFound => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Чат с алиасом '{targetChatAlias}' не найден", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                        (int)EventCode.CanNotBreakOffRelationsWithSelf => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Нельзя разорвать дипломатические отношения с самим собой", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                        (int)EventCode.OutgoingRelationDoesNotExist => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Дипломатические отношения с чатом '{targetChatAlias}' не установлены", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                        _ => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось разорвать дипломатические отношения: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                    };
                    return sentTelegramMessages;
                }, async result =>
                {
                    List<Message> sentTelegramMessages = result switch
                    {
                        { IsOutgoingRelationPresent: false, IsIncomingRelationPresent: true } => [
                            await _telegramBotClient.SendMessage(result.TargetChatId, $"Чат {_previewGenerator.GetChatDisplayString(result.SourceChatAlias, result.SourceChatTitle)} разорвал с вами дипломатические отношения", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            await _telegramBotClient.SendMessage(result.SourceChatId, $"Вы разорвали дипломатические отношения с чатом {_previewGenerator.GetChatDisplayString(result.TargetChatAlias, result.TargetChatTitle)}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        ],
                        { IsOutgoingRelationPresent: false, IsIncomingRelationPresent: false } => [
                            await _telegramBotClient.SendMessage(result.SourceChatId, $"Вы отменили запрос на установление дипломатических отношений с чатом {_previewGenerator.GetChatDisplayString(result.TargetChatAlias, result.TargetChatTitle)}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        ],
                        _ => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось разорвать дипломатические отношения: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                    };
                    return sentTelegramMessages;
                });
            }
            else
            {
                await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Только администраторы могут разрывать дипломатические отношения", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ожидаемый формат команды: {BotCommands.BreakOffRelations} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
    }

    [GeneratedRegex(@$"^{BotCommands.BreakOffRelations}(?:@(?<botname>[A-Za-z0-9_]+))?\s+(?<alias>[A-Za-z0-9_]+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 500)]
    private static partial Regex BreakOffRelationsRegex();
}
