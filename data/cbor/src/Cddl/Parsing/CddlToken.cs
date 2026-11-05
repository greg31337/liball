using System.Collections.Generic;
using System.Numerics;

namespace cbor;

internal enum CddlTriviaKind
{
    Whitespace = 0,
    Comment = 1
}

internal class CddlTrivia
{
    internal CddlTriviaKind Kind;
    internal string Text = "";
    internal CddlSourceSpan Span = new CddlSourceSpan();
}

internal class CddlToken
{
    internal CddlTokenKind Kind;
    internal CddlLiteralKind LiteralKind = CddlLiteralKind.None;
    internal string Text = "";
    internal string? StringValue;
    internal byte[]? ByteStringValue;
    internal BigInteger IntegerValue;
    internal bool HasIntegerValue;
    internal double FloatValue;
    internal bool HasFloatValue;
    internal CddlSourceSpan Span = new CddlSourceSpan();
    internal List<CddlTrivia> LeadingTrivia = new List<CddlTrivia>();
    internal string? ErrorCode;
    internal string? ErrorMessage;
}
