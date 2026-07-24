using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace Windows10CE.InlineIL.Processor;

internal closed record UserData;
internal sealed record StringData(string Value) : UserData;
internal sealed record Int32Data(int Value) : UserData;
internal sealed record Int64Data(long Value) : UserData;
internal sealed record SingleData(float Value) : UserData;
internal sealed record DoubleData(double Value) : UserData;
internal sealed record MetadataMemberData(IMetadataMember Member) : UserData;
internal sealed record ConstructedTypeData(TypeSignature Signature) : UserData;

internal sealed record ConstructedMethodData(
    IMemberRefParent OwningType,
    string Name,
    TypeSignature ReturnType,
    CallingConventionAttributes Attributes,
    ImmutableList<TypeSignature> ParameterTypes,
    ImmutableList<TypeSignature> GenericArguments
) : UserData
{
    public IMethodDescriptor ToMethodDescriptor()
    {
        var method = OwningType.CreateMemberReference(Name,
            new MethodSignature(Attributes, ReturnType, ParameterTypes)
            {
                GenericParameterCount = GenericArguments.Count,
            });
        return GenericArguments is [] ? method : method.MakeGenericInstanceMethod(GenericArguments.ToArray());
    }
}

internal sealed record ConstructedMethodSignatureData(
    TypeSignature ReturnType,
    CallingConventionAttributes Attributes,
    ImmutableList<TypeSignature> ParameterTypes
) : UserData
{
    public StandAloneSignature ToSignature() => new MethodSignature(Attributes, ReturnType, ParameterTypes).MakeStandAloneSignature();
}

internal sealed record ConstructedFieldData(IMemberRefParent OwningType, string Name, TypeSignature FieldType, CallingConventionAttributes Attributes) : UserData
{
    public IFieldDescriptor ToFieldDescriptor() => OwningType.CreateMemberReference(Name, new FieldSignature(Attributes, FieldType));
}

internal sealed record LocalReferenceData(CilLocalVariable Variable) : UserData;
internal sealed record ParameterReferenceData(Parameter Parameter) : UserData;

internal sealed record LabelData(CilLocalVariable LabelVar) : UserData, ICilLabel
{
    [DisallowNull]
    public ICilLabel? CilLabel
    {
        private get;
        set
        {
            if (field is not null)
            {
                throw new InvalidOperationException("CilLabel may only be set once");
            }

            field = value;
        }
    }

    public int Offset => CilLabel?.Offset ?? throw new InvalidOperationException("Did not fill in a label");

    public bool Equals(ICilLabel other) => CilLabel?.Equals(other) ?? false;
}

internal sealed record ConstructedAssemblyData(AssemblyReference AssemblyDescriptor) : UserData;
