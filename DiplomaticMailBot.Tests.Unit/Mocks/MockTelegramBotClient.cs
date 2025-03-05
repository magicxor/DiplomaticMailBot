using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DiplomaticMailBot.Tests.Unit.Mocks;

public class MockTelegramBotClient : ITelegramBotClient
{
    public int SendMessageCallCount { get; private set; }

    public MockClientOptions Options { get; }

    public MockTelegramBotClient(MockClientOptions? options = null)
    {
        Options = options ?? new MockClientOptions();
    }

    public async Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (Options.ExceptionToThrow is not null)
        {
            throw Options.ExceptionToThrow;
        }

        Options.GlobalCancelToken.ThrowIfCancellationRequested();

        switch (request)
        {
            case SendMessageRequest sendMessageRequest:
                SendMessageCallCount++;
                return (TResponse)(object)new Message
                {
                    Chat = new Chat
                    {
                        Id = sendMessageRequest.ChatId.Identifier ?? 0,
                        Username = sendMessageRequest.ChatId.Username,
                        Title = "test",
                        Type = ChatType.Supergroup,
                    },
                    Text = sendMessageRequest.Text,
                };
            case GetChatMemberRequest getChatMemberRequest:
                if (Options.ChatMemberStatus == ChatMemberStatus.Creator)
                {
                    return (TResponse)(object)new ChatMemberOwner
                    {
                        User = new User
                        {
                            Id = getChatMemberRequest.UserId,
                            Username = "test",
                        },
                    };
                }

                if (Options.ChatMemberStatus == ChatMemberStatus.Administrator)
                {
                    return (TResponse)(object)new ChatMemberAdministrator
                    {
                        User = new User
                        {
                            Id = getChatMemberRequest.UserId,
                            Username = "test",
                        },
                    };
                }

                if (Options.ChatMemberStatus == ChatMemberStatus.Member)
                {
                    return (TResponse)(object)new ChatMemberMember
                    {
                        User = new User
                        {
                            Id = getChatMemberRequest.UserId,
                            Username = "test",
                        },
                    };
                }

                if (Options.ChatMemberStatus == ChatMemberStatus.Restricted)
                {
                    return (TResponse)(object)new ChatMemberRestricted
                    {
                        User = new User
                        {
                            Id = getChatMemberRequest.UserId,
                            Username = "test",
                        },
                    };
                }

                if (Options.ChatMemberStatus == ChatMemberStatus.Kicked)
                {
                    return (TResponse)(object)new ChatMemberBanned
                    {
                        User = new User
                        {
                            Id = getChatMemberRequest.UserId,
                            Username = "test",
                        },
                    };
                }

                if (Options.ChatMemberStatus == ChatMemberStatus.Left)
                {
                    return (TResponse)(object)new ChatMemberLeft
                    {
                        User = new User
                        {
                            Id = getChatMemberRequest.UserId,
                            Username = "test",
                        },
                    };
                }

                throw new NotSupportedException($"{Options.ChatMemberStatus} is not supported");
            default:
                return (TResponse)new object();
        }
    }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(50);
    public IExceptionParser ExceptionsParser { get; set; } = new DefaultExceptionParser();

    // NOT IMPLEMENTED:
    public bool LocalBotServer => throw new NotSupportedException();
    public long BotId => throw new NotSupportedException();
    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest = async (botClient, args, cancellationToken) => { };
    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived = async (botClient, args, cancellationToken) => { };
    public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> TestApi(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<TResponse> MakeRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<TResponse> MakeRequestAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
