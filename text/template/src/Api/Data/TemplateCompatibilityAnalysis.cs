using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace template;

/// <summary>
/// Contains converted native text and diagnostics from a compatibility analysis.
/// </summary>
public class TemplateCompatibilityAnalysis
{
    /// <summary>
    /// Specifies the source profile name for this compatibility analysis.
    /// </summary>
    public string SourceProfileName;
    /// <summary>
    /// Specifies the target profile name for this compatibility analysis.
    /// </summary>
    public string TargetProfileName;
    /// <summary>
    /// Specifies the native template text for this compatibility analysis.
    /// </summary>
    public string NativeTemplateText;
    /// <summary>
    /// Stores the ordered diagnostics.
    /// </summary>
    public List<TemplateDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new template compatibility analysis and validates any supplied values.
    /// </summary>
    /// <param name="sourceProfileName">The dialect used by the input text.</param>
    /// <param name="targetProfileName">The dialect whose behavior should be checked.</param>
    /// <param name="nativeTemplateText">The native template text used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateCompatibilityAnalysis(string sourceProfileName, string targetProfileName, string nativeTemplateText)
    {
        if (sourceProfileName == null)
        {
            throw new ArgumentNullException("sourceProfileName");
        }

        if (targetProfileName == null)
        {
            throw new ArgumentNullException("targetProfileName");
        }

        if (nativeTemplateText == null)
        {
            throw new ArgumentNullException("nativeTemplateText");
        }

        SourceProfileName = sourceProfileName;
        TargetProfileName = targetProfileName;
        NativeTemplateText = nativeTemplateText;
        Diagnostics = new List<TemplateDiagnostic>();
    }

    /// <summary>
    /// Indicates whether this value contains diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        return Diagnostics.Count > 0;
    }

    /// <summary>
    /// Formats this value into a stable, human-readable form.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string Format()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("CompatibilityAnalysis source=");
        WriteQuoted(builder, SourceProfileName);
        builder.Append(" target=");
        WriteQuoted(builder, TargetProfileName);
        builder.Append(" nativeLength=");
        builder.Append(NativeTemplateText.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append('\n');

        int index = 0;
        while (index < Diagnostics.Count)
        {
            TemplateDiagnostic diagnostic = Diagnostics[index];
            builder.Append("  Diagnostic ");
            builder.Append(diagnostic.Code);
            builder.Append(" ");
            builder.Append(diagnostic.Severity.ToString());
            builder.Append(" message=");
            WriteQuoted(builder, diagnostic.Message);
            builder.Append('\n');
            index = index + 1;
        }

        return builder.ToString();
    }

    internal void WriteQuoted(StringBuilder builder, string text)
    {
        builder.Append('"');
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '\\' || value == '"')
            {
                builder.Append('\\');
                builder.Append(value);
            }
            else if (value == '\n')
            {
                builder.Append("\\n");
            }
            else if (value == '\r')
            {
                builder.Append("\\r");
            }
            else if (value == '\t')
            {
                builder.Append("\\t");
            }
            else
            {
                builder.Append(value);
            }

            index = index + 1;
        }

        builder.Append('"');
    }
}

