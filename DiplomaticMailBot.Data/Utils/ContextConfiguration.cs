using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace DiplomaticMailBot.Data.Utils;

public static class ContextConfiguration
{
    public static readonly Action<NpgsqlDbContextOptionsBuilder> NpgsqlOptionsAction = sql
        => sql
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
}
