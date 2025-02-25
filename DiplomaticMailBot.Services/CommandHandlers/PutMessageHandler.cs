using System.Text.RegularExpressions;
using DiplomaticMailBot.Common.Configuration;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Domain;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.ServiceModels.DiplomaticMailCandidate;
using DiplomaticMailBot.TelegramInterop.Extensions;
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
    private readonly DiplomaticMailCandidatesRepository _diplomaticMailCandidatesRepository;
    private readonly PreviewGenerator _previewGenerator;
    private readonly SlotTemplateRepository _slotTemplateRepository;
    private readonly SlotDateCalculator _slotDateCalculator;

    public PutMessageHandler(
        IOptions<BotConfiguration> options,
        TimeProvider timeProvider,
        ITelegramBotClient telegramBotClient,
        DiplomaticMailCandidatesRepository diplomaticMailCandidatesRepository,
        PreviewGenerator previewGenerator,
        SlotTemplateRepository slotTemplateRepository,
        SlotDateCalculator slotDateCalculator)
    {
        _options = options;
        _timeProvider = timeProvider;
        _telegramBotClient = telegramBotClient;
        _diplomaticMailCandidatesRepository = diplomaticMailCandidatesRepository;
        _previewGenerator = previewGenerator;
        _slotTemplateRepository = slotTemplateRepository;
        _slotDateCalculator = slotDateCalculator;
    }

    public async Task HandlePutMessageAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var userCommandText = userCommand.Text ?? string.Empty;
        var replyToMessage = userCommand.ReplyToMessage;

        if (replyToMessage is null)
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ответьте на сообщение командой {BotCommands.PutMessage} <алиас чата латиницей>, чтобы вынести его на голосование", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
        else
        {
            var slotTemplate = await _slotTemplateRepository.GetAsync(cancellationToken);
            if (slotTemplate is null)
            {
                await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Ошибка конфигурации бота: не найдено расписание", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            }
            else if (_slotDateCalculator.IsVotingGoingOn(slotTemplate.VoteStartAt, slotTemplate.VoteEndAt))
            {
                var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
                var dateNow = DateOnly.FromDateTime(utcNow);
                var voteEndsAt = new DateTime(dateNow, slotTemplate.VoteEndAt, DateTimeKind.Utc);
                var voteEndsIn = voteEndsAt - utcNow;
                var voteEndsInStr = voteEndsIn.Humanize(precision: 2, culture: _options.Value.GetCultureInfo());

                await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Голосование уже идёт, дождитесь его окончания (осталось {voteEndsInStr})", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
            }
            else
            {
                var match = PutMessageRegex().Match(userCommandText);
                if (match.Success)
                {
                    var targetChatAlias = match.Groups["alias"].Value.ToLowerInvariant();

                    var nextVoteSlotDate = _slotDateCalculator.GetNextAvailableSlotDate(slotTemplate.VoteStartAt);

                    var putResult = await _diplomaticMailCandidatesRepository.PutAsync(new DiplomaticMailCandidatePutSm
                    {
                        SlotTemplateId = slotTemplate.Id,
                        NextVoteSlotDate = nextVoteSlotDate,
                        MessageId = replyToMessage.MessageId,
                        Preview = _previewGenerator.GetMessagePreview(replyToMessage, 100),
                        SubmitterId = userCommand.From?.Id ?? 0,
                        AuthorId = replyToMessage.From?.Id ?? 0,
                        AuthorName = _previewGenerator.GetAuthorName(replyToMessage, 100),
                        SourceChatId = userCommand.Chat.Id,
                        TargetChatAlias = targetChatAlias,
                    }, cancellationToken);

                    var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
                    var voteStart = new DateTime(nextVoteSlotDate, slotTemplate.VoteStartAt, DateTimeKind.Utc);
                    var voteStartsIn = voteStart - utcNow;

                    await putResult.MatchAsync(
                        async err =>
                        {
                            return err.Code switch
                            {
                                (int)EventCode.SourceChatNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Текущий чат не зарегистрирован, используйте команду {BotCommands.RegisterChat} <алиас чата латиницей>", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                (int)EventCode.TargetChatNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Чат с алиасом '{targetChatAlias}' не найден", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                (int)EventCode.CanNotSendMailToSelf => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Нельзя отправить послание самому себе", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                (int)EventCode.OutgoingRelationDoesNotExist => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Дипломатические отношения с целевым чатом пока не установлены. Отправьте команду {BotCommands.EstablishRelations} {targetChatAlias}", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                (int)EventCode.IncomingRelationDoesNotExist => await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Дипломатические отношения с целевым чатом пока не установлены. Дождитесь подтверждения от другого чата.", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                (int)EventCode.MailCandidateAlreadyExists => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Сообщение уже вынесено на голосование", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                                (int)EventCode.MailCandidateLimitReached => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Достигнут лимит вариантов для голосования (10 штук)", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
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
        }
    }

    [GeneratedRegex(@$"^{BotCommands.PutMessage}(?:@(?<botname>[A-Za-z0-9_]+))?\s+(?<alias>[A-Za-z0-9_]+)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, 500)]
    private static partial Regex PutMessageRegex();
}
