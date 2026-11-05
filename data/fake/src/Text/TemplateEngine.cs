using System;
using System.Collections.Generic;
using System.Text;

namespace fake;

internal class TemplateToken
{
    internal string Name = string.Empty;
    internal string? Argument;
    internal int Position;
    internal int End;

    internal TemplateToken()
    {
        Name = "";
        Argument = null;
        Position = 0;
        End = 0;
    }
}

internal class TemplateState
{
    internal FakeRandom RandomValue;
    internal Dictionary<string, string> Values;
    internal TemplateResolverRegistry Resolvers;
    internal HashSet<string> Active;
    internal int MaximumDepth;
    internal int MaximumCount;
    internal int MaximumOutputSize;

    internal TemplateState(FakeRandom random, Dictionary<string, string> values, TemplateResolverRegistry resolvers, int maximumDepth, int maximumCount, int maximumOutputSize)
    {
        RandomValue = random;
        Values = values;
        Resolvers = resolvers;
        Active = new HashSet<string>(values.Comparer);
        MaximumDepth = maximumDepth;
        MaximumCount = maximumCount;
        MaximumOutputSize = maximumOutputSize;
    }
}

internal static class TemplateEngine
{
    internal static string[] Tokens()
    {
        return new string[]
        {
            "primitive.alpha",
            "primitive.alphanumeric",
            "primitive.digit",
            "primitive.digits",
            "primitive.hash",
            "primitive.hex",
            "primitive.passphrase",
            "primitive.password",
            "primitive.pin",
            "primitive.recovery-code",
            "primitive.token",
            "text.format",
            "text.pattern"
        };
    }

    internal static string Generate(FakeRandom random, string template, Dictionary<string, string> values, int maximumDepth, int maximumCount, int maximumOutputSize)
    {
        TemplateResolverRegistry resolvers = new TemplateResolverRegistry(maximumCount);
        return GenerateWithResolvers(
            random,
            template,
            values,
            resolvers,
            maximumDepth,
            maximumCount,
            maximumOutputSize);
    }

    internal static string GenerateWithResolvers(FakeRandom random, string template, Dictionary<string, string> values, TemplateResolverRegistry resolvers, int maximumDepth, int maximumCount, int maximumOutputSize)
    {
        Guard.NotNull(values, "values");
        Guard.NotNull(resolvers, "resolvers");
        Guard.Positive(maximumDepth, "maximumDepth");
        Guard.Count(maximumDepth, maximumCount, "maximumDepth");
        TemplateState inspecting = new TemplateState(random, values, resolvers, maximumDepth, maximumCount, maximumOutputSize);
        Inspect(template, inspecting, 0);
        TemplateState measuring = new TemplateState(random, values, resolvers, maximumDepth, maximumCount, maximumOutputSize);
        long maximumLength = Measure(template, measuring, 0);
        Guard.OutputSize(maximumLength, maximumOutputSize, "template");
        TemplateState rendering = new TemplateState(random, values, resolvers, maximumDepth, maximumCount, maximumOutputSize);
        string result = Render(template, rendering, 0);
        Guard.OutputSize(result.Length, maximumOutputSize, "template");
        return result;
    }

    internal static void ValidateWithResolvers(FakeRandom random, string template, Dictionary<string, string> values, TemplateResolverRegistry resolvers, int maximumDepth, int maximumCount, int maximumOutputSize)
    {
        Guard.NotNull(values, "values");
        Guard.NotNull(resolvers, "resolvers");
        Guard.Positive(maximumDepth, "maximumDepth");
        Guard.Count(maximumDepth, maximumCount, "maximumDepth");
        TemplateState inspecting = new TemplateState(random, values, resolvers, maximumDepth, maximumCount, maximumOutputSize);
        Inspect(template, inspecting, 0);
        TemplateState measuring = new TemplateState(random, values, resolvers, maximumDepth, maximumCount, maximumOutputSize);
        long maximumLength = Measure(template, measuring, 0);
        Guard.OutputSize(maximumLength, maximumOutputSize, "template");
    }

    internal static string ResolveWithResolvers(FakeRandom random, string name, string? argument, TemplateResolverRegistry resolvers, int maximumCount, int maximumOutputSize)
    {
        name = ValidateResolverName(name, maximumCount);
        Guard.NotNull(resolvers, "resolvers");
        if (argument != null)
        {
            UnicodeText.Validate(argument, "argument");
            Guard.Count(argument.Length, maximumCount, "argument");
        }
        TemplateToken token = new TemplateToken();
        token.Name = name;
        token.Argument = argument;
        token.Position = 0;
        token.End = 0;
        Dictionary<string, string> values = new Dictionary<string, string>();
        TemplateState state = new TemplateState(random, values, resolvers, 1, maximumCount, maximumOutputSize);
        if (IsBuiltInToken(name))
        {
            long length = BuiltInLength(token, state);
            Guard.OutputSize(length, maximumOutputSize, "token");
            return Invoke(token, state);
        }
        if (!resolvers.Contains(name))
        {
            throw new ArgumentException(
                $"The token '{name}' is not registered.",
                "name");
        }
        string result = resolvers.Resolve(name, argument);
        UnicodeText.Validate(result, "resolver");
        Guard.OutputSize(result.Length, maximumOutputSize, "token");
        return result;
    }

    internal static string ValidateResolverName(string name, int maximumCount)
    {
        name = Guard.NotNullOrWhiteSpace(name, "name");
        UnicodeText.Validate(name, "name");
        Guard.Count(name.Length, maximumCount, "name");
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            bool valid = (character >= 'a' && character <= 'z')
                || (character >= 'A' && character <= 'Z')
                || (character >= '0' && character <= '9')
                || character == '.'
                || character == '-'
                || character == '_';
            if (!valid)
            {
                throw new ArgumentException(
                    "Token names may contain only ASCII letters, digits, dots, hyphens, and underscores.",
                    "name");
            }
            index = index + 1;
        }
        return name;
    }

    static void Inspect(string template, TemplateState state, int depth)
    {
        ValidateTemplate(template, state.MaximumCount);
        int index = 0;
        while (index < template.Length)
        {
            char character = template[index];
            if (character == '\\')
            {
                Escape(template, index);
                index = index + 2;
                continue;
            }
            if (character == '{' && index + 1 < template.Length && template[index + 1] == '{')
            {
                TemplateToken token = ReadToken(template, index);
                InspectToken(token, state, depth);
                index = token.End;
                continue;
            }
            index = index + 1;
        }
    }

    static void InspectToken(TemplateToken token, TemplateState state, int depth)
    {
        if (IsBuiltInToken(token.Name))
        {
            BuiltInLength(token, state);
            return;
        }
        if (state.Values.ContainsKey(token.Name))
        {
            NoArgument(token);
            Enter(token, state, depth);
            string nested = state.Values[token.Name];
            if (nested == null)
            {
                throw new ArgumentException($"values contains a null template for token '{token.Name}'.", "values");
            }
            Inspect(nested, state, depth + 1);
            state.Active.Remove(token.Name);
            return;
        }
        if (state.Resolvers.Contains(token.Name))
        {
            return;
        }
        Invalid($"token '{token.Name}' is not present in the provider catalog, registered resolvers, or supplied values", token.Position);
    }

    static long Measure(string template, TemplateState state, int depth)
    {
        ValidateTemplate(template, state.MaximumCount);
        long length = 0L;
        int index = 0;
        while (index < template.Length)
        {
            char character = template[index];
            if (character == '\\')
            {
                Escape(template, index);
                length = AddLength(length, 1L, state.MaximumOutputSize);
                if (length > state.MaximumOutputSize)
                {
                    return length;
                }
                index = index + 2;
                continue;
            }
            if (character == '{' && index + 1 < template.Length && template[index + 1] == '{')
            {
                TemplateToken token = ReadToken(template, index);
                length = AddLength(length, MeasureToken(token, state, depth), state.MaximumOutputSize);
                if (length > state.MaximumOutputSize)
                {
                    return length;
                }
                index = token.End;
                continue;
            }
            length = AddLength(length, 1L, state.MaximumOutputSize);
            if (length > state.MaximumOutputSize)
            {
                return length;
            }
            index = index + 1;
        }
        return length;
    }

    static string Render(string template, TemplateState state, int depth)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < template.Length)
        {
            char character = template[index];
            if (character == '\\')
            {
                builder.Append(template[index + 1]);
                Guard.OutputSize(builder.Length, state.MaximumOutputSize, "template");
                index = index + 2;
                continue;
            }
            if (character == '{' && index + 1 < template.Length && template[index + 1] == '{')
            {
                TemplateToken token = ReadToken(template, index);
                builder.Append(RenderToken(token, state, depth));
                Guard.OutputSize(builder.Length, state.MaximumOutputSize, "template");
                index = token.End;
                continue;
            }
            builder.Append(character);
            Guard.OutputSize(builder.Length, state.MaximumOutputSize, "template");
            index = index + 1;
        }
        return builder.ToString();
    }

    static long MeasureToken(TemplateToken token, TemplateState state, int depth)
    {
        if (IsBuiltInToken(token.Name))
        {
            return BuiltInLength(token, state);
        }
        if (state.Values.ContainsKey(token.Name))
        {
            NoArgument(token);
            Enter(token, state, depth);
            string nested = state.Values[token.Name];
            if (nested == null)
            {
                throw new ArgumentException($"values contains a null template for token '{token.Name}'.", "values");
            }
            long length = Measure(nested, state, depth + 1);
            state.Active.Remove(token.Name);
            return length;
        }
        if (state.Resolvers.Contains(token.Name))
        {
            return 0L;
        }
        Invalid($"token '{token.Name}' is not present in the provider catalog, registered resolvers, or supplied values", token.Position);
        return 0L;
    }

    static string RenderToken(TemplateToken token, TemplateState state, int depth)
    {
        if (IsBuiltInToken(token.Name))
        {
            return Invoke(token, state);
        }
        if (state.Values.ContainsKey(token.Name))
        {
            Enter(token, state, depth);
            string result = Render(state.Values[token.Name], state, depth + 1);
            state.Active.Remove(token.Name);
            return result;
        }
        string resolved = state.Resolvers.Resolve(token.Name, token.Argument);
        UnicodeText.Validate(resolved, "resolver");
        return resolved;
    }

    static long BuiltInLength(TemplateToken token, TemplateState state)
    {
        if (token.Name == "primitive.digit")
        {
            NoArgument(token);
            return 1L;
        }
        if (token.Name == "primitive.recovery-code")
        {
            NoArgument(token);
            return SecretGenerator.RecoveryOutputLength(4, 4, '-', state.MaximumCount, state.MaximumOutputSize);
        }
        if (token.Name == "text.format")
        {
            string argument = RequiredArgument(token);
            return MaskFormatter.OutputLength(argument, state.MaximumOutputSize);
        }
        if (token.Name == "text.pattern")
        {
            string argument = RequiredArgument(token);
            return PatternGenerator.MaximumOutputLength(argument, 10, state.MaximumCount, state.MaximumOutputSize);
        }
        int defaultCount = DefaultCount(token.Name);
        int count = CountArgument(token, defaultCount);
        if (token.Name == "primitive.password" && count < 4)
        {
            Invalid("primitive.password requires a length of at least four", token.Position);
        }
        if (token.Name == "primitive.passphrase")
        {
            return SecretGenerator.PassphraseOutputLength(count, "-", state.MaximumCount, state.MaximumOutputSize);
        }
        return Guard.OutputSize(count, state.MaximumOutputSize, "template");
    }

    static string Invoke(TemplateToken token, TemplateState state)
    {
        FakeRandom random = state.RandomValue;
        if (token.Name == "primitive.digit")
        {
            return NumericStringGenerator.Digits(random, 1, state.MaximumOutputSize);
        }
        if (token.Name == "primitive.recovery-code")
        {
            return SecretGenerator.RecoveryCode(random, 4, 4, '-', state.MaximumCount, state.MaximumOutputSize);
        }
        if (token.Name == "text.format")
        {
            return MaskFormatter.Format(
                random,
                RequiredArgument(token),
                state.MaximumOutputSize);
        }
        if (token.Name == "text.pattern")
        {
            return PatternGenerator.Generate(
                random,
                RequiredArgument(token),
                10,
                state.MaximumCount,
                state.MaximumOutputSize);
        }
        int count = CountArgument(token, DefaultCount(token.Name));
        if (token.Name == "primitive.alpha")
        {
            return CharacterStringGenerator.Generate(random, CharacterStringGenerator.AlphaAlphabet, count, true, state.MaximumOutputSize, state.MaximumCount, false);
        }
        if (token.Name == "primitive.alphanumeric")
        {
            return CharacterStringGenerator.Generate(random, CharacterStringGenerator.AlphaNumericAlphabet, count, true, state.MaximumOutputSize, state.MaximumCount, false);
        }
        if (token.Name == "primitive.digits")
        {
            return NumericStringGenerator.Digits(random, count, state.MaximumOutputSize);
        }
        if (token.Name == "primitive.hash" || token.Name == "primitive.hex")
        {
            return SecretGenerator.Hex(random, count, state.MaximumOutputSize);
        }
        if (token.Name == "primitive.token")
        {
            return SecretGenerator.Token(random, count, state.MaximumOutputSize);
        }
        if (token.Name == "primitive.pin")
        {
            return SecretGenerator.Pin(random, count, state.MaximumOutputSize);
        }
        if (token.Name == "primitive.password")
        {
            return SecretGenerator.Password(random, count, true, true, true, true, state.MaximumOutputSize);
        }
        return SecretGenerator.Passphrase(random, count, "-", state.MaximumCount, state.MaximumOutputSize);
    }

    static int DefaultCount(string name)
    {
        if (name == "primitive.hash")
        {
            return 40;
        }
        if (name == "primitive.hex")
        {
            return 16;
        }
        if (name == "primitive.token")
        {
            return 24;
        }
        if (name == "primitive.pin" || name == "primitive.passphrase")
        {
            return 4;
        }
        if (name == "primitive.password")
        {
            return 16;
        }
        return 8;
    }

    static int CountArgument(TemplateToken token, int defaultCount)
    {
        if (token.Argument == null)
        {
            return defaultCount;
        }
        string value = token.Argument.Trim();
        if (value.Length == 0)
        {
            Invalid($"token '{token.Name}' requires a decimal count after the colon", token.Position);
        }
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                Invalid($"token '{token.Name}' count must contain decimal digits only", token.Position);
            }
            int digit = character - '0';
            if (result > (int.MaxValue - digit) / 10)
            {
                Invalid($"token '{token.Name}' count exceeds the supported integer range", token.Position);
            }
            result = (result * 10) + digit;
            index = index + 1;
        }
        return result;
    }

    internal static bool IsBuiltInToken(string name)
    {
        return name == "primitive.alpha"
            || name == "primitive.alphanumeric"
            || name == "primitive.digit"
            || name == "primitive.digits"
            || name == "primitive.hash"
            || name == "primitive.hex"
            || name == "primitive.passphrase"
            || name == "primitive.password"
            || name == "primitive.pin"
            || name == "primitive.recovery-code"
            || name == "primitive.token"
            || name == "text.format"
            || name == "text.pattern";
    }

    static void Enter(TemplateToken token, TemplateState state, int depth)
    {
        if (state.Active.Contains(token.Name))
        {
            Invalid($"token '{token.Name}' forms a template cycle", token.Position);
        }
        if (depth >= state.MaximumDepth)
        {
            Invalid($"token '{token.Name}' exceeds the configured recursion depth of {state.MaximumDepth}", token.Position);
        }
        state.Active.Add(token.Name);
    }

    static TemplateToken ReadToken(string template, int position)
    {
        int search = position + 2;
        int closing = -1;
        int end = -1;
        while (search < template.Length)
        {
            if (template[search] == '}')
            {
                int runEnd = search + 1;
                while (runEnd < template.Length && template[runEnd] == '}')
                {
                    runEnd = runEnd + 1;
                }
                if (runEnd - search >= 2)
                {
                    closing = runEnd - 2;
                    end = runEnd;
                    break;
                }
                search = runEnd;
            }
            else
            {
                search = search + 1;
            }
        }
        if (closing < 0)
        {
            Invalid("placeholder is missing a closing delimiter", position);
        }
        string content = template.Substring(position + 2, closing - position - 2);
        int colon = content.IndexOf(':');
        TemplateToken token = new TemplateToken();
        token.Position = position;
        token.End = end;
        if (colon < 0)
        {
            token.Name = content.Trim();
        }
        else
        {
            token.Name = content.Substring(0, colon).Trim();
            token.Argument = content.Substring(colon + 1);
        }
        if (token.Name.Length == 0)
        {
            Invalid("placeholder token name cannot be empty", position);
        }
        int nameIndex = 0;
        while (nameIndex < token.Name.Length)
        {
            char character = token.Name[nameIndex];
            bool valid = (character >= 'a' && character <= 'z')
                || (character >= 'A' && character <= 'Z')
                || (character >= '0' && character <= '9')
                || character == '.'
                || character == '-'
                || character == '_';
            if (!valid)
            {
                Invalid($"placeholder token name '{token.Name}' contains an unsupported character", position);
            }
            nameIndex = nameIndex + 1;
        }
        return token;
    }

    static string RequiredArgument(TemplateToken token)
    {
        if (token.Argument == null)
        {
            Invalid($"token '{token.Name}' requires an argument after a colon", token.Position);
        }
        return token.Argument ?? throw FakeException.Create(
            "Template argument validation did not produce a value.");
    }

    static void NoArgument(TemplateToken token)
    {
        if (token.Argument != null)
        {
            Invalid($"token '{token.Name}' does not accept an argument", token.Position);
        }
    }

    static void ValidateTemplate(string template, int maximumCount)
    {
        UnicodeText.Validate(template, "template");
        Guard.Count(template.Length, maximumCount, "template");
    }

    static void Escape(string template, int position)
    {
        if (position + 1 >= template.Length)
        {
            Invalid("template contains a trailing escape", position);
        }
        char escaped = template[position + 1];
        if (escaped != '\\' && escaped != '{' && escaped != '}')
        {
            Invalid($"template contains the unsupported escape '\\{escaped}'", position);
        }
    }

    static long AddLength(long first, long second, int maximumOutputSize)
    {
        if (first > maximumOutputSize || second > maximumOutputSize || first + second > maximumOutputSize)
        {
            return (long)maximumOutputSize + 1L;
        }
        return first + second;
    }

    static void Invalid(string message, int position)
    {
        throw new ArgumentException($"Invalid template at index {position}: {message}.", "template");
    }
}
