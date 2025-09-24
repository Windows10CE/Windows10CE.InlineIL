namespace Windows10CE.InlineIL;

public static class IL
{
    public static void Push<T>(
#if NET9_0_OR_GREATER
        scoped
#endif
        T t)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }

    public static void Push<T>(scoped ref T t)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }

    public static unsafe void Push(void* ptr) { }

    public static T Return<T>() => throw new InvalidOperationException();

    public static InlineLabel DefineLabel() => throw new InvalidOperationException();

    public static void MarkLabel(InlineLabel label) => throw new InvalidOperationException();
    public static InlineLabel DefineAndMarkLabel() => throw new InvalidOperationException();
}