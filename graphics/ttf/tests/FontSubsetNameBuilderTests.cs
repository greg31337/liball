using ttf;

namespace ttf.Tests;

internal class FontSubsetNameBuilderTests
{
    [Test]
    public void RebuildsConsistentPrefixedNamesAndPreservesLicenses()
    {
        FontFace face = CreateNamedFace(BuildSourceNameTable());
        FontSubsetNameData data = BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData();
        SfntNameTable? table = Parse(data.CopyTableData());
        string prefix = data.GetPrefix();
        string family = prefix + "+Demo Sans";
        string postScript = prefix + "+DemoSans-Book";

        Assert.Multiple(() =>
        {
            Assert.That(prefix, Has.Length.EqualTo(6));
            Assert.That(IsUppercasePrefix(prefix), Is.True);
            Assert.That(data.GetFamilyName(), Is.EqualTo(family));
            Assert.That(data.GetFullName(), Is.EqualTo(family + " Book"));
            Assert.That(data.GetPostScriptName(), Is.EqualTo(postScript));
            Assert.That(Select(table, 1), Is.EqualTo(family));
            Assert.That(Select(table, 2), Is.EqualTo("Book"));
            Assert.That(Select(table, 3), Is.EqualTo(postScript));
            Assert.That(Select(table, 4), Is.EqualTo(family + " Book"));
            Assert.That(Select(table, 6), Is.EqualTo(postScript));
            Assert.That(Select(table, 16), Is.EqualTo(family));
            Assert.That(Select(table, 17), Is.EqualTo("Book"));
            Assert.That(Select(table, 13), Is.EqualTo("Open Font License"));
            Assert.That(Select(table, 14),
                Is.EqualTo("https://example.test/license"));
            Assert.That(Select(table, 5), Is.Null);
            Assert.That(table!.GetDuplicateRecordCount(), Is.Zero);
            Assert.That(table.GetInvalidRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void RetainSafeKeepsNonIdentityMetadataWhileMinimalDropsIt()
    {
        FontFace face = CreateNamedFace(BuildSourceNameTable());
        SfntNameTable? minimal = Parse(BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData().CopyTableData());
        SfntNameTable? retained = Parse(BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.RetainSafe,
            2).CreateNameData().CopyTableData());

        Assert.Multiple(() =>
        {
            Assert.That(Select(minimal, 0), Is.Null);
            Assert.That(Select(minimal, 5), Is.Null);
            Assert.That(Select(minimal, 256), Is.Null);
            Assert.That(Select(minimal, 13),
                Is.EqualTo("Open Font License"));
            Assert.That(Select(retained, 0), Is.EqualTo("Copyright Demo"));
            Assert.That(Select(retained, 5), Is.EqualTo("Version 2.5"));
            Assert.That(Select(retained, 256), Is.EqualTo("Palette Label"));
            Assert.That(Select(retained, 13),
                Is.EqualTo("Open Font License"));
        });
    }

    [Test]
    public void PreservesReferencedLicenseLanguageTags()
    {
        FontFace face = CreateNamedFace(BuildTaggedLicenseNameTable());
        SfntNameTable? table = Parse(BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            1).CreateNameData().CopyTableData());
        SfntLocalizedName? french = table!.SelectName(13, "fr");

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.EqualTo(1));
            Assert.That(table.GetLanguageTagCount(), Is.EqualTo(1));
            Assert.That(table.GetLanguageTagRecord(0).GetLanguageTag(),
                Is.EqualTo("fr"));
            Assert.That(french, Is.Not.Null);
            Assert.That(french!.GetValue(), Is.EqualTo("Licence ouverte"));
            Assert.That(table.GetInvalidLanguageTagCount(), Is.Zero);
            Assert.That(table.GetInvalidRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void PrefixIsDeterministicByGlyphSetAndAbsentForFullFont()
    {
        FontFace face = CreateNamedFace(BuildSourceNameTable());
        FontSubsetNameData first = BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData();
        FontSubsetNameData second = BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData();
        FontSubsetNameData different = BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            3).CreateNameData();
        FontSubsetNameData full = BuildSubset(
            face,
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData();
        byte[] copy = first.CopyTableData();
        copy[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(first.GetPrefix(), Is.EqualTo(second.GetPrefix()));
            Assert.That(first.CopyTableData(),
                Is.EqualTo(second.CopyTableData()));
            Assert.That(different.GetPrefix(), Is.Not.EqualTo(
                first.GetPrefix()));
            Assert.That(full.GetPrefix(), Is.Empty);
            Assert.That(full.GetFamilyName(), Is.EqualTo("Demo Sans"));
            Assert.That(full.GetPostScriptName(),
                Is.EqualTo("DemoSans-Book"));
            Assert.That(first.CopyTableData()[0], Is.Zero);
        });
    }

    [Test]
    public void CallerOverrideProvidesValidatedCollisionEscapePrefix()
    {
        FontFace face = CreateNamedFace(BuildSourceNameTable());
        FontSubsetNameData automatic = BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData();
        FontSubsetNameData overridden = BuildSubset(
            face,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2,
            "ABCDEF").CreateNameData();
        FontSubsetNameData full = BuildSubset(
            face,
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetMetadataPolicy.Minimal,
            2,
            "ABCDEF").CreateNameData();

        Assert.Multiple(() =>
        {
            Assert.That(overridden.GetPrefix(), Is.EqualTo("ABCDEF"));
            Assert.That(overridden.GetPrefix(),
                Is.Not.EqualTo(automatic.GetPrefix()));
            Assert.That(overridden.GetFamilyName(),
                Is.EqualTo("ABCDEF+Demo Sans"));
            Assert.That(overridden.GetPostScriptName(),
                Is.EqualTo("ABCDEF+DemoSans-Book"));
            Assert.That(full.GetPrefix(), Is.Empty);
            Assert.That(full.GetPostScriptName(),
                Is.EqualTo("DemoSans-Book"));
        });
    }

    [Test]
    public void AutomaticPrefixIncludesFaceCoordinatesAndPolicies()
    {
        byte[] changedName = BuildSourceNameTable();
        changedName[changedName.Length - 1] ^= 1;
        FontFace firstFace = CreateNamedFace(BuildSourceNameTable());
        FontFace secondFace = CreateNamedFace(changedName);
        string first = BuildSubset(
            firstFace,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData().GetPrefix();
        string changedIdentity = BuildSubset(
            secondFace,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData().GetPrefix();
        string changedPolicy = BuildSubset(
            firstFace,
            FontSubsetEmbeddingMode.Subset,
            FontSubsetMetadataPolicy.RetainSafe,
            2).CreateNameData().GetPrefix();
        FontFace variableFace = FontInstanceTests.CreateVariableFace(true);
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(Tag("wght"), 0.5);
        string defaultLocation = BuildSubset(
            variableFace.CreateDefaultInstance(),
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData().GetPrefix();
        string changedLocation = BuildSubset(
            variableFace.CreateInstance(coordinates),
            FontSubsetMetadataPolicy.Minimal,
            2).CreateNameData().GetPrefix();

        Assert.Multiple(() =>
        {
            Assert.That(changedIdentity, Is.Not.EqualTo(first));
            Assert.That(changedPolicy, Is.Not.EqualTo(first));
            Assert.That(changedLocation, Is.Not.EqualTo(defaultLocation));
            Assert.That(IsUppercasePrefix(first), Is.True);
            Assert.That(first, Has.Length.EqualTo(6));
        });
    }

    static FontSubset BuildSubset(
        FontFace face,
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetMetadataPolicy metadataPolicy,
        int glyphId,
        string? prefixOverride = null)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetEmbeddingMode(embeddingMode);
        options.SetMetadataPolicy(metadataPolicy);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);

        if (prefixOverride != null)
        {
            options.SetSubsetPrefixOverride(prefixOverride);
        }

        FontSubsetBuilder builder = new FontSubsetBuilder(
            face.CreateDefaultInstance(),
            options);
        builder.AddGlyph(glyphId);
        return builder.Build();
    }

    static FontSubset BuildSubset(
        FontInstance instance,
        FontSubsetMetadataPolicy metadataPolicy,
        int glyphId)
    {
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetMetadataPolicy(metadataPolicy);
        FontSubsetBuilder builder = new FontSubsetBuilder(instance, options);
        builder.AddGlyph(glyphId);
        return builder.Build();
    }

    static FontFace CreateNamedFace(byte[] name)
    {
        FontFace sourceFace = FontInstanceTests.CreateFace();
        SfntTableStore source = sourceFace.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            tables.Add(source.GetTable(index));
            index++;
        }

        AddTable(tables, FontTags.Name, name);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return new FontFace(
            0,
            sourceFace.GetScalerType(),
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                System.Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntNameTable? Parse(byte[] name)
    {
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        AddTable(tables, FontTags.Name, name);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                tables.ToArray(),
                System.Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible);
        return SfntNameTableParser.FindTable(cache);
    }

    static byte[] BuildSourceNameTable()
    {
        return BuildNameTable(
            new int[]
            {
                0, 1, 2, 3, 4, 5, 6, 13, 14, 16, 17, 256
            },
            new int[]
            {
                0x0409, 0x0409, 0x0409, 0x0409, 0x0409, 0x0409,
                0x0409, 0x0409, 0x0409, 0x0409, 0x0409, 0x0409
            },
            new string[]
            {
                "Copyright Demo",
                "Legacy Demo",
                "Regular",
                "Original Unique",
                "Legacy Demo Regular",
                "Version 2.5",
                "DemoSans-Book",
                "Open Font License",
                "https://example.test/license",
                "Demo Sans",
                "Book",
                "Palette Label"
            },
            new string[0]);
    }

    static byte[] BuildTaggedLicenseNameTable()
    {
        return BuildNameTable(
            new int[] { 1, 2, 6, 16, 17, 13 },
            new int[]
            {
                0x0409, 0x0409, 0x0409, 0x0409, 0x0409, 0x8000
            },
            new string[]
            {
                "Legacy Demo",
                "Regular",
                "DemoSans-Book",
                "Demo Sans",
                "Book",
                "Licence ouverte"
            },
            new string[] { "fr" });
    }

    static byte[] BuildNameTable(
        int[] nameIds,
        int[] languageIds,
        string[] values,
        string[] languageTags)
    {
        int version = languageTags.Length == 0 ? 0 : 1;
        byte[][] strings = new byte[values.Length][];
        byte[][] tags = new byte[languageTags.Length][];
        int index = 0;

        while (index < strings.Length)
        {
            strings[index] = System.Text.Encoding.BigEndianUnicode.GetBytes(
                values[index]);
            index++;
        }

        index = 0;

        while (index < tags.Length)
        {
            tags[index] = System.Text.Encoding.BigEndianUnicode.GetBytes(
                languageTags[index]);
            index++;
        }

        int headerLength = 6 + values.Length * 12 +
            (version == 0 ? 0 : 2 + tags.Length * 4);
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)version)
            .WriteUInt16((ushort)values.Length)
            .WriteUInt16((ushort)headerLength);
        int offset = 0;
        index = 0;

        while (index < strings.Length)
        {
            builder.WriteUInt16(3)
                .WriteUInt16(10)
                .WriteUInt16((ushort)languageIds[index])
                .WriteUInt16((ushort)nameIds[index])
                .WriteUInt16((ushort)strings[index].Length)
                .WriteUInt16((ushort)offset);
            offset += strings[index].Length;
            index++;
        }

        if (version == 1)
        {
            builder.WriteUInt16((ushort)tags.Length);
            index = 0;

            while (index < tags.Length)
            {
                builder.WriteUInt16((ushort)tags[index].Length)
                    .WriteUInt16((ushort)offset);
                offset += tags[index].Length;
                index++;
            }
        }

        index = 0;

        while (index < strings.Length)
        {
            builder.WriteBytes(strings[index]);
            index++;
        }

        index = 0;

        while (index < tags.Length)
        {
            builder.WriteBytes(tags[index]);
            index++;
        }

        return builder.CopyBytes();
    }

    static string? Select(SfntNameTable? table, int nameId)
    {
        SfntLocalizedName? name = table!.SelectDefaultName(nameId);
        return name == null ? null : name.GetValue();
    }

    static bool IsUppercasePrefix(string value)
    {
        int index = 0;

        while (index < value.Length)
        {
            if (value[index] < 'A' || value[index] > 'Z')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true));
    }
}
