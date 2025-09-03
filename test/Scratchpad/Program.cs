using System.Reflection;
using Windows10CE.InlineIL.Processor;
using Windows10CE.InlineIL;

AssemblyProcessor.Process(Assembly.GetExecutingAssembly().Location, [], "outdir");


static int TestMethod()
{
    var label = IL.DefineLabel();
    ILEmit.Br(label);
    ILEmit.Ldc_I4(5);
    IL.MarkLabel(label);
    ILEmit.Ldc_I4(20);
    ILEmit.Ldc_I4(10);
    label = IL.DefineLabel();
    IL.MarkLabel(label);
    ILEmit.Br(label);
    ILEmit.Call(
        MethodRef.Create(typeof(C), "Add", typeof(int), false)
            .WithParameter(typeof(int))
            .WithParameter(typeof(int))
    );
    
    return IL.Return<int>();
}

struct S
{
    public S()
    {
    }

    public void M()
    {
    }
}

class C
{
    public static int Add(int a, int b) => a + b;
}
