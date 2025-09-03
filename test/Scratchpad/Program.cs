using System.Reflection;
using Windows10CE.InlineIL.Processor;
using Windows10CE.InlineIL;

AssemblyProcessor.Process(Assembly.GetExecutingAssembly().Location, [], "outdir");

static int TestMethod()
{
    ILEmit.Ldc_I4(5);
    ILEmit.Ldc_I4(10);
    ILEmit.Call(
        MethodRef.Create(typeof(C), "Add", typeof(int), false)
            .WithParameter(typeof(int))
            .WithParameter(typeof(int))
    );
    return IL.Return<int>();
}

class C
{
    public static int Add(int a, int b) => a + b;
}
