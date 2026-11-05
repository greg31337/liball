using System.Buffers.Binary;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class FontLoadingPublicMatrixTests
{
    [Test]
    public void LoadsEveryPublicInputFormAndHonorsOwnership()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontLoader loader = factory.CreateFontLoader();
        byte[] expected = BuildValidSfnt(0x00010000U);
        byte[] arrayInput = expected.ToArray();
        byte[] copiedMemoryInput = expected.ToArray();
        byte[] spanInput = expected.ToArray();
        byte[] retainedMemoryInput = expected.ToArray();
        FontPackage arrayPackage = loader.LoadFromBytes(
            arrayInput,
            RetainOptions(factory));
        FontPackage copiedMemoryPackage = loader.LoadFromMemory(
            new ReadOnlyMemory<byte>(copiedMemoryInput),
            CompatibleOptions(factory));
        FontPackage spanPackage = loader.LoadFromSpan(
            spanInput.AsSpan(),
            RetainOptions(factory));
        FontPackage retainedMemoryPackage = loader.LoadFromMemory(
            new ReadOnlyMemory<byte>(retainedMemoryInput),
            RetainOptions(factory));
        ForwardOnlyStream stream = new ForwardOnlyStream(expected);
        FontPackage streamPackage = loader.LoadFromStream(
            stream,
            RetainOptions(factory));
        string path = Path.Combine(
            Path.GetTempPath(),
            "ttf-public-loader-" + Guid.NewGuid().ToString("N") + ".ttf");
        FontPackage filePackage;

        File.WriteAllBytes(path, expected);

        try
        {
            filePackage = loader.LoadFromFile(path, RetainOptions(factory));
        }
        finally
        {
            File.Delete(path);
        }

        int mutationOffset = expected.Length - 1;
        arrayInput[mutationOffset] ^= 0x7F;
        copiedMemoryInput[mutationOffset] ^= 0x7F;
        spanInput[mutationOffset] ^= 0x7F;
        retainedMemoryInput[mutationOffset] ^= 0x7F;

        Assert.Multiple(() =>
        {
            Assert.That(arrayPackage.CopyOriginalBytes(), Is.EqualTo(expected));
            Assert.That(copiedMemoryPackage.CopyOriginalBytes(),
                Is.EqualTo(expected));
            Assert.That(spanPackage.CopyOriginalBytes(), Is.EqualTo(expected));
            Assert.That(retainedMemoryPackage.CopyOriginalBytes()[mutationOffset],
                Is.EqualTo(retainedMemoryInput[mutationOffset]));
            Assert.That(streamPackage.CopyOriginalBytes(), Is.EqualTo(expected));
            Assert.That(filePackage.CopyOriginalBytes(), Is.EqualTo(expected));
            Assert.That(stream.GetBytesRead(), Is.EqualTo(expected.Length));
            Assert.That(stream.WasDisposed(), Is.False);
            Assert.That(File.Exists(path), Is.False);
        });
    }

    [TestCase(0x00010000U)]
    [TestCase(0x4F54544FU)]
    [TestCase(0x74727565U)]
    [TestCase(0x74797031U)]
    public void RecognizesEverySupportedScalarSfntType(uint scalerType)
    {
        IFontApiFactory factory = new FontApiFactory();
        byte[] font = BuildValidSfnt(scalerType);
        FontPackage package = factory.CreateFontLoader().LoadFromBytes(
            font,
            factory.CreateInspectOnlyLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(package.GetSourceKind(),
                Is.EqualTo(FontContainerKind.Sfnt));
            Assert.That(package.GetFaceCount(), Is.EqualTo(1));
            Assert.That(package.GetFace(0).GetScalerType(),
                Is.EqualTo(scalerType));
            Assert.That(package.GetProvenance().GetFaceOffset(0), Is.Zero);
        });
    }

    [Test]
    public void ReportsUnknownAndEveryRecognizedUnsupportedSignature()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontLoader loader = factory.CreateFontLoader();
        FontLoadOptions options = factory.CreateCompatibleLoadOptions();
        byte[][] unsupported = new byte[][]
        {
            new byte[] { 0x80, 0x01 },
            Encoding.ASCII.GetBytes("%!PS-AdobeFont-1.0: Test"),
            BuildEotHeader(),
            new byte[] { 0x01, 0x66, 0x63, 0x70 },
            Encoding.ASCII.GetBytes("STARTFONT 2.1\n")
        };
        int index = 0;

        while (index < unsupported.Length)
        {
            FontFormatException exception = Assert.Throws<FontFormatException>(
                () => loader.LoadFromBytes(unsupported[index], options))!;
            Assert.That(exception.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.UnsupportedContainer));
            index += 1;
        }

        FontFormatException unknown = Assert.Throws<FontFormatException>(() =>
            loader.LoadFromBytes(
                new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
                options))!;

        Assert.That(unknown.GetDiagnosticCode(),
            Is.EqualTo(FontDiagnosticCode.UnknownContainer));
    }

    [Test]
    public void LoadsVersionOneTtcAndVersionTwoOtcPublicly()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontLoader loader = factory.CreateFontLoader();
        FontLoadOptions inspect = factory.CreateInspectOnlyLoadOptions();
        FontPackage versionOne = loader.LoadFromBytes(
            BuildCollection(0x00010000U, false),
            inspect);
        FontPackage versionTwo = loader.LoadFromBytes(
            BuildCollection(0x00020000U, true),
            inspect);
        FontContainerProvenance first = versionOne.GetProvenance();
        FontContainerProvenance second = versionTwo.GetProvenance();

        Assert.Multiple(() =>
        {
            Assert.That(versionOne.GetSourceKind(),
                Is.EqualTo(FontContainerKind.Collection));
            Assert.That(versionOne.GetFaceCount(), Is.EqualTo(2));
            Assert.That(versionOne.GetFace(0).GetScalerType(),
                Is.EqualTo(0x00010000U));
            Assert.That(versionOne.GetFace(1).GetScalerType(),
                Is.EqualTo(0x4F54544FU));
            Assert.That(first.GetCollectionVersion(),
                Is.EqualTo(0x00010000U));
            Assert.That(first.HasCollectionDsig(), Is.False);
            Assert.That(versionTwo.GetFaceCount(), Is.EqualTo(2));
            Assert.That(second.GetCollectionVersion(),
                Is.EqualTo(0x00020000U));
            Assert.That(second.HasCollectionDsig(), Is.True);
            Assert.That(second.CopyCollectionDsigBytes(),
                Is.EqualTo(new byte[] { 9, 8, 7, 6, 5 }));
        });
    }

    [Test]
    public void LoadsWoffVariantsAndExposesOptionalBlocks()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontLoader loader = factory.CreateFontLoader();
        byte[] metadata = Encoding.UTF8.GetBytes(
            "<metadata version=\"1.0\"/>");
        byte[] privateData = new byte[] { 7, 5, 3, 1 };
        FontPackage woff1 = loader.LoadFromBytes(
            BuildWoff1(Encoding.UTF8.GetString(metadata), privateData),
            CompatibleOptions(factory, "font/woff"));
        FontPackage woff2 = loader.LoadFromBytes(
            BuildWoff2(false, metadata, privateData),
            CompatibleOptions(factory, "font/woff2"));
        FontPackage woff2Collection = loader.LoadFromBytes(
            BuildWoff2(true, metadata, privateData),
            factory.CreateInspectOnlyLoadOptions());
        FontPackage transformed = loader.LoadFromBytes(
            BuildTransformedWoff2(),
            CompatibleOptions(factory));

        AssertWoffProvenance(
            woff1,
            FontContainerKind.Woff1,
            metadata,
            privateData,
            "font/woff");
        AssertWoffProvenance(
            woff2,
            FontContainerKind.Woff2,
            metadata,
            privateData,
            "font/woff2");

        Assert.Multiple(() =>
        {
            Assert.That(woff1.CopySfntBytes(0), Is.Not.Empty);
            Assert.That(woff2.CopySfntBytes(0), Is.Not.Empty);
            Assert.That(woff2Collection.GetSourceKind(),
                Is.EqualTo(FontContainerKind.Woff2));
            Assert.That(woff2Collection.GetProvenance().HasCollectionHeader(),
                Is.True);
            Assert.That(transformed.GetSourceKind(),
                Is.EqualTo(FontContainerKind.Woff2));
            Assert.That(transformed.GetFace(0).HasTable(FontTags.Glyf),
                Is.True);
            Assert.That(transformed.GetFace(0).HasTable(FontTags.Loca),
                Is.True);
        });
    }

    [Test]
    public void AppliesEveryChecksumPolicyAndPreservesDiagnosticContext()
    {
        IFontApiFactory factory = new FontApiFactory();
        byte[] corrupt = BuildValidSfnt(0x00010000U);
        int checksumOffset = FindRecordOffset(corrupt, FontTags.Name) + 4;
        corrupt[checksumOffset] ^= 0x01;
        FontLoader loader = factory.CreateFontLoader();
        FontFormatException required = Assert.Throws<FontFormatException>(() =>
            loader.LoadFromBytes(corrupt, StrictOptions(factory)))!;
        FontPackage reported = loader.LoadFromBytes(
            corrupt,
            CompatibleOptions(factory));
        FontPackage ignored = loader.LoadFromBytes(
            corrupt,
            IgnoreChecksumOptions(factory));
        FontDiagnostic diagnostic = FindDiagnostic(
            reported.GetDiagnostics(),
            FontDiagnosticCode.TableChecksumMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(required.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.TableChecksumMismatch));
            Assert.That(diagnostic.GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(diagnostic.GetFaceIndex(), Is.Zero);
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Name));
            Assert.That(diagnostic.GetAbsoluteOffset(),
                Is.EqualTo(checksumOffset));
            Assert.That(diagnostic.GetLogicalPath(),
                Does.EndWith(".checksum"));
            Assert.That(reported.GetFace(0).GetChecksumValidationState(),
                Is.EqualTo(FontChecksumValidationState.Invalid));
            Assert.That(ignored.GetDiagnostics(), Is.Empty);
            Assert.That(ignored.GetFace(0).GetChecksumValidationState(),
                Is.EqualTo(FontChecksumValidationState.NotChecked));
        });
    }

    [Test]
    public void EnforcesContainerLoadingLimitsWithMachineReadableCodes()
    {
        IFontApiFactory factory = new FontApiFactory();
        FontLoader loader = factory.CreateFontLoader();
        byte[] sfnt = BuildValidSfnt(0x00010000U);
        byte[] collection = BuildCollection(0x00010000U, false);
        byte[] woff = BuildWoff1(
            "<metadata version=\"1.0\"/>",
            new byte[] { 1, 2, 3, 4 });

        AssertLimit(() => loader.LoadFromBytes(
            sfnt,
            OptionsWithLimits(factory, CreateLimits(
                factory,
                sfnt.Length - 1,
                536870912,
                256,
                1024,
                536870912,
                250,
                4096))));
        AssertLimit(() => loader.LoadFromBytes(
            collection,
            OptionsWithLimits(factory, CreateLimits(
                factory,
                268435456,
                536870912,
                1,
                1024,
                536870912,
                250,
                4096))));
        AssertLimit(() => loader.LoadFromBytes(
            sfnt,
            OptionsWithLimits(factory, CreateLimits(
                factory,
                268435456,
                536870912,
                256,
                1,
                536870912,
                250,
                4096))));
        AssertLimit(() => loader.LoadFromBytes(
            woff,
            OptionsWithLimits(factory, CreateLimits(
                factory,
                268435456,
                1,
                256,
                1024,
                536870912,
                250,
                4096))));
        AssertLimit(() => loader.LoadFromBytes(
            woff,
            OptionsWithLimits(factory, CreateLimits(
                factory,
                268435456,
                536870912,
                256,
                1024,
                1,
                250,
                4096))));
        AssertLimit(() => loader.LoadFromBytes(
            woff,
            OptionsWithLimits(factory, CreateLimits(
                factory,
                268435456,
                536870912,
                256,
                1024,
                536870912,
                1,
                4096))));
        AssertLimit(() => loader.LoadFromBytes(
            new TinySfntBuilder()
                .AddTable("name", new byte[] { 1 })
                .Build(),
            InspectOptionsWithLimits(factory, CreateLimits(
                factory,
                268435456,
                536870912,
                256,
                1024,
                536870912,
                250,
                1))));
    }

    [Test]
    public void ReturnsStructuredInspectDiagnosticsAndFailureCodes()
    {
        IFontApiFactory factory = new FontApiFactory();
        byte[] incomplete = new TinySfntBuilder()
            .AddTable("name", new byte[] { 1 })
            .Build();
        FontPackage package = factory.CreateFontLoader().LoadFromBytes(
            incomplete,
            factory.CreateInspectOnlyLoadOptions());
        FontDiagnostic diagnostic = FindDiagnostic(
            package.GetDiagnostics(),
            FontDiagnosticCode.MissingRequiredTable);
        FontFormatException failure = Assert.Throws<FontFormatException>(() =>
            factory.CreateFontLoader().LoadFromBytes(
                incomplete,
                factory.CreateCompatibleLoadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Error));
            Assert.That(diagnostic.GetFaceIndex(), Is.Zero);
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Cmap));
            Assert.That(diagnostic.GetAbsoluteOffset(), Is.EqualTo(-1));
            Assert.That(diagnostic.GetLogicalPath(),
                Is.EqualTo("sfnt.requiredTables"));
            Assert.That(diagnostic.GetMessage(), Is.Not.Empty);
            Assert.That(failure.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.MissingRequiredTable));
        });
    }

    static byte[] BuildValidSfnt(uint scalerType)
    {
        return AddRequiredSfntTables(
            new TinySfntBuilder().SetScalerType(scalerType)).Build();
    }

    static TinySfntBuilder AddRequiredSfntTables(TinySfntBuilder builder)
    {
        return builder
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[] { 1, 2, 3 })
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4]);
    }

    static byte[] BuildCollection(uint version, bool includeDsig)
    {
        int headerLength = version == 0x00020000U ? 32 : 20;
        int firstFaceOffset = headerLength;
        int secondFaceOffset = firstFaceOffset + 28;
        int firstTableOffset = secondFaceOffset + 28;
        int secondTableOffset = firstTableOffset + 4;
        int dsigOffset = secondTableOffset + 4;
        int totalLength = includeDsig ? dsigOffset + 5 : dsigOffset;
        byte[] bytes = new byte[totalLength];
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(0, 4),
            0x74746366U);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(4, 4),
            version);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 2);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(12, 4),
            (uint)firstFaceOffset);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(16, 4),
            (uint)secondFaceOffset);

        if (version == 0x00020000U)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                bytes.AsSpan(20, 4),
                includeDsig ? (uint)FontTags.Dsig : 0);
            BinaryPrimitives.WriteUInt32BigEndian(
                bytes.AsSpan(24, 4),
                includeDsig ? 5U : 0);
            BinaryPrimitives.WriteUInt32BigEndian(
                bytes.AsSpan(28, 4),
                includeDsig ? (uint)dsigOffset : 0);
        }

        WriteCollectionFace(
            bytes,
            firstFaceOffset,
            0x00010000U,
            firstTableOffset,
            new byte[] { 1, 2, 3, 4 });
        WriteCollectionFace(
            bytes,
            secondFaceOffset,
            0x4F54544FU,
            secondTableOffset,
            new byte[] { 5, 6, 7, 8 });

        if (includeDsig)
        {
            new byte[] { 9, 8, 7, 6, 5 }.CopyTo(bytes, dsigOffset);
        }

        return bytes;
    }

    static void WriteCollectionFace(
        byte[] bytes,
        int faceOffset,
        uint scalerType,
        int tableOffset,
        byte[] tableData)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset, 4),
            scalerType);
        BinaryPrimitives.WriteUInt16BigEndian(
            bytes.AsSpan(faceOffset + 4, 2),
            1);
        BinaryPrimitives.WriteUInt16BigEndian(
            bytes.AsSpan(faceOffset + 6, 2),
            16);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 12, 4),
            (uint)FontTags.Name);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 16, 4),
            FontChecksum.CalculateTable(tableData));
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 20, 4),
            (uint)tableOffset);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(faceOffset + 24, 4),
            (uint)tableData.Length);
        tableData.CopyTo(bytes, tableOffset);
    }

    static byte[] BuildWoff1(string metadata, byte[] privateData)
    {
        return new Woff1Builder()
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[256], true)
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .AddMetadata(metadata)
            .AddPrivateData(privateData)
            .Build();
    }

    static byte[] BuildWoff2(
        bool collection,
        byte[] metadata,
        byte[] privateData)
    {
        Woff2Builder builder = AddRequiredWoff2Tables(
            new Woff2Builder(),
            true)
            .AddKnownTable(10, new byte[4])
            .AddKnownTable(11, new byte[4])
            .AddKnownTable(3, new byte[4])
            .AddMetadata(metadata)
            .AddPrivateData(privateData);

        if (collection)
        {
            builder.AddCollectionFace(
                0x00010000U,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        return builder.Build();
    }

    static byte[] BuildTransformedWoff2()
    {
        byte[] transformedGlyf = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(2)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(4)
            .WriteUInt32(0)
            .WriteInt16(0)
            .WriteUInt32(0)
            .CopyBytes();
        return AddRequiredWoff2Tables(new Woff2Builder(), false)
            .AddTransformedKnownTable(
                10,
                0,
                (uint)transformedGlyf.Length,
                transformedGlyf)
            .AddTransformedKnownTable(11, 0, 4, Array.Empty<byte>())
            .AddTransformedKnownTable(
                3,
                1,
                4,
                new byte[] { 3, 1, 244 })
            .Build();
    }

    static Woff2Builder AddRequiredWoff2Tables(
        Woff2Builder builder,
        bool shortDependencies)
    {
        byte[] head = shortDependencies ? new byte[12] : BuildHead();
        byte[] hhea = shortDependencies ? new byte[4] : BuildHhea();
        byte[] maxp = shortDependencies ? new byte[4] : BuildMaxp();
        return builder
            .AddKnownTable(0, new byte[4])
            .AddKnownTable(1, head)
            .AddKnownTable(2, hhea)
            .AddKnownTable(4, maxp)
            .AddKnownTable(5, new byte[] { 1 })
            .AddKnownTable(6, new byte[4])
            .AddKnownTable(7, new byte[4]);
    }

    static byte[] BuildHead()
    {
        byte[] data = new byte[54];
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(50, 2), 0);
        return data;
    }

    static byte[] BuildHhea()
    {
        byte[] data = new byte[36];
        BinaryPrimitives.WriteUInt32BigEndian(
            data.AsSpan(0, 4),
            0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(34, 2), 1);
        return data;
    }

    static byte[] BuildMaxp()
    {
        byte[] data = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(
            data.AsSpan(0, 4),
            0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(4, 2), 1);
        return data;
    }

    static byte[] BuildEotHeader()
    {
        byte[] data = new byte[36];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0, 4), 100);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), 20);
        BinaryPrimitives.WriteUInt32LittleEndian(
            data.AsSpan(8, 4),
            0x00020001U);
        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(34, 2),
            0x504C);
        return data;
    }

    static void AssertWoffProvenance(
        FontPackage package,
        FontContainerKind expectedKind,
        byte[] metadata,
        byte[] privateData,
        string mediaType)
    {
        FontContainerProvenance provenance = package.GetProvenance();

        Assert.Multiple(() =>
        {
            Assert.That(package.GetSourceKind(), Is.EqualTo(expectedKind));
            Assert.That(provenance.HasWoffHeader(), Is.True);
            Assert.That(provenance.GetMediaTypeHint(), Is.EqualTo(mediaType));
            Assert.That(provenance.CopyWoffMetadataBytes(),
                Is.EqualTo(metadata));
            Assert.That(provenance.CopyWoffPrivateDataBytes(),
                Is.EqualTo(privateData));
            Assert.That(provenance.GetWoffMetadataCompressedLength(),
                Is.GreaterThan(0));
            Assert.That(provenance.GetWoffPrivateDataOffset(),
                Is.GreaterThan(0));
        });
    }

    static int FindRecordOffset(byte[] sfnt, int tag)
    {
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(
            sfnt.AsSpan(4, 2));
        int index = 0;

        while (index < tableCount)
        {
            int offset = 12 + index * 16;
            int recordTag = BinaryPrimitives.ReadInt32BigEndian(
                sfnt.AsSpan(offset, 4));

            if (recordTag == tag)
            {
                return offset;
            }

            index += 1;
        }

        throw new InvalidOperationException("The table record was not found.");
    }

    static FontDiagnostic FindDiagnostic(
        FontDiagnostic[] diagnostics,
        FontDiagnosticCode code)
    {
        int index = 0;

        while (index < diagnostics.Length)
        {
            if (diagnostics[index].GetCode() == code)
            {
                return diagnostics[index];
            }

            index += 1;
        }

        throw new AssertionException("The expected diagnostic was not found.");
    }

    static void AssertLimit(TestDelegate operation)
    {
        FontLimitException exception =
            Assert.Throws<FontLimitException>(operation)!;
        Assert.That(exception.GetDiagnosticCode(),
            Is.EqualTo(FontDiagnosticCode.LimitExceeded));
    }

    static FontLoadOptions CompatibleOptions(IFontApiFactory factory)
    {
        return factory.CreateCompatibleLoadOptions();
    }

    static FontLoadOptions CompatibleOptions(
        IFontApiFactory factory,
        string mediaType)
    {
        return factory.CreateConfiguredLoadOptionsWithMediaTypeHint(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            factory.CreateCompatibleLimits(),
            CancellationToken.None,
            mediaType);
    }

    static FontLoadOptions RetainOptions(IFontApiFactory factory)
    {
        return factory.CreateConfiguredLoadOptions(
            FontLoadMode.Compatible,
            FontInputOwnership.RetainReadOnlyMemory,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            factory.CreateCompatibleLimits(),
            CancellationToken.None);
    }

    static FontLoadOptions StrictOptions(IFontApiFactory factory)
    {
        return factory.CreateStrictLoadOptions();
    }

    static FontLoadOptions IgnoreChecksumOptions(IFontApiFactory factory)
    {
        return factory.CreateConfiguredLoadOptions(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.Ignore,
            factory.CreateCompatibleLimits(),
            CancellationToken.None);
    }

    static FontLoadOptions OptionsWithLimits(
        IFontApiFactory factory,
        FontLimits limits)
    {
        return factory.CreateConfiguredLoadOptions(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            limits,
            CancellationToken.None);
    }

    static FontLoadOptions InspectOptionsWithLimits(
        IFontApiFactory factory,
        FontLimits limits)
    {
        return factory.CreateConfiguredLoadOptions(
            FontLoadMode.InspectOnly,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.None,
            FontChecksumPolicy.ReportMismatch,
            limits,
            CancellationToken.None);
    }

    static FontLimits CreateLimits(
        IFontApiFactory factory,
        int maxInputBytes,
        int maxOutputBytes,
        int maxFaces,
        int maxTablesPerFace,
        int maxDecompressedBytes,
        int maxDecompressionRatio,
        int maxDiagnostics)
    {
        FontLimits defaults = factory.CreateCompatibleLimits();
        return factory.CreateConfiguredLimits(
            maxInputBytes,
            maxOutputBytes,
            maxFaces,
            maxTablesPerFace,
            defaults.GetMaxGlyphsPerFace(),
            defaults.GetMaxPointsPerGlyph(),
            defaults.GetMaxContoursPerGlyph(),
            defaults.GetMaxComponentDepth(),
            defaults.GetMaxCffSubroutineDepth(),
            defaults.GetMaxLayoutNesting(),
            defaults.GetMaxLookupsPerTable(),
            defaults.GetMaxLayoutOperations(),
            defaults.GetMaxCoverageEntries(),
            defaults.GetMaxStringBytes(),
            maxDecompressedBytes,
            maxDecompressionRatio,
            defaults.GetMaxVariationRegions(),
            defaults.GetMaxPaintDepth(),
            maxDiagnostics);
    }

    sealed class ForwardOnlyStream : Stream
    {
        byte[] data;
        int position;
        bool disposed;

        internal ForwardOnlyStream(byte[] data)
        {
            this.data = data.ToArray();
        }

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = data.Length - position;
            int length = Math.Min(available, count);
            data.AsSpan(position, length).CopyTo(
                buffer.AsSpan(offset, length));
            position += length;
            return length;
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
            disposed = true;
            base.Dispose(disposing);
        }

        internal int GetBytesRead()
        {
            return position;
        }

        internal bool WasDisposed()
        {
            return disposed;
        }
    }
}
