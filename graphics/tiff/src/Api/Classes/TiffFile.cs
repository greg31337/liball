using System;
using System.IO;

namespace tiff;

/// <summary>Represents an opened TIFF or BigTIFF file whose header and image directories have been parsed.</summary>
/// <remarks>Tag values and image segments remain backed by a shared random-access source and are read only when requested.</remarks>
public class TiffFile : ITiffFile
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    internal bool Success;
    /// <summary>Indicates whether the file or writer is still open for operations.</summary>
    internal bool IsOpen;
    internal TiffHeader Header;
    internal TiffDirectory[] Directories;
    internal ITiffRandomAccessSource Source;
    internal TiffReaderLifetime Lifetime;
    internal TiffDiagnostics Diagnostics;
    internal TiffReadOptions Options;
    internal bool TrailingRangeLocated;
    internal TiffByteRange TrailingRange;

    internal TiffFile()
    {
        Success = false;
        IsOpen = false;
        Header = new TiffHeader();
        Directories = new TiffDirectory[0];
        Source = new TiffMemorySource(new byte[0]);
        Lifetime = new TiffReaderLifetime(Source, true, false);
        Diagnostics = new TiffDiagnostics();
        Options = new TiffReadOptions();
        TrailingRangeLocated = false;
        TrailingRange = new TiffByteRange();
    }

    /// <summary>Indicates whether the most recent operation completed without error diagnostics.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    public bool GetSuccess()
    {
        return Success && !Diagnostics.HasErrors();
    }

    /// <summary>Indicates whether image access remains enabled.</summary>
    /// <returns><c>true</c> while the file is open.</returns>
    public bool GetIsOpen()
    {
        return IsOpen && Lifetime != null && Lifetime.IsOpen && Source != null && Source.GetIsOpen();
    }

    /// <summary>Indicates whether image payload reads are enabled.</summary>
    /// <returns><c>true</c> for a live full-reader handle.</returns>
    public bool GetCanReadImageData()
    {
        return GetIsOpen() && Options.ReadImageData;
    }

    internal static TiffFile Create(TiffHeader header, TiffDirectory[] directories, ITiffRandomAccessSource source, TiffDiagnostics diagnostics, TiffReadOptions options, bool closeSource)
    {
        TiffFile value = new TiffFile();
        value.Initialize(header, directories, source, diagnostics, options, closeSource);
        return value;
    }

    internal void Initialize(TiffHeader header, TiffDirectory[] directories, ITiffRandomAccessSource source, TiffDiagnostics diagnostics, TiffReadOptions options, bool closeSource)
    {
        Header = CopyHeader(header);
        Directories = TiffDirectoryReadResult.CopyDirectories(directories);
        if (source == null)
        {
            Source = new TiffMemorySource(new byte[0]);
        }
        else
        {
            Source = source;
        }
        Diagnostics = diagnostics;
        Options = NormalizeOptions(options);
        Success = !Diagnostics.HasErrors();
        IsOpen = Success;
        Lifetime = new TiffReaderLifetime(Source, closeSource, Success);
        TrailingRangeLocated = false;
        TrailingRange = new TiffByteRange();
        if (!Success)
        {
            Lifetime.CloseAfterFailedOpen();
        }
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(byte[] fileBytes)
    {
        return OpenRead(fileBytes, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(byte[] fileBytes, TiffReadOptions options)
    {
        if (fileBytes == null)
        {
            return OpenRead(ReadOnlySpan<byte>.Empty, options);
        }

        return OpenRead(fileBytes.AsSpan(), options);
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(ReadOnlySpan<byte> fileBytes)
    {
        return OpenRead(fileBytes, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = TiffDirectoryReader.GetLimits(actualOptions);
        if (!limits.AllowsAllocation(fileBytes.Length))
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "The TIFF memory input exceeds the configured allocation limit.");
            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        byte[] bytes = TiffIfdEntry.CopyBytes(fileBytes);
        return OpenReadOwnedSource(new TiffMemorySource(bytes), actualOptions);
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(string path)
    {
        return OpenRead(path, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(string path, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (string.IsNullOrWhiteSpace(path))
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A file path is required to open a TIFF file.");
            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        if (!File.Exists(path))
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "The TIFF file path does not exist.");
            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        return OpenReadOwnedSource(new TiffFileSource(path), actualOptions);
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(Stream stream)
    {
        return OpenRead(stream, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source and parses its header and image-directory chain into memory.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenRead(Stream stream, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (stream == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A stream is required to open a TIFF file.");
            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        if (!stream.CanRead)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "The TIFF stream must be readable.");
            if (!actualOptions.LeaveOpen)
            {
                stream.Dispose();
            }

            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        if (!stream.CanSeek)
        {
            diagnostics.AddError(TiffErrorCode.NonSeekableStream, "The TIFF stream must be seekable for random access.");
            if (!actualOptions.LeaveOpen)
            {
                stream.Dispose();
            }

            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        return OpenReadSource(new TiffStreamSource(stream), actualOptions);
    }

    internal static TiffFile OpenReadSource(ITiffRandomAccessSource source, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        return OpenReadCore(source, actualOptions, !actualOptions.LeaveOpen);
    }

    internal static TiffFile OpenReadOwnedSource(ITiffRandomAccessSource source, TiffReadOptions options)
    {
        return OpenReadCore(source, NormalizeOptions(options), true);
    }

    internal static TiffFile OpenReadCore(ITiffRandomAccessSource source, TiffReadOptions actualOptions, bool closeSource)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (source == null)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "An open random-access TIFF source is required.");
            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        if (!source.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "An open random-access TIFF source is required.");
            if (closeSource)
            {
                source.Close();
            }

            return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], new TiffMemorySource(new byte[0]), diagnostics, actualOptions, true);
        }

        if (actualOptions.GetMemoryPolicy().SourceLoadingMode == TiffSourceLoadingMode.Buffered)
        {
            TiffLimits limits = TiffDirectoryReader.GetLimits(actualOptions);
            long bufferedLength = source.GetLength();
            if (bufferedLength < 0 || bufferedLength > int.MaxValue || !limits.AllowsAllocation(bufferedLength))
            {
                diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "The TIFF source is too large to buffer under the configured allocation limit.");
                return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], source, diagnostics, actualOptions, closeSource);
            }

            byte[] bufferedBytes = TiffRandomAccessSourceReader.ReadBytes(source, 0, (int)bufferedLength, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffFile.Create(new TiffHeader(), new TiffDirectory[0], source, diagnostics, actualOptions, closeSource);
            }

            if (closeSource)
            {
                source.Close();
            }

            source = new TiffMemorySource(bufferedBytes);
            closeSource = true;
        }

        long sourceLength = source.GetLength();
        int headerByteCount = 16;
        if (sourceLength < headerByteCount)
        {
            headerByteCount = (int)sourceLength;
        }

        byte[] headerBytes = TiffRandomAccessSourceReader.ReadBytes(source, 0, headerByteCount, diagnostics);
        TiffHeaderReadResult headerResult = ReadHeader(headerBytes, actualOptions);
        diagnostics.Merge(headerResult.Diagnostics);
        if (!headerResult.Success)
        {
            return TiffFile.Create(headerResult.Header, new TiffDirectory[0], source, diagnostics, actualOptions, closeSource);
        }

        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadSource(headerResult.Header, source, actualOptions);
        diagnostics.Merge(directoryResult.Diagnostics);
        TiffFile file = TiffFile.Create(headerResult.Header, directoryResult.Directories, source, diagnostics, actualOptions, closeSource);
        file.ApplyTrailingDataPolicy();
        return file;
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(byte[] fileBytes)
    {
        return OpenMetadata(fileBytes, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(byte[] fileBytes, TiffReadOptions options)
    {
        if (fileBytes == null)
        {
            return OpenMetadata(ReadOnlySpan<byte>.Empty, options);
        }

        return OpenMetadata(fileBytes.AsSpan(), options);
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(ReadOnlySpan<byte> fileBytes)
    {
        return OpenMetadata(fileBytes, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeMetadataOptions(options);
        return OpenRead(fileBytes, actualOptions);
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(string path)
    {
        return OpenMetadata(path, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(string path, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeMetadataOptions(options);
        return OpenRead(path, actualOptions);
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(Stream stream)
    {
        return OpenMetadata(stream, new TiffReadOptions());
    }

    /// <summary>Opens a TIFF source, parses its metadata, and disables access to image payload bytes.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>An in-memory TIFF handle whose <c>Success</c> field and diagnostics describe the open operation.</returns>
    internal static TiffFile OpenMetadata(Stream stream, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeMetadataOptions(options);
        return OpenRead(stream, actualOptions);
    }

    internal static TiffFile OpenMetadataSource(ITiffRandomAccessSource source, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeMetadataOptions(options);
        return OpenReadSource(source, actualOptions);
    }

    internal static TiffHeaderReadResult ReadHeader(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        if (TiffHeader.IsClassicTiff(fileBytes))
        {
            return TiffHeader.ReadClassic(fileBytes, options);
        }

        if (TiffHeader.IsBigTiff(fileBytes))
        {
            return TiffHeader.ReadBigTiff(fileBytes, options);
        }

        if (fileBytes.Length >= 4)
        {
            return TiffHeader.ReadClassic(fileBytes, options);
        }

        return TiffHeader.ReadClassic(fileBytes, options);
    }

    /// <summary>Closes the handle and releases any owned stream resources; repeated calls have no additional effect.</summary>
    public void Close()
    {
        if (Lifetime != null)
        {
            Lifetime.Close();
        }

        IsOpen = false;
    }

    /// <summary>Returns the header.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffHeader GetHeader()
    {
        return CopyHeader(Header);
    }

    /// <summary>Returns the number of available get image entries.</summary>
    /// <returns>The requested value or model.</returns>
    public int GetImageCount()
    {
        return Directories.Length;
    }

    /// <summary>Returns the image.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <returns>The requested value or model.</returns>
    public TiffImage GetImage(int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= Directories.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
            Success = false;
            return new TiffImage();
        }

        if (!GetIsOpen())
        {
            Diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The TIFF file is closed.");
            Success = false;
        }

        return TiffImage.Create(Header, Directories[imageIndex], Lifetime, Diagnostics, Options);
    }

    /// <summary>Returns the images.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffImage[] GetImages()
    {
        if (!GetIsOpen())
        {
            Diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The TIFF file is closed.");
            Success = false;
        }

        TiffImage[] images = new TiffImage[Directories.Length];
        int index = 0;
        while (index < Directories.Length)
        {
            images[index] = TiffImage.Create(Header, Directories[index], Lifetime, Diagnostics, Options);
            index = index + 1;
        }

        return images;
    }

    /// <summary>Returns the number of top-level images marked as reduced-resolution images.</summary>
    /// <returns>The reduced-resolution image count.</returns>
    public int GetReducedResolutionImageCount()
    {
        int[] indexes = GetReducedResolutionIndexes();
        return indexes.Length;
    }

    /// <summary>Returns one top-level reduced-resolution image.</summary>
    /// <param name="reducedImageIndex">The zero-based index within the reduced-resolution image collection.</param>
    /// <returns>The selected image.</returns>
    public TiffImage GetReducedResolutionImage(int reducedImageIndex)
    {
        int[] indexes = GetReducedResolutionIndexes();
        if (reducedImageIndex < 0 || reducedImageIndex >= indexes.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Reduced-resolution image index is outside the available range.");
            Success = false;
            return new TiffImage();
        }

        return TiffImage.Create(Header, Directories[indexes[reducedImageIndex]], Lifetime, Diagnostics, Options);
    }

    /// <summary>Returns the number of top-level thumbnail or reduced-resolution images.</summary>
    /// <returns>The thumbnail count.</returns>
    public int GetThumbnailCount()
    {
        return GetReducedResolutionImageCount();
    }

    /// <summary>Returns one top-level thumbnail or reduced-resolution image.</summary>
    /// <param name="thumbnailIndex">The zero-based index within the thumbnail collection.</param>
    /// <returns>The selected image.</returns>
    public TiffImage GetThumbnail(int thumbnailIndex)
    {
        return GetReducedResolutionImage(thumbnailIndex);
    }

    /// <summary>Groups related top-level images into navigable series.</summary>
    /// <returns>The grouped series and diagnostics.</returns>
    public TiffSeriesGroupResult GetSeriesGroup()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The TIFF file is closed.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffSeriesGroupResult.Create(new TiffSeries[0], diagnostics);
        }

        TiffSeriesGroupResult result = TiffSeriesBuilder.GroupSource(Directories, Source, Header.ByteOrder, Options);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffSeriesGroupResult.Create(result.Series, result.Diagnostics);
    }

    /// <summary>Returns the number of grouped image series.</summary>
    /// <returns>The series count.</returns>
    public int GetSeriesCount()
    {
        return GetSeriesGroup().Series.Length;
    }

    /// <summary>Returns one grouped image series.</summary>
    /// <param name="seriesIndex">The zero-based series index.</param>
    /// <returns>The selected series.</returns>
    public TiffSeries GetSeriesByIndex(int seriesIndex)
    {
        TiffSeriesGroupResult result = GetSeriesGroup();
        if (seriesIndex < 0 || seriesIndex >= result.Series.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Series index is outside the available range.");
            Success = false;
            return new TiffSeries();
        }

        return result.Series[seriesIndex].Copy();
    }

    /// <summary>Returns the image at explicit channel, depth, and time coordinates in a series.</summary>
    /// <param name="seriesIndex">The zero-based series index.</param>
    /// <param name="channelIndex">The zero-based channel coordinate.</param>
    /// <param name="zIndex">The zero-based depth coordinate.</param>
    /// <param name="timeIndex">The zero-based time coordinate.</param>
    /// <returns>The selected plane image.</returns>
    public TiffImage GetSeriesPlaneByCoordinates(int seriesIndex, int channelIndex, int zIndex, int timeIndex)
    {
        TiffSeriesGroupResult result = GetSeriesGroup();
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (seriesIndex < 0 || seriesIndex >= result.Series.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Series index is outside the available range.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return new TiffImage();
        }

        TiffSeriesPlane plane = TiffSeriesBuilder.FindPlane(result.Series[seriesIndex], channelIndex, zIndex, timeIndex, diagnostics);
        Diagnostics.Merge(diagnostics);
        if (diagnostics.HasErrors())
        {
            Success = false;
            return new TiffImage();
        }

        Success = !Diagnostics.HasErrors();
        return TiffImage.Create(Header, plane.Directory, Lifetime, Diagnostics, Options);
    }

    /// <summary>Reads the OME dataset description and resolves its local pyramid relationships.</summary>
    /// <returns>The parsed OME dataset and diagnostics.</returns>
    public TiffOmeMetadataReadResult GetOmeDataset()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!EnsureOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The TIFF file is closed.");
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        int imageIndex = 0;
        while (imageIndex < Directories.Length)
        {
            TiffOmeMetadataReadResult result = TiffOmeMetadataReader.ReadSource(Directories[imageIndex], Source, Options.Limits);
            diagnostics.Merge(result.Diagnostics);
            if (result.Metadata.HasOmeXml)
            {
                TiffOmeMetadata metadata = TiffOmeDatasetNavigator.ResolvePyramids(result.Metadata, Directories, Source, Header.ByteOrder, Header.FileKind, Options, diagnostics);
                Diagnostics.Merge(diagnostics);
                Success = !Diagnostics.HasErrors();
                return TiffOmeMetadataReadResult.Create(metadata, diagnostics);
            }

            imageIndex = imageIndex + 1;
        }

        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
    }

    /// <summary>Returns the local TIFF image mapped to explicit OME channel, depth, and time coordinates.</summary>
    /// <param name="omeImageIndex">The zero-based OME image index.</param>
    /// <param name="channelIndex">The zero-based channel coordinate.</param>
    /// <param name="zIndex">The zero-based depth coordinate.</param>
    /// <param name="timeIndex">The zero-based time coordinate.</param>
    /// <returns>The mapped local TIFF image.</returns>
    public TiffImage GetOmePlaneByCoordinates(int omeImageIndex, int channelIndex, int zIndex, int timeIndex)
    {
        TiffOmeMetadataReadResult result = GetOmeDataset();
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffOmePlane plane = TiffOmeDatasetNavigator.FindPlane(result.Metadata, omeImageIndex, channelIndex, zIndex, timeIndex, diagnostics);
        if (!result.Success || diagnostics.HasErrors())
        {
            Diagnostics.Merge(diagnostics);
            Success = false;
            return new TiffImage();
        }

        if (plane.IsExternal)
        {
            Diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The requested OME plane belongs to an external TIFF file.");
            Success = false;
            return new TiffImage();
        }

        if (!plane.HasIfd || plane.IfdIndex < 0 || plane.IfdIndex >= Directories.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "The requested OME plane does not map to an available local TIFF IFD.");
            Success = false;
            return new TiffImage();
        }

        Success = !Diagnostics.HasErrors();
        return TiffImage.Create(Header, Directories[plane.IfdIndex], Lifetime, Diagnostics, Options);
    }

    internal int[] GetReducedResolutionIndexes()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The TIFF file is closed.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return new int[0];
        }

        int[] indexes = new int[0];
        int index = 0;
        while (index < Directories.Length)
        {
            TiffImageInfoResult infoResult = TiffSourceImageInfoExtractor.Extract(Source, Directories[index], Header.ByteOrder, Options);
            diagnostics.Merge(infoResult.Diagnostics);
            if (infoResult.Info.IsReducedResolution)
            {
                int[] expanded = new int[indexes.Length + 1];
                int copyIndex = 0;
                while (copyIndex < indexes.Length)
                {
                    expanded[copyIndex] = indexes[copyIndex];
                    copyIndex = copyIndex + 1;
                }

                expanded[indexes.Length] = index;
                indexes = expanded;
            }

            index = index + 1;
        }

        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return indexes;
    }

    /// <summary>Returns the directory.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <returns>The requested value or model.</returns>
    public TiffDirectory GetDirectory(int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= Directories.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Image index is outside the available image range.");
            Success = false;
            return new TiffDirectory();
        }

        return Directories[imageIndex].Copy();
    }

    /// <summary>Returns the directories.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffDirectory[] GetDirectories()
    {
        return TiffDirectoryReadResult.CopyDirectories(Directories);
    }

    /// <summary>Returns the diagnostics.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffDiagnostics GetDiagnostics()
    {
        return Diagnostics;
    }

    /// <summary>Returns the shared random-access source backing this file.</summary>
    /// <returns>The source used for deferred reads.</returns>
    public ITiffRandomAccessSource GetSource()
    {
        return Source;
    }

    /// <summary>Returns the preserved trailing-data byte range.</summary>
    /// <returns>The range following all referenced TIFF structures and payloads.</returns>
    public TiffByteRange GetTrailingDataRange()
    {
        if (!EnsureOpen() || !Options.PreserveTrailingData)
        {
            return new TiffByteRange();
        }

        return LocateTrailingDataRange();
    }

    internal TiffByteRange LocateTrailingDataRange()
    {
        if (!TrailingRangeLocated)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            TrailingRange = TiffTrailingDataLocator.Locate(Header, Directories, Source, Options, diagnostics);
            Diagnostics.Merge(diagnostics);
            Success = !Diagnostics.HasErrors();
            TrailingRangeLocated = true;
        }

        return TrailingRange.Copy();
    }

    internal void ApplyTrailingDataPolicy()
    {
        if (!GetIsOpen() || Options.PreserveTrailingData)
        {
            return;
        }

        TiffByteRange range = LocateTrailingDataRange();
        if (!Diagnostics.HasErrors() && range.Offset <= long.MaxValue)
        {
            Lifetime.VisibleLength = (long)range.Offset;
        }
    }

    /// <summary>Returns preserved trailing bytes.</summary>
    /// <returns>A detached trailing-data byte array.</returns>
    public byte[] GetTrailingData()
    {
        TiffByteRange range = GetTrailingDataRange();
        if (range.ByteCount == 0 || Diagnostics.HasErrors())
        {
            return new byte[0];
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        if (range.ByteCount > int.MaxValue || !limits.AllowsAllocation((long)range.ByteCount))
        {
            Diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Trailing data exceeds the configured allocation limit.");
            Success = false;
            return new byte[0];
        }

        byte[] bytes = TiffRandomAccessSourceReader.ReadBytes(Source, (long)range.Offset, (int)range.ByteCount, Diagnostics);
        Success = !Diagnostics.HasErrors();
        return bytes;
    }

    /// <summary>Reads preserved trailing bytes into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics ReadTrailingData(Span<byte> destination)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffByteRange range = GetTrailingDataRange();
        if (Diagnostics.HasErrors())
        {
            diagnostics.Merge(Diagnostics);
            return diagnostics;
        }

        if (range.ByteCount > int.MaxValue || destination.Length < (int)range.ByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Destination is shorter than the preserved trailing data.");
        }
        else
        {
            TiffRandomAccessSourceReader.ReadToBuffer(Source, (long)range.Offset, destination.Slice(0, (int)range.ByteCount), diagnostics);
        }

        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return diagnostics;
    }

    /// <summary>Returns the file bytes.</summary>
    /// <returns>The requested value or model.</returns>
    public byte[] GetFileBytes()
    {
        if (!EnsureOpen())
        {
            return new byte[0];
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (Lifetime.VisibleLength < 0 || Lifetime.VisibleLength > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "The visible TIFF data is too large to return as one byte array.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return new byte[0];
        }

        byte[] bytes = TiffRandomAccessSourceReader.ReadBytes(Source, 0, (int)Lifetime.VisibleLength, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return bytes;
    }

    /// <summary>Returns the number of available get file byte entries.</summary>
    /// <returns>The requested value or model.</returns>
    public int GetFileByteCount()
    {
        if (!GetIsOpen() || Source == null || !Source.GetIsOpen())
        {
            return 0;
        }

        long length = Lifetime.VisibleLength;
        if (length > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)length;
    }

    internal bool EnsureOpen()
    {
        if (GetIsOpen())
        {
            return true;
        }

        Diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The TIFF file is closed.");
        Success = false;
        return false;
    }

    internal static TiffHeader CopyHeader(TiffHeader header)
    {
        if (header == null)
        {
            return new TiffHeader();
        }

        return TiffHeader.Create(header.ByteOrder, header.FileKind, header.FirstIfdOffset);
    }

    internal static TiffReadOptions NormalizeOptions(TiffReadOptions options)
    {
        if (options == null)
        {
            return new TiffReadOptions();
        }

        return options.Copy();
    }

    internal static TiffReadOptions NormalizeMetadataOptions(TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        actualOptions.ReadImageData = false;
        return actualOptions;
    }
}
