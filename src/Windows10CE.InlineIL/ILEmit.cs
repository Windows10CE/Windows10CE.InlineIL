namespace Windows10CE.InlineIL;

public static class ILEmit
{
    public static void Ldc_I4(int i) { }

    public static void Call(MethodRef method) { }
    
    public static void Br(InlineLabel label) { }

    public static void Throw() { }

    public static void Ldarg(string argName) { }
    
    public static void Ret() { }
}