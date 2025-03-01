namespace DiplomaticMailBot.Tests.Integration.Utils;

public static class MathUtils
{
    public static bool AreEqual(float a, float b, float tolerance = 0.0001f)
    {
        return MathF.Abs(a - b) < tolerance;
    }

    public static bool AreEqual(double a, double b, double tolerance = 0.0001d)
    {
        return Math.Abs(a - b) < tolerance;
    }
}
