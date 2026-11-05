namespace svg;

/// <summary>Resolves bounded CSS <c>var()</c> substitutions with fallbacks and cycle detection.</summary>
public class SvgCssVariableResolver : ISvgCssVariableResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCssVariableResolver()
    {
    }

    /// <summary>Resolves custom properties in one component-value string.</summary>
    /// <param name="properties">The custom-property table.</param>
    /// <param name="value">The component-value text.</param>
    /// <param name="maxDepth">The maximum nested variable depth.</param>
    /// <param name="maxExpansionTokens">The maximum expanded token count.</param>
    /// <param name="maxTokenCharacters">The maximum individual token length.</param>
    /// <returns>Substituted text, dependencies, or a stable failure.</returns>
    public SvgCssVariableResolveResult Resolve(SvgCustomPropertySet properties, string value, int maxDepth, int maxExpansionTokens, int maxTokenCharacters)
    {
        System.Collections.Generic.List<string> dependencies = new System.Collections.Generic.List<string>();
        if (properties == null || value == null || maxDepth < 1 || maxExpansionTokens < 1 || maxTokenCharacters < 1) return new SvgCssVariableResolveResult(false, SvgCssVariableError.InvalidArgument, "", dependencies, 0, 0);
        System.Collections.Generic.List<string> stack = new System.Collections.Generic.List<string>();
        int maximumCharacters = new SvgCssVariableResolver().MaximumCharacters(maxExpansionTokens, maxTokenCharacters);
        SvgCssVariableResolveWork work = new SvgCssVariableResolver().ResolveText(properties, value, stack, dependencies, 0, maxDepth, maximumCharacters);
        if (!work.Success) return new SvgCssVariableResolveResult(false, work.Error, "", dependencies, work.ErrorOffset, 0);
        SvgCustomPropertyStoreResult tokenized = SvgCssVariableTokenizer.Tokenize("--resolved", work.Text, maxExpansionTokens, maxTokenCharacters);
        if (!tokenized.Success)
        {
            SvgCssVariableError error = tokenized.Error == SvgCssVariableError.TokenLimitExceeded ? SvgCssVariableError.ExpansionLimitExceeded : tokenized.Error;
            return new SvgCssVariableResolveResult(false, error, "", dependencies, tokenized.ErrorOffset, 0);
        }
        return new SvgCssVariableResolveResult(true, SvgCssVariableError.None, work.Text, dependencies, -1, (tokenized.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Tokens.Count);
    }
SvgCssVariableResolveWork ResolveText(SvgCustomPropertySet properties, string text, System.Collections.Generic.List<string> stack, System.Collections.Generic.List<string> dependencies, int depth, int maximumDepth, int maximumCharacters)
    {
        if (depth > maximumDepth) return new SvgCssVariableResolver().WorkFailure(SvgCssVariableError.DepthExceeded, 0);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int position = 0;
        while (position < text.Length)
        {
            int function = new SvgCssVariableResolver().FindVariableFunction(text, position);
            if (function < 0)
            {
                builder.Append(text.Substring(position));
                if (builder.Length > maximumCharacters) return new SvgCssVariableResolver().WorkFailure(SvgCssVariableError.ExpansionLimitExceeded, position);
                break;
            }
            builder.Append(text.Substring(position, function - position));
            int close = new SvgCssVariableResolver().FindMatchingParenthesis(text, function + 3);
            if (close < 0) return new SvgCssVariableResolver().WorkFailure(SvgCssVariableError.InvalidFunction, function);
            int comma = new SvgCssVariableResolver().FindFallbackComma(text, function + 4, close);
            int nameEnd = comma < 0 ? close : comma;
            string name = new SvgCssVariableResolver().Trim(text.Substring(function + 4, nameEnd - function - 4));
            if (!SvgCssVariableRules.IsCustomName(name)) return new SvgCssVariableResolver().WorkFailure(SvgCssVariableError.InvalidName, function + 4);
            new SvgCssVariableResolver().AddDependency(dependencies, name);
            SvgCssVariableResolveWork replacement;
            if (new SvgCssVariableResolver().StackHas(stack, name)) replacement = new SvgCssVariableResolver().WorkCycleFailure(function, name);
            else
            {
                SvgCustomPropertyValue? stored = properties.Get(name);
                if (stored == null || !stored.IsValid) replacement = new SvgCssVariableResolver().WorkFailure(SvgCssVariableError.MissingVariable, function);
                else
                {
                    stack.Add(name);
                    replacement = new SvgCssVariableResolver().ResolveText(properties, stored.RawText, stack, dependencies, depth + 1, maximumDepth, maximumCharacters);
                    stack.RemoveAt(stack.Count - 1);
                }
            }
            if (!replacement.Success && comma >= 0 && new SvgCssVariableResolver().CanUseFallback(replacement, stack)) replacement = new SvgCssVariableResolver().ResolveText(properties, text.Substring(comma + 1, close - comma - 1), stack, dependencies, depth + 1, maximumDepth, maximumCharacters);
            if (!replacement.Success) return new SvgCssVariableResolveWork(false, replacement.Error, "", function, replacement.CycleName);
            builder.Append(replacement.Text);
            if (builder.Length > maximumCharacters) return new SvgCssVariableResolver().WorkFailure(SvgCssVariableError.ExpansionLimitExceeded, function);
            position = close + 1;
        }
        return new SvgCssVariableResolveWork(true, SvgCssVariableError.None, builder.ToString(), -1, "");
    }
int FindVariableFunction(string text, int start)
    {
        int position = start;
        while (position < text.Length)
        {
            char current = text[position];
            if (current == '\'' || current == '"')
            {
                int next = new SvgCssVariableResolver().SkipString(text, position);
                if (next < 0) return -1;
                position = next;
                continue;
            }
            if (current == '/' && position + 1 < text.Length && text[position + 1] == '*')
            {
                int next = new SvgCssVariableResolver().SkipComment(text, position);
                if (next < 0) return -1;
                position = next;
                continue;
            }
            if ((current == 'v' || current == 'V') && position + 3 < text.Length && (text[position + 1] == 'a' || text[position + 1] == 'A') && (text[position + 2] == 'r' || text[position + 2] == 'R') && text[position + 3] == '(' && (position == 0 || !SvgCssVariableRules.IsNameCharacter(text[position - 1]))) return position;
            position = position + 1;
        }
        return -1;
    }
int FindMatchingParenthesis(string text, int open)
    {
        int depth = 1;
        int position = open + 1;
        while (position < text.Length)
        {
            char current = text[position];
            if (current == '\'' || current == '"')
            {
                position = new SvgCssVariableResolver().SkipString(text, position);
                if (position < 0) return -1;
                continue;
            }
            if (current == '/' && position + 1 < text.Length && text[position + 1] == '*')
            {
                position = new SvgCssVariableResolver().SkipComment(text, position);
                if (position < 0) return -1;
                continue;
            }
            if (current == '(') depth = depth + 1;
            else if (current == ')')
            {
                depth = depth - 1;
                if (depth == 0) return position;
            }
            position = position + 1;
        }
        return -1;
    }
int FindFallbackComma(string text, int start, int end)
    {
        int depth = 0;
        int position = start;
        while (position < end)
        {
            char current = text[position];
            if (current == '\'' || current == '"')
            {
                position = new SvgCssVariableResolver().SkipString(text, position);
                if (position < 0) return -1;
                continue;
            }
            if (current == '/' && position + 1 < end && text[position + 1] == '*')
            {
                position = new SvgCssVariableResolver().SkipComment(text, position);
                if (position < 0) return -1;
                continue;
            }
            if (current == '(' || current == '[' || current == '{') depth = depth + 1;
            else if (current == ')' || current == ']' || current == '}') depth = depth - 1;
            else if (current == ',' && depth == 0) return position;
            position = position + 1;
        }
        return -1;
    }
int SkipString(string text, int start)
    {
        char quote = text[start];
        int position = start + 1;
        while (position < text.Length)
        {
            if (text[position] == quote) return position + 1;
            if (text[position] == '\\') position = position + 1;
            position = position + 1;
        }
        return -1;
    }
int SkipComment(string text, int start)
    {
        int position = start + 2;
        while (position + 1 < text.Length)
        {
            if (text[position] == '*' && text[position + 1] == '/') return position + 2;
            position = position + 1;
        }
        return -1;
    }
string Trim(string text)
    {
        int start = 0;
        int end = text.Length;
        while (start < end && SvgSpanCharacters.IsCssWhitespace(text[start])) start = start + 1;
        while (end > start && SvgSpanCharacters.IsCssWhitespace(text[end - 1])) end = end - 1;
        return text.Substring(start, end - start);
    }
bool StackHas(System.Collections.Generic.List<string> stack, string name)
    {
        int index = 0;
        while (index < stack.Count)
        {
            if (stack[index] == name) return true;
            index = index + 1;
        }
        return false;
    }
void AddDependency(System.Collections.Generic.List<string> dependencies, string name)
    {
        if (new SvgCssVariableResolver().StackHas(dependencies, name)) return;
        dependencies.Add(name);
    }
int MaximumCharacters(int tokens, int tokenCharacters)
    {
        if (tokens > System.Int32.MaxValue / tokenCharacters) return System.Int32.MaxValue;
        return tokens * tokenCharacters;
    }
SvgCssVariableResolveWork WorkFailure(SvgCssVariableError error, int offset)
    {
        return new SvgCssVariableResolveWork(false, error, "", offset, "");
    }
SvgCssVariableResolveWork WorkCycleFailure(int offset, string name)
    {
        return new SvgCssVariableResolveWork(false, SvgCssVariableError.CyclicVariable, "", offset, name);
    }
bool CanUseFallback(SvgCssVariableResolveWork failure, System.Collections.Generic.List<string> stack)
    {
        if (failure.Error == SvgCssVariableError.MissingVariable || failure.Error == SvgCssVariableError.InvalidName || failure.Error == SvgCssVariableError.InvalidFunction) return true;
        return failure.Error == SvgCssVariableError.CyclicVariable && !new SvgCssVariableResolver().StackHas(stack, failure.CycleName);
    }
}
