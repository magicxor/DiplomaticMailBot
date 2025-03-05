using DiplomaticMailBot.Infra.ServiceModels.DiplomaticRelation;
using LanguageExt;
using LanguageExt.Common;

namespace DiplomaticMailBot.Infra.Repositories.Contracts;

public interface IDiplomaticRelationRepository
{
    Task<Either<DiplomaticRelationsInfoSm, Error>> EstablishRelationsAsync(long sourceChatId, string targetChatAlias, CancellationToken cancellationToken = default);
    Task<Either<DiplomaticRelationsInfoSm, Error>> BreakOffRelationsAsync(long sourceChatId, string targetChatAlias, CancellationToken cancellationToken = default);
}
