﻿namespace DiplomaticMailBot.Common.Enums;

public enum EventCode
{
    None = 0,
    DatabaseQuery = Defaults.StartingEventCode + 1,
    AliasIsTaken = Defaults.StartingEventCode + 2,
    ChatNotFound = Defaults.StartingEventCode + 3,
    SourceChatNotFound = Defaults.StartingEventCode + 4,
    TargetChatNotFound = Defaults.StartingEventCode + 5,
    CanNotEstablishRelationsWithSelf = Defaults.StartingEventCode + 6,
    CanNotBreakOffRelationsWithSelf = Defaults.StartingEventCode + 7,
    OutgoingRelationAlreadyExists = Defaults.StartingEventCode + 8,
    OutgoingRelationDoesNotExist = Defaults.StartingEventCode + 9,
    IncomingRelationDoesNotExist = Defaults.StartingEventCode + 10,
    MessageCandidateNotFound = Defaults.StartingEventCode + 11,
    CanNotSendMessageToSelf = Defaults.StartingEventCode + 12,
    OpeningBracketNotFound = Defaults.StartingEventCode + 13,
    ClosingBracketNotFound = Defaults.StartingEventCode + 14,
    MessageIdNotFound = Defaults.StartingEventCode + 15,
    ErrorClosingPoll = Defaults.StartingEventCode + 16,
    ChatRegistrationUpdateRateLimitExceeded = Defaults.StartingEventCode + 17,
    ChatAliasMismatch = Defaults.StartingEventCode + 18,
    MessageCandidateAlreadyExists = Defaults.StartingEventCode + 19,
    MessageCandidateLimitReached = Defaults.StartingEventCode + 20,
}
