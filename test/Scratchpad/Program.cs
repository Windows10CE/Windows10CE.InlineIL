using System.Runtime.CompilerServices;
using Windows10CE.InlineIL;
using static Windows10CE.InlineIL.IL;

[assembly: IgnoresAccessChecksTo("System.Private.CoreLib")]

static class Program
{
    static void Main()
    {
        Newobj(MethodRef.Create(typeof(B), ".ctor", typeof(void), CallingConventionAttributes.HasThis));
        Newobj(MethodRef.Create(typeof(A), ".ctor", typeof(void), CallingConventionAttributes.HasThis));
        Ldvirtftn(MethodRef.Create(typeof(I), "M", typeof(string), CallingConventionAttributes.HasThis));
        Calli(MethodSig.Create(typeof(string), CallingConventionAttributes.ExplicitThis | CallingConventionAttributes.HasThis).WithParameter(typeof(I)));
        Call(MethodRef.Create(typeof(Console), "WriteLine", typeof(void), default).WithParameter(typeof(string)));
    }
}

interface I
{
    public string M();
}

class A : I
{
    public string M() => "A";
}

class B : I
{
    public string M() => "B";
}
