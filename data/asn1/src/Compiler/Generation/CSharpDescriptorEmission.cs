namespace asn1;

internal class CSharpDescriptorEmission
{
    internal string Content = "";
    internal CSharpNameMapping SchemaName = new CSharpNameMapping();
    internal CSharpNameMapping[] TypeNames = [];
    internal CSharpNameMapping[] FieldNames = [];
    internal CSharpNameMapping[] AssignmentNames = [];
}
