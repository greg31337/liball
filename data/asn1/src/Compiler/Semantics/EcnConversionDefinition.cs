namespace asn1;

internal class EcnConversionDefinition
{
    internal EcnReplacementDefinition[] Replacements = [];
    internal EcnContainedValueMapping? ContainedValueMapping;
    internal EcnOpenTypeMapping? OpenTypeMapping;
    internal EcnTransformPipeline[] TransformPipelines = [];
    internal bool Reversible;
    internal bool Valid;
}
