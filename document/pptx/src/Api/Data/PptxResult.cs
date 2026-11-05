using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Collects diagnostics produced by validation, repair, loading, and other operations that can partially fail.
/// </summary>
public class PptxResult
{
    /// <summary>
    /// Contains the diagnostics in package or presentation order.
    /// </summary>
    public List<PptxDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new result with safe defaults or the supplied values.
    /// </summary>
    public PptxResult()
    {
        Diagnostics = new List<PptxDiagnostic>();
    }
    internal int Count()
    {
        return Diagnostics.Count;
    }
    internal bool Succeeded()
    {
        return !HasErrors();
    }
    internal bool Failed()
    {
        return HasErrors();
    }
    internal bool HasErrors()
    {
        for (int index = 0; index < Diagnostics.Count; index++)
        {
            PptxDiagnostic diagnostic = Diagnostics[index];
            if (diagnostic.IsError())
            {
                return true;
            }
        }

        return false;
    }
    internal void Add(PptxDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new System.ArgumentNullException("diagnostic");
        }

        Diagnostics.Add(diagnostic);
    }
    internal PptxDiagnostic AddInfo(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        PptxDiagnostic diagnostic = PptxDiagnostic.CreateInfo(code, message, partName, relationshipId, xmlPath);
        Add(diagnostic);
        return diagnostic;
    }
    internal PptxDiagnostic AddWarning(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        PptxDiagnostic diagnostic = PptxDiagnostic.CreateWarning(code, message, partName, relationshipId, xmlPath);
        Add(diagnostic);
        return diagnostic;
    }
    internal PptxDiagnostic AddError(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        PptxDiagnostic diagnostic = PptxDiagnostic.CreateError(code, message, partName, relationshipId, xmlPath);
        Add(diagnostic);
        return diagnostic;
    }
    internal string FormatDiagnostics()
    {
        string result = "";

        for (int index = 0; index < Diagnostics.Count; index++)
        {
            if (index > 0)
            {
                result = result + "\n";
            }

            result = result + Diagnostics[index].Format();
        }

        return result;
    }
}

