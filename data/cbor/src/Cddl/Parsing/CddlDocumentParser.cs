using System.Numerics;

namespace cbor;

internal class CddlDocumentParser
{
    internal string Source;
    internal CddlParseOptions Options;
    internal CddlLexer Lexer;
    internal CddlDocument Document;
    internal CddlToken Current = new CddlToken();
    internal int LexerDiagnosticIndex;
    internal int SourceOrder;
    internal int Depth;
    internal int ErrorCount;
    internal int NodeCount;
    internal bool HasError;
    internal bool RecoveryLimitReached;
    internal bool NodeLimitReached;

    internal CddlDocumentParser(string source, CddlParseOptions options)
    {
        Source = source;
        Options = options;
        Lexer = new CddlLexer(source, options);
        Document = new CddlDocument();
        Document.SourceName = options.SourceName;
        Document.SourceText = source;
    }

    internal CddlParseResult ParseSource()
    {
        ReadParserToken();
        bool parsing = true;
        while (parsing && Current.Kind != CddlTokenKind.EndOfFile)
        {
            int errorCountBeforeRule = ErrorCount;
            int ruleStart = Current.Span.Start;
            bool ruleFailed = false;
            if (Current.Kind == CddlTokenKind.Invalid)
            {
                HasError = true;
                ruleFailed = true;
            }
            else if (Current.Kind != CddlTokenKind.Identifier)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_RULE_NAME_EXPECTED",
                    "A CDDL rule must begin with a rule name.",
                    Current.Span
                );
                parsing = false;
            }
            else
            {
                CddlRule? rule = ParseRuleDefinition();
                if (rule != null)
                {
                    Document.Rules.Add(rule);
                    if (Document.RootRuleName == null
                        && (rule.Kind == CddlRuleKind.Type
                            || rule.Kind == CddlRuleKind.Group))
                    {
                        Document.RootRuleName = rule.Name;
                    }
                }
            }
            if (ruleFailed || ErrorCount > errorCountBeforeRule)
            {
                if (!Options.Recover || RecoveryLimitReached)
                {
                    parsing = false;
                }
                else
                {
                    RecoverToNextRule(ruleStart);
                }
            }
            else if (Current.Kind != CddlTokenKind.EndOfFile
                && Current.Span.Start <= ruleStart)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_RECOVERY_STALLED",
                    "The parser could not make progress after the current rule.",
                    Current.Span
                );
                parsing = false;
            }
        }

        CddlParseResult result = new CddlParseResult();
        result.IsSuccess = !HasError;
        result.Document = Document;
        result.Diagnostics = Document.Diagnostics.ToArray();
        result.Options = Options;
        return result;
    }

    internal CddlRule? ParseRuleDefinition()
    {
        int errorCountBeforeRule = ErrorCount;
        CddlToken name = Current;
        ReadParserToken();
        CddlRule rule = new CddlRule();
        int socketPrefixLength = SocketPrefixLength(name.Text);
        if (socketPrefixLength > 0 && name.Text.Length == socketPrefixLength)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_SOCKET_NAME_EXPECTED",
                "A socket prefix must be followed by a rule name.",
                name.Span
            );
            return null;
        }
        rule.Name = NormalizeRuleName(name.Text, socketPrefixLength);
        if (Current.Kind == CddlTokenKind.LessThan
            && AreSourceAdjacent(name.Span, Current.Span))
        {
            if (!ParseGenericParameterList(rule))
            {
                return null;
            }
        }
        CddlToken assignment = Current;
        if (assignment.Kind != CddlTokenKind.Equal
            && assignment.Kind != CddlTokenKind.TypeExtensionAssign
            && assignment.Kind != CddlTokenKind.GroupExtensionAssign)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_ASSIGNMENT_EXPECTED",
                "A rule name must be followed by '=', '/=', or '//='.",
                assignment.Span
            );
            return null;
        }
        ReadParserToken();

        rule.SourceOrder = SourceOrder;
        SourceOrder = SourceOrder + 1;

        CddlNode? value = null;
        bool missingBody = Current.Kind == CddlTokenKind.EndOfFile
            || IsRuleStartAhead();
        if (socketPrefixLength == 1)
        {
            if (assignment.Kind != CddlTokenKind.TypeExtensionAssign)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_TYPE_SOCKET_ASSIGNMENT_INVALID",
                    "A type socket plug must use '/='.",
                    assignment.Span
                );
                return null;
            }
            rule.Kind = CddlRuleKind.TypeSocket;
        }
        else if (socketPrefixLength == 2)
        {
            if (assignment.Kind != CddlTokenKind.GroupExtensionAssign)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_GROUP_SOCKET_ASSIGNMENT_INVALID",
                    "A group socket plug must use '//='.",
                    assignment.Span
                );
                return null;
            }
            rule.Kind = CddlRuleKind.GroupSocket;
        }
        else if (assignment.Kind == CddlTokenKind.TypeExtensionAssign)
        {
            rule.Kind = CddlRuleKind.TypeExtension;
        }
        else if (assignment.Kind == CddlTokenKind.GroupExtensionAssign)
        {
            rule.Kind = CddlRuleKind.GroupExtension;
        }
        else
        {
            rule.Kind = CddlRuleKind.Type;
        }
        if (missingBody)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_TYPE_EXPECTED",
                "A CDDL rule assignment requires a right-hand side.",
                Current.Span
            );
        }
        else if (rule.Kind == CddlRuleKind.TypeSocket
            || rule.Kind == CddlRuleKind.TypeExtension)
        {
            value = ParseTypeChoiceNode();
        }
        else if (rule.Kind == CddlRuleKind.GroupSocket
            || rule.Kind == CddlRuleKind.GroupExtension)
        {
            value = ParseGroupEntryNode();
        }
        else
        {
            value = ParseEqualDefinition(rule);
        }
        rule.Value = value;
        if (value == null)
        {
            rule.Span = SpanBetween(name.Span, assignment.Span);
        }
        else
        {
            rule.Span = SpanBetween(name.Span, value.Span);
        }

        if (ErrorCount == errorCountBeforeRule
            && Current.Kind != CddlTokenKind.EndOfFile
            && Current.Kind != CddlTokenKind.Identifier)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_RULE_END_EXPECTED",
                "The rule body must end before the next token.",
                Current.Span
            );
        }
        return rule;
    }

    internal void RecoverToNextRule(int failedRuleStart)
    {
        bool scanning = true;
        while (scanning && Current.Kind != CddlTokenKind.EndOfFile)
        {
            if (Current.Span.Start > failedRuleStart && IsRuleStartAhead())
            {
                scanning = false;
            }
            else
            {
                ReadParserToken();
                if (RecoveryLimitReached)
                {
                    scanning = false;
                }
            }
        }
    }

    internal bool IsRuleStartAhead()
    {
        if (Current.Kind != CddlTokenKind.Identifier)
        {
            return false;
        }
        CddlParserSnapshot snapshot = CaptureParserState();
        CddlSourceSpan nameSpan = Current.Span;
        ReadParserToken();
        if (Current.Kind == CddlTokenKind.LessThan
            && AreSourceAdjacent(nameSpan, Current.Span))
        {
            int angleDepth = 1;
            ReadParserToken();
            while (angleDepth > 0 && Current.Kind != CddlTokenKind.EndOfFile)
            {
                if (Current.Kind == CddlTokenKind.LessThan)
                {
                    angleDepth = angleDepth + 1;
                }
                else if (Current.Kind == CddlTokenKind.GreaterThan)
                {
                    angleDepth = angleDepth - 1;
                }
                ReadParserToken();
            }
        }
        bool isRule = Current.Kind == CddlTokenKind.Equal
            || Current.Kind == CddlTokenKind.TypeExtensionAssign
            || Current.Kind == CddlTokenKind.GroupExtensionAssign;
        RestoreParserState(snapshot);
        return isRule;
    }

    internal bool ParseGenericParameterList(CddlRule rule)
    {
        ReadParserToken();
        if (Current.Kind != CddlTokenKind.Identifier)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_GENERIC_PARAMETER_EXPECTED",
                "A generic parameter list requires a parameter name.",
                Current.Span
            );
            return false;
        }
        bool reading = true;
        while (reading)
        {
            rule.GenericParameters.Add(Current.Text);
            ReadParserToken();
            if (Current.Kind == CddlTokenKind.Comma)
            {
                ReadParserToken();
                if (Current.Kind != CddlTokenKind.Identifier)
                {
                    AddParserDiagnostic(
                        "CDDL_PARSE_GENERIC_PARAMETER_EXPECTED",
                        "A generic parameter comma requires another name.",
                        Current.Span
                    );
                    return false;
                }
            }
            else
            {
                reading = false;
            }
        }
        if (Current.Kind != CddlTokenKind.GreaterThan)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_GENERIC_PARAMETER_CLOSE_EXPECTED",
                "A generic parameter list has no closing angle bracket.",
                Current.Span
            );
            return false;
        }
        ReadParserToken();
        return true;
    }

    internal int SocketPrefixLength(string name)
    {
        if (name.Length >= 2 && name[0] == '$' && name[1] == '$')
        {
            return 2;
        }
        if (name.Length >= 1 && name[0] == '$')
        {
            return 1;
        }
        return 0;
    }

    internal string NormalizeRuleName(string name, int prefixLength)
    {
        if (prefixLength == 0)
        {
            return name;
        }
        return name.Substring(prefixLength);
    }

    internal CddlNode? ParseEqualDefinition(CddlRule rule)
    {
        if (IsOccurrenceStart())
        {
            rule.Kind = CddlRuleKind.Group;
            return ParseGroupEntryNode();
        }
        CddlParserSnapshot snapshot = CaptureParserState();
        CddlNode? first = ParseTypeExpressionNode();
        if (first == null)
        {
            rule.Kind = CddlRuleKind.Type;
            return null;
        }
        if (first.Kind == CddlNodeKind.ParenthesizedGroup)
        {
            rule.Kind = CddlRuleKind.Group;
            return CreateUnkeyedEntry(first, first.Span);
        }
        if (Current.Kind == CddlTokenKind.Colon
            || Current.Kind == CddlTokenKind.Arrow
            || Current.Kind == CddlTokenKind.Cut)
        {
            RestoreParserState(snapshot);
            rule.Kind = CddlRuleKind.Group;
            return ParseGroupEntryNode();
        }
        rule.Kind = CddlRuleKind.Type;
        return ParseTypeChoiceTail(first);
    }

    internal CddlNode? ParseGroupEntryNode()
    {
        CddlSourceSpan start = Current.Span;
        if (!ReserveSyntaxNode(start))
        {
            return null;
        }
        CddlNode entry = new CddlNode();
        entry.Kind = CddlNodeKind.GroupEntry;
        if (!ParseOccurrencePrefix(entry))
        {
            return null;
        }
        CddlNode? candidate = ParseTypeExpressionNode();
        if (candidate == null)
        {
            return null;
        }

        if (Current.Kind == CddlTokenKind.Cut)
        {
            entry.IsCut = true;
            ReadParserToken();
            if (Current.Kind != CddlTokenKind.Arrow)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_CUT_ARROW_EXPECTED",
                    "A map cut must be followed by '=>'.",
                    Current.Span
                );
                return null;
            }
        }
        if (Current.Kind == CddlTokenKind.Arrow)
        {
            entry.Children.Add(candidate);
            ReadParserToken();
            CddlNode? value = ParseTypeChoiceNode();
            if (value == null)
            {
                return null;
            }
            entry.Children.Add(value);
            entry.Span = SpanBetween(start, value.Span);
            return entry;
        }
        if (Current.Kind == CddlTokenKind.Colon)
        {
            bool bareword = candidate.Kind == CddlNodeKind.RuleReference
                && candidate.Children.Count == 0;
            bool literalKey = candidate.Kind == CddlNodeKind.Literal;
            if (!bareword && !literalKey)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_MEMBER_KEY_INVALID",
                    "A ':' member key must be a bareword or literal value.",
                    candidate.Span
                );
                return null;
            }
            if (bareword)
            {
                entry.Name = candidate.Name;
            }
            else
            {
                entry.Children.Add(candidate);
            }
            ReadParserToken();
            CddlNode? value = ParseTypeChoiceNode();
            if (value == null)
            {
                return null;
            }
            entry.Children.Add(value);
            entry.Span = SpanBetween(start, value.Span);
            return entry;
        }
        candidate = ParseTypeChoiceTail(candidate);
        if (candidate == null)
        {
            return null;
        }
        if (candidate.Kind == CddlNodeKind.TypeChoice
            && (Current.Kind == CddlTokenKind.Cut
                || Current.Kind == CddlTokenKind.Arrow
                || Current.Kind == CddlTokenKind.Colon))
        {
            AddParserDiagnostic(
                "CDDL_PARSE_MEMBER_KEY_INVALID",
                "A member key cannot contain a type choice.",
                candidate.Span
            );
            return null;
        }
        entry.Children.Add(candidate);
        entry.Span = SpanBetween(start, candidate.Span);
        return entry;
    }

    internal bool ParseOccurrencePrefix(CddlNode entry)
    {
        entry.Occurrence = CddlOccurrenceKind.ExactlyOne;
        entry.MinimumOccurrence = 1;
        entry.MaximumOccurrence = 1;
        if (Current.Kind == CddlTokenKind.Optional)
        {
            entry.Occurrence = CddlOccurrenceKind.Optional;
            entry.MinimumOccurrence = 0;
            entry.MaximumOccurrence = 1;
            ReadParserToken();
            return true;
        }
        if (Current.Kind == CddlTokenKind.Plus)
        {
            entry.Occurrence = CddlOccurrenceKind.OneOrMore;
            entry.MinimumOccurrence = 1;
            entry.MaximumOccurrence = -1;
            ReadParserToken();
            return true;
        }
        if (Current.Kind == CddlTokenKind.Asterisk)
        {
            entry.Occurrence = CddlOccurrenceKind.ZeroOrMore;
            entry.MinimumOccurrence = 0;
            entry.MaximumOccurrence = -1;
            ReadParserToken();
            if (Current.Kind == CddlTokenKind.Number)
            {
                entry.Occurrence = CddlOccurrenceKind.Bounded;
                if (!ReadOccurrenceBound(entry, false))
                {
                    return false;
                }
            }
            return true;
        }
        if (Current.Kind == CddlTokenKind.Number
            && PeekParserTokenKind() == CddlTokenKind.Asterisk)
        {
            entry.Occurrence = CddlOccurrenceKind.Bounded;
            if (!ReadOccurrenceBound(entry, true))
            {
                return false;
            }
            ReadParserToken();
            entry.MaximumOccurrence = -1;
            if (Current.Kind == CddlTokenKind.Number)
            {
                if (!ReadOccurrenceBound(entry, false))
                {
                    return false;
                }
            }
        }
        return true;
    }

    internal bool ReadOccurrenceBound(CddlNode entry, bool minimum)
    {
        CddlToken token = Current;
        if (!token.HasIntegerValue
            || token.LiteralKind != CddlLiteralKind.UnsignedInteger
            || token.IntegerValue > new BigInteger(int.MaxValue))
        {
            AddParserDiagnostic(
                "CDDL_PARSE_OCCURRENCE_BOUND_INVALID",
                "An occurrence bound must be a nonnegative Int32 integer.",
                token.Span
            );
            return false;
        }
        int value = (int)token.IntegerValue;
        if (minimum)
        {
            entry.MinimumOccurrence = value;
        }
        else
        {
            entry.MaximumOccurrence = value;
        }
        ReadParserToken();
        return true;
    }

    internal bool IsOccurrenceStart()
    {
        if (Current.Kind == CddlTokenKind.Optional
            || Current.Kind == CddlTokenKind.Plus
            || Current.Kind == CddlTokenKind.Asterisk)
        {
            return true;
        }
        return Current.Kind == CddlTokenKind.Number
            && PeekParserTokenKind() == CddlTokenKind.Asterisk;
    }

    internal CddlNode? ParseTypeExpressionNode()
    {
        CddlNode? left = ParsePrimaryNode();
        if (left == null)
        {
            return null;
        }
        if (Current.Kind == CddlTokenKind.RangeInclusive
            || Current.Kind == CddlTokenKind.RangeExclusive)
        {
            bool exclusive = Current.Kind == CddlTokenKind.RangeExclusive;
            ReadParserToken();
            CddlNode? right = ParsePrimaryNode();
            if (right == null)
            {
                return null;
            }
            if (!ReserveSyntaxNode(left.Span))
            {
                return null;
            }
            CddlNode range = new CddlNode();
            range.Kind = CddlNodeKind.Range;
            range.IsRangeUpperExclusive = exclusive;
            range.Children.Add(left);
            range.Children.Add(right);
            range.Span = SpanBetween(left.Span, right.Span);
            return range;
        }
        if (Current.Kind == CddlTokenKind.Dot)
        {
            CddlToken dot = Current;
            ReadParserToken();
            if (Current.Kind != CddlTokenKind.Identifier
                || !AreSourceAdjacent(dot.Span, Current.Span))
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_CONTROL_NAME_EXPECTED",
                    "A control operator dot must be followed by a name.",
                    Current.Span
                );
                return null;
            }
            CddlToken name = Current;
            ReadParserToken();
            CddlNode? controller = ParsePrimaryNode();
            if (controller == null)
            {
                return null;
            }
            if (!ReserveSyntaxNode(left.Span))
            {
                return null;
            }
            CddlNode control = new CddlNode();
            control.Kind = CddlNodeKind.Control;
            control.ControlName = "." + name.Text;
            control.Children.Add(left);
            control.Children.Add(controller);
            control.Span = SpanBetween(left.Span, controller.Span);
            return control;
        }
        return left;
    }

    internal CddlNode? ParseTypeChoiceNode()
    {
        CddlNode? first = ParseTypeExpressionNode();
        if (first == null)
        {
            return null;
        }
        return ParseTypeChoiceTail(first);
    }

    internal CddlNode? ParseTypeChoiceTail(CddlNode first)
    {
        if (Current.Kind != CddlTokenKind.Slash)
        {
            return first;
        }
        if (!ReserveSyntaxNode(first.Span))
        {
            return null;
        }
        CddlNode choice = new CddlNode();
        choice.Kind = CddlNodeKind.TypeChoice;
        choice.Children.Add(first);
        CddlNode last = first;
        while (Current.Kind == CddlTokenKind.Slash && !NodeLimitReached)
        {
            ReadParserToken();
            CddlNode? alternative = ParseTypeExpressionNode();
            if (alternative == null)
            {
                break;
            }
            choice.Children.Add(alternative);
            last = alternative;
        }
        choice.Span = SpanBetween(first.Span, last.Span);
        return choice;
    }

    internal CddlNode? ParseGroupChoiceNode()
    {
        CddlNode? first = ParseGroupSequenceNode();
        if (first == null)
        {
            return null;
        }
        if (Current.Kind != CddlTokenKind.GroupChoice)
        {
            return first;
        }
        if (!ReserveSyntaxNode(first.Span))
        {
            return null;
        }
        CddlNode choice = new CddlNode();
        choice.Kind = CddlNodeKind.GroupChoice;
        choice.Children.Add(first);
        CddlNode last = first;
        while (Current.Kind == CddlTokenKind.GroupChoice && !NodeLimitReached)
        {
            ReadParserToken();
            CddlNode? alternative = ParseGroupSequenceNode();
            if (alternative == null)
            {
                break;
            }
            choice.Children.Add(alternative);
            last = alternative;
        }
        choice.Span = SpanBetween(first.Span, last.Span);
        return choice;
    }

    internal CddlNode? ParseGroupSequenceNode()
    {
        if (!ReserveSyntaxNode(Current.Span))
        {
            return null;
        }
        CddlNode group = new CddlNode();
        group.Kind = CddlNodeKind.Group;
        CddlSourceSpan start = EmptySpanAtCurrent();
        CddlSourceSpan last = start;
        bool reading = true;
        while (reading && !NodeLimitReached && CanStartGroupEntry())
        {
            CddlNode? entry = ParseGroupEntryNode();
            if (entry == null)
            {
                return null;
            }
            if (group.Children.Count == 0)
            {
                start = entry.Span;
            }
            group.Children.Add(entry);
            last = entry.Span;
            if (Current.Kind == CddlTokenKind.Comma)
            {
                last = Current.Span;
                ReadParserToken();
                if (!CanStartGroupEntry())
                {
                    reading = false;
                }
            }
            else if (!CanStartGroupEntry())
            {
                reading = false;
            }
        }
        group.Span = SpanBetween(start, last);
        return group;
    }

    internal bool CanStartGroupEntry()
    {
        if (IsOccurrenceStart())
        {
            return true;
        }
        return Current.Kind == CddlTokenKind.Identifier
            || Current.Kind == CddlTokenKind.Number
            || Current.Kind == CddlTokenKind.TextString
            || Current.Kind == CddlTokenKind.ByteString
            || Current.Kind == CddlTokenKind.LeftParenthesis
            || Current.Kind == CddlTokenKind.LeftBracket
            || Current.Kind == CddlTokenKind.LeftBrace
            || Current.Kind == CddlTokenKind.Unwrap
            || Current.Kind == CddlTokenKind.GroupEnumeration
            || Current.Kind == CddlTokenKind.Hash;
    }

    internal CddlNode? ParsePrimaryNode()
    {
        CddlToken token = Current;
        if (token.Kind == CddlTokenKind.Invalid)
        {
            HasError = true;
            return null;
        }
        if (token.Kind == CddlTokenKind.Identifier)
        {
            return ParseReferenceNode();
        }
        if (token.Kind == CddlTokenKind.Number
            || token.Kind == CddlTokenKind.TextString
            || token.Kind == CddlTokenKind.ByteString)
        {
            CddlNode? literal = CreateLiteralNode(token);
            if (literal == null)
            {
                return null;
            }
            ReadParserToken();
            return literal;
        }
        if (token.Kind == CddlTokenKind.LeftBracket)
        {
            return ParseArrayNode();
        }
        if (token.Kind == CddlTokenKind.LeftBrace)
        {
            return ParseMapNode();
        }
        if (token.Kind == CddlTokenKind.LeftParenthesis)
        {
            return ParseParenthesizedNode();
        }
        if (token.Kind == CddlTokenKind.Unwrap)
        {
            return ParseUnwrapNode();
        }
        if (token.Kind == CddlTokenKind.GroupEnumeration)
        {
            return ParseGroupEnumerationNode();
        }
        if (token.Kind == CddlTokenKind.Hash)
        {
            return ParseHashNode();
        }
        AddParserDiagnostic(
            "CDDL_PARSE_TYPE_EXPECTED",
            "A rule body must contain a type or group entry.",
            token.Span
        );
        return null;
    }

    internal CddlNode? CreateLiteralNode(CddlToken token)
    {
        if (!ReserveSyntaxNode(token.Span))
        {
            return null;
        }
        CddlNode literal = new CddlNode();
        literal.Kind = CddlNodeKind.Literal;
        literal.LiteralKind = token.LiteralKind;
        literal.Span = token.Span;
        literal.IntegerValue = token.IntegerValue;
        literal.HasIntegerValue = token.HasIntegerValue;
        literal.FloatValue = token.FloatValue;
        literal.HasFloatValue = token.HasFloatValue;
        if (token.Kind == CddlTokenKind.TextString)
        {
            literal.TextValue = token.StringValue;
        }
        else if (token.Kind == CddlTokenKind.ByteString)
        {
            literal.ByteStringValue = token.ByteStringValue;
        }
        else
        {
            literal.TextValue = token.Text;
        }
        return literal;
    }

    internal CddlNode? ParseReferenceNode()
    {
        CddlToken name = Current;
        if (!ReserveSyntaxNode(name.Span))
        {
            return null;
        }
        CddlNode reference = new CddlNode();
        reference.Kind = CddlNodeKind.RuleReference;
        reference.Name = name.Text;
        reference.Span = name.Span;
        ReadParserToken();
        if (Current.Kind == CddlTokenKind.LessThan
            && AreSourceAdjacent(name.Span, Current.Span))
        {
            if (!ParseGenericArgumentList(reference))
            {
                return null;
            }
        }
        return reference;
    }

    internal bool ParseGenericArgumentList(CddlNode reference)
    {
        CddlToken opening = Current;
        if (!EnterNestedSyntax(opening.Span))
        {
            return false;
        }
        ReadParserToken();
        if (Current.Kind == CddlTokenKind.GreaterThan)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_GENERIC_ARGUMENT_EXPECTED",
                "A generic argument list requires at least one argument.",
                Current.Span
            );
            Depth = Depth - 1;
            return false;
        }
        bool reading = true;
        while (reading)
        {
            CddlNode? argument = ParseTypeExpressionNode();
            if (argument == null)
            {
                Depth = Depth - 1;
                return false;
            }
            reference.Children.Add(argument);
            if (Current.Kind == CddlTokenKind.Comma)
            {
                ReadParserToken();
                if (Current.Kind == CddlTokenKind.GreaterThan)
                {
                    AddParserDiagnostic(
                        "CDDL_PARSE_GENERIC_ARGUMENT_EXPECTED",
                        "A generic argument comma requires another type.",
                        Current.Span
                    );
                    Depth = Depth - 1;
                    return false;
                }
            }
            else
            {
                reading = false;
            }
        }
        if (Current.Kind != CddlTokenKind.GreaterThan)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_GENERIC_ARGUMENT_CLOSE_EXPECTED",
                "A generic argument list has no closing angle bracket.",
                Current.Span
            );
            Depth = Depth - 1;
            return false;
        }
        CddlToken closing = Current;
        ReadParserToken();
        Depth = Depth - 1;
        reference.Span = SpanBetween(reference.Span, closing.Span);
        return true;
    }

    internal CddlNode? ParseUnwrapNode()
    {
        CddlToken opening = Current;
        ReadParserToken();
        if (Current.Kind != CddlTokenKind.Identifier)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_UNWRAP_NAME_EXPECTED",
                "The unwrapping operator must be followed by a type name.",
                Current.Span
            );
            return null;
        }
        CddlNode? reference = ParseReferenceNode();
        if (reference == null)
        {
            return null;
        }
        if (!ReserveSyntaxNode(opening.Span))
        {
            return null;
        }
        CddlNode unwrap = new CddlNode();
        unwrap.Kind = CddlNodeKind.Unwrap;
        unwrap.Name = reference.Name;
        int index = 0;
        while (index < reference.Children.Count)
        {
            unwrap.Children.Add(reference.Children[index]);
            index = index + 1;
        }
        unwrap.Span = SpanBetween(opening.Span, reference.Span);
        return unwrap;
    }

    internal CddlNode? ParseGroupEnumerationNode()
    {
        CddlToken opening = Current;
        ReadParserToken();
        if (!ReserveSyntaxNode(opening.Span))
        {
            return null;
        }
        CddlNode enumeration = new CddlNode();
        enumeration.Kind = CddlNodeKind.GroupEnumeration;
        if (Current.Kind == CddlTokenKind.Identifier)
        {
            CddlNode? reference = ParseReferenceNode();
            if (reference == null)
            {
                return null;
            }
            enumeration.Name = reference.Name;
            int index = 0;
            while (index < reference.Children.Count)
            {
                enumeration.Children.Add(reference.Children[index]);
                index = index + 1;
            }
            enumeration.Span = SpanBetween(opening.Span, reference.Span);
            return enumeration;
        }
        if (Current.Kind == CddlTokenKind.LeftParenthesis)
        {
            CddlNode? group = ParseParenthesizedGroupNode();
            if (group == null)
            {
                return null;
            }
            enumeration.Children.Add(group);
            enumeration.Span = SpanBetween(opening.Span, group.Span);
            return enumeration;
        }
        AddParserDiagnostic(
            "CDDL_PARSE_GROUP_ENUMERATION_EXPECTED",
            "The group enumeration operator requires a group name or group.",
            Current.Span
        );
        return null;
    }

    internal CddlNode? ParseHashNode()
    {
        CddlToken hash = Current;
        ReadParserToken();
        if (Current.Kind != CddlTokenKind.Number
            || !AreSourceAdjacent(hash.Span, Current.Span))
        {
            return CreateRepresentationNode(hash, -1, null, hash.Span);
        }
        CddlToken majorToken = Current;
        if (!majorToken.HasIntegerValue
            || majorToken.LiteralKind != CddlLiteralKind.UnsignedInteger
            || majorToken.IntegerValue > new BigInteger(7))
        {
            AddParserDiagnostic(
                "CDDL_PARSE_MAJOR_TYPE_INVALID",
                "A representation major type must be one decimal digit.",
                majorToken.Span
            );
            return null;
        }
        int majorType = (int)majorToken.IntegerValue;
        ReadParserToken();
        CddlHeadArgument? argument = null;
        CddlSourceSpan headEnd = majorToken.Span;
        if (Current.Kind == CddlTokenKind.Dot
            && AreSourceAdjacent(majorToken.Span, Current.Span))
        {
            CddlToken dot = Current;
            ReadParserToken();
            if (!AreSourceAdjacent(dot.Span, Current.Span))
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_HEAD_ARGUMENT_EXPECTED",
                    "A representation head cannot contain whitespace after its dot.",
                    Current.Span
                );
                return null;
            }
            argument = ParseHeadArgumentNode();
            if (argument == null)
            {
                return null;
            }
            headEnd = argument.Span;
        }

        if (majorType == 6)
        {
            if (Current.Kind == CddlTokenKind.LeftParenthesis
                && AreSourceAdjacent(headEnd, Current.Span))
            {
                return ParseTagNode(hash, argument);
            }
            if (argument != null && argument.Type != null)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_TAG_CONTENT_EXPECTED",
                    "A type-valued tag number must be followed by tag content.",
                    Current.Span
                );
                return null;
            }
            return CreateRepresentationNode(
                hash,
                majorType,
                argument,
                majorToken.Span
            );
        }
        if (majorType != 7 && argument != null && argument.Type != null)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_REPRESENTATION_ARGUMENT_INVALID",
                "A representation additional-information value must be an integer.",
                argument.Span
            );
            return null;
        }
        return CreateRepresentationNode(
            hash,
            majorType,
            argument,
            majorToken.Span
        );
    }

    internal CddlHeadArgument? ParseHeadArgumentNode()
    {
        if (Current.Kind == CddlTokenKind.Number)
        {
            CddlToken number = Current;
            if (!number.HasIntegerValue
                || number.LiteralKind != CddlLiteralKind.UnsignedInteger)
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_HEAD_ARGUMENT_INVALID",
                    "A literal head argument must be an unsigned integer.",
                    number.Span
                );
                return null;
            }
            CddlHeadArgument literal = new CddlHeadArgument();
            literal.Value = number.IntegerValue;
            literal.HasValue = true;
            literal.Span = number.Span;
            ReadParserToken();
            return literal;
        }
        if (Current.Kind == CddlTokenKind.LessThan)
        {
            CddlToken opening = Current;
            if (!EnterNestedSyntax(opening.Span))
            {
                return null;
            }
            ReadParserToken();
            if (!AreSourceAdjacent(opening.Span, Current.Span))
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_HEAD_ARGUMENT_EXPECTED",
                    "A type-valued head argument cannot start with whitespace.",
                    Current.Span
                );
                Depth = Depth - 1;
                return null;
            }
            CddlNode? type = ParseTypeChoiceNode();
            if (type == null)
            {
                Depth = Depth - 1;
                return null;
            }
            if (Current.Kind != CddlTokenKind.GreaterThan
                || !AreSourceAdjacent(type.Span, Current.Span))
            {
                AddParserDiagnostic(
                    "CDDL_PARSE_HEAD_ARGUMENT_CLOSE_EXPECTED",
                    "A type-valued head argument has no closing angle bracket.",
                    Current.Span
                );
                Depth = Depth - 1;
                return null;
            }
            CddlToken closing = Current;
            ReadParserToken();
            Depth = Depth - 1;
            CddlHeadArgument typed = new CddlHeadArgument();
            typed.Type = type;
            typed.Span = SpanBetween(opening.Span, closing.Span);
            return typed;
        }
        AddParserDiagnostic(
            "CDDL_PARSE_HEAD_ARGUMENT_EXPECTED",
            "A dot in a representation head requires an integer or '<type>'.",
            Current.Span
        );
        return null;
    }

    internal CddlNode? ParseTagNode(
        CddlToken hash,
        CddlHeadArgument? argument)
    {
        CddlToken opening = Current;
        if (!EnterNestedSyntax(opening.Span))
        {
            return null;
        }
        ReadParserToken();
        CddlNode? content = ParseTypeChoiceNode();
        if (content == null)
        {
            Depth = Depth - 1;
            return null;
        }
        if (Current.Kind != CddlTokenKind.RightParenthesis)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_TAG_CONTENT_CLOSE_EXPECTED",
                "A semantic tag has no closing parenthesis.",
                Current.Span
            );
            Depth = Depth - 1;
            return null;
        }
        CddlToken closing = Current;
        ReadParserToken();
        Depth = Depth - 1;
        if (!ReserveSyntaxNode(hash.Span))
        {
            return null;
        }
        CddlNode tag = new CddlNode();
        tag.Kind = CddlNodeKind.Tag;
        tag.MajorType = 6;
        if (argument != null)
        {
            tag.ArgumentValue = argument.Value;
            tag.HasArgumentValue = argument.HasValue;
            if (argument.Type != null)
            {
                tag.Children.Add(argument.Type);
            }
        }
        tag.Children.Add(content);
        tag.Span = SpanBetween(hash.Span, closing.Span);
        return tag;
    }

    internal CddlNode? CreateRepresentationNode(
        CddlToken hash,
        int majorType,
        CddlHeadArgument? argument,
        CddlSourceSpan majorSpan)
    {
        if (!ReserveSyntaxNode(hash.Span))
        {
            return null;
        }
        CddlNode representation = new CddlNode();
        representation.Kind = CddlNodeKind.Representation;
        representation.MajorType = majorType;
        representation.Span = SpanBetween(hash.Span, majorSpan);
        if (argument != null)
        {
            representation.ArgumentValue = argument.Value;
            representation.HasArgumentValue = argument.HasValue;
            if (argument.Type != null)
            {
                representation.Children.Add(argument.Type);
            }
            representation.Span = SpanBetween(hash.Span, argument.Span);
        }
        return representation;
    }

    internal CddlNode? ParseArrayNode()
    {
        return ParseContainerNode(CddlNodeKind.Array, CddlTokenKind.RightBracket);
    }

    internal CddlNode? ParseMapNode()
    {
        return ParseContainerNode(CddlNodeKind.Map, CddlTokenKind.RightBrace);
    }

    internal CddlNode? ParseContainerNode(
        CddlNodeKind kind,
        CddlTokenKind closingKind)
    {
        CddlToken opening = Current;
        if (!EnterNestedSyntax(opening.Span))
        {
            return null;
        }
        ReadParserToken();
        CddlNode? group = ParseGroupChoiceNode();
        if (group == null)
        {
            Depth = Depth - 1;
            return null;
        }
        if (Current.Kind != closingKind)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_CONTAINER_CLOSE_EXPECTED",
                "The container has no matching closing delimiter.",
                Current.Span
            );
            Depth = Depth - 1;
            return null;
        }
        CddlToken closing = Current;
        ReadParserToken();
        Depth = Depth - 1;
        if (!ReserveSyntaxNode(opening.Span))
        {
            return null;
        }
        CddlNode container = new CddlNode();
        container.Kind = kind;
        container.Children.Add(group);
        container.Span = SpanBetween(opening.Span, closing.Span);
        return container;
    }

    internal CddlNode? ParseParenthesizedNode()
    {
        CddlParserSnapshot snapshot = CaptureParserState();
        CddlNode? type = ParseParenthesizedTypeNode();
        if (type != null && !HasError)
        {
            return type;
        }
        RestoreParserState(snapshot);
        return ParseParenthesizedGroupNode();
    }

    internal CddlNode? ParseParenthesizedTypeNode()
    {
        CddlToken opening = Current;
        if (!EnterNestedSyntax(opening.Span))
        {
            return null;
        }
        ReadParserToken();
        CddlNode? type = ParseTypeChoiceNode();
        if (type == null)
        {
            Depth = Depth - 1;
            return null;
        }
        if (Current.Kind != CddlTokenKind.RightParenthesis)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_PARENTHESIS_CLOSE_EXPECTED",
                "The parenthesized type has no closing parenthesis.",
                Current.Span
            );
            Depth = Depth - 1;
            return null;
        }
        CddlToken closing = Current;
        ReadParserToken();
        Depth = Depth - 1;
        if (!ReserveSyntaxNode(opening.Span))
        {
            return null;
        }
        CddlNode parenthesized = new CddlNode();
        parenthesized.Kind = CddlNodeKind.ParenthesizedType;
        parenthesized.Children.Add(type);
        parenthesized.Span = SpanBetween(opening.Span, closing.Span);
        return parenthesized;
    }

    internal CddlNode? ParseParenthesizedGroupNode()
    {
        CddlToken opening = Current;
        if (!EnterNestedSyntax(opening.Span))
        {
            return null;
        }
        ReadParserToken();
        CddlNode? group = ParseGroupChoiceNode();
        if (group == null)
        {
            Depth = Depth - 1;
            return null;
        }
        if (Current.Kind != CddlTokenKind.RightParenthesis)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_PARENTHESIS_CLOSE_EXPECTED",
                "The parenthesized group has no closing parenthesis.",
                Current.Span
            );
            Depth = Depth - 1;
            return null;
        }
        CddlToken closing = Current;
        ReadParserToken();
        Depth = Depth - 1;
        if (!ReserveSyntaxNode(opening.Span))
        {
            return null;
        }
        CddlNode parenthesized = new CddlNode();
        parenthesized.Kind = CddlNodeKind.ParenthesizedGroup;
        parenthesized.Children.Add(group);
        parenthesized.Span = SpanBetween(opening.Span, closing.Span);
        return parenthesized;
    }

    internal bool EnterNestedSyntax(CddlSourceSpan span)
    {
        if (Depth >= Options.MaxNestingDepth)
        {
            AddParserDiagnostic(
                "CDDL_PARSE_DEPTH_LIMIT",
                "The CDDL syntax exceeds the configured nesting depth.",
                span
            );
            return false;
        }
        Depth = Depth + 1;
        return true;
    }

    internal bool ReserveSyntaxNode(CddlSourceSpan span)
    {
        if (Options.MaxNodes < 0 || NodeCount >= Options.MaxNodes)
        {
            if (!NodeLimitReached)
            {
                NodeLimitReached = true;
                RecoveryLimitReached = true;
                AddParserDiagnostic(
                    "CDDL_PARSE_NODE_LIMIT",
                    "The CDDL syntax exceeds the configured AST node limit.",
                    span
                );
            }
            return false;
        }
        NodeCount = NodeCount + 1;
        return true;
    }

    internal CddlNode? CreateUnkeyedEntry(
        CddlNode value,
        CddlSourceSpan start)
    {
        if (!ReserveSyntaxNode(start))
        {
            return null;
        }
        CddlNode entry = new CddlNode();
        entry.Kind = CddlNodeKind.GroupEntry;
        entry.Children.Add(value);
        entry.Span = SpanBetween(start, value.Span);
        return entry;
    }

    internal CddlTokenKind PeekParserTokenKind()
    {
        CddlParserSnapshot snapshot = CaptureParserState();
        ReadParserToken();
        CddlTokenKind kind = Current.Kind;
        RestoreParserState(snapshot);
        return kind;
    }

    internal CddlParserSnapshot CaptureParserState()
    {
        CddlParserSnapshot snapshot = new CddlParserSnapshot();
        snapshot.Current = Current;
        snapshot.LexerOffset = Lexer.Offset;
        snapshot.LexerLine = Lexer.Line;
        snapshot.LexerColumn = Lexer.Column;
        snapshot.LexerTokenCount = Lexer.TokenCount;
        snapshot.LexerDiagnosticCount = Lexer.Diagnostics.Count;
        snapshot.LexerDiagnosticIndex = LexerDiagnosticIndex;
        snapshot.DocumentDiagnosticCount = Document.Diagnostics.Count;
        snapshot.TriviaCount = Document.Trivia.Count;
        snapshot.TriviaSpanCount = Document.TriviaSpans.Count;
        snapshot.Depth = Depth;
        snapshot.ErrorCount = ErrorCount;
        snapshot.NodeCount = NodeCount;
        snapshot.HasError = HasError;
        snapshot.RecoveryLimitReached = RecoveryLimitReached;
        snapshot.NodeLimitReached = NodeLimitReached;
        return snapshot;
    }

    internal void RestoreParserState(CddlParserSnapshot snapshot)
    {
        Current = snapshot.Current;
        Lexer.Offset = snapshot.LexerOffset;
        Lexer.Line = snapshot.LexerLine;
        Lexer.Column = snapshot.LexerColumn;
        Lexer.TokenCount = snapshot.LexerTokenCount;
        LexerDiagnosticIndex = snapshot.LexerDiagnosticIndex;
        Depth = snapshot.Depth;
        ErrorCount = snapshot.ErrorCount;
        NodeCount = snapshot.NodeCount;
        HasError = snapshot.HasError;
        RecoveryLimitReached = snapshot.RecoveryLimitReached;
        NodeLimitReached = snapshot.NodeLimitReached;
        while (Lexer.Diagnostics.Count > snapshot.LexerDiagnosticCount)
        {
            Lexer.Diagnostics.RemoveAt(Lexer.Diagnostics.Count - 1);
        }
        while (Document.Diagnostics.Count > snapshot.DocumentDiagnosticCount)
        {
            Document.Diagnostics.RemoveAt(Document.Diagnostics.Count - 1);
        }
        while (Document.Trivia.Count > snapshot.TriviaCount)
        {
            Document.Trivia.RemoveAt(Document.Trivia.Count - 1);
        }
        while (Document.TriviaSpans.Count > snapshot.TriviaSpanCount)
        {
            Document.TriviaSpans.RemoveAt(Document.TriviaSpans.Count - 1);
        }
    }

    internal void ReadParserToken()
    {
        Current = Lexer.ReadToken();
        int errorCountBeforeToken = ErrorCount;
        while (LexerDiagnosticIndex < Lexer.Diagnostics.Count)
        {
            RegisterParserDiagnostic(Lexer.Diagnostics[LexerDiagnosticIndex]);
            LexerDiagnosticIndex = LexerDiagnosticIndex + 1;
        }
        if (Current.Kind == CddlTokenKind.Invalid
            && ErrorCount == errorCountBeforeToken)
        {
            CddlDiagnostic diagnostic = new CddlDiagnostic();
            diagnostic.Severity = CddlDiagnosticSeverity.Error;
            diagnostic.Code = Current.ErrorCode ?? "CDDL_LEX_TOKEN_INVALID";
            diagnostic.Message = Current.ErrorMessage ?? "The CDDL token is invalid.";
            diagnostic.Span = Current.Span;
            RegisterParserDiagnostic(diagnostic);
        }
        int index = 0;
        while (index < Current.LeadingTrivia.Count)
        {
            CddlTrivia trivia = Current.LeadingTrivia[index];
            Document.Trivia.Add(trivia.Text);
            Document.TriviaSpans.Add(trivia.Span);
            index = index + 1;
        }
    }

    internal void AddParserDiagnostic(
        string code,
        string message,
        CddlSourceSpan span)
    {
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = CddlDiagnosticSeverity.Error;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Span = span;
        RegisterParserDiagnostic(diagnostic);
    }

    internal void RegisterParserDiagnostic(CddlDiagnostic diagnostic)
    {
        if (!IsDiagnosticSpanAvailable(diagnostic.Span))
        {
            HasError = true;
            return;
        }
        HasError = true;
        ErrorCount = ErrorCount + 1;
        if (Options.MaxDiagnostics > 0
            && Document.Diagnostics.Count < Options.MaxDiagnostics)
        {
            Document.Diagnostics.Add(diagnostic);
        }
        if (Options.MaxDiagnostics <= 0
            || Document.Diagnostics.Count >= Options.MaxDiagnostics)
        {
            RecoveryLimitReached = true;
        }
    }

    internal bool IsDiagnosticSpanAvailable(CddlSourceSpan span)
    {
        int index = 0;
        int spanEnd = span.Start + (span.Length > 0 ? span.Length : 1);
        while (index < Document.Diagnostics.Count)
        {
            CddlSourceSpan existing = Document.Diagnostics[index].Span;
            int existingEnd = existing.Start
                + (existing.Length > 0 ? existing.Length : 1);
            if (span.Start < existingEnd && existing.Start < spanEnd)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal CddlSourceSpan EmptySpanAtCurrent()
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = Current.Span.SourceName;
        span.Start = Current.Span.Start;
        span.StartLine = Current.Span.StartLine;
        span.StartColumn = Current.Span.StartColumn;
        span.EndLine = Current.Span.StartLine;
        span.EndColumn = Current.Span.StartColumn;
        return span;
    }

    internal bool AreSourceAdjacent(
        CddlSourceSpan first,
        CddlSourceSpan second)
    {
        return first.Start + first.Length == second.Start;
    }

    internal CddlSourceSpan SpanBetween(
        CddlSourceSpan first,
        CddlSourceSpan last)
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = first.SourceName;
        span.Start = first.Start;
        span.Length = last.Start + last.Length - first.Start;
        span.StartLine = first.StartLine;
        span.StartColumn = first.StartColumn;
        span.EndLine = last.EndLine;
        span.EndColumn = last.EndColumn;
        return span;
    }
}
