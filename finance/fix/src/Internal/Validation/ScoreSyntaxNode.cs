using System.Collections.Generic;

namespace fix;

internal class ScoreSyntaxNode
{
    internal ScoreSyntaxKind Kind;
    internal ScoreTokenKind Operator;
    internal string Text = "";
    internal int Start;
    internal int Length;
    internal bool WasParenthesized;
    internal ScoreSyntaxNode? Left;
    internal ScoreSyntaxNode? Right;
    internal ScoreSyntaxNode? Third;
    internal List<ScoreSyntaxNode> Children = [];
}
