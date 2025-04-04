﻿using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.Telegram.Implementations.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Services.CommandHandlers;

public sealed class WithdrawMessageHandler
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IMessageCandidateRepository _messageCandidateRepository;

    public WithdrawMessageHandler(
        ITelegramBotClient telegramBotClient,
        IMessageCandidateRepository messageCandidateRepository)
    {
        _telegramBotClient = telegramBotClient;
        _messageCandidateRepository = messageCandidateRepository;
    }

    public async Task HandleWithdrawMessageAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(userCommand);

        var replyToMessage = userCommand.ReplyToMessage;
        var commandSenderId = userCommand.From?.Id ?? 0;

        if (replyToMessage is null)
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ответьте на сообщение командой {BotCommands.WithdrawMessage}, чтобы снять его с голосования", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
        else
        {
            var withdrawResult = await _messageCandidateRepository.WithdrawAsync(userCommand.Chat.Id, replyToMessage.MessageId, commandSenderId, cancellationToken);

            await withdrawResult.MatchAsync(
                async err =>
                {
                    return err.Code switch
                    {
                        (int)EventCode.MessageCandidateNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Я не нашёл сообщения, которое можно снять с голосования. Чтобы я мог отозвать сообщение: \n1) Сообщение должно быть вынесено на голосование \n2) Голосование ещё не должно быть начато \n3) Вы должны быть автором сообщения", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось снять сообщение с голосования: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                    };
                },
                async _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Сообщение снято с голосования", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)
            );
        }
    }
}
