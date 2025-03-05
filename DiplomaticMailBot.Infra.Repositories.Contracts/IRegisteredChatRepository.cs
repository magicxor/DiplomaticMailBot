using DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;
using DiplomaticMailBot.Infra.ServiceModels.SlotTemplate;
using LanguageExt;
using LanguageExt.Common;

namespace DiplomaticMailBot.Infra.Repositories.Contracts;

public interface IRegisteredChatRepository
{
    Task<IReadOnlyCollection<RegisteredChatSm>> ListRegisteredChatsAsync(CancellationToken cancellationToken = default);
    Task<SlotTemplateSm?> GetChatSlotTemplateByTelegramChatIdAsync(long telegramChatId, CancellationToken cancellationToken = default);
    Task<Either<RegisteredChatCreateOrUpdateResultSm, Error>> CreateOrUpdateAsync(RegisteredChatCreateOrUpdateRequestSm registeredChatCreateOrUpdateRequestSm, CancellationToken cancellationToken = default);
    Task<Either<bool, Error>> DeleteAsync(long chatId, CancellationToken cancellationToken = default);
    Task<Either<bool, Error>> DeleteAsync(long chatId, string chatAlias, bool checkAlias = true, CancellationToken cancellationToken = default);
}
