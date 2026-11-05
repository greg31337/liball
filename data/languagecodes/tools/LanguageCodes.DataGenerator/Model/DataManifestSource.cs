using System;

namespace LanguageCodes.DataGenerator.Model;

internal sealed class DataManifestSource
{
    internal DataManifestSource(
        string id,
        string url,
        string path,
        DateTimeOffset retrievedAt,
        string upstreamVersion,
        string? upstreamDate,
        string upstreamDateBasis,
        string sha256,
        long byteLength,
        string encoding,
        string mediaType,
        string expectedHeader)
    {
        Id = id;
        Url = url;
        Path = path;
        RetrievedAt = retrievedAt;
        UpstreamVersion = upstreamVersion;
        UpstreamDate = upstreamDate;
        UpstreamDateBasis = upstreamDateBasis;
        Sha256 = sha256;
        ByteLength = byteLength;
        Encoding = encoding;
        MediaType = mediaType;
        ExpectedHeader = expectedHeader;
    }

    internal string Id { get; }

    internal string Url { get; }

    internal string Path { get; }

    internal DateTimeOffset RetrievedAt { get; }

    internal string UpstreamVersion { get; }

    internal string? UpstreamDate { get; }

    internal string UpstreamDateBasis { get; }

    internal string Sha256 { get; }

    internal long ByteLength { get; }

    internal string Encoding { get; }

    internal string MediaType { get; }

    internal string ExpectedHeader { get; }
}
