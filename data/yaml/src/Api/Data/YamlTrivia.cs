using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Stores one preserved YAML comment or blank line together with its placement and source range.
/// </summary>
public class YamlTrivia
{
    /// <summary>
    /// Whether this item is a comment or a blank source line.
    /// </summary>
    public YamlTriviaKind Kind;
    /// <summary>
    /// Where this item appears relative to its owning event, document, node, or pair.
    /// </summary>
    public YamlTriviaPlacement Placement;
    /// <summary>
    /// The comment text including its number sign, or an empty string for a blank line.
    /// </summary>
    public string Text;
    /// <summary>
    /// The source range occupied by this trivia item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a trivia item with empty or default state.
    /// </summary>
    public YamlTrivia()
    {
        Kind = YamlTriviaKind.Unknown;
        Placement = YamlTriviaPlacement.Unknown;
        Text = "";
        Span = YamlSourceSpan.Empty();
    }

    internal static YamlTrivia Create(YamlTriviaKind kind, YamlTriviaPlacement placement, string? text, YamlSourceSpan? span)
    {
        YamlTrivia trivia = new YamlTrivia();
        trivia.Kind = kind;
        trivia.Placement = placement;
        trivia.Text = text == null ? "" : text;
        trivia.Span = span == null ? YamlSourceSpan.Empty() : span;
        return trivia;
    }

    internal YamlTrivia Copy()
    {
        return Create(Kind, Placement, Text, Span == null ? YamlSourceSpan.Empty() : Span.Copy());
    }

    internal static bool IsCommentPlacement(YamlTriviaPlacement placement)
    {
        return placement == YamlTriviaPlacement.Before
            || placement == YamlTriviaPlacement.Inline
            || placement == YamlTriviaPlacement.Following;
    }

    internal static bool IsBlankLinePlacement(YamlTriviaPlacement placement)
    {
        return placement == YamlTriviaPlacement.Before
            || placement == YamlTriviaPlacement.Following;
    }

    internal static void RemoveFirstText(List<string> values, string text)
    {
        if (values == null)
        {
            return;
        }

        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == text)
            {
                values.RemoveAt(index);
                return;
            }

            index = index + 1;
        }
    }
}
