using System.Collections.Immutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;

namespace Windows10CE.InlineIL.Processor;

internal abstract record UserData
{
    private UserData() { }

    public sealed record String(string Value) : UserData;

    public sealed record Int32(int Value) : UserData;
    public sealed record Int64(long Value) : UserData;
    public sealed record Single(float Value) : UserData;
    public sealed record Double(double Value) : UserData;

    public sealed record MetadataMember(IMetadataMember Member) : UserData;
        
    public sealed record ConstructedType(TypeSignature Signature) : UserData;

    public sealed record ConstructedMethod(
        IMemberRefParent OwningType,
        string Name,
        TypeSignature ReturnType,
        CallingConventionAttributes Attributes,
        ImmutableList<TypeSignature> ParameterTypes
    ) : UserData
    {
        public IMethodDescriptor ToMethodDescriptor()
        {
            return OwningType.CreateMemberReference(Name, new MethodSignature(Attributes, ReturnType, ParameterTypes));
        }
    }

    public sealed record LocalReference(CilLocalVariable Variable) : UserData;

    public sealed record ParameterReference(Parameter Parameter) : UserData;
}