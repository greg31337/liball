using System.Collections.Generic;

namespace image;

/// <summary>Contains the complete result of reading one image source.</summary>
/// <remarks>The collections preserve source order. Parsers may return partial data together with diagnostics when input is malformed or a safety limit is reached.</remarks>
public class ImageMetadataDocument
{
    /// <summary>The detected source format.</summary>
    public ImageFormatInfo Format;
    /// <summary>The number of source bytes visible from the stream position where reading began.</summary>
    public long ByteLength;
    /// <summary>Indicates that recoverable structural errors or truncation diagnostics describe corrupt source data.</summary>
    public bool IsCorrupt;
    /// <summary>Indicates that truncation diagnostics show the source input ended before a required structure was complete.</summary>
    public bool IsIncomplete;
    /// <summary>The frames, pages, or image items discovered in source order.</summary>
    public List<ImageMetadataFrame> Frames;
    /// <summary>The top-level metadata directories discovered in source order.</summary>
    public List<MetadataDirectory> Directories;
    /// <summary>The embedded metadata profiles and packets that were preserved.</summary>
    public List<MetadataProfile> Profiles;
    /// <summary>The recognized raw metadata ranges, optionally including their bytes.</summary>
    public List<MetadataRawBlock> RawBlocks;
    /// <summary>The warnings, recoverable errors, unsupported features, and limit events produced while reading.</summary>
    public List<MetadataDiagnostic> Diagnostics;
    /// <summary>The normalized common fields derived from frames, profiles, and tags.</summary>
    public ImageMetadataSummary Summary;

    /// <summary>Creates an empty document with initialized collections and an unknown format.</summary>
    public ImageMetadataDocument()
    {
        Format = new ImageFormatInfo();
        ByteLength = 0;
        IsCorrupt = false;
        IsIncomplete = false;
        Frames = new List<ImageMetadataFrame>();
        Directories = new List<MetadataDirectory>();
        Profiles = new List<MetadataProfile>();
        RawBlocks = new List<MetadataRawBlock>();
        Diagnostics = new List<MetadataDiagnostic>();
        Summary = new ImageMetadataSummary();
    }

    internal ImageMetadataFrame AddFrame(ImageMetadataFrame? frame)
    {
        ImageMetadataFrame? actual = frame;
        if (actual == null)
        {
            actual = new ImageMetadataFrame();
        }

        actual.Index = Frames.Count;
        if (actual.Role == ImageMetadataRole.Unknown && Frames.Count == 0)
        {
            actual.Role = ImageMetadataRole.Primary;
        }
        Frames.Add(actual);
        Summary.FrameCount = Frames.Count;

        if (Summary.Width == 0 && actual.Width > 0)
        {
            Summary.Width = actual.Width;
        }

        if (Summary.Height == 0 && actual.Height > 0)
        {
            Summary.Height = actual.Height;
        }

        return actual;
    }

    internal ImageMetadataFrame AddFrameWithDimensions(int width, int height)
    {
        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = width;
        frame.Height = height;
        frame.CanvasWidth = width;
        frame.CanvasHeight = height;
        return AddFrame(frame);
    }

    internal MetadataDirectory AddDirectory(MetadataDirectory? directory)
    {
        MetadataDirectory? actual = directory;
        if (actual == null)
        {
            actual = new MetadataDirectory();
        }

        Directories.Add(actual);
        return actual;
    }

    internal MetadataDirectory AddDirectoryWithName(MetadataFamily family, string group, string name)
    {
        MetadataDirectory directory = MetadataDirectory.Create(family, group, name);
        return AddDirectory(directory);
    }

    internal MetadataProfile AddProfile(MetadataProfile? profile)
    {
        MetadataProfile? actual = profile;
        if (actual == null)
        {
            actual = new MetadataProfile();
        }

        actual.Index = Profiles.Count;
        if (actual.Role == ImageMetadataRole.Unknown && actual.Kind == MetadataProfileKind.Thumbnail)
        {
            actual.Role = ImageMetadataRole.Thumbnail;
        }
        if (actual.SourceFrameIndex < 0 && Frames.Count > 0 && (actual.Role == ImageMetadataRole.Thumbnail || actual.Role == ImageMetadataRole.Preview))
        {
            actual.SourceFrameIndex = 0;
        }
        Profiles.Add(actual);
        return actual;
    }

    internal MetadataProfile AddProfileWithData(MetadataProfileKind kind, MetadataFamily family, string key, string name, byte[] data)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = kind;
        profile.Family = family;
        profile.Key = key;
        profile.Name = name;
        profile.Data = data;
        if (data != null)
        {
            profile.Length = data.Length;
        }

        return AddProfile(profile);
    }

    internal MetadataRawBlock AddRawBlock(MetadataRawBlock? rawBlock)
    {
        MetadataRawBlock? actual = rawBlock;
        if (actual == null)
        {
            actual = new MetadataRawBlock();
        }

        RawBlocks.Add(actual);
        return actual;
    }

    internal MetadataRawBlock AddRawBlockWithRange(MetadataFamily family, string blockType, string name, long offset, long length)
    {
        MetadataRawBlock rawBlock = new MetadataRawBlock();
        rawBlock.Family = family;
        rawBlock.BlockType = blockType;
        rawBlock.Name = name;
        rawBlock.Offset = offset;
        rawBlock.Length = length;
        return AddRawBlock(rawBlock);
    }

    internal MetadataDiagnostic AddDiagnostic(MetadataDiagnostic? diagnostic)
    {
        MetadataDiagnostic? actual = diagnostic;
        if (actual == null)
        {
            actual = new MetadataDiagnostic();
        }

        Diagnostics.Add(actual);
        return actual;
    }

    internal MetadataDiagnostic AddDiagnosticWithSeverity(MetadataSeverity severity, string code, string message)
    {
        MetadataDiagnostic diagnostic = MetadataDiagnostic.Create(severity, code, message);
        return AddDiagnostic(diagnostic);
    }

    internal void RefreshDiagnosticStatus()
    {
        IsCorrupt = false;
        IsIncomplete = false;

        if (Diagnostics == null)
        {
            return;
        }

        foreach (MetadataDiagnostic diagnostic in Diagnostics)
        {
            if (diagnostic != null && diagnostic.Severity == MetadataSeverity.RecoverableError)
            {
                IsCorrupt = true;
            }

            if (diagnostic != null && diagnostic.Severity == MetadataSeverity.Truncated)
            {
                IsCorrupt = true;
                IsIncomplete = true;
            }
        }
    }
}
