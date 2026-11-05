using System;

namespace fake;

internal static class EdgeCaseCatalog
{
    internal static EdgeCaseRecord[] Cases(
        GenerationContext context,
        EdgeCaseCategory category)
    {
        switch (category)
        {
            case EdgeCaseCategory.EmptyNullWhitespace:
                return EmptyNullWhitespace(context);
            case EdgeCaseCategory.BoundaryNumber:
                return BoundaryNumbers(context);
            case EdgeCaseCategory.LongRepeatedText:
                return LongRepeatedText(context, 256);
            case EdgeCaseCategory.Unicode:
                return Unicode(context);
            case EdgeCaseCategory.Temporal:
                return Temporal(context);
            case EdgeCaseCategory.Identifier:
                return Identifiers(context);
            case EdgeCaseCategory.Injection:
                return Injections(context);
            case EdgeCaseCategory.ContradictoryInput:
                return ContradictoryInputs(context);
            default:
                throw new ArgumentOutOfRangeException(
                    "category",
                    category,
                    "category is not supported.");
        }
    }

    internal static EdgeCaseRecord[] EmptyNullWhitespace(
        GenerationContext context)
    {
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            Create(EdgeCaseCategory.EmptyNullWhitespace, "null-reference", null, null, "A reference null value."),
            Text(context, EdgeCaseCategory.EmptyNullWhitespace, "empty-string", "", "A zero-length string."),
            Text(context, EdgeCaseCategory.EmptyNullWhitespace, "ascii-space", " ", "One ASCII space."),
            Text(context, EdgeCaseCategory.EmptyNullWhitespace, "mixed-whitespace", "\t\r\n\u00A0", "Tab, record separators, and a nonbreaking space.")
        };
        return Checked(context, result);
    }

    internal static EdgeCaseRecord[] BoundaryNumbers(
        GenerationContext context)
    {
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            Create(EdgeCaseCategory.BoundaryNumber, "zero", 0, null, "Integral zero."),
            Create(EdgeCaseCategory.BoundaryNumber, "negative-one", -1, null, "The first negative integer."),
            Create(EdgeCaseCategory.BoundaryNumber, "int32-minimum", int.MinValue, null, "The minimum signed 32-bit integer."),
            Create(EdgeCaseCategory.BoundaryNumber, "int32-maximum", int.MaxValue, null, "The maximum signed 32-bit integer."),
            Create(EdgeCaseCategory.BoundaryNumber, "int64-minimum", long.MinValue, null, "The minimum signed 64-bit integer."),
            Create(EdgeCaseCategory.BoundaryNumber, "int64-maximum", long.MaxValue, null, "The maximum signed 64-bit integer."),
            Create(EdgeCaseCategory.BoundaryNumber, "decimal-minimum", decimal.MinValue, null, "The minimum decimal value."),
            Create(EdgeCaseCategory.BoundaryNumber, "decimal-maximum", decimal.MaxValue, null, "The maximum decimal value."),
            Create(EdgeCaseCategory.BoundaryNumber, "double-minimum", double.MinValue, null, "The minimum finite double value."),
            Create(EdgeCaseCategory.BoundaryNumber, "double-maximum", double.MaxValue, null, "The maximum finite double value."),
            Create(EdgeCaseCategory.BoundaryNumber, "negative-zero", -0.0d, 0.0d, "Distinct floating-point encodings that compare equal."),
            Create(EdgeCaseCategory.BoundaryNumber, "double-epsilon", double.Epsilon, 0.0d, "The smallest positive double value and zero."),
            Create(EdgeCaseCategory.BoundaryNumber, "positive-infinity", double.PositiveInfinity, null, "Positive infinity."),
            Create(EdgeCaseCategory.BoundaryNumber, "negative-infinity", double.NegativeInfinity, null, "Negative infinity."),
            Create(EdgeCaseCategory.BoundaryNumber, "not-a-number", double.NaN, null, "A non-number floating-point value."),
            Create(EdgeCaseCategory.BoundaryNumber, "binary-rounding", 0.1d + 0.2d, 0.3d, "A familiar binary floating-point rounding boundary.")
        };
        return Checked(context, result);
    }

    internal static EdgeCaseRecord[] LongRepeatedText(
        GenerationContext context,
        int length)
    {
        Guard.Positive(length, "length");
        Guard.Count(length, context.CapturedOptions.MaximumCount, "length");
        Guard.OutputSize(length, context.CapturedOptions.MaximumOutputSize, "length");
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            Text(context, EdgeCaseCategory.LongRepeatedText, "repeated-letter", new string('x', length), "An all-equal alphabetic string."),
            Text(context, EdgeCaseCategory.LongRepeatedText, "repeated-digit", new string('0', length), "An all-equal numeric string."),
            Text(context, EdgeCaseCategory.LongRepeatedText, "repeated-pattern", RepeatPattern(length), "A long alternating text pattern.")
        };
        return Checked(context, result);
    }

    internal static EdgeCaseRecord[] Unicode(GenerationContext context)
    {
        EdgeCaseRecord malformed = Create(
            EdgeCaseCategory.Unicode,
            "unpaired-high-surrogate",
            new char[] { '\uD800' },
            null,
            "Malformed UTF-16 represented only as a character array.");
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            Text(context, EdgeCaseCategory.Unicode, "right-to-left", "مرحبا", "Right-to-left Arabic text."),
            Text(context, EdgeCaseCategory.Unicode, "bidi-control", "abc\u202Edef\u202C", "Text containing explicit bidi override controls."),
            Text(context, EdgeCaseCategory.Unicode, "combining-mark", "e\u0301", "A base character followed by a combining mark."),
            Text(context, EdgeCaseCategory.Unicode, "compatibility-character", "\u212B", "A character changed by Unicode normalization."),
            Text(context, EdgeCaseCategory.Unicode, "emoji-sequence", "👩‍💻", "An emoji sequence joined with a zero-width joiner."),
            Text(context, EdgeCaseCategory.Unicode, "non-bmp", "𐐷", "A Unicode scalar represented by a surrogate pair."),
            Text(context, EdgeCaseCategory.Unicode, "zero-width-non-joiner", "می\u200Cخواهم", "Text containing a zero-width non-joiner."),
            Text(context, EdgeCaseCategory.Unicode, "confusable", "раypal", "Visually confusable Cyrillic and Latin characters."),
            Text(context, EdgeCaseCategory.Unicode, "mixed-scripts", "AБ中", "Latin, Cyrillic, and Han scripts in one string."),
            malformed
        };
        return Checked(context, result);
    }

    internal static EdgeCaseRecord[] Temporal(GenerationContext context)
    {
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            Create(EdgeCaseCategory.Temporal, "runtime-minimum", DateTimeOffset.MinValue, null, "The minimum runtime date and time with offset."),
            Create(EdgeCaseCategory.Temporal, "runtime-maximum", DateTimeOffset.MaxValue, null, "The maximum runtime date and time with offset."),
            Create(EdgeCaseCategory.Temporal, "unix-epoch", DateTimeOffset.UnixEpoch, null, "The Unix epoch."),
            Create(EdgeCaseCategory.Temporal, "leap-day", new DateTimeOffset(2000, 2, 29, 12, 0, 0, TimeSpan.Zero), null, "A leap day in a century divisible by 400."),
            Create(EdgeCaseCategory.Temporal, "offset-minimum", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-14)), null, "The minimum supported UTC offset."),
            Create(EdgeCaseCategory.Temporal, "offset-maximum", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(14)), null, "The maximum supported UTC offset."),
            Create(EdgeCaseCategory.Temporal, "year-boundary", new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.Zero), new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), "Adjacent instants in different years."),
            Create(EdgeCaseCategory.Temporal, "dst-gap-local", new DateTime(2024, 3, 10, 2, 30, 0, DateTimeKind.Unspecified), null, "A wall time in the America/New_York spring DST gap."),
            Create(EdgeCaseCategory.Temporal, "dst-overlap-local", new DateTime(2024, 11, 3, 1, 30, 0, DateTimeKind.Unspecified), null, "A wall time repeated during the America/New_York autumn DST overlap.")
        };
        return Checked(context, result);
    }

    internal static EdgeCaseRecord[] Identifiers(GenerationContext context)
    {
        int limit = context.CapturedOptions.MaximumOutputSize;
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            Text(context, EdgeCaseCategory.Identifier, "wrong-checksum", IdentifierMutations.InvalidFixture(IdentifierMutationKind.Checksum, limit), "An identifier with a deliberately wrong check digit."),
            Text(context, EdgeCaseCategory.Identifier, "bad-length", IdentifierMutations.InvalidFixture(IdentifierMutationKind.Length, limit), "An identifier with a deliberately unsupported length."),
            Text(context, EdgeCaseCategory.Identifier, "invalid-character", IdentifierMutations.InvalidFixture(IdentifierMutationKind.Character, limit), "An identifier containing a forbidden character."),
            Text(context, EdgeCaseCategory.Identifier, "forbidden-prefix", IdentifierMutations.InvalidFixture(IdentifierMutationKind.Prefix, limit), "An identifier with a forbidden prefix."),
            Text(context, EdgeCaseCategory.Identifier, "bad-uuid-version", IdentifierMutations.InvalidFixture(IdentifierMutationKind.Version, limit), "A canonical UUID string with an unsupported version."),
            Text(context, EdgeCaseCategory.Identifier, "ulid-overflow", IdentifierMutations.InvalidFixture(IdentifierMutationKind.Overflow, limit), "A ULID string above its textual range."),
            Text(context, EdgeCaseCategory.Identifier, "invalid-iban-country", "ZZ00INVALID", "An IBAN-shaped value with an unsupported country and length."),
            Text(context, EdgeCaseCategory.Identifier, "malformed-idn", "xn--", "An incomplete internationalized domain label."),
            Text(context, EdgeCaseCategory.Identifier, "ambiguous-phone", "+1 555 0100 x123/456", "A phone-shaped value with ambiguous extension formatting.")
        };
        return Checked(context, result);
    }

    internal static EdgeCaseRecord[] Injections(GenerationContext context)
    {
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            AdversarialText(context, AdversarialTextContext.Sql),
            AdversarialText(context, AdversarialTextContext.Html),
            AdversarialText(context, AdversarialTextContext.Spreadsheet),
            AdversarialText(context, AdversarialTextContext.Shell),
            AdversarialText(context, AdversarialTextContext.Path),
            AdversarialText(context, AdversarialTextContext.FormatString),
            AdversarialText(context, AdversarialTextContext.Template),
            AdversarialText(context, AdversarialTextContext.Header)
        };
        return Checked(context, result);
    }

    internal static EdgeCaseRecord AdversarialText(
        GenerationContext context,
        AdversarialTextContext textContext)
    {
        switch (textContext)
        {
            case AdversarialTextContext.Sql:
                return Text(context, EdgeCaseCategory.Injection, "sql", "' OR 1=1 --", "SQL-injection-shaped inert text.");
            case AdversarialTextContext.Html:
                return Text(context, EdgeCaseCategory.Injection, "html", "<script>alert(1)</script>", "HTML-script-shaped inert text.");
            case AdversarialTextContext.Spreadsheet:
                return Text(context, EdgeCaseCategory.Injection, "spreadsheet", "=1+1", "Spreadsheet-formula-shaped inert text.");
            case AdversarialTextContext.Shell:
                return Text(context, EdgeCaseCategory.Injection, "shell", "$(touch /tmp/fake-edge-probe)", "Shell-command-substitution-shaped inert text.");
            case AdversarialTextContext.Path:
                return Text(context, EdgeCaseCategory.Injection, "path-traversal", "../../etc/passwd", "Path-traversal-shaped inert text.");
            case AdversarialTextContext.FormatString:
                return Text(context, EdgeCaseCategory.Injection, "format-string", "%s%s%s%s%n", "Format-string-shaped inert text.");
            case AdversarialTextContext.Template:
                return Text(context, EdgeCaseCategory.Injection, "template", "{{7*7}}", "Template-expression-shaped inert text.");
            case AdversarialTextContext.Header:
                return Text(context, EdgeCaseCategory.Injection, "header-splitting", "value\r\nX-Fake: injected", "Header-splitting-shaped inert text.");
            default:
                throw new ArgumentOutOfRangeException(
                    "context",
                    textContext,
                    "context is not supported.");
        }
    }

    internal static EdgeCaseRecord[] ContradictoryInputs(
        GenerationContext context)
    {
        DateTimeOffset instant = new DateTimeOffset(
            2024,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        EdgeCaseRecord[] result = new EdgeCaseRecord[]
        {
            Create(EdgeCaseCategory.ContradictoryInput, "reversed-numeric-range", 10, 1, "The minimum is greater than the maximum."),
            Create(EdgeCaseCategory.ContradictoryInput, "negative-count", -1, 10, "The requested count is negative despite a positive limit."),
            Create(EdgeCaseCategory.ContradictoryInput, "empty-exclusive-time-range", instant, instant, "Equal endpoints cannot form a nonempty exclusive interval."),
            TextPair(context, EdgeCaseCategory.ContradictoryInput, "conflicting-required-value", "status=active", "status=inactive", "One field is required to equal two different values."),
            TextPair(context, EdgeCaseCategory.ContradictoryInput, "required-and-forbidden", "feature=required", "feature=forbidden", "One feature is simultaneously required and forbidden.")
        };
        return Checked(context, result);
    }

    static EdgeCaseRecord[] Checked(
        GenerationContext context,
        EdgeCaseRecord[] records)
    {
        Guard.Count(
            records.Length,
            context.CapturedOptions.MaximumCount,
            "category");
        return records;
    }

    static EdgeCaseRecord Text(
        GenerationContext context,
        EdgeCaseCategory category,
        string name,
        string value,
        string description)
    {
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "value");
        return Create(category, name, value, null, description);
    }

    static EdgeCaseRecord TextPair(
        GenerationContext context,
        EdgeCaseCategory category,
        string name,
        string value,
        string relatedValue,
        string description)
    {
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "value");
        Guard.OutputSize(
            relatedValue.Length,
            context.CapturedOptions.MaximumOutputSize,
            "relatedValue");
        return Create(category, name, value, relatedValue, description);
    }

    static EdgeCaseRecord Create(
        EdgeCaseCategory category,
        string name,
        object? value,
        object? relatedValue,
        string description)
    {
        EdgeCaseRecord result = new EdgeCaseRecord();
        result.Category = category;
        result.Name = name;
        result.Value = value;
        result.RelatedValue = relatedValue;
        result.Description = description;
        return result;
    }

    static string RepeatPattern(int length)
    {
        char[] result = new char[length];
        int index = 0;
        while (index < length)
        {
            result[index] = index % 2 == 0 ? 'a' : 'b';
            index = index + 1;
        }
        return new string(result);
    }
}
