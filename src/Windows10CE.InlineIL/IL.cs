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

    public static T Return<T>()
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
        => throw new InvalidOperationException();
    public static ref T ReturnRef<T>()
#if NET9_0_OR_GREATER
        where T : allows ref struct
#endif
        => throw new InvalidOperationException();
    
    public static unsafe void* ReturnPointer() => throw new InvalidOperationException();
    
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
    
    public static void Ldarg<T>(scoped ref readonly T arg)
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
    
    public static void Ldfld(FieldRef field) { }

    public static void Ldstr(string s) { }
    
    public static void Ldind_I4() { }

    public static void Ldvirtftn(MethodRef method) { }

    public static void Stind_I4() { }

    public static void Add() { }

    public static void Dup() { }

    public static void Ret() { }
    
    public static void Conv_I() { }
    
    public static void Conv_U() { }
    
    public static void Ldtoken(TypeRef tr) { }
    
    public static void Newobj(MethodRef ctor) { }
    
    public static void Ldftn(MethodRef method) { }
    
    public static void Ldnull() { }
    
    public static void Ldsfld(FieldRef field) { }
    
    public static void Box(TypeRef tr) { }
}
