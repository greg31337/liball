using System;
using System.Text;

namespace html;
/// <summary>Identifies the element whose parsing rules govern an HTML fragment.</summary>
public class HtmlFragmentContext : IHtmlFragmentContext
{
    internal HtmlNode element;
    internal HtmlFragmentContext(HtmlNode element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (element.GetKindStorage() != HtmlNodeKind.Element && (element.GetKindStorage() != HtmlNodeKind.Fragment || element.GetTemplateHostStorage() == null))
        {
            throw new ArgumentException("A fragment context must be an element or template content.", "element");
        }

        this.element = element;
    }

    /// <summary>Creates a fragment parsing context from an element or template-content node.</summary>
    /// <param name = "element">The element whose parsing rules govern the fragment.</param>
    /// <returns>A fragment context that applies the supplied element's tokenizer and tree-construction rules.</returns>
    internal static HtmlFragmentContext Create(HtmlNode element)
    {
        return new HtmlFragmentContext(element);
    }

    /// <summary>Gets the element whose parsing state and namespace govern fragment construction.</summary>
    /// <returns>The element supplied by the caller, or a template-content fragment whose host supplies the context.</returns>
    public HtmlNode GetElement()
    {
        return this.element;
    }
}
