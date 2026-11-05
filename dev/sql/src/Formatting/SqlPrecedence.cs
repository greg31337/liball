using System;

namespace sql;

internal class SqlPrecedence
{
    internal static int GetExpressionPrecedence(SqlExpression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (expression.Kind == SqlExpressionKind.Binary)
        {
            if (expression.OperatorData != null)
            {
                return expression.OperatorData.Precedence;
            }

            return GetBinaryPrecedence(expression.BinaryOperator);
        }

        if (expression.Kind == SqlExpressionKind.JsonOperator)
        {
            if (expression.OperatorData != null)
            {
                return expression.OperatorData.Precedence;
            }

            return 50;
        }

        if (expression.Kind == SqlExpressionKind.Unary)
        {
            return GetUnaryPrecedence(expression.UnaryOperator);
        }

        if (expression.Kind == SqlExpressionKind.Collate)
        {
            return 85;
        }

        if (expression.Kind == SqlExpressionKind.Is || expression.Kind == SqlExpressionKind.Like || expression.Kind == SqlExpressionKind.Between || expression.Kind == SqlExpressionKind.In || expression.Kind == SqlExpressionKind.Distinctness)
        {
            return 40;
        }

        if (expression.Kind == SqlExpressionKind.Window)
        {
            return 100;
        }

        if (expression.Kind == SqlExpressionKind.Subscript)
        {
            return 100;
        }

        if (expression.Kind == SqlExpressionKind.Cast && expression.CastData != null && expression.CastData.SyntaxKind == SqlCastSyntaxKind.PostgreSql)
        {
            return 95;
        }

        return 110;
    }

    internal static int GetUnaryPrecedence(SqlUnaryOperatorKind operatorKind)
    {
        if (operatorKind == SqlUnaryOperatorKind.Not)
        {
            return 30;
        }

        if (operatorKind != SqlUnaryOperatorKind.None)
        {
            return 90;
        }

        return 0;
    }

    internal static int GetBinaryPrecedence(SqlBinaryOperatorKind operatorKind)
    {
        if (operatorKind == SqlBinaryOperatorKind.Concat)
        {
            return 80;
        }

        if (operatorKind == SqlBinaryOperatorKind.Multiply || operatorKind == SqlBinaryOperatorKind.Divide || operatorKind == SqlBinaryOperatorKind.Modulo)
        {
            return 70;
        }

        if (operatorKind == SqlBinaryOperatorKind.Add || operatorKind == SqlBinaryOperatorKind.Subtract)
        {
            return 60;
        }

        if (operatorKind == SqlBinaryOperatorKind.BitwiseAnd || operatorKind == SqlBinaryOperatorKind.BitwiseOr || operatorKind == SqlBinaryOperatorKind.BitwiseXor || operatorKind == SqlBinaryOperatorKind.ShiftLeft || operatorKind == SqlBinaryOperatorKind.ShiftRight)
        {
            return 50;
        }

        if (operatorKind == SqlBinaryOperatorKind.JsonExtract || operatorKind == SqlBinaryOperatorKind.JsonExtractText)
        {
            return 50;
        }

        if (operatorKind == SqlBinaryOperatorKind.Equal || operatorKind == SqlBinaryOperatorKind.NotEqual || operatorKind == SqlBinaryOperatorKind.Less || operatorKind == SqlBinaryOperatorKind.LessOrEqual || operatorKind == SqlBinaryOperatorKind.Greater || operatorKind == SqlBinaryOperatorKind.GreaterOrEqual)
        {
            return 40;
        }

        if (operatorKind == SqlBinaryOperatorKind.And)
        {
            return 20;
        }

        if (operatorKind == SqlBinaryOperatorKind.Or)
        {
            return 10;
        }

        return 0;
    }
}
