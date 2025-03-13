using System.Text.RegularExpressions;
using DiplomaticMailBot.Common.Configuration;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Common.Utils;
using DiplomaticMailBot.Domain.Contracts;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.ServiceModels.MessageCandidate;
using DiplomaticMailBot.Infra.Telegram.Implementations.Extensions;
using Humanizer;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Services.CommandHandlers;

public sealed partial class PutMessageHandler
{
    private readonly IOptions<BotConfiguration> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IMessageCandidateRepository _messageCandidateRepository;
    private readonly IPreviewGenerator _previewGenerator;
    private readonly IRegisteredChatRepository _registeredChatRepository;

    public PutMessageHandler(
        IOptions<BotConfiguration> options,
        TimeProvider timeProvider,
        ITelegramBotClient telegramBotClient,
        IMessageCandidateRepository messageCandidateRepository,
        IPreviewGenerator previewGenerator,
        IRegisteredChatRepository registeredChatRepository)
    {
        _options = options;
        _timeProvider = timeProvider;
        _telegramBotClient = telegramBotClient;
        _messageCandidateRepository = messageCandidateRepository;
        _previewGenerator = previewGenerator;
        _registeredChatRepository = registeredChatRepository;
    }

    public async Task HandlePutMessageAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var userCommandText = userCommand.Text ?? string.Empty;
        var replyToMessage = userCommand.ReplyToMessage;

        if (replyToMessage is null)
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ответьте на сообщение командой {BotCommands.PutMessage} <алиас чата латиницей>, чтобы вынести его на голосование", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            return;
        }

        var slotTemplateSm = await _registeredChatRepository.GetChatSlotTemplateByTelegramChatIdAsync(userCommand.Chat.Id, cancellationToken);
        if (slotTemplateSm is null)
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Ошибка конфигурации чата: не найдено расписание", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            return;
        }

        var isVoteGoingOn = SlotDateUtils.IsVoteGoingOn(utcNow, slotTemplateSm.VoteStartAt, slotTemplateSm.VoteEndAt);
        if (isVoteGoingOn)
        {
            var voteEndsIn = SlotDateUtils.VoteEndsIn(utcNow, slotTemplateSm.VoteEndAt);
            var voteEndsInStr = voteEndsIn.Humanize(precision: 2, culture: _options.Value.GetCultureInfo());
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Голосование уже идёт, дождитесь его окончания (осталось {voteEndsInStr})", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            return;
        }

        var match = PutMessageRegex().Match(userCommandText);
        if (match.Success)
        {
            var targetChatAlias = match.Groups["alias"].Value.ToLowerInvariant();

            var nextVoteSlotDate = SlotDateUtils.GetNearestVoteStartDate(utcNow, slotTemplateSm.VoteStartAt);

            var putResult = await _messageCandidateRepository.PutAsync(new MessageCandidatePutSm
            {
                SlotTemplateId = slotTemplateSm.Id,
                NextVoteSlotDate = nextVoteSlotDate,
                MessageId = replyToMessage.MessageId,
                Preview = _previewGenerator.GetMessagePreview(replyToMessage, 100),
                SubmitterId = userCommand.From?.Id ?? 0,
                AuthorId = replyToMessage.From?.Id ?? 0,
                AuthorName = _previewGenerator.GetAuthorName(replyToMessage, 100),
                SourceChatId = userCommand.Chat.Id,
                TargetChatAlias = targetChatAlias,
            }, cancellationToken);

            var voteStartsIn = SlotDateUtils.VoteStartsIn(utcNow, slotTemplateSm.VoteStartAt);

            await putResult.MatchAsync(
                async err =>
                {
                    return err.Code switch
                    {
                        (int)EventCode.SourceChatNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Текущий чат не зарегистрирован, используйте команду {BotCommands.RegisterChat} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        (int)EventCode.TargetChatNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Чат с алиасом '{targetChatAlias}' не найден", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        (int)EventCode.CanNotSendMessageToSelf => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Нельзя отправить послание самому себе", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        (int)EventCode.OutgoingRelationDoesNotExist => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Дипломатические отношения с целевым чатом пока не установлены. Отправьте команду {BotCommands.EstablishRelations} {targetChatAlias}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        (int)EventCode.IncomingRelationDoesNotExist => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Дипломатические отношения с целевым чатом пока не установлены. Дождитесь подтверждения от другого чата.", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        (int)EventCode.MessageCandidateAlreadyExists => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Сообщение уже вынесено на голосование", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        (int)EventCode.MessageCandidateLimitReached => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Достигнут лимит вариантов для голосования (10 штук)", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось вынести сообщение на голосование: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                    };
                },
                async _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Сообщение вынесено на голосование (до начала осталось {voteStartsIn.Humanize(precision: 2, culture: _options.Value.GetCultureInfo())})", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken));
        }
        else
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ожидаемый формат команды: {BotCommands.PutMessage} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
    }

    [GeneratedRegex(@$"^{BotCommands.PutMessage}(?:@(?<botname>[A-Za-z0-9_]+))?\s+(?<alias>[A-Za-z0-9_]+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture, 500)]
    private static partial Regex PutMessageRegex();
}
