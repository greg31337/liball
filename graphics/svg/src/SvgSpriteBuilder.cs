namespace svg;

internal class SvgSpriteBuilderInput
{
    internal SvgDocument Document;
    internal string RequestedName;
    internal string FragmentIdentifier;
    internal SvgSpriteAccessibilityOptions? Accessibility;

    internal SvgSpriteBuilderInput(SvgDocument document, string requestedName, string fragmentIdentifier, SvgSpriteAccessibilityOptions? accessibility)
    {
        Document = document;
        RequestedName = requestedName;
        FragmentIdentifier = fragmentIdentifier;
        Accessibility = accessibility;
    }
}
