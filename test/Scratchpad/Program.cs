using Windows10CE.InlineIL;

var dict = new Dictionary<string, int>
{
    ["abc"] = 5,
};
nint ptr = 0;

ILEmit.Ldloc(ref dict);
ILEmit.Ldvirtftn(
    MethodRef.Create(
        typeof(Dictionary<string, int>),
        "FindValue",
        TypeRef.GenericTypeParam(1).MakeByRefType(),
        CallingConventionAttributes.HasThis
    )
    .WithParameter(TypeRef.GenericTypeParam(0))
);
ILEmit.Stloc(ref ptr);
ILEmit.Ldloc(ref dict);
ILEmit.Ldstr("abc");
ILEmit.Ldloc(ref ptr);
ILEmit.Calli(
    MethodSig.Create(
        typeof(int).MakeByRefType(),
        CallingConventionAttributes.HasThis | CallingConventionAttributes.ExplicitThis
    )
    .WithParameter(typeof(Dictionary<string, int>))
    .WithParameter(typeof(string))
);
ILEmit.Dup();
ILEmit.Ldind_I4();
ILEmit.Ldc_I4(10);
ILEmit.Add();
ILEmit.Stind_I4();

Console.WriteLine(dict["abc"]);
