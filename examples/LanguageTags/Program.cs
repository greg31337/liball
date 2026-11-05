using languagecodes;

var service = new LanguageCodeService();
Console.WriteLine("Language catalog lookup");
foreach (string code in new[] { "en", "de", "pl", "ja" })
{
    var entry = service.RequireValue(code, new LanguageLookupOptions());
    Console.WriteLine($"{code,-3} -> {entry.Set3Code,-4} {entry.ReferenceName}");
}

Console.WriteLine("\nBCP 47 language tags");

// A language tag can add script and region subtags to the base language code.
foreach (string text in new[] { "sr-Latn-RS", "zh-Hant-TW", "en-GB" })
{
    var tag = service.ParseTag(text);
    Console.WriteLine(
        $"{text,-12} language={tag.Language}, script={tag.Script ?? "(unspecified)"}, region={tag.Region}");
}

var matches = service.FilterTags("en", new[] { "en-US", "pl-PL", "en-GB", "ja-JP" });
Console.WriteLine($"\nEnglish options: {string.Join(", ", matches.Select(m => m.MatchedTag))}");
