namespace Windows10CE.InlineIL;

public sealed class TypeRef
{
    private TypeRef() { }

    public static implicit operator TypeRef(Type t) => throw new NotSupportedException();
}