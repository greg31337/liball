namespace pptx;

/// <summary>
/// Describes one informational message, warning, or error associated with a package operation.
/// </summary>
public class PptxDiagnostic
{
    /// <summary>
    /// Stores the severity value associated with this object.
    /// </summary>
    public PptxDiagnosticSeverity Severity;
    /// <summary>
    /// Stores the code associated with this object.
    /// </summary>
    public string Code;
    /// <summary>
    /// Stores the message associated with this object.
    /// </summary>
    public string Message;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the xml path associated with this object.
    /// </summary>
    public string XmlPath;

    /// <summary>
    /// Initializes a diagnostic with its severity, stable code, message, and optional package location.
    /// </summary>
    /// <param name="severity">The severity that determines whether the diagnostic makes an operation fail.</param>
    /// <param name="code">The stable machine-readable diagnostic code.</param>
    /// <param name="message">The human-readable explanation of the condition.</param>
    /// <param name="partName">The related package part name, or an empty string when the diagnostic is package-wide.</param>
    /// <param name="relationshipId">The related relationship identifier, or an empty string when none applies.</param>
    /// <param name="xmlPath">The approximate XML location, or an empty string when none applies.</param>
    public PptxDiagnostic(
        PptxDiagnosticSeverity severity,
        string code,
        string message,
        string partName,
        string relationshipId,
        string xmlPath)
    {
        Severity = severity;
        Code = CleanText(code);
        Message = CleanText(message);
        PartName = CleanText(partName);
        RelationshipId = CleanText(relationshipId);
        XmlPath = CleanText(xmlPath);
    }
    internal bool IsError()
    {
        return Severity == PptxDiagnosticSeverity.Error;
    }
    internal bool HasLocation()
    {
        if (PartName.Length > 0)
        {
            return true;
        }

        if (RelationshipId.Length > 0)
        {
            return true;
        }

        if (XmlPath.Length > 0)
        {
            return true;
        }

        return false;
    }

    internal string FormatLocation()
    {
        string result = "";

        if (PartName.Length > 0)
        {
            result = PartName;
        }

        if (RelationshipId.Length > 0)
        {
            result = AppendLocation(result, RelationshipId);
        }

        if (XmlPath.Length > 0)
        {
            result = AppendLocation(result, XmlPath);
        }

        return result;
    }
    internal string Format()
    {
        string result = Severity.ToString() + " " + Code + ": " + Message;
        string location = FormatLocation();

        if (location.Length > 0)
        {
            result = result + " (" + location + ")";
        }

        return result;
    }
    internal static PptxDiagnostic CreateInfo(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        return new PptxDiagnostic(PptxDiagnosticSeverity.Info, code, message, partName, relationshipId, xmlPath);
    }
    internal static PptxDiagnostic CreateWarning(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        return new PptxDiagnostic(PptxDiagnosticSeverity.Warning, code, message, partName, relationshipId, xmlPath);
    }
    internal static PptxDiagnostic CreateError(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        return new PptxDiagnostic(PptxDiagnosticSeverity.Error, code, message, partName, relationshipId, xmlPath);
    }

    internal static string CleanText(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal static string AppendLocation(string current, string value)
    {
        if (current.Length == 0)
        {
            return value;
        }

        return current + " " + value;
    }
}

