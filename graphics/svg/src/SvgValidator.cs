namespace svg;

internal class SvgValidationWork
{
    internal SvgValidationResult Result;
    internal SvgValidationOptions? Options;
    internal SvgTypedAttributeAccessor TypedValues;
    internal System.Collections.Generic.List<SvgNode> ReferenceNodes;

    internal SvgValidationWork(SvgValidationResult result, SvgValidationOptions? options)
    {
        Result = result;
        Options = options;
        SvgTypedAttributeOptions typed = new SvgTypedAttributeOptions();
        typed.ElementCatalog = (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Elements;
        typed.GeometryLimits = options.TypedValues.GeometryLimits;
        typed.MaxTokenCharacters = options.TypedValues.MaxTokenCharacters;
        typed.MaxListItems = options.TypedValues.MaxListItems;
        typed.MaxTransforms = options.TypedValues.MaxTransforms;
        TypedValues = new SvgTypedAttributeAccessor().Create(typed);
        ReferenceNodes = new System.Collections.Generic.List<SvgNode>();
    }
}
