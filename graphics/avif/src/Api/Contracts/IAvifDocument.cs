using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifDocument"/>.</summary>
public interface IAvifDocument
{
    /// <summary>Returns the parse or validation outcome.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns file-level brands and primary-item metadata.</summary>
    /// <returns>The file information, or null when unavailable.</returns>
    AvifFileInfo? GetFileInfo();

    /// <summary>Returns validated movie and timed-track metadata.</summary>
    /// <returns>The movie, or null when the file has no MovieBox.</returns>
    AvifMovie? GetMovie();

    /// <summary>Returns detected document features and their supported operations.</summary>
    /// <returns>The capability report derived from the parsed structure.</returns>
    AvifCapabilityReport GetCapabilities();

    /// <summary>Resolves a primary-item or timed-sequence image source.</summary>
    /// <param name="requestedSelection">The caller-requested source policy.</param>
    /// <returns>The resolved source, or null when the requested source is unavailable.</returns>
    AvifImageSource? SelectSource(AvifSourceSelection requestedSelection);

    /// <summary>Returns the number of top-level boxes.</summary>
    /// <returns>The top-level box count.</returns>
    int GetTopLevelBoxCount();

    /// <summary>Returns one top-level box.</summary>
    /// <param name="index">The zero-based top-level index.</param>
    /// <returns>The box, or null for an invalid index.</returns>
    AvifBox? GetTopLevelBox(int index);

    /// <summary>Returns the number of boxes in depth-first source order.</summary>
    /// <returns>The complete box count.</returns>
    int GetBoxCount();

    /// <summary>Returns one box in depth-first source order.</summary>
    /// <param name="index">The zero-based flattened box index.</param>
    /// <returns>The box, or null for an invalid index.</returns>
    AvifBox? GetBox(int index);

    /// <summary>Returns the number of parsed items.</summary>
    /// <returns>The item count.</returns>
    int GetItemCount();

    /// <summary>Returns one parsed item.</summary>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The item, or null for an invalid index.</returns>
    AvifImageItem? GetItem(int index);

    /// <summary>Finds an item by identifier.</summary>
    /// <param name="itemId">The item identifier.</param>
    /// <returns>The item, or null when it is absent.</returns>
    AvifImageItem? FindItem(uint itemId);

    /// <summary>Returns the number of auxiliary images associated with one master item.</summary>
    /// <param name="masterItemId">The master item identifier.</param>
    /// <returns>The associated auxiliary count, or zero when the master is absent.</returns>
    int GetAuxiliaryImageCount(uint masterItemId);

    /// <summary>Resolves one auxiliary association for a master item.</summary>
    /// <param name="masterItemId">The master item identifier.</param>
    /// <param name="index">The zero-based auxiliary index.</param>
    /// <returns>The resolved descriptor, or null for an invalid master or index.</returns>
    AvifAuxiliaryImage? GetAuxiliaryImage(
        uint masterItemId,
        int index);

    /// <summary>Returns the number of directional item references.</summary>
    /// <returns>The reference count.</returns>
    int GetReferenceCount();

    /// <summary>Returns one directional item reference.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The reference, or null for an invalid index.</returns>
    AvifItemReference? GetReference(int index);

    /// <summary>Returns the number of parsed entity groups.</summary>
    /// <returns>The group count.</returns>
    int GetGroupCount();

    /// <summary>Returns one parsed entity group.</summary>
    /// <param name="index">The zero-based group index.</param>
    /// <returns>The group, or null for an invalid index.</returns>
    AvifEntityGroup? GetGroup(int index);

    /// <summary>Returns the number of parse and validation findings.</summary>
    /// <returns>The finding count.</returns>
    int GetFindingCount();

    /// <summary>Returns one parse or validation finding.</summary>
    /// <param name="index">The zero-based finding index.</param>
    /// <returns>The finding, or null for an invalid index.</returns>
    AvifDiagnostic? GetFinding(int index);

    /// <summary>Creates a deterministic text dump of the complete parsed model.</summary>
    /// <returns>The complete container dump.</returns>
    string Dump();

    /// <summary>Creates a deterministic text dump of the parsed box tree.</summary>
    /// <returns>The box dump.</returns>
    string DumpBoxes();

    /// <summary>Creates a deterministic text dump of parsed items.</summary>
    /// <returns>The item dump.</returns>
    string DumpItems();

    /// <summary>Creates a deterministic text dump of item extents.</summary>
    /// <returns>The extent dump.</returns>
    string DumpExtents();

    /// <summary>Creates a deterministic text dump of item-property associations.</summary>
    /// <returns>The property dump.</returns>
    string DumpProperties();

    /// <summary>Creates a deterministic text dump of directional item references.</summary>
    /// <returns>The reference dump.</returns>
    string DumpReferences();

    /// <summary>Creates a deterministic text dump of entity groups.</summary>
    /// <returns>The group dump.</returns>
    string DumpGroups();

    /// <summary>Creates a deterministic text dump of parse and validation findings.</summary>
    /// <returns>The finding dump.</returns>
    string DumpFindings();
}
