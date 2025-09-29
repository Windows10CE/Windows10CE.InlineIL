namespace Windows10CE.InlineIL;

public static class ILEmit
{
    public static void Ldc_I4(int i) { }

    public static void Call(MethodRef method) { }
    public static void Callvirt(MethodRef method) { }
    public static void Calli(MethodSig signature) { }

    public static void Br(InlineLabel label) { }
    
    public static void Ldloc<T>(scoped ref readonly T local)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }

    public static void Ldloca<T>(scoped ref readonly T local)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }

    public static void Ldarga<T>(scoped ref readonly T arg)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }
    
    public static void Stloc<T>(scoped ref readonly T local)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }

    public static void Starg<T>(scoped ref readonly T arg)
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
    { }

    public static void Throw() { }

    public static void Ldarg(string argName) { }

    public static void Ldstr(string s) { }
    
    public static void Ldind_I4() { }

    public static void Ldvirtftn(MethodRef method) { }

    public static void Stind_I4() { }

    public static void Add() { }

    public static void Dup() { }

    public static void Ret() { }
}