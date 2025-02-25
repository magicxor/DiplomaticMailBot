using DiplomaticMailBot.Common.Enums;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Common.Extensions;

public static class ErrorCodeExtensions
{
    public static int ToInt(this EventCode eventCode)
    {
        return (int)eventCode;
    }

    public static EventId ToEventId(this EventCode eventCode)
    {
        return new EventId(eventCode.ToInt());
    }
}
