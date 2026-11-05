using System;
using System.Threading;

namespace tiff;

/// <summary>Provides image-level access to one TIFF directory, including its tags, subimages, and raw strip or tile data.</summary>
/// <remarks>An image shares its file's random-access source. Metadata-only opens deliberately reject raw strip and tile reads.</remarks>
public class TiffImage : ITiffImage
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    internal bool Success;
    internal int ImageIndex;
    internal TiffHeader Header;
    internal TiffDirectory Directory;
    internal ITiffRandomAccessSource Source;
    internal TiffReaderLifetime Lifetime;
    internal TiffDiagnostics Diagnostics;
    internal TiffReadOptions Options;
    internal TiffTagValueLoadResult[] CachedTagBytes;
    internal TiffTagValue[] CachedTagValues;
    internal bool[] TagBytesLoaded;
    internal bool[] TagValuesDecoded;

    internal TiffImage()
    {
        Success = false;
        ImageIndex = TiffDiagnostic.NoImageIndex;
        Header = new TiffHeader();
        Directory = new TiffDirectory();
        Source = new TiffMemorySource(new byte[0]);
        Lifetime = new TiffReaderLifetime(Source, true, false);
        Diagnostics = new TiffDiagnostics();
        Options = new TiffReadOptions();
        CachedTagBytes = new TiffTagValueLoadResult[0];
        CachedTagValues = new TiffTagValue[0];
        TagBytesLoaded = new bool[0];
        TagValuesDecoded = new bool[0];
    }

    /// <summary>Indicates whether the most recent operation completed without error diagnostics.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    public bool GetSuccess()
    {
        return Success && !Diagnostics.HasErrors();
    }

    /// <summary>Indicates whether the parent reader remains open.</summary>
    /// <returns><c>true</c> while source-backed operations are permitted.</returns>
    public bool GetIsOpen()
    {
        return Lifetime != null && Lifetime.IsOpen && Source != null && Source.GetIsOpen();
    }

    /// <summary>Indicates whether image payload reads are enabled.</summary>
    /// <returns><c>true</c> for an open image from a full-reader handle.</returns>
    public bool GetCanReadImageData()
    {
        return GetIsOpen() && Options.ReadImageData;
    }

    internal static TiffImage Create(TiffHeader header, TiffDirectory directory, TiffReaderLifetime lifetime, TiffDiagnostics diagnostics, TiffReadOptions options)
    {
        TiffImage value = new TiffImage();
        value.Initialize(header, directory, lifetime, diagnostics, options);
        return value;
    }

    internal void Initialize(TiffHeader header, TiffDirectory directory, TiffReaderLifetime lifetime, TiffDiagnostics diagnostics, TiffReadOptions options)
    {
        Header = TiffFile.CopyHeader(header);
        if (directory == null)
        {
            Directory = new TiffDirectory();
        }
        else
        {
            Directory = directory.Copy();
        }

        ImageIndex = Directory.ImageIndex;
        if (lifetime == null)
        {
            Source = new TiffMemorySource(new byte[0]);
            Lifetime = new TiffReaderLifetime(Source, true, false);
        }
        else
        {
            Lifetime = lifetime;
            Source = lifetime.Source;
        }
        Diagnostics = diagnostics;
        Options = TiffFile.NormalizeOptions(options);
        CachedTagBytes = new TiffTagValueLoadResult[Directory.Entries.Length];
        CachedTagValues = new TiffTagValue[Directory.Entries.Length];
        TagBytesLoaded = new bool[Directory.Entries.Length];
        TagValuesDecoded = new bool[Directory.Entries.Length];
        Success = !Diagnostics.HasErrors();
    }

    /// <summary>Returns the index.</summary>
    /// <returns>The requested value or model.</returns>
    public int GetIndex()
    {
        return ImageIndex;
    }

    /// <summary>Returns the header.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffHeader GetHeader()
    {
        return TiffFile.CopyHeader(Header);
    }

    /// <summary>Returns the directory.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffDirectory GetDirectory()
    {
        return Directory.Copy();
    }

    /// <summary>Returns the info.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffImageInfo GetInfo()
    {
        if (!EnsureOpen())
        {
            return new TiffImageInfo();
        }

        TiffImageInfoResult result = TiffSourceImageInfoExtractor.Extract(Source, Directory, Header.ByteOrder, Options);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Info.Copy();
    }

    /// <summary>Reads EXIF metadata through this image's random-access source.</summary>
    /// <returns>The EXIF metadata and diagnostics.</returns>
    public TiffExifReadResult ReadExif()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        TiffExifReadResult result = TiffSourceMetadataReader.ReadExif(Directory, Source, Header.ByteOrder, Header.FileKind, Options);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads GPS metadata through this image's random-access source.</summary>
    /// <returns>The GPS metadata and diagnostics.</returns>
    public TiffGpsReadResult ReadGps()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        TiffGpsReadResult result = TiffSourceMetadataReader.ReadGps(Directory, Source, Header.ByteOrder, Header.FileKind, Options);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads the XMP packet through this image's random-access source.</summary>
    /// <returns>The XMP packet and diagnostics.</returns>
    public TiffXmpReadResult ReadXmp()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffXmpReadResult.Create(new TiffXmpPacket(), diagnostics);
        }

        TiffXmpReadResult result = TiffSourceMetadataReader.ReadXmp(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads the ICC profile through this image's random-access source.</summary>
    /// <returns>The ICC profile and diagnostics.</returns>
    public TiffIccProfileReadResult ReadIccProfile()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffIccProfileReadResult.Create(new TiffIccProfile(), diagnostics);
        }

        TiffIccProfileReadResult result = TiffSourceMetadataReader.ReadIccProfile(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads Photoshop image resources through this image's random-access source.</summary>
    /// <returns>The Photoshop metadata and diagnostics.</returns>
    public TiffPhotoshopReadResult ReadPhotoshop()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffPhotoshopReadResult.Create(new TiffPhotoshopMetadata(), diagnostics);
        }

        TiffPhotoshopReadResult result = TiffSourceMetadataReader.ReadPhotoshop(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads IPTC metadata through this image's random-access source.</summary>
    /// <returns>The IPTC metadata and diagnostics.</returns>
    public TiffIptcReadResult ReadIptc()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffIptcReadResult.Create(new TiffIptcMetadata(), diagnostics);
        }

        TiffIptcReadResult result = TiffSourceMetadataReader.ReadIptc(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads ImageDescription metadata through this image's random-access source.</summary>
    /// <returns>The image-description metadata and diagnostics.</returns>
    public TiffImageDescriptionReadResult ReadImageDescription()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffImageDescriptionReadResult.Create(new TiffImageDescriptionMetadata(), diagnostics);
        }

        TiffImageDescriptionReadResult result = TiffSourceMetadataReader.ReadImageDescription(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads OME metadata through this image's random-access source.</summary>
    /// <returns>The OME metadata and diagnostics.</returns>
    public TiffOmeMetadataReadResult ReadOmeMetadata()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffOmeMetadataReadResult.Create(new TiffOmeMetadata(), diagnostics);
        }

        TiffOmeMetadataReadResult result = TiffSourceMetadataReader.ReadOmeMetadata(Directory, Source, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads ImageJ metadata through this image's random-access source.</summary>
    /// <returns>The ImageJ metadata and diagnostics.</returns>
    public TiffImageJMetadataReadResult ReadImageJMetadata()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffImageJMetadataReadResult.Create(new TiffImageJMetadata(), diagnostics);
        }

        TiffImageJMetadataReadResult result = TiffSourceMetadataReader.ReadImageJMetadata(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads GDAL metadata through this image's random-access source.</summary>
    /// <returns>The GDAL metadata and diagnostics.</returns>
    public TiffGdalMetadataReadResult ReadGdalMetadata()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffGdalMetadataReadResult.Create(new TiffGdalMetadata(), diagnostics);
        }

        TiffGdalMetadataReadResult result = TiffSourceMetadataReader.ReadGdalMetadata(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads the GeoTIFF key directory through this image's random-access source.</summary>
    /// <returns>The geographic key directory and diagnostics.</returns>
    public TiffGeoKeyDirectoryReadResult ReadGeoKeyDirectory()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffGeoKeyDirectoryReadResult.Create(new TiffGeoKeyDirectory(), diagnostics);
        }

        TiffGeoKeyDirectoryReadResult result = TiffSourceMetadataReader.ReadGeoKeyDirectory(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads GeoTIFF model metadata through this image's random-access source.</summary>
    /// <returns>The geographic model metadata and diagnostics.</returns>
    public TiffGeoModelReadResult ReadGeoModel()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffGeoModelReadResult.Create(new TiffGeoModelMetadata(), diagnostics);
        }

        TiffGeoModelReadResult result = TiffSourceMetadataReader.ReadGeoModel(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Reads GeoTIFF parameter metadata through this image's random-access source.</summary>
    /// <returns>The geographic parameters and diagnostics.</returns>
    public TiffGeoParameterReadResult ReadGeoParameters()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffGeoParameterReadResult.Create(new TiffGeoParameterMetadata(), diagnostics);
        }

        TiffGeoParameterReadResult result = TiffSourceMetadataReader.ReadGeoParameters(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    /// <summary>Recognizes vendor metadata through this image's random-access source.</summary>
    /// <returns>The recognized vendor metadata and diagnostics.</returns>
    public TiffVendorMetadataReadResult ReadVendorMetadata()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateMetadataSource(diagnostics))
        {
            return TiffVendorMetadataReadResult.Create(new TiffVendorMetadata(), diagnostics);
        }

        TiffVendorMetadataReadResult result = TiffSourceMetadataReader.ReadVendorMetadata(Directory, Source, Header.ByteOrder, Options.Limits);
        CompleteMetadataRead(result.Diagnostics);
        return result;
    }

    internal bool ValidateMetadataSource(TiffDiagnostics diagnostics)
    {
        if (GetIsOpen())
        {
            return true;
        }

        diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
        Diagnostics.Merge(diagnostics);
        Success = false;
        return false;
    }

    internal void CompleteMetadataRead(TiffDiagnostics diagnostics)
    {
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
    }

    /// <summary>Returns the tags.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffIfdEntry[] GetTags()
    {
        return TiffDirectory.CopyEntries(Directory.Entries);
    }

    /// <summary>Returns the tag.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A detached copy of the requested entry, or <c>null</c> when the tag is absent.</returns>
    public TiffIfdEntry? GetTag(int tagId)
    {
        return Directory.FindEntry(tagId);
    }

    /// <summary>Returns one decoded tag value without changing its TIFF storage type.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A detached decoded value, or <c>null</c> when the tag is absent.</returns>
    public TiffTagValue? GetTagValue(int tagId)
    {
        int index = FindTagIndex(tagId);
        if (index < 0)
        {
            return null;
        }

        TiffTagValue value = LoadDecodedTagValueAtIndex(index);
        if (value.DataType == TiffDataType.Unknown)
        {
            return null;
        }

        return value.Copy();
    }

    /// <summary>Returns all decoded tag values in the exact order stored by the image directory.</summary>
    /// <returns>Detached decoded tag values.</returns>
    public TiffTagValue[] GetTagValues()
    {
        TiffTagValue[] values = new TiffTagValue[Directory.Entries.Length];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = LoadDecodedTagValueAtIndex(index);
            index = index + 1;
        }

        return values;
    }

    /// <summary>Returns the encoded bytes of one tag without semantic conversion.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>Detached bytes, or an empty array when the tag is absent or cannot be read.</returns>
    public byte[] GetRawTagBytes(int tagId)
    {
        int index = FindTagIndex(tagId);
        if (index < 0)
        {
            return new byte[0];
        }

        TiffTagValueLoadResult result = LoadTagBytesAtIndex(index);
        return TiffTagValue.CopyByteArray(result.Bytes);
    }

    /// <summary>Returns every tag's encoded bytes in the exact order stored by the image directory.</summary>
    /// <returns>Detached encoded byte arrays.</returns>
    public byte[][] GetRawTagBytesInOriginalOrder()
    {
        byte[][] values = new byte[Directory.Entries.Length][];
        int index = 0;
        while (index < values.Length)
        {
            TiffTagValueLoadResult result = LoadTagBytesAtIndex(index);
            values[index] = TiffTagValue.CopyByteArray(result.Bytes);
            index = index + 1;
        }

        return values;
    }

    /// <summary>Indicates whether the first matching tag value has been materialized from the source.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns><c>true</c> after raw or typed value access.</returns>
    public bool GetIsTagValueLoaded(int tagId)
    {
        int index = FindTagIndex(tagId);
        return index >= 0 && TagBytesLoaded[index];
    }

    /// <summary>Returns one unsigned scalar while retaining support for all unsigned TIFF integer and IFD types.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The scalar value, or zero after a diagnostic when the value is incompatible.</returns>
    public ulong GetUnsignedTagScalar(int tagId)
    {
        ulong[] values = GetUnsignedTagArray(tagId);
        if (values.Length != 1)
        {
            AddTagAccessError(TiffErrorCode.InvalidTagCount, "Unsigned scalar tag access requires exactly one value.", tagId);
            return 0;
        }

        return values[0];
    }

    /// <summary>Returns unsigned values from BYTE, SHORT, LONG, IFD, LONG8, or IFD8 storage.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>Detached values, or an empty array after a diagnostic when the value is incompatible.</returns>
    public ulong[] GetUnsignedTagArray(int tagId)
    {
        TiffTagValue? value = GetTagValue(tagId);
        if (value == null)
        {
            AddTagAccessError(TiffErrorCode.MissingRequiredTag, "Unsigned tag access requires a present tag.", tagId);
            return new ulong[0];
        }

        if (!value.IsUnsigned())
        {
            AddTagAccessError(TiffErrorCode.InvalidTagType, "Tag does not use an unsigned TIFF integer or IFD type.", tagId);
            return new ulong[0];
        }

        return TiffTagValue.CopyUnsignedArray(value.UnsignedValues);
    }

    /// <summary>Returns one signed scalar from SBYTE, SSHORT, SLONG, or SLONG8 storage.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The scalar value, or zero after a diagnostic when the value is incompatible.</returns>
    public long GetSignedTagScalar(int tagId)
    {
        long[] values = GetSignedTagArray(tagId);
        if (values.Length != 1)
        {
            AddTagAccessError(TiffErrorCode.InvalidTagCount, "Signed scalar tag access requires exactly one value.", tagId);
            return 0;
        }

        return values[0];
    }

    /// <summary>Returns signed values from SBYTE, SSHORT, SLONG, or SLONG8 storage.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>Detached values, or an empty array after a diagnostic when the value is incompatible.</returns>
    public long[] GetSignedTagArray(int tagId)
    {
        TiffTagValue? value = GetTagValue(tagId);
        if (value == null)
        {
            AddTagAccessError(TiffErrorCode.MissingRequiredTag, "Signed tag access requires a present tag.", tagId);
            return new long[0];
        }

        if (!value.IsSigned())
        {
            AddTagAccessError(TiffErrorCode.InvalidTagType, "Tag does not use a signed TIFF integer type.", tagId);
            return new long[0];
        }

        return TiffTagValue.CopySignedArray(value.SignedValues);
    }

    /// <summary>Returns one RATIONAL or SRATIONAL scalar.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The detached rational, or zero over one after a diagnostic when the value is incompatible.</returns>
    public TiffRational GetRationalTagScalar(int tagId)
    {
        TiffRational[] values = GetRationalTagArray(tagId);
        if (values.Length != 1)
        {
            AddTagAccessError(TiffErrorCode.InvalidTagCount, "Rational scalar tag access requires exactly one value.", tagId);
            return new TiffRational();
        }

        return values[0].Copy();
    }

    /// <summary>Returns values from RATIONAL or SRATIONAL storage.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>Detached values, or an empty array after a diagnostic when the value is incompatible.</returns>
    public TiffRational[] GetRationalTagArray(int tagId)
    {
        TiffTagValue? value = GetTagValue(tagId);
        if (value == null)
        {
            AddTagAccessError(TiffErrorCode.MissingRequiredTag, "Rational tag access requires a present tag.", tagId);
            return new TiffRational[0];
        }

        if (!value.IsRational())
        {
            AddTagAccessError(TiffErrorCode.InvalidTagType, "Tag does not use a rational TIFF type.", tagId);
            return new TiffRational[0];
        }

        return TiffTagValue.CopyRationalArray(value.RationalValues);
    }

    /// <summary>Returns one FLOAT or DOUBLE scalar.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The scalar value, or zero after a diagnostic when the value is incompatible.</returns>
    public double GetFloatingTagScalar(int tagId)
    {
        double[] values = GetFloatingTagArray(tagId);
        if (values.Length != 1)
        {
            AddTagAccessError(TiffErrorCode.InvalidTagCount, "Floating scalar tag access requires exactly one value.", tagId);
            return 0.0;
        }

        return values[0];
    }

    /// <summary>Returns values from FLOAT or DOUBLE storage.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>Detached values, or an empty array after a diagnostic when the value is incompatible.</returns>
    public double[] GetFloatingTagArray(int tagId)
    {
        TiffTagValue? value = GetTagValue(tagId);
        if (value == null)
        {
            AddTagAccessError(TiffErrorCode.MissingRequiredTag, "Floating tag access requires a present tag.", tagId);
            return new double[0];
        }

        if (!value.IsFloating())
        {
            AddTagAccessError(TiffErrorCode.InvalidTagType, "Tag does not use a floating-point TIFF type.", tagId);
            return new double[0];
        }

        return TiffTagValue.CopyFloatingArray(value.FloatingValues);
    }

    /// <summary>Returns one string from ASCII storage.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The string, or an empty string after a diagnostic when the value is incompatible.</returns>
    public string GetAsciiTagScalar(int tagId)
    {
        string[] values = GetAsciiTagArray(tagId);
        if (values.Length != 1)
        {
            AddTagAccessError(TiffErrorCode.InvalidTagCount, "ASCII scalar tag access requires exactly one string.", tagId);
            return "";
        }

        return values[0];
    }

    /// <summary>Returns NUL-separated strings from ASCII storage.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>Detached strings, or an empty array after a diagnostic when the value is incompatible.</returns>
    public string[] GetAsciiTagArray(int tagId)
    {
        TiffTagValue? value = GetTagValue(tagId);
        if (value == null)
        {
            AddTagAccessError(TiffErrorCode.MissingRequiredTag, "ASCII tag access requires a present tag.", tagId);
            return new string[0];
        }

        if (!value.IsAscii())
        {
            AddTagAccessError(TiffErrorCode.InvalidTagType, "Tag does not use the ASCII TIFF type.", tagId);
            return new string[0];
        }

        return TiffAscii.CopyStrings(value.TextValues);
    }

    /// <summary>Returns the tag defaulted.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffTagDefaultResult GetTagDefaulted(int tagId)
    {
        if (!EnsureOpen())
        {
            return new TiffTagDefaultResult();
        }

        int index = FindTagIndex(tagId);
        if (index < 0)
        {
            return TiffTagDefaultLookup.Find(tagId);
        }

        TiffTagValue value = LoadDecodedTagValueAtIndex(index);
        if (value.DataType == TiffDataType.Unknown)
        {
            return new TiffTagDefaultResult();
        }

        return TiffTagDefaultResult.Create(true, false, value);
    }

    /// <summary>Returns the number of available get sub image entries.</summary>
    /// <returns>The requested value or model.</returns>
    public int GetSubImageCount()
    {
        TiffSubIfdReadResult result = ReadSubIfds();
        return result.SubIfdCount();
    }

    /// <summary>Returns the sub image.</summary>
    /// <param name="subImageIndex">The sub image index.</param>
    /// <returns>The requested value or model.</returns>
    public TiffImage GetSubImage(int subImageIndex)
    {
        TiffSubIfdReadResult result = ReadSubIfds();
        if (subImageIndex < 0 || subImageIndex >= result.SubIfds.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Subimage index is outside the available SubIFD range.");
            Success = false;
            return new TiffImage();
        }

        if (result.SubIfds[subImageIndex].Directories.Length == 0)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Subimage does not contain a directory.");
            Success = false;
            return new TiffImage();
        }

        Success = !Diagnostics.HasErrors();
        return TiffImage.Create(Header, result.SubIfds[subImageIndex].Directories[0], Lifetime, Diagnostics, Options);
    }

    /// <summary>Detects reduced-resolution SubIFD pyramid levels beneath this image.</summary>
    /// <returns>The detected pyramid and diagnostics.</returns>
    public TiffPyramidDetectionResult GetPyramid()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!EnsureOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
            return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
        }

        TiffPyramidDetectionResult result = TiffSourcePyramidNavigator.Detect(Directory, Source, Header.ByteOrder, Header.FileKind, Options);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffPyramidDetectionResult.Create(result.Pyramid, result.Diagnostics);
    }

    /// <summary>Returns the number of directly navigable pyramid levels, including the base image as level zero.</summary>
    /// <returns>The pyramid level count.</returns>
    public int GetPyramidLevelCount()
    {
        TiffPyramidDetectionResult result = GetPyramid();
        if (!result.Success)
        {
            return 0;
        }

        return result.Pyramid.Levels.Length + 1;
    }

    /// <summary>Returns one pyramid level, where level zero is the base image.</summary>
    /// <param name="levelIndex">The zero-based pyramid level index.</param>
    /// <returns>The selected level image.</returns>
    public TiffImage GetPyramidLevel(int levelIndex)
    {
        TiffPyramidDetectionResult result = GetPyramid();
        if (!result.Success || levelIndex < 0 || levelIndex > result.Pyramid.Levels.Length)
        {
            if (result.Success)
            {
                Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Pyramid level index is outside the available range.");
            }

            Success = false;
            return new TiffImage();
        }

        if (levelIndex == 0)
        {
            return TiffImage.Create(Header, Directory, Lifetime, Diagnostics, Options);
        }

        return TiffImage.Create(Header, result.Pyramid.Levels[levelIndex - 1].Directory, Lifetime, Diagnostics, Options);
    }

    /// <summary>Looks up the tile containing a coordinate at a selected pyramid level.</summary>
    /// <param name="levelIndex">The zero-based pyramid level index, with zero identifying the base image.</param>
    /// <param name="column">The level-relative pixel column.</param>
    /// <param name="row">The level-relative pixel row.</param>
    /// <param name="depth">The level-relative depth coordinate.</param>
    /// <param name="sampleIndex">The zero-based sample plane for separate data.</param>
    /// <returns>The tile lookup and diagnostics.</returns>
    public TiffTileLookupResult LookupPyramidTile(int levelIndex, ulong column, ulong row, ulong depth, int sampleIndex)
    {
        TiffImage level = GetPyramidLevel(levelIndex);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!level.GetSuccess())
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "The selected pyramid level is unavailable.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffImageInfo info = level.GetInfo();
        ulong[] offsets = level.GetUnsignedTagArray(TiffTagId.TileOffsets);
        ulong[] byteCounts = level.GetUnsignedTagArray(TiffTagId.TileByteCounts);
        TiffTileLookupResult result = TiffTileLookupBuilder.LookupVolume(info, offsets, byteCounts, column, row, depth, sampleIndex);
        diagnostics.Merge(result.Diagnostics);
        Diagnostics.Merge(result.Diagnostics);
        if (diagnostics.HasErrors())
        {
            Success = false;
            return TiffTileLookupResult.Create(new TiffTileLookup(), diagnostics);
        }

        TiffTileLookup lookup = result.Lookup.Copy();
        lookup.PyramidLevelIndex = levelIndex;
        Success = !Diagnostics.HasErrors();
        return TiffTileLookupResult.Create(lookup, diagnostics);
    }

    /// <summary>Reads the stored bytes for the tile containing a coordinate at a selected pyramid level.</summary>
    /// <param name="levelIndex">The zero-based pyramid level index, with zero identifying the base image.</param>
    /// <param name="column">The level-relative pixel column.</param>
    /// <param name="row">The level-relative pixel row.</param>
    /// <param name="depth">The level-relative depth coordinate.</param>
    /// <param name="sampleIndex">The zero-based sample plane for separate data.</param>
    /// <returns>The selected raw tile and diagnostics.</returns>
    public TiffRawTileReadResult ReadPyramidTile(int levelIndex, ulong column, ulong row, ulong depth, int sampleIndex)
    {
        TiffTileLookupResult lookupResult = LookupPyramidTile(levelIndex, column, row, depth, sampleIndex);
        if (!lookupResult.Success || lookupResult.Lookup.TileIndex > int.MaxValue)
        {
            TiffDiagnostics diagnostics = lookupResult.Diagnostics;
            if (lookupResult.Success)
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, "Pyramid tile index exceeds the supported public index range.");
            }

            Success = false;
            return TiffRawTileReadResult.Create(lookupResult.Lookup.TileIndex, lookupResult.Lookup.ByteRange, new byte[0], diagnostics);
        }

        TiffImage level = GetPyramidLevel(levelIndex);
        TiffRawTileReadResult result = level.ReadRawTile((int)lookupResult.Lookup.TileIndex);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    /// <summary>Returns the number of available get strip entries.</summary>
    /// <returns>The requested value or model.</returns>
    public ulong GetStripCount()
    {
        TiffImageInfo info = GetInfo();
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (info.IsTiled)
        {
            return 0;
        }

        ulong count = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return count;
    }

    /// <summary>Returns the number of available get tile entries.</summary>
    /// <returns>The requested value or model.</returns>
    public ulong GetTileCount()
    {
        TiffImageInfo info = GetInfo();
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!info.IsTiled)
        {
            return 0;
        }

        ulong count = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return count;
    }

    /// <summary>Returns the scanline size.</summary>
    /// <returns>The requested value or model.</returns>
    public ulong GetScanlineSize()
    {
        return GetSampleScanlineSize(0);
    }

    /// <summary>Returns the decoded byte count of one scanline for the selected sample plane.</summary>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <returns>The byte count for separate data, or the complete interleaved byte count for contiguous data.</returns>
    public ulong GetSampleScanlineSize(int sampleIndex)
    {
        TiffImageInfo info = GetInfo();
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        ulong size = TiffSourceScanlineReader.CalculateScanlineByteCount(info, sampleIndex, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return size;
    }

    /// <summary>Reads and decodes one scanline into a caller-provided buffer.</summary>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="destination">The destination buffer. Any bytes beyond the scanline are left unchanged.</param>
    /// <returns>The diagnostics produced by this read.</returns>
    public TiffDiagnostics ReadScanlineToBuffer(ulong row, Span<byte> destination)
    {
        return ReadSampleScanlineToBuffer(row, 0, destination);
    }

    /// <summary>Reads and decodes one scanline for the selected sample plane into a caller-provided buffer.</summary>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="sampleIndex">The zero-based sample index. Contiguous data returns the complete interleaved row.</param>
    /// <param name="destination">The destination buffer. Any bytes beyond the scanline are left unchanged.</param>
    /// <returns>The diagnostics produced by this read.</returns>
    public TiffDiagnostics ReadSampleScanlineToBuffer(ulong row, int sampleIndex, Span<byte> destination)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!EnsureOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
            return diagnostics;
        }

        if (!Options.ReadImageData)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image data reads are disabled for metadata-only TIFF handles.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return diagnostics;
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = new ulong[0];
        ulong[] byteCounts = new ulong[0];
        if (info.IsTiled)
        {
            offsets = GetUnsignedTagArray(TiffTagId.TileOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.TileByteCounts);
        }
        else
        {
            offsets = GetUnsignedTagArray(TiffTagId.StripOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.StripByteCounts);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        TiffDiagnostics readDiagnostics = TiffSourceScanlineReader.ReadScanlineWithCodec(info, offsets, byteCounts, Source, Header.ByteOrder, limits, row, sampleIndex, destination, Options.CompressionCodec);
        diagnostics.Merge(readDiagnostics);
        Diagnostics.Merge(readDiagnostics);
        Success = !Diagnostics.HasErrors();
        return diagnostics;
    }

    /// <summary>Returns the output layout and caller-buffer size for a rectangular region read.</summary>
    /// <param name="options">The region bounds and output-layout selection.</param>
    /// <returns>The validated region-read information.</returns>
    public TiffRegionReadInfo GetRegionReadInfo(TiffRegionReadOptions options)
    {
        TiffImageInfo info = GetInfo();
        TiffRegionReadInfo result = TiffSourceRegionReader.CreateReadInfo(info, options);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    /// <summary>Reads and decodes a rectangular region into a caller-provided buffer.</summary>
    /// <param name="options">The region bounds and output-layout selection.</param>
    /// <param name="destination">The destination buffer. Bytes beyond the required region size are left unchanged.</param>
    /// <returns>The diagnostics produced by this read.</returns>
    public TiffDiagnostics ReadRegionToBuffer(TiffRegionReadOptions options, Span<byte> destination)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!EnsureOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
            return diagnostics;
        }

        if (!Options.ReadImageData)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image data reads are disabled for metadata-only TIFF handles.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return diagnostics;
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = new ulong[0];
        ulong[] byteCounts = new ulong[0];
        if (info.IsTiled)
        {
            offsets = GetUnsignedTagArray(TiffTagId.TileOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.TileByteCounts);
        }
        else
        {
            offsets = GetUnsignedTagArray(TiffTagId.StripOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.StripByteCounts);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        TiffDiagnostics readDiagnostics = TiffSourceRegionReader.ReadRegionWithCodec(info, offsets, byteCounts, Source, Header.ByteOrder, limits, options, destination, Options.CompressionCodec);
        diagnostics.Merge(readDiagnostics);
        Diagnostics.Merge(readDiagnostics);
        Success = !Diagnostics.HasErrors();
        return diagnostics;
    }

    /// <summary>Reads and decodes a rectangular region with row progress and cancellation.</summary>
    /// <param name="options">The region bounds and output-layout selection.</param>
    /// <param name="destination">The destination buffer. Bytes beyond the required region size are left unchanged.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The diagnostics produced by this read.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    public TiffDiagnostics ReadRegionToBufferWithCancellation(TiffRegionReadOptions options, Span<byte> destination, ITiffProgress? progress, CancellationToken cancellationToken)
    {
        TiffOperationMonitor monitor = new TiffOperationMonitor(TiffProgressOperation.RegionRead, progress, cancellationToken);
        TiffRegionReadInfo readInfo = TiffSourceRegionReader.CreateReadInfo(GetInfo(), options);
        long totalUnits = TiffOperationMonitor.ConvertUnits(readInfo.Region.Length);
        monitor.Start(totalUnits);
        if (!readInfo.Success || readInfo.TotalByteCount > (ulong)destination.Length)
        {
            return ReadRegionToBuffer(options, destination);
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRegionReadOptions rowOptions = options.Copy();
        rowOptions.Region.Length = 1;
        ulong rowIndex = 0;
        while (rowIndex < readInfo.Region.Length)
        {
            monitor.CheckCancellation();
            rowOptions.Region.Row = readInfo.Region.Row + rowIndex;
            ulong destinationOffset = rowIndex * readInfo.RowByteCount;
            Span<byte> rowDestination = destination.Slice((int)destinationOffset, (int)readInfo.RowByteCount);
            diagnostics.Merge(ReadRegionToBuffer(rowOptions, rowDestination));
            if (diagnostics.HasErrors())
            {
                return diagnostics;
            }

            rowIndex = rowIndex + 1;
            if (rowIndex < readInfo.Region.Length)
            {
                monitor.Advance(TiffOperationMonitor.ConvertUnits(rowIndex), totalUnits);
            }
        }

        monitor.Complete(totalUnits);
        return diagnostics;
    }

    /// <summary>Returns the exact output layout and caller-buffer size for a complete source-raster read.</summary>
    /// <returns>The source-raster read information.</returns>
    public TiffRasterReadInfo GetSourceRasterReadInfo()
    {
        TiffImageInfo info = GetInfo();
        TiffRasterReadInfo result = TiffSourceSampleRasterReader.CreateRasterInfo(info);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    /// <summary>Reads the complete decoded source raster into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer. Bytes beyond the required raster size are left unchanged.</param>
    /// <returns>The diagnostics produced by this read.</returns>
    public TiffDiagnostics ReadSourceRasterToBuffer(Span<byte> destination)
    {
        return ReadSourceSamplesToBuffer(-1, destination);
    }

    /// <summary>Returns the exact output layout and caller-buffer size for one source sample plane.</summary>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <returns>The sample-plane read information.</returns>
    public TiffRasterReadInfo GetSamplePlaneReadInfo(int sampleIndex)
    {
        TiffImageInfo info = GetInfo();
        TiffRasterReadInfo result = TiffSourceSampleRasterReader.CreatePlaneInfo(info, sampleIndex);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    /// <summary>Reads one decoded source sample plane into a caller-provided buffer.</summary>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="destination">The destination buffer. Bytes beyond the required plane size are left unchanged.</param>
    /// <returns>The diagnostics produced by this read.</returns>
    public TiffDiagnostics ReadSamplePlaneToBuffer(int sampleIndex, Span<byte> destination)
    {
        if (sampleIndex < 0)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sample index is outside the SamplesPerPixel range.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return diagnostics;
        }

        return ReadSourceSamplesToBuffer(sampleIndex, destination);
    }

    /// <summary>Reads the complete image as interleaved red, green, blue, and alpha bytes.</summary>
    /// <param name="options">The display conversion controls.</param>
    /// <returns>The converted RGBA8 raster and diagnostics.</returns>
    public TiffRgba8ReadResult ReadRgba8(TiffRgbaReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!EnsureOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
            return TiffRgba8ReadResult.Create(0, 0, 0, 0, new byte[0], diagnostics);
        }

        if (!Options.ReadImageData)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image data reads are disabled for metadata-only TIFF handles.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffRgba8ReadResult.Create(0, 0, 0, 0, new byte[0], diagnostics);
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = GetRgbaOffsets(info);
        ulong[] byteCounts = GetRgbaByteCounts(info);
        TiffRgba8ReadResult result = TiffSourceRgbaReader.ReadRgba8WithCodec(info, offsets, byteCounts, Source, Header.ByteOrder, TiffDirectoryReader.GetLimits(Options), options, Options.CompressionCodec);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    /// <summary>Reads the complete image as interleaved red, green, blue, and alpha 16-bit samples.</summary>
    /// <param name="options">The display conversion controls.</param>
    /// <returns>The converted RGBA16 raster and diagnostics.</returns>
    public TiffRgba16ReadResult ReadRgba16(TiffRgbaReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!EnsureOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
            return TiffRgba16ReadResult.Create(0, 0, 0, 0, new ushort[0], diagnostics);
        }

        if (!Options.ReadImageData)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image data reads are disabled for metadata-only TIFF handles.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffRgba16ReadResult.Create(0, 0, 0, 0, new ushort[0], diagnostics);
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = GetRgbaOffsets(info);
        ulong[] byteCounts = GetRgbaByteCounts(info);
        TiffRgba16ReadResult result = TiffSourceRgbaReader.ReadRgba16WithCodec(info, offsets, byteCounts, Source, Header.ByteOrder, TiffDirectoryReader.GetLimits(Options), options, Options.CompressionCodec);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    internal ulong[] GetRgbaOffsets(TiffImageInfo info)
    {
        if (info.IsTiled)
        {
            return GetUnsignedTagArray(TiffTagId.TileOffsets);
        }

        return GetUnsignedTagArray(TiffTagId.StripOffsets);
    }

    internal ulong[] GetRgbaByteCounts(TiffImageInfo info)
    {
        if (info.IsTiled)
        {
            return GetUnsignedTagArray(TiffTagId.TileByteCounts);
        }

        return GetUnsignedTagArray(TiffTagId.StripByteCounts);
    }

    internal TiffDiagnostics ReadSourceSamplesToBuffer(int sampleIndex, Span<byte> destination)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!EnsureOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
            return diagnostics;
        }

        if (!Options.ReadImageData)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image data reads are disabled for metadata-only TIFF handles.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return diagnostics;
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = new ulong[0];
        ulong[] byteCounts = new ulong[0];
        if (info.IsTiled)
        {
            offsets = GetUnsignedTagArray(TiffTagId.TileOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.TileByteCounts);
        }
        else
        {
            offsets = GetUnsignedTagArray(TiffTagId.StripOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.StripByteCounts);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        TiffDiagnostics readDiagnostics = new TiffDiagnostics();
        if (sampleIndex < 0)
        {
            readDiagnostics = TiffSourceSampleRasterReader.ReadRasterWithCodec(info, offsets, byteCounts, Source, Header.ByteOrder, limits, destination, Options.CompressionCodec);
        }
        else
        {
            readDiagnostics = TiffSourceSampleRasterReader.ReadPlaneWithCodec(info, offsets, byteCounts, Source, Header.ByteOrder, limits, sampleIndex, destination, Options.CompressionCodec);
        }

        diagnostics.Merge(readDiagnostics);
        Diagnostics.Merge(readDiagnostics);
        Success = !Diagnostics.HasErrors();
        return diagnostics;
    }

    /// <summary>Returns the strip size.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <returns>The requested value or model.</returns>
    public ulong GetStripSize(int stripIndex)
    {
        if (stripIndex < 0)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            Success = false;
            return 0;
        }

        TiffImageInfo info = GetInfo();
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        ulong size = TiffStrileLayout.CalculateStripByteCount(info, (ulong)stripIndex, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return size;
    }

    /// <summary>Returns the tile size.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <returns>The requested value or model.</returns>
    public ulong GetTileSize(int tileIndex)
    {
        if (tileIndex < 0)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            Success = false;
            return 0;
        }

        TiffImageInfo info = GetInfo();
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        ulong size = TiffStrileLayout.CalculateTileImageByteCount(info, (ulong)tileIndex, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return size;
    }

    /// <summary>Returns the normalized strip layout, including a virtual chopped view when enabled by the read memory policy.</summary>
    /// <returns>The strip-layout result.</returns>
    public TiffStripLayoutResult GetStripLayout()
    {
        if (!EnsureOpen())
        {
            return TiffStripLayoutResult.Create(new TiffStripLayout(), Diagnostics);
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = new ulong[0];
        ulong[] byteCounts = new ulong[0];
        if (!info.IsTiled)
        {
            offsets = GetUnsignedTagArray(TiffTagId.StripOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.StripByteCounts);
        }

        TiffMemoryPolicy memoryPolicy = Options.GetMemoryPolicy();
        TiffStripLayoutResult result = new TiffStripLayoutResult();
        if (memoryPolicy.ChoppedStripByteCount > 0)
        {
            result = TiffSegmentLayoutBuilder.CreateChoppedStripLayout(info, offsets, byteCounts, memoryPolicy.ChoppedStripByteCount, TiffDirectoryReader.GetLimits(Options));
        }
        else
        {
            result = TiffSegmentLayoutBuilder.CreateStripLayout(info, offsets, byteCounts);
        }
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffStripLayoutResult.Create(result.Layout, result.Diagnostics);
    }

    /// <summary>Returns an optional virtual chopped-strip view of a large uncompressed single strip per plane.</summary>
    /// <param name="targetDecodedByteCount">The preferred maximum decoded byte count of each virtual strip.</param>
    /// <returns>The strip-layout result.</returns>
    public TiffStripLayoutResult GetChoppedStripLayout(ulong targetDecodedByteCount)
    {
        if (!EnsureOpen())
        {
            return TiffStripLayoutResult.Create(new TiffStripLayout(), Diagnostics);
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = new ulong[0];
        ulong[] byteCounts = new ulong[0];
        if (!info.IsTiled)
        {
            offsets = GetUnsignedTagArray(TiffTagId.StripOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.StripByteCounts);
        }

        TiffStripLayoutResult result = TiffSegmentLayoutBuilder.CreateChoppedStripLayout(info, offsets, byteCounts, targetDecodedByteCount);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffStripLayoutResult.Create(result.Layout, result.Diagnostics);
    }

    /// <summary>Returns the normalized tile layout.</summary>
    /// <returns>The tile-layout result.</returns>
    public TiffTileLayoutResult GetTileLayout()
    {
        if (!EnsureOpen())
        {
            return TiffTileLayoutResult.Create(new TiffTileLayout(), Diagnostics);
        }

        TiffImageInfo info = GetInfo();
        ulong[] offsets = new ulong[0];
        ulong[] byteCounts = new ulong[0];
        if (info.IsTiled)
        {
            offsets = GetUnsignedTagArray(TiffTagId.TileOffsets);
            byteCounts = GetUnsignedTagArray(TiffTagId.TileByteCounts);
        }

        TiffTileLayoutResult result = TiffSegmentLayoutBuilder.CreateTileLayout(info, offsets, byteCounts);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffTileLayoutResult.Create(result.Layout, result.Diagnostics);
    }

    /// <summary>Reads the stored compressed or uncompressed segment bytes without decoding them.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawStripReadResult ReadRawStrip(int stripIndex)
    {
        if (!EnsureOpen())
        {
            return TiffRawStripReadResult.Create(0, new TiffByteRange(), new byte[0], Diagnostics);
        }

        if (!Options.ReadImageData)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image data reads are disabled for metadata-only TIFF handles.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffRawStripReadResult.Create(0, new TiffByteRange(), new byte[0], diagnostics);
        }

        if (stripIndex < 0)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffRawStripReadResult.Create(0, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        TiffRawStripReadResult result = TiffRawStripReader.ReadSourceStrip(Directory, Source, Header.ByteOrder, (ulong)stripIndex, limits);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    /// <summary>Reads the stored compressed or uncompressed segment bytes without decoding them.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public TiffDiagnostics ReadRawStripToBuffer(int stripIndex, Span<byte> destination)
    {
        TiffRawStripReadResult result = ReadRawStrip(stripIndex);
        return CopyResultBytes(result.Bytes, destination);
    }

    /// <summary>Reads and decodes one strip.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawStripReadResult ReadEncodedStrip(int stripIndex)
    {
        TiffRawStripReadResult rawResult = ReadRawStrip(stripIndex);
        if (!rawResult.Success)
        {
            return rawResult;
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.Merge(rawResult.Diagnostics);
        TiffImageInfo info = GetInfo();
        byte[] decodedBytes = DecodeStoredSegment(info, false, stripIndex, rawResult.IsSparse, rawResult.Bytes, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffRawStripReadResult.Create((ulong)stripIndex, rawResult.Range, decodedBytes, diagnostics);
    }

    /// <summary>Reads and decodes one strip into a caller buffer.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public TiffDiagnostics ReadEncodedStripToBuffer(int stripIndex, Span<byte> destination)
    {
        TiffRawStripReadResult result = ReadEncodedStrip(stripIndex);
        return CopyResultBytes(result.Bytes, destination);
    }

    /// <summary>Reads the stored compressed or uncompressed segment bytes without decoding them.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawTileReadResult ReadRawTile(int tileIndex)
    {
        if (!EnsureOpen())
        {
            return TiffRawTileReadResult.Create(0, new TiffByteRange(), new byte[0], Diagnostics);
        }

        if (!Options.ReadImageData)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image data reads are disabled for metadata-only TIFF handles.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffRawTileReadResult.Create(0, new TiffByteRange(), new byte[0], diagnostics);
        }

        if (tileIndex < 0)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffRawTileReadResult.Create(0, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        TiffRawTileReadResult result = TiffRawTileReader.ReadSourceTile(Directory, Source, Header.ByteOrder, (ulong)tileIndex, limits);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    /// <summary>Reads the stored compressed or uncompressed segment bytes without decoding them.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public TiffDiagnostics ReadRawTileToBuffer(int tileIndex, Span<byte> destination)
    {
        TiffRawTileReadResult result = ReadRawTile(tileIndex);
        return CopyResultBytes(result.Bytes, destination);
    }

    /// <summary>Reads and decodes one tile.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffRawTileReadResult ReadEncodedTile(int tileIndex)
    {
        TiffRawTileReadResult rawResult = ReadRawTile(tileIndex);
        if (!rawResult.Success)
        {
            return rawResult;
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.Merge(rawResult.Diagnostics);
        TiffImageInfo info = GetInfo();
        byte[] decodedBytes = DecodeStoredSegment(info, true, tileIndex, rawResult.IsSparse, rawResult.Bytes, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffRawTileReadResult.Create((ulong)tileIndex, rawResult.Range, decodedBytes, diagnostics);
    }

    /// <summary>Reads and decodes one tile into a caller buffer.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public TiffDiagnostics ReadEncodedTileToBuffer(int tileIndex, Span<byte> destination)
    {
        TiffRawTileReadResult result = ReadEncodedTile(tileIndex);
        return CopyResultBytes(result.Bytes, destination);
    }

    internal byte[] DecodeStoredSegment(TiffImageInfo info, bool isTile, int segmentIndex, bool isSparse, byte[] source, TiffDiagnostics diagnostics)
    {
        ulong expectedByteCount = 0;
        if (isTile)
        {
            expectedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, (ulong)segmentIndex, diagnostics);
        }
        else
        {
            expectedByteCount = TiffStrileLayout.CalculateStripByteCount(info, (ulong)segmentIndex, diagnostics);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        TiffCodecContext context = TiffCodecContext.Create(info, isTile, (ulong)segmentIndex, expectedByteCount, limits);
        context.ByteOrder = Header.ByteOrder;
        if (isSparse)
        {
            return TiffSparseSegment.CreateZeroDecodedBytes(context, diagnostics);
        }

        TiffCodecResult result = TiffCodec.Decode(context, source, Options.CompressionCodec);
        diagnostics.Merge(result.Diagnostics);
        return result.Bytes;
    }

    /// <summary>Returns the diagnostics.</summary>
    /// <returns>The requested value or model.</returns>
    public TiffDiagnostics GetDiagnostics()
    {
        return Diagnostics;
    }

    /// <summary>Returns the shared random-access source backing this image.</summary>
    /// <returns>The source used for deferred reads.</returns>
    public ITiffRandomAccessSource GetSource()
    {
        return Source;
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

    /// <summary>Reads sub IFD s from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public TiffSubIfdReadResult ReadSubIfds()
    {
        if (!EnsureOpen())
        {
            return TiffSubIfdReadResult.Create(new TiffSubIfd[0], Diagnostics);
        }

        TiffSubIfdReadResult result = TiffSourceSubIfdReader.Read(Directory, Source, Header.ByteOrder, Header.FileKind, Options);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result;
    }

    /// <summary>Reads nested EXIF, GPS, and interoperability IFD pointers directly from the shared random-access source.</summary>
    /// <returns>A detached pointer traversal result.</returns>
    public TiffIfdPointerReadResult ReadIfdPointers()
    {
        if (!EnsureOpen())
        {
            return TiffIfdPointerReadResult.Create(new TiffIfdPointer[0], Diagnostics);
        }

        TiffIfdPointerReadResult result = TiffSourceIfdPointerReader.Read(Directory, Source, Header.ByteOrder, Header.FileKind, Options);
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return TiffIfdPointerReadResult.Create(result.Pointers, result.Diagnostics);
    }

    internal int FindTagIndex(int tagId)
    {
        int index = 0;
        while (index < Directory.Entries.Length)
        {
            if (Directory.Entries[index] != null && Directory.Entries[index].TagId == tagId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal TiffTagValueLoadResult LoadTagBytesAtIndex(int index)
    {
        if (index < 0 || index >= Directory.Entries.Length)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tag index is outside the image directory.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
        }

        if (TagBytesLoaded[index])
        {
            return CachedTagBytes[index].Copy();
        }

        if (!EnsureOpen())
        {
            return TiffTagValueLoadResult.Create(new byte[0], false, Diagnostics);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(Options);
        TiffTagValueLoadResult result = TiffTagValueLoader.LoadSourceBytes(Directory.Entries[index], Source, limits);
        CachedTagBytes[index] = result.Copy();
        TagBytesLoaded[index] = true;
        Diagnostics.Merge(result.Diagnostics);
        Success = !Diagnostics.HasErrors();
        return result.Copy();
    }

    internal TiffTagValue LoadDecodedTagValueAtIndex(int index)
    {
        if (index < 0 || index >= Directory.Entries.Length)
        {
            return new TiffTagValue();
        }

        if (TagValuesDecoded[index])
        {
            return CachedTagValues[index].Copy();
        }

        TiffTagValueLoadResult loadResult = LoadTagBytesAtIndex(index);
        if (!loadResult.Success)
        {
            return new TiffTagValue();
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffTagValue value = TiffExifMetadataReader.DecodeValue(Directory.Entries[index], loadResult.Bytes, Header.ByteOrder, diagnostics);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        if (diagnostics.HasErrors())
        {
            return new TiffTagValue();
        }

        CachedTagValues[index] = value.Copy();
        TagValuesDecoded[index] = true;
        return value.Copy();
    }

    internal void AddTagAccessError(TiffErrorCode code, string message, int tagId)
    {
        Diagnostics.AddErrorForTag(code, message, ImageIndex, tagId);
        Success = false;
    }

    internal bool EnsureOpen()
    {
        if (GetIsOpen())
        {
            return true;
        }

        Diagnostics.AddError(TiffErrorCode.UnsupportedStream, "The parent TIFF file is closed.");
        Success = false;
        return false;
    }

    internal TiffDiagnostics CopyResultBytes(byte[] bytes, Span<byte> destination)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (bytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Source bytes are required.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return diagnostics;
        }

        if (destination.Length < bytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Destination is shorter than the result bytes.");
            Diagnostics.Merge(diagnostics);
            Success = false;
            return diagnostics;
        }

        bytes.AsSpan().CopyTo(destination);
        Diagnostics.Merge(diagnostics);
        Success = !Diagnostics.HasErrors();
        return diagnostics;
    }

    internal static ulong CalculateImageRowBits(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to calculate scanline size.");
            return 0;
        }

        if (info.Width == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Image width must be greater than zero.");
            return 0;
        }

        ulong bitsPerPixel = 0;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagCount, "BitsPerSample must contain at least one value.");
                return 0;
            }

            if (info.BitsPerSample[0] < 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "BitsPerSample values must be non-negative.");
                return 0;
            }

            bitsPerPixel = (ulong)info.BitsPerSample[0];
        }
        else
        {
            bitsPerPixel = TiffStrileLayout.SumBitsPerPixel(info.BitsPerSample, diagnostics);
        }

        if (bitsPerPixel == 0)
        {
            return 0;
        }

        if (info.Width > ulong.MaxValue / bitsPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Scanline bit count overflows the supported range.");
            return 0;
        }

        return info.Width * bitsPerPixel;
    }
}
