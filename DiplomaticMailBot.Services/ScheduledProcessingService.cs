using DiplomaticMailBot.Common.Configuration;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Domain;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.TelegramInterop.Extensions;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DiplomaticMailBot.Services;

public sealed class ScheduledProcessingService
{
    private readonly IOptions<BotConfiguration> _options;
    private readonly ILogger<ScheduledProcessingService> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly PollRepository _pollRepository;
    private readonly MessageOutboxRepository _messageOutboxRepository;
    private readonly PollOptionParser _pollOptionParser;
    private readonly PreviewGenerator _previewGenerator;

    public ScheduledProcessingService(
        IOptions<BotConfiguration> options,
        ILogger<ScheduledProcessingService> logger,
        ITelegramBotClient telegramBotClient,
        PollRepository pollRepository,
        MessageOutboxRepository messageOutboxRepository,
        PollOptionParser pollOptionParser,
        PreviewGenerator previewGenerator)
    {
        _options = options;
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _pollRepository = pollRepository;
        _messageOutboxRepository = messageOutboxRepository;
        _pollOptionParser = pollOptionParser;
        _previewGenerator = previewGenerator;
    }

    public async Task OpenPendingPollsAsync(CancellationToken stoppingToken = default)
    {
        await _pollRepository.OpenPendingPollsAsync(
            async (fromChat, toChat, timeLeft, mailCandidate, cancellationToken) => await _telegramBotClient.SendMessage(
                fromChat.ChatId,
                $"{_previewGenerator.GetMessageLinkHtml(fromChat.ChatId, mailCandidate.MessageId, "Послание")} в чат {_previewGenerator.GetChatDisplayString(toChat.ChatAlias, toChat.ChatTitle)} будет отправлено через {timeLeft.Humanize(precision: 2, culture: _options.Value.GetCultureInfo())}".TryLeft(2048),
                ParseMode.Html,
                cancellationToken: cancellationToken),
            async (fromChat, toChat, options, cancellationToken) =>
            {
                var inputPollOptions = options
                    .Select(candidate => new InputPollOption(_previewGenerator.GetPollOptionPreview(candidate.MessageId, candidate.AuthorName, candidate.Preview, 20, 100)))
                    .ToList();
                var pollQuestion = $"Какое послание отправляем в {_previewGenerator.GetChatDisplayString(toChat.ChatAlias, toChat.ChatTitle)}?".TryLeft(300);

                _logger.LogInformation("Opening poll in chat {ChatId} with question: {PollQuestion}", fromChat.ChatId, pollQuestion);

                var poll = await _telegramBotClient.SendPoll(
                    fromChat.ChatId,
                    pollQuestion,
                    inputPollOptions,
                    cancellationToken: cancellationToken);

                var detailedPollOptions = options
                    .Select(candidate =>
                        _previewGenerator.GetMessageLinkHtml(
                            fromChat.ChatId,
                            candidate.MessageId,
                            _previewGenerator.GetPollOptionPreview(
                                candidate.MessageId,
                                candidate.AuthorName,
                                candidate.Preview,
                                20,
                                100)))
                    .ToList();
                var detailedPollOptionsStr = string.Join("\n\n", detailedPollOptions);
                var detailedPollOptionsInfoMessageText = $"Ссылки на посты из голосования:\n\n{detailedPollOptionsStr}"
                    .TryLeft(2048)
                    .CutToLastClosingLinkTag();
                await _telegramBotClient.SendMessage(
                    fromChat.ChatId,
                    detailedPollOptionsInfoMessageText,
                    ParseMode.Html,
                    poll.ToReplyParameters(),
                    cancellationToken: cancellationToken);

                return poll.MessageId;
            },
            stoppingToken);
    }

    public async Task CloseExpiredPollsAsync(CancellationToken stoppingToken = default)
    {
        await _pollRepository.CloseExpiredPollsAsync(
            async (chatId, messageId, cancellationToken) =>
            {
                try
                {
                    var closedPoll = await _telegramBotClient.StopPoll(chatId, messageId, cancellationToken: cancellationToken);
                    var chosenOption = closedPoll.Options
                        .OrderByDescending(x => x.VoterCount)
                        .ThenBy(x => x.Text)
                        .FirstOrDefault();
                    return _pollOptionParser.GetMessageId(chosenOption?.Text ?? string.Empty);
                }
                catch (Exception e)
                {
                    return new DomainError(EventCode.ErrorClosingPoll.ToInt(), e.Message, true, false);
                }
            },
            stoppingToken);
    }

    public async Task SendPendingMailsAsync(CancellationToken stoppingToken = default)
    {
        await _messageOutboxRepository.SendPendingMailsAsync(
            async (fromChat, toChat, mailCandidate, cancellationToken) =>
            {
                await _telegramBotClient.SendMessage(
                    toChat.ChatId,
                    $"Послание из чата {_previewGenerator.GetChatDisplayString(fromChat.ChatAlias, fromChat.ChatTitle)}:",
                    cancellationToken: cancellationToken);

                await _telegramBotClient.CopyMessage(
                    toChat.ChatId,
                    fromChat.ChatId,
                    mailCandidate.MessageId,
                    cancellationToken: cancellationToken);

                await _telegramBotClient.SendMessage(
                    fromChat.ChatId,
                    $"Ваше {_previewGenerator.GetMessageLinkHtml(fromChat.ChatId, mailCandidate.MessageId, "послание")} в чат {_previewGenerator.GetChatDisplayString(toChat.ChatAlias, toChat.ChatTitle)} отправлено!",
                    ParseMode.Html,
                    cancellationToken: cancellationToken);
            },
            stoppingToken);
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        try
        {
            await OpenPendingPollsAsync(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error opening polls");
        }

        try
        {
            await CloseExpiredPollsAsync(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error closing polls");
        }

        try
        {
            await SendPendingMailsAsync(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending pending mails");
        }
    }
}
