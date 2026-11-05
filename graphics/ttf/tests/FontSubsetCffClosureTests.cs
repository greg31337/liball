namespace ttf.Tests;

internal class FontSubsetCffClosureTests
{
    [Test]
    public void ComputesTransitiveLocalGlobalAndFontDictionaryClosure()
    {
        byte[][] glyphPrograms =
        {
            new byte[] { 14 },
            new byte[] { 32, 10, 14 },
            new byte[] { 14 }
        };
        int[] glyphFontDictionaries = { 0, 1, 0 };
        byte[][] globalPrograms =
        {
            new byte[] { 11 },
            new byte[] { 34, 10, 11 },
            new byte[] { 11 }
        };
        byte[][][] localPrograms =
        {
            new byte[][]
            {
                new byte[] { 11 }
            },
            new byte[][]
            {
                new byte[] { 33, 29, 11 },
                new byte[] { 11 },
                new byte[] { 11 }
            }
        };

        FontSubsetCffClosure closure =
            FontSubsetCffClosureBuilder.CreateForPrograms(
                glyphPrograms,
                glyphFontDictionaries,
                globalPrograms,
                localPrograms,
                true,
                new int[] { 1 },
                32,
                10);

        Assert.That(closure.IsApplicable(), Is.True);
        Assert.That(closure.CopyFontDictionaryIndices(), Is.EqualTo(
            new int[] { 1 }));
        Assert.That(closure.CopyGlobalSubroutineIndices(), Is.EqualTo(
            new int[] { 1 }));
        FontSubsetCffLocalSubroutineClosure[] locals =
            closure.CopyLocalSubroutines();
        Assert.That(locals, Has.Length.EqualTo(1));
        Assert.That(locals[0].GetFontDictionaryIndex(), Is.EqualTo(1));
        Assert.That(locals[0].CopySubroutineIndices(), Is.EqualTo(
            new int[] { 0, 2 }));
        Assert.That(closure.CopyDiagnostics(), Is.Empty);
    }

    [Test]
    public void DynamicCallsConservativelyRetainEveryCandidate()
    {
        byte[][] glyphPrograms =
        {
            new byte[] { 29, 14 }
        };
        byte[][] globalPrograms =
        {
            new byte[] { 10, 11 },
            new byte[] { 11 }
        };
        byte[][][] localPrograms =
        {
            new byte[][]
            {
                new byte[] { 11 },
                new byte[] { 11 },
                new byte[] { 11 }
            }
        };

        FontSubsetCffClosure closure =
            FontSubsetCffClosureBuilder.CreateForPrograms(
                glyphPrograms,
                new int[] { 0 },
                globalPrograms,
                localPrograms,
                false,
                new int[] { 0 },
                32,
                10);

        Assert.That(closure.CopyFontDictionaryIndices(), Is.Empty);
        Assert.That(closure.CopyGlobalSubroutineIndices(), Is.EqualTo(
            new int[] { 0, 1 }));
        Assert.That(
            closure.CopyLocalSubroutines()[0].CopySubroutineIndices(),
            Is.EqualTo(new int[] { 0, 1, 2 }));
        FontDiagnostic[] diagnostics = closure.CopyDiagnostics();
        Assert.That(diagnostics, Has.Length.EqualTo(2));
        Assert.That(
            diagnostics.Select(value => value.GetCode()),
            Is.All.EqualTo(FontDiagnosticCode.DynamicSubroutineFallback));
        Assert.That(
            diagnostics.Select(value => value.GetSeverity()),
            Is.All.EqualTo(FontDiagnosticSeverity.Warning));
    }

    [Test]
    public void RejectsStaticCallsOutsideTheirSubroutineIndex()
    {
        FontFormatException? error = Assert.Throws<FontFormatException>(() =>
            FontSubsetCffClosureBuilder.CreateForPrograms(
                new byte[][]
                {
                    new byte[] { 33, 29, 14 }
                },
                new int[] { 0 },
                new byte[][]
                {
                    new byte[] { 11 }
                },
                new byte[][][]
                {
                    new byte[0][]
                },
                false,
                new int[] { 0 },
                32,
                10));

        Assert.That(error!.Message, Does.Contain("outside its INDEX"));
    }

    [Test]
    public void FallsBackWhenInheritedStemStateMakesMaskLengthUncertain()
    {
        FontSubsetCffClosure closure =
            FontSubsetCffClosureBuilder.CreateForPrograms(
                new byte[][]
                {
                    new byte[] { 32, 10, 19, 0, 14 }
                },
                new int[] { 0 },
                new byte[0][],
                new byte[][][]
                {
                    new byte[][]
                    {
                        new byte[] { 11 },
                        new byte[] { 11 }
                    }
                },
                false,
                new int[] { 0 },
                32,
                10);

        Assert.That(
            closure.CopyLocalSubroutines()[0].CopySubroutineIndices(),
            Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(closure.CopyDiagnostics(), Has.Length.EqualTo(1));
    }

    [Test]
    public void EnforcesConfiguredClosureEntryLimit()
    {
        Assert.Throws<FontLimitException>(() =>
            FontSubsetCffClosureBuilder.CreateForPrograms(
                new byte[][]
                {
                    new byte[] { 29, 14 }
                },
                new int[] { 0 },
                new byte[][]
                {
                    new byte[] { 11 },
                    new byte[] { 11 }
                },
                new byte[][][]
                {
                    new byte[0][]
                },
                false,
                new int[] { 0 },
                1,
                10));
    }
}
