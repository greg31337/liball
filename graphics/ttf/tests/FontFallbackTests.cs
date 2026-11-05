using ttf;

namespace ttf.Tests;

internal class FontFallbackTests
{
    [Test]
    public void ResolvesGenericFamiliesPerFontSetWithoutSharedDefaults()
    {
        FontFace alpha = Face("Alpha Sans", "Regular", 400, false);
        FontFace beta = Face("Beta Sans", "Regular", 400, false);
        FontSet first = new FontSet();
        FontSet second = new FontSet();
        FontSet unconfigured = new FontSet();
        first.Add(alpha);
        first.Add(beta);
        second.Add(alpha);
        second.Add(beta);
        unconfigured.Add(alpha);
        first.SetGenericFamily(FontGenericFamily.SansSerif, "Alpha Sans");
        second.SetGenericFamily(FontGenericFamily.SansSerif, "Beta Sans");
        FontRequest request = new FontApiFactory().CreateGenericRequest(
            FontGenericFamily.SansSerif);
        FontFallbackList firstFallbacks = first.GetFallbacks(request, "Latn");

        Assert.Multiple(() =>
        {
            Assert.That(request.HasGenericFamily(), Is.True);
            Assert.That(request.GetGenericFamily(), Is.EqualTo(FontGenericFamily.SansSerif));
            Assert.That(request.GetFamilyName(), Is.EqualTo("sans-serif"));
            Assert.That(first.GetGenericFamily(FontGenericFamily.SansSerif),
                Is.EqualTo("Alpha Sans"));
            Assert.That(second.GetGenericFamily(FontGenericFamily.SansSerif),
                Is.EqualTo("Beta Sans"));
            Assert.That(first.Match(request)!.GetFace(), Is.SameAs(alpha));
            Assert.That(second.Match(request)!.GetFace(), Is.SameAs(beta));
            Assert.That(unconfigured.Match(request), Is.Null);
            Assert.That(firstFallbacks.GetMatch(0).GetFallbackMatchKind(),
                Is.EqualTo(FontFallbackMatchKind.GenericFamily));
            Assert.That(firstFallbacks.GetMatch(0).GetConfiguredFamilyName(),
                Is.EqualTo("Alpha Sans"));
        });

        first.ClearGenericFamily(FontGenericFamily.SansSerif);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetGenericFamily(FontGenericFamily.SansSerif), Is.Null);
            Assert.That(first.Match(request), Is.Null);
            Assert.That(second.Match(request)!.GetFace(), Is.SameAs(beta));
        });
    }

    [Test]
    public void ResolvesPrimaryScriptAndSetWideFallbacksInOrder()
    {
        FontFace primary = Face("Primary", "Regular", 400, false);
        FontFace arabic = Face("Arabic Support", "Regular", 400, false);
        FontFace shared = Face("Shared Support", "Regular", 400, false);
        FontFace global = Face("Global Last", "Regular", 400, false);
        FontSet set = new FontSet();
        set.Add(primary);
        set.Add(arabic);
        set.Add(shared);
        set.Add(global);
        set.AddScriptFallbackFamily("arab", "Arabic Support");
        set.AddScriptFallbackFamily("Arab", "Shared Support");
        set.AddFallbackFamily("Shared Support");
        set.AddFallbackFamily("Missing Family");
        set.AddFallbackFamily("Global Last");

        FontFallbackList arabicFallbacks = set.GetFallbacks(
            new FontRequest("Primary"),
            "ARAB");
        FontFallbackList latinFallbacks = set.GetFallbacks(
            new FontRequest("Primary"),
            "Latn");

        Assert.Multiple(() =>
        {
            Assert.That(arabicFallbacks.GetCount(), Is.EqualTo(4));
            Assert.That(arabicFallbacks.GetMatch(0).GetFace(), Is.SameAs(primary));
            Assert.That(arabicFallbacks.GetMatch(1).GetFace(), Is.SameAs(arabic));
            Assert.That(arabicFallbacks.GetMatch(2).GetFace(), Is.SameAs(shared));
            Assert.That(arabicFallbacks.GetMatch(3).GetFace(), Is.SameAs(global));
            Assert.That(arabicFallbacks.GetMatch(0).GetFallbackMatchKind(),
                Is.EqualTo(FontFallbackMatchKind.RequestedFamily));
            Assert.That(arabicFallbacks.GetMatch(0).GetConfiguredFamilyName(),
                Is.EqualTo("Primary"));
            Assert.That(arabicFallbacks.GetMatch(1).GetFallbackMatchKind(),
                Is.EqualTo(FontFallbackMatchKind.ScriptFallback));
            Assert.That(arabicFallbacks.GetMatch(1).GetConfiguredFamilyName(),
                Is.EqualTo("Arabic Support"));
            Assert.That(arabicFallbacks.GetMatch(3).GetFallbackMatchKind(),
                Is.EqualTo(FontFallbackMatchKind.FallbackFamily));
            Assert.That(arabicFallbacks.GetMatch(3).GetConfiguredFamilyName(),
                Is.EqualTo("Global Last"));
            Assert.That(latinFallbacks.GetCount(), Is.EqualTo(3));
            Assert.That(latinFallbacks.GetMatch(0).GetFace(), Is.SameAs(primary));
            Assert.That(latinFallbacks.GetMatch(1).GetFace(), Is.SameAs(shared));
            Assert.That(latinFallbacks.GetMatch(2).GetFace(), Is.SameAs(global));
            Assert.Throws<ArgumentOutOfRangeException>(() => arabicFallbacks.GetMatch(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => arabicFallbacks.GetMatch(4));
        });
    }

    [Test]
    public void ContinuesWhenPrimaryOrConfiguredFamiliesAreUnavailable()
    {
        FontFace script = Face("Script Choice", "Regular", 400, false);
        FontFace global = Face("Global Choice", "Regular", 400, false);
        FontSet set = new FontSet();
        set.Add(script);
        set.Add(global);
        set.AddScriptFallbackFamily("Deva", "Script Choice");
        set.AddFallbackFamily("Global Choice");

        FontFallbackList concrete = set.GetFallbacks(
            new FontRequest("Unavailable Primary"),
            "Deva");
        FontFallbackList generic = set.GetFallbacks(
            new FontApiFactory().CreateGenericRequest(FontGenericFamily.Serif),
            "Deva");

        Assert.Multiple(() =>
        {
            Assert.That(concrete.GetCount(), Is.EqualTo(2));
            Assert.That(concrete.GetMatch(0).GetFace(), Is.SameAs(script));
            Assert.That(concrete.GetMatch(1).GetFace(), Is.SameAs(global));
            Assert.That(generic.GetCount(), Is.EqualTo(2));
            Assert.That(generic.GetMatch(0).GetFace(), Is.SameAs(script));
            Assert.That(generic.GetMatch(1).GetFace(), Is.SameAs(global));
        });
    }

    [Test]
    public void PreservesFacePreferencesForEveryFallbackFamily()
    {
        FontFace regular = Face("Fallback Styles", "Regular", 400, false);
        FontFace bold = Face("Fallback Styles", "Bold", 700, true);
        FontSet set = new FontSet();
        set.Add(regular);
        set.Add(bold);
        set.AddFallbackFamily("Fallback Styles");
        FontRequest request = new FontRequest("Unavailable");
        request.SetWeight(700);

        FontFallbackList fallbacks = set.GetFallbacks(request, "Latn");

        Assert.Multiple(() =>
        {
            Assert.That(fallbacks.GetCount(), Is.EqualTo(1));
            Assert.That(fallbacks.GetMatch(0).GetFace(), Is.SameAs(bold));
            Assert.That(fallbacks.GetMatch(0).GetInstance().GetFace(), Is.SameAs(bold));
        });
    }

    [Test]
    public void StoresDistinctConfigurationAndSupportsClearing()
    {
        FontSet set = new FontSet();

        Assert.That(set.AddFallbackFamily("Alpha"), Is.True);
        Assert.That(set.AddFallbackFamily(" alpha "), Is.False);
        Assert.That(set.AddFallbackFamily("Beta"), Is.True);
        Assert.That(set.AddScriptFallbackFamily("cyrl", "Gamma"), Is.True);
        Assert.That(set.AddScriptFallbackFamily("Cyrl", " gamma "), Is.False);
        Assert.That(set.AddScriptFallbackFamily("Cyrl", "Delta"), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(set.GetFallbackFamilyCount(), Is.EqualTo(2));
            Assert.That(set.GetFallbackFamily(0), Is.EqualTo("Alpha"));
            Assert.That(set.GetFallbackFamily(1), Is.EqualTo("Beta"));
            Assert.That(set.GetScriptFallbackFamilyCount("CYRL"), Is.EqualTo(2));
            Assert.That(set.GetScriptFallbackFamily("cyrl", 0), Is.EqualTo("Gamma"));
            Assert.That(set.GetScriptFallbackFamily("Cyrl", 1), Is.EqualTo("Delta"));
        });

        set.ClearFallbackFamilies();
        set.ClearScriptFallbackFamilies("cYrL");

        Assert.Multiple(() =>
        {
            Assert.That(set.GetFallbackFamilyCount(), Is.Zero);
            Assert.That(set.GetScriptFallbackFamilyCount("Cyrl"), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => set.GetFallbackFamily(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => set.GetScriptFallbackFamily("Cyrl", 0));
        });
    }

    [Test]
    public void ValidatesGenericFamilyFallbackAndScriptArguments()
    {
        FontSet set = new FontSet();
        FontRequest concrete = new FontRequest("Concrete");

        Assert.Multiple(() =>
        {
            Assert.That(concrete.HasGenericFamily(), Is.False);
            Assert.Throws<InvalidOperationException>(() => concrete.GetGenericFamily());
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FontApiFactory().CreateGenericRequest(
                    (FontGenericFamily)99));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => set.SetGenericFamily((FontGenericFamily)99, "Family"));
            Assert.Throws<ArgumentNullException>(
                () => set.SetGenericFamily(FontGenericFamily.Serif, null!));
            Assert.Throws<ArgumentException>(
                () => set.SetGenericFamily(FontGenericFamily.Serif, "  "));
            Assert.Throws<ArgumentNullException>(() => set.AddFallbackFamily(null!));
            Assert.Throws<ArgumentException>(() => set.AddFallbackFamily("\t"));
            Assert.Throws<ArgumentNullException>(
                () => set.AddScriptFallbackFamily(null!, "Family"));
            Assert.Throws<ArgumentNullException>(
                () => set.AddScriptFallbackFamily("Latn", null!));
            Assert.Throws<ArgumentException>(
                () => set.AddScriptFallbackFamily("Latin", "Family"));
            Assert.Throws<ArgumentException>(
                () => set.AddScriptFallbackFamily("La1n", "Family"));
            Assert.Throws<ArgumentNullException>(() => set.GetFallbacks(null!, "Latn"));
            Assert.Throws<ArgumentNullException>(() => set.GetFallbacks(concrete, null!));
            Assert.Throws<ArgumentException>(() => set.GetFallbacks(concrete, "latin"));
        });
    }

    static FontFace Face(string family, string style, int weight, bool bold)
    {
        return FontMatchingTests.CreateStaticFace(
            family,
            style,
            weight,
            5,
            false,
            bold,
            false);
    }
}
