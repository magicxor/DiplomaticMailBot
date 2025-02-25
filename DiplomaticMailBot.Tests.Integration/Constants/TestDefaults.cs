namespace DiplomaticMailBot.Tests.Integration.Constants;

public static class TestDefaults
{
    public const string DbPassword = "mdkoerim4958jsdihuq";
    public const string DbName = "DiplomaticMailTestDb";
    public const int DbPort = 5432;
    public const int TestTimeout = 20000;
    public static readonly TimeSpan DbStartTimeout = TimeSpan.FromMinutes(2);
}
