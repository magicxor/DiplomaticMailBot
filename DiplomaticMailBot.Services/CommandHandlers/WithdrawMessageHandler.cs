using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.TelegramInterop.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Services.CommandHandlers;

public sealed class WithdrawMessageHandler
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly DiplomaticMailCandidatesRepository _diplomaticMailCandidatesRepository;

    public WithdrawMessageHandler(
        ITelegramBotClient telegramBotClient,
        DiplomaticMailCandidatesRepository diplomaticMailCandidatesRepository)
    {
        _telegramBotClient = telegramBotClient;
        _diplomaticMailCandidatesRepository = diplomaticMailCandidatesRepository;
    }

    public async Task HandleWithdrawMessageAsync(User bot, Message userCommand, CancellationToken cancellationToken = default)
    {
        var replyToMessage = userCommand.ReplyToMessage;
        var commandSenderId = userCommand.From?.Id ?? 0;

        if (replyToMessage is null)
        {
            await _telegramBotClient.SendMessage(userCommand.Chat.Id, $"Ответьте на сообщение командой {BotCommands.WithdrawMessage}, чтобы снять его с голосования", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken);
        }
        else
        {
            var withdrawResult = await _diplomaticMailCandidatesRepository.WithdrawAsync(userCommand.Chat.Id, replyToMessage.MessageId, commandSenderId, cancellationToken);

            await withdrawResult.MatchAsync(
                async err =>
                {
                    return err.Code switch
                    {
                        (int)EventCode.MailCandidateNotFound => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Я не нашёл сообщения, которое можно снять с голосования. Чтобы я мог отозвать сообщение: \n1) Сообщение должно быть вынесено на голосование \n2) Голосование ещё не должно быть начато \n3) Вы должны быть автором сообщения", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                        _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Не удалось снять сообщение с голосования: непредвиденная ошибка", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken),
                    };
                },
                async _ => await _telegramBotClient.SendMessage(userCommand.Chat.Id, "Сообщение снято с голосования", replyParameters: userCommand.ToReplyParameters(), cancellationToken: cancellationToken)
            );
        }
    }
}
