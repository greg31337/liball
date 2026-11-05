using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="Av1ObuReader"/>.</summary>
public interface IAv1ObuReader
{
    /// <summary>Returns the outcome of the most recent reader operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Advances to the next parsed OBU.</summary>
    /// <returns><c>Success</c>, <c>EndOfData</c>, or a parse or lifecycle failure.</returns>
    AvifResultCode ReadNext();

    /// <summary>Returns the numeric AV1 OBU type of the current unit.</summary>
    /// <returns>The four-bit OBU type, or zero when no unit is selected.</returns>
    int GetObuType();

    /// <summary>Reports whether the current unit carries an extension header.</summary>
    /// <returns><c>true</c> when temporal and spatial identifiers are present.</returns>
    bool HasExtension();

    /// <summary>Returns the current unit temporal identifier.</summary>
    /// <returns>The temporal identifier, or zero when unavailable.</returns>
    int GetTemporalId();

    /// <summary>Returns the current unit spatial identifier.</summary>
    /// <returns>The spatial identifier, or zero when unavailable.</returns>
    int GetSpatialId();

    /// <summary>Returns the current unit byte offset in the retained stream.</summary>
    /// <returns>The zero-based offset, or negative one when unavailable.</returns>
    int GetOffset();

    /// <summary>Returns the complete encoded size of the current unit.</summary>
    /// <returns>The byte size, or zero when unavailable.</returns>
    int GetSize();

    /// <summary>Returns a copy of the current OBU payload.</summary>
    /// <returns>The payload, or an empty array when no unit is selected.</returns>
    byte[] GetPayload();

    /// <summary>Returns the current sequence-header payload.</summary>
    /// <returns>A payload copy, or an empty array when the current unit is not a sequence header.</returns>
    byte[] GetSequenceHeader();

    /// <summary>Returns the current metadata OBU payload.</summary>
    /// <returns>A payload copy, or an empty array when the current unit is not metadata.</returns>
    byte[] GetMetadata();

    /// <summary>Rewinds traversal to the first parsed OBU.</summary>
    /// <returns>The operation result.</returns>
    AvifResultCode Reset();

    /// <summary>Releases retained source and parsed units.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
