using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="Av1Encoder"/>.</summary>
public interface IAv1Encoder
{
    /// <summary>Returns the outcome of the most recent encoder operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Replaces encoder settings before any frame has been added.</summary>
    /// <param name="requestedOptions">The options and limits to snapshot.</param>
    /// <returns>The operation result.</returns>
    AvifResultCode Configure(Av1EncoderOptions requestedOptions);

    /// <summary>Returns an independent copy of the active encoder controls.</summary>
    /// <returns>The active options, or null after closure.</returns>
    Av1EncoderOptions? GetOptions();

    /// <summary>Snapshots and encodes one frame with a positive caller-defined duration.</summary>
    /// <param name="image">The image to encode immediately.</param>
    /// <param name="duration">The positive duration associated with the frame.</param>
    /// <returns>The operation result.</returns>
    AvifResultCode AddFrame(AvifImage image, long duration);

    /// <summary>Appends one opaque metadata OBU to the authored stream.</summary>
    /// <param name="metadata">The unknown metadata value to snapshot.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or Closed.</returns>
    AvifResultCode AddMetadataObu(AvifMetadataObu metadata);

    /// <summary>Finalizes all queued frames into one low-overhead AV1 byte stream.</summary>
    /// <returns>The encoded result.</returns>
    AvifEncodeResult Finish();

    /// <summary>Releases queued frames and encoded output.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
