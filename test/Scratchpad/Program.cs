using System.Reflection;
using Windows10CE.InlineIL.Processor;
using Windows10CE.InlineIL;

AssemblyProcessor.Process(Assembly.GetExecutingAssembly().Location, [], "newasm.dll");

int a = 5;
var b = int.Parse(Console.ReadLine()!);

ILEmit.Ldloca(ref a);
IL.Push(b);
ILEmit.Call(
    MethodRef.Create(typeof(C), "TestCall", typeof(void), CallingConventionAttributes.Default)
        .WithParameter(typeof(int).MakeByRefType())
        .WithParameter(typeof(int))
);

Console.WriteLine(a);

static class C
{
    public static void TestCall(ref int test, int value)
    {
        test += value;
    }
}
