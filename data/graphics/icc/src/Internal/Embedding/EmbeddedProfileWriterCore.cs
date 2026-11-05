namespace icc;

internal class EmbeddedProfileWriterCore
{
    internal EmbeddedProfileWriterCore()
    {
    }

    internal byte[] InsertBytes(System.ReadOnlySpan<byte> carrierBytes,
        IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options)
    {
        RequireSupportedFormat(format);
        byte[] profileBytes = WriteProfile(profile, options);
        RequireCarrierLimit(carrierBytes.Length,
            options.ResourceLimits.MaximumCarrierBytes);
        return InsertCarrier(carrierBytes, profileBytes, format,
            options.ResourceLimits);
    }

    internal byte[] ReplaceBytes(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, IccProfile profile, IccEmbeddedFormat format,
        IccWriteOptions options)
    {
        RequireSupportedFormat(format);
        byte[] profileBytes = WriteProfile(profile, options);
        RequireCarrierLimit(carrierBytes.Length,
            options.ResourceLimits.MaximumCarrierBytes);
        return ReplaceCarrier(carrierBytes, profileIndex, profileBytes, format,
            options.ResourceLimits);
    }

    internal byte[] RemoveBytes(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, IccEmbeddedFormat format)
    {
        RequireSupportedFormat(format);
        IccResourceLimits limits = new IccResourceLimits();
        RequireCarrierLimit(carrierBytes.Length, limits.MaximumCarrierBytes);
        return RemoveCarrier(carrierBytes, profileIndex, format, limits);
    }

    internal void InsertStream(System.IO.Stream sourceStream,
        System.IO.Stream destinationStream, IccProfile profile,
        IccEmbeddedFormat format, IccWriteOptions options)
    {
        ValidateStreams(sourceStream, destinationStream);
        RequireSupportedFormat(format);
        byte[] profileBytes = WriteProfile(profile, options);
        EmbeddedCarrierBuffer buffer = ReadStream(sourceStream,
            options.ResourceLimits.MaximumCarrierBytes);
        byte[] carrier = InsertCarrier(buffer.Bytes, profileBytes, format,
            options.ResourceLimits);
        WriteStreamResult(destinationStream, carrier);
    }

    internal void ReplaceStream(System.IO.Stream sourceStream,
        System.IO.Stream destinationStream, int profileIndex,
        IccProfile profile, IccEmbeddedFormat format, IccWriteOptions options)
    {
        ValidateStreams(sourceStream, destinationStream);
        RequireSupportedFormat(format);
        byte[] profileBytes = WriteProfile(profile, options);
        EmbeddedCarrierBuffer buffer = ReadStream(sourceStream,
            options.ResourceLimits.MaximumCarrierBytes);
        byte[] carrier = ReplaceCarrier(buffer.Bytes, profileIndex,
            profileBytes, format, options.ResourceLimits);
        WriteStreamResult(destinationStream, carrier);
    }

    internal void RemoveStream(System.IO.Stream sourceStream,
        System.IO.Stream destinationStream, int profileIndex,
        IccEmbeddedFormat format)
    {
        ValidateStreams(sourceStream, destinationStream);
        RequireSupportedFormat(format);
        IccResourceLimits limits = new IccResourceLimits();
        EmbeddedCarrierBuffer buffer = ReadStream(sourceStream,
            limits.MaximumCarrierBytes);
        byte[] carrier = RemoveCarrier(buffer.Bytes, profileIndex, format,
            limits);
        WriteStreamResult(destinationStream, carrier);
    }

    internal void WriteStreamResult(System.IO.Stream destinationStream,
        byte[] carrier)
    {
        destinationStream.Write(carrier, 0, carrier.Length);
        if (destinationStream.CanSeek)
        {
            destinationStream.SetLength(destinationStream.Position);
        }
    }

    internal IccOperationResult ProbeOperation(IccEmbeddedFormat format,
        IccEmbeddedOperation operation)
    {
        if (format < IccEmbeddedFormat.Unknown
            || format > IccEmbeddedFormat.Unsupported
            || operation < IccEmbeddedOperation.Detect
            || operation > IccEmbeddedOperation.Remove)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "The carrier format or embedding operation is invalid.");
        }
        if (format == IccEmbeddedFormat.Jpeg
            || format == IccEmbeddedFormat.Png
            || format == IccEmbeddedFormat.Tiff
            || format == IccEmbeddedFormat.BigTiff
            || format == IccEmbeddedFormat.Dng
            || format == IccEmbeddedFormat.WebP)
        {
            return CreateResult(IccOperationStatus.Success,
                $"{format} ICC detection, enumeration, extraction, insertion, replacement, and removal are supported.");
        }
        if (format == IccEmbeddedFormat.Jpeg2000
            && operation != IccEmbeddedOperation.Insert
            && operation != IccEmbeddedOperation.Remove)
        {
            return CreateResult(IccOperationStatus.Success,
                "JPEG 2000 ICC detection, enumeration, extraction, and replacement are supported.");
        }
        if ((format == IccEmbeddedFormat.Avif
                || format == IccEmbeddedFormat.Heif)
            && operation != IccEmbeddedOperation.Insert
            && operation != IccEmbeddedOperation.Remove)
        {
            return CreateResult(IccOperationStatus.Success,
                $"{format} ICC detection, enumeration, extraction, and replacement are supported.");
        }
        if (format == IccEmbeddedFormat.Unsupported)
        {
            return CreateResult(IccOperationStatus.UnsupportedFeature,
                "Document-carrier ICC integration is not implemented. Use a format-specific parser to extract or update the carrier and IccProfileReader or IccProfileWriter for raw ICC profile bytes.");
        }
        return CreateResult(IccOperationStatus.UnsupportedFeature,
            $"The {operation} operation is not available for {format} carriers.");
    }

    internal byte[] WriteProfile(IccProfile profile, IccWriteOptions options)
    {
        if (profile == null || options == null || options.ResourceLimits == null)
        {
            throw new IccProfileWriteException(
                "A profile, write options, and resource limits are required for embedding.");
        }
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile, options);
        return ProfileWriterCore.EncodePlan(plan);
    }

    internal EmbeddedCarrierBuffer ReadStream(System.IO.Stream sourceStream,
        int maximumBytes)
    {
        EmbeddedProfileReaderCore reader = new EmbeddedProfileReaderCore();
        EmbeddedCarrierBuffer buffer = reader.ReadCarrierStream(sourceStream,
            maximumBytes);
        if (buffer.LimitExceeded)
        {
            throw new IccEmbeddingException(
                $"The carrier exceeds the configured maximum of {maximumBytes} bytes.");
        }
        return buffer;
    }

    internal void ValidateStreams(System.IO.Stream sourceStream,
        System.IO.Stream destinationStream)
    {
        if (sourceStream == null || destinationStream == null)
        {
            throw new IccProfileValidationException(
                "Source and destination carrier streams cannot be null.");
        }
        if (!sourceStream.CanRead)
        {
            throw new IccEmbeddingException(
                "The source carrier stream is not readable.");
        }
        if (!destinationStream.CanWrite)
        {
            throw new IccEmbeddingException(
                "The destination carrier stream is not writable.");
        }
        if (object.ReferenceEquals(sourceStream, destinationStream))
        {
            throw new IccEmbeddingException(
                "Carrier stream mutation requires distinct source and destination streams.");
        }
    }

    internal void RequireSupportedFormat(IccEmbeddedFormat format)
    {
        if (format != IccEmbeddedFormat.Jpeg
            && format != IccEmbeddedFormat.Png
            && format != IccEmbeddedFormat.Tiff
            && format != IccEmbeddedFormat.BigTiff
            && format != IccEmbeddedFormat.Dng
            && format != IccEmbeddedFormat.WebP
            && format != IccEmbeddedFormat.Jpeg2000
            && format != IccEmbeddedFormat.Avif
            && format != IccEmbeddedFormat.Heif)
        {
            if (format == IccEmbeddedFormat.Unsupported)
            {
                throw new IccEmbeddingException(
                    "Document-carrier ICC mutation is integration-only and is not implemented. Use a format-specific parser to update the carrier and IccProfileReader or IccProfileWriter for raw ICC profile bytes.");
            }
            throw new IccEmbeddingException(
                "This embedding operation requires a JPEG, PNG, TIFF, BigTIFF, DNG, WebP, JPEG 2000, AVIF, or HEIF carrier format.");
        }
    }

    internal byte[] InsertCarrier(System.ReadOnlySpan<byte> carrierBytes,
        byte[] profileBytes, IccEmbeddedFormat format,
        IccResourceLimits limits)
    {
        if (format == IccEmbeddedFormat.Png)
        {
            return PngEmbeddingCore.InsertProfile(carrierBytes, profileBytes,
                limits);
        }
        if (format == IccEmbeddedFormat.Tiff
            || format == IccEmbeddedFormat.BigTiff
            || format == IccEmbeddedFormat.Dng)
        {
            return TiffEmbeddingCore.InsertProfile(carrierBytes, profileBytes,
                format, limits);
        }
        if (format == IccEmbeddedFormat.WebP)
        {
            return WebPEmbeddingCore.InsertProfile(carrierBytes, profileBytes,
                limits);
        }
        if (format == IccEmbeddedFormat.Jpeg2000)
        {
            throw new IccEmbeddingException(
                "JPEG 2000 ICC profile insertion is not supported; only replacement is available.");
        }
        if (format == IccEmbeddedFormat.Avif
            || format == IccEmbeddedFormat.Heif)
        {
            throw new IccEmbeddingException(
                $"{format} ICC profile insertion is not supported; only replacement is available.");
        }
        return JpegEmbeddingCore.InsertProfile(carrierBytes, profileBytes,
            limits);
    }

    internal byte[] ReplaceCarrier(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, byte[] profileBytes, IccEmbeddedFormat format,
        IccResourceLimits limits)
    {
        if (format == IccEmbeddedFormat.Png)
        {
            return PngEmbeddingCore.ReplaceProfile(carrierBytes, profileIndex,
                profileBytes, limits);
        }
        if (format == IccEmbeddedFormat.Tiff
            || format == IccEmbeddedFormat.BigTiff
            || format == IccEmbeddedFormat.Dng)
        {
            return TiffEmbeddingCore.ReplaceProfile(carrierBytes,
                profileIndex, profileBytes, format, limits);
        }
        if (format == IccEmbeddedFormat.WebP)
        {
            return WebPEmbeddingCore.ReplaceProfile(carrierBytes,
                profileIndex, profileBytes, limits);
        }
        if (format == IccEmbeddedFormat.Jpeg2000)
        {
            return Jpeg2000EmbeddingCore.ReplaceProfile(carrierBytes,
                profileIndex, profileBytes, limits);
        }
        if (format == IccEmbeddedFormat.Avif
            || format == IccEmbeddedFormat.Heif)
        {
            return IsoBmffEmbeddingCore.ReplaceProfile(carrierBytes,
                profileIndex, profileBytes, format, limits);
        }
        return JpegEmbeddingCore.ReplaceProfile(carrierBytes, profileIndex,
            profileBytes, limits);
    }

    internal byte[] RemoveCarrier(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, IccEmbeddedFormat format, IccResourceLimits limits)
    {
        if (format == IccEmbeddedFormat.Png)
        {
            return PngEmbeddingCore.RemoveProfile(carrierBytes, profileIndex,
                limits);
        }
        if (format == IccEmbeddedFormat.Tiff
            || format == IccEmbeddedFormat.BigTiff
            || format == IccEmbeddedFormat.Dng)
        {
            return TiffEmbeddingCore.RemoveProfile(carrierBytes, profileIndex,
                format, limits);
        }
        if (format == IccEmbeddedFormat.WebP)
        {
            return WebPEmbeddingCore.RemoveProfile(carrierBytes,
                profileIndex, limits);
        }
        if (format == IccEmbeddedFormat.Jpeg2000)
        {
            throw new IccEmbeddingException(
                "JPEG 2000 ICC profile removal is not supported; only replacement is available.");
        }
        if (format == IccEmbeddedFormat.Avif
            || format == IccEmbeddedFormat.Heif)
        {
            throw new IccEmbeddingException(
                $"{format} ICC profile removal is not supported; only replacement is available.");
        }
        return JpegEmbeddingCore.RemoveProfile(carrierBytes, profileIndex,
            limits);
    }

    internal void RequireCarrierLimit(int carrierLength, int maximumBytes)
    {
        if (maximumBytes < 0)
        {
            throw new IccProfileValidationException(
                "The maximum carrier size cannot be negative.");
        }
        if (carrierLength > maximumBytes)
        {
            throw new IccEmbeddingException(
                $"The carrier contains {carrierLength} bytes, exceeding the configured maximum of {maximumBytes} bytes.");
        }
    }

    internal IccOperationResult CreateResult(IccOperationStatus status,
        string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }
}
