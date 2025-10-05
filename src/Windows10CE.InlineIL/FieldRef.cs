namespace Windows10CE.InlineIL;

public sealed class FieldRef
{
    private FieldRef() { }

    public static FieldRef Create(TypeRef owningType, string name, TypeRef fieldType, CallingConventionAttributes attributes) => throw new NotSupportedException();
}
