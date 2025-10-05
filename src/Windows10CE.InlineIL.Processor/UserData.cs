using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

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
        ImmutableList<TypeSignature> ParameterTypes,
        ImmutableList<TypeSignature> GenericArguments
    ) : UserData
    {
        public IMethodDescriptor ToMethodDescriptor()
        {
            var method = OwningType.CreateMemberReference(Name, new MethodSignature(Attributes, ReturnType, ParameterTypes)
            {
                GenericParameterCount = GenericArguments.Count,
            });
            return GenericArguments is [] ? method : method.MakeGenericInstanceMethod(GenericArguments.ToArray());
        }
    }

    public sealed record ConstructedMethodSignature(
        TypeSignature ReturnType,
        CallingConventionAttributes Attributes,
        ImmutableList<TypeSignature> ParameterTypes
    ) : UserData
    {
        public StandAloneSignature ToSignature() => new MethodSignature(Attributes, ReturnType, ParameterTypes).MakeStandAloneSignature();
    }

    public sealed record ConstructedField(IMemberRefParent OwningType, string Name, TypeSignature FieldType, CallingConventionAttributes Attributes) : UserData
    {
        public IFieldDescriptor ToFieldDescriptor() => OwningType.CreateMemberReference(Name, new FieldSignature(Attributes, FieldType));
    }

    public sealed record LocalReference(CilLocalVariable Variable) : UserData;

    public sealed record ParameterReference(Parameter Parameter) : UserData;

    public sealed record Label(CilLocalVariable LabelVar) : UserData, ICilLabel
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
}