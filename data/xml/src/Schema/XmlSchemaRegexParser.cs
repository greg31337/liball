using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaRegexCompiler
{
    internal XmlSchemaRegexCompiler()
    {
    }

    internal XmlSchemaRegexCompilationResult CompileXmlSchemaRegex(string pattern, string flags, bool xpathMode)
    {
        XmlSchemaRegexParser parser = new XmlSchemaRegexParser(pattern, flags, xpathMode);
        XmlSchemaRegexNode root = parser.ParseXmlSchemaRegex();
        if (parser.ErrorMessage != null)
        {
            return new XmlSchemaRegexCompilationResult(false, null, parser.ErrorMessage);
        }

        XmlSchemaRegexProgram program = new XmlSchemaRegexProgram(root, parser.CaptureCount, parser.DotAll, parser.MultiLine, parser.IgnoreCase, parser.HasBackReferences);
        return new XmlSchemaRegexCompilationResult(true, program, null);
    }
}

internal class XmlSchemaRegexParser
{
    internal string Pattern;
    internal int Offset;
    internal bool XPathMode;
    internal bool DotAll;
    internal bool MultiLine;
    internal bool IgnoreCase;
    internal bool FreeSpacing;
    internal bool Quote;
    internal int CaptureCount;
    internal bool HasBackReferences;
    internal List<bool> ClosedCaptures;
    internal string? ErrorMessage;
    internal XmlSchemaRegexUnicodeProperties UnicodeProperties;

    internal XmlSchemaRegexParser(string pattern, string flags, bool xpathMode)
    {
        Pattern = pattern;
        Offset = 0;
        XPathMode = xpathMode;
        DotAll = false;
        MultiLine = false;
        IgnoreCase = false;
        FreeSpacing = false;
        Quote = false;
        CaptureCount = 0;
        HasBackReferences = false;
        ClosedCaptures = new List<bool>();
        ErrorMessage = null;
        UnicodeProperties = new XmlSchemaRegexUnicodeProperties();
        ParseXmlSchemaRegexFlags(flags);
        if (FreeSpacing && !Quote)
        {
            Pattern = RemoveXmlSchemaRegexWhitespace(Pattern);
        }
    }

    internal void ParseXmlSchemaRegexFlags(string flags)
    {
        int index = 0;
        while (index < flags.Length)
        {
            char flag = flags[index];
            if (flag == 's')
            {
                DotAll = true;
            }
            else if (flag == 'm')
            {
                MultiLine = true;
            }
            else if (flag == 'i')
            {
                IgnoreCase = true;
            }
            else if (flag == 'x')
            {
                FreeSpacing = true;
            }
            else if (flag == 'q')
            {
                Quote = true;
            }
            else
            {
                SetXmlSchemaRegexError("The regular expression contains an unsupported flag.");
                return;
            }

            index = index + 1;
        }
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegex()
    {
        if (ErrorMessage != null)
        {
            return CreateXmlSchemaRegexEmptyNode();
        }

        if (Quote)
        {
            return ParseXmlSchemaQuotedRegex();
        }

        SkipXmlSchemaRegexInsignificant();
        XmlSchemaRegexNode result = ParseXmlSchemaRegexAlternation();
        SkipXmlSchemaRegexInsignificant();
        if (ErrorMessage == null && Offset != Pattern.Length)
        {
            SetXmlSchemaRegexError("The regular expression contains an unexpected closing token.");
        }

        return result;
    }

    internal XmlSchemaRegexNode ParseXmlSchemaQuotedRegex()
    {
        List<XmlSchemaRegexNode> nodes = new List<XmlSchemaRegexNode>();
        while (Offset < Pattern.Length)
        {
            int codepoint = ReadXmlSchemaRegexScalar();
            XmlSchemaRegexNode node = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.Literal);
            node.Codepoint = codepoint;
            nodes.Add(node);
        }

        return CreateXmlSchemaRegexComposite(XmlSchemaRegexNodeKind.Sequence, nodes);
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegexAlternation()
    {
        List<XmlSchemaRegexNode> branches = new List<XmlSchemaRegexNode>();
        branches.Add(ParseXmlSchemaRegexBranch());
        SkipXmlSchemaRegexInsignificant();
        while (ErrorMessage == null && Offset < Pattern.Length && Pattern[Offset] == '|')
        {
            Offset = Offset + 1;
            branches.Add(ParseXmlSchemaRegexBranch());
            SkipXmlSchemaRegexInsignificant();
        }

        return CreateXmlSchemaRegexComposite(XmlSchemaRegexNodeKind.Alternation, branches);
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegexBranch()
    {
        List<XmlSchemaRegexNode> pieces = new List<XmlSchemaRegexNode>();
        SkipXmlSchemaRegexInsignificant();
        while (ErrorMessage == null && Offset < Pattern.Length && Pattern[Offset] != ')' && Pattern[Offset] != '|')
        {
            pieces.Add(ParseXmlSchemaRegexPiece());
            SkipXmlSchemaRegexInsignificant();
        }

        return CreateXmlSchemaRegexComposite(XmlSchemaRegexNodeKind.Sequence, pieces);
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegexPiece()
    {
        XmlSchemaRegexNode atom = ParseXmlSchemaRegexAtom();
        SkipXmlSchemaRegexInsignificant();
        if (ErrorMessage != null || Offset >= Pattern.Length)
        {
            return atom;
        }

        char token = Pattern[Offset];
        int minimum;
        int maximum;
        if (token == '?')
        {
            minimum = 0;
            maximum = 1;
            Offset = Offset + 1;
        }
        else if (token == '*')
        {
            minimum = 0;
            maximum = -1;
            Offset = Offset + 1;
        }
        else if (token == '+')
        {
            minimum = 1;
            maximum = -1;
            Offset = Offset + 1;
        }
        else if (token == '{')
        {
            int[] quantity = ParseXmlSchemaRegexQuantity();
            minimum = quantity[0];
            maximum = quantity[1];
        }
        else
        {
            return atom;
        }

        XmlSchemaRegexNode result = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.Repeat);
        result.Children = [atom];
        result.Minimum = minimum;
        result.Maximum = maximum;
        SkipXmlSchemaRegexInsignificant();
        if (XPathMode && Offset < Pattern.Length && Pattern[Offset] == '?')
        {
            result.Reluctant = true;
            Offset = Offset + 1;
        }

        return result;
    }

    internal int[] ParseXmlSchemaRegexQuantity()
    {
        Offset = Offset + 1;
        SkipXmlSchemaRegexInsignificant();
        int minimum = ReadXmlSchemaRegexNumber();
        int maximum = minimum;
        SkipXmlSchemaRegexInsignificant();
        if (Offset < Pattern.Length && Pattern[Offset] == ',')
        {
            Offset = Offset + 1;
            SkipXmlSchemaRegexInsignificant();
            maximum = Offset < Pattern.Length && IsXmlSchemaRegexDigit(Pattern[Offset]) ? ReadXmlSchemaRegexNumber() : -1;
            SkipXmlSchemaRegexInsignificant();
        }

        if (Offset >= Pattern.Length || Pattern[Offset] != '}')
        {
            SetXmlSchemaRegexError("A quantified range must end with a right brace.");
        }
        else
        {
            Offset = Offset + 1;
        }

        if (maximum >= 0 && maximum < minimum)
        {
            SetXmlSchemaRegexError("A quantified range has a maximum below its minimum.");
        }

        return [minimum, maximum];
    }

    internal int ReadXmlSchemaRegexNumber()
    {
        if (Offset >= Pattern.Length || !IsXmlSchemaRegexDigit(Pattern[Offset]))
        {
            SetXmlSchemaRegexError("A quantified range requires a decimal integer.");
            return 0;
        }

        int result = 0;
        while (Offset < Pattern.Length && IsXmlSchemaRegexDigit(Pattern[Offset]))
        {
            int digit = Pattern[Offset] - '0';
            if (result > 100000 || result == 100000 && digit > 0)
            {
                SetXmlSchemaRegexError("A quantified range exceeds the implementation limit.");
                return 0;
            }

            result = result * 10 + digit;
            Offset = Offset + 1;
        }

        return result;
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegexAtom()
    {
        SkipXmlSchemaRegexInsignificant();
        if (Offset >= Pattern.Length)
        {
            SetXmlSchemaRegexError("A regular expression branch ends where an atom is required.");
            return CreateXmlSchemaRegexEmptyNode();
        }

        char token = Pattern[Offset];
        if (token == '(')
        {
            return ParseXmlSchemaRegexGroup();
        }

        if (token == '[')
        {
            XmlSchemaRegexNode characterClass = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.CharacterClass);
            characterClass.CharacterSet = ParseXmlSchemaRegexCharacterClass();
            return characterClass;
        }

        if (token == '.')
        {
            Offset = Offset + 1;
            return new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.Dot);
        }

        if (XPathMode && token == '^')
        {
            Offset = Offset + 1;
            return new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.StartAnchor);
        }

        if (XPathMode && token == '$')
        {
            Offset = Offset + 1;
            return new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.EndAnchor);
        }

        if (token == '\\')
        {
            return ParseXmlSchemaRegexEscapeNode();
        }

        if (token == '?' || token == '*' || token == '+' || token == '{' || token == '}' || token == ']')
        {
            SetXmlSchemaRegexError("A regular expression contains a quantifier or delimiter without an atom.");
            Offset = Offset + 1;
            return CreateXmlSchemaRegexEmptyNode();
        }

        XmlSchemaRegexNode literal = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.Literal);
        literal.Codepoint = ReadXmlSchemaRegexScalar();
        return literal;
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegexGroup()
    {
        Offset = Offset + 1;
        bool capturing = true;
        if (XPathMode && Offset + 1 < Pattern.Length && Pattern[Offset] == '?' && Pattern[Offset + 1] == ':')
        {
            capturing = false;
            Offset = Offset + 2;
        }

        int captureNumber = 0;
        if (capturing)
        {
            CaptureCount = CaptureCount + 1;
            captureNumber = CaptureCount;
            ClosedCaptures.Add(false);
        }

        XmlSchemaRegexNode contents = ParseXmlSchemaRegexAlternation();
        SkipXmlSchemaRegexInsignificant();
        if (Offset >= Pattern.Length || Pattern[Offset] != ')')
        {
            SetXmlSchemaRegexError("A parenthesized sub-expression is not closed.");
        }
        else
        {
            Offset = Offset + 1;
        }

        if (!capturing)
        {
            return contents;
        }

        ClosedCaptures[captureNumber - 1] = true;
        XmlSchemaRegexNode capture = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.Capture);
        capture.CaptureNumber = captureNumber;
        capture.Children = [contents];
        return capture;
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegexEscapeNode()
    {
        Offset = Offset + 1;
        if (Offset >= Pattern.Length)
        {
            SetXmlSchemaRegexError("A regular expression ends with an incomplete escape.");
            return CreateXmlSchemaRegexEmptyNode();
        }

        char escaped = Pattern[Offset];
        if (XPathMode && IsXmlSchemaRegexDigit(escaped))
        {
            return ParseXmlSchemaRegexBackReference();
        }

        XmlSchemaRegexClassAtom atom = ParseXmlSchemaRegexClassEscape();
        if (atom.Single)
        {
            XmlSchemaRegexNode literal = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.Literal);
            literal.Codepoint = atom.Codepoint;
            return literal;
        }

        XmlSchemaRegexCharacterSet set = new XmlSchemaRegexCharacterSet(false);
        if (atom.Property != null)
        {
            set.Properties.Add(atom.Property);
        }

        XmlSchemaRegexNode result = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.CharacterClass);
        result.CharacterSet = set;
        return result;
    }

    internal XmlSchemaRegexNode ParseXmlSchemaRegexBackReference()
    {
        HasBackReferences = true;
        int accepted = 0;
        int acceptedOffset = Offset;
        int candidate = 0;
        while (Offset < Pattern.Length && IsXmlSchemaRegexDigit(Pattern[Offset]))
        {
            candidate = candidate * 10 + Pattern[Offset] - '0';
            if (candidate > 0 && candidate <= CaptureCount)
            {
                accepted = candidate;
                acceptedOffset = Offset + 1;
            }
            else
            {
                break;
            }

            Offset = Offset + 1;
        }

        if (accepted == 0 || !ClosedCaptures[accepted - 1])
        {
            SetXmlSchemaRegexError("A back-reference must refer to an earlier closed capturing group.");
            return CreateXmlSchemaRegexEmptyNode();
        }

        Offset = acceptedOffset;
        XmlSchemaRegexNode result = new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.BackReference);
        result.CaptureNumber = accepted;
        return result;
    }

    internal XmlSchemaRegexCharacterSet ParseXmlSchemaRegexCharacterClass()
    {
        Offset = Offset + 1;
        bool negated = Offset < Pattern.Length && Pattern[Offset] == '^';
        if (negated)
        {
            Offset = Offset + 1;
        }

        XmlSchemaRegexCharacterSet result = new XmlSchemaRegexCharacterSet(negated);
        bool hasAtom = false;
        while (ErrorMessage == null && Offset < Pattern.Length && Pattern[Offset] != ']')
        {
            if (hasAtom && Pattern[Offset] == '-' && Offset + 1 < Pattern.Length && Pattern[Offset + 1] == '[')
            {
                Offset = Offset + 1;
                result.Subtraction = ParseXmlSchemaRegexCharacterClass();
                if (Offset >= Pattern.Length || Pattern[Offset] != ']')
                {
                    SetXmlSchemaRegexError("A character class subtraction must be the final group member.");
                }

                break;
            }

            int firstOffset = Offset;
            XmlSchemaRegexClassAtom first = ReadXmlSchemaRegexClassAtom();
            if (Pattern[firstOffset] == '-' && hasAtom && firstOffset + 1 < Pattern.Length && Pattern[firstOffset + 1] != ']' && !(Pattern[firstOffset + 1] == '-' && firstOffset + 2 < Pattern.Length && Pattern[firstOffset + 2] == '['))
            {
                SetXmlSchemaRegexError("An ambiguous hyphen must be escaped in a character class.");
            }
            hasAtom = true;
            bool beforeLiteralHyphenAndSubtraction = Offset + 2 < Pattern.Length && Pattern[Offset] == '-' && Pattern[Offset + 1] == '-' && Pattern[Offset + 2] == '[';
            if (first.Single && !beforeLiteralHyphenAndSubtraction && Offset < Pattern.Length && Pattern[Offset] == '-' && Offset + 1 < Pattern.Length && Pattern[Offset + 1] != '[' && Pattern[Offset + 1] != ']')
            {
                Offset = Offset + 1;
                XmlSchemaRegexClassAtom last = ReadXmlSchemaRegexClassAtom();
                if (!last.Single || last.Codepoint < first.Codepoint)
                {
                    SetXmlSchemaRegexError("A character range requires two ordered single characters.");
                }
                else
                {
                    AddXmlSchemaRegexRange(result, first.Codepoint, last.Codepoint);
                }
            }
            else
            {
                AddXmlSchemaRegexClassAtom(result, first);
            }
        }

        if (!hasAtom)
        {
            SetXmlSchemaRegexError("A character class cannot be empty.");
        }

        if (Offset >= Pattern.Length || Pattern[Offset] != ']')
        {
            SetXmlSchemaRegexError("A character class is not closed.");
        }
        else
        {
            Offset = Offset + 1;
        }

        return result;
    }

    internal XmlSchemaRegexClassAtom ReadXmlSchemaRegexClassAtom()
    {
        if (Offset >= Pattern.Length)
        {
            SetXmlSchemaRegexError("A character class ends unexpectedly.");
            return new XmlSchemaRegexClassAtom(true, 0, null);
        }

        if (Pattern[Offset] == '\\')
        {
            Offset = Offset + 1;
            if (Offset < Pattern.Length && IsXmlSchemaRegexDigit(Pattern[Offset]))
            {
                SetXmlSchemaRegexError("Back-references are not permitted in character classes.");
                return new XmlSchemaRegexClassAtom(true, 0, null);
            }

            return ParseXmlSchemaRegexClassEscape();
        }

        if (Pattern[Offset] == '[')
        {
            SetXmlSchemaRegexError("An unescaped left bracket is not permitted in a character class.");
            Offset = Offset + 1;
            return new XmlSchemaRegexClassAtom(true, 0, null);
        }

        int codepoint = ReadXmlSchemaRegexScalar();
        return new XmlSchemaRegexClassAtom(true, codepoint, null);
    }

    internal XmlSchemaRegexClassAtom ParseXmlSchemaRegexClassEscape()
    {
        if (Offset >= Pattern.Length)
        {
            SetXmlSchemaRegexError("A regular expression ends with an incomplete escape.");
            return new XmlSchemaRegexClassAtom(true, 0, null);
        }

        char escaped = Pattern[Offset];
        Offset = Offset + 1;
        if (escaped == 'n' || escaped == 'r' || escaped == 't')
        {
            int codepoint = escaped == 'n' ? 10 : escaped == 'r' ? 13 : 9;
            return new XmlSchemaRegexClassAtom(true, codepoint, null);
        }

        if (escaped == 's' || escaped == 'S' || escaped == 'i' || escaped == 'I' || escaped == 'c' || escaped == 'C' || escaped == 'd' || escaped == 'D' || escaped == 'w' || escaped == 'W')
        {
            string name = char.ToLowerInvariant(escaped).ToString();
            return new XmlSchemaRegexClassAtom(false, 0, new XmlSchemaRegexProperty(name, char.IsUpper(escaped)));
        }

        if (escaped == 'p' || escaped == 'P')
        {
            if (Offset >= Pattern.Length || Pattern[Offset] != '{')
            {
                SetXmlSchemaRegexError("A Unicode property escape requires a braced property name.");
                return new XmlSchemaRegexClassAtom(true, 0, null);
            }

            Offset = Offset + 1;
            int start = Offset;
            while (Offset < Pattern.Length && Pattern[Offset] != '}')
            {
                Offset = Offset + 1;
            }

            if (Offset >= Pattern.Length || Offset == start)
            {
                SetXmlSchemaRegexError("A Unicode property escape is incomplete.");
                return new XmlSchemaRegexClassAtom(true, 0, null);
            }

            string name = Pattern.Substring(start, Offset - start);
            Offset = Offset + 1;
            if (!UnicodeProperties.IsXmlSchemaRegexPropertyName(name))
            {
                SetXmlSchemaRegexError("The regular expression names an unsupported Unicode category or block.");
            }

            return new XmlSchemaRegexClassAtom(false, 0, new XmlSchemaRegexProperty(name, escaped == 'P'));
        }

        string allowed = "\\|.?*+(){}-[]^$";
        if (allowed.IndexOf(escaped) < 0)
        {
            SetXmlSchemaRegexError("The regular expression contains an invalid escape.");
        }

        return new XmlSchemaRegexClassAtom(true, escaped, null);
    }

    internal void AddXmlSchemaRegexClassAtom(XmlSchemaRegexCharacterSet set, XmlSchemaRegexClassAtom atom)
    {
        if (atom.Single)
        {
            AddXmlSchemaRegexRange(set, atom.Codepoint, atom.Codepoint);
        }
        else if (atom.Property != null)
        {
            set.Properties.Add(atom.Property);
        }
    }

    internal void AddXmlSchemaRegexRange(XmlSchemaRegexCharacterSet set, int start, int end)
    {
        set.RangeStarts.Add(start);
        set.RangeEnds.Add(end);
    }

    internal XmlSchemaRegexNode CreateXmlSchemaRegexComposite(XmlSchemaRegexNodeKind kind, List<XmlSchemaRegexNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return CreateXmlSchemaRegexEmptyNode();
        }

        if (nodes.Count == 1)
        {
            return nodes[0];
        }

        XmlSchemaRegexNode result = new XmlSchemaRegexNode(kind);
        result.Children = nodes.ToArray();
        return result;
    }

    internal XmlSchemaRegexNode CreateXmlSchemaRegexEmptyNode()
    {
        return new XmlSchemaRegexNode(XmlSchemaRegexNodeKind.Empty);
    }

    internal int ReadXmlSchemaRegexScalar()
    {
        int first = Pattern[Offset];
        if (first >= 0xD800 && first <= 0xDBFF)
        {
            if (Offset + 1 >= Pattern.Length || Pattern[Offset + 1] < 0xDC00 || Pattern[Offset + 1] > 0xDFFF)
            {
                SetXmlSchemaRegexError("The regular expression contains an unpaired surrogate.");
                Offset = Offset + 1;
                return first;
            }

            int second = Pattern[Offset + 1];
            Offset = Offset + 2;
            return 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            SetXmlSchemaRegexError("The regular expression contains an unpaired surrogate.");
        }

        Offset = Offset + 1;
        return first;
    }

    internal void SkipXmlSchemaRegexInsignificant()
    {
    }

    internal string RemoveXmlSchemaRegexWhitespace(string pattern)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(pattern.Length);
        int index = 0;
        int characterClassDepth = 0;
        bool escaped = false;
        while (index < pattern.Length)
        {
            char character = pattern[index];
            if (characterClassDepth == 0 && IsXmlSchemaRegexWhitespace(character))
            {
                index = index + 1;
                continue;
            }

            if (!escaped && character == '[')
            {
                characterClassDepth = characterClassDepth + 1;
            }
            else if (!escaped && character == ']' && characterClassDepth > 0)
            {
                characterClassDepth = characterClassDepth - 1;
            }

            result.Append(character);
            if (!escaped && character == '\\')
            {
                escaped = true;
            }
            else
            {
                escaped = false;
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal bool IsXmlSchemaRegexWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r';
    }

    internal bool IsXmlSchemaRegexDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal void SetXmlSchemaRegexError(string message)
    {
        if (ErrorMessage == null)
        {
            ErrorMessage = message + " At UTF-16 offset " + Offset.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
        }
    }
}
