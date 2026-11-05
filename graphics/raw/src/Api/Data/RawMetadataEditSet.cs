using System;

namespace raw;

/// <summary>Describes one ordered source-targeted metadata edit.</summary>
public class RawMetadataEdit
{
    /// <summary>The requested operation.</summary>
    public RawMetadataEditKind Kind;
    /// <summary>The stable unified field name.</summary>
    public string Name;
    /// <summary>The replacement values, or an empty array for removal.</summary>
    public string[] Values;
    /// <summary>The exact language, an empty string for the default language, or <c>null</c> to match every language during removal.</summary>
    public string? Language;
    /// <summary>The target representation, or <see cref="RawMetadataSource.None"/> to target every existing representation.</summary>
    public RawMetadataSource Source;

    /// <summary>Creates one metadata edit description.</summary>
    /// <param name="kind">The operation kind.</param>
    /// <param name="name">The stable field name.</param>
    /// <param name="values">The values, or <c>null</c> for none.</param>
    /// <param name="language">The language, or <c>null</c>.</param>
    /// <param name="source">The target representation.</param>
    public RawMetadataEdit(RawMetadataEditKind kind, string name, string[]? values, string? language, RawMetadataSource source)
    {
        Kind = kind;
        Name = name == null ? string.Empty : name;
        Values = RawMetadataField.Copy(values);
        Language = language;
        Source = source;
    }
}

/// <summary>Describes a caller-authorized replacement of preview bytes and declared metadata.</summary>
public class RawPreviewMetadataEdit
{
    /// <summary>The zero-based thumbnail collection index.</summary>
    public int Index;
    /// <summary>The replacement representation.</summary>
    public RawThumbnailKind Kind;
    /// <summary>The replacement width.</summary>
    public int Width;
    /// <summary>The replacement height.</summary>
    public int Height;
    /// <summary>The replacement orientation.</summary>
    public RawOrientation Orientation;
    /// <summary>The replacement media type.</summary>
    public string MimeType;
    /// <summary>The complete replacement encoded or uncompressed bytes.</summary>
    public byte[] EncodedBytes;

    /// <summary>Creates a preview replacement description.</summary>
    /// <param name="index">The thumbnail collection index.</param>
    /// <param name="kind">The replacement representation.</param>
    /// <param name="width">The replacement width.</param>
    /// <param name="height">The replacement height.</param>
    /// <param name="orientation">The replacement orientation.</param>
    /// <param name="mimeType">The replacement media type.</param>
    /// <param name="encodedBytes">The complete replacement bytes.</param>
    public RawPreviewMetadataEdit(int index, RawThumbnailKind kind, int width, int height, RawOrientation orientation, string mimeType, byte[] encodedBytes)
    {
        Index = index;
        Kind = kind;
        Width = width;
        Height = height;
        Orientation = orientation;
        MimeType = mimeType == null ? string.Empty : mimeType;
        EncodedBytes = encodedBytes == null ? System.Array.Empty<byte>() : RawTextValue.Copy(encodedBytes);
    }
}

/// <summary>Reports one semantic metadata or preview change.</summary>
public class RawMetadataChange
{
    /// <summary>The applied or planned operation.</summary>
    public RawMetadataEditKind Kind;
    /// <summary>The stable metadata or preview location.</summary>
    public string Location;
    /// <summary>The target representation, or <see cref="RawMetadataSource.None"/> for preview metadata.</summary>
    public RawMetadataSource Source;
    /// <summary>The previous normalized values.</summary>
    public string[] OldValues;
    /// <summary>The replacement normalized values.</summary>
    public string[] NewValues;
    /// <summary>Whether the change was committed to the in-memory model.</summary>
    public bool Applied;

    /// <summary>Creates one metadata change report entry.</summary>
    /// <param name="kind">The operation kind.</param>
    /// <param name="location">The stable location.</param>
    /// <param name="source">The target representation.</param>
    /// <param name="oldValues">The previous values.</param>
    /// <param name="newValues">The replacement values.</param>
    /// <param name="applied">Whether the change was committed.</param>
    public RawMetadataChange(RawMetadataEditKind kind, string location, RawMetadataSource source, string[]? oldValues, string[]? newValues, bool applied)
    {
        Kind = kind;
        Location = location == null ? string.Empty : location;
        Source = source;
        OldValues = RawMetadataField.Copy(oldValues);
        NewValues = RawMetadataField.Copy(newValues);
        Applied = applied;
    }
}

/// <summary>Contains the deterministic result of applying or previewing a metadata edit set.</summary>
public class RawMetadataEditReport
{
    /// <summary>Whether the operation was a non-mutating dry run.</summary>
    public bool DryRun;
    /// <summary>Whether every validated change was committed to the in-memory model.</summary>
    public bool Applied;
    /// <summary>The ordered semantic changes.</summary>
    public RawMetadataChange[] Changes;
    /// <summary>Validation and application diagnostics.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a metadata edit report.</summary>
    /// <param name="dryRun">Whether no mutation was requested.</param>
    /// <param name="applied">Whether changes were committed.</param>
    /// <param name="changes">The ordered changes.</param>
    /// <param name="diagnostics">Operation diagnostics.</param>
    public RawMetadataEditReport(bool dryRun, bool applied, RawMetadataChange[] changes, RawDiagnostic[] diagnostics)
    {
        DryRun = dryRun;
        Applied = applied;
        Changes = changes == null ? System.Array.Empty<RawMetadataChange>() : changes;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Returns whether the report contains no error diagnostic.</summary>
    /// <returns><c>true</c> when planning or application succeeded; otherwise <c>false</c>.</returns>
    internal bool Succeeded()
    {
        int index = 0;
        while (index < Diagnostics.Length)
        {
            if (Diagnostics[index] != null && Diagnostics[index].Severity == RawSeverity.Error)
            {
                return false;
            }
            index++;
        }
        return true;
    }
}

/// <summary>Collects ordered metadata and preview edits for atomic application or dry-run reporting.</summary>
public class RawMetadataEditSet
{
    /// <summary>The ordered field edits.</summary>
    public RawMetadataEdit[] Edits;
    /// <summary>The ordered preview replacements.</summary>
    public RawPreviewMetadataEdit[] PreviewEdits;

    /// <summary>Creates an empty metadata edit set.</summary>
    public RawMetadataEditSet()
    {
        Edits = System.Array.Empty<RawMetadataEdit>();
        PreviewEdits = System.Array.Empty<RawPreviewMetadataEdit>();
    }

    /// <summary>Adds a new field to one source representation.</summary>
    /// <param name="name">The stable field name.</param>
    /// <param name="values">One or more normalized values.</param>
    /// <param name="language">The language, or <c>null</c> for the default language.</param>
    /// <param name="source">The source representation to add.</param>
    /// <returns>This edit set.</returns>
    internal RawMetadataEditSet Add(string name, string[] values, string? language, RawMetadataSource source)
    {
        AppendEdit(new RawMetadataEdit(RawMetadataEditKind.Add, name, values, language, source));
        return this;
    }

    /// <summary>Replaces a field in one source or every existing representation.</summary>
    /// <param name="name">The stable field name.</param>
    /// <param name="values">One or more normalized replacement values.</param>
    /// <param name="language">The exact language, or <c>null</c> to match every language.</param>
    /// <param name="source">The source representation, or <see cref="RawMetadataSource.None"/> for every source.</param>
    /// <returns>This edit set.</returns>
    internal RawMetadataEditSet Replace(string name, string[] values, string? language, RawMetadataSource source)
    {
        AppendEdit(new RawMetadataEdit(RawMetadataEditKind.Replace, name, values, language, source));
        return this;
    }

    /// <summary>Removes a field from one source or every existing representation.</summary>
    /// <param name="name">The stable field name.</param>
    /// <param name="language">The exact language, or <c>null</c> to match every language.</param>
    /// <param name="source">The source representation, or <see cref="RawMetadataSource.None"/> for every source.</param>
    /// <returns>This edit set.</returns>
    internal RawMetadataEditSet Remove(string name, string? language, RawMetadataSource source)
    {
        AppendEdit(new RawMetadataEdit(RawMetadataEditKind.Remove, name, null, language, source));
        return this;
    }

    /// <summary>Appends explicit removals for a conservative privacy scrub preset.</summary>
    /// <param name="preset">The privacy preset.</param>
    /// <returns>This edit set.</returns>
    internal RawMetadataEditSet Scrub(RawPrivacyScrubPreset preset)
    {
        MetadataEditor.AppendScrub(this, preset);
        return this;
    }

    /// <summary>Stages replacement preview bytes together with synchronized geometry and orientation metadata.</summary>
    /// <param name="index">The zero-based thumbnail collection index.</param>
    /// <param name="kind">The replacement representation.</param>
    /// <param name="width">The replacement width.</param>
    /// <param name="height">The replacement height.</param>
    /// <param name="orientation">The replacement orientation.</param>
    /// <param name="mimeType">The replacement media type.</param>
    /// <param name="encodedBytes">The complete replacement bytes.</param>
    /// <returns>This edit set.</returns>
    internal RawMetadataEditSet ReplacePreview(int index, RawThumbnailKind kind, int width, int height, RawOrientation orientation, string mimeType, byte[] encodedBytes)
    {
        RawPreviewMetadataEdit edit = new RawPreviewMetadataEdit(index, kind, width, height, orientation, mimeType, encodedBytes);
        RawPreviewMetadataEdit[] result = new RawPreviewMetadataEdit[PreviewEdits.Length + 1];
        PreviewEdits.AsSpan().CopyTo(result);
        result[result.Length - 1] = edit;
        PreviewEdits = result;
        return this;
    }

    /// <summary>Applies the edit set atomically to a document or performs an equivalent non-mutating dry run.</summary>
    /// <param name="document">The opened document.</param>
    /// <param name="dryRun">Whether to report without mutating.</param>
    /// <returns>The ordered changes and diagnostics.</returns>
    internal RawMetadataEditReport Apply(RawDocument document, bool dryRun)
    {
        return MetadataEditor.Apply(document, this, dryRun);
    }

    /// <summary>Applies field edits atomically to metadata or performs an equivalent non-mutating dry run.</summary>
    /// <param name="metadata">The metadata model.</param>
    /// <param name="dryRun">Whether to report without mutating.</param>
    /// <returns>The ordered changes and diagnostics.</returns>
    internal RawMetadataEditReport ApplyToMetadata(RawMetadata metadata, bool dryRun)
    {
        return MetadataEditor.Apply(metadata, this, dryRun);
    }

    internal void AppendEdit(RawMetadataEdit edit)
    {
        RawMetadataEdit[] result = new RawMetadataEdit[Edits.Length + 1];
        Edits.AsSpan().CopyTo(result);
        result[result.Length - 1] = edit;
        Edits = result;
    }
}
