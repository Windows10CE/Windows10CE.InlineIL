namespace Windows10CE.InlineIL;

public sealed class InlineLabel
{
    private InlineLabel() { }

    public static void Define(out InlineLabel label) => throw null!;
    
    public static void Mark(InlineLabel label) { }

    public static void DefineAndMark(out InlineLabel label) => throw null!;

    public static InlineLabel Use(out InlineLabel label) => throw null!;
}