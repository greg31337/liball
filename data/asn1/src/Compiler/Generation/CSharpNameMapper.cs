namespace asn1;

internal class CSharpNameMapper
{
    internal static readonly string[] Keywords =
    [
        "abstract", "and", "as", "ascending", "async", "await", "base",
        "bool", "break", "by", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate",
        "descending", "do", "double", "dynamic", "else", "enum", "equals",
        "event", "explicit", "extern", "false", "file", "finally", "fixed",
        "float", "for", "foreach", "from", "get", "global", "goto", "group",
        "if", "implicit", "in", "init", "int", "interface", "internal",
        "into", "is", "join", "let", "lock", "long", "managed", "nameof",
        "namespace", "new", "nint", "not", "notnull", "nuint", "null",
        "object", "on", "operator", "or", "orderby", "out", "override",
        "params", "partial", "private", "protected", "public", "readonly",
        "record", "ref", "remove", "required", "return", "sbyte", "scoped",
        "sealed", "select", "set", "short", "sizeof", "stackalloc", "static",
        "string", "struct", "switch", "this", "throw", "true", "try",
        "typeof", "uint", "ulong", "unchecked", "unmanaged", "unsafe",
        "ushort", "using", "value", "var", "virtual", "void", "volatile",
        "when", "where", "while", "with", "yield"
    ];

    internal CSharpNameMapping MapNamespaceName(
        string sourceName,
        string stableId,
        CSharpNameScope scope)
    {
        this.RequireMappingInputs(sourceName, stableId, scope);
        string normalized = this.NormalizeSourceName(sourceName);
        string baseName = this.CreateNamespaceIdentifier(normalized);
        CSharpNameReservation reservation = scope.ReserveName(baseName);
        return this.CreateMapping(
            sourceName,
            normalized,
            stableId,
            reservation.Name,
            reservation.CollisionIndex);
    }

    internal CSharpNameMapping MapTypeName(
        string sourceName,
        string stableId,
        CSharpNameScope scope)
    {
        this.RequireMappingInputs(sourceName, stableId, scope);
        string normalized = this.NormalizeSourceName(sourceName);
        string baseName = this.CreateIdentifier(normalized, "UnnamedType");
        baseName = this.AvoidKeyword(baseName, normalized);
        CSharpNameReservation reservation = scope.ReserveName(baseName);
        return this.CreateMapping(
            sourceName,
            normalized,
            stableId,
            reservation.Name,
            reservation.CollisionIndex);
    }

    internal CSharpNameMapping MapMemberName(
        string sourceName,
        string stableId,
        CSharpNameScope scope)
    {
        this.RequireMappingInputs(sourceName, stableId, scope);
        string normalized = this.NormalizeSourceName(sourceName);
        string baseName = this.CreateIdentifier(normalized, "UnnamedMember");
        baseName = this.AvoidKeyword(baseName, normalized);
        CSharpNameReservation reservation = scope.ReserveName(baseName);
        return this.CreateMapping(
            sourceName,
            normalized,
            stableId,
            reservation.Name,
            reservation.CollisionIndex);
    }

    internal CSharpNameMapping MapFileName(
        CSharpNameMapping typeName,
        CSharpNameScope scope)
    {
        if (typeName == null)
        {
            throw new System.ArgumentNullException("typeName");
        }

        if (scope == null)
        {
            throw new System.ArgumentNullException("scope");
        }

        string baseName = typeName.GeneratedName;
        if (this.IsReservedFileBase(baseName))
        {
            baseName = baseName + "File";
        }

        CSharpNameReservation reservation = scope.ReserveName(baseName);
        string generatedName = reservation.Name + ".cs";
        return this.CreateMapping(
            typeName.SourceName,
            typeName.NormalizedSourceName,
            typeName.StableId,
            generatedName,
            reservation.CollisionIndex);
    }

    internal void RequireMappingInputs(
        string sourceName,
        string stableId,
        CSharpNameScope scope)
    {
        if (sourceName == null)
        {
            throw new System.ArgumentNullException("sourceName");
        }

        if (stableId == null)
        {
            throw new System.ArgumentNullException("stableId");
        }

        if (scope == null)
        {
            throw new System.ArgumentNullException("scope");
        }
    }

    internal string NormalizeSourceName(string sourceName)
    {
        if (sourceName == null)
        {
            throw new System.ArgumentNullException("sourceName");
        }

        return sourceName.Normalize(System.Text.NormalizationForm.FormC);
    }

    internal string CreateNamespaceIdentifier(string normalizedSourceName)
    {
        if (normalizedSourceName.Length == 0)
        {
            return "Generated";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int segmentStart = 0;
        int index = 0;
        while (index <= normalizedSourceName.Length)
        {
            if (index == normalizedSourceName.Length
                || normalizedSourceName[index] == '.')
            {
                string segment = normalizedSourceName.Substring(
                    segmentStart,
                    index - segmentStart);
                string identifier = this.CreateIdentifier(
                    segment,
                    "Generated");
                identifier = this.AvoidKeyword(identifier, segment);
                if (result.Length > 0)
                {
                    result.Append('.');
                }

                result.Append(identifier);
                segmentStart = index + 1;
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string CreateIdentifier(
        string normalizedSourceName,
        string fallbackName)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool startOfWord = true;
        int index = 0;
        while (index < normalizedSourceName.Length)
        {
            char character = normalizedSourceName[index];
            if (character >= 'A' && character <= 'Z'
                || character >= 'a' && character <= 'z')
            {
                char mapped = startOfWord
                    ? char.ToUpperInvariant(character)
                    : character;
                result.Append(mapped);
                startOfWord = false;
            }
            else if (character >= '0' && character <= '9')
            {
                if (result.Length == 0)
                {
                    result.Append('N');
                }

                result.Append(character);
                startOfWord = false;
            }
            else if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= normalizedSourceName.Length
                    || !char.IsLowSurrogate(normalizedSourceName[index + 1]))
                {
                    throw new System.ArgumentException(
                        "The source name contains an invalid Unicode surrogate.",
                        "normalizedSourceName");
                }

                int scalar = char.ConvertToUtf32(
                    character,
                    normalizedSourceName[index + 1]);
                this.AppendUnicodeScalar(result, scalar);
                startOfWord = false;
                index = index + 1;
            }
            else if (char.IsLowSurrogate(character))
            {
                throw new System.ArgumentException(
                    "The source name contains an invalid Unicode surrogate.",
                    "normalizedSourceName");
            }
            else if (character > 127)
            {
                this.AppendUnicodeScalar(result, character);
                startOfWord = false;
            }
            else
            {
                startOfWord = true;
            }

            index = index + 1;
        }

        if (result.Length == 0)
        {
            return fallbackName;
        }

        return result.ToString();
    }

    internal void AppendUnicodeScalar(
        System.Text.StringBuilder result,
        int scalar)
    {
        result.Append('U');
        if (scalar <= 65535)
        {
            result.Append(scalar.ToString(
                "X4",
                System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            result.Append(scalar.ToString(
                "X",
                System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    internal string AvoidKeyword(string identifier, string normalizedSourceName)
    {
        if (this.IsKeyword(identifier)
            || this.IsKeyword(normalizedSourceName))
        {
            return identifier + "Value";
        }

        return identifier;
    }

    internal bool IsKeyword(string value)
    {
        int index = 0;
        while (index < Keywords.Length)
        {
            if (string.Equals(
                Keywords[index],
                value,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsReservedFileBase(string value)
    {
        if (string.Equals(value, "CON", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "PRN", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "AUX", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "NUL", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Length == 4
            && (value.StartsWith("COM", System.StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("LPT", System.StringComparison.OrdinalIgnoreCase))
            && value[3] >= '1'
            && value[3] <= '9')
        {
            return true;
        }

        return false;
    }

    internal CSharpNameMapping CreateMapping(
        string sourceName,
        string normalizedSourceName,
        string stableId,
        string generatedName,
        int collisionIndex)
    {
        return new CSharpNameMapping
        {
            SourceName = sourceName,
            NormalizedSourceName = normalizedSourceName,
            StableId = stableId,
            GeneratedName = generatedName,
            CollisionIndex = collisionIndex,
            Changed = sourceName != generatedName
        };
    }
}
