using System.Collections.Immutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace Windows10CE.InlineIL.Processor;

internal abstract record UserData
{
    public sealed record String(string Value) : UserData;

    public sealed record Int32(int Value) : UserData;
        
    public sealed record ConstructedType(TypeSignature Signature) : UserData;

    public sealed record ConstructedMethod(
        TypeReference OwningType,
        string Name,
        TypeSignature ReturnType,
        CallingConventionAttributes Attributes,
        ImmutableList<TypeSignature> ParameterTypes
    ) : UserData;
}