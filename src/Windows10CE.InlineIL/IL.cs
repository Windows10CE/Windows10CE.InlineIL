namespace Windows10CE.InlineIL;

public static class IL
{
    public static T Return<T>() => throw new InvalidOperationException("");
}