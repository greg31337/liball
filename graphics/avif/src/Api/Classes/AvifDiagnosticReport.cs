using System.Collections.Generic;

namespace avif;

/// <summary>Provides an independently owned, ordered collection of structured diagnostics.</summary>
public class AvifDiagnosticReport : IAvifDiagnosticReport
{
    List<AvifDiagnostic> diagnostics;

    /// <summary>Creates a report with independent copies of caller-supplied diagnostics.</summary>
    /// <param name="source">The ordered diagnostics, or null for an empty report.</param>
    public AvifDiagnosticReport(AvifDiagnostic[]? source)
    {
        diagnostics = new List<AvifDiagnostic>();
        if (source == null)
        {
            return;
        }
        int index = 0;
        while (index < source.Length)
        {
            diagnostics.Add(CopyDiagnostic(source[index]));
            index++;
        }
    }

    /// <summary>Returns the number of diagnostics.</summary>
    /// <returns>The diagnostic count.</returns>
    public int GetDiagnosticCount()
    {
        return diagnostics.Count;
    }

    /// <summary>Returns one independent diagnostic description.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic, or null for an invalid index.</returns>
    public AvifDiagnostic? GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Count)
        {
            return null;
        }
        return CopyDiagnostic(diagnostics[index]);
    }

    internal static AvifDiagnosticReport FromCode(
        AvifResultCode code,
        AvifDiagnosticCategory category,
        string message)
    {
        List<AvifDiagnostic> source = new List<AvifDiagnostic>();
        AddFallback(source, code, category, message);
        return new AvifDiagnosticReport(source.ToArray());
    }

    internal static AvifDiagnosticReport FromDocument(
        AvifDocument? document,
        AvifResultCode code,
        AvifDiagnosticCategory category,
        string message)
    {
        List<AvifDiagnostic> source = new List<AvifDiagnostic>();
        if (document != null)
        {
            int index = 0;
            while (index < document.GetFindingCount())
            {
                AvifDiagnostic? diagnostic = document.GetFinding(index);
                if (diagnostic != null)
                {
                    source.Add(diagnostic);
                }
                index++;
            }
        }
        AddFallback(source, code, category, message);
        return new AvifDiagnosticReport(source.ToArray());
    }

    internal static AvifDiagnosticReport FromLog(
        DiagnosticLog? log,
        AvifResultCode code,
        AvifDiagnosticCategory category,
        string message)
    {
        List<AvifDiagnostic> source = new List<AvifDiagnostic>();
        if (log != null)
        {
            int index = 0;
            while (index < log.Diagnostics.Count)
            {
                source.Add(log.Diagnostics[index]);
                index++;
            }
        }
        AddFallback(source, code, category, message);
        return new AvifDiagnosticReport(source.ToArray());
    }

    static void AddFallback(
        List<AvifDiagnostic> source,
        AvifResultCode code,
        AvifDiagnosticCategory category,
        string message)
    {
        if (source.Count == 0
            && code != AvifResultCode.Success
            && code != AvifResultCode.NeedMoreData
            && code != AvifResultCode.EndOfData)
        {
            source.Add(AvifDiagnostic.Create(
                code,
                AvifDiagnosticSeverity.Error,
                category,
                message));
        }
    }

    static AvifDiagnostic CopyDiagnostic(AvifDiagnostic source)
    {
        return new AvifDiagnostic(
            source.Code,
            source.Severity,
            source.Category,
            source.Message,
            source.ByteOffset,
            source.BitOffset,
            source.BoxPath,
            source.ItemId,
            source.TrackId,
            source.FrameIndex,
            source.TileIndex,
            source.Av1SyntaxPath,
            source.StandardReference);
    }
}
