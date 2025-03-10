﻿using DiplomaticMailBot.Common.Configuration;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Domain.Contracts;
using DiplomaticMailBot.Domain.Implementations;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.Telegram.Implementations.Extensions;
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
    private readonly IPollRepository _pollRepository;
    private readonly IMessageOutboxRepository _messageOutboxRepository;
    private readonly IPollOptionParser _pollOptionParser;
    private readonly IPreviewGenerator _previewGenerator;

    public ScheduledProcessingService(
        IOptions<BotConfiguration> options,
        ILogger<ScheduledProcessingService> logger,
        ITelegramBotClient telegramBotClient,
        IPollRepository pollRepository,
        IMessageOutboxRepository messageOutboxRepository,
        IPollOptionParser pollOptionParser,
        IPreviewGenerator previewGenerator)
    {
        _options = options;
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _pollRepository = pollRepository;
        _messageOutboxRepository = messageOutboxRepository;
        _pollOptionParser = pollOptionParser;
        _previewGenerator = previewGenerator;
    }

    public async Task SendVoteApproachingRemindersAsync(CancellationToken stoppingToken = default)
    {
        await _pollRepository.SendVoteApproachingRemindersAsync(
            sendReminderCallback: async (sourceChat, targetChat, timeLeft, cancellationToken) =>
            {
                _logger.LogInformation("Sending reminder about approaching vote start in chat {ChatId}", sourceChat.ChatId);
                await _telegramBotClient.SendMessage(
                    sourceChat.ChatId,
                    $"Желаете отправить послание в чат {_previewGenerator.GetChatDisplayString(targetChat.ChatAlias, targetChat.ChatTitle).EscapeHtml()}? Ответьте на любое сообщение командой <code>{BotCommands.PutMessage} {targetChat.ChatAlias}</code>. До начала голосования: {timeLeft.Humanize(precision: 2, culture: _options.Value.GetCultureInfo())}.".TryLeft(Defaults.NormalMessageMaxChars),
                    ParseMode.Html,
                    cancellationToken: cancellationToken);
            },
            cancellationToken: stoppingToken);
    }

    public async Task OpenPendingPollsAsync(CancellationToken stoppingToken = default)
    {
        await _pollRepository.OpenPendingPollsAsync(
            sendMessageCallback: async (sourceChat, targetChat, timeLeft, mailCandidate, cancellationToken) =>
            {
                _logger.LogInformation("Sending message instead of poll in chat {ChatId}", sourceChat.ChatId);

                var message = await _telegramBotClient.SendMessage(
                    sourceChat.ChatId,
                    $"{_previewGenerator.GetMessageLinkHtml(sourceChat.ChatId, mailCandidate.MessageId, "Послание")} в чат {_previewGenerator.GetChatDisplayString(targetChat.ChatAlias, targetChat.ChatTitle).EscapeHtml()} будет отправлено через {timeLeft.Humanize(precision: 2, culture: _options.Value.GetCultureInfo())}".TryLeft(Defaults.NormalMessageMaxChars),
                    ParseMode.Html,
                    cancellationToken: cancellationToken);

                try
                {
                    await _telegramBotClient.PinChatMessage(message.Chat.Id, message.MessageId, disableNotification: true, cancellationToken: cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error pinning message {MessageId} in chat {ChatId}", message.MessageId, message.Chat.Id);
                }
            },
            sendPollCallback: async (sourceChat, targetChat, options, cancellationToken) =>
            {
                var inputPollOptions = options
                    .Select(candidate => new InputPollOption(_previewGenerator.GetPollOptionPreview(candidate.MessageId, candidate.AuthorName, candidate.Preview, Defaults.MaxReasonableAuthorNameLength, Defaults.PollOptionMaxChars)))
                    .ToList();
                var pollQuestion = $"Какое послание отправляем в {_previewGenerator.GetChatDisplayString(targetChat.ChatAlias, targetChat.ChatTitle)}? (* Отправка без никнейма, от лица бота)".TryLeft(Defaults.PollMessageMaxChars);

                _logger.LogInformation("Opening poll in chat {ChatId} with question: {PollQuestion}", sourceChat.ChatId, pollQuestion);

                var poll = await _telegramBotClient.SendPoll(
                    sourceChat.ChatId,
                    pollQuestion,
                    inputPollOptions,
                    cancellationToken: cancellationToken);

                try
                {
                    await _telegramBotClient.PinChatMessage(poll.Chat.Id, poll.MessageId, disableNotification: true, cancellationToken: cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error pinning poll message {MessageId} in chat {ChatId}", poll.MessageId, poll.Chat.Id);
                }

                var detailedPollOptions = options
                    .Select(candidate =>
                        _previewGenerator.GetMessageLinkHtml(
                            sourceChat.ChatId,
                            candidate.MessageId,
                            _previewGenerator.GetPollOptionPreview(
                                candidate.MessageId,
                                candidate.AuthorName,
                                candidate.Preview,
                                Defaults.MaxReasonableAuthorNameLength,
                                Defaults.PollOptionMaxChars)))
                    .ToList();
                var detailedPollOptionsStr = string.Join("\n\n", detailedPollOptions);
                var detailedPollOptionsInfoMessageText = $"Ссылки на посты из голосования:\n\n{detailedPollOptionsStr}"
                    .TryLeft(Defaults.NormalMessageMaxChars)
                    .CutToLastClosingLinkTag();
                await _telegramBotClient.SendMessage(
                    sourceChat.ChatId,
                    detailedPollOptionsInfoMessageText,
                    ParseMode.Html,
                    poll.ToReplyParameters(),
                    cancellationToken: cancellationToken);

                return poll.MessageId;
            },
            cancellationToken: stoppingToken);
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

                    try
                    {
                        await _telegramBotClient.UnpinChatMessage(chatId, messageId, cancellationToken: cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error unpining poll message");
                    }

                    return _pollOptionParser.GetMessageId(chosenOption?.Text ?? string.Empty);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error closing poll {MessageId} in chat {ChatId}", messageId, chatId);
                    return new DomainError(EventCode.ErrorClosingPoll.ToInt(), e.Message, true, false);
                }
            },
            stoppingToken);
    }

    public async Task SendPendingMailsAsync(CancellationToken stoppingToken = default)
    {
        await _messageOutboxRepository.SendPendingMailsAsync(
            async (sourceChat, targetChat, mailCandidate, cancellationToken) =>
            {
                await _telegramBotClient.SendMessage(
                    targetChat.ChatId,
                    $"Послание из чата {_previewGenerator.GetChatDisplayString(sourceChat.ChatAlias, sourceChat.ChatTitle)}:",
                    cancellationToken: cancellationToken);

                var copiedMessageId = await _telegramBotClient.CopyMessage(
                    targetChat.ChatId,
                    sourceChat.ChatId,
                    mailCandidate.MessageId,
                    cancellationToken: cancellationToken);

                try
                {
                    await _telegramBotClient.PinChatMessage(targetChat.ChatId, copiedMessageId, disableNotification: true, cancellationToken: cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error pinning message {MessageId} in chat {ChatId}", copiedMessageId.Id, targetChat.ChatId);
                }

                await _telegramBotClient.SendMessage(
                    sourceChat.ChatId,
                    $"Ваше {_previewGenerator.GetMessageLinkHtml(sourceChat.ChatId, mailCandidate.MessageId, "послание")} в чат {_previewGenerator.GetChatDisplayString(targetChat.ChatAlias, targetChat.ChatTitle).EscapeHtml()} отправлено!",
                    ParseMode.Html,
                    cancellationToken: cancellationToken);
            },
            stoppingToken);
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        try
        {
            await SendVoteApproachingRemindersAsync(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sending vote approaching reminders");
        }

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
