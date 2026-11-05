using System;

namespace image.Tests;

internal static class RequiredMetadataQuery
{
    internal static MetadataTag FindFirst(ImageMetadataDocument? document, string? keyOrPath)
    {
        MetadataTag? tag = new ImageMetadataQuery().FindFirst(document, keyOrPath);
        if (tag == null)
        {
            throw new InvalidOperationException("Expected metadata tag was not found: " + (keyOrPath == null ? "" : keyOrPath));
        }

        return tag;
    }
}
