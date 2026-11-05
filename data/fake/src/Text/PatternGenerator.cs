using System;
using System.Collections.Generic;
using System.Text;

namespace fake;

internal class PatternNode
{
    internal int Kind;
    internal string Text = string.Empty;
    internal PatternNode[] Children = new PatternNode[0];
    internal int Minimum;
    internal int Maximum;

    internal PatternNode(int kind)
    {
        Kind = kind;
        Text = "";
        Children = new PatternNode[0];
        Minimum = 0;
        Maximum = 0;
    }
}

internal class PatternParser
{
    const int EmptyKind = 0;
    const int LiteralKind = 1;
    const int AlphabetKind = 2;
    const int SequenceKind = 3;
    const int AlternationKind = 4;
    const int RepeatKind = 5;
    const string Digits = "0123456789";
    const string Word = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
    const string Space = " \t\r\n";
    const string Printable = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
    string Pattern;
    int Index;
    int MaximumRepetitions;
    int MaximumNodes;
    int NodeCount;

    internal PatternParser(string pattern, int maximumRepetitions, int maximumNodes)
    {
        Pattern = pattern;
        Index = 0;
        MaximumRepetitions = maximumRepetitions;
        MaximumNodes = maximumNodes;
        NodeCount = 0;
    }

    internal PatternNode Parse()
    {
        PatternNode result = Expression(0);
        if (Index != Pattern.Length)
        {
            Invalid("unexpected closing parenthesis", Index);
        }
        return result;
    }

    internal static int Empty()
    {
        return EmptyKind;
    }

    internal static int Literal()
    {
        return LiteralKind;
    }

    internal static int Alphabet()
    {
        return AlphabetKind;
    }

    internal static int Sequence()
    {
        return SequenceKind;
    }

    internal static int Alternation()
    {
        return AlternationKind;
    }

    internal static int Repeat()
    {
        return RepeatKind;
    }

    PatternNode Expression(int depth)
    {
        List<PatternNode> branches = new List<PatternNode>();
        branches.Add(SequenceNode(depth));
        while (Index < Pattern.Length && Pattern[Index] == '|')
        {
            Index = Index + 1;
            branches.Add(SequenceNode(depth));
        }
        if (branches.Count == 1)
        {
            return branches[0];
        }
        PatternNode result = NewNode(AlternationKind);
        result.Children = branches.ToArray();
        return result;
    }

    PatternNode SequenceNode(int depth)
    {
        List<PatternNode> nodes = new List<PatternNode>();
        bool beginning = true;
        while (Index < Pattern.Length && Pattern[Index] != '|' && Pattern[Index] != ')')
        {
            char character = Pattern[Index];
            if (character == '^')
            {
                if (!beginning)
                {
                    Invalid("the start anchor is only supported at the beginning of a branch", Index);
                }
                Index = Index + 1;
                beginning = false;
                continue;
            }
            if (character == '$')
            {
                int position = Index;
                Index = Index + 1;
                if (Index < Pattern.Length && Pattern[Index] != '|' && Pattern[Index] != ')')
                {
                    Invalid("the end anchor is only supported at the end of a branch", position);
                }
                beginning = false;
                continue;
            }
            if (character == '*' || character == '+' || character == '?' || character == '{')
            {
                Invalid("quantifier has no preceding atom", Index);
            }
            nodes.Add(RepeatedAtom(depth));
            beginning = false;
        }
        if (nodes.Count == 0)
        {
            return NewNode(EmptyKind);
        }
        if (nodes.Count == 1)
        {
            return nodes[0];
        }
        PatternNode result = NewNode(SequenceKind);
        result.Children = nodes.ToArray();
        return result;
    }

    PatternNode RepeatedAtom(int depth)
    {
        PatternNode atom = Atom(depth);
        if (Index >= Pattern.Length)
        {
            return atom;
        }
        char character = Pattern[Index];
        int minimum;
        int maximum;
        if (character == '?')
        {
            minimum = 0;
            maximum = 1;
            Index = Index + 1;
        }
        else if (character == '*')
        {
            minimum = 0;
            maximum = MaximumRepetitions;
            Index = Index + 1;
        }
        else if (character == '+')
        {
            minimum = 1;
            maximum = MaximumRepetitions;
            Index = Index + 1;
        }
        else if (character == '{')
        {
            int[] bounds = Bounds();
            minimum = bounds[0];
            maximum = bounds[1];
        }
        else
        {
            return atom;
        }
        if (Index < Pattern.Length && (Pattern[Index] == '?' || Pattern[Index] == '*' || Pattern[Index] == '+' || Pattern[Index] == '{'))
        {
            Invalid("multiple quantifiers cannot be applied to one atom", Index);
        }
        PatternNode result = NewNode(RepeatKind);
        result.Children = new PatternNode[] { atom };
        result.Minimum = minimum;
        result.Maximum = maximum;
        return result;
    }

    PatternNode Atom(int depth)
    {
        int position = Index;
        char character = Pattern[Index];
        if (character == '(')
        {
            if (depth >= 64)
            {
                Invalid("group nesting exceeds the supported depth of 64", position);
            }
            Index = Index + 1;
            if (Index < Pattern.Length && Pattern[Index] == '?')
            {
                Invalid("special group constructs are not supported", position);
            }
            PatternNode group = Expression(depth + 1);
            if (Index >= Pattern.Length || Pattern[Index] != ')')
            {
                Invalid("group is missing a closing parenthesis", position);
            }
            Index = Index + 1;
            return group;
        }
        if (character == '[')
        {
            return CharacterClass();
        }
        if (character == '.')
        {
            Index = Index + 1;
            return AlphabetNode(Printable);
        }
        if (character == '\\')
        {
            return EscapedNode();
        }
        if (character == ']' || character == '}' || character == ')')
        {
            Invalid($"unexpected metacharacter '{character}'", position);
        }
        int scalar = char.ConvertToUtf32(Pattern, Index);
        string literal = char.ConvertFromUtf32(scalar);
        Index = Index + literal.Length;
        return LiteralNode(literal);
    }

    PatternNode EscapedNode()
    {
        int position = Index;
        Index = Index + 1;
        if (Index >= Pattern.Length)
        {
            Invalid("pattern contains a trailing escape", position);
        }
        char escaped = Pattern[Index];
        Index = Index + 1;
        if (escaped == 'd')
        {
            return AlphabetNode(Digits);
        }
        if (escaped == 'w')
        {
            return AlphabetNode(Word);
        }
        if (escaped == 's')
        {
            return AlphabetNode(Space);
        }
        if (escaped == 'D')
        {
            return AlphabetNode(Complement(Digits));
        }
        if (escaped == 'W')
        {
            return AlphabetNode(Complement(Word));
        }
        if (escaped == 'S')
        {
            return AlphabetNode(Complement(Space));
        }
        if (escaped == 't')
        {
            return LiteralNode("\t");
        }
        if (escaped == 'r')
        {
            return LiteralNode("\r");
        }
        if (escaped == 'n')
        {
            return LiteralNode("\n");
        }
        if (Escapable(escaped))
        {
            return LiteralNode(escaped.ToString());
        }
        Invalid($"escape '\\{escaped}' is not supported", position);
        return NewNode(EmptyKind);
    }

    PatternNode CharacterClass()
    {
        int position = Index;
        Index = Index + 1;
        bool negate = false;
        if (Index < Pattern.Length && Pattern[Index] == '^')
        {
            negate = true;
            Index = Index + 1;
        }
        List<char> values = new List<char>();
        HashSet<char> seen = new HashSet<char>();
        while (Index < Pattern.Length && Pattern[Index] != ']')
        {
            string first = ClassUnit(position);
            if (Index < Pattern.Length && Pattern[Index] == '-' && Index + 1 < Pattern.Length && Pattern[Index + 1] != ']')
            {
                int rangePosition = Index;
                Index = Index + 1;
                string last = ClassUnit(position);
                if (first.Length != 1 || last.Length != 1)
                {
                    Invalid("character-class range endpoints must be single characters", rangePosition);
                }
                if (last[0] < first[0])
                {
                    Invalid("character-class range is descending", rangePosition);
                }
                int scalar = first[0];
                while (scalar <= last[0])
                {
                    AddClassValue(values, seen, (char)scalar, position);
                    scalar = scalar + 1;
                }
            }
            else
            {
                AddClassValues(values, seen, first, position);
            }
        }
        if (Index >= Pattern.Length)
        {
            Invalid("character class is missing a closing bracket", position);
        }
        Index = Index + 1;
        if (values.Count == 0)
        {
            Invalid("character class cannot be empty", position);
        }
        string alphabet = new string(values.ToArray());
        if (negate)
        {
            alphabet = Complement(alphabet);
        }
        if (alphabet.Length == 0)
        {
            Invalid("negated character class excludes the entire printable repertoire", position);
        }
        return AlphabetNode(alphabet);
    }

    string ClassUnit(int classPosition)
    {
        if (Index >= Pattern.Length)
        {
            Invalid("character class is missing a closing bracket", classPosition);
        }
        char character = Pattern[Index];
        if (character != '\\')
        {
            if (character > 127)
            {
                Invalid("character classes support ASCII characters only", Index);
            }
            Index = Index + 1;
            return character.ToString();
        }
        int position = Index;
        Index = Index + 1;
        if (Index >= Pattern.Length)
        {
            Invalid("character class contains a trailing escape", position);
        }
        char escaped = Pattern[Index];
        Index = Index + 1;
        if (escaped == 'd')
        {
            return Digits;
        }
        if (escaped == 'w')
        {
            return Word;
        }
        if (escaped == 's')
        {
            return Space;
        }
        if (escaped == 'D')
        {
            return Complement(Digits);
        }
        if (escaped == 'W')
        {
            return Complement(Word);
        }
        if (escaped == 'S')
        {
            return Complement(Space);
        }
        if (escaped == 't')
        {
            return "\t";
        }
        if (escaped == 'r')
        {
            return "\r";
        }
        if (escaped == 'n')
        {
            return "\n";
        }
        if (Escapable(escaped))
        {
            return escaped.ToString();
        }
        Invalid($"escape '\\{escaped}' is not supported", position);
        return "";
    }

    int[] Bounds()
    {
        int position = Index;
        Index = Index + 1;
        int minimum = Number(position);
        int maximum = minimum;
        if (Index < Pattern.Length && Pattern[Index] == ',')
        {
            Index = Index + 1;
            if (Index < Pattern.Length && Pattern[Index] == '}')
            {
                maximum = MaximumRepetitions;
            }
            else
            {
                maximum = Number(position);
            }
        }
        if (Index >= Pattern.Length || Pattern[Index] != '}')
        {
            Invalid("quantifier is missing a closing brace", position);
        }
        Index = Index + 1;
        if (minimum > maximum)
        {
            Invalid("quantifier minimum exceeds its maximum", position);
        }
        return new int[] { minimum, maximum };
    }

    int Number(int quantifierPosition)
    {
        if (Index >= Pattern.Length || Pattern[Index] < '0' || Pattern[Index] > '9')
        {
            Invalid("quantifier requires a decimal bound", quantifierPosition);
        }
        int value = 0;
        while (Index < Pattern.Length && Pattern[Index] >= '0' && Pattern[Index] <= '9')
        {
            int digit = Pattern[Index] - '0';
            if (value > (MaximumRepetitions - digit) / 10)
            {
                Invalid($"quantifier exceeds the configured repetition maximum of {MaximumRepetitions}", quantifierPosition);
            }
            value = (value * 10) + digit;
            Index = Index + 1;
        }
        return value;
    }

    void AddClassValues(List<char> values, HashSet<char> seen, string additions, int position)
    {
        int index = 0;
        while (index < additions.Length)
        {
            AddClassValue(values, seen, additions[index], position);
            index = index + 1;
        }
    }

    void AddClassValue(List<char> values, HashSet<char> seen, char value, int position)
    {
        if (seen.Add(value))
        {
            values.Add(value);
            if (values.Count > MaximumNodes)
            {
                Invalid($"character-class repertoire exceeds the configured maximum count of {MaximumNodes}", position);
            }
        }
    }

    PatternNode LiteralNode(string value)
    {
        PatternNode result = NewNode(LiteralKind);
        result.Text = value;
        return result;
    }

    PatternNode AlphabetNode(string value)
    {
        PatternNode result = NewNode(AlphabetKind);
        result.Text = value;
        return result;
    }

    PatternNode NewNode(int kind)
    {
        NodeCount = NodeCount + 1;
        if (NodeCount > MaximumNodes)
        {
            Invalid($"pattern node count exceeds the configured maximum of {MaximumNodes}", Index);
        }
        return new PatternNode(kind);
    }

    static bool Escapable(char character)
    {
        return character == '\\'
            || character == '.'
            || character == '['
            || character == ']'
            || character == '('
            || character == ')'
            || character == '{'
            || character == '}'
            || character == '?'
            || character == '*'
            || character == '+'
            || character == '|'
            || character == '^'
            || character == '$'
            || character == '-';
    }

    static string Complement(string excluded)
    {
        StringBuilder builder = new StringBuilder(Printable.Length);
        int index = 0;
        while (index < Printable.Length)
        {
            char candidate = Printable[index];
            if (excluded.IndexOf(candidate) < 0)
            {
                builder.Append(candidate);
            }
            index = index + 1;
        }
        return builder.ToString();
    }

    static void Invalid(string message, int position)
    {
        throw new ArgumentException($"Invalid pattern at index {position}: {message}.", "pattern");
    }
}

internal static class PatternGenerator
{
    internal static string Generate(FakeRandom random, string pattern, int maximumRepetitions, int maximumCount, int maximumOutputSize)
    {
        MaximumOutputLength(pattern, maximumRepetitions, maximumCount, maximumOutputSize);
        PatternParser parser = new PatternParser(pattern, maximumRepetitions, maximumCount);
        PatternNode root = parser.Parse();
        StringBuilder builder = new StringBuilder();
        Append(random, builder, root);
        return builder.ToString();
    }

    internal static int MaximumOutputLength(string pattern, int maximumRepetitions, int maximumCount, int maximumOutputSize)
    {
        UnicodeText.Validate(pattern, "pattern");
        Guard.Positive(maximumRepetitions, "maximumRepetitions");
        Guard.Count(maximumRepetitions, maximumCount, "maximumRepetitions");
        Guard.Count(pattern.Length, maximumCount, "pattern");
        PatternParser parser = new PatternParser(pattern, maximumRepetitions, maximumCount);
        PatternNode root = parser.Parse();
        long maximumLength = MaximumLength(root, maximumOutputSize);
        return Guard.OutputSize(maximumLength, maximumOutputSize, "pattern");
    }

    static long MaximumLength(PatternNode node, int maximumOutputSize)
    {
        if (node.Kind == PatternParser.Empty())
        {
            return 0L;
        }
        if (node.Kind == PatternParser.Literal())
        {
            return node.Text.Length;
        }
        if (node.Kind == PatternParser.Alphabet())
        {
            return 1L;
        }
        if (node.Kind == PatternParser.Repeat())
        {
            long childLength = MaximumLength(node.Children[0], maximumOutputSize);
            return LimitedMultiply(childLength, node.Maximum, maximumOutputSize);
        }
        if (node.Kind == PatternParser.Sequence())
        {
            long total = 0L;
            int index = 0;
            while (index < node.Children.Length)
            {
                total = LimitedAdd(total, MaximumLength(node.Children[index], maximumOutputSize), maximumOutputSize);
                index = index + 1;
            }
            return total;
        }
        long maximum = 0L;
        int branchIndex = 0;
        while (branchIndex < node.Children.Length)
        {
            long branchLength = MaximumLength(node.Children[branchIndex], maximumOutputSize);
            if (branchLength > maximum)
            {
                maximum = branchLength;
            }
            branchIndex = branchIndex + 1;
        }
        return maximum;
    }

    static long LimitedAdd(long first, long second, int maximumOutputSize)
    {
        if (first > maximumOutputSize || second > maximumOutputSize || first + second > maximumOutputSize)
        {
            return (long)maximumOutputSize + 1L;
        }
        return first + second;
    }

    static long LimitedMultiply(long value, int count, int maximumOutputSize)
    {
        if (value == 0L || count == 0)
        {
            return 0L;
        }
        if (value > maximumOutputSize || value > maximumOutputSize / count)
        {
            return (long)maximumOutputSize + 1L;
        }
        return value * count;
    }

    static void Append(FakeRandom random, StringBuilder builder, PatternNode node)
    {
        if (node.Kind == PatternParser.Empty())
        {
            return;
        }
        if (node.Kind == PatternParser.Literal())
        {
            builder.Append(node.Text);
            return;
        }
        if (node.Kind == PatternParser.Alphabet())
        {
            if (node.Text.Length == 1)
            {
                builder.Append(node.Text[0]);
            }
            else
            {
                builder.Append(node.Text[random.IntFromMaximumExclusive(node.Text.Length)]);
            }
            return;
        }
        if (node.Kind == PatternParser.Sequence())
        {
            int sequenceIndex = 0;
            while (sequenceIndex < node.Children.Length)
            {
                Append(random, builder, node.Children[sequenceIndex]);
                sequenceIndex = sequenceIndex + 1;
            }
            return;
        }
        if (node.Kind == PatternParser.Alternation())
        {
            int branch = random.IntFromMaximumExclusive(node.Children.Length);
            Append(random, builder, node.Children[branch]);
            return;
        }
        int repetitions = node.Minimum;
        if (node.Maximum > node.Minimum)
        {
            repetitions = random.IntFromMinimumAndMaximumAndMaximumInclusive(node.Minimum, node.Maximum, true);
        }
        int index = 0;
        while (index < repetitions)
        {
            Append(random, builder, node.Children[0]);
            index = index + 1;
        }
    }
}
