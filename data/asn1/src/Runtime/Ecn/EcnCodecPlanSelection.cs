namespace asn1;

internal class EcnCodecPlanSelection
{
    internal string Name = "";
    internal EcnExecutablePlan Plan = new EcnExecutablePlan();
    internal EcnClassCategory Category;
    internal EcnEncodingProperty[] Properties = [];
    internal AsnTypeDescriptor? Type;
}
