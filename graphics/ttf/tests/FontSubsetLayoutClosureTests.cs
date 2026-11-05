using ttf;

namespace ttf.Tests;

internal class FontSubsetLayoutClosureTests
{
    [Test]
    public void LayoutPolicyControlsWhetherClosureIsComputed()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetOptions drop = FontSubsetOptions.Create();
        FontSubsetOptions retain = FontSubsetOptions.Create();
        retain.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);

        FontSubsetLayoutClosure dropped =
            FontSubsetLayoutClosureBuilder.Create(
                instance,
                drop,
                new int[] { 0 },
                Array.Empty<int>(),
                20);
        FontSubsetLayoutClosure retained =
            FontSubsetLayoutClosureBuilder.Create(
                instance,
                retain,
                new int[] { 0 },
                Array.Empty<int>(),
                20);
        FontSubset droppedSubset = new FontSubsetBuilder(instance, drop)
            .Build();
        FontSubset retainedSubset = new FontSubsetBuilder(instance, retain)
            .Build();
        FontSubsetClosureReport droppedReport =
            droppedSubset.CreateResult().GetClosureReport();
        FontSubsetClosureReport retainedReport =
            retainedSubset.CreateResult().GetClosureReport();

        Assert.Multiple(() =>
        {
            Assert.That(dropped.IsApplicable(), Is.False);
            Assert.That(dropped.CopyGlyphIds(), Is.EqualTo(new int[] { 0 }));
            Assert.That(retained.IsApplicable(), Is.True);
            Assert.That(retained.CopyGlyphIds(), Is.EqualTo(new int[] { 0 }));
            Assert.That(retained.CopyGsubLookupIndices(), Is.Empty);
            Assert.That(droppedReport.IsLayoutApplicable(), Is.False);
            Assert.That(retainedReport.IsLayoutApplicable(), Is.True);
            Assert.That(retainedReport.GetGposLookupIndices(), Is.Empty);
        });
    }

    [Test]
    public void ComputesFeatureFilteredTransitiveGsubClosure()
    {
        int liga = Tag("liga");
        int rlig = Tag("rlig");
        int salt = Tag("salt");
        SfntGsubLookup[] lookups =
        {
            SingleLookup(1, 2),
            SingleLookup(2, 3),
            SingleLookup(3, 9)
        };
        SfntLayoutFeature[] features =
        {
            new SfntLayoutFeature(liga, 0, 0, new int[] { 0 }),
            new SfntLayoutFeature(rlig, 0, 0, new int[] { 1 }),
            new SfntLayoutFeature(salt, 0, 0, new int[] { 2 })
        };
        SfntLayoutLanguageSystem language =
            new SfntLayoutLanguageSystem(0, 1, Array.Empty<int>());
        SfntLayoutScript script = new SfntLayoutScript(
            Tag("latn"),
            language,
            Array.Empty<SfntLayoutLanguageSystem>());
        SfntLayoutTable layout = Layout(
            features,
            lookups.Select(value => value.GetLayoutLookup()).ToArray(),
            new SfntLayoutScript[] { script });
        SfntGsubTable gsub = new SfntGsubTable(
            layout,
            lookups,
            FontLimits.Create());

        FontSubsetLayoutClosure closure =
            FontSubsetLayoutClosureBuilder.CreateForTables(
                gsub,
                null!,
                null!,
                new int[] { 1 },
                new int[] { liga },
                20);

        Assert.Multiple(() =>
        {
            Assert.That(closure.CopyGlyphIds(),
                Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(closure.CopyGsubFeatureIndices(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(closure.CopyGsubLookupIndices(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(closure.CopyGlyphIds(), Does.Not.Contain(9));
        });
    }

    [Test]
    public void RetainsNestedGposLookupAndReferencedGdefMarkSet()
    {
        SfntSequenceLookupRecords records =
            new SfntSequenceLookupRecords(
                new SfntSequenceLookupRecord[]
                {
                    new SfntSequenceLookupRecord(0, 1)
                });
        SfntGsubContextSubstitution context =
            new SfntGsubContextSubstitution(
                3,
                Coverage(1),
                null,
                Array.Empty<SfntGsubContextRuleSet>(),
                new SfntCoverageTable[] { Coverage(1) },
                records);
        SfntLayoutLookup contextLayout = LayoutLookup(7, 0, 0);
        SfntLayoutLookup nestedLayout = LayoutLookup(1, 0x0010, 2);
        SfntGposLookup contextLookup = new SfntGposLookup(
            contextLayout,
            Array.Empty<SfntGposSingleAdjustment>(),
            Array.Empty<SfntGposPairAdjustment>(),
            Array.Empty<SfntGposCursiveAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            Array.Empty<SfntGposMarkLigatureAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            new SfntGsubContextSubstitution[] { context },
            Array.Empty<SfntGsubChainedContextSubstitution>(),
            Array.Empty<SfntExtensionLookup>(),
            7);
        SfntGposLookup nestedLookup = new SfntGposLookup(
            nestedLayout,
            new SfntGposSingleAdjustment[] { null! },
            Array.Empty<SfntGposPairAdjustment>(),
            Array.Empty<SfntGposCursiveAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            Array.Empty<SfntGposMarkLigatureAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            Array.Empty<SfntGsubContextSubstitution>(),
            Array.Empty<SfntGsubChainedContextSubstitution>(),
            Array.Empty<SfntExtensionLookup>(),
            1);
        SfntLayoutTable layout = Layout(
            new SfntLayoutFeature[]
            {
                new SfntLayoutFeature(
                    Tag("kern"), 0, 0, new int[] { 0 })
            },
            new SfntLayoutLookup[] { contextLayout, nestedLayout },
            Array.Empty<SfntLayoutScript>());
        SfntGposTable gpos = new SfntGposTable(
            layout,
            new SfntGposLookup[] { contextLookup, nestedLookup },
            FontLimits.Create());
        SfntGdefTable gdef = new SfntGdefTable(
            1,
            2,
            EmptyClasses(),
            null,
            null,
            null,
            new SfntGdefMarkGlyphSets(
                1,
                new SfntCoverageTable[]
                {
                    Coverage(4),
                    Coverage(5),
                    Coverage(6)
                }),
            null);

        FontSubsetLayoutClosure closure =
            FontSubsetLayoutClosureBuilder.CreateForTables(
                null!,
                gpos,
                gdef,
                new int[] { 1 },
                new int[] { Tag("kern") },
                20);

        Assert.Multiple(() =>
        {
            Assert.That(closure.CopyGposFeatureIndices(),
                Is.EqualTo(new int[] { 0 }));
            Assert.That(closure.CopyGposLookupIndices(),
                Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(closure.RetainsGdef(), Is.True);
            Assert.That(closure.CopyGdefMarkGlyphSetIndices(),
                Is.EqualTo(new int[] { 2 }));
            Assert.That(closure.CopyGlyphIds(), Is.EqualTo(new int[] { 1 }));
        });
    }

    [Test]
    public void EnforcesLayoutGlyphClosureLimit()
    {
        SfntGsubLookup lookup = SingleLookup(1, 2);
        SfntLayoutTable layout = Layout(
            new SfntLayoutFeature[]
            {
                new SfntLayoutFeature(
                    Tag("liga"), 0, 0, new int[] { 0 })
            },
            new SfntLayoutLookup[] { lookup.GetLayoutLookup() },
            Array.Empty<SfntLayoutScript>());

        Assert.Throws<FontLimitException>(() =>
            FontSubsetLayoutClosureBuilder.CreateForTables(
                new SfntGsubTable(
                    layout,
                    new SfntGsubLookup[] { lookup },
                    FontLimits.Create()),
                null!,
                null!,
                new int[] { 1 },
                Array.Empty<int>(),
                1));
    }

    [Test]
    public void HandlesLookupCyclesAndEnforcesSubsetNestingLimit()
    {
        SfntGsubLookup cyclicLookup = ContextLookup(0);
        SfntLayoutTable cyclicLayout = Layout(
            new SfntLayoutFeature[]
            {
                new SfntLayoutFeature(
                    Tag("calt"), 0, 0, new int[] { 0 })
            },
            new SfntLayoutLookup[] { cyclicLookup.GetLayoutLookup() },
            Array.Empty<SfntLayoutScript>());
        FontSubsetLayoutClosure cycleClosure =
            FontSubsetLayoutClosureBuilder.CreateForTables(
                new SfntGsubTable(
                    cyclicLayout,
                    new SfntGsubLookup[] { cyclicLookup },
                    FontLimits.Create()),
                null,
                null,
                new int[] { 1 },
                Array.Empty<int>(),
                20,
                1);

        SfntGsubLookup firstLookup = ContextLookup(1);
        SfntGsubLookup secondLookup = ContextLookup(1);
        SfntLayoutTable nestedLayout = Layout(
            new SfntLayoutFeature[]
            {
                new SfntLayoutFeature(
                    Tag("calt"), 0, 0, new int[] { 0 })
            },
            new SfntLayoutLookup[]
            {
                firstLookup.GetLayoutLookup(),
                secondLookup.GetLayoutLookup()
            },
            Array.Empty<SfntLayoutScript>());
        FontLimitException? exception = Assert.Throws<FontLimitException>(() =>
            FontSubsetLayoutClosureBuilder.CreateForTables(
                new SfntGsubTable(
                    nestedLayout,
                    new SfntGsubLookup[] { firstLookup, secondLookup },
                    FontLimits.Create()),
                null,
                null,
                new int[] { 1 },
                Array.Empty<int>(),
                20,
                1));

        Assert.Multiple(() =>
        {
            Assert.That(cycleClosure.CopyGsubLookupIndices(),
                Is.EqualTo(new int[] { 0 }));
            Assert.That(exception!.Message, Is.EqualTo(
                "Layout subset closure exceeds the configured nesting limit."));
        });
    }

    [Test]
    public void RejectsRetainedLookupWithMissingGdefFilterData()
    {
        SfntLayoutLookup layoutLookup = LayoutLookup(1, 0x0010, 0);
        SfntGposLookup lookup = new SfntGposLookup(
            layoutLookup,
            new SfntGposSingleAdjustment[] { null! },
            Array.Empty<SfntGposPairAdjustment>(),
            Array.Empty<SfntGposCursiveAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            Array.Empty<SfntGposMarkLigatureAdjustment>(),
            Array.Empty<SfntGposMarkAttachment>(),
            Array.Empty<SfntGsubContextSubstitution>(),
            Array.Empty<SfntGsubChainedContextSubstitution>(),
            Array.Empty<SfntExtensionLookup>(),
            1);
        SfntLayoutTable layout = Layout(
            new SfntLayoutFeature[]
            {
                new SfntLayoutFeature(
                    Tag("kern"), 0, 0, new int[] { 0 })
            },
            new SfntLayoutLookup[] { layoutLookup },
            Array.Empty<SfntLayoutScript>());

        Assert.Throws<FontFormatException>(() =>
            FontSubsetLayoutClosureBuilder.CreateForTables(
                null!,
                new SfntGposTable(
                    layout,
                    new SfntGposLookup[] { lookup },
                    FontLimits.Create()),
                null!,
                new int[] { 1 },
                Array.Empty<int>(),
                20));
    }

    static SfntGsubLookup SingleLookup(int input, int output)
    {
        SfntLayoutLookup layout = LayoutLookup(1, 0, 0);
        return new SfntGsubLookup(
            layout,
            new SfntGsubSingleSubstitution[]
            {
                new SfntGsubSingleSubstitution(
                    2,
                    Coverage(input),
                    0,
                    new int[] { output })
            },
            Array.Empty<SfntGsubMultipleSubstitution>(),
            Array.Empty<SfntGsubAlternateSubstitution>(),
            Array.Empty<SfntGsubLigatureSubstitution>(),
            Array.Empty<SfntGsubContextSubstitution>(),
            Array.Empty<SfntGsubChainedContextSubstitution>(),
            Array.Empty<SfntExtensionLookup>(),
            Array.Empty<SfntGsubReverseSubstitution>(),
            1);
    }

    static SfntGsubLookup ContextLookup(int nestedLookupIndex)
    {
        SfntSequenceLookupRecords records =
            new SfntSequenceLookupRecords(
                new SfntSequenceLookupRecord[]
                {
                    new SfntSequenceLookupRecord(0, nestedLookupIndex)
                });
        SfntGsubContextSubstitution context =
            new SfntGsubContextSubstitution(
                3,
                Coverage(1),
                null,
                Array.Empty<SfntGsubContextRuleSet>(),
                new SfntCoverageTable[] { Coverage(1) },
                records);
        SfntLayoutLookup layout = LayoutLookup(5, 0, 0);
        return new SfntGsubLookup(
            layout,
            Array.Empty<SfntGsubSingleSubstitution>(),
            Array.Empty<SfntGsubMultipleSubstitution>(),
            Array.Empty<SfntGsubAlternateSubstitution>(),
            Array.Empty<SfntGsubLigatureSubstitution>(),
            new SfntGsubContextSubstitution[] { context },
            Array.Empty<SfntGsubChainedContextSubstitution>(),
            Array.Empty<SfntExtensionLookup>(),
            Array.Empty<SfntGsubReverseSubstitution>(),
            5);
    }

    static SfntLayoutLookup LayoutLookup(
        int type,
        int flags,
        int markFilteringSet)
    {
        return new SfntLayoutLookup(
            type,
            flags,
            new int[] { 0 },
            new int[] { 0 },
            markFilteringSet);
    }

    static SfntLayoutTable Layout(
        SfntLayoutFeature[] features,
        SfntLayoutLookup[] lookups,
        SfntLayoutScript[] scripts)
    {
        return new SfntLayoutTable(
            1,
            0,
            0,
            new SfntLayoutScriptList(scripts),
            new SfntLayoutFeatureList(features),
            new SfntLayoutLookupList(lookups),
            null!);
    }

    static SfntCoverageTable Coverage(params int[] glyphIds)
    {
        return new SfntCoverageTable(
            1,
            glyphIds,
            Array.Empty<SfntCoverageRange>(),
            glyphIds.Length);
    }

    static SfntClassDefinitionTable EmptyClasses()
    {
        return new SfntClassDefinitionTable(
            1,
            0,
            Array.Empty<int>(),
            Array.Empty<SfntClassRange>(),
            0);
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
