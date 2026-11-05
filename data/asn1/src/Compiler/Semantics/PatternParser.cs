namespace asn1;

internal class PatternParser
{
    internal string Text;
    internal int Position;
    internal string ErrorMessage = "";
    internal int ErrorOffset;

    internal PatternParser(string text)
    {
        this.Text = text == null ? throw new System.ArgumentNullException("text") : text;
    }

    internal PatternParseResult ParsePattern()
    {
        PatternNode? root = this.ParseAlternation();
        if (root != null && this.Position != this.Text.Length)
        {
            this.SetError("Unexpected pattern character.");
            root = null;
        }

        return new PatternParseResult
        {
            Root = root,
            ErrorMessage = this.ErrorMessage,
            ErrorOffset = this.ErrorOffset,
            Success = root != null && this.ErrorMessage.Length == 0
        };
    }

    internal PatternNode? ParseAlternation()
    {
        PatternNode? first = this.ParseSequence();
        if (first == null)
        {
            return null;
        }

        System.Collections.Generic.List<PatternNode> alternatives =
            new System.Collections.Generic.List<PatternNode>();
        alternatives.Add(first);
        while (this.Position < this.Text.Length && this.Text[this.Position] == '|')
        {
            this.Position = this.Position + 1;
            if (this.Position >= this.Text.Length
                || this.Text[this.Position] == '|'
                || this.Text[this.Position] == ')')
            {
                this.SetError("A pattern alternative cannot be empty.");
                return null;
            }

            PatternNode? alternative = this.ParseSequence();
            if (alternative == null)
            {
                return null;
            }

            alternatives.Add(alternative);
        }

        return alternatives.Count == 1
            ? alternatives[0]
            : new PatternNode
            {
                Kind = PatternNodeKind.Alternative,
                Children = alternatives.ToArray()
            };
    }

    internal PatternNode? ParseSequence()
    {
        System.Collections.Generic.List<PatternNode> items =
            new System.Collections.Generic.List<PatternNode>();
        while (this.Position < this.Text.Length
            && this.Text[this.Position] != '|'
            && this.Text[this.Position] != ')')
        {
            PatternNode? item = this.ParseRepetition();
            if (item == null)
            {
                return null;
            }

            if (item.Kind == PatternNodeKind.StartAnchor && items.Count != 0)
            {
                this.SetError("The start anchor must begin an alternative.");
                return null;
            }

            items.Add(item);
            if (item.Kind == PatternNodeKind.EndAnchor
                && this.Position < this.Text.Length
                && this.Text[this.Position] != '|'
                && this.Text[this.Position] != ')')
            {
                this.SetError("The end anchor must end an alternative.");
                return null;
            }
        }

        return items.Count == 1
            ? items[0]
            : new PatternNode
            {
                Kind = PatternNodeKind.Sequence,
                Children = items.ToArray()
            };
    }

    internal PatternNode? ParseRepetition()
    {
        PatternNode? atom = this.ParseAtom();
        if (atom == null || this.Position >= this.Text.Length)
        {
            return atom;
        }

        int minimum = 0;
        int maximum = 0;
        bool unbounded = false;
        bool repeated = true;
        char current = this.Text[this.Position];
        if (current == '*')
        {
            unbounded = true;
            this.Position = this.Position + 1;
        }
        else if (current == '+')
        {
            minimum = 1;
            unbounded = true;
            this.Position = this.Position + 1;
        }
        else if (current == '?')
        {
            maximum = 1;
            this.Position = this.Position + 1;
        }
        else if (current == '{')
        {
            PatternNode? quantified = this.ParseBraceRepetition(atom);
            if (quantified == null)
            {
                return null;
            }

            atom = quantified;
            repeated = false;
        }
        else
        {
            repeated = false;
        }

        if (repeated)
        {
            if (atom.Kind == PatternNodeKind.StartAnchor
                || atom.Kind == PatternNodeKind.EndAnchor)
            {
                this.SetError("An anchor cannot be repeated.");
                return null;
            }

            atom = new PatternNode
            {
                Kind = PatternNodeKind.Repeat,
                Children = [atom],
                Minimum = minimum,
                Maximum = maximum,
                Unbounded = unbounded
            };
        }

        if (this.Position < this.Text.Length
            && (this.Text[this.Position] == '*'
                || this.Text[this.Position] == '+'
                || this.Text[this.Position] == '?'
                || this.Text[this.Position] == '{'))
        {
            this.SetError("A pattern atom cannot have multiple repetition operators.");
            return null;
        }

        return atom;
    }

    internal PatternNode? ParseBraceRepetition(PatternNode atom)
    {
        if (atom.Kind == PatternNodeKind.StartAnchor
            || atom.Kind == PatternNodeKind.EndAnchor)
        {
            this.SetError("An anchor cannot be repeated.");
            return null;
        }

        this.Position = this.Position + 1;
        int minimum = this.ParseDecimalNumber();
        if (minimum < 0)
        {
            this.SetError("A repetition requires a decimal lower bound.");
            return null;
        }

        int maximum = minimum;
        bool unbounded = false;
        if (this.Position < this.Text.Length && this.Text[this.Position] == ',')
        {
            this.Position = this.Position + 1;
            if (this.Position < this.Text.Length && this.Text[this.Position] == '}')
            {
                unbounded = true;
            }
            else
            {
                maximum = this.ParseDecimalNumber();
                if (maximum < 0)
                {
                    this.SetError("A repetition requires a decimal upper bound.");
                    return null;
                }
            }
        }

        if (this.Position >= this.Text.Length || this.Text[this.Position] != '}')
        {
            this.SetError("A repetition must end with '}'.");
            return null;
        }

        this.Position = this.Position + 1;
        if (!unbounded && maximum < minimum)
        {
            this.SetError("A repetition upper bound cannot be less than its lower bound.");
            return null;
        }

        return new PatternNode
        {
            Kind = PatternNodeKind.Repeat,
            Children = [atom],
            Minimum = minimum,
            Maximum = maximum,
            Unbounded = unbounded
        };
    }

    internal int ParseDecimalNumber()
    {
        if (this.Position >= this.Text.Length
            || this.Text[this.Position] < '0'
            || this.Text[this.Position] > '9')
        {
            return -1;
        }

        int value = 0;
        while (this.Position < this.Text.Length
            && this.Text[this.Position] >= '0'
            && this.Text[this.Position] <= '9')
        {
            int digit = this.Text[this.Position] - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                this.SetError("A repetition bound is too large.");
                return -1;
            }

            value = value * 10 + digit;
            this.Position = this.Position + 1;
        }

        return value;
    }

    internal PatternNode? ParseAtom()
    {
        if (this.Position >= this.Text.Length)
        {
            this.SetError("Expected a pattern atom.");
            return null;
        }

        char current = this.Text[this.Position];
        if (current == '*' || current == '+' || current == '?'
            || current == '{' || current == '}')
        {
            this.SetError("A repetition operator must follow a pattern atom.");
            return null;
        }

        if (current == '(')
        {
            this.Position = this.Position + 1;
            PatternNode? grouped = this.ParseAlternation();
            if (grouped == null)
            {
                return null;
            }

            if (this.Position >= this.Text.Length || this.Text[this.Position] != ')')
            {
                this.SetError("A pattern group must end with ')'.");
                return null;
            }

            this.Position = this.Position + 1;
            return grouped;
        }

        if (current == '[')
        {
            return this.ParseCharacterClass();
        }

        this.Position = this.Position + 1;
        if (current == '.')
        {
            return new PatternNode { Kind = PatternNodeKind.Any };
        }

        if (current == '^')
        {
            return new PatternNode { Kind = PatternNodeKind.StartAnchor };
        }

        if (current == '$')
        {
            return new PatternNode { Kind = PatternNodeKind.EndAnchor };
        }

        if (current == '\\')
        {
            PatternScalarReadResult escaped = this.ParseEscapedScalar();
            return escaped.Success
                ? new PatternNode
                {
                    Kind = PatternNodeKind.Literal,
                    Scalar = escaped.Scalar
                }
                : null;
        }

        this.Position = this.Position - 1;
        PatternScalarReadResult literal = this.ReadScalar();
        return literal.Success
            ? new PatternNode
            {
                Kind = PatternNodeKind.Literal,
                Scalar = literal.Scalar
            }
            : null;
    }

    internal PatternNode? ParseCharacterClass()
    {
        this.Position = this.Position + 1;
        bool negated = false;
        if (this.Position < this.Text.Length && this.Text[this.Position] == '^')
        {
            negated = true;
            this.Position = this.Position + 1;
        }

        System.Collections.Generic.List<PatternNode> ranges =
            new System.Collections.Generic.List<PatternNode>();
        while (this.Position < this.Text.Length && this.Text[this.Position] != ']')
        {
            PatternScalarReadResult lower = this.ParseClassScalar();
            if (!lower.Success)
            {
                return null;
            }

            int upper = lower.Scalar;
            if (this.Position < this.Text.Length
                && this.Text[this.Position] == '-'
                && this.Position + 1 < this.Text.Length
                && this.Text[this.Position + 1] != ']')
            {
                this.Position = this.Position + 1;
                PatternScalarReadResult parsedUpper = this.ParseClassScalar();
                if (!parsedUpper.Success)
                {
                    return null;
                }

                upper = parsedUpper.Scalar;
                if (upper < lower.Scalar)
                {
                    this.SetError("A character-class range is descending.");
                    return null;
                }
            }

            ranges.Add(new PatternNode
            {
                Kind = PatternNodeKind.Range,
                LowerScalar = lower.Scalar,
                UpperScalar = upper
            });
        }

        if (ranges.Count == 0)
        {
            this.SetError("A character class cannot be empty.");
            return null;
        }

        if (this.Position >= this.Text.Length || this.Text[this.Position] != ']')
        {
            this.SetError("A character class must end with ']'.");
            return null;
        }

        this.Position = this.Position + 1;
        return new PatternNode
        {
            Kind = PatternNodeKind.CharacterClass,
            Children = ranges.ToArray(),
            Negated = negated
        };
    }

    internal PatternScalarReadResult ParseClassScalar()
    {
        if (this.Position >= this.Text.Length || this.Text[this.Position] == ']')
        {
            this.SetError("Expected a character-class item.");
            return new PatternScalarReadResult();
        }

        if (this.Text[this.Position] == '\\')
        {
            this.Position = this.Position + 1;
            return this.ParseEscapedScalar();
        }

        return this.ReadScalar();
    }

    internal PatternScalarReadResult ParseEscapedScalar()
    {
        if (this.Position >= this.Text.Length)
        {
            this.SetError("A pattern escape is incomplete.");
            return new PatternScalarReadResult();
        }

        char current = this.Text[this.Position];
        this.Position = this.Position + 1;
        if (current == 'n')
        {
            return new PatternScalarReadResult { Success = true, Scalar = 10 };
        }

        if (current == 'r')
        {
            return new PatternScalarReadResult { Success = true, Scalar = 13 };
        }

        if (current == 't')
        {
            return new PatternScalarReadResult { Success = true, Scalar = 9 };
        }

        if (current == 'u' || current == 'U')
        {
            return this.ParseHexScalar(current == 'u' ? 4 : 8);
        }

        this.Position = this.Position - 1;
        return this.ReadScalar();
    }

    internal PatternScalarReadResult ParseHexScalar(int digits)
    {
        int value = 0;
        int index = 0;
        while (index < digits)
        {
            if (this.Position >= this.Text.Length)
            {
                this.SetError("A Unicode pattern escape is incomplete.");
                return new PatternScalarReadResult();
            }

            int digit = this.HexValue(this.Text[this.Position]);
            if (digit < 0)
            {
                this.SetError("A Unicode pattern escape contains a non-hexadecimal digit.");
                return new PatternScalarReadResult();
            }

            value = value * 16 + digit;
            this.Position = this.Position + 1;
            index = index + 1;
        }

        if (!this.IsUnicodeScalar(value))
        {
            this.SetError("A Unicode pattern escape does not identify a scalar value.");
            return new PatternScalarReadResult();
        }

        return new PatternScalarReadResult { Success = true, Scalar = value };
    }

    internal int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return value >= 'a' && value <= 'f' ? value - 'a' + 10 : -1;
    }

    internal PatternScalarReadResult ReadScalar()
    {
        if (this.Position >= this.Text.Length)
        {
            return new PatternScalarReadResult();
        }

        int first = this.Text[this.Position];
        if (first >= 55296 && first <= 56319)
        {
            if (this.Position + 1 >= this.Text.Length)
            {
                this.SetError("A pattern contains an incomplete Unicode scalar.");
                return new PatternScalarReadResult();
            }

            int second = this.Text[this.Position + 1];
            if (second < 56320 || second > 57343)
            {
                this.SetError("A pattern contains an invalid Unicode scalar.");
                return new PatternScalarReadResult();
            }

            this.Position = this.Position + 2;
            return new PatternScalarReadResult
            {
                Success = true,
                Scalar = 65536 + (first - 55296) * 1024 + second - 56320
            };
        }

        if (first >= 56320 && first <= 57343)
        {
            this.SetError("A pattern contains an invalid Unicode scalar.");
            return new PatternScalarReadResult();
        }

        this.Position = this.Position + 1;
        return new PatternScalarReadResult { Success = true, Scalar = first };
    }

    internal bool IsUnicodeScalar(int value)
    {
        return value >= 0 && value <= 1114111
            && (value < 55296 || value > 57343);
    }

    internal void SetError(string message)
    {
        if (this.ErrorMessage.Length == 0)
        {
            this.ErrorMessage = message;
            this.ErrorOffset = this.Position;
        }
    }
}
