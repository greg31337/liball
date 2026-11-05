using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Produces a readable structural dump of parsed template syntax.
/// </summary>
public class TemplateSyntaxDumper : ITemplateSyntaxDumper
{
    /// <summary>
    /// Initializes a syntax dumper that emits the library's stable diagnostic tree format.
    /// </summary>
    public TemplateSyntaxDumper()
    {
    }

    /// <summary>
    /// Produces a readable native or structural representation of supplied syntax.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string Dump(string templateText)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateParseResult parseResult = TemplateParser.ParseNative(templateText);
        return DumpDocument(parseResult.Document);
    }

    /// <summary>
    /// Produces a readable native or structural representation of supplied syntax.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string DumpDocument(TemplateDocumentSyntax document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return TemplateSyntaxPrinter.Print(document);
    }

    /// <summary>
    /// Produces a readable native or structural representation of supplied syntax.
    /// </summary>
    /// <param name="node">The syntax node to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string DumpNode(TemplateSyntaxNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return TemplateSyntaxPrinter.Print(node);
    }

    /// <summary>
    /// Produces a readable native or structural representation of supplied syntax.
    /// </summary>
    /// <param name="expression">The expression syntax to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string DumpExpression(TemplateExpressionSyntax expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        return TemplateSyntaxPrinter.PrintExpression(expression);
    }
}
