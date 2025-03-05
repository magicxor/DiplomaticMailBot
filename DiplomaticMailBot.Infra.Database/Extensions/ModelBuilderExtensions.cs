using System.Data;
using System.Reflection;
using DiplomaticMailBot.Infra.Database.EfFunctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace DiplomaticMailBot.Infra.Database.Extensions;

public static class ModelBuilderExtensions
{
    private static readonly MethodInfo DateToCharInfo = typeof(EfFunc).GetMethod(nameof(EfFunc.DateToChar), [typeof(DateOnly?), typeof(string)])
                                                            ?? throw new MissingMethodException(nameof(EfFunc), nameof(EfFunc.DateToChar));

    private static readonly MethodInfo TimeToCharInfo = typeof(EfFunc).GetMethod(nameof(EfFunc.TimeToChar), [typeof(TimeOnly?), typeof(string)])
                                                            ?? throw new MissingMethodException(nameof(EfFunc), nameof(EfFunc.TimeToChar));

    private static readonly RelationalTypeMapping StringMapping = new StringTypeMapping("text", DbType.String, true);

    public static void AddEfFunctions(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(DateToCharInfo)
            .HasTranslation(args => new SqlFunctionExpression("to_char", args, nullable: true, argumentsPropagateNullability: [true, true], DateToCharInfo.ReturnType, StringMapping));

        modelBuilder.HasDbFunction(TimeToCharInfo)
            .HasTranslation(args => new SqlFunctionExpression("to_char", args, nullable: true, argumentsPropagateNullability: [true, true], TimeToCharInfo.ReturnType, StringMapping));
    }
}
