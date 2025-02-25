using System.Text.RegularExpressions;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Domain;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.TelegramInterop.Extensions;
using DiplomaticMailBot.TelegramInterop.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Services.CommandHandlers;

public sealed partial class EstablishRelationsHandler
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly TelegramInfoService _telegramInfoService;
    private readonly DiplomaticRelationRepository _diplomaticRelationRepository;
    private readonly PreviewGenerator _previewGenerator;

    public EstablishRelationsHandler(
        ITelegramBotClient telegramBotClient,
        TelegramInfoService telegramInfoService,
        DiplomaticRelationRepository diplomaticRelationRepository,
        PreviewGenerator previewGenerator)
    {
        _telegramBotClient = telegramBotClient;
        _telegramInfoService = telegramInfoService;
        _diplomaticRelationRepository = diplomaticRelationRepository;
        _previewGenerator = previewGenerator;
    }

    public async Task HandleEstablishRelationsAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var userCommandText = userCommand.Text ?? string.Empty;

        var match = EstablishRelationsRegex().Match(userCommandText);
        if (match.Success)
        {
            if (await _telegramInfoService.IsSentByChatAdminAsync(userCommand, cancellationToken))
            {
                var targetChatAlias = match.Groups["alias"].Value.ToLowerInvariant();

                var establishRelationsResult = await _diplomaticRelationRepository.EstablishRelationsAsync(userCommand.Chat.Id, targetChatAlias, cancellationToken);

                await establishRelationsResult.MatchAsync(
                    async err =>
                    {
                        List<Message> sentTelegramMessages = err.Code switch
                        {
                            (int)EventCode.SourceChatNotFound => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Текущий чат не зарегистрирован, используйте команду {BotCommands.RegisterChat} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                            (int)EventCode.TargetChatNotFound => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Чат с алиасом '{targetChatAlias}' не найден", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                            (int)EventCode.CanNotEstablishRelationsWithSelf => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Нельзя установить дипломатические отношения с самим собой", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                            (int)EventCode.OutgoingRelationAlreadyExists => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Приглашение на установление дипломатических отношений с чатом '{targetChatAlias}' уже отправлено", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                            _ => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось установить дипломатические отношения: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                        };
                        return sentTelegramMessages;
                    },
                    async relationsInfoSm =>
                    {
                        List<Message> sentTelegramMessages = relationsInfoSm switch
                        {
                            { IsOutgoingRelationPresent: true, IsIncomingRelationPresent: true } => [
                                await _telegramBotClient.SendMessage(relationsInfoSm.SourceChatId, $"Установлены дипломатические отношения с чатом {_previewGenerator.GetChatDisplayString(relationsInfoSm.TargetChatAlias, relationsInfoSm.TargetChatTitle)}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                await _telegramBotClient.SendMessage(relationsInfoSm.TargetChatId, $"Установлены дипломатические отношения с чатом {_previewGenerator.GetChatDisplayString(relationsInfoSm.SourceChatAlias, relationsInfoSm.SourceChatTitle)}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            ],
                            { IsOutgoingRelationPresent: true, IsIncomingRelationPresent: false } => [
                                await _telegramBotClient.SendMessage(relationsInfoSm.SourceChatId, $"Отправлен запрос на установление дипломатических отношений с чатом {_previewGenerator.GetChatDisplayString(relationsInfoSm.TargetChatAlias, relationsInfoSm.TargetChatTitle)}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                await _telegramBotClient.SendMessage(relationsInfoSm.TargetChatId, $"Получен запрос на установление дипломатических отношений от чата {_previewGenerator.GetChatDisplayString(relationsInfoSm.SourceChatAlias, relationsInfoSm.SourceChatTitle)}. Чтобы принять запрос, отправьте команду {BotCommands.EstablishRelations} {relationsInfoSm.SourceChatAlias}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                            ],
                            _ => [await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось установить дипломатические отношения: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)],
                        };
                        return sentTelegramMessages;
                    });
            }
            else
            {
                await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Только администраторы могут устанавливать дипломатические отношения", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ожидаемый формат команды: {BotCommands.EstablishRelations} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
    }

    [GeneratedRegex(@$"^{BotCommands.EstablishRelations}(?:@(?<botname>[A-Za-z0-9_]+))?\s+(?<alias>[A-Za-z0-9_]+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, 500)]
    private static partial Regex EstablishRelationsRegex();
}
