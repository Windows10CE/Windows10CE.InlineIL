using System.Collections.Immutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace Windows10CE.InlineIL.Processor;

internal abstract record UserData
{
    public sealed record String(string Value) : UserData;

    public sealed record Number(ulong Value) : UserData;

    public sealed record MetadataMember(IMetadataMember Member) : UserData;
        
    public sealed record ConstructedType(TypeSignature Signature) : UserData;

    public sealed record ConstructedMethod(
        IMemberRefParent OwningType,
        string Name,
        TypeSignature ReturnType,
        CallingConventionAttributes Attributes,
        ImmutableList<TypeSignature> ParameterTypes
    ) : UserData;
}