namespace Windows10CE.InlineIL;

public sealed class MethodSig
{
    private MethodSig() { }

    public static MethodSig Create(TypeRef returnType, CallingConventionAttributes attributes) => throw new NotSupportedException();
    
    public MethodSig WithParameter(TypeRef type) => throw new NotSupportedException();
}
