using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifBoxReader"/>.</summary>
public interface IAvifBoxReader
{
    /// <summary>Returns the outcome of the most recent reader operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Advances to the next box at the current hierarchy level.</summary>
    /// <returns><c>Success</c>, <c>EndOfData</c>, or a parse or lifecycle failure.</returns>
    AvifResultCode ReadNext();

    /// <summary>Returns the current box selected by <c>ReadNext</c>.</summary>
    /// <returns>The immutable box description, or null when no box is selected.</returns>
    AvifBox? GetCurrentBox();

    /// <summary>Returns a copy of the current box payload bytes.</summary>
    /// <returns>The payload, or an empty array when no box is selected.</returns>
    byte[] GetPayload();

    /// <summary>Enters the immediate children of the current box.</summary>
    /// <returns>The operation result.</returns>
    AvifResultCode Enter();

    /// <summary>Leaves the current child level and resumes its parent level.</summary>
    /// <returns>The operation result.</returns>
    AvifResultCode Leave();

    /// <summary>Clears the current box selection without entering its children.</summary>
    /// <returns>The operation result.</returns>
    AvifResultCode Skip();

    /// <summary>Releases retained source and traversal state.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
