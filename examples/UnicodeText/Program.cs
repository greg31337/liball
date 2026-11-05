using System.Text;
using unicode;

Console.OutputEncoding = Encoding.UTF8;
var normalization = new UnicodeNormalization();
var casing = new UnicodeCase();
var segments = new UnicodeSegments();

// The accent starts as a separate code point; NFC composes it with the letter.
const string decomposed = "Cafe\u0301";
string composed = normalization.ToNfc(decomposed);
Console.WriteLine("Unicode normalization and user-perceived characters");
Console.WriteLine($"NFC: {decomposed} -> {composed}");
Console.WriteLine($"UTF-16 lengths: {decomposed.Length} -> {composed.Length}");
Console.WriteLine($"Normalized match: {composed == "Café"}");
Console.WriteLine($"Case-folded match: Straße / STRASSE = {casing.EqualsFolded("Straße", "STRASSE")}");
Console.WriteLine($"Turkish uppercase of istanbul: {casing.ToUpperLocale("istanbul", "tr")}");

// Grapheme clusters keep joined emoji, flags, and combining marks together.
const string text = "A👩‍💻🇵🇱e\u0301";
Console.WriteLine($"\nText: {text}");
Console.WriteLine(
    $"UTF-16 code units: {text.Length}; grapheme clusters: {segments.GetGraphemeClusterCount(text)}");
for (int i = 0; i < segments.GetGraphemeClusterCount(text); i++)
{
    Console.WriteLine($"  Character {i + 1}: {segments.SliceGraphemeClusters(text, i, 1)}");
}

Console.WriteLine(
    $"First three characters, without splitting an emoji: {segments.TruncateGraphemeClusters(text, 3)}");
