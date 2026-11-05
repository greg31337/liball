using ttf;

namespace ttf.Tests;

internal class CffOutlineDifferentialTests
{
    // Source Serif 4 release commit 5f220b17d27ed64873f22cde0dd593685387bd19,
    // subset to .notdef and o with fontTools 4.60.2 under the upstream OFL-1.1.
    // The constants contain only the resulting raw CFF/CFF2 tables.
    const string SourceSerifCffBase64 =
        "AQAEAgABAQEVU291cmNlU2VyaWY0LVJlZ3VsYXIAAQEBIvgbAfgcDAD4HQNADAP7hPvVHAi8+n8F9+wPy/hEEvfvEQADAQFMorBT" +
        "b3VyY2UgaXMgYSB0cmFkZW1hcmsgb2YgQWRvYmUgaW4gdGhlIFVuaXRlZCBTdGF0ZXMgYW5kL29yIG90aGVyIGNvdW50cmllcy5D" +
        "b3B5cmlnaHQgMjAxNCAtIDIwMjMgQWRvYmUgKGh0dHA6Ly93d3cuYWRvYmUuY29tLyksIHdpdGggUmVzZXJ2ZWQgRm9udCBOYW1l" +
        "ICdTb3VyY2UnLlNvdXJjZSBTZXJpZiA0AAYBAQgeMkZOVlV+uPguuQs0XND3GvcbutPi4bxD+xv7GlpGNR8LIB0BuOr3oeoD96Z+" +
        "FSMduAQhHQv3CPcG3vc59zr7B+L7ByQdJR0fC/sI+wU0+zoL+zn3BDj3CQsAAFAAAgEBTVCwi774zL4B28j3+sgD2xb4dPky/HQG" +
        "6fz/Ffcm9373Jvt+BfvZ+J0V9yj7gfso+4EF92b3gRX3KPeBBfxuB/vZ+J0V97gG+yb7fgUOIh0OfJr4b5iymJSZ35iVmrifBvuO" +
        "lQd8mvhvmLKYlJnfmJWauJ8I+46VCR43XE8MCYsMC7kK4AuvlQwM4JUMDfjvFQ==";

    const string SourceSerifCff2Base64 =
        "AgAFAA33Mgwl+icMJPc1EaEYAAAAAACGAAEAAAAMAAEAAABwAAIACMAAwAAAAAAAAAAAAAAAQABAAAAAAAAAAAAAAAAAAMAAwAAA" +
        "AAAAAAAAAAAAQABAAMAAwAAAAMAAwAAAAMAAwAAAAAAAQABAAAAAQABAAMAAwAAAAAAAQABAAAAAQABAAAAAAAAACAAAAAEAAgAD" +
        "AAQABQAGAAcAAAEAAAACAgABAT8C6Iu++My+bp6Ki4yLjIvLU42MiYqJim6eioqMjIyMjhAB28j3+shnnoqLjIuMi9NljYuJi4mL" +
        "Z56Ki4yLjIuOEAPbFslmsYmKjYyNjIwQBvg2+TJNsGWNjImKiYqReYuLi4uLi7BljYyJiomKjhCL/Db9MmaxiYqNjI2MhZ2Li4uL" +
        "i4uNEAX4dBb8NvkyTWaxiYqNjI2MkXmLi4uLi4uwZY2MiYqJio4Qi/g2/TKwZY2MiYqJioWdi4uLi4uLjRAFyWaxiYqNjI2MjBAG" +
        "/De+Z56Ki4yLjItunoqLjIuMi40QFfjM9/r8zPv6y1ONjImKiYrTZY2LiYuJi0vDiYqNjI2MQ7GJi42LjYuPEAdOWK94jIuKi4qL" +
        "qHiMi4qLiouNEBX4dPkykXmLi4uLi4uMEPx0/TKFnYuLi4uLi4wQBn64+C65i4iMjIuLiopylZpzhJyRiLmKjaOVaXyOcZSZcoWd" +
        "komPEAG46veh6pJ0joqUiJaUTPSUdYObjIXw+yGuiZRniJVM9JR2g5qMhI8QA/emfoaXqXSQhZWSi4iMjIuLioqNEBX3CPcG3vc5" +
        "9zr7B+L7B/sI+wU0+zr7OfcEOPcJgsKfhIyFZnWHdpF7h4+woIOnoI2Li2CDkX6MgYWNrYqQfYuChouuipCQhpyOhnaZg5yUg5OK" +
        "b5OTZHaRi5KgfpRld5GKkp+Njo6Em5CHeIuTd36Oh4qqioaciZWSiWeJhZuIlJOLaImJd5N8hY6vjZNsfo6DjbWSgcKehI2FZoeb" +
        "EB+4cpWac4SckYiMEAQ0XND3GvcbutPi4bxD+xv7GlpGNXO2go+Tn46HcKaCh3+KioubgYqSi3WCcpKXkZCQjoqlkZWMkJWOkKWm" +
        "cJSPl4yMi5x+h5KGeYFzo2CUh4N3iI+kYZWHgneHj6Ruko2Zjo6NepiPh4yikaGFgYaHhYeKb4R/iYWCiYRzcqiEiX2IiIl7lYyB" +
        "j5ycpnK1gY+Un4+HmxAfAAAAAgEBBgv3Bvo5Evcu+qsSfJr5G5iNhoqKiY2QkImQjIyNiYaGknSMi4iNjouKjoqKjIyMjI8XBgce" +
        "N1xPi4sek3XG/4uLi4uLjBcMCYsMC7l0nJ11gJyKfowXCuBS9JZ5hJmOf4wXC7l0nJ11gJyKfowXDAzgUvSWeYSZjn+MFwwNfJr4" +
        "b5j3hp+NhoqKiY2QkImQjIyNiYaGhpesdIiMh4eLjoqKi4uMjJhpa6eOi42Oi4uDi4uLi4uRFwb7jpWGnZh2gY9+jouLjY2Lh4uJ" +
        "jRcHHjdcT4uLHpN1xv+Li4uLi4wXDAmLDAu5dJyddYCcin6MFwrgUvSWeYSZjn+MFwu5dJyddYCcin6MFwwM4FL0lnmEmY5/jBcM" +
        "DQ==";

    [Test]
    public void SourceSerifCffMatchesFontToolsAndFreeTypeOutlineOracle()
    {
        SfntCffTable? table = SfntCffTableParser.FindTable(
            CreateCache(FontTags.Cff, SourceSerifCffBase64));
        CffGlyph glyph = table!.GetNormalizedGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetCharStringsIndex().GetCount(), Is.EqualTo(2));
            Assert.That(table.GetCharset().GetName(1), Is.EqualTo("o"));
            Assert.That(glyph.GetAdvanceWidth(), Is.EqualTo(549));
        });
        AssertOracleOutline(glyph.GetOutline());
    }

    [Test]
    public void SourceSerifCff2DefaultMatchesFontToolsAndFreeTypeOutlineOracle()
    {
        SfntCff2Table? table = SfntCff2TableParser.FindTable(
            CreateCache(FontTags.Cff2, SourceSerifCff2Base64));
        Cff2VariationStore? store = table!.GetVariationData();
        CffGlyph glyph = table.GetNormalizedGlyph(1, new double[] { 0, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(table.GetCharStringsIndex().GetCount(), Is.EqualTo(2));
            Assert.That(store!.GetAxisCount(), Is.EqualTo(2));
            Assert.That(store.GetRegionCount(), Is.GreaterThan(0));
        });
        AssertOracleOutline(glyph.GetOutline());
    }

    static void AssertOracleOutline(SfntGlyphOutline outline)
    {
        SfntPathBounds bounds = outline.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(outline.GetContourCount(), Is.EqualTo(2));
            Assert.That(outline.GetCommandCount(), Is.EqualTo(12));
            Assert.That(outline.GetNormalizedPointCount(), Is.EqualTo(26));
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(45));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(-13));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(504));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(488));
        });

        AssertMove(outline.GetCommand(0), 274, -13);
        AssertCubic(outline.GetCommand(1), 390, -13, 504, 70, 504, 235);
        AssertCubic(outline.GetCommand(2), 504, 401, 389, 488, 274, 488);
        AssertCubic(outline.GetCommand(3), 158, 488, 45, 401, 45, 235);
        AssertCubic(outline.GetCommand(4), 45, 70, 157, -13, 274, -13);
        AssertClose(outline.GetCommand(5));
        AssertMove(outline.GetCommand(6), 274, 32);
        AssertCubic(outline.GetCommand(7), 187, 32, 140, 101, 140, 235);
        AssertCubic(outline.GetCommand(8), 140, 370, 187, 442, 274, 442);
        AssertCubic(outline.GetCommand(9), 360, 442, 409, 370, 409, 235);
        AssertCubic(outline.GetCommand(10), 409, 101, 360, 32, 274, 32);
        AssertClose(outline.GetCommand(11));
    }

    static void AssertMove(SfntPathCommand command, double x, double y)
    {
        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(SfntPathCommandKind.Move));
            Assert.That(command.GetEndX(), Is.EqualTo(x));
            Assert.That(command.GetEndY(), Is.EqualTo(y));
        });
    }

    static void AssertCubic(
        SfntPathCommand command,
        double firstX,
        double firstY,
        double secondX,
        double secondY,
        double endX,
        double endY)
    {
        Assert.Multiple(() =>
        {
            Assert.That(command.GetKind(), Is.EqualTo(SfntPathCommandKind.Cubic));
            Assert.That(command.GetControlX(), Is.EqualTo(firstX));
            Assert.That(command.GetControlY(), Is.EqualTo(firstY));
            Assert.That(command.GetSecondControlX(), Is.EqualTo(secondX));
            Assert.That(command.GetSecondControlY(), Is.EqualTo(secondY));
            Assert.That(command.GetEndX(), Is.EqualTo(endX));
            Assert.That(command.GetEndY(), Is.EqualTo(endY));
        });
    }

    static void AssertClose(SfntPathCommand command)
    {
        Assert.That(command.GetKind(), Is.EqualTo(SfntPathCommandKind.Close));
    }

    static SfntTableCache CreateCache(int tag, string base64)
    {
        byte[] bytes = Convert.FromBase64String(base64);
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        return SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Strict,
            FontLimits.CreateStrict());
    }
}
