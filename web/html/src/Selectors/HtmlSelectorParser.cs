using System;
using System.Collections.Generic;
using System.Text;

namespace html;

internal class HtmlSelectorParseResult
{
    internal HtmlSelectorListAst? selectorList;
    internal HtmlDiagnostic[] diagnostics = Array.Empty<HtmlDiagnostic>();

    internal HtmlSelectorParseResult(HtmlSelectorListAst? selectorList,
        HtmlDiagnostic[] diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        this.selectorList = selectorList;
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    internal bool IsSuccess()
    {
        return this.selectorList != null && this.diagnostics.Length == 0;
    }

    internal HtmlSelectorListAst? GetSelectorList()
    {
        return this.selectorList;
    }

    internal HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A selector diagnostic cannot be null.", "diagnostics");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal class HtmlSelectorParser
{
    internal const int MaximumLogicalNesting = 128;
    internal const string ColumnCombinatorUnsupportedMessage =
        "The CSS column combinator '||' is not supported.";
    internal const string StatePseudoClassUnsupportedMessage =
        "This pseudo-class depends on browser or user-agent state and is "
            + "not supported.";
    internal const string PseudoElementUnsupportedMessage =
        "Pseudo-elements are not supported.";

    internal HtmlSelectorToken[] tokens = Array.Empty<HtmlSelectorToken>();
    internal int position;
    internal List<HtmlDiagnostic> diagnostics = new List<HtmlDiagnostic>();
    internal int lexicalDiagnosticCount;
    internal bool failed;
    internal bool terminalFailure;
    internal bool parsed;
    internal HtmlSelectorParseResult? result;
    internal int complexity;
    internal int maximumComplexity;
    internal int selectorListNesting;
    internal int hasNesting;

    internal HtmlSelectorParser(string selector, HtmlLimits limits)
    {
        this.Initialize(selector, limits);
    }

    internal static HtmlSelectorParser Create(string selector)
    {
        return new HtmlSelectorParser(selector, HtmlLimits.CreateDefault());
    }

    internal static HtmlSelectorParser Create(string selector,
        HtmlLimits limits)
    {
        return new HtmlSelectorParser(selector, limits);
    }

    internal void Initialize(string selector, HtmlLimits limits)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        HtmlSelectorLexer lexer = new HtmlSelectorLexer(selector);
        this.tokens = lexer.Tokenize();
        this.position = 0;
        this.diagnostics = new List<HtmlDiagnostic>();
        HtmlDiagnostic[] lexicalDiagnostics = lexer.GetDiagnostics();
        int index = 0;
        while (index < lexicalDiagnostics.Length)
        {
            this.diagnostics.Add(lexicalDiagnostics[index]);
            index = index + 1;
        }

        this.lexicalDiagnosticCount = lexicalDiagnostics.Length;
        this.failed = false;
        this.terminalFailure = false;
        this.parsed = false;
        this.result = null;
        this.complexity = 0;
        this.maximumComplexity = limits.GetMaximumSelectorComplexity();
        this.selectorListNesting = 0;
        this.hasNesting = 0;
    }

    internal HtmlSelectorParseResult? Parse()
    {
        return this.ParseTopLevel(false);
    }

    internal HtmlSelectorParseResult? ParseForgiving()
    {
        return this.ParseTopLevel(true);
    }

    internal HtmlSelectorParseResult? ParseTopLevel(bool forgiving)
    {
        if (this.parsed)
        {
            return this.result;
        }

        this.parsed = true;
        this.SkipWhitespace();
        HtmlSelectorToken first = this.Current();
        if (first.GetKind() == HtmlSelectorTokenKind.End)
        {
            this.Fail("A selector list cannot be empty.", first);
            return this.Complete(null);
        }

        List<HtmlComplexSelectorAst> selectors =
            new List<HtmlComplexSelectorAst>();
        bool complete = false;
        while (!complete && !this.terminalFailure)
        {
            this.SkipWhitespace();
            if (this.Current().GetKind() == HtmlSelectorTokenKind.End)
            {
                complete = true;
                continue;
            }

            if (this.IsDelimiter(","))
            {
                this.Fail("A selector list cannot contain an empty selector.",
                    this.Current());
                if (!forgiving)
                {
                    complete = true;
                    continue;
                }

                this.failed = false;
                this.ConsumeCurrent();
                continue;
            }

            int branchStart = this.position;
            HtmlComplexSelectorAst? selector = this.ParseComplexSelector();
            this.SkipWhitespace();
            if (!this.failed && !this.IsDelimiter(",")
                && this.Current().GetKind()
                    != HtmlSelectorTokenKind.End)
            {
                this.Fail("Unexpected token after a complex selector.",
                    this.Current());
            }

            if (this.failed)
            {
                if (!forgiving || this.terminalFailure)
                {
                    complete = true;
                    continue;
                }

                this.failed = false;
                this.position = branchStart;
                this.SkipInvalidTopLevelSelector();
            }
            else if (selector != null
                && (!forgiving || !this.HasLexicalDiagnostic(
                    selector.GetSourceSpan())))
            {
                selectors.Add(selector);
            }

            this.SkipWhitespace();
            if (this.IsDelimiter(","))
            {
                HtmlSelectorToken comma = this.ConsumeCurrent();
                this.SkipWhitespace();
                if (this.Current().GetKind()
                    == HtmlSelectorTokenKind.End)
                {
                    this.Fail("A comma must be followed by a selector.",
                        comma);
                    if (forgiving)
                    {
                        this.failed = false;
                        complete = true;
                    }
                }
            }
            else if (this.Current().GetKind()
                == HtmlSelectorTokenKind.End)
            {
                complete = true;
            }
        }

        if (this.terminalFailure || selectors.Count == 0
            || (!forgiving && (this.failed
                || this.diagnostics.Count > 0)))
        {
            return this.Complete(null);
        }

        HtmlSourceSpan span = CreateSpan(
            selectors[0].GetSourceSpan().GetStart(),
            selectors[selectors.Count - 1].GetSourceSpan().GetEnd());
        return this.Complete(new HtmlSelectorListAst(selectors.ToArray(),
            span));
    }

    internal void SkipInvalidTopLevelSelector()
    {
        int parentheses = 0;
        int brackets = 0;
        bool complete = false;
        while (!complete
            && this.Current().GetKind() != HtmlSelectorTokenKind.End)
        {
            HtmlSelectorToken token = this.Current();
            if (token.GetKind() == HtmlSelectorTokenKind.Function)
            {
                parentheses = parentheses + 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter("["))
            {
                brackets = brackets + 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter("]") && brackets > 0)
            {
                brackets = brackets - 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter("("))
            {
                parentheses = parentheses + 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter(")") && parentheses > 0)
            {
                parentheses = parentheses - 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter(",") && parentheses == 0
                && brackets == 0)
            {
                complete = true;
            }
            else
            {
                this.ConsumeCurrent();
            }
        }
    }

    internal bool HasLexicalDiagnostic(HtmlSourceSpan span)
    {
        int start = span.GetStart().GetCharacterOffset();
        int end = span.GetEnd().GetCharacterOffset();
        int index = 0;
        while (index < this.lexicalDiagnosticCount)
        {
            int diagnosticOffset = this.diagnostics[index].GetSpan()
                .GetStart().GetCharacterOffset();
            if (diagnosticOffset >= start && diagnosticOffset < end)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal HtmlComplexSelectorAst? ParseComplexSelector()
    {
        List<HtmlCompoundSelectorAst> compounds =
            new List<HtmlCompoundSelectorAst>();
        List<HtmlSelectorCombinator> combinators =
            new List<HtmlSelectorCombinator>();
        HtmlCompoundSelectorAst? first = this.ParseCompound();
        if (first == null)
        {
            return null;
        }

        compounds.Add(first);
        bool reading = true;
        while (reading && !this.failed)
        {
            bool hadWhitespace = this.SkipWhitespace();
            if (this.IsSelectorListTerminator())
            {
                reading = false;
                continue;
            }

            if (this.IsColumnCombinator())
            {
                HtmlSelectorToken firstBar = this.ConsumeCurrent();
                HtmlSelectorToken secondBar = this.ConsumeCurrent();
                this.FailWithCode(HtmlDiagnosticCode.SelectorUnsupportedFeature,
                    ColumnCombinatorUnsupportedMessage, CreateSpan(
                        firstBar.GetSourceSpan().GetStart(),
                        secondBar.GetSourceSpan().GetEnd()));
                continue;
            }

            HtmlSelectorCombinator combinator =
                HtmlSelectorCombinator.Descendant;
            bool found = true;
            if (this.IsDelimiter(">"))
            {
                combinator = HtmlSelectorCombinator.Child;
                this.ConsumeCurrent();
                this.SkipWhitespace();
            }
            else if (this.IsDelimiter("+"))
            {
                combinator = HtmlSelectorCombinator.NextSibling;
                this.ConsumeCurrent();
                this.SkipWhitespace();
            }
            else if (this.IsDelimiter("~"))
            {
                combinator = HtmlSelectorCombinator.SubsequentSibling;
                this.ConsumeCurrent();
                this.SkipWhitespace();
            }
            else if (!hadWhitespace)
            {
                found = false;
            }

            if (!found)
            {
                reading = false;
                continue;
            }

            HtmlCompoundSelectorAst? compound = this.ParseCompound();
            if (compound == null)
            {
                continue;
            }

            if (this.ConsumeComplexity(compound.GetSourceSpan()))
            {
                combinators.Add(combinator);
                compounds.Add(compound);
            }
        }

        if (this.failed)
        {
            return null;
        }

        HtmlSourceSpan span = CreateSpan(
            compounds[0].GetSourceSpan().GetStart(),
            compounds[compounds.Count - 1].GetSourceSpan().GetEnd());
        return new HtmlComplexSelectorAst(compounds.ToArray(),
            combinators.ToArray(), span);
    }

    internal HtmlCompoundSelectorAst? ParseCompound()
    {
        List<HtmlSimpleSelectorAst> simpleSelectors =
            new List<HtmlSimpleSelectorAst>();
        HtmlSelectorToken first = this.Current();
        if (this.CanStartType())
        {
            HtmlSelectorQualifiedNameAst? type = this.ParseQualifiedName(true,
                "type selector");
            if (type == null)
            {
                return null;
            }

            this.AddSimpleSelector(simpleSelectors,
                HtmlSimpleSelectorAst.CreateType(type));
        }

        bool reading = true;
        while (reading && !this.failed)
        {
            HtmlSelectorToken current = this.Current();
            if (current.GetKind() == HtmlSelectorTokenKind.Hash)
            {
                this.ConsumeCurrent();
                if (!current.IsIdHash())
                {
                    this.Fail("An ID selector requires a CSS identifier.",
                        current);
                }
                else
                {
                    this.AddSimpleSelector(simpleSelectors,
                        HtmlSimpleSelectorAst.CreateId(current.GetValue(),
                            current.GetSourceSpan()));
                }
            }
            else if (this.IsDelimiter("."))
            {
                HtmlSelectorToken dot = this.ConsumeCurrent();
                HtmlSelectorToken name = this.Current();
                if (name.GetKind() != HtmlSelectorTokenKind.Identifier)
                {
                    this.Fail("A class selector requires an identifier.",
                        name.GetKind() == HtmlSelectorTokenKind.End
                            ? dot
                            : name);
                }
                else
                {
                    this.ConsumeCurrent();
                    this.AddSimpleSelector(simpleSelectors,
                        HtmlSimpleSelectorAst.CreateClass(name.GetValue(),
                            CreateSpan(dot.GetSourceSpan().GetStart(),
                                name.GetSourceSpan().GetEnd())));
                }
            }
            else if (this.IsDelimiter("["))
            {
                HtmlSimpleSelectorAst? attribute = this.ParseAttribute();
                if (attribute != null)
                {
                    this.AddSimpleSelector(simpleSelectors, attribute);
                }
            }
            else if (this.IsDelimiter(":"))
            {
                HtmlSimpleSelectorAst? pseudoClass = this.ParsePseudoClass();
                if (pseudoClass != null)
                {
                    this.AddSimpleSelector(simpleSelectors, pseudoClass);
                }
            }
            else
            {
                reading = false;
            }
        }

        if (this.failed)
        {
            return null;
        }

        if (simpleSelectors.Count == 0)
        {
            this.Fail("Expected a compound selector.", first);
            return null;
        }

        HtmlSourceSpan span = CreateSpan(
            simpleSelectors[0].GetSourceSpan().GetStart(),
            simpleSelectors[simpleSelectors.Count - 1]
                .GetSourceSpan().GetEnd());
        return new HtmlCompoundSelectorAst(simpleSelectors.ToArray(), span);
    }

    internal HtmlSimpleSelectorAst? ParsePseudoClass()
    {
        HtmlSelectorToken colon = this.ConsumeCurrent();
        HtmlSelectorToken name = this.Current();
        if (this.IsDelimiter(":"))
        {
            this.FailWithCode(HtmlDiagnosticCode.SelectorUnsupportedFeature,
                PseudoElementUnsupportedMessage, CreateSpan(
                    colon.GetSourceSpan().GetStart(),
                    name.GetSourceSpan().GetEnd()));
            return null;
        }

        if (name.GetKind() == HtmlSelectorTokenKind.Identifier)
        {
            HtmlSelectorPseudoClassKind pseudoClassKind =
                HtmlSelectorPseudoClassKind.Root;
            bool supported = true;
            if (IsAsciiName(name.GetValue(), "root"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Root;
            }
            else if (IsAsciiName(name.GetValue(), "scope"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Scope;
            }
            else if (IsAsciiName(name.GetValue(), "empty"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Empty;
            }
            else if (IsAsciiName(name.GetValue(), "first-child"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.FirstChild;
            }
            else if (IsAsciiName(name.GetValue(), "last-child"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.LastChild;
            }
            else if (IsAsciiName(name.GetValue(), "only-child"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.OnlyChild;
            }
            else if (IsAsciiName(name.GetValue(), "first-of-type"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.FirstOfType;
            }
            else if (IsAsciiName(name.GetValue(), "last-of-type"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.LastOfType;
            }
            else if (IsAsciiName(name.GetValue(), "only-of-type"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.OnlyOfType;
            }
            else if (IsAsciiName(name.GetValue(), "any-link"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.AnyLink;
            }
            else if (IsAsciiName(name.GetValue(), "link"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Link;
            }
            else if (IsAsciiName(name.GetValue(), "enabled"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Enabled;
            }
            else if (IsAsciiName(name.GetValue(), "disabled"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Disabled;
            }
            else if (IsAsciiName(name.GetValue(), "checked"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Checked;
            }
            else if (IsAsciiName(name.GetValue(), "indeterminate"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Indeterminate;
            }
            else if (IsAsciiName(name.GetValue(), "required"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Required;
            }
            else if (IsAsciiName(name.GetValue(), "optional"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Optional;
            }
            else if (IsAsciiName(name.GetValue(), "read-only"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.ReadOnly;
            }
            else if (IsAsciiName(name.GetValue(), "read-write"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.ReadWrite;
            }
            else if (IsAsciiName(name.GetValue(), "placeholder-shown"))
            {
                pseudoClassKind =
                    HtmlSelectorPseudoClassKind.PlaceholderShown;
            }
            else if (IsAsciiName(name.GetValue(), "default"))
            {
                pseudoClassKind = HtmlSelectorPseudoClassKind.Default;
            }
            else if (IsLegacyPseudoElement(name.GetValue()))
            {
                this.FailWithCode(HtmlDiagnosticCode.SelectorUnsupportedFeature,
                    PseudoElementUnsupportedMessage, CreateSpan(
                        colon.GetSourceSpan().GetStart(),
                        name.GetSourceSpan().GetEnd()));
                return null;
            }
            else if (IsUnsupportedStatePseudoClass(name.GetValue()))
            {
                this.FailWithCode(HtmlDiagnosticCode.SelectorUnsupportedFeature,
                    StatePseudoClassUnsupportedMessage,
                    name.GetSourceSpan());
                return null;
            }
            else
            {
                supported = false;
            }

            if (!supported)
            {
                this.Fail("Unsupported or unknown pseudo-class.", name);
                return null;
            }

            this.ConsumeCurrent();
            return HtmlSimpleSelectorAst.CreatePseudoClass(pseudoClassKind,
                null, CreateSpan(colon.GetSourceSpan().GetStart(),
                    name.GetSourceSpan().GetEnd()));
        }

        if (name.GetKind() != HtmlSelectorTokenKind.Function)
        {
            this.Fail("A pseudo-class requires a CSS identifier or "
                + "function.", name.GetKind() == HtmlSelectorTokenKind.End
                    ? colon
                    : name);
            return null;
        }

        if (IsAsciiName(name.GetValue(), "lang")
            || IsAsciiName(name.GetValue(), "dir"))
        {
            HtmlSelectorPseudoClassKind argumentKind =
                IsAsciiName(name.GetValue(), "lang")
                    ? HtmlSelectorPseudoClassKind.Lang
                    : HtmlSelectorPseudoClassKind.Dir;
            return this.ParseArgumentPseudoClass(colon, name, argumentKind);
        }

        if (IsAsciiName(name.GetValue(), "has"))
        {
            if (this.hasNesting > 0)
            {
                this.Fail("The :has() pseudo-class cannot be nested.", name);
                return null;
            }

            this.ConsumeCurrent();
            this.hasNesting = this.hasNesting + 1;
            HtmlRelativeSelectorAst[]? relativeArguments =
                this.ParseNestedRelativeSelectorList(name);
            this.hasNesting = this.hasNesting - 1;
            if (relativeArguments == null)
            {
                return null;
            }

            HtmlSelectorToken hasClosing = this.Current();
            this.ConsumeCurrent();
            return HtmlSimpleSelectorAst.CreateHasPseudoClass(
                relativeArguments, CreateSpan(
                    colon.GetSourceSpan().GetStart(),
                    hasClosing.GetSourceSpan().GetEnd()));
        }

        HtmlSelectorPseudoClassKind logicalKind =
            HtmlSelectorPseudoClassKind.Is;
        bool logical = true;
        if (IsAsciiName(name.GetValue(), "is"))
        {
            logicalKind = HtmlSelectorPseudoClassKind.Is;
        }
        else if (IsAsciiName(name.GetValue(), "where"))
        {
            logicalKind = HtmlSelectorPseudoClassKind.Where;
        }
        else if (IsAsciiName(name.GetValue(), "not"))
        {
            logicalKind = HtmlSelectorPseudoClassKind.Not;
        }
        else
        {
            logical = false;
        }

        if (logical)
        {
            this.ConsumeCurrent();
            bool forgiving = logicalKind != HtmlSelectorPseudoClassKind.Not;
            HtmlComplexSelectorAst[]? arguments =
                this.ParseNestedSelectorList(forgiving, name);
            if (arguments == null)
            {
                return null;
            }

            HtmlSelectorToken logicalClosing = this.Current();
            this.ConsumeCurrent();
            return HtmlSimpleSelectorAst.CreateLogicalPseudoClass(
                logicalKind, arguments, CreateSpan(
                    colon.GetSourceSpan().GetStart(),
                    logicalClosing.GetSourceSpan().GetEnd()));
        }

        HtmlSelectorPseudoClassKind functionalKind =
            HtmlSelectorPseudoClassKind.NthChild;
        bool functionalSupported = true;
        if (IsAsciiName(name.GetValue(), "nth-child"))
        {
            functionalKind = HtmlSelectorPseudoClassKind.NthChild;
        }
        else if (IsAsciiName(name.GetValue(), "nth-last-child"))
        {
            functionalKind = HtmlSelectorPseudoClassKind.NthLastChild;
        }
        else if (IsAsciiName(name.GetValue(), "nth-of-type"))
        {
            functionalKind = HtmlSelectorPseudoClassKind.NthOfType;
        }
        else if (IsAsciiName(name.GetValue(), "nth-last-of-type"))
        {
            functionalKind = HtmlSelectorPseudoClassKind.NthLastOfType;
        }
        else
        {
            functionalSupported = false;
        }

        if (!functionalSupported)
        {
            if (IsUnsupportedStatePseudoClass(name.GetValue()))
            {
                this.FailWithCode(HtmlDiagnosticCode.SelectorUnsupportedFeature,
                    StatePseudoClassUnsupportedMessage,
                    name.GetSourceSpan());
                return null;
            }

            this.Fail("Unsupported or unknown functional pseudo-class.",
                name);
            return null;
        }

        this.ConsumeCurrent();
        StringBuilder argument = new StringBuilder();
        while (!this.failed
            && this.Current().GetKind() != HtmlSelectorTokenKind.End
            && !this.IsDelimiter(")"))
        {
            HtmlSelectorToken token = this.ConsumeCurrent();
            if (token.GetKind() == HtmlSelectorTokenKind.Whitespace)
            {
                argument.Append(' ');
            }
            else if (token.GetKind() == HtmlSelectorTokenKind.Identifier)
            {
                argument.Append(token.GetValue());
            }
            else if (token.GetKind() == HtmlSelectorTokenKind.Number)
            {
                argument.Append(token.GetNumberRepresentation());
            }
            else if (token.GetKind() == HtmlSelectorTokenKind.Dimension)
            {
                argument.Append(token.GetNumberRepresentation());
                argument.Append(token.GetUnit());
            }
            else if (token.GetKind() == HtmlSelectorTokenKind.Delimiter)
            {
                argument.Append(token.GetValue());
            }
            else
            {
                this.Fail("An nth pseudo-class requires an An+B "
                    + "expression.", token);
            }
        }

        if (this.failed)
        {
            return null;
        }

        HtmlSelectorToken closing = this.Current();
        if (!this.IsDelimiter(")"))
        {
            this.Fail("An nth pseudo-class must end with ')'.", name);
            return null;
        }

        HtmlNthExpressionParseResult parsed = HtmlNthExpression.Parse(
            argument.ToString());
        if (!parsed.IsSuccess())
        {
            this.Fail(parsed.WasOverflow()
                    ? "An nth pseudo-class coefficient is out of range."
                    : "An nth pseudo-class requires a valid An+B expression.",
                name);
            return null;
        }

        this.ConsumeCurrent();
        return HtmlSimpleSelectorAst.CreatePseudoClass(functionalKind,
            parsed.GetExpression(), CreateSpan(
                colon.GetSourceSpan().GetStart(),
                closing.GetSourceSpan().GetEnd()));
    }

    internal HtmlSimpleSelectorAst? ParseArgumentPseudoClass(HtmlSelectorToken colon,
        HtmlSelectorToken function,
        HtmlSelectorPseudoClassKind pseudoClassKind)
    {
        this.ConsumeCurrent();
        List<string> arguments = new List<string>();
        bool expectingArgument = true;
        bool complete = false;
        while (!complete && !this.failed)
        {
            this.SkipWhitespace();
            HtmlSelectorToken current = this.Current();
            if (current.GetKind() == HtmlSelectorTokenKind.End)
            {
                this.Fail("The functional pseudo-class must end with ')'.",
                    function);
            }
            else if (this.IsDelimiter(")"))
            {
                if (expectingArgument)
                {
                    this.Fail("The functional pseudo-class requires an "
                        + "argument.", current);
                }
                else
                {
                    complete = true;
                }
            }
            else if (current.GetKind() != HtmlSelectorTokenKind.Identifier
                && (pseudoClassKind != HtmlSelectorPseudoClassKind.Lang
                    || current.GetKind()
                        != HtmlSelectorTokenKind.String))
            {
                this.Fail("Expected a pseudo-class identifier or string.",
                    current);
            }
            else
            {
                arguments.Add(current.GetValue());
                this.ConsumeCurrent();
                this.SkipWhitespace();
                expectingArgument = false;
                if (this.IsDelimiter(",")
                    && pseudoClassKind == HtmlSelectorPseudoClassKind.Lang)
                {
                    this.ConsumeCurrent();
                    expectingArgument = true;
                }
                else if (this.IsDelimiter(")"))
                {
                    complete = true;
                }
                else
                {
                    this.Fail("Unexpected token in a functional "
                        + "pseudo-class.", this.Current());
                }
            }
        }

        if (this.failed)
        {
            return null;
        }

        HtmlSelectorToken closing = this.Current();
        this.ConsumeCurrent();
        return HtmlSimpleSelectorAst.CreateArgumentPseudoClass(
            pseudoClassKind, arguments.ToArray(), CreateSpan(
                colon.GetSourceSpan().GetStart(),
                closing.GetSourceSpan().GetEnd()));
    }

    internal HtmlComplexSelectorAst[]? ParseNestedSelectorList(bool forgiving,
        HtmlSelectorToken function)
    {
        if (this.selectorListNesting >= MaximumLogicalNesting)
        {
            this.FailComplexity(
                "The selector exceeds the logical nesting limit.",
                function.GetSourceSpan());
            return null;
        }

        this.selectorListNesting = this.selectorListNesting + 1;
        List<HtmlComplexSelectorAst> selectors =
            new List<HtmlComplexSelectorAst>();
        bool expectingSelector = true;
        bool complete = false;
        while (!complete && !this.failed)
        {
            this.SkipWhitespace();
            if (this.Current().GetKind() == HtmlSelectorTokenKind.End)
            {
                this.Fail("A logical pseudo-class must end with ')'.",
                    function);
                continue;
            }

            if (this.IsDelimiter(")"))
            {
                if (!forgiving && expectingSelector)
                {
                    this.Fail("A strict selector list cannot contain an "
                        + "empty selector.", this.Current());
                }
                else
                {
                    complete = true;
                }

                continue;
            }

            if (this.IsDelimiter(","))
            {
                if (!forgiving)
                {
                    this.Fail("A strict selector list cannot contain an "
                        + "empty selector.", this.Current());
                }
                else
                {
                    if (this.ConsumeComplexity(
                        this.Current().GetSourceSpan()))
                    {
                        this.ConsumeCurrent();
                        expectingSelector = true;
                    }
                }

                continue;
            }

            int branchStart = this.position;
            int diagnosticStart = this.diagnostics.Count;
            if (!this.ConsumeComplexity(this.Current().GetSourceSpan()))
            {
                continue;
            }

            HtmlComplexSelectorAst? selector = this.ParseComplexSelector();
            this.SkipWhitespace();
            if (!this.failed && !this.IsDelimiter(",")
                && !this.IsDelimiter(")"))
            {
                this.Fail("Unexpected token in a nested selector list.",
                    this.Current());
            }

            if (this.failed)
            {
                if (!forgiving || this.terminalFailure)
                {
                    this.selectorListNesting =
                        this.selectorListNesting - 1;
                    return null;
                }

                this.RemoveDiagnosticsFrom(diagnosticStart);
                this.failed = false;
                this.position = branchStart;
                this.SkipInvalidNestedSelector();
            }
            else if (selector != null)
            {
                selectors.Add(selector);
            }

            this.SkipWhitespace();
            expectingSelector = false;
            if (this.IsDelimiter(","))
            {
                this.ConsumeCurrent();
                expectingSelector = true;
            }
            else if (this.IsDelimiter(")"))
            {
                complete = true;
            }
        }

        this.selectorListNesting = this.selectorListNesting - 1;
        if (this.failed)
        {
            return null;
        }

        if (!forgiving && selectors.Count == 0)
        {
            this.Fail("A strict selector list cannot be empty.", function);
            return null;
        }

        return selectors.ToArray();
    }

    internal HtmlRelativeSelectorAst[]? ParseNestedRelativeSelectorList(
        HtmlSelectorToken function)
    {
        if (this.selectorListNesting >= MaximumLogicalNesting)
        {
            this.FailComplexity(
                "The selector exceeds the nesting limit.",
                function.GetSourceSpan());
            return null;
        }

        this.selectorListNesting = this.selectorListNesting + 1;
        List<HtmlRelativeSelectorAst> selectors =
            new List<HtmlRelativeSelectorAst>();
        bool expectingSelector = true;
        bool complete = false;
        while (!complete && !this.failed)
        {
            this.SkipWhitespace();
            if (this.Current().GetKind() == HtmlSelectorTokenKind.End)
            {
                this.Fail("The :has() pseudo-class must end with ')'.",
                    function);
            }
            else if (this.IsDelimiter(")"))
            {
                if (expectingSelector)
                {
                    this.Fail("A relative selector list cannot contain an "
                        + "empty selector.", this.Current());
                }
                else
                {
                    complete = true;
                }
            }
            else if (this.IsDelimiter(","))
            {
                this.Fail("A relative selector list cannot contain an "
                    + "empty selector.", this.Current());
            }
            else if (this.ConsumeComplexity(
                this.Current().GetSourceSpan()))
            {
                HtmlRelativeSelectorAst? selector =
                    this.ParseRelativeSelector();
                if (selector != null)
                {
                    selectors.Add(selector);
                }

                this.SkipWhitespace();
                expectingSelector = false;
                if (!this.failed && this.IsDelimiter(","))
                {
                    this.ConsumeCurrent();
                    expectingSelector = true;
                }
                else if (!this.failed && this.IsDelimiter(")"))
                {
                    complete = true;
                }
                else if (!this.failed)
                {
                    this.Fail("Unexpected token in a relative selector "
                        + "list.", this.Current());
                }
            }
        }

        this.selectorListNesting = this.selectorListNesting - 1;
        if (this.failed)
        {
            return null;
        }

        if (selectors.Count == 0)
        {
            this.Fail("The :has() pseudo-class requires a relative "
                + "selector.", function);
            return null;
        }

        return selectors.ToArray();
    }

    internal HtmlRelativeSelectorAst? ParseRelativeSelector()
    {
        HtmlSelectorToken first = this.Current();
        HtmlSelectorCombinator leadingCombinator =
            HtmlSelectorCombinator.Descendant;
        if (this.IsColumnCombinator())
        {
            HtmlSelectorToken firstBar = this.ConsumeCurrent();
            HtmlSelectorToken secondBar = this.ConsumeCurrent();
            this.FailWithCode(HtmlDiagnosticCode.SelectorUnsupportedFeature,
                ColumnCombinatorUnsupportedMessage, CreateSpan(
                    firstBar.GetSourceSpan().GetStart(),
                    secondBar.GetSourceSpan().GetEnd()));
            return null;
        }

        if (this.IsDelimiter(">"))
        {
            leadingCombinator = HtmlSelectorCombinator.Child;
            this.ConsumeCurrent();
            this.SkipWhitespace();
        }
        else if (this.IsDelimiter("+"))
        {
            leadingCombinator = HtmlSelectorCombinator.NextSibling;
            this.ConsumeCurrent();
            this.SkipWhitespace();
        }
        else if (this.IsDelimiter("~"))
        {
            leadingCombinator = HtmlSelectorCombinator.SubsequentSibling;
            this.ConsumeCurrent();
            this.SkipWhitespace();
        }

        if (!this.ConsumeComplexity(first.GetSourceSpan()))
        {
            return null;
        }

        HtmlComplexSelectorAst? selector = this.ParseComplexSelector();
        if (selector == null)
        {
            return null;
        }

        return new HtmlRelativeSelectorAst(leadingCombinator, selector,
            CreateSpan(first.GetSourceSpan().GetStart(),
                selector.GetSourceSpan().GetEnd()));
    }

    internal void SkipInvalidNestedSelector()
    {
        int parentheses = 0;
        int brackets = 0;
        bool complete = false;
        while (!complete
            && this.Current().GetKind() != HtmlSelectorTokenKind.End)
        {
            HtmlSelectorToken token = this.Current();
            if (token.GetKind() == HtmlSelectorTokenKind.Function)
            {
                parentheses = parentheses + 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter("["))
            {
                brackets = brackets + 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter("]") && brackets > 0)
            {
                brackets = brackets - 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter("("))
            {
                parentheses = parentheses + 1;
                this.ConsumeCurrent();
            }
            else if (this.IsDelimiter(")"))
            {
                if (parentheses > 0)
                {
                    parentheses = parentheses - 1;
                    this.ConsumeCurrent();
                }
                else if (brackets == 0)
                {
                    complete = true;
                }
                else
                {
                    this.ConsumeCurrent();
                }
            }
            else if (this.IsDelimiter(",") && parentheses == 0
                && brackets == 0)
            {
                complete = true;
            }
            else
            {
                this.ConsumeCurrent();
            }
        }
    }

    internal HtmlSimpleSelectorAst? ParseAttribute()
    {
        HtmlSelectorToken opening = this.ConsumeCurrent();
        this.SkipWhitespace();
        HtmlSelectorQualifiedNameAst? name = this.ParseQualifiedName(false,
            "attribute selector");
        if (name == null)
        {
            return null;
        }

        this.SkipWhitespace();
        if (this.IsDelimiter("]"))
        {
            HtmlSelectorToken closing = this.ConsumeCurrent();
            HtmlSourceSpan presenceSpan = CreateSpan(
                opening.GetSourceSpan().GetStart(),
                closing.GetSourceSpan().GetEnd());
            return HtmlSimpleSelectorAst.CreateAttribute(name,
                HtmlSelectorAttributeOperator.Exists, "",
                HtmlSelectorAttributeCase.Default, presenceSpan);
        }

        if (this.Current().GetKind() == HtmlSelectorTokenKind.End)
        {
            HtmlSourceSpan presenceSpan = CreateSpan(
                opening.GetSourceSpan().GetStart(),
                this.Current().GetSourceSpan().GetEnd());
            return HtmlSimpleSelectorAst.CreateAttribute(name,
                HtmlSelectorAttributeOperator.Exists, "",
                HtmlSelectorAttributeCase.Default, presenceSpan);
        }

        HtmlSelectorAttributeOperator attributeOperator =
            this.ParseAttributeOperator();
        if (this.failed)
        {
            return null;
        }

        this.SkipWhitespace();
        HtmlSelectorToken value = this.Current();
        if (value.GetKind() != HtmlSelectorTokenKind.Identifier
            && value.GetKind() != HtmlSelectorTokenKind.String)
        {
            this.Fail("An attribute selector operator must be followed by "
                + "an identifier or string.", value);
            return null;
        }

        this.ConsumeCurrent();
        this.SkipWhitespace();
        HtmlSelectorAttributeCase attributeCase =
            HtmlSelectorAttributeCase.Default;
        HtmlSelectorToken modifier = this.Current();
        if (modifier.GetKind() == HtmlSelectorTokenKind.Identifier)
        {
            if (Ascii.EqualsIgnoreCase(modifier.GetValue().AsSpan(),
                "i".AsSpan()))
            {
                attributeCase = HtmlSelectorAttributeCase.Insensitive;
            }
            else if (Ascii.EqualsIgnoreCase(modifier.GetValue().AsSpan(),
                "s".AsSpan()))
            {
                attributeCase = HtmlSelectorAttributeCase.Sensitive;
            }
            else
            {
                this.Fail("An attribute selector modifier must be 'i' or "
                    + "'s'.", modifier);
                return null;
            }

            this.ConsumeCurrent();
            this.SkipWhitespace();
        }

        HtmlSelectorToken ending = this.Current();
        if (ending.GetKind() != HtmlSelectorTokenKind.End
            && !this.IsDelimiter("]"))
        {
            this.Fail("An attribute selector must end with ']'.",
                ending);
            return null;
        }

        if (ending.GetKind() != HtmlSelectorTokenKind.End)
        {
            this.ConsumeCurrent();
        }

        HtmlSourceSpan span = CreateSpan(
            opening.GetSourceSpan().GetStart(),
            ending.GetSourceSpan().GetEnd());
        return HtmlSimpleSelectorAst.CreateAttribute(name,
            attributeOperator, value.GetValue(), attributeCase, span);
    }

    internal HtmlSelectorAttributeOperator ParseAttributeOperator()
    {
        if (this.IsDelimiter("="))
        {
            this.ConsumeCurrent();
            return HtmlSelectorAttributeOperator.Equals;
        }

        HtmlSelectorToken first = this.Current();
        HtmlSelectorAttributeOperator result =
            HtmlSelectorAttributeOperator.Exists;
        bool supported = true;
        if (this.IsDelimiter("~"))
        {
            result = HtmlSelectorAttributeOperator.Includes;
        }
        else if (this.IsDelimiter("|"))
        {
            result = HtmlSelectorAttributeOperator.DashMatch;
        }
        else if (this.IsDelimiter("^"))
        {
            result = HtmlSelectorAttributeOperator.PrefixMatch;
        }
        else if (this.IsDelimiter("$"))
        {
            result = HtmlSelectorAttributeOperator.SuffixMatch;
        }
        else if (this.IsDelimiter("*"))
        {
            result = HtmlSelectorAttributeOperator.SubstringMatch;
        }
        else
        {
            supported = false;
        }

        if (!supported || !this.IsDelimiterAt(1, "="))
        {
            this.Fail("An attribute selector requires a supported operator.",
                first);
            return HtmlSelectorAttributeOperator.Exists;
        }

        this.ConsumeCurrent();
        this.ConsumeCurrent();
        return result;
    }

    internal HtmlSelectorQualifiedNameAst? ParseQualifiedName(bool allowUniversal,
        string description)
    {
        HtmlSelectorToken first = this.Current();
        HtmlSelectorNamespaceKind namespaceKind =
            HtmlSelectorNamespaceKind.Unspecified;
        string prefix = "";
        HtmlSelectorToken? local = null;
        bool universal = false;
        if (first.GetKind() == HtmlSelectorTokenKind.Identifier)
        {
            this.ConsumeCurrent();
            if (this.IsDelimiter("|") && !this.IsColumnCombinator()
                && (allowUniversal || !this.IsDelimiterAt(1, "=")))
            {
                namespaceKind = HtmlSelectorNamespaceKind.Prefix;
                prefix = first.GetValue();
                HtmlSelectorToken separator = this.ConsumeCurrent();
                local = this.TakeQualifiedLocalName(allowUniversal,
                    description, separator);
            }
            else
            {
                local = first;
            }
        }
        else if (this.IsDelimiter("*"))
        {
            this.ConsumeCurrent();
            if (this.IsDelimiter("|") && !this.IsColumnCombinator())
            {
                namespaceKind = HtmlSelectorNamespaceKind.Any;
                HtmlSelectorToken separator = this.ConsumeCurrent();
                local = this.TakeQualifiedLocalName(allowUniversal,
                    description, separator);
            }
            else if (allowUniversal)
            {
                local = first;
                universal = true;
            }
            else
            {
                this.Fail("An attribute name cannot be universal.", first);
            }
        }
        else if (this.IsDelimiter("|"))
        {
            namespaceKind = HtmlSelectorNamespaceKind.None;
            HtmlSelectorToken separator = this.ConsumeCurrent();
            local = this.TakeQualifiedLocalName(allowUniversal, description,
                separator);
        }
        else
        {
            this.Fail("Expected a qualified name in the " + description
                + ".", first);
        }

        if (this.failed || local == null)
        {
            return null;
        }

        universal = local.GetKind() == HtmlSelectorTokenKind.Delimiter
            && local.GetValue() == "*";
        string localName = universal ? "" : local.GetValue();
        return new HtmlSelectorQualifiedNameAst(namespaceKind, prefix,
            localName, universal, CreateSpan(
                first.GetSourceSpan().GetStart(),
                local.GetSourceSpan().GetEnd()));
    }

    internal HtmlSelectorToken? TakeQualifiedLocalName(bool allowUniversal,
        string description, HtmlSelectorToken separator)
    {
        HtmlSelectorToken local = this.Current();
        if (local.GetKind() == HtmlSelectorTokenKind.Identifier)
        {
            return this.ConsumeCurrent();
        }

        if (allowUniversal && this.IsDelimiter("*"))
        {
            return this.ConsumeCurrent();
        }

        this.Fail("A namespace separator must be followed by a valid "
            + description + " name.",
            local.GetKind() == HtmlSelectorTokenKind.End ? separator : local);
        return null;
    }

    internal bool CanStartType()
    {
        HtmlSelectorToken current = this.Current();
        if (current.GetKind() == HtmlSelectorTokenKind.Identifier
            || this.IsDelimiter("*"))
        {
            return true;
        }

        if (!this.IsDelimiter("|"))
        {
            return false;
        }

        HtmlSelectorToken next = this.Peek(1);
        return next.GetKind() == HtmlSelectorTokenKind.Identifier
            || next.GetKind() == HtmlSelectorTokenKind.Delimiter
                && next.GetValue() == "*";
    }

    internal void AddSimpleSelector(List<HtmlSimpleSelectorAst> simpleSelectors,
        HtmlSimpleSelectorAst simpleSelector)
    {
        if (this.ConsumeComplexity(simpleSelector.GetSourceSpan()))
        {
            simpleSelectors.Add(simpleSelector);
        }
    }

    internal bool ConsumeComplexity(HtmlSourceSpan span)
    {
        if (this.complexity >= this.maximumComplexity)
        {
            this.FailComplexity("The selector exceeds the configured "
                + "complexity limit.", span);
            return false;
        }

        this.complexity = this.complexity + 1;
        return true;
    }

    internal void FailComplexity(string message, HtmlSourceSpan span)
    {
        this.terminalFailure = true;
        this.FailWithCode(HtmlDiagnosticCode.SelectorComplexityExceeded, message,
            span);
    }

    internal void RemoveDiagnosticsFrom(int start)
    {
        while (this.diagnostics.Count > start)
        {
            this.diagnostics.RemoveAt(this.diagnostics.Count - 1);
        }
    }

    internal bool IsSelectorListTerminator()
    {
        return this.Current().GetKind() == HtmlSelectorTokenKind.End
            || this.IsDelimiter(",")
            || this.selectorListNesting > 0 && this.IsDelimiter(")");
    }

    internal bool SkipWhitespace()
    {
        bool found = false;
        while (this.Current().GetKind() == HtmlSelectorTokenKind.Whitespace)
        {
            found = true;
            this.position = this.position + 1;
        }

        return found;
    }

    internal bool IsColumnCombinator()
    {
        HtmlSelectorToken next = this.Peek(1);
        return this.IsDelimiter("|")
            && next.GetKind() == HtmlSelectorTokenKind.Delimiter
            && next.GetValue() == "|";
    }

    internal bool IsDelimiter(string value)
    {
        return this.IsDelimiterAt(0, value);
    }

    internal bool IsDelimiterAt(int offset, string value)
    {
        HtmlSelectorToken token = this.Peek(offset);
        return token.GetKind() == HtmlSelectorTokenKind.Delimiter
            && token.GetValue() == value;
    }

    internal HtmlSelectorToken ConsumeCurrent()
    {
        HtmlSelectorToken result = this.Current();
        if (result.GetKind() != HtmlSelectorTokenKind.End)
        {
            this.position = this.position + 1;
        }

        return result;
    }

    internal HtmlSelectorToken Current()
    {
        return this.Peek(0);
    }

    internal HtmlSelectorToken Peek(int offset)
    {
        int index = this.position + offset;
        return index < this.tokens.Length
            ? this.tokens[index]
            : this.tokens[this.tokens.Length - 1];
    }

    internal void Fail(string message, HtmlSelectorToken token)
    {
        this.FailWithCode(HtmlDiagnosticCode.SelectorInvalidSyntax, message,
            token.GetSourceSpan());
    }

    internal void FailWithCode(HtmlDiagnosticCode code, string message, HtmlSourceSpan span)
    {
        if (this.failed)
        {
            return;
        }

        this.failed = true;
        this.diagnostics.Add(HtmlDiagnostic.Create(
            code, HtmlDiagnosticSeverity.Error, message, span));
    }

    internal HtmlSelectorParseResult Complete(HtmlSelectorListAst? selectorList)
    {
        this.result = new HtmlSelectorParseResult(selectorList,
            this.diagnostics.ToArray());
        return this.result;
    }

    internal static HtmlSourceSpan CreateSpan(HtmlSourcePosition start,
        HtmlSourcePosition end)
    {
        return HtmlSourceSpan.Create(start, end);
    }

    internal static bool IsAsciiName(string value, string expected)
    {
        return Ascii.EqualsIgnoreCase(value.AsSpan(), expected.AsSpan());
    }

    internal static bool IsLegacyPseudoElement(string value)
    {
        return IsAsciiName(value, "before")
            || IsAsciiName(value, "after")
            || IsAsciiName(value, "first-letter")
            || IsAsciiName(value, "first-line");
    }

    internal static bool IsUnsupportedStatePseudoClass(string value)
    {
        return IsAsciiName(value, "active")
            || IsAsciiName(value, "autofill")
            || IsAsciiName(value, "buffering")
            || IsAsciiName(value, "current")
            || IsAsciiName(value, "defined")
            || IsAsciiName(value, "focus")
            || IsAsciiName(value, "focus-visible")
            || IsAsciiName(value, "focus-within")
            || IsAsciiName(value, "fullscreen")
            || IsAsciiName(value, "future")
            || IsAsciiName(value, "host")
            || IsAsciiName(value, "hover")
            || IsAsciiName(value, "in-range")
            || IsAsciiName(value, "invalid")
            || IsAsciiName(value, "local-link")
            || IsAsciiName(value, "modal")
            || IsAsciiName(value, "muted")
            || IsAsciiName(value, "open")
            || IsAsciiName(value, "out-of-range")
            || IsAsciiName(value, "past")
            || IsAsciiName(value, "paused")
            || IsAsciiName(value, "picture-in-picture")
            || IsAsciiName(value, "playing")
            || IsAsciiName(value, "popover-open")
            || IsAsciiName(value, "seeking")
            || IsAsciiName(value, "stalled")
            || IsAsciiName(value, "state")
            || IsAsciiName(value, "target")
            || IsAsciiName(value, "target-within")
            || IsAsciiName(value, "user-invalid")
            || IsAsciiName(value, "user-valid")
            || IsAsciiName(value, "valid")
            || IsAsciiName(value, "visited")
            || IsAsciiName(value, "volume-locked");
    }
}
