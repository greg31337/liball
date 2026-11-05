using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace avif;

/// <summary>
/// Provides a parsed, pixel-independent view of an AVIF container and its validation findings.
/// </summary>
public class AvifDocument : IAvifDocument, IAvifDiagnosticSource
{
    AvifResultCode code;
    AvifFileInfo? fileInfo;
    AvifMovie? movie;
    List<AvifBox> topLevelBoxes;
    List<AvifBox> boxes;
    List<AvifImageItem> items;
    List<AvifItemReference> references;
    List<AvifEntityGroup> groups;
    List<AvifDiagnostic> findings;

    internal AvifDocument(
        AvifResultCode code,
        AvifFileInfo? fileInfo,
        AvifMovie? movie,
        List<AvifBox> topLevelBoxes,
        List<AvifBox> boxes,
        List<AvifImageItem> items,
        List<AvifItemReference> references,
        List<AvifEntityGroup> groups,
        List<AvifDiagnostic> findings)
    {
        this.code = code;
        this.fileInfo = fileInfo;
        this.movie = movie;
        this.topLevelBoxes = new List<AvifBox>(topLevelBoxes);
        this.boxes = new List<AvifBox>(boxes);
        this.items = new List<AvifImageItem>(items);
        this.references = new List<AvifItemReference>(references);
        this.groups = new List<AvifEntityGroup>(groups);
        this.findings = new List<AvifDiagnostic>(findings);
    }

    /// <summary>Parses and validates a byte array with default validation options.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <returns>The parsed document or an argument-error document.</returns>
    internal static AvifDocument Parse(byte[]? source)
    {
        return Parse(source, new AvifValidationOptions());
    }

    /// <summary>Parses and validates a byte array with caller-selected options.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document or an argument-error document.</returns>
    internal static AvifDocument Parse(byte[]? source, AvifValidationOptions options)
    {
        if (source == null)
        {
            return AvifDocumentBuilder.CreateArgumentError();
        }

        return Parse(source.AsSpan(), options);
    }

    /// <summary>Parses and validates a byte span with default validation options.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <returns>The parsed document.</returns>
    internal static AvifDocument Parse(ReadOnlySpan<byte> source)
    {
        return Parse(source, new AvifValidationOptions());
    }

    /// <summary>Parses and validates a byte span with caller-selected options.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document.</returns>
    internal static AvifDocument Parse(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        AvifContainerValidationResult validation = AvifContainerValidator.Validate(
            source,
            options);
        return AvifDocumentBuilder.Create(source, validation);
    }

    /// <summary>Returns the parse or validation outcome.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns file-level brands and primary-item metadata.</summary>
    /// <returns>The file information, or null when unavailable.</returns>
    public AvifFileInfo? GetFileInfo()
    {
        return fileInfo;
    }

    /// <summary>Returns validated movie and timed-track metadata.</summary>
    /// <returns>The movie, or null when the file has no MovieBox.</returns>
    public AvifMovie? GetMovie()
    {
        return movie;
    }

    /// <summary>Returns detected document features and their supported operations.</summary>
    /// <returns>The capability report derived from the parsed structure.</returns>
    public AvifCapabilityReport GetCapabilities()
    {
        return new AvifCapabilityReport(null, this);
    }

    /// <summary>Resolves a primary-item or timed-sequence image source.</summary>
    /// <param name="requestedSelection">The caller-requested source policy.</param>
    /// <returns>The resolved source, or null when the requested source is unavailable.</returns>
    public AvifImageSource? SelectSource(AvifSourceSelection requestedSelection)
    {
        AvifImageItem? primaryItem = FindPrimaryItem();
        AvifTrack? sequenceTrack = FindSequenceTrack();
        if (requestedSelection == AvifSourceSelection.PrimaryItem)
        {
            return primaryItem == null ? null : AvifImageSource.FromPrimaryItem(primaryItem);
        }
        if (requestedSelection == AvifSourceSelection.ImageSequence)
        {
            return sequenceTrack == null
                ? null : new AvifImageSource(movie, sequenceTrack);
        }
        if (requestedSelection != AvifSourceSelection.Automatic)
        {
            return null;
        }
        if (fileInfo != null && fileInfo.GetMajorBrand() == "avif")
        {
            return primaryItem == null ? null : AvifImageSource.FromPrimaryItem(primaryItem);
        }
        if (fileInfo != null && fileInfo.GetMajorBrand() == "avis")
        {
            return sequenceTrack == null
                ? null : new AvifImageSource(movie, sequenceTrack);
        }
        if (sequenceTrack != null)
        {
            return new AvifImageSource(movie, sequenceTrack);
        }
        return primaryItem == null ? null : AvifImageSource.FromPrimaryItem(primaryItem);
    }

    AvifImageItem? FindPrimaryItem()
    {
        if (fileInfo == null || !fileInfo.HasPrimaryItem())
        {
            return null;
        }
        return FindItem(fileInfo.GetPrimaryItemId());
    }

    AvifTrack? FindSequenceTrack()
    {
        if (movie == null)
        {
            return null;
        }
        return movie.GetVisualTrack(0);
    }

    /// <summary>Returns the number of top-level boxes.</summary>
    /// <returns>The top-level box count.</returns>
    public int GetTopLevelBoxCount()
    {
        return topLevelBoxes.Count;
    }

    /// <summary>Returns one top-level box.</summary>
    /// <param name="index">The zero-based top-level index.</param>
    /// <returns>The box, or null for an invalid index.</returns>
    public AvifBox? GetTopLevelBox(int index)
    {
        if (index < 0 || index >= topLevelBoxes.Count)
        {
            return null;
        }

        return topLevelBoxes[index];
    }

    /// <summary>Returns the number of boxes in depth-first source order.</summary>
    /// <returns>The complete box count.</returns>
    public int GetBoxCount()
    {
        return boxes.Count;
    }

    /// <summary>Returns one box in depth-first source order.</summary>
    /// <param name="index">The zero-based flattened box index.</param>
    /// <returns>The box, or null for an invalid index.</returns>
    public AvifBox? GetBox(int index)
    {
        if (index < 0 || index >= boxes.Count)
        {
            return null;
        }

        return boxes[index];
    }

    /// <summary>Returns the number of parsed items.</summary>
    /// <returns>The item count.</returns>
    public int GetItemCount()
    {
        return items.Count;
    }

    /// <summary>Returns one parsed item.</summary>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The item, or null for an invalid index.</returns>
    public AvifImageItem? GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        return items[index];
    }

    /// <summary>Finds an item by identifier.</summary>
    /// <param name="itemId">The item identifier.</param>
    /// <returns>The item, or null when it is absent.</returns>
    public AvifImageItem? FindItem(uint itemId)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].GetItemId() == itemId)
            {
                return items[index];
            }

            index++;
        }

        return null;
    }

    /// <summary>Returns the number of auxiliary images associated with one master item.</summary>
    /// <param name="masterItemId">The master item identifier.</param>
    /// <returns>The associated auxiliary count, or zero when the master is absent.</returns>
    public int GetAuxiliaryImageCount(uint masterItemId)
    {
        AvifImageItem? master = FindItem(masterItemId);
        return master == null ? 0 : master.GetAssociatedAuxiliaryItemCount();
    }

    /// <summary>Resolves one auxiliary association for a master item.</summary>
    /// <param name="masterItemId">The master item identifier.</param>
    /// <param name="index">The zero-based auxiliary index.</param>
    /// <returns>The resolved descriptor, or null for an invalid master or index.</returns>
    public AvifAuxiliaryImage? GetAuxiliaryImage(
        uint masterItemId,
        int index)
    {
        AvifImageItem? master = FindItem(masterItemId);
        if (master == null
            || index < 0
            || index >= master.GetAssociatedAuxiliaryItemCount())
        {
            return null;
        }

        uint auxiliaryItemId = master.GetAssociatedAuxiliaryItemId(index);
        AvifImageItem? auxiliary = FindItem(auxiliaryItemId);
        if (auxiliary == null)
        {
            return null;
        }

        return AvifAuxiliaryImage.FromItems(
            auxiliary,
            master,
            HasPremultipliedReference(master, auxiliaryItemId));
    }

    static bool HasPremultipliedReference(
        AvifImageItem master,
        uint auxiliaryItemId)
    {
        int referenceIndex = 0;
        while (referenceIndex < master.GetOutgoingReferenceCount())
        {
            AvifItemReference? reference = master.GetOutgoingReference(
                referenceIndex);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind() == AvifItemReferenceKind.Premultiplied)
            {
                int targetIndex = 0;
                while (targetIndex < reference.GetTargetCount())
                {
                    if (reference.GetTargetItemId(targetIndex) == auxiliaryItemId)
                    {
                        return true;
                    }
                    targetIndex++;
                }
            }
            referenceIndex++;
        }
        return false;
    }

    /// <summary>Returns the number of directional item references.</summary>
    /// <returns>The reference count.</returns>
    public int GetReferenceCount()
    {
        return references.Count;
    }

    /// <summary>Returns one directional item reference.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The reference, or null for an invalid index.</returns>
    public AvifItemReference? GetReference(int index)
    {
        if (index < 0 || index >= references.Count)
        {
            return null;
        }

        return references[index];
    }

    /// <summary>Returns the number of parsed entity groups.</summary>
    /// <returns>The group count.</returns>
    public int GetGroupCount()
    {
        return groups.Count;
    }

    /// <summary>Returns one parsed entity group.</summary>
    /// <param name="index">The zero-based group index.</param>
    /// <returns>The group, or null for an invalid index.</returns>
    public AvifEntityGroup? GetGroup(int index)
    {
        if (index < 0 || index >= groups.Count)
        {
            return null;
        }

        return groups[index];
    }

    /// <summary>Returns the number of parse and validation findings.</summary>
    /// <returns>The finding count.</returns>
    public int GetFindingCount()
    {
        return findings.Count;
    }

    /// <summary>Returns one parse or validation finding.</summary>
    /// <param name="index">The zero-based finding index.</param>
    /// <returns>The finding, or null for an invalid index.</returns>
    public AvifDiagnostic? GetFinding(int index)
    {
        if (index < 0 || index >= findings.Count)
        {
            return null;
        }

        return AvifDocumentBuilder.CopyFinding(findings[index]);
    }

    /// <summary>Returns all structured parse and validation diagnostics.</summary>
    /// <returns>The document diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromDocument(
            this,
            code,
            AvifDiagnosticCategory.Validation,
            "The AVIF document is not valid.");
    }

    /// <summary>Creates a deterministic text dump of the complete parsed model.</summary>
    /// <returns>The complete container dump.</returns>
    public string Dump()
    {
        return AvifDocumentDumper.Dump(this);
    }

    /// <summary>Creates a deterministic text dump of the parsed box tree.</summary>
    /// <returns>The box dump.</returns>
    public string DumpBoxes()
    {
        return AvifDocumentDumper.DumpBoxes(this);
    }

    /// <summary>Creates a deterministic text dump of parsed items.</summary>
    /// <returns>The item dump.</returns>
    public string DumpItems()
    {
        return AvifDocumentDumper.DumpItems(this);
    }

    /// <summary>Creates a deterministic text dump of item extents.</summary>
    /// <returns>The extent dump.</returns>
    public string DumpExtents()
    {
        return AvifDocumentDumper.DumpExtents(this);
    }

    /// <summary>Creates a deterministic text dump of item-property associations.</summary>
    /// <returns>The property dump.</returns>
    public string DumpProperties()
    {
        return AvifDocumentDumper.DumpProperties(this);
    }

    /// <summary>Creates a deterministic text dump of directional item references.</summary>
    /// <returns>The reference dump.</returns>
    public string DumpReferences()
    {
        return AvifDocumentDumper.DumpReferences(this);
    }

    /// <summary>Creates a deterministic text dump of entity groups.</summary>
    /// <returns>The group dump.</returns>
    public string DumpGroups()
    {
        return AvifDocumentDumper.DumpGroups(this);
    }

    /// <summary>Creates a deterministic text dump of parse and validation findings.</summary>
    /// <returns>The finding dump.</returns>
    public string DumpFindings()
    {
        return AvifDocumentDumper.DumpFindings(this);
    }
}
