using System;

namespace sql;

/// <summary>Describes one unary, binary, or caller-defined symbolic operator and its precedence.</summary>
public class SqlOperatorPolicy
{
    /// <summary>The operator text.</summary>
    public string Text;
    /// <summary>The token kind produced for the operator.</summary>
    public SqlTokenKind TokenKind;
    /// <summary>The unary operator kind, or <see cref="SqlUnaryOperatorKind.None"/> for a non-unary operator.</summary>
    public SqlUnaryOperatorKind UnaryOperator;
    /// <summary>The binary operator kind, or <see cref="SqlBinaryOperatorKind.None"/> for a custom operator.</summary>
    public SqlBinaryOperatorKind BinaryOperator;
    /// <summary>The positive Pratt precedence assigned to the operator.</summary>
    public int Precedence;
    /// <summary>Indicates whether a binary operator is right associative.</summary>
    public bool IsRightAssociative;

    /// <summary>Initializes an operator policy.</summary>
    /// <param name="text">The operator text.</param>
    /// <param name="tokenKind">The token kind produced for the operator.</param>
    /// <param name="unaryOperator">The unary operator kind, or none.</param>
    /// <param name="binaryOperator">The binary operator kind, or none.</param>
    /// <param name="precedence">The positive Pratt precedence.</param>
    /// <param name="isRightAssociative">Whether a binary operator is right associative.</param>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">text is empty or whitespace, or both operator kinds are populated.</exception>
    /// <exception cref="ArgumentOutOfRangeException">precedence is less than one.</exception>
    public SqlOperatorPolicy(string text, SqlTokenKind tokenKind, SqlUnaryOperatorKind unaryOperator, SqlBinaryOperatorKind binaryOperator, int precedence, bool isRightAssociative)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.Length == 0 || string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("text must not be empty or whitespace");
        }

        if (unaryOperator != SqlUnaryOperatorKind.None && binaryOperator != SqlBinaryOperatorKind.None)
        {
            throw new ArgumentException("only one operator kind can be populated");
        }

        if (precedence < 1)
        {
            throw new ArgumentOutOfRangeException("precedence");
        }

        Text = text;
        TokenKind = tokenKind;
        UnaryOperator = unaryOperator;
        BinaryOperator = binaryOperator;
        Precedence = precedence;
        IsRightAssociative = isRightAssociative;
    }
}
