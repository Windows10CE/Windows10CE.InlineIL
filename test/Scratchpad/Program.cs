using System.Reflection;
using Windows10CE.InlineIL.Processor;
using InlineIL;
using ILE = InlineIL.IL.Emit;

AssemblyProcessor.Process(Assembly.GetExecutingAssembly().Location, [], "outdir");

static int TestMethod()
{
    ILE.Ldc_R4(1f);
    ILE.Ldc_R4(1f);
    ILE.Call(new MethodRef(typeof(MathF), "Log", typeof(float), 0, typeof(float), typeof(float)));
    ILE.Conv_I4();
    return IL.Return<int>();
}
