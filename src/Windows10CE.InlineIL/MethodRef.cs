namespace Windows10CE.InlineIL;

public sealed class MethodRef
{
    private MethodRef() { }

    public static MethodRef Create(TypeRef owner, string name, TypeRef returnType, CallingConventionAttributes attributes) => throw new NotSupportedException();

    public MethodRef WithParameter(TypeRef type) => throw new NotSupportedException();
}