namespace asn1;

internal class CSharpModelEmission
{
    internal string Content = "";
    internal CSharpNameMapping TypeName = new CSharpNameMapping();
    internal CSharpNameMapping? ChoiceName;
    internal CSharpNameMapping[] FieldNames = [];
    internal CSharpNameMapping[] PresenceNames = [];
}

internal class CSharpTypeReference
{
    internal string TypeName = "AsnRuntimeValue";
    internal string Initializer = "new AsnRuntimeValue()";
}
