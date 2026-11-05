using System.IO;

namespace png;

/// <summary>Default service for strict, tolerant, and best-effort PNG validation.</summary>
public class PngValidationService : IPngValidator
{
    /// <summary>Creates an independent PNG validation service.</summary>
    public PngValidationService()
    {
    }

    /// <summary>Validates PNG bytes using strict defaults.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateBytes(byte[] data)
    {
        return Png.ValidateBytes(data);
    }

    /// <summary>Validates PNG bytes using explicit options.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateBytesWithOptions(byte[] data, PngDecoderOptions options)
    {
        return Png.ValidateBytes(data, options);
    }

    /// <summary>Validates PNG bytes beginning at an offset.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateBytesAtOffset(byte[] data, int offset, PngDecoderOptions options)
    {
        return Png.ValidateBytes(data, offset, options);
    }

    /// <summary>Validates a PNG stream using strict defaults.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateStream(Stream stream)
    {
        return Png.ValidateStream(stream);
    }

    /// <summary>Validates a PNG stream using explicit options.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateStreamWithOptions(Stream stream, PngDecoderOptions options)
    {
        return Png.ValidateStream(stream, options);
    }

    /// <summary>Inspects PNG bytes and reports as many issues as possible.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The inspection result.</returns>
    public PngValidationResult InspectBestEffortBytes(byte[] data)
    {
        return Png.InspectBestEffortBytes(data);
    }

    /// <summary>Inspects PNG bytes beginning at an offset.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The inspection result.</returns>
    public PngValidationResult InspectBestEffortBytesAtOffset(byte[] data, int offset)
    {
        return Png.InspectBestEffortBytes(data, offset);
    }

    /// <summary>Inspects a PNG stream and reports as many issues as possible.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The inspection result.</returns>
    public PngValidationResult InspectBestEffortStream(Stream stream)
    {
        return Png.InspectBestEffortStream(stream);
    }

    /// <summary>Validates PNG bytes using tolerant repair policy.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateTolerantRepairBytes(byte[] data)
    {
        return Png.ValidateTolerantRepairBytes(data);
    }

    /// <summary>Validates offset PNG bytes using tolerant repair policy.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateTolerantRepairBytesAtOffset(byte[] data, int offset)
    {
        return Png.ValidateTolerantRepairBytes(data, offset);
    }

    /// <summary>Validates a PNG stream using tolerant repair policy.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The validation result.</returns>
    public PngValidationResult ValidateTolerantRepairStream(Stream stream)
    {
        return Png.ValidateTolerantRepairStream(stream);
    }
}
