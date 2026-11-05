namespace raw;

/// <summary>Describes one validation or conformance finding.</summary>
public class RawValidationMessage
{
    /// <summary>The validation subsystem that produced the message.</summary>
    public RawValidationCategory Category;
    /// <summary>The machine-readable diagnostic identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The message severity.</summary>
    public RawSeverity Severity;
    /// <summary>The validated format.</summary>
    public RawFormat Format;
    /// <summary>The zero-based file offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;
    /// <summary>The stable context path, or an empty string.</summary>
    public string Context;
    /// <summary>The human-readable validation description.</summary>
    public string Message;
    /// <summary>Whether tolerant validation applied a named recovery.</summary>
    public bool Recovered;

    /// <summary>Creates a validation message.</summary>
    /// <param name="category">The validation subsystem.</param>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="format">The validated format.</param>
    /// <param name="offset">The file offset, or <c>-1</c>.</param>
    /// <param name="context">The stable validation context.</param>
    /// <param name="message">The human-readable description.</param>
    /// <param name="recovered">Whether recovery was applied.</param>
    public RawValidationMessage(RawValidationCategory category, RawDiagnosticCode code, RawSeverity severity, RawFormat format, long offset, string context, string message, bool recovered)
    {
        Category = category;
        Code = code;
        Severity = severity;
        Format = format;
        Offset = offset;
        Context = context == null ? string.Empty : context;
        Message = message == null ? string.Empty : message;
        Recovered = recovered;
    }

    /// <summary>Converts this validation finding to a general operation diagnostic.</summary>
    /// <returns>A diagnostic containing the same code, severity, format, offset, message, and recovery state.</returns>
    internal RawDiagnostic ToDiagnostic()
    {
        return new RawDiagnostic(Code, Severity, Format, Offset, Context, -1, Category.ToString(), Message, Recovered);
    }
}

/// <summary>Summarizes findings for one machine-readable validation category.</summary>
public class RawValidationCategoryReport
{
    /// <summary>The summarized validation category.</summary>
    public RawValidationCategory Category;
    /// <summary>Whether the category was selected for validation.</summary>
    public bool Selected;
    /// <summary>The number of informational findings.</summary>
    public int InformationCount;
    /// <summary>The number of warning findings.</summary>
    public int WarningCount;
    /// <summary>The number of error findings.</summary>
    public int ErrorCount;
    /// <summary>The number of findings produced by named recovery rules.</summary>
    public int RecoveredCount;

    /// <summary>Creates one category summary.</summary>
    /// <param name="category">The summarized category.</param>
    /// <param name="selected">Whether the category was selected.</param>
    /// <param name="messages">The complete report findings.</param>
    public RawValidationCategoryReport(RawValidationCategory category, bool selected, RawValidationMessage[]? messages)
    {
        Category = category;
        Selected = selected;
        InformationCount = 0;
        WarningCount = 0;
        ErrorCount = 0;
        RecoveredCount = 0;
        int index = 0;
        while (messages != null && index < messages.Length)
        {
            RawValidationMessage message = messages[index];
            if (message != null && message.Category == category)
            {
                if (message.Severity == RawSeverity.Error)
                {
                    ErrorCount++;
                }
                else if (message.Severity == RawSeverity.Warning)
                {
                    WarningCount++;
                }
                else
                {
                    InformationCount++;
                }
                if (message.Recovered)
                {
                    RecoveredCount++;
                }
            }
            index++;
        }
    }

    /// <summary>Returns whether this selected category satisfies the requested validation mode.</summary>
    /// <param name="strict">Whether warnings are rejected as non-conformance.</param>
    /// <returns><c>true</c> when the category is unselected or conformant; otherwise <c>false</c>.</returns>
    internal bool IsConformant(bool strict)
    {
        if (!Selected)
        {
            return true;
        }
        return ErrorCount == 0 && (!strict || WarningCount == 0);
    }
}

/// <summary>Contains machine-readable structural, codec, semantic, profile, metadata, and writer findings.</summary>
public class RawValidationReport
{
    /// <summary>Whether strict validation rules were requested.</summary>
    public bool Strict;
    /// <summary>Validation findings in deterministic production order.</summary>
    public RawValidationMessage[]? Messages;
    /// <summary>Category summaries in stable enum order.</summary>
    public RawValidationCategoryReport[]? Categories;

    /// <summary>Creates a validation report.</summary>
    /// <param name="strict">Whether strict validation rules were requested.</param>
    /// <param name="messages">The findings, or <c>null</c> for an empty set.</param>
    public RawValidationReport(bool strict, RawValidationMessage[]? messages)
    {
        Initialize(strict, messages, true, true, true, true, true, true);
    }

    internal static RawValidationReport CreateSelected(bool strict, RawValidationMessage[] messages, bool structural, bool codec, bool semantic, bool profile, bool metadata, bool writer)
    {
        RawValidationReport report = new RawValidationReport(strict, messages);
        report.Initialize(strict, messages, structural, codec, semantic, profile, metadata, writer);
        return report;
    }

    internal void Initialize(bool strict, RawValidationMessage[]? messages, bool structural, bool codec, bool semantic, bool profile, bool metadata, bool writer)
    {
        Strict = strict;
        Messages = messages == null ? System.Array.Empty<RawValidationMessage>() : messages;
        Categories = new RawValidationCategoryReport[6];
        Categories[0] = new RawValidationCategoryReport(RawValidationCategory.Structural, structural, Messages);
        Categories[1] = new RawValidationCategoryReport(RawValidationCategory.Codec, codec, Messages);
        Categories[2] = new RawValidationCategoryReport(RawValidationCategory.Semantic, semantic, Messages);
        Categories[3] = new RawValidationCategoryReport(RawValidationCategory.Profile, profile, Messages);
        Categories[4] = new RawValidationCategoryReport(RawValidationCategory.Metadata, metadata, Messages);
        Categories[5] = new RawValidationCategoryReport(RawValidationCategory.Writer, writer, Messages);
    }

    /// <summary>Returns whether no error finding is present.</summary>
    /// <returns><c>true</c> when the report contains no errors; otherwise <c>false</c>.</returns>
    internal bool IsValid()
    {
        int index = 0;
        while (index < (Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawValidationMessage message = Messages[index];
            if (message != null && message.Severity == RawSeverity.Error)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    /// <summary>Returns whether at least one warning finding is present.</summary>
    /// <returns><c>true</c> when the report contains a warning; otherwise <c>false</c>.</returns>
    internal bool HasWarnings()
    {
        int index = 0;
        while (index < (Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawValidationMessage message = Messages[index];
            if (message != null && message.Severity == RawSeverity.Warning)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Returns whether all selected categories conform to the requested validation mode.</summary>
    /// <returns><c>true</c> when no selected error exists and strict reports contain no warning; otherwise <c>false</c>.</returns>
    internal bool IsConformant()
    {
        int index = 0;
        while (index < (Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawValidationMessage message = Messages[index];
            if (message != null && (message.Severity == RawSeverity.Error || Strict && message.Severity == RawSeverity.Warning))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    /// <summary>Returns the stable summary for one validation category.</summary>
    /// <param name="category">The category to find.</param>
    /// <returns>The category summary, or <c>null</c> for an unknown category.</returns>
    internal RawValidationCategoryReport? GetCategory(RawValidationCategory category)
    {
        int index = 0;
        while (index < (Categories ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            if (Categories[index].Category == category)
            {
                RawValidationCategoryReport current = new RawValidationCategoryReport(category, Categories[index].Selected, Messages);
                Categories[index] = current;
                return current;
            }
            index++;
        }
        return null;
    }
}
