﻿using DiplomaticMailBot.Data.EfFunctions;

namespace DiplomaticMailBot.Tests.Unit.DbFunctions;

[TestFixture]
public class EfFuncTests
{
    [Test]
    public void DateToChar_ShouldConvertDateToString()
    {
        // Arrange
        var date = new DateOnly(2023, 5, 10);
        var format = "yyyy-MM-dd";

        // Act
        var result = EfFunc.DateToChar(date, format);

        // Assert
        Assert.That(result, Is.EqualTo("2023-05-10"));
    }

    [Test]
    public void TimeToChar_ShouldConvertTimeToString()
    {
        // Arrange
        var time = new TimeOnly(12, 34, 56);
        var format = "HH:mm:ss";

        // Act
        var result = EfFunc.TimeToChar(time, format);

        // Assert
        Assert.That(result, Is.EqualTo("12:34:56"));
    }
}
