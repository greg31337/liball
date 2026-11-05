namespace ttf.Tests;

internal class VariableStaticInstanceDifferentialTests
{
    // The variable font is a two-master synthetic fixture generated with
    // fontTools 4.60.2. The pinned static instance was produced by
    // fontTools.varLib.instancer at wght=700. Both fixtures are test-authored
    // and have SHA-256 values c8f44ea92a5365e5b14a5f47a9794316488701a7669e78eff378bfbb69b585b0
    // and 63d4ff2f6d6ab34462de45fa0b202db4f6d26c2ee9259eef6ca0fe31ce85c836.
    // The explicit outline and advance oracle below was captured with
    // FreeType 2.13.2 through freetype-py 2.5.1 using unscaled, unhinted loads.
    const string VariableFontBase64 =
        "AAEAAAAOAIAAAwBgSFZBUkAPQKcAAAP8AAAANE9TLzJFIUQDAAABaAAAAGBTVEFUeHBojAAABDAAAAAcY21hcAAMAJQAAAHQAAAA" +
        "NGZ2YXJ+HWmSAAAETAAAACRnbHlm/Ub+dgAAAgwAAAA8Z3Zhcm1nm7wAAARwAAAANmhlYWQpnjKvAAAA7AAAADZoaGVhBUgBxQAA" +
        "ASQAAAAkaG10eARMAJYAAAHIAAAACGxvY2EAHgANAAACBAAAAAZtYXhwAAQACAAAAUgAAAAgbmFtZXRCdFMAAAJIAAABjHBvc3QA" +
        "KAAAAAAD1AAAACYAAQAAAAEAAB+OCkFfDzz1AAMD6AAAAADht7EAAAAAAOaqPhMAMgAAAfQCvAAAAAMAAgAAAAAAAAABAAADIP84" +
        "AAACWAAyADIB9AABAAAAAAAAAAAAAAAAAAAAAgABAAAAAgAGAAEAAAAAAAIAAAAAAAAAAAAAAAAAAAAAAAMCJgGQAAUABAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAD8/Pz8AAABBAEEDIP84AAADIADIAAAAAAAAAAAAAAAA" +
        "AAAAIAAAAfQAMgJYAGQAAAACAAAAAwAAABQAAwABAAAAFAAEACAAAAAEAAQAAQAAAEH//wAAAEH////AAAEAAAAAAAAADQAeAAAA" +
        "AQAyAAABwgK8AAMAADMRIREyAZACvP1EAAABAGQAAAH0ArwABQAAMxMTIwMDZMjIZGRkArz9RAHg/iAAAAAOAK4AAQAAAAAAAQAL" +
        "AAAAAQAAAAAAAgAHAAsAAQAAAAAAAwATABIAAQAAAAAABAATABIAAQAAAAAABQANACUAAQAAAAAABgASADIAAQAAAAABAAAGAEQA" +
        "AwABBAkAAQAWAEoAAwABBAkAAgAOAGAAAwABBAkAAwAmAG4AAwABBAkABAAmAG4AAwABBAkABQAaAJQAAwABBAkABgAkAK4AAwAB" +
        "BAkBAAAMANJUMDk0IE9yYWNsZVJlZ3VsYXJUMDk0IE9yYWNsZSBSZWd1bGFyVmVyc2lvbiAxLjAwMFQwOTRPcmFjbGUtUmVndWxh" +
        "cldlaWdodABUADAAOQA0ACAATwByAGEAYwBsAGUAUgBlAGcAdQBsAGEAcgBUADAAOQA0ACAATwByAGEAYwBsAGUAIABSAGUAZwB1" +
        "AGwAYQByAFYAZQByAHMAaQBvAG4AIAAxAC4AMAAwADAAVAAwADkANABPAHIAYQBjAGwAZQAtAFIAZQBnAHUAbABhAHIAVwBlAGkA" +
        "ZwBoAHQAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAJAAAAAEAAAAAABQAAAAAAAAAAAAAAAAAAQAAAAwAAQAA" +
        "ABYAAQABAABAAEAAAAIAAAABAAAAeAABAAEACAABAAAAFAAAAAAAAAACd2dodAEAAAAAAQAAABAAAgABABQAAAAId2dodAGQAAAB" +
        "kAAAA4QAAAAAAQAAAQAAAAEAAAAAABoAAgAAAAAAGgAAAAAADoABAAoAEYAAQAAAB9gAKAoA9gB4gYAAPIEAxIQAAA==";

    const string FontToolsInstanceBase64 =
        "AAEAAAALAIAAAwAwT1MvMkZNRCcAAAE4AAAAYFNUQVR4cGiMAAADzAAAABxjbWFwAAwAlAAAAaAAAAA0Z2x5ZlVZLLgAAAHcAAAA" +
        "PGhlYWQptjLTAAAAvAAAADZoaGVhBWACDQAAAPQAAAAkaG10eASUAH4AAAGYAAAACGxvY2EAHgANAAAB1AAAAAZtYXhwAAQACAAA" +
        "ARgAAAAgbmFtZXRCdFMAAAIYAAABjHBvc3QAKAAAAAADpAAAACYAAQAAAAEAANmQTKdfDzz1AAMD6AAAAADht7EAAAAAAOaqPhMA" +
        "MgAAAgwC4AAAAAMAAgAAAAAAAAABAAADIP84AAACoAAyADICDAABAAAAAAAAAAAAAAAAAAAAAgABAAAAAgAGAAEAAAAAAAIAAAAA" +
        "AAAAAAAAAAAAAAAAAAMCSgK8AAUABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAD8/Pz8A" +
        "AABBAEEDIP84AAADIADIAAAAAAAAAAAAAAAAAAAAIAAAAfQAMgKgAEwAAAACAAAAAwAAABQAAwABAAAAFAAEACAAAAAEAAQAAQAA" +
        "AEH//wAAAEH////AAAEAAAAAAAAADQAeAAAAAQAyAAABwgK8AAMAAHMRIREyAZACvP1EAAABAEwAAAIMAuAABQAAcxMTIwMDTODg" +
        "dmpqAuD9IAG8/kQAAAAOAK4AAQAAAAAAAQALAAAAAQAAAAAAAgAHAAsAAQAAAAAAAwATABIAAQAAAAAABAATABIAAQAAAAAABQAN" +
        "ACUAAQAAAAAABgASADIAAQAAAAABAAAGAEQAAwABBAkAAQAWAEoAAwABBAkAAgAOAGAAAwABBAkAAwAmAG4AAwABBAkABAAmAG4A" +
        "AwABBAkABQAaAJQAAwABBAkABgAkAK4AAwABBAkBAAAMANJUMDk0IE9yYWNsZVJlZ3VsYXJUMDk0IE9yYWNsZSBSZWd1bGFyVmVy" +
        "c2lvbiAxLjAwMFQwOTRPcmFjbGUtUmVndWxhcldlaWdodABUADAAOQA0ACAATwByAGEAYwBsAGUAUgBlAGcAdQBsAGEAcgBUADAA" +
        "OQA0ACAATwByAGEAYwBsAGUAIABSAGUAZwB1AGwAYQByAFYAZQByAHMAaQBvAG4AIAAxAC4AMAAwADAAVAAwADkANABPAHIAYQBj" +
        "AGwAZQAtAFIAZQBnAHUAbABhAHIAVwBlAGkAZwBoAHQAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAJAAAAAEA" +
        "AQAIAAEAAAAUAAAAAAAAAAJ3Z2h0AQAAAA==";

    [Test]
    public void StaticInstanceMatchesPinnedFontToolsVarLibInstance()
    {
        FontFace source = Load(VariableFontBase64);
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(0x77676874, 700);
        byte[] actualBytes = source.CreateInstance(coordinates)
            .CopyStaticSfntBytes();
        FontInstance actual = FontLoader
            .Load(actualBytes, FontLoadOptions.CreateStrict())
            .GetFace(0)
            .CreateDefaultInstance();
        FontInstance expected = Load(FontToolsInstanceBase64)
            .CreateDefaultInstance();

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(actualBytes), Is.True);
            Assert.That(actual.GetGlyphCount(), Is.EqualTo(expected.GetGlyphCount()));
        });

        int glyphId = 0;
        while (glyphId < expected.GetGlyphCount())
        {
            AssertOutlinesEqual(
                expected.GetGlyphOutline(glyphId),
                actual.GetGlyphOutline(glyphId));
            AssertMetricsEqual(
                expected.GetGlyphMetrics(glyphId),
                actual.GetGlyphMetrics(glyphId));
            glyphId += 1;
        }
    }

    [Test]
    public void StaticInstanceMatchesPinnedFreeTypeOutlineAndAdvance()
    {
        FontFace source = Load(VariableFontBase64);
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(0x77676874, 700);
        FontInstance instance = FontLoader
            .Load(
                source.CreateInstance(coordinates).CopyStaticSfntBytes(),
                FontLoadOptions.CreateStrict())
            .GetFace(0)
            .CreateDefaultInstance();
        GlyphOutline outline = instance.GetGlyphOutline(1);

        Assert.Multiple(() =>
        {
            Assert.That(instance.GetGlyphMetrics(1).GetAdvanceWidth(),
                Is.EqualTo(672));
            Assert.That(instance.GetGlyphMetrics(1).GetLeftSideBearing(),
                Is.EqualTo(76));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(7));
        });

        AssertEndpoint(outline.GetCommand(0), GlyphPathCommandKind.Move, 76, 0);
        AssertEndpoint(outline.GetCommand(1), GlyphPathCommandKind.Line, 300, 736);
        AssertEndpoint(outline.GetCommand(2), GlyphPathCommandKind.Line, 524, 0);
        AssertEndpoint(outline.GetCommand(3), GlyphPathCommandKind.Line, 406, 0);
        AssertEndpoint(outline.GetCommand(4), GlyphPathCommandKind.Line, 300, 444);
        AssertEndpoint(outline.GetCommand(5), GlyphPathCommandKind.Line, 194, 0);
        Assert.That(outline.GetCommand(6).GetKind(),
            Is.EqualTo(GlyphPathCommandKind.Close));
    }

    static FontFace Load(string base64)
    {
        return FontLoader
            .Load(
                System.Convert.FromBase64String(base64),
                FontLoadOptions.CreateStrict())
            .GetFace(0);
    }

    internal static FontFace GetVariableFontFace()
    {
        return Load(VariableFontBase64);
    }

    internal static FontFace GetFontToolsInstanceFace()
    {
        return Load(FontToolsInstanceBase64);
    }

    static void AssertOutlinesEqual(GlyphOutline expected, GlyphOutline actual)
    {
        Assert.That(actual.GetCommandCount(),
            Is.EqualTo(expected.GetCommandCount()));
        int commandIndex = 0;
        while (commandIndex < expected.GetCommandCount())
        {
            GlyphPathCommand expectedCommand = expected.GetCommand(commandIndex);
            GlyphPathCommand actualCommand = actual.GetCommand(commandIndex);
            Assert.That(actualCommand.GetKind(),
                Is.EqualTo(expectedCommand.GetKind()));

            if (expectedCommand.GetKind() != GlyphPathCommandKind.Close)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(actualCommand.GetEndX(),
                        Is.EqualTo(expectedCommand.GetEndX()));
                    Assert.That(actualCommand.GetEndY(),
                        Is.EqualTo(expectedCommand.GetEndY()));
                });
            }

            commandIndex += 1;
        }
    }

    static void AssertMetricsEqual(GlyphMetrics expected, GlyphMetrics actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.GetAdvanceWidth(),
                Is.EqualTo(expected.GetAdvanceWidth()));
            Assert.That(actual.GetLeftSideBearing(),
                Is.EqualTo(expected.GetLeftSideBearing()));
        });
    }

    static void AssertEndpoint(
        GlyphPathCommand command,
        GlyphPathCommandKind kind,
        double x,
        double y)
    {
        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(kind));
            Assert.That(command.GetEndX(), Is.EqualTo(x));
            Assert.That(command.GetEndY(), Is.EqualTo(y));
        });
    }
}
