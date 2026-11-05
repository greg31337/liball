namespace svg.Tests;

public class SvgOpenTypeLayoutTests
{
    [Test]
    public void MapsScalarsAndAppliesSelectedLigatureSubstitution()
    {
        SvgFont font = CreateFont(new Dictionary<int, int> { [0x66] = 3, [0x69] = 4 }, new Dictionary<string, byte[]> { ["GSUB"] = Layout("liga", 4, LigatureSubtable()) });

        SvgShapingResult shaped = new SvgOpenTypeLayout().ShapeWithFontAndText(font, "fi");
        SvgShapingOptions disabledOptions = new SvgShapingOptions();
        disabledOptions.Features.Add("kern");
        SvgShapingResult disabled = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "fi", disabledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(shaped.Success, Is.True);
            Assert.That(shaped.Glyphs.Select(glyph => glyph.GlyphIndex), Is.EqualTo(new[] { 6 }));
            Assert.That(shaped.Glyphs[0].ScalarStart, Is.Zero);
            Assert.That(shaped.Glyphs[0].ScalarCount, Is.EqualTo(2));
            Assert.That(shaped.Glyphs[0].Utf16Length, Is.EqualTo(2));
            Assert.That(shaped.Glyphs[0].LigatureComponent, Is.EqualTo(2));
            Assert.That(shaped.AppliedFeatures, Is.EqualTo(new[] { "liga" }));
            Assert.That(shaped.Glyphs[0].XAdvance, Is.EqualTo(9.6).Within(0.0001));
            Assert.That(disabled.Success, Is.True);
            Assert.That(disabled.Glyphs.Select(glyph => glyph.GlyphIndex), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(disabled.AppliedFeatures, Is.Empty);
        });
    }

    [Test]
    public void SelectsLookupsForTheRequestedScriptAndLanguage()
    {
        SvgFont font = CreateFont(
            new Dictionary<int, int> { [0x66] = 3, [0x69] = 4 },
            new Dictionary<string, byte[]> { ["GSUB"] = LanguageLayout("latn", "TRK ", "liga", 4, LigatureSubtable()) });
        SvgShapingOptions selectedOptions = new SvgShapingOptions();
        selectedOptions.ScriptTag = "latn";
        selectedOptions.LanguageTag = "TRK ";
        SvgShapingOptions absentOptions = new SvgShapingOptions();
        absentOptions.ScriptTag = "latn";
        absentOptions.LanguageTag = "ROM ";

        SvgShapingResult selected = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "fi", selectedOptions);
        SvgShapingResult absent = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "fi", absentOptions);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Success, Is.True);
            Assert.That(selected.Glyphs.Select(glyph => glyph.GlyphIndex), Is.EqualTo(new[] { 6 }));
            Assert.That(selected.AppliedFeatures, Is.EqualTo(new[] { "liga" }));
            Assert.That(absent.Success, Is.True);
            Assert.That(absent.Glyphs.Select(glyph => glyph.GlyphIndex), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(absent.AppliedFeatures, Is.Empty);
        });
    }

    [Test]
    public void AppliesGposPairKerningBeforeLegacyKernFallback()
    {
        Dictionary<int, int> mappings = new Dictionary<int, int> { [0x41] = 1, [0x56] = 2 };
        SvgFont gposFont = CreateFont(mappings, new Dictionary<string, byte[]> { ["GPOS"] = Layout("kern", 2, PairSubtable(-80)), ["kern"] = KernTable(-50) });
        SvgFont legacyFont = CreateFont(mappings, new Dictionary<string, byte[]> { ["kern"] = KernTable(-50) });

        SvgShapingResult gpos = new SvgOpenTypeLayout().ShapeWithFontAndText(gposFont, "AV");
        SvgShapingResult legacy = new SvgOpenTypeLayout().ShapeWithFontAndText(legacyFont, "AV");

        Assert.Multiple(() =>
        {
            Assert.That(gpos.Success, Is.True);
            Assert.That(gpos.Glyphs[0].XAdvance, Is.EqualTo(8.32).Within(0.0001));
            Assert.That(gpos.Glyphs[1].XAdvance, Is.EqualTo(9.6).Within(0.0001));
            Assert.That(gpos.AppliedFeatures, Is.EqualTo(new[] { "kern" }));
            Assert.That(legacy.Success, Is.True);
            Assert.That(legacy.Glyphs[0].XAdvance, Is.EqualTo(8.8).Within(0.0001));
        });
    }

    [Test]
    public void UsesGdefClassesAndAttachesMarksToBaseAnchors()
    {
        SvgFont font = CreateFont(
            new Dictionary<int, int> { [0x41] = 1, [0x0301] = 5 },
            new Dictionary<string, byte[]> { ["GDEF"] = GdefTable(), ["GPOS"] = Layout("mark", 4, MarkBaseSubtable()) });

        SvgShapingResult result = new SvgOpenTypeLayout().ShapeWithFontAndText(font, "A\u0301");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Glyphs, Has.Count.EqualTo(2));
            Assert.That(result.Glyphs[0].GlyphClass, Is.EqualTo(SvgOpenTypeGlyphClass.Base));
            Assert.That(result.Glyphs[1].GlyphClass, Is.EqualTo(SvgOpenTypeGlyphClass.Mark));
            Assert.That(result.Glyphs[1].XAdvance, Is.Zero);
            Assert.That(result.Glyphs[1].AttachmentIndex, Is.Zero);
            Assert.That(result.Glyphs[1].XOffset, Is.EqualTo(3.2).Within(0.0001));
            Assert.That(result.Glyphs[1].YOffset, Is.EqualTo(8).Within(0.0001));
        });
    }

    [Test]
    public void ConnectsCursiveEntryAndExitAnchors()
    {
        SvgFont font = CreateFont(
            new Dictionary<int, int> { [0x0627] = 7, [0x0628] = 8 },
            new Dictionary<string, byte[]> { ["GPOS"] = Layout("curs", 3, CursiveSubtable()) });

        SvgShapingResult result = new SvgOpenTypeLayout().ShapeWithFontAndText(font, "اب");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Glyphs[1].AttachmentIndex, Is.Zero);
            Assert.That(result.Glyphs[1].XOffset, Is.EqualTo(16).Within(0.0001));
            Assert.That(result.Glyphs[1].YOffset, Is.Zero);
        });
    }

    [Test]
    public void ClampsNormalizesAndAvarMapsVariationCoordinates()
    {
        SvgFont font = CreateFont(
            new Dictionary<int, int> { [0x41] = 1 },
            new Dictionary<string, byte[]> { ["fvar"] = FvarTable(), ["avar"] = AvarTable() });
        SvgShapingOptions options = new SvgShapingOptions();
        options.VariationCoordinates.Add(new SvgFontVariationCoordinate("wght", 1000));

        SvgShapingResult result = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.VariationCoordinates, Has.Count.EqualTo(1));
            Assert.That(result.VariationCoordinates[0].AxisTag, Is.EqualTo("wght"));
            Assert.That(result.VariationCoordinates[0].RequestedValue, Is.EqualTo(1000));
            Assert.That(result.VariationCoordinates[0].Value, Is.EqualTo(900));
            Assert.That(result.VariationCoordinates[0].NormalizedValue, Is.EqualTo(0.5).Within(0.0001));
        });
    }

    [Test]
    public void UsesVerticalMetricsAndReportsUnsupportedLookupsDeterministically()
    {
        SvgFont font = CreateFont(
            new Dictionary<int, int> { [0x41] = 1 },
            new Dictionary<string, byte[]> { ["GSUB"] = Layout("ccmp", 5, new byte[] { 0, 1 }) });
        font.VerticalHeader = new SvgOpenTypeVerticalHeaderTable();
        for (int index = 0; index < font.MaximumProfile.GlyphCount; index++) font.VerticalMetrics.Add(new SvgOpenTypeVerticalMetric(index, 1000, 0));
        SvgShapingOptions options = new SvgShapingOptions();
        options.Vertical = true;

        SvgShapingResult result = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.UnsupportedLookupTypes, Is.EqualTo(new[] { 5 }));
            Assert.That(result.Glyphs[0].XAdvance, Is.Zero);
            Assert.That(result.Glyphs[0].YAdvance, Is.EqualTo(16));
        });
    }

    [Test]
    public void HonorsInvalidInputBoundsMalformedTablesAndCancellation()
    {
        SvgFont font = CreateFont(new Dictionary<int, int> { [0x41] = 1, [0x42] = 2 }, new Dictionary<string, byte[]>());
        SvgShapingOptions glyphLimit = new SvgShapingOptions();
        glyphLimit.MaxGlyphs = 1;
        SvgShapingOptions operationLimit = new SvgShapingOptions();
        operationLimit.MaxOperations = 1;
        SvgShapingOptions unknownAxis = new SvgShapingOptions();
        unknownAxis.VariationCoordinates.Add(new SvgFontVariationCoordinate("wght", 400));
        SvgShapingOptions cancelled = new SvgShapingOptions();
        System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();
        source.Cancel();
        cancelled.CancellationToken = source.Token;
        SvgFont malformed = CreateFont(new Dictionary<int, int> { [0x41] = 1 }, new Dictionary<string, byte[]> { ["GSUB"] = new byte[4] });

        SvgShapingResult nullText = new SvgOpenTypeLayout().ShapeWithFontAndText(font, (string)null);
        SvgShapingResult tooManyGlyphs = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "AB", glyphLimit);
        SvgShapingResult tooManyOperations = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "AB", operationLimit);
        SvgShapingResult absentAxis = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", unknownAxis);
        SvgShapingResult stopped = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", cancelled);
        SvgShapingResult badTable = new SvgOpenTypeLayout().ShapeWithFontAndText(malformed, "A");

        Assert.Multiple(() =>
        {
            Assert.That(nullText.Error, Is.EqualTo(SvgShapingError.InvalidArgument));
            Assert.That(tooManyGlyphs.Error, Is.EqualTo(SvgShapingError.LimitExceeded));
            Assert.That(tooManyOperations.Error, Is.EqualTo(SvgShapingError.LimitExceeded));
            Assert.That(absentAxis.Error, Is.EqualTo(SvgShapingError.InvalidArgument));
            Assert.That(stopped.Error, Is.EqualTo(SvgShapingError.Cancelled));
            Assert.That(badTable.Error, Is.EqualTo(SvgShapingError.InvalidTable));
        });
    }

    [Test]
    public void UsesValidatedCallbackGlyphsForAnUnsupportedScript()
    {
        SvgFont font = CreateFont(new Dictionary<int, int> { [0x66] = 3, [0x69] = 4 }, new Dictionary<string, byte[]>());
        SvgShapingCallbackRequest received = null;
        SvgShapingCallbackResponse returned = null;
        SvgShapingOptions options = new SvgShapingOptions();
        options.ScriptTag = "arab";
        options.FontIdentifier = 41;
        options.ShapingCallback = request =>
        {
            received = request;
            request.BaselineGlyphs[0].GlyphIndex = 31;
            SvgShapedGlyph glyph = new SvgShapedGlyph();
            glyph.FontIdentifier = request.FontIdentifier;
            glyph.GlyphIndex = 6;
            glyph.ScalarStart = 0;
            glyph.ScalarCount = 2;
            glyph.Utf16Start = 0;
            glyph.Utf16Length = 2;
            glyph.Missing = false;
            glyph.GlyphClass = SvgOpenTypeGlyphClass.Ligature;
            glyph.XAdvance = 12.25;
            glyph.YAdvance = -0.5;
            glyph.XOffset = 1.75;
            glyph.YOffset = -2.25;
            glyph.AttachmentIndex = -1;
            glyph.LigatureComponent = 2;
            SvgShapingCallbackResponse response = new SvgShapingCallbackResponse();
            response.Success = true;
            response.FontIdentifier = request.FontIdentifier;
            response.Glyphs.Add(glyph);
            response.Message = "host shaped";
            returned = response;
            return response;
        };

        SvgShapingResult result = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "fi", options);
        returned.Glyphs[0].GlyphIndex = 30;
        returned.Glyphs[0].XAdvance = 99;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RequiresCallback, Is.True);
            Assert.That(result.CallbackReason, Is.EqualTo(SvgShapingCallbackReason.UnsupportedScript));
            Assert.That(result.CallbackInvoked, Is.True);
            Assert.That(result.UsedCallback, Is.True);
            Assert.That(result.UsedDeterministicFallback, Is.False);
            Assert.That(result.CallbackMessage, Is.EqualTo("host shaped"));
            Assert.That(result.FontIdentifier, Is.EqualTo(41));
            Assert.That(result.Glyphs, Has.Count.EqualTo(1));
            Assert.That(result.Glyphs[0].FontIdentifier, Is.EqualTo(41));
            Assert.That(result.Glyphs[0].GlyphIndex, Is.EqualTo(6));
            Assert.That(result.Glyphs[0].ScalarStart, Is.Zero);
            Assert.That(result.Glyphs[0].ScalarCount, Is.EqualTo(2));
            Assert.That(result.Glyphs[0].Utf16Length, Is.EqualTo(2));
            Assert.That(result.Glyphs[0].XAdvance, Is.EqualTo(12.25));
            Assert.That(result.Glyphs[0].YAdvance, Is.EqualTo(-0.5));
            Assert.That(result.Glyphs[0].XOffset, Is.EqualTo(1.75));
            Assert.That(result.Glyphs[0].YOffset, Is.EqualTo(-2.25));
            Assert.That(received, Is.Not.Null);
            Assert.That(received.Font, Is.SameAs(font));
            Assert.That(received.BaselineGlyphs.Select(glyph => glyph.FontIdentifier), Is.EqualTo(new[] { 41, 41 }));
        });
    }

    [Test]
    public void RetainsIndependentBaselineGlyphsWhenCallbackOutputIsUnavailableOrInvalid()
    {
        SvgFont font = CreateFont(
            new Dictionary<int, int> { [0x41] = 1 },
            new Dictionary<string, byte[]> { ["GSUB"] = Layout("ccmp", 5, new byte[] { 0, 1 }) });
        SvgShapingOptions absentOptions = new SvgShapingOptions();
        absentOptions.FontIdentifier = 7;
        SvgShapingOptions declinedOptions = new SvgShapingOptions();
        declinedOptions.FontIdentifier = 7;
        declinedOptions.ShapingCallback = request =>
        {
            request.BaselineGlyphs[0].GlyphIndex = 31;
            SvgShapingCallbackResponse response = new SvgShapingCallbackResponse();
            response.Message = "host declined";
            return response;
        };
        SvgShapingOptions invalidOptions = new SvgShapingOptions();
        invalidOptions.FontIdentifier = 7;
        invalidOptions.ShapingCallback = request =>
        {
            SvgShapingCallbackResponse response = new SvgShapingCallbackResponse();
            response.Success = true;
            response.FontIdentifier = 8;
            response.Glyphs.Add(request.BaselineGlyphs[0]);
            return response;
        };

        SvgShapingResult absent = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", absentOptions);
        SvgShapingResult declined = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", declinedOptions);
        SvgShapingResult invalid = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Success, Is.True);
            Assert.That(absent.CallbackReason, Is.EqualTo(SvgShapingCallbackReason.UnsupportedLayout));
            Assert.That(absent.CallbackInvoked, Is.False);
            Assert.That(absent.UsedDeterministicFallback, Is.True);
            Assert.That(absent.UnsupportedLookupTypes, Is.EqualTo(new[] { 5 }));
            Assert.That(absent.Glyphs[0].GlyphIndex, Is.EqualTo(1));
            Assert.That(absent.Glyphs[0].FontIdentifier, Is.EqualTo(7));
            Assert.That(declined.CallbackInvoked, Is.True);
            Assert.That(declined.UsedCallback, Is.False);
            Assert.That(declined.UsedDeterministicFallback, Is.True);
            Assert.That(declined.CallbackMessage, Is.EqualTo("host declined"));
            Assert.That(declined.Glyphs[0].GlyphIndex, Is.EqualTo(1));
            Assert.That(invalid.CallbackInvoked, Is.True);
            Assert.That(invalid.UsedCallback, Is.False);
            Assert.That(invalid.UsedDeterministicFallback, Is.True);
            Assert.That(invalid.Glyphs[0].FontIdentifier, Is.EqualTo(7));
        });
    }

    [Test]
    public void CallbackCannotRelaxOriginalIdentityGlyphLimitOrCancellation()
    {
        SvgFont font = CreateFont(new Dictionary<int, int> { [0x41] = 1 }, new Dictionary<string, byte[]>());
        SvgShapingOptions identityOptions = new SvgShapingOptions();
        identityOptions.ScriptTag = "arab";
        identityOptions.FontIdentifier = 7;
        identityOptions.ShapingCallback = request =>
        {
            identityOptions.FontIdentifier = 8;
            request.FontIdentifier = 8;
            SvgShapedGlyph glyph = request.BaselineGlyphs[0];
            glyph.FontIdentifier = 8;
            SvgShapingCallbackResponse response = new SvgShapingCallbackResponse();
            response.Success = true;
            response.FontIdentifier = 8;
            response.Glyphs.Add(glyph);
            return response;
        };
        SvgShapingOptions limitOptions = new SvgShapingOptions();
        limitOptions.ScriptTag = "arab";
        limitOptions.MaxGlyphs = 1;
        limitOptions.ShapingCallback = request =>
        {
            limitOptions.MaxGlyphs = 2;
            request.MaxGlyphs = 2;
            SvgShapingCallbackResponse response = new SvgShapingCallbackResponse();
            response.Success = true;
            response.Glyphs.Add(request.BaselineGlyphs[0]);
            response.Glyphs.Add(request.BaselineGlyphs[0]);
            return response;
        };
        CancellationTokenSource source = new CancellationTokenSource();
        SvgShapingOptions cancellationOptions = new SvgShapingOptions();
        cancellationOptions.ScriptTag = "arab";
        cancellationOptions.CancellationToken = source.Token;
        cancellationOptions.ShapingCallback = request =>
        {
            source.Cancel();
            cancellationOptions.CancellationToken = CancellationToken.None;
            request.CancellationToken = CancellationToken.None;
            SvgShapingCallbackResponse response = new SvgShapingCallbackResponse();
            response.Success = true;
            response.Glyphs.Add(request.BaselineGlyphs[0]);
            return response;
        };

        SvgShapingResult identity = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", identityOptions);
        SvgShapingResult limit = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", limitOptions);
        SvgShapingResult cancelled = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "A", cancellationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(identity.Success, Is.True);
            Assert.That(identity.UsedCallback, Is.False);
            Assert.That(identity.UsedDeterministicFallback, Is.True);
            Assert.That(identity.FontIdentifier, Is.EqualTo(7));
            Assert.That(identity.Glyphs, Has.Count.EqualTo(1));
            Assert.That(identity.Glyphs[0].FontIdentifier, Is.EqualTo(7));
            Assert.That(limit.Success, Is.True);
            Assert.That(limit.UsedCallback, Is.False);
            Assert.That(limit.UsedDeterministicFallback, Is.True);
            Assert.That(limit.Glyphs, Has.Count.EqualTo(1));
            Assert.That(cancelled.Success, Is.False);
            Assert.That(cancelled.Error, Is.EqualTo(SvgShapingError.Cancelled));
            Assert.That(cancelled.UsedCallback, Is.False);
        });
    }

    [Test]
    public void SkipsCallbacksForBaselineScriptsAndDetectsRetainedShapingTables()
    {
        int calls = 0;
        SvgFont baselineFont = CreateFont(new Dictionary<int, int> { [0x41] = 1 }, new Dictionary<string, byte[]>());
        SvgShapingOptions baselineOptions = new SvgShapingOptions();
        baselineOptions.ScriptTag = "latn";
        baselineOptions.ShapingCallback = request =>
        {
            calls++;
            return new SvgShapingCallbackResponse();
        };
        SvgFont tableFont = CreateFont(
            new Dictionary<int, int> { [0x41] = 1 },
            new Dictionary<string, byte[]> { ["morx"] = new byte[] { 0, 2, 0, 0 } });

        SvgShapingResult baseline = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(baselineFont, "A", baselineOptions);
        SvgShapingResult retainedTable = new SvgOpenTypeLayout().ShapeWithFontAndText(tableFont, "A");

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Success, Is.True);
            Assert.That(baseline.RequiresCallback, Is.False);
            Assert.That(baseline.CallbackInvoked, Is.False);
            Assert.That(calls, Is.Zero);
            Assert.That(retainedTable.Success, Is.True);
            Assert.That(retainedTable.RequiresCallback, Is.True);
            Assert.That(retainedTable.CallbackReason, Is.EqualTo(SvgShapingCallbackReason.UnsupportedLayout));
            Assert.That(retainedTable.UnsupportedTableTags, Is.EqualTo(new[] { "morx" }));
            Assert.That(retainedTable.UsedDeterministicFallback, Is.True);
        });
    }

    [Test]
    public void DerivesGlyphFontIdentityFromARegisteredFace()
    {
        SvgFontDatabase database = new SvgFontDatabase();
        SvgFontFaceResult registered = database.AddFont(SvgOpenTypeReaderTests.BuildFont(false, true));

        SvgShapingResult result = new SvgOpenTypeLayout().ShapeFontFaceWithFaceAndText(registered.Face, "A");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.FontIdentifier, Is.EqualTo(registered.Face.Identifier));
            Assert.That(result.Glyphs, Is.Not.Empty);
            Assert.That(result.Glyphs.All(glyph => glyph.FontIdentifier == registered.Face.Identifier), Is.True);
        });
    }

    internal static SvgFont CreateFont(Dictionary<int, int> mappings, Dictionary<string, byte[]> extraTables)
    {
        byte[] cmap = Cmap(mappings);
        int total = cmap.Length + extraTables.Values.Sum(value => value.Length);
        byte[] bytes = new byte[total];
        Array.Copy(cmap, bytes, cmap.Length);
        SvgFont font = new SvgFont(bytes, false);
        font.Tables.Add(new SvgOpenTypeTable("cmap", 0, 0, cmap.Length));
        int offset = cmap.Length;
        foreach (KeyValuePair<string, byte[]> entry in extraTables.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            Array.Copy(entry.Value, 0, bytes, offset, entry.Value.Length);
            font.Tables.Add(new SvgOpenTypeTable(entry.Key, 0, offset, entry.Value.Length));
            offset += entry.Value.Length;
        }
        font.Head = new SvgOpenTypeHeadTable();
        font.Head.UnitsPerEm = 1000;
        font.MaximumProfile = new SvgOpenTypeMaximumProfileTable();
        font.MaximumProfile.GlyphCount = 32;
        for (int glyph = 0; glyph < 32; glyph++) font.HorizontalMetrics.Add(new SvgOpenTypeHorizontalMetric(glyph, 600, 0));
        SvgOpenTypeCmapRecord record = new SvgOpenTypeCmapRecord();
        record.PlatformIdentifier = 0;
        record.EncodingIdentifier = 4;
        record.Offset = 12;
        record.Format = 12;
        record.Length = cmap.Length - 12;
        font.CharacterMaps.Add(record);
        font.Success = true;
        return font;
    }

    static byte[] Cmap(Dictionary<int, int> mappings)
    {
        KeyValuePair<int, int>[] values = mappings.OrderBy(entry => entry.Key).ToArray();
        byte[] bytes = new byte[28 + values.Length * 12];
        U16(bytes, 0, 0);
        U16(bytes, 2, 1);
        U16(bytes, 4, 0);
        U16(bytes, 6, 4);
        U32(bytes, 8, 12);
        U16(bytes, 12, 12);
        U16(bytes, 14, 0);
        U32(bytes, 16, (uint)(16 + values.Length * 12));
        U32(bytes, 20, 0);
        U32(bytes, 24, (uint)values.Length);
        for (int index = 0; index < values.Length; index++)
        {
            int offset = 28 + index * 12;
            U32(bytes, offset, (uint)values[index].Key);
            U32(bytes, offset + 4, (uint)values[index].Key);
            U32(bytes, offset + 8, (uint)values[index].Value);
        }
        return bytes;
    }

    internal static byte[] Layout(string feature, int lookupType, byte[] subtable)
    {
        byte[] bytes = new byte[56 + subtable.Length];
        U16(bytes, 0, 1);
        U16(bytes, 2, 0);
        U16(bytes, 4, 10);
        U16(bytes, 6, 30);
        U16(bytes, 8, 44);
        U16(bytes, 10, 1);
        Tag(bytes, 12, "DFLT");
        U16(bytes, 16, 8);
        U16(bytes, 18, 4);
        U16(bytes, 20, 0);
        U16(bytes, 22, 0);
        U16(bytes, 24, 0xFFFF);
        U16(bytes, 26, 1);
        U16(bytes, 28, 0);
        U16(bytes, 30, 1);
        Tag(bytes, 32, feature);
        U16(bytes, 36, 8);
        U16(bytes, 38, 0);
        U16(bytes, 40, 1);
        U16(bytes, 42, 0);
        U16(bytes, 44, 1);
        U16(bytes, 46, 4);
        U16(bytes, 48, lookupType);
        U16(bytes, 50, 0);
        U16(bytes, 52, 1);
        U16(bytes, 54, 8);
        Array.Copy(subtable, 0, bytes, 56, subtable.Length);
        return bytes;
    }

    static byte[] LanguageLayout(string script, string language, string feature, int lookupType, byte[] subtable)
    {
        byte[] bytes = new byte[62 + subtable.Length];
        U16(bytes, 0, 1);
        U16(bytes, 2, 0);
        U16(bytes, 4, 10);
        U16(bytes, 6, 36);
        U16(bytes, 8, 50);
        U16(bytes, 10, 1);
        Tag(bytes, 12, script);
        U16(bytes, 16, 8);
        U16(bytes, 18, 0);
        U16(bytes, 20, 1);
        Tag(bytes, 22, language);
        U16(bytes, 26, 10);
        U16(bytes, 28, 0);
        U16(bytes, 30, 0xFFFF);
        U16(bytes, 32, 1);
        U16(bytes, 34, 0);
        U16(bytes, 36, 1);
        Tag(bytes, 38, feature);
        U16(bytes, 42, 8);
        U16(bytes, 44, 0);
        U16(bytes, 46, 1);
        U16(bytes, 48, 0);
        U16(bytes, 50, 1);
        U16(bytes, 52, 4);
        U16(bytes, 54, lookupType);
        U16(bytes, 56, 0);
        U16(bytes, 58, 1);
        U16(bytes, 60, 8);
        Array.Copy(subtable, 0, bytes, 62, subtable.Length);
        return bytes;
    }

    internal static byte[] LigatureSubtable()
    {
        byte[] bytes = new byte[24];
        U16(bytes, 0, 1);
        U16(bytes, 2, 8);
        U16(bytes, 4, 1);
        U16(bytes, 6, 14);
        Coverage(bytes, 8, 3);
        U16(bytes, 14, 1);
        U16(bytes, 16, 4);
        U16(bytes, 18, 6);
        U16(bytes, 20, 2);
        U16(bytes, 22, 4);
        return bytes;
    }

    static byte[] PairSubtable(int adjustment)
    {
        byte[] bytes = new byte[24];
        U16(bytes, 0, 1);
        U16(bytes, 2, 12);
        U16(bytes, 4, 4);
        U16(bytes, 6, 0);
        U16(bytes, 8, 1);
        U16(bytes, 10, 18);
        Coverage(bytes, 12, 1);
        U16(bytes, 18, 1);
        U16(bytes, 20, 2);
        I16(bytes, 22, adjustment);
        return bytes;
    }

    static byte[] MarkBaseSubtable()
    {
        byte[] bytes = new byte[46];
        U16(bytes, 0, 1);
        U16(bytes, 2, 12);
        U16(bytes, 4, 18);
        U16(bytes, 6, 1);
        U16(bytes, 8, 24);
        U16(bytes, 10, 36);
        Coverage(bytes, 12, 5);
        Coverage(bytes, 18, 1);
        U16(bytes, 24, 1);
        U16(bytes, 26, 0);
        U16(bytes, 28, 6);
        Anchor(bytes, 30, 100, 200);
        U16(bytes, 36, 1);
        U16(bytes, 38, 4);
        Anchor(bytes, 40, 300, 700);
        return bytes;
    }

    static byte[] CursiveSubtable()
    {
        byte[] bytes = new byte[34];
        U16(bytes, 0, 1);
        U16(bytes, 2, 14);
        U16(bytes, 4, 2);
        U16(bytes, 6, 0);
        U16(bytes, 8, 22);
        U16(bytes, 10, 28);
        U16(bytes, 12, 0);
        U16(bytes, 14, 1);
        U16(bytes, 16, 2);
        U16(bytes, 18, 7);
        U16(bytes, 20, 8);
        Anchor(bytes, 22, 500, 0);
        Anchor(bytes, 28, 100, 0);
        return bytes;
    }

    static byte[] GdefTable()
    {
        byte[] bytes = new byte[28];
        U16(bytes, 0, 1);
        U16(bytes, 2, 0);
        U16(bytes, 4, 12);
        U16(bytes, 12, 2);
        U16(bytes, 14, 2);
        U16(bytes, 16, 1);
        U16(bytes, 18, 1);
        U16(bytes, 20, 1);
        U16(bytes, 22, 5);
        U16(bytes, 24, 5);
        U16(bytes, 26, 3);
        return bytes;
    }

    static byte[] KernTable(int adjustment)
    {
        byte[] bytes = new byte[24];
        U16(bytes, 0, 0);
        U16(bytes, 2, 1);
        U16(bytes, 4, 0);
        U16(bytes, 6, 20);
        U16(bytes, 8, 1);
        U16(bytes, 10, 1);
        U16(bytes, 12, 6);
        U16(bytes, 14, 0);
        U16(bytes, 16, 0);
        U16(bytes, 18, 1);
        U16(bytes, 20, 2);
        I16(bytes, 22, adjustment);
        return bytes;
    }

    static byte[] FvarTable()
    {
        byte[] bytes = new byte[36];
        U32(bytes, 0, 0x00010000);
        U16(bytes, 4, 16);
        U16(bytes, 6, 2);
        U16(bytes, 8, 1);
        U16(bytes, 10, 20);
        U16(bytes, 12, 0);
        U16(bytes, 14, 8);
        Tag(bytes, 16, "wght");
        Fixed(bytes, 20, 100);
        Fixed(bytes, 24, 400);
        Fixed(bytes, 28, 900);
        U16(bytes, 32, 0);
        U16(bytes, 34, 256);
        return bytes;
    }

    static byte[] AvarTable()
    {
        byte[] bytes = new byte[22];
        U16(bytes, 0, 1);
        U16(bytes, 2, 0);
        U16(bytes, 4, 0);
        U16(bytes, 6, 1);
        U16(bytes, 8, 3);
        I16(bytes, 10, -16384);
        I16(bytes, 12, -16384);
        I16(bytes, 14, 0);
        I16(bytes, 16, 0);
        I16(bytes, 18, 16384);
        I16(bytes, 20, 8192);
        return bytes;
    }

    static void Coverage(byte[] bytes, int offset, int glyph)
    {
        U16(bytes, offset, 1);
        U16(bytes, offset + 2, 1);
        U16(bytes, offset + 4, glyph);
    }

    static void Anchor(byte[] bytes, int offset, int x, int y)
    {
        U16(bytes, offset, 1);
        I16(bytes, offset + 2, x);
        I16(bytes, offset + 4, y);
    }

    static void Tag(byte[] bytes, int offset, string value)
    {
        for (int index = 0; index < 4; index++) bytes[offset + index] = (byte)value[index];
    }

    static void Fixed(byte[] bytes, int offset, double value)
    {
        I32(bytes, offset, (int)(value * 65536));
    }

    static void U16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void I16(byte[] bytes, int offset, int value)
    {
        U16(bytes, offset, value & 0xFFFF);
    }

    static void U32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    static void I32(byte[] bytes, int offset, int value)
    {
        uint encoded = value >= 0 ? (uint)value : 2147483648u + (uint)(value - int.MinValue);
        U32(bytes, offset, encoded);
    }
}
