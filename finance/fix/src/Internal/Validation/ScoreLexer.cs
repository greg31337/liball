using System.Collections.Generic;

namespace fix;

internal static class ScoreLexer
{
    internal const int MaximumExpressionLength = 1024;

    internal static ScoreLexResult Lex(string source)
    {
        ScoreLexResult result = new ScoreLexResult();
        if (source == null)
        {
            SetLexError(result, 0, "missing-expression", "The Score expression is missing.");
            return result;
        }
        if (source.Length > MaximumExpressionLength)
        {
            SetLexError(result, MaximumExpressionLength, "expression-too-long", "The Score expression exceeds the supported length.");
            return result;
        }

        int index = 0;
        while (index < source.Length)
        {
            char current = source[index];
            if (IsWhitespace(current))
            {
                index++;
                continue;
            }
            if (current == '/' && index + 1 < source.Length && source[index + 1] == '/')
            {
                index = SkipLineComment(source, index + 2);
                continue;
            }
            if (current == '/' && index + 1 < source.Length && source[index + 1] == '*')
            {
                int commentEnd = FindBlockCommentEnd(source, index + 2);
                if (commentEnd < 0)
                {
                    SetLexError(result, index, "unterminated-comment", "A Score block comment is not terminated.");
                    return result;
                }
                index = commentEnd;
                continue;
            }
            if (current == '"' || current == '\'')
            {
                int quotedEnd = ScanQuotedLiteral(source, index, result);
                if (quotedEnd < 0)
                {
                    return result;
                }
                ScoreTokenKind quotedKind = current == '"' ? ScoreTokenKind.String : ScoreTokenKind.Character;
                AddToken(result.Tokens, quotedKind, source, index, quotedEnd - index);
                index = quotedEnd;
                continue;
            }
            if (current == '#')
            {
                int hashEnd = FindHashEnd(source, index + 1);
                if (hashEnd < 0)
                {
                    SetLexError(result, index, "unterminated-temporal-literal", "A Score date, time, timestamp, or duration literal is not terminated.");
                    return result;
                }
                ScoreTokenKind temporalKind = ClassifyTemporalLiteral(source, index + 1, hashEnd);
                if (temporalKind == ScoreTokenKind.Unknown)
                {
                    SetLexError(result, index, "invalid-temporal-literal", "A hash-delimited Score temporal literal has invalid syntax.");
                    return result;
                }
                AddToken(result.Tokens, temporalKind, source, index, hashEnd - index + 1);
                index = hashEnd + 1;
                continue;
            }
            if (IsDigit(current))
            {
                int numberEnd = ScanDigits(source, index);
                ScoreTokenKind numberKind = ScoreTokenKind.UnsignedInteger;
                if (numberEnd < source.Length && source[numberEnd] == '.')
                {
                    int fractionalStart = numberEnd + 1;
                    int fractionalEnd = ScanDigits(source, fractionalStart);
                    if (fractionalEnd == fractionalStart)
                    {
                        SetLexError(result, numberEnd, "invalid-decimal-literal", "A Score decimal point must be followed by a digit.");
                        return result;
                    }
                    numberKind = ScoreTokenKind.Decimal;
                    numberEnd = fractionalEnd;
                }
                AddToken(result.Tokens, numberKind, source, index, numberEnd - index);
                index = numberEnd;
                continue;
            }
            if (IsLetter(current))
            {
                int wordEnd = ScanIdentifier(source, index);
                string word = source.Substring(index, wordEnd - index);
                AddToken(result.Tokens, ResolveWordKind(word), source, index, wordEnd - index);
                index = wordEnd;
                continue;
            }

            int operatorLength = AddOperatorToken(result.Tokens, source, index);
            if (operatorLength == 0)
            {
                SetLexError(result, index, "unexpected-character", "The Score expression contains an unexpected character.");
                return result;
            }
            index += operatorLength;
        }

        AddToken(result.Tokens, ScoreTokenKind.End, source, source.Length, 0);
        result.Success = true;
        return result;
    }

    internal static int AddOperatorToken(List<ScoreToken> tokens, string source, int index)
    {
        char current = source[index];
        if (index + 1 < source.Length)
        {
            char next = source[index + 1];
            if (current == '=' && next == '=')
            {
                AddToken(tokens, ScoreTokenKind.Equal, source, index, 2);
                return 2;
            }
            if (current == '!' && next == '=')
            {
                AddToken(tokens, ScoreTokenKind.NotEqual, source, index, 2);
                return 2;
            }
            if (current == '<' && next == '=')
            {
                AddToken(tokens, ScoreTokenKind.LessEqual, source, index, 2);
                return 2;
            }
            if (current == '>' && next == '=')
            {
                AddToken(tokens, ScoreTokenKind.GreaterEqual, source, index, 2);
                return 2;
            }
            if (current == '&' && next == '&')
            {
                AddToken(tokens, ScoreTokenKind.And, source, index, 2);
                return 2;
            }
            if (current == '|' && next == '|')
            {
                AddToken(tokens, ScoreTokenKind.Or, source, index, 2);
                return 2;
            }
        }
        if (current == '=') return AddSingleToken(tokens, ScoreTokenKind.Assign, source, index);
        if (current == '<') return AddSingleToken(tokens, ScoreTokenKind.Less, source, index);
        if (current == '>') return AddSingleToken(tokens, ScoreTokenKind.Greater, source, index);
        if (current == '+') return AddSingleToken(tokens, ScoreTokenKind.Plus, source, index);
        if (current == '-') return AddSingleToken(tokens, ScoreTokenKind.Minus, source, index);
        if (current == '*') return AddSingleToken(tokens, ScoreTokenKind.Multiply, source, index);
        if (current == '/') return AddSingleToken(tokens, ScoreTokenKind.Divide, source, index);
        if (current == '%') return AddSingleToken(tokens, ScoreTokenKind.Modulo, source, index);
        if (current == '!') return AddSingleToken(tokens, ScoreTokenKind.Not, source, index);
        if (current == '(') return AddSingleToken(tokens, ScoreTokenKind.LeftParenthesis, source, index);
        if (current == ')') return AddSingleToken(tokens, ScoreTokenKind.RightParenthesis, source, index);
        if (current == '{') return AddSingleToken(tokens, ScoreTokenKind.LeftBrace, source, index);
        if (current == '}') return AddSingleToken(tokens, ScoreTokenKind.RightBrace, source, index);
        if (current == '[') return AddSingleToken(tokens, ScoreTokenKind.LeftBracket, source, index);
        if (current == ']') return AddSingleToken(tokens, ScoreTokenKind.RightBracket, source, index);
        if (current == '$') return AddSingleToken(tokens, ScoreTokenKind.Dollar, source, index);
        if (current == '^') return AddSingleToken(tokens, ScoreTokenKind.Caret, source, index);
        if (current == '.') return AddSingleToken(tokens, ScoreTokenKind.Dot, source, index);
        if (current == ',') return AddSingleToken(tokens, ScoreTokenKind.Comma, source, index);
        return 0;
    }

    internal static int AddSingleToken(List<ScoreToken> tokens, ScoreTokenKind kind, string source, int index)
    {
        AddToken(tokens, kind, source, index, 1);
        return 1;
    }

    internal static ScoreTokenKind ResolveWordKind(string word)
    {
        if (word == "eq") return ScoreTokenKind.Equal;
        if (word == "ne") return ScoreTokenKind.NotEqual;
        if (word == "lt") return ScoreTokenKind.Less;
        if (word == "le") return ScoreTokenKind.LessEqual;
        if (word == "gt") return ScoreTokenKind.Greater;
        if (word == "ge") return ScoreTokenKind.GreaterEqual;
        if (word == "mod") return ScoreTokenKind.Modulo;
        if (word == "not") return ScoreTokenKind.Not;
        if (word == "or") return ScoreTokenKind.Or;
        if (word == "and") return ScoreTokenKind.And;
        if (word == "in") return ScoreTokenKind.In;
        if (word == "between") return ScoreTokenKind.Between;
        if (word == "exists") return ScoreTokenKind.Exists;
        return ScoreTokenKind.Identifier;
    }

    internal static int ScanQuotedLiteral(string source, int start, ScoreLexResult result)
    {
        char quote = source[start];
        int index = start + 1;
        int characterCount = 0;
        while (index < source.Length)
        {
            char current = source[index];
            if (current == quote)
            {
                if (characterCount == 0 || quote == '\'' && characterCount != 1)
                {
                    string code = quote == '\'' ? "invalid-character-literal" : "empty-string-literal";
                    SetLexError(result, start, code, "A Score quoted literal has an invalid number of characters.");
                    return -1;
                }
                return index + 1;
            }
            if (current == '\\')
            {
                if (index + 1 >= source.Length || !IsEscapeCharacter(source[index + 1]))
                {
                    SetLexError(result, index, "invalid-escape-sequence", "A Score quoted literal contains an invalid escape sequence.");
                    return -1;
                }
                index += 2;
            }
            else
            {
                index++;
            }
            characterCount++;
        }
        SetLexError(result, start, "unterminated-quoted-literal", "A Score character or string literal is not terminated.");
        return -1;
    }

    internal static bool IsEscapeCharacter(char value)
    {
        return value == 'b' || value == 't' || value == 'n' || value == 'f' || value == 'r' || value == '"' || value == '\'' || value == '\\';
    }

    internal static int SkipLineComment(string source, int index)
    {
        while (index < source.Length && source[index] != '\r' && source[index] != '\n')
        {
            index++;
        }
        return index;
    }

    internal static int FindBlockCommentEnd(string source, int index)
    {
        while (index + 1 < source.Length)
        {
            if (source[index] == '*' && source[index + 1] == '/')
            {
                return index + 2;
            }
            index++;
        }
        return -1;
    }

    internal static int FindHashEnd(string source, int index)
    {
        while (index < source.Length)
        {
            if (source[index] == '#')
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal static ScoreTokenKind ClassifyTemporalLiteral(string source, int start, int end)
    {
        if (IsTimestampLiteral(source, start, end)) return ScoreTokenKind.Timestamp;
        if (IsDateLiteral(source, start, end)) return ScoreTokenKind.Date;
        if (IsTimeLiteral(source, start, end)) return ScoreTokenKind.Time;
        if (IsDurationLiteral(source, start, end)) return ScoreTokenKind.Duration;
        return ScoreTokenKind.Unknown;
    }

    internal static bool IsTimestampLiteral(string source, int start, int end)
    {
        return end - start > 11 && source[start + 10] == 'T' && IsDateLiteral(source, start, start + 10) && IsTimeLiteral(source, start + 11, end);
    }

    internal static bool IsDateLiteral(string source, int start, int end)
    {
        if (end - start != 10 || !IsDigits(source, start, 4) || source[start + 4] != '-' || !IsDigits(source, start + 5, 2) || source[start + 7] != '-' || !IsDigits(source, start + 8, 2))
        {
            return false;
        }
        int year = ReadTemporalNumber(source, start, 4);
        int month = ReadTemporalNumber(source, start + 5, 2);
        int day = ReadTemporalNumber(source, start + 8, 2);
        return TimestampParser.IsValidDate(year, month, day);
    }

    internal static bool IsTimeLiteral(string source, int start, int end)
    {
        int index = start;
        if (index + 5 > end || !IsDigits(source, index, 2) || source[index + 2] != ':' || !IsDigits(source, index + 3, 2))
        {
            return false;
        }
        int hour = ReadTemporalNumber(source, index, 2);
        int minute = ReadTemporalNumber(source, index + 3, 2);
        if (hour > 23 || minute > 59)
        {
            return false;
        }
        index += 5;
        bool hasSeconds = false;
        if (index < end && source[index] == ':')
        {
            if (index + 3 > end || !IsDigits(source, index + 1, 2))
            {
                return false;
            }
            int second = ReadTemporalNumber(source, index + 1, 2);
            if (second > 59)
            {
                return false;
            }
            hasSeconds = true;
            index += 3;
        }
        if (index < end && source[index] == '.')
        {
            if (!hasSeconds)
            {
                return false;
            }
            int fractionStart = index + 1;
            index = ScanDigitsUntil(source, fractionStart, end);
            if (index == fractionStart)
            {
                return false;
            }
        }
        if (index < end && source[index] == 'Z')
        {
            return index + 1 == end;
        }
        if (index >= end || source[index] != '+' && source[index] != '-')
        {
            return false;
        }
        index++;
        if (index + 5 != end || !IsDigits(source, index, 2) || source[index + 2] != ':' || !IsDigits(source, index + 3, 2))
        {
            return false;
        }
        int offsetHour = ReadTemporalNumber(source, index, 2);
        int offsetMinute = ReadTemporalNumber(source, index + 3, 2);
        if (offsetHour > 14 || offsetMinute > 59 || offsetHour == 14 && offsetMinute != 0)
        {
            return false;
        }
        return true;
    }

    internal static bool IsDurationLiteral(string source, int start, int end)
    {
        if (start >= end || source[start] != 'P')
        {
            return false;
        }
        int index = start + 1;
        int dateStart = index;
        index = ConsumeDurationUnit(source, index, end, 'Y');
        index = ConsumeDurationUnit(source, index, end, 'M');
        index = ConsumeDurationUnit(source, index, end, 'W');
        index = ConsumeDurationUnit(source, index, end, 'D');
        bool hasUnit = index > dateStart;
        if (index < end && source[index] == 'T')
        {
            index++;
            int timeStart = index;
            index = ConsumeDurationUnit(source, index, end, 'H');
            index = ConsumeDurationUnit(source, index, end, 'M');
            index = ConsumeDurationUnit(source, index, end, 'S');
            if (index == timeStart)
            {
                return false;
            }
            hasUnit = true;
        }
        return hasUnit && index == end;
    }

    internal static int ReadTemporalNumber(string source, int start, int count)
    {
        int value = 0;
        for (int index = start; index < start + count; index++)
        {
            value = value * 10 + source[index] - '0';
        }
        return value;
    }

    internal static int ConsumeDurationUnit(string source, int start, int end, char unit)
    {
        int index = ScanDigitsUntil(source, start, end);
        if (index > start && index < end && source[index] == unit)
        {
            return index + 1;
        }
        return start;
    }

    internal static int ScanDigits(string source, int start)
    {
        return ScanDigitsUntil(source, start, source.Length);
    }

    internal static int ScanDigitsUntil(string source, int start, int end)
    {
        int index = start;
        while (index < end && IsDigit(source[index]))
        {
            index++;
        }
        return index;
    }

    internal static int ScanIdentifier(string source, int start)
    {
        int index = start + 1;
        while (index < source.Length && (IsLetter(source[index]) || IsDigit(source[index]) || source[index] == '_'))
        {
            index++;
        }
        return index;
    }

    internal static bool IsDigits(string source, int start, int count)
    {
        if (start < 0 || start + count > source.Length)
        {
            return false;
        }
        for (int index = start; index < start + count; index++)
        {
            if (!IsDigit(source[index]))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool IsLetter(char value)
    {
        return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z';
    }

    internal static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    internal static void AddToken(List<ScoreToken> tokens, ScoreTokenKind kind, string source, int start, int length)
    {
        ScoreToken token = new ScoreToken();
        token.Kind = kind;
        token.Start = start;
        token.Length = length;
        token.Text = length == 0 ? "" : source.Substring(start, length);
        tokens.Add(token);
    }

    internal static void SetLexError(ScoreLexResult result, int offset, string code, string description)
    {
        result.ErrorOffset = offset;
        result.ErrorCode = code;
        result.ErrorMessage = description;
    }
}
