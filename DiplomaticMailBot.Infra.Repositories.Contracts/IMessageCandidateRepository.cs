using DiplomaticMailBot.Infra.ServiceModels.MessageCandidate;
using LanguageExt;
using LanguageExt.Common;

namespace DiplomaticMailBot.Infra.Repositories.Contracts;

public interface IMessageCandidateRepository
{
    Task<Either<bool, Error>> PutAsync(MessageCandidatePutSm sm, CancellationToken cancellationToken = default);
    Task<Either<int, Error>> WithdrawAsync(long sourceChatId, int messageToWithdrawId, long commandSenderId, CancellationToken cancellationToken = default);
}
