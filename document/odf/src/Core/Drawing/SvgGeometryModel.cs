using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace odf;

internal class SvgGeometryCursor
{
    internal string Text = "";
    internal int Position;
}

internal class SvgGeometryNumber
{
    internal string LexicalValue = "";
    internal double Value;
    internal bool IsValid;
}

internal static class SvgGeometryModel
{
    internal static OdfSvgPointList ParsePoints(string text)
    {
        OdfSvgPointList result = new OdfSvgPointList();
        result.LexicalValue = text;
        result.OriginalLexicalValue = text;
        result.IsSpecified = text.Length > 0;
        if (!result.IsSpecified)
        {
            return result;
        }
        List<OdfSvgPoint> points = new List<OdfSvgPoint>();
        int index = 0;
        bool valid = true;
        while (index < text.Length && valid)
        {
            if (index > 0)
            {
                if (!IsGeometryWhitespace(text[index]))
                {
                    valid = false;
                    break;
                }
                while (index < text.Length
                    && IsGeometryWhitespace(text[index]))
                {
                    index = index + 1;
                }
            }
            int xStart = index;
            index = ScanInteger(text, index);
            if (index == xStart || index >= text.Length
                || text[index] != ',')
            {
                valid = false;
                break;
            }
            string xText = text.Substring(xStart, index - xStart);
            index = index + 1;
            int yStart = index;
            index = ScanInteger(text, index);
            if (index == yStart)
            {
                valid = false;
                break;
            }
            string yText = text.Substring(yStart, index - yStart);
            OdfLexicalValue x = OdfLexicalParser.ParseInteger(xText);
            OdfLexicalValue y = OdfLexicalParser.ParseInteger(yText);
            if (!x.IsValid || !y.IsValid)
            {
                valid = false;
                break;
            }
            OdfSvgPoint point = new OdfSvgPoint();
            point.X = x.IntegerValue;
            point.Y = y.IntegerValue;
            points.Add(point);
        }
        result.IsValid = valid && points.Count > 0;
        if (result.IsValid)
        {
            result.Points = points.ToArray();
            result.OriginalCanonicalValue = FormatPoints(result);
        }
        return result;
    }

    internal static string FormatPoints(OdfSvgPointList value)
    {
        if (value == null || !value.IsValid)
        {
            return value == null ? "" : value.LexicalValue;
        }
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Points.Length)
        {
            if (index > 0)
            {
                result.Append(' ');
            }
            result.Append(value.Points[index].X.ToString(
                CultureInfo.InvariantCulture));
            result.Append(',');
            result.Append(value.Points[index].Y.ToString(
                CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return result.ToString();
    }

    internal static OdfSvgViewBox ParseViewBox(string text)
    {
        OdfSvgViewBox result = new OdfSvgViewBox();
        result.LexicalValue = text;
        result.OriginalLexicalValue = text;
        result.IsSpecified = text.Length > 0;
        if (!result.IsSpecified)
        {
            return result;
        }
        OdfViewBoxValue parsed = OdfLexicalParser.ParseViewBox(text);
        result.IsValid = parsed.IsValid;
        if (parsed.IsValid)
        {
            result.Left = parsed.Left;
            result.Top = parsed.Top;
            result.Width = parsed.Width;
            result.Height = parsed.Height;
            result.OriginalCanonicalValue = FormatViewBox(result);
        }
        return result;
    }

    internal static string FormatViewBox(OdfSvgViewBox value)
    {
        if (value == null || !value.IsValid)
        {
            return value == null ? "" : value.LexicalValue;
        }
        return value.Left.ToString(CultureInfo.InvariantCulture) + " "
            + value.Top.ToString(CultureInfo.InvariantCulture) + " "
            + value.Width.ToString(CultureInfo.InvariantCulture) + " "
            + value.Height.ToString(CultureInfo.InvariantCulture);
    }

    internal static OdfSvgPathData ParsePath(string text)
    {
        OdfSvgPathData result = new OdfSvgPathData();
        result.LexicalValue = text;
        result.OriginalLexicalValue = text;
        result.IsSpecified = text.Length > 0;
        if (!result.IsSpecified)
        {
            return result;
        }
        SvgGeometryCursor cursor = new SvgGeometryCursor();
        cursor.Text = text;
        List<OdfSvgPathCommand> commands =
            new List<OdfSvgPathCommand>();
        string current = "";
        bool valid = true;
        while (valid)
        {
            SkipGeometryWhitespace(cursor);
            if (cursor.Position >= text.Length)
            {
                break;
            }
            char next = text[cursor.Position];
            bool explicitCommand = IsPathCommand(next);
            if (explicitCommand)
            {
                current = next.ToString();
                cursor.Position = cursor.Position + 1;
            }
            else if (current.Length == 0
                || current == "Z" || current == "z")
            {
                valid = false;
                break;
            }
            if (commands.Count == 0 && current != "M" && current != "m")
            {
                valid = false;
                break;
            }
            int count = PathParameterCount(current[0]);
            if (count < 0)
            {
                valid = false;
                break;
            }
            if (count == 0)
            {
                if (!explicitCommand)
                {
                    valid = false;
                    break;
                }
                OdfSvgPathCommand close = new OdfSvgPathCommand();
                close.Command = current;
                commands.Add(close);
                current = "";
                continue;
            }
            OdfSvgPathCommand segment = ReadPathSegment(cursor, current, count);
            if (segment.Command.Length == 0)
            {
                valid = false;
                break;
            }
            commands.Add(segment);
            if (current == "M")
            {
                current = "L";
            }
            else if (current == "m")
            {
                current = "l";
            }
        }
        result.IsValid = valid && commands.Count > 0;
        if (result.IsValid)
        {
            result.Commands = commands.ToArray();
            result.OriginalCanonicalValue = FormatPath(result);
        }
        return result;
    }

    internal static string FormatPath(OdfSvgPathData value)
    {
        if (value == null || !value.IsValid)
        {
            return value == null ? "" : value.LexicalValue;
        }
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Commands.Length)
        {
            OdfSvgPathCommand command = value.Commands[index];
            if (result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(command.Command);
            int parameterIndex = 0;
            while (parameterIndex < command.Parameters.Length)
            {
                result.Append(' ');
                result.Append(command.Parameters[parameterIndex].ToString(
                    "R",
                    CultureInfo.InvariantCulture));
                parameterIndex = parameterIndex + 1;
            }
            index = index + 1;
        }
        return result.ToString();
    }

    internal static OdfTransform ParseTransform(string text)
    {
        OdfTransform result = new OdfTransform();
        result.Expression = text;
        result.IsSpecified = text.Length > 0;
        if (!result.IsSpecified)
        {
            result.IsValid = true;
            return result;
        }
        SvgGeometryCursor cursor = new SvgGeometryCursor();
        cursor.Text = text;
        List<OdfTransformOperation> operations =
            new List<OdfTransformOperation>();
        bool valid = true;
        while (valid)
        {
            if (operations.Count > 0
                && cursor.Position < text.Length
                && !IsGeometryWhitespace(text[cursor.Position])
                && text[cursor.Position] != ',')
            {
                valid = false;
                break;
            }
            SkipTransformSeparators(cursor);
            if (cursor.Position >= text.Length)
            {
                break;
            }
            OdfTransformOperation operation = ReadTransformOperation(cursor);
            if (operation.Kind == OdfTransformOperationKind.Unknown)
            {
                valid = false;
                break;
            }
            operations.Add(operation);
        }
        result.IsValid = valid && operations.Count > 0;
        if (result.IsValid)
        {
            result.Operations = operations.ToArray();
            result.OriginalCanonicalExpression = FormatTransform(result);
        }
        result.OriginalExpression = text;
        return result;
    }

    internal static string FormatTransform(OdfTransform value)
    {
        if (value == null || !value.IsValid)
        {
            return value == null ? "" : value.Expression;
        }
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Operations.Length)
        {
            OdfTransformOperation operation = value.Operations[index];
            if (result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(TransformName(operation));
            result.Append('(');
            int argumentIndex = 0;
            while (argumentIndex < operation.Arguments.Length)
            {
                if (argumentIndex > 0)
                {
                    result.Append(' ');
                }
                result.Append(FormatTransformArgument(
                    operation,
                    operation.Arguments[argumentIndex]));
                argumentIndex = argumentIndex + 1;
            }
            result.Append(')');
            index = index + 1;
        }
        return result.ToString();
    }

    static OdfSvgPathCommand ReadPathSegment(
        SvgGeometryCursor cursor,
        string command,
        int count)
    {
        OdfSvgPathCommand result = new OdfSvgPathCommand();
        result.Command = command;
        result.Parameters = new double[count];
        int index = 0;
        while (index < count)
        {
            if (!SkipPathNumberSeparator(cursor, index > 0))
            {
                result.Command = "";
                return result;
            }
            SvgGeometryNumber number = ReadPathNumber(cursor);
            if (!number.IsValid)
            {
                result.Command = "";
                return result;
            }
            if ((command == "A" || command == "a")
                && (index == 3 || index == 4)
                && number.Value != 0 && number.Value != 1)
            {
                result.Command = "";
                return result;
            }
            result.Parameters[index] = number.Value;
            index = index + 1;
        }
        return result;
    }

    static SvgGeometryNumber ReadPathNumber(SvgGeometryCursor cursor)
    {
        SvgGeometryNumber result = new SvgGeometryNumber();
        int start = cursor.Position;
        string text = cursor.Text;
        if (cursor.Position < text.Length
            && (text[cursor.Position] == '+'
                || text[cursor.Position] == '-'))
        {
            cursor.Position = cursor.Position + 1;
        }
        bool digits = false;
        while (cursor.Position < text.Length
            && IsDigit(text[cursor.Position]))
        {
            digits = true;
            cursor.Position = cursor.Position + 1;
        }
        if (cursor.Position < text.Length
            && text[cursor.Position] == '.')
        {
            cursor.Position = cursor.Position + 1;
            while (cursor.Position < text.Length
                && IsDigit(text[cursor.Position]))
            {
                digits = true;
                cursor.Position = cursor.Position + 1;
            }
        }
        if (!digits)
        {
            cursor.Position = start;
            return result;
        }
        if (cursor.Position < text.Length
            && (text[cursor.Position] == 'e'
                || text[cursor.Position] == 'E'))
        {
            int exponent = cursor.Position;
            cursor.Position = cursor.Position + 1;
            if (cursor.Position < text.Length
                && (text[cursor.Position] == '+'
                    || text[cursor.Position] == '-'))
            {
                cursor.Position = cursor.Position + 1;
            }
            int exponentStart = cursor.Position;
            while (cursor.Position < text.Length
                && IsDigit(text[cursor.Position]))
            {
                cursor.Position = cursor.Position + 1;
            }
            if (cursor.Position == exponentStart)
            {
                cursor.Position = exponent;
            }
        }
        result.LexicalValue = text.Substring(
            start,
            cursor.Position - start);
        OdfLexicalValue parsed = OdfLexicalParser.ParseFloat(
            result.LexicalValue);
        result.IsValid = parsed.IsValid
            && !double.IsInfinity(parsed.FloatValue)
            && !double.IsNaN(parsed.FloatValue);
        result.Value = parsed.FloatValue;
        return result;
    }

    static OdfTransformOperation ReadTransformOperation(
        SvgGeometryCursor cursor)
    {
        OdfTransformOperation result = new OdfTransformOperation();
        int nameStart = cursor.Position;
        while (cursor.Position < cursor.Text.Length
            && IsTransformNameCharacter(cursor.Text[cursor.Position]))
        {
            cursor.Position = cursor.Position + 1;
        }
        result.Name = cursor.Text.Substring(
            nameStart,
            cursor.Position - nameStart);
        result.Kind = TransformKind(result.Name);
        SkipGeometryWhitespace(cursor);
        if (result.Kind == OdfTransformOperationKind.Unknown
            || cursor.Position >= cursor.Text.Length
            || cursor.Text[cursor.Position] != '(')
        {
            result.Kind = OdfTransformOperationKind.Unknown;
            return result;
        }
        cursor.Position = cursor.Position + 1;
        List<string> arguments = new List<string>();
        bool closed = false;
        while (cursor.Position < cursor.Text.Length)
        {
            SkipGeometryWhitespace(cursor);
            if (cursor.Position < cursor.Text.Length
                && cursor.Text[cursor.Position] == ')')
            {
                cursor.Position = cursor.Position + 1;
                closed = true;
                break;
            }
            if (arguments.Count > 0
                && cursor.Position < cursor.Text.Length
                && cursor.Text[cursor.Position] == ',')
            {
                cursor.Position = cursor.Position + 1;
                SkipGeometryWhitespace(cursor);
            }
            int start = cursor.Position;
            while (cursor.Position < cursor.Text.Length
                && !IsGeometryWhitespace(cursor.Text[cursor.Position])
                && cursor.Text[cursor.Position] != ','
                && cursor.Text[cursor.Position] != ')')
            {
                cursor.Position = cursor.Position + 1;
            }
            if (cursor.Position == start)
            {
                result.Kind = OdfTransformOperationKind.Unknown;
                return result;
            }
            arguments.Add(cursor.Text.Substring(
                start,
                cursor.Position - start));
        }
        result.Arguments = arguments.ToArray();
        if (!closed || !TransformArgumentsValid(result))
        {
            result.Kind = OdfTransformOperationKind.Unknown;
        }
        return result;
    }

    static bool TransformArgumentsValid(OdfTransformOperation operation)
    {
        int count = operation.Arguments.Length;
        if (operation.Kind == OdfTransformOperationKind.Matrix && count != 6)
        {
            return false;
        }
        if ((operation.Kind == OdfTransformOperationKind.Translate
                || operation.Kind == OdfTransformOperationKind.Scale)
            && count != 1 && count != 2)
        {
            return false;
        }
        if ((operation.Kind == OdfTransformOperationKind.Rotate
                || operation.Kind == OdfTransformOperationKind.SkewX
                || operation.Kind == OdfTransformOperationKind.SkewY)
            && count != 1)
        {
            return false;
        }
        int index = 0;
        while (index < count)
        {
            if (!TransformArgumentValid(operation.Kind, operation.Arguments[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return count > 0;
    }

    static bool TransformArgumentValid(
        OdfTransformOperationKind kind,
        string argument)
    {
        if (kind == OdfTransformOperationKind.Translate)
        {
            return OdfLexicalParser.ParseLength(argument).IsValid;
        }
        OdfLexicalValue value = OdfLexicalParser.ParseFloat(argument);
        return value.IsValid
            && !double.IsInfinity(value.FloatValue)
            && !double.IsNaN(value.FloatValue);
    }

    static string FormatTransformArgument(
        OdfTransformOperation operation,
        string argument)
    {
        if (operation.Kind == OdfTransformOperationKind.Translate)
        {
            return OdfLexicalFormatter.FormatMeasurement(
                OdfLexicalParser.ParseLength(argument));
        }
        return OdfLexicalFormatter.FormatFloat(
            OdfLexicalParser.ParseFloat(argument));
    }

    static string TransformName(OdfTransformOperation operation)
    {
        if (operation.Kind == OdfTransformOperationKind.Matrix) return "matrix";
        if (operation.Kind == OdfTransformOperationKind.Translate) return "translate";
        if (operation.Kind == OdfTransformOperationKind.Scale) return "scale";
        if (operation.Kind == OdfTransformOperationKind.Rotate) return "rotate";
        if (operation.Kind == OdfTransformOperationKind.SkewX) return "skewX";
        if (operation.Kind == OdfTransformOperationKind.SkewY) return "skewY";
        return operation.Name;
    }

    static OdfTransformOperationKind TransformKind(string name)
    {
        if (name == "matrix") return OdfTransformOperationKind.Matrix;
        if (name == "translate") return OdfTransformOperationKind.Translate;
        if (name == "scale") return OdfTransformOperationKind.Scale;
        if (name == "rotate") return OdfTransformOperationKind.Rotate;
        if (name == "skewX") return OdfTransformOperationKind.SkewX;
        if (name == "skewY") return OdfTransformOperationKind.SkewY;
        return OdfTransformOperationKind.Unknown;
    }

    static int PathParameterCount(char command)
    {
        char upper = global::System.Char.ToUpperInvariant(command);
        if (upper == 'M' || upper == 'L' || upper == 'T') return 2;
        if (upper == 'H' || upper == 'V') return 1;
        if (upper == 'S' || upper == 'Q') return 4;
        if (upper == 'C') return 6;
        if (upper == 'A') return 7;
        if (upper == 'Z') return 0;
        return -1;
    }

    static bool SkipPathNumberSeparator(
        SvgGeometryCursor cursor,
        bool afterNumber)
    {
        bool whitespace = false;
        while (cursor.Position < cursor.Text.Length
            && IsGeometryWhitespace(cursor.Text[cursor.Position]))
        {
            whitespace = true;
            cursor.Position = cursor.Position + 1;
        }
        if (cursor.Position < cursor.Text.Length
            && cursor.Text[cursor.Position] == ',')
        {
            cursor.Position = cursor.Position + 1;
            while (cursor.Position < cursor.Text.Length
                && IsGeometryWhitespace(cursor.Text[cursor.Position]))
            {
                cursor.Position = cursor.Position + 1;
            }
            return cursor.Position < cursor.Text.Length;
        }
        if (!afterNumber)
        {
            return cursor.Position < cursor.Text.Length;
        }
        if (cursor.Position < cursor.Text.Length
            && (cursor.Text[cursor.Position] == '+'
                || cursor.Text[cursor.Position] == '-'))
        {
            return true;
        }
        return whitespace && cursor.Position < cursor.Text.Length;
    }

    static void SkipTransformSeparators(SvgGeometryCursor cursor)
    {
        while (cursor.Position < cursor.Text.Length
            && (IsGeometryWhitespace(cursor.Text[cursor.Position])
                || cursor.Text[cursor.Position] == ','))
        {
            cursor.Position = cursor.Position + 1;
        }
    }

    static void SkipGeometryWhitespace(SvgGeometryCursor cursor)
    {
        while (cursor.Position < cursor.Text.Length
            && IsGeometryWhitespace(cursor.Text[cursor.Position]))
        {
            cursor.Position = cursor.Position + 1;
        }
    }

    static int ScanInteger(string text, int start)
    {
        int index = start;
        if (index < text.Length && text[index] == '-')
        {
            index = index + 1;
        }
        int digits = index;
        while (index < text.Length && IsDigit(text[index]))
        {
            index = index + 1;
        }
        return index == digits ? start : index;
    }

    static bool IsPathCommand(char value)
    {
        char upper = global::System.Char.ToUpperInvariant(value);
        return upper == 'M' || upper == 'L' || upper == 'H'
            || upper == 'V' || upper == 'C' || upper == 'S'
            || upper == 'Q' || upper == 'T' || upper == 'A'
            || upper == 'Z';
    }

    static bool IsTransformNameCharacter(char value)
    {
        return value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z';
    }

    static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool IsGeometryWhitespace(char value)
    {
        return value == ' ' || value == '\t'
            || value == '\r' || value == '\n';
    }
}
