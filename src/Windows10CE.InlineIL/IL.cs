namespace Windows10CE.InlineIL;

public static class IL
{
    public static T Return<T>() => throw new InvalidOperationException();

    public static InlineLabel DefineLabel() => throw new InvalidOperationException();

    public static void MarkLabel(InlineLabel label) => throw new InvalidOperationException();
    public static InlineLabel DefineAndMarkLabel() => throw new InvalidOperationException();
}