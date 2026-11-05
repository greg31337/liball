namespace svg.Tests;

public class SvgFontDatabaseTests
{
    [Test]
    public void RegistersOwnedCallerAndResolvedFontFacesWithStableSnapshots()
    {
        byte[] callerBytes = SvgOpenTypeReaderTests.BuildFont(false, true);
        SvgFontDatabase database = new SvgFontDatabase();

        SvgFontFaceResult caller = database.AddFont(callerBytes);
        callerBytes[1] = 0;

        byte[] resolvedBytes = SvgOpenTypeReaderTests.BuildFont(false, true);
        SvgResourceRequest resourceRequest = new SvgResourceRequest("https://example.test/font.ttf", "font/ttf", System.Threading.CancellationToken.None);
        resourceRequest.ResourceKind = SvgResourceKind.Font;
        SvgResourceAcquisitionOptions acquisitionOptions = new SvgResourceAcquisitionOptions();
        acquisitionOptions.ResourceProvider = new SvgResourceProvider(value =>
        {
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = resolvedBytes;
            response.ActualMediaType = "font/ttf";
            response.FinalUri = value.NormalizedUri;
            return response;
        });
        SvgResourceAcquisitionResult acquisition = new SvgResourceResolver().Resolve(resourceRequest, acquisitionOptions);
        SvgFontFaceDescriptor resolvedDescriptor = Descriptor("Web Fixture", SvgFontStyle.Normal, 400, 100);
        SvgFontFaceResult resolved = database.AddResolvedFont(acquisition, resolvedDescriptor);

        database.SetGenericFamily(SvgGenericFontFamily.SansSerif, new List<string> { "Web Fixture" });
        SvgFontDatabaseSnapshot snapshot = database.Snapshot();
        snapshot.Faces.Clear();
        snapshot.GenericFamilies[SvgGenericFontFamily.SansSerif].Add("Mutation");
        SvgFontDatabaseSnapshot afterMutation = database.Snapshot();

        Assert.Multiple(() =>
        {
            Assert.That(caller.Success, Is.True);
            Assert.That(caller.Face.Identifier, Is.EqualTo(1));
            Assert.That(caller.Face.Family, Is.EqualTo("Fixture"));
            Assert.That(caller.Face.Style, Is.EqualTo(SvgFontStyle.Italic));
            Assert.That(caller.Face.MinimumWeight, Is.EqualTo(700));
            Assert.That(caller.Face.MinimumStretch, Is.EqualTo(100));
            Assert.That(caller.Face.Font.OwnsBytes, Is.True);
            Assert.That(caller.Face.Font.Bytes[1], Is.EqualTo(1));
            Assert.That(caller.Face.Sources.Single().Kind, Is.EqualTo(SvgFontSourceKind.Bytes));
            Assert.That(resolved.Success, Is.True);
            Assert.That(resolved.Face.Identifier, Is.EqualTo(2));
            Assert.That(resolved.Face.Origin, Is.EqualTo(SvgFontFaceOrigin.ResolvedFontFace));
            Assert.That(resolved.Face.Sources.Single().Value, Is.EqualTo("https://example.test/font.ttf"));
            Assert.That(resolved.Face.Sources.Single().Format, Is.EqualTo("font/ttf"));
            Assert.That(afterMutation.Faces, Has.Count.EqualTo(2));
            Assert.That(afterMutation.GenericFamilies[SvgGenericFontFamily.SansSerif], Is.EqualTo(new[] { "Web Fixture" }));
            Assert.That(afterMutation.TotalBytes, Is.EqualTo(caller.Face.Font.Bytes.Length + resolved.Face.Font.Bytes.Length));
        });
    }

    [Test]
    public void MatchesWidthThenStyleThenCssWeightSearchOrder()
    {
        SvgFontDatabase database = new SvgFontDatabase();
        SvgFontFaceResult narrow = Add(database, "Match", SvgFontStyle.Normal, 900, 75);
        SvgFontFaceResult normal400 = Add(database, "Match", SvgFontStyle.Normal, 400, 100);
        SvgFontFaceResult normal500 = Add(database, "Match", SvgFontStyle.Normal, 500, 100);
        SvgFontFaceResult italic700 = Add(database, "Match", SvgFontStyle.Italic, 700, 100);
        Add(database, "Match", SvgFontStyle.Normal, 800, 125);

        SvgFontMatchRequest narrowRequest = Request("match", SvgFontStyle.Normal, 400, 80, false, 0);
        SvgFontMatchRequest italicRequest = Request("Match", SvgFontStyle.Italic, 400, 100, false, 0);
        SvgFontMatchRequest weightRequest = Request("Match", SvgFontStyle.Normal, 450, 100, false, 0);

        SvgFontMatchResult narrowMatch = database.Match(narrowRequest);
        SvgFontMatchResult italicMatch = database.Match(italicRequest);
        SvgFontMatchResult weightMatch = database.Match(weightRequest);

        Assert.Multiple(() =>
        {
            Assert.That(narrowMatch.Face, Is.SameAs(narrow.Face), "width matching must precede style and weight");
            Assert.That(italicMatch.Face, Is.SameAs(italic700.Face), "style matching must precede weight");
            Assert.That(weightMatch.Face, Is.SameAs(normal500.Face), "450 searches upward through 500 before lower weights");
            Assert.That(weightMatch.Face, Is.Not.SameAs(normal400.Face));
        });
    }

    [Test]
    public void AppliesUnicodeRangesAfterStyleSelectionAndExpandsGenericFamilies()
    {
        SvgFontDatabase database = new SvgFontDatabase();
        SvgFontFaceDescriptor uprightDescriptor = Descriptor("Primary", SvgFontStyle.Normal, 400, 100);
        uprightDescriptor.UnicodeRanges.Add(new SvgUnicodeRange(0x42, 0x42));
        SvgFontFaceResult upright = database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), uprightDescriptor);
        SvgFontFaceDescriptor italicDescriptor = Descriptor("Primary", SvgFontStyle.Italic, 400, 100);
        italicDescriptor.UnicodeRanges.Add(new SvgUnicodeRange(0x41, 0x41));
        SvgFontFaceResult italic = database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), italicDescriptor);
        SvgFontFaceDescriptor fallbackDescriptor = Descriptor("Fallback", SvgFontStyle.Normal, 400, 100);
        fallbackDescriptor.UnicodeRanges.Add(new SvgUnicodeRange(0x41, 0x41));
        SvgFontFaceResult fallback = database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), fallbackDescriptor);
        database.SetGenericFamily(SvgGenericFontFamily.SansSerif, new List<string> { "Unavailable", "Fallback" });

        SvgFontMatchRequest familyRequest = Request("Primary", SvgFontStyle.Normal, 400, 100, true, 0x41);
        familyRequest.Families.Add("Fallback");
        familyRequest.AllowAnyFamilyFallback = false;
        SvgFontMatchRequest genericRequest = Request("sans-serif", SvgFontStyle.Normal, 400, 100, true, 0x41);
        genericRequest.AllowAnyFamilyFallback = false;

        SvgFontMatchResult familyMatch = database.Match(familyRequest);
        SvgFontMatchResult genericMatch = database.Match(genericRequest);

        Assert.Multiple(() =>
        {
            Assert.That(upright.Success && italic.Success && fallback.Success, Is.True);
            Assert.That(upright.Face.Includes(0x41), Is.False);
            Assert.That(italic.Face.Covers(0x41), Is.True);
            Assert.That(familyMatch.Face, Is.SameAs(fallback.Face), "a worse-style face in the same family must not replace the selected face");
            Assert.That(familyMatch.FamilyIndex, Is.EqualTo(1));
            Assert.That(genericMatch.Face, Is.SameAs(fallback.Face));
            Assert.That(genericMatch.UsedGenericFamily, Is.True);
            Assert.That(genericMatch.ResolvedFamily, Is.EqualTo("Fallback"));
            Assert.That(genericMatch.GlyphMapping.Found, Is.True);
        });
    }

    [Test]
    public void SelectsCompositeUnicodeFaceInReverseRegistrationOrder()
    {
        SvgFontDatabase database = new SvgFontDatabase();
        SvgFontFaceDescriptor firstDescriptor = Descriptor("Composite", SvgFontStyle.Normal, 400, 100);
        firstDescriptor.UnicodeRanges.Add(new SvgUnicodeRange(0x41, 0x41));
        SvgFontFaceResult first = database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), firstDescriptor);
        SvgFontFaceDescriptor secondDescriptor = Descriptor("Composite", SvgFontStyle.Normal, 400, 100);
        secondDescriptor.UnicodeRanges.Add(new SvgUnicodeRange(0x41, 0x41));
        SvgFontFaceResult second = database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), secondDescriptor);

        SvgFontMatchResult match = database.Match(Request("Composite", SvgFontStyle.Normal, 400, 100, true, 0x41));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success && second.Success, Is.True);
            Assert.That(match.Success, Is.True);
            Assert.That(match.Face, Is.SameAs(second.Face));
        });
    }

    [Test]
    public void CreatesDeterministicUtf16FallbackRunsAndCoalescesAdjacentFaces()
    {
        SvgFontDatabase database = new SvgFontDatabase();
        SvgFontFaceDescriptor excludedDescriptor = Descriptor("Excluded", SvgFontStyle.Normal, 400, 100);
        excludedDescriptor.UnicodeRanges.Add(new SvgUnicodeRange(0x42, 0x42));
        database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), excludedDescriptor);
        SvgFontFaceDescriptor includedDescriptor = Descriptor("Included", SvgFontStyle.Normal, 400, 100);
        includedDescriptor.UnicodeRanges.Add(new SvgUnicodeRange(0x41, 0x41));
        SvgFontFaceResult included = database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), includedDescriptor);
        SvgFontMatchRequest request = Request("Unknown", SvgFontStyle.Normal, 400, 100, false, 0);

        SvgFontFallbackResult result = database.CreateFallbackRuns("AA\U0001F600A", request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MissingCodePoints, Is.EqualTo(1));
            Assert.That(result.Runs, Has.Count.EqualTo(3));
            Assert.That(result.Runs[0].Start, Is.Zero);
            Assert.That(result.Runs[0].Length, Is.EqualTo(2));
            Assert.That(result.Runs[0].Face, Is.SameAs(included.Face));
            Assert.That(result.Runs[1].Start, Is.EqualTo(2));
            Assert.That(result.Runs[1].Length, Is.EqualTo(2));
            Assert.That(result.Runs[1].Missing, Is.True);
            Assert.That(result.Runs[2].Start, Is.EqualTo(4));
            Assert.That(result.Runs[2].Length, Is.EqualTo(1));
            Assert.That(result.Runs[2].Face, Is.SameAs(included.Face));
        });
    }

    [Test]
    public void EnforcesDatabaseLimitsAndSupportsRemovalWithoutReusingIdentifiers()
    {
        SvgFontLimits limits = new SvgFontLimits();
        limits.MaxFonts = 1;
        SvgFontDatabase database = SvgFontDatabase.Create(limits);

        SvgFontFaceResult first = Add(database, "One", SvgFontStyle.Normal, 400, 100);
        SvgFontFaceResult limited = Add(database, "Two", SvgFontStyle.Normal, 400, 100);
        bool removed = database.RemoveFont(first.Face.Identifier);
        SvgFontFaceResult replacement = Add(database, "Two", SvgFontStyle.Normal, 400, 100);
        SvgFontFaceResult malformed = new SvgFontDatabase().AddFont(new byte[8]);
        SvgFontFaceDescriptor invalidDescriptor = Descriptor("Invalid", SvgFontStyle.Normal, 700, 100);
        invalidDescriptor.MaximumWeight = 600;
        SvgFontFaceResult invalid = new SvgFontDatabase().AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), invalidDescriptor);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(limited.Error, Is.EqualTo(SvgFontDatabaseError.LimitExceeded));
            Assert.That(removed, Is.True);
            Assert.That(replacement.Success, Is.True);
            Assert.That(replacement.Face.Identifier, Is.GreaterThan(first.Face.Identifier));
            Assert.That(malformed.Error, Is.EqualTo(SvgFontDatabaseError.FontReadFailed));
            Assert.That(malformed.ReadResult, Is.Not.Null);
            Assert.That(invalid.Error, Is.EqualTo(SvgFontDatabaseError.InvalidArgument));
        });
    }

    static SvgFontFaceResult Add(SvgFontDatabase database, string family, SvgFontStyle style, int weight, double stretch)
    {
        return database.AddFontWithDescriptor(SvgOpenTypeReaderTests.BuildFont(false, true), Descriptor(family, style, weight, stretch));
    }

    static SvgFontFaceDescriptor Descriptor(string family, SvgFontStyle style, int weight, double stretch)
    {
        SvgFontFaceDescriptor descriptor = new SvgFontFaceDescriptor();
        descriptor.Family = family;
        descriptor.Style = style;
        descriptor.MinimumWeight = weight;
        descriptor.MaximumWeight = weight;
        descriptor.MinimumStretch = stretch;
        descriptor.MaximumStretch = stretch;
        return descriptor;
    }

    static SvgFontMatchRequest Request(string family, SvgFontStyle style, int weight, double stretch, bool hasCodePoint, int codePoint)
    {
        SvgFontMatchRequest request = new SvgFontMatchRequest();
        request.Families.Add(family);
        request.Style = style;
        request.Weight = weight;
        request.Stretch = stretch;
        request.HasCodePoint = hasCodePoint;
        request.CodePoint = codePoint;
        return request;
    }
}
