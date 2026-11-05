namespace asn1;

internal class EcnConstructorDefinition
{
    internal EcnConstructorKind Kind;
    internal EcnEncodingStructure? Structure;
    internal EcnEncodingField[] Fields = [];
    internal EcnControlDependency[] ControlDependencies = [];
    internal EcnConditionDefinition[] Conditions = [];
    internal bool Valid;
}
