using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using iso20022;

namespace iso20022.Tests;

public class RepositoryArtifactOpenerTests
{
    [Test]
    public void RawNonSeekableArtifactRetainsBytesHashAndCallerProvenance()
    {
        byte[] xml = Encoding.UTF8.GetBytes("\uFEFF \n<Repository xmlns='urn:iso:std:iso:20022:2013:ecore'/>");
        RepositoryArtifactOpenOptions options = new RepositoryArtifactOpenOptions
        {
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.Repository,
                Edition = "2026-06-26",
                Identifier = "eRepository",
                Version = "2013-ecore"
            },
            OriginUri = "https://example.test/repository.iso20022",
            RetrievedAt = new IsoDateTimeValue { LexicalValue = "2026-08-31T12:00:00Z" },
            PublicationDate = new IsoPartialDateValue
            {
                Kind = PartialDateKind.YearMonth,
                Year = 2026,
                Month = 6,
                LexicalValue = "2026-06"
            },
            LicenseNotice = "Example license",
            IsApproved = true
        };
        using NonSeekableReadStream source = new NonSeekableReadStream(xml);

        RepositoryArtifactOpenResult result = RepositoryArtifactOpener.OpenArtifact(source, options);
        byte[] content = ReadAll(result.Content);
        string expectedHash = Convert.ToHexString(SHA256.HashData(xml));
        result.Content?.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.WasArchive, Is.False);
            Assert.That(result.ContentMediaType, Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(result.ArtifactByteCount, Is.EqualTo(xml.Length));
            Assert.That(result.ContentByteCount, Is.EqualTo(xml.Length));
            Assert.That(content, Is.EqualTo(xml));
            Assert.That(result.Manifest.MediaType, Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(result.Manifest.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(result.Manifest.Hash.Encoding, Is.EqualTo(BinaryEncodingKind.Hexadecimal));
            Assert.That(result.Manifest.Hash.LexicalValue, Is.EqualTo(expectedHash));
            Assert.That(result.ContentHash.LexicalValue, Is.EqualTo(expectedHash));
            Assert.That(result.Manifest.Coordinate.Edition, Is.EqualTo("2026-06-26"));
            Assert.That(result.Manifest.OriginUri, Is.EqualTo("https://example.test/repository.iso20022"));
            Assert.That(result.Manifest.PublicationDate?.LexicalValue, Is.EqualTo("2026-06"));
            Assert.That(result.Manifest.RetrievedAt?.LexicalValue, Is.EqualTo("2026-08-31T12:00:00Z"));
            Assert.That(result.Manifest.LicenseNotice, Is.EqualTo("Example license"));
            Assert.That(result.Manifest.IsApproved, Is.True);
            Assert.That(result.Provenance.Artifact, Is.SameAs(result.Manifest));
            Assert.That(result.Provenance.SourceEntryName, Is.Empty);
        });
    }

    [Test]
    public void XmlMediaDetectionRecognizesUtf8Utf16AndUtf32ByteOrders()
    {
        byte[] utf8 = WithPreamble(new UTF8Encoding(true), "<Repository/>");
        byte[] utf16Little = WithPreamble(new UnicodeEncoding(false, true), "<Repository/>");
        byte[] utf16Big = WithPreamble(new UnicodeEncoding(true, true), "<Repository/>");
        byte[] utf32Little = WithPreamble(new UTF32Encoding(false, true), "<Repository/>");
        byte[] utf32Big = WithPreamble(new UTF32Encoding(true, true), "<Repository/>");

        Assert.Multiple(() =>
        {
            Assert.That(RepositoryArtifactOpener.DetectMediaType(utf8), Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(RepositoryArtifactOpener.DetectMediaType(utf16Little), Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(RepositoryArtifactOpener.DetectMediaType(utf16Big), Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(RepositoryArtifactOpener.DetectMediaType(utf32Little), Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(RepositoryArtifactOpener.DetectMediaType(utf32Big), Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(RepositoryArtifactOpener.DetectMediaType(Encoding.UTF8.GetBytes("not xml")), Is.Empty);
        });
    }

    [Test]
    public void ZipArtifactSelectsRepositoryEntryAndCapturesArtifactAndContentHashes()
    {
        byte[] xml = Encoding.UTF8.GetBytes("<Repository id='_repository'/>");
        byte[] zip = CreateZip(
            new[]
            {
                ("docs/readme.txt", Encoding.UTF8.GetBytes("documentation")),
                ("nested/eRepository.iso20022", xml)
            },
            CompressionLevel.Optimal);
        RepositoryArtifactOpenOptions options = new RepositoryArtifactOpenOptions
        {
            OriginUri = "https://example.test/repository.zip"
        };

        RepositoryArtifactOpenResult result = RepositoryArtifactOpener.OpenArtifact(new MemoryStream(zip), options);
        byte[] content = ReadAll(result.Content);
        result.Content?.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.WasArchive, Is.True);
            Assert.That(result.EntryName, Is.EqualTo("nested/eRepository.iso20022"));
            Assert.That(result.Manifest.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
            Assert.That(result.ContentMediaType, Is.EqualTo(Iso20022MediaTypes.Xml));
            Assert.That(result.ArtifactByteCount, Is.EqualTo(zip.Length));
            Assert.That(result.ContentByteCount, Is.EqualTo(xml.Length));
            Assert.That(result.CompressedEntryByteCount, Is.GreaterThan(0));
            Assert.That(content, Is.EqualTo(xml));
            Assert.That(result.Manifest.Hash.LexicalValue,
                Is.EqualTo(Convert.ToHexString(SHA256.HashData(zip))));
            Assert.That(result.ContentHash.LexicalValue,
                Is.EqualTo(Convert.ToHexString(SHA256.HashData(xml))));
            Assert.That(result.Manifest.Hash.LexicalValue, Is.Not.EqualTo(result.ContentHash.LexicalValue));
            Assert.That(result.Provenance.SourceEntryName, Is.EqualTo("nested/eRepository.iso20022"));
            Assert.That(result.Provenance.Artifact, Is.SameAs(result.Manifest));
        });
    }

    [Test]
    public void MultipleRepositoryEntriesRequireExactCallerSelection()
    {
        byte[] first = Encoding.UTF8.GetBytes("<Repository id='first'/>");
        byte[] second = Encoding.UTF8.GetBytes("<Repository id='second'/>");
        byte[] zip = CreateZip(
            new[]
            {
                ("first.iso20022", first),
                ("nested/second.xml", second)
            },
            CompressionLevel.NoCompression);

        RepositoryArtifactOpenResult ambiguous = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(zip),
            new RepositoryArtifactOpenOptions());
        RepositoryArtifactOpenOptions selectedOptions = new RepositoryArtifactOpenOptions
        {
            EntryName = "nested/second.xml"
        };
        RepositoryArtifactOpenResult selected = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(zip),
            selectedOptions);
        byte[] selectedContent = ReadAll(selected.Content);
        selected.Content?.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.Success, Is.False);
            Assert.That(ambiguous.ErrorCode, Is.EqualTo("artifact.archive.entry_ambiguous"));
            Assert.That(ambiguous.WasArchive, Is.True);
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.EntryName, Is.EqualTo("nested/second.xml"));
            Assert.That(selectedContent, Is.EqualTo(second));
        });
    }

    [TestCase("../repository.iso20022")]
    [TestCase("nested/../../repository.iso20022")]
    [TestCase("/absolute/repository.iso20022")]
    [TestCase("\\\\server\\share\\repository.iso20022")]
    [TestCase("C:\\repository.iso20022")]
    [TestCase("./repository.iso20022")]
    public void UnsafeArchiveEntryPathsAreRejectedBeforeSelectionOrExpansion(string entryName)
    {
        byte[] zip = CreateZip(
            new[] { (entryName, Encoding.UTF8.GetBytes("<Repository/>")) },
            CompressionLevel.NoCompression);

        RepositoryArtifactOpenResult result = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(zip),
            new RepositoryArtifactOpenOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.WasArchive, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo("artifact.archive.path"));
            Assert.That(result.Content, Is.Null);
        });
    }

    [Test]
    public void ArtifactInputLimitIsEnforcedForRawAndArchiveBytes()
    {
        byte[] xml = Encoding.UTF8.GetBytes("<Repository/>");
        byte[] zip = CreateZip(
            new[] { ("repository.iso20022", xml) },
            CompressionLevel.NoCompression);
        RepositoryArtifactOpenOptions rawOptions = new RepositoryArtifactOpenOptions
        {
            MaxArtifactBytes = xml.Length - 1
        };
        RepositoryArtifactOpenOptions zipOptions = new RepositoryArtifactOpenOptions
        {
            MaxArtifactBytes = zip.Length - 1
        };

        RepositoryArtifactOpenResult raw = RepositoryArtifactOpener.OpenArtifact(new MemoryStream(xml), rawOptions);
        RepositoryArtifactOpenResult archive = RepositoryArtifactOpener.OpenArtifact(new MemoryStream(zip), zipOptions);

        Assert.Multiple(() =>
        {
            Assert.That(raw.ErrorCode, Is.EqualTo("artifact.input.limit"));
            Assert.That(archive.ErrorCode, Is.EqualTo("artifact.input.limit"));
            Assert.That(raw.Success, Is.False);
            Assert.That(archive.Success, Is.False);
        });
    }

    [Test]
    public void ArchiveEntryCountLimitIncludesDirectoriesAndNonRepositoryFiles()
    {
        byte[] zip = CreateZip(
            new[]
            {
                ("first.txt", Encoding.UTF8.GetBytes("first")),
                ("repository.iso20022", Encoding.UTF8.GetBytes("<Repository/>"))
            },
            CompressionLevel.NoCompression);
        RepositoryArtifactOpenOptions options = new RepositoryArtifactOpenOptions
        {
            MaxArchiveEntryCount = 1
        };

        RepositoryArtifactOpenResult result = RepositoryArtifactOpener.OpenArtifact(new MemoryStream(zip), options);

        Assert.That(result.ErrorCode, Is.EqualTo("artifact.archive.entry_count"));
    }

    [Test]
    public void ArchiveEntryAndCombinedExpansionLimitsAreEnforcedBeforeDecompression()
    {
        byte[] xml = Encoding.UTF8.GetBytes("<Repository>expanded</Repository>");
        byte[] entryZip = CreateZip(
            new[] { ("repository.iso20022", xml) },
            CompressionLevel.NoCompression);
        RepositoryArtifactOpenOptions entryOptions = new RepositoryArtifactOpenOptions
        {
            MaxArchiveEntryBytes = xml.Length - 1,
            MaxArchiveExpandedBytes = xml.Length * 2
        };
        byte[] totalZip = CreateZip(
            new[]
            {
                ("repository.iso20022", Encoding.UTF8.GetBytes("<R>1234</R>")),
                ("notes.txt", Encoding.UTF8.GetBytes("12345678"))
            },
            CompressionLevel.NoCompression);
        RepositoryArtifactOpenOptions totalOptions = new RepositoryArtifactOpenOptions
        {
            MaxArchiveEntryBytes = 12,
            MaxArchiveExpandedBytes = 12
        };

        RepositoryArtifactOpenResult entryResult = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(entryZip),
            entryOptions);
        RepositoryArtifactOpenResult totalResult = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(totalZip),
            totalOptions);

        Assert.Multiple(() =>
        {
            Assert.That(entryResult.ErrorCode, Is.EqualTo("artifact.archive.entry_size"));
            Assert.That(totalResult.ErrorCode, Is.EqualTo("artifact.archive.total_size"));
        });
    }

    [Test]
    public void CompressionRatioLimitRejectsHighlyCompressibleEntries()
    {
        byte[] xml = Encoding.UTF8.GetBytes("<Repository>" + new string('A', 32768) + "</Repository>");
        byte[] zip = CreateZip(
            new[] { ("repository.iso20022", xml) },
            CompressionLevel.SmallestSize);
        RepositoryArtifactOpenOptions options = new RepositoryArtifactOpenOptions
        {
            MaxArchiveCompressionRatio = 2
        };

        RepositoryArtifactOpenResult result = RepositoryArtifactOpener.OpenArtifact(new MemoryStream(zip), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("artifact.archive.ratio"));
            Assert.That(result.ErrorMessage, Does.Contain("MaxArchiveCompressionRatio"));
        });
    }

    [Test]
    public void MissingAndNonXmlSelectedEntriesReturnStableDiagnostics()
    {
        byte[] zip = CreateZip(
            new[] { ("payload.bin", new byte[] { 1, 2, 3, 4 }) },
            CompressionLevel.NoCompression);
        RepositoryArtifactOpenOptions missingOptions = new RepositoryArtifactOpenOptions
        {
            EntryName = "missing.iso20022"
        };
        RepositoryArtifactOpenOptions binaryOptions = new RepositoryArtifactOpenOptions
        {
            EntryName = "payload.bin"
        };

        RepositoryArtifactOpenResult missing = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(zip),
            missingOptions);
        RepositoryArtifactOpenResult binary = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(zip),
            binaryOptions);

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("artifact.archive.entry_not_found"));
            Assert.That(binary.ErrorCode, Is.EqualTo("artifact.archive.entry_media"));
            Assert.That(missing.Success, Is.False);
            Assert.That(binary.Success, Is.False);
        });
    }

    [Test]
    public void UnsupportedMediaAndInvalidLimitConfigurationAreRejectedDeterministically()
    {
        RepositoryArtifactOpenResult unsupported = RepositoryArtifactOpener.OpenArtifact(
            new MemoryStream(Encoding.UTF8.GetBytes("plain text")),
            new RepositoryArtifactOpenOptions());
        RepositoryArtifactOpenOptions zeroLimit = new RepositoryArtifactOpenOptions
        {
            MaxArchiveEntryCount = 0
        };
        RepositoryArtifactOpenOptions inconsistentLimits = new RepositoryArtifactOpenOptions
        {
            MaxArchiveEntryBytes = 20,
            MaxArchiveExpandedBytes = 10
        };

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.ErrorCode, Is.EqualTo("artifact.media.unsupported"));
            Assert.That(unsupported.Manifest.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.Throws<ArgumentOutOfRangeException>(() => RepositoryArtifactOpener.OpenArtifact(
                new MemoryStream(Encoding.UTF8.GetBytes("<R/>")),
                zeroLimit));
            Assert.Throws<ArgumentOutOfRangeException>(() => RepositoryArtifactOpener.OpenArtifact(
                new MemoryStream(Encoding.UTF8.GetBytes("<R/>")),
                inconsistentLimits));
        });
    }

    private static byte[] CreateZip((string Name, byte[] Content)[] entries, CompressionLevel compressionLevel)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach ((string name, byte[] content) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name, compressionLevel);
                using Stream stream = entry.Open();
                stream.Write(content, 0, content.Length);
            }
        }

        return output.ToArray();
    }

    private static byte[] WithPreamble(Encoding encoding, string text)
    {
        byte[] preamble = encoding.GetPreamble();
        byte[] content = encoding.GetBytes(text);
        byte[] bytes = new byte[preamble.Length + content.Length];
        Array.Copy(preamble, 0, bytes, 0, preamble.Length);
        Array.Copy(content, 0, bytes, preamble.Length, content.Length);
        return bytes;
    }

    private static byte[] ReadAll(Stream? stream)
    {
        Assert.That(stream, Is.Not.Null);
        using MemoryStream output = new MemoryStream();
        stream?.CopyTo(output);
        return output.ToArray();
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream source;

        public NonSeekableReadStream(byte[] bytes)
        {
            source = new MemoryStream(bytes, false);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return source.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                source.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
