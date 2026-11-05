using System.Collections.Generic;

namespace tiff;

/// <summary>Collects warnings and errors without using exceptions as the normal TIFF error-reporting path.</summary>
public class TiffDiagnostics : ITiffDiagnostics
{
    internal List<TiffDiagnostic> Items;

    /// <summary>Creates a new instance of the TIFF Diagnostics class.</summary>
    public TiffDiagnostics()
    {
        Items = new List<TiffDiagnostic>();
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffDiagnostics Create()
    {
        return new TiffDiagnostics();
    }

    /// <summary>Returns the number of available value entries.</summary>
    /// <returns>The resulting int.</returns>
    public int Count()
    {
        return Items.Count;
    }

    /// <summary>Returns the value.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The requested value or model.</returns>
    public TiffDiagnostic Get(int index)
    {
        return Items[index];
    }

    /// <summary>Removes all currently collected diagnostics.</summary>
    public void Clear()
    {
        Items.Clear();
    }

    /// <summary>Adds value to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="diagnostic">The diagnostic.</param>
    public void Add(TiffDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        Items.Add(diagnostic);
    }

    /// <summary>Adds error to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The resulting TIFF diagnostic.</returns>
    public TiffDiagnostic AddError(TiffErrorCode code, string message)
    {
        TiffDiagnostic diagnostic = new TiffDiagnostic(TiffDiagnosticSeverity.Error, code, message);
        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Adds warning to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The resulting TIFF diagnostic.</returns>
    public TiffDiagnostic AddWarning(TiffErrorCode code, string message)
    {
        TiffDiagnostic diagnostic = new TiffDiagnostic(TiffDiagnosticSeverity.Warning, code, message);
        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Adds error at to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>The resulting TIFF diagnostic.</returns>
    public TiffDiagnostic AddErrorAt(TiffErrorCode code, string message, long offset)
    {
        TiffDiagnostic diagnostic = TiffDiagnostic.Create(TiffDiagnosticSeverity.Error, code, message, offset, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId);
        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Adds warning at to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>The resulting TIFF diagnostic.</returns>
    public TiffDiagnostic AddWarningAt(TiffErrorCode code, string message, long offset)
    {
        TiffDiagnostic diagnostic = TiffDiagnostic.Create(TiffDiagnosticSeverity.Warning, code, message, offset, TiffDiagnostic.NoImageIndex, TiffDiagnostic.NoTagId);
        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Adds error for tag to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The resulting TIFF diagnostic.</returns>
    public TiffDiagnostic AddErrorForTag(TiffErrorCode code, string message, int imageIndex, int tagId)
    {
        TiffDiagnostic diagnostic = TiffDiagnostic.Create(TiffDiagnosticSeverity.Error, code, message, TiffDiagnostic.NoOffset, imageIndex, tagId);
        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Adds warning for tag to the mutable model and returns its assigned value when applicable.</summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The resulting TIFF diagnostic.</returns>
    public TiffDiagnostic AddWarningForTag(TiffErrorCode code, string message, int imageIndex, int tagId)
    {
        TiffDiagnostic diagnostic = TiffDiagnostic.Create(TiffDiagnosticSeverity.Warning, code, message, TiffDiagnostic.NoOffset, imageIndex, tagId);
        Add(diagnostic);
        return diagnostic;
    }

    /// <summary>Determines whether has errors under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool HasErrors()
    {
        int index = 0;
        while (index < Items.Count)
        {
            if (Items[index].IsError())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Determines whether has warnings under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool HasWarnings()
    {
        int index = 0;
        while (index < Items.Count)
        {
            if (Items[index].IsWarning())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Returns the number of available error entries.</summary>
    /// <returns>The resulting int.</returns>
    public int ErrorCount()
    {
        int count = 0;
        int index = 0;
        while (index < Items.Count)
        {
            if (Items[index].IsError())
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>Returns the number of available warning entries.</summary>
    /// <returns>The resulting int.</returns>
    public int WarningCount()
    {
        int count = 0;
        int index = 0;
        while (index < Items.Count)
        {
            if (Items[index].IsWarning())
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>Determines whether the collection contains code.</summary>
    /// <param name="code">The code.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool ContainsCode(TiffErrorCode code)
    {
        int index = 0;
        while (index < Items.Count)
        {
            if (Items[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Formats one diagnostic for display.</summary>
    /// <param name="diagnostic">The diagnostic.</param>
    /// <returns>The formatted diagnostic.</returns>
    public string Format(TiffDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            return "";
        }

        return diagnostic.Format();
    }

    /// <summary>Appends copies of the supplied diagnostics in reporting order.</summary>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    public void Merge(TiffDiagnostics diagnostics)
    {
        if (diagnostics == null)
        {
            return;
        }

        int index = 0;
        while (index < diagnostics.Count())
        {
            Add(diagnostics.Get(index));
            index = index + 1;
        }
    }

    internal void MergeWithLocation(TiffDiagnostics diagnostics, long offset, int imageIndex, int tagId)
    {
        if (diagnostics == null)
        {
            return;
        }

        int index = 0;
        while (index < diagnostics.Count())
        {
            TiffDiagnostic source = diagnostics.Get(index);
            long actualOffset = source.Offset;
            int actualImageIndex = source.ImageIndex;
            int actualTagId = source.TagId;
            if (actualOffset == TiffDiagnostic.NoOffset)
            {
                actualOffset = offset;
            }

            if (actualImageIndex == TiffDiagnostic.NoImageIndex)
            {
                actualImageIndex = imageIndex;
            }

            if (actualTagId == TiffDiagnostic.NoTagId)
            {
                actualTagId = tagId;
            }

            Add(TiffDiagnostic.Create(source.Severity, source.Code, source.Message, actualOffset, actualImageIndex, actualTagId));
            index = index + 1;
        }
    }

    internal void FillMissingLocation(long offset, int imageIndex, int tagId)
    {
        int index = 0;
        while (index < Items.Count)
        {
            TiffDiagnostic diagnostic = Items[index];
            if (diagnostic.Offset == TiffDiagnostic.NoOffset)
            {
                diagnostic.Offset = offset;
            }

            if (diagnostic.ImageIndex == TiffDiagnostic.NoImageIndex)
            {
                diagnostic.ImageIndex = imageIndex;
            }

            if (diagnostic.TagId == TiffDiagnostic.NoTagId)
            {
                diagnostic.TagId = tagId;
            }

            index = index + 1;
        }
    }

    /// <summary>Returns the diagnostics as a new array in reporting order.</summary>
    /// <returns>The resulting TIFF diagnostic array.</returns>
    public TiffDiagnostic[] ToArray()
    {
        return Items.ToArray();
    }
}
