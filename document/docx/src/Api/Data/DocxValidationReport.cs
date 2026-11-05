namespace docx;

/// <summary>
/// Collects validation errors and repair notices in discovery order.
/// </summary>
public class DocxValidationReport
{
    /// <summary>
    /// The validation entries in discovery order.
    /// </summary>
    public List<DocxError> Errors;

    /// <summary>
    /// Creates a validation report instance.
    /// </summary>
    public DocxValidationReport()
    {
        this.Errors = new List<DocxError>();
    }

    internal void AddError(DocxError error)
    {
        if (error == null)
        {
            throw new ArgumentNullException("error");
        }

        this.Errors.Add(error);
    }

    internal DocxError Add(DocxErrorSeverity severity, string code, string message, string location)
    {
        DocxError error = new DocxError(severity, code, message, location);
        this.Errors.Add(error);
        return error;
    }

    internal int Count()
    {
        return this.Errors.Count;
    }

    internal bool HasErrors()
    {
        foreach (DocxError error in this.Errors)
        {
            if (error.IsError())
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasWarnings()
    {
        foreach (DocxError error in this.Errors)
        {
            if (error.IsWarning())
            {
                return true;
            }
        }

        return false;
    }

    internal bool ContainsCode(string code)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        foreach (DocxError error in this.Errors)
        {
            if (error.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
