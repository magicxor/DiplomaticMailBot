using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Data.EfFunctions;

public static class DateTimeExpr
{
    public static readonly Expression<Func<DateOnly, TimeOnly, DateTime>> FromParts = (dateOnly, timeOnly) =>
        EF.Functions.ToTimestamp(
            EfFunc.DateToChar(dateOnly, "YYYY-MM-DD") + " " + EfFunc.TimeToChar(timeOnly, "HH24:MI:SS"),
            "YYYY-MM-DD HH24:MI:SS"
        );
}
