using System.Reflection;
using System.Runtime.CompilerServices;
using Windows10CE.InlineIL.Processor;
using Windows10CE.InlineIL;

AssemblyProcessor.Process(Assembly.GetExecutingAssembly().Location, [], "newasm.dll");

try
{
    Assembly.LoadFile(Path.GetFullPath("newasm.dll")).GetTypes()
        .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
        .Single(m => m.Name.Contains("ThrowAny")).MakeGenericMethod(typeof(string))
        .CreateDelegate<Action<string>>()("test");
}
catch (Exception e)
{
    Console.WriteLine($"lmao: {e}");
}
catch
{
    Console.WriteLine("lol");
}

static void ThrowAny<T>(T t)
{
    ILEmit.Ldarg(nameof(t));
    ILEmit.Throw();
}
