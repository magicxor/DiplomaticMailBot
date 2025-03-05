using Telegram.Bot.Types.Enums;

namespace DiplomaticMailBot.Tests.Unit.Mocks;

public class MockClientOptions
{
    public Exception? ExceptionToThrow { get; set; }
    public CancellationToken GlobalCancelToken { get; set; }
    public ChatMemberStatus ChatMemberStatus { get; set; }
}
