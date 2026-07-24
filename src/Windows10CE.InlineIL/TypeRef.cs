namespace Windows10CE.InlineIL;

public sealed class TypeRef
{
    private TypeRef() { }

    public static implicit operator TypeRef(Type t) => throw new NotSupportedException();

    public static TypeRef GenericTypeParam(int index) => throw new NotSupportedException();
    public static TypeRef GenericMethodParam(int index) => throw new NotSupportedException();

    public TypeRef MakeByRefType() => throw new NotSupportedException();
    public TypeRef WithGenericArg(TypeRef arg) => throw new NotSupportedException();
}
