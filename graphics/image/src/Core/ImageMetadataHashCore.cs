using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace image;

/// <summary>Computes hashes from selected, safely separable metadata source ranges.</summary>
internal static class ImageMetadataHashCore
{
    internal const int StreamBufferLength = 8192;

    public static string ComputeSha256(byte[]? sourceBytes, ImageMetadataDocument? document)
    {
        return ComputeHash(sourceBytes, document, "SHA-256");
    }

    public static string ComputeHash(byte[]? sourceBytes, ImageMetadataDocument? document, string? algorithmName)
    {
        ImageMetadataHashOptions options = new ImageMetadataHashOptions();
        options.AlgorithmName = algorithmName == null ? "SHA-256" : algorithmName;
        ImageMetadataHashResult result = ComputeBytes(sourceBytes, document, options);
        if (result.Status != ImageMetadataHashStatus.Success)
        {
            return "";
        }
        return result.HexDigest;
    }

    internal static ImageMetadataHashResult ComputeBytes(byte[]? sourceBytes, ImageMetadataDocument? document, ImageMetadataHashOptions? options)
    {
        if (sourceBytes == null)
        {
            ImageMetadataHashResult invalid = PrepareResult(options);
            if (invalid.Status == ImageMetadataHashStatus.Unknown)
            {
                SetInvalidInput(invalid, "Metadata hash source byte array was null.");
            }
            return invalid;
        }
        return ComputeSpan(sourceBytes, document, options);
    }

    internal static ImageMetadataHashResult ComputeSpan(ReadOnlySpan<byte> sourceBytes, ImageMetadataDocument? document, ImageMetadataHashOptions? options)
    {
        ImageMetadataHashResult result = PrepareResult(options);
        if (result.Status != ImageMetadataHashStatus.Unknown)
        {
            return result;
        }
        if (document == null)
        {
            SetInvalidInput(result, "Metadata hash document was null.");
            return result;
        }

        ImageMetadataHashOptions actualOptions = EffectiveOptions(options);
        ImageMetadataHashCollection collection = CollectSelectedRanges(sourceBytes.Length, document, actualOptions);
        if (!SelectionCanHash(result, collection))
        {
            return result;
        }

        using (IncrementalHash hash = CreateIncrementalHash(result.AlgorithmName))
        {
            foreach (ImageMetadataHashRange range in collection.Ranges)
            {
                hash.AppendData(sourceBytes.Slice((int)range.Offset, (int)range.Length));
            }
            FinishSuccess(result, collection.Ranges, hash.GetHashAndReset());
        }
        return result;
    }

    internal static ImageMetadataHashResult ComputeStream(Stream? sourceStream, ImageMetadataDocument? document, ImageMetadataHashOptions? options)
    {
        ImageMetadataHashResult result = PrepareResult(options);
        if (result.Status != ImageMetadataHashStatus.Unknown)
        {
            return result;
        }
        if (sourceStream == null || document == null)
        {
            SetInvalidInput(result, "Metadata hash stream or document was null.");
            return result;
        }
        if (!sourceStream.CanRead)
        {
            SetInvalidInput(result, "Metadata hash stream was not readable.");
            return result;
        }

        long initialPosition = 0;
        long sourceLength = document.ByteLength > 0 ? document.ByteLength : long.MaxValue;
        if (sourceStream.CanSeek)
        {
            initialPosition = sourceStream.Position;
            sourceLength = sourceStream.Length - initialPosition;
            if (sourceLength < 0)
            {
                SetInvalidInput(result, "Metadata hash stream position exceeded its length.");
                return result;
            }
        }

        ImageMetadataHashOptions actualOptions = EffectiveOptions(options);
        ImageMetadataHashCollection collection = CollectSelectedRanges(sourceLength, document, actualOptions);
        if (!SelectionCanHash(result, collection))
        {
            return result;
        }

        bool completed;
        byte[] digest;
        using (IncrementalHash hash = CreateIncrementalHash(result.AlgorithmName))
        {
            completed = AppendStreamRanges(sourceStream, initialPosition, collection.Ranges, hash, result.Diagnostics);
            digest = completed ? hash.GetHashAndReset() : new byte[0];
        }
        if (sourceStream.CanSeek)
        {
            sourceStream.Position = initialPosition;
        }
        if (!completed)
        {
            result.Status = ImageMetadataHashStatus.InvalidInput;
            return result;
        }

        FinishSuccess(result, collection.Ranges, digest);
        return result;
    }

    internal static ImageMetadataHashResult PrepareResult(ImageMetadataHashOptions? options)
    {
        ImageMetadataHashResult result = new ImageMetadataHashResult();
        ImageMetadataHashOptions actual = EffectiveOptions(options);
        string canonical = CanonicalAlgorithm(actual.AlgorithmName);
        result.AlgorithmName = canonical.Length == 0 ? SafeText(actual.AlgorithmName) : canonical;
        if (canonical.Length == 0)
        {
            result.Status = ImageMetadataHashStatus.UnsupportedAlgorithm;
            MetadataDiagnostic.AddUnsupported(result.Diagnostics, "metadata-hash.algorithm.unsupported", "Metadata hash algorithm was unsupported.", "MetadataHash");
        }
        return result;
    }

    internal static ImageMetadataHashOptions EffectiveOptions(ImageMetadataHashOptions? options)
    {
        return options == null ? new ImageMetadataHashOptions() : options;
    }

    internal static bool SelectionCanHash(ImageMetadataHashResult result, ImageMetadataHashCollection collection)
    {
        if (collection.CandidateCount == 0)
        {
            result.Status = ImageMetadataHashStatus.EmptyMetadata;
            MetadataDiagnostic.Add(result.Diagnostics, MetadataSeverity.Info, "metadata-hash.metadata.empty", "No metadata matched the requested hash selection.");
            return false;
        }
        if (collection.UnsupportedCount > 0 || collection.Ranges.Count == 0)
        {
            result.Status = ImageMetadataHashStatus.UnsupportedSeparation;
            MetadataDiagnostic.AddUnsupported(result.Diagnostics, "metadata-hash.separation.unsupported", "Selected metadata could not be safely separated from other source bytes.", "MetadataHash");
            return false;
        }
        return true;
    }

    internal static void SetInvalidInput(ImageMetadataHashResult result, string message)
    {
        result.Status = ImageMetadataHashStatus.InvalidInput;
        MetadataDiagnostic.AddRecoverableError(result.Diagnostics, "metadata-hash.input.invalid", message);
    }

    internal static void FinishSuccess(ImageMetadataHashResult result, List<ImageMetadataHashRange> ranges, byte[] digest)
    {
        result.Status = ImageMetadataHashStatus.Success;
        result.HexDigest = HashHelper.ToHex(digest);
        result.RangeCount = ranges.Count;
        long byteCount = 0;
        foreach (ImageMetadataHashRange range in ranges)
        {
            byteCount = byteCount + range.Length;
        }
        result.HashedByteCount = byteCount;
    }

    internal static ImageMetadataHashCollection CollectSelectedRanges(long sourceLength, ImageMetadataDocument document, ImageMetadataHashOptions options)
    {
        ImageMetadataHashCollection collection = new ImageMetadataHashCollection();
        if (sourceLength < 0 || document == null || options == null)
        {
            return collection;
        }

        if (options.IncludeDirectories)
        {
            CollectDirectoryRanges(collection, document.Directories, sourceLength, options);
            if (document.Frames != null)
            {
                foreach (ImageMetadataFrame frame in document.Frames)
                {
                    if (frame != null)
                    {
                        CollectDirectoryRanges(collection, frame.Directories, sourceLength, options);
                    }
                }
            }
        }
        if (options.IncludeProfiles)
        {
            CollectProfileRanges(collection, document.Profiles, sourceLength, options);
        }
        if (options.IncludeRawBlocks)
        {
            CollectRawBlockRanges(collection, document.RawBlocks, sourceLength, options);
        }

        SortRanges(collection.Ranges);
        collection.Ranges = MergeRanges(collection.Ranges);
        return collection;
    }

    internal static void CollectDirectoryRanges(ImageMetadataHashCollection collection, List<MetadataDirectory> directories, long sourceLength, ImageMetadataHashOptions options)
    {
        if (collection == null || directories == null)
        {
            return;
        }
        foreach (MetadataDirectory directory in directories)
        {
            if (directory != null)
            {
                CollectDirectoryRange(collection, directory, sourceLength, options);
                CollectDirectoryRanges(collection, directory.Children, sourceLength, options);
            }
        }
    }

    internal static void CollectDirectoryRange(ImageMetadataHashCollection collection, MetadataDirectory directory, long sourceLength, ImageMetadataHashOptions options)
    {
        if (!ShouldIncludeDirectory(directory))
        {
            if (directory.Family == MetadataFamily.Svg && DirectoryHasSelection(directory, options))
            {
                collection.CandidateCount = collection.CandidateCount + 1;
                collection.UnsupportedCount = collection.UnsupportedCount + 1;
            }
            return;
        }

        bool directorySelected = FamilySelected(directory.Family, options.Families);
        bool hasCandidate = directorySelected && directory.Length > 0;
        int before = collection.Ranges.Count;
        if (hasCandidate)
        {
            AddRange(collection.Ranges, directory.Offset, directory.Length, sourceLength);
        }
        if (directory.Tags != null)
        {
            foreach (MetadataTag tag in directory.Tags)
            {
                if (tag != null && TagSelected(tag, directory, options))
                {
                    hasCandidate = true;
                    AddRange(collection.Ranges, tag.Offset, tag.Length, sourceLength);
                }
            }
        }
        if (hasCandidate)
        {
            collection.CandidateCount = collection.CandidateCount + 1;
            if (collection.Ranges.Count == before)
            {
                collection.UnsupportedCount = collection.UnsupportedCount + 1;
            }
        }
    }

    internal static void CollectProfileRanges(ImageMetadataHashCollection collection, List<MetadataProfile> profiles, long sourceLength, ImageMetadataHashOptions options)
    {
        if (collection == null || profiles == null)
        {
            return;
        }
        foreach (MetadataProfile profile in profiles)
        {
            if (profile != null && profile.Kind != MetadataProfileKind.Thumbnail && FamilySelected(profile.Family, options.Families) && ProfileKindSelected(profile.Kind, options.ProfileKinds))
            {
                collection.CandidateCount = collection.CandidateCount + 1;
                if (!AddRange(collection.Ranges, profile.Offset, profile.Length, sourceLength))
                {
                    collection.UnsupportedCount = collection.UnsupportedCount + 1;
                }
            }
        }
    }

    internal static void CollectRawBlockRanges(ImageMetadataHashCollection collection, List<MetadataRawBlock> blocks, long sourceLength, ImageMetadataHashOptions options)
    {
        if (collection == null || blocks == null)
        {
            return;
        }
        foreach (MetadataRawBlock block in blocks)
        {
            if (block != null && FamilySelected(block.Family, options.Families) && RawBlockTypeSelected(block.BlockType, options.RawBlockTypes))
            {
                if (ShouldIncludeRawBlock(block))
                {
                    collection.CandidateCount = collection.CandidateCount + 1;
                    if (!AddRange(collection.Ranges, block.Offset, block.Length, sourceLength))
                    {
                        collection.UnsupportedCount = collection.UnsupportedCount + 1;
                    }
                }
                else if (block.Family == MetadataFamily.Svg)
                {
                    collection.CandidateCount = collection.CandidateCount + 1;
                    collection.UnsupportedCount = collection.UnsupportedCount + 1;
                }
            }
        }
    }

    internal static bool DirectoryHasSelection(MetadataDirectory directory, ImageMetadataHashOptions options)
    {
        if (FamilySelected(directory.Family, options.Families) && directory.Length > 0)
        {
            return true;
        }
        if (directory.Tags != null)
        {
            foreach (MetadataTag tag in directory.Tags)
            {
                if (tag != null && TagSelected(tag, directory, options))
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool TagSelected(MetadataTag tag, MetadataDirectory directory, ImageMetadataHashOptions options)
    {
        MetadataFamily family = tag.Family == MetadataFamily.Unknown ? directory.Family : tag.Family;
        return FamilySelected(family, options.Families);
    }

    internal static bool FamilySelected(MetadataFamily family, MetadataFamily[]? families)
    {
        if (families == null || families.Length == 0)
        {
            return true;
        }
        foreach (MetadataFamily expected in families)
        {
            if (family == expected)
            {
                return true;
            }
        }
        return false;
    }

    internal static bool ProfileKindSelected(MetadataProfileKind kind, MetadataProfileKind[]? kinds)
    {
        if (kinds == null || kinds.Length == 0)
        {
            return true;
        }
        foreach (MetadataProfileKind expected in kinds)
        {
            if (kind == expected)
            {
                return true;
            }
        }
        return false;
    }

    internal static bool RawBlockTypeSelected(string blockType, string[]? blockTypes)
    {
        if (blockTypes == null || blockTypes.Length == 0)
        {
            return true;
        }
        foreach (string expected in blockTypes)
        {
            if (string.Equals(blockType, expected, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool AppendStreamRanges(Stream stream, long initialPosition, List<ImageMetadataHashRange> ranges, IncrementalHash hash, List<MetadataDiagnostic> diagnostics)
    {
        byte[] buffer = new byte[StreamBufferLength];
        long current = 0;
        foreach (ImageMetadataHashRange range in ranges)
        {
            if (stream.CanSeek)
            {
                stream.Position = initialPosition + range.Offset;
            }
            else
            {
                long skip = range.Offset - current;
                if (!SkipStreamBytes(stream, skip, buffer, diagnostics))
                {
                    return false;
                }
            }
            if (!ReadStreamRange(stream, range.Length, buffer, hash, diagnostics))
            {
                return false;
            }
            current = range.Offset + range.Length;
        }
        return true;
    }

    internal static bool SkipStreamBytes(Stream stream, long count, byte[] buffer, List<MetadataDiagnostic> diagnostics)
    {
        long remaining = count;
        while (remaining > 0)
        {
            int request = remaining > buffer.Length ? buffer.Length : (int)remaining;
            int read = stream.Read(buffer, 0, request);
            if (read == 0)
            {
                MetadataDiagnostic.AddTruncated(diagnostics, "metadata-hash.stream.truncated", "Metadata hash stream ended before a selected range.", "MetadataHash", 0, count);
                return false;
            }
            remaining = remaining - read;
        }
        return true;
    }

    internal static bool ReadStreamRange(Stream stream, long count, byte[] buffer, IncrementalHash hash, List<MetadataDiagnostic> diagnostics)
    {
        long remaining = count;
        while (remaining > 0)
        {
            int request = remaining > buffer.Length ? buffer.Length : (int)remaining;
            int read = stream.Read(buffer, 0, request);
            if (read == 0)
            {
                MetadataDiagnostic.AddTruncated(diagnostics, "metadata-hash.stream.truncated", "Metadata hash stream ended within a selected range.", "MetadataHash", 0, count);
                return false;
            }
            hash.AppendData(buffer, 0, read);
            remaining = remaining - read;
        }
        return true;
    }

    internal static IncrementalHash CreateIncrementalHash(string algorithmName)
    {
        if (SameText(algorithmName, "SHA-256")) return IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        if (SameText(algorithmName, "SHA-1")) return IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        return IncrementalHash.CreateHash(HashAlgorithmName.MD5);
    }

    internal static string CanonicalAlgorithm(string? name)
    {
        if (SameText(name, "SHA256") || SameText(name, "SHA-256")) return "SHA-256";
        if (SameText(name, "SHA1") || SameText(name, "SHA-1")) return "SHA-1";
        if (SameText(name, "MD5")) return "MD5";
        return "";
    }

    internal static bool ShouldIncludeDirectory(MetadataDirectory directory)
    {
        if (directory == null || directory.Family == MetadataFamily.Svg)
        {
            return false;
        }
        string group = SafeText(directory.Group);
        if (directory.Family == MetadataFamily.Png) return group != "IDAT" && group != "fdAT" && group != "PLTE" && group != "IEND";
        if (directory.Family == MetadataFamily.WebP) return group != "VP8 " && group != "VP8L" && group != "ALPH" && group != "ANMF";
        if (directory.Family == MetadataFamily.Gif) return group != "ImageData" && group != "Trailer";
        if (directory.Family == MetadataFamily.Bmff || directory.Family == MetadataFamily.Container) return group != "mdat" && group != "idat";
        if (directory.Family == MetadataFamily.Jpeg2000) return group != "jp2c";
        if (directory.Family == MetadataFamily.JpegXl) return group != "jxlc" && group != "jxlp";
        if (directory.Family == MetadataFamily.Psd) return group != "ImageData";
        return true;
    }

    internal static bool ShouldIncludeRawBlock(MetadataRawBlock block)
    {
        if (block == null || block.Family == MetadataFamily.Thumbnail || block.Family == MetadataFamily.Svg)
        {
            return false;
        }
        string type = SafeText(block.BlockType);
        string name = SafeText(block.Name);
        if (block.Family == MetadataFamily.Ico && type == "Item") return false;
        if (type == "JPEG" || type == "TIFFStrip") return false;
        return name.IndexOf("Thumbnail", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) < 0;
    }

    internal static bool AddRange(List<ImageMetadataHashRange> ranges, long offset, long length, long sourceLength)
    {
        if (ranges == null || offset < 0 || length <= 0 || offset > sourceLength || length > sourceLength - offset)
        {
            return false;
        }
        ImageMetadataHashRange range = new ImageMetadataHashRange();
        range.Offset = offset;
        range.Length = length;
        ranges.Add(range);
        return true;
    }

    internal static void SortRanges(List<ImageMetadataHashRange> ranges)
    {
        if (ranges == null) return;
        int index = 1;
        while (index < ranges.Count)
        {
            ImageMetadataHashRange item = ranges[index];
            int position = index - 1;
            while (position >= 0 && RangeAfter(ranges[position], item))
            {
                ranges[position + 1] = ranges[position];
                position = position - 1;
            }
            ranges[position + 1] = item;
            index = index + 1;
        }
    }

    internal static bool RangeAfter(ImageMetadataHashRange left, ImageMetadataHashRange right)
    {
        if (left.Offset != right.Offset) return left.Offset > right.Offset;
        return left.Length > right.Length;
    }

    internal static List<ImageMetadataHashRange> MergeRanges(List<ImageMetadataHashRange> ranges)
    {
        List<ImageMetadataHashRange> merged = new List<ImageMetadataHashRange>();
        if (ranges == null) return merged;
        foreach (ImageMetadataHashRange range in ranges)
        {
            if (range != null)
            {
                if (merged.Count == 0)
                {
                    merged.Add(CopyRange(range));
                }
                else
                {
                    ImageMetadataHashRange previous = merged[merged.Count - 1];
                    long previousEnd = previous.Offset + previous.Length;
                    long rangeEnd = range.Offset + range.Length;
                    if (range.Offset <= previousEnd)
                    {
                        if (rangeEnd > previousEnd) previous.Length = rangeEnd - previous.Offset;
                    }
                    else
                    {
                        merged.Add(CopyRange(range));
                    }
                }
            }
        }
        return merged;
    }

    internal static ImageMetadataHashRange CopyRange(ImageMetadataHashRange range)
    {
        ImageMetadataHashRange copy = new ImageMetadataHashRange();
        copy.Offset = range.Offset;
        copy.Length = range.Length;
        return copy;
    }

    internal static bool SameText(string? left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static string SafeText(string text)
    {
        return text == null ? "" : text;
    }
}

internal class ImageMetadataHashCollection
{
    internal List<ImageMetadataHashRange> Ranges;
    internal int CandidateCount;
    internal int UnsupportedCount;

    internal ImageMetadataHashCollection()
    {
        Ranges = new List<ImageMetadataHashRange>();
        CandidateCount = 0;
        UnsupportedCount = 0;
    }
}

internal class ImageMetadataHashRange
{
    internal long Offset;
    internal long Length;

    internal ImageMetadataHashRange()
    {
        Offset = 0;
        Length = 0;
    }
}
