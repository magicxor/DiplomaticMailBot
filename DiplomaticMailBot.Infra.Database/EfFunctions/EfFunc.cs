using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DiplomaticMailBot.Infra.Database.EfFunctions;

public static class EfFunc
{
    // SQL: to_char(your_time_column, 'HH24:MI:SS')
    [return: NotNullIfNotNull(nameof(dateOnly))]
    public static string? DateToChar(DateOnly? dateOnly, string format)
        => dateOnly?.ToString(format, CultureInfo.InvariantCulture) ?? null;

    // SQL: to_char(your_time_column, 'HH24:MI:SS')
    [return: NotNullIfNotNull(nameof(timeOnly))]
    public static string? TimeToChar(TimeOnly? timeOnly, string format)
        => timeOnly?.ToString(format, CultureInfo.InvariantCulture) ?? null;
}
