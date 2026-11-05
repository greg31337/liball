using System;
using System.Collections.Generic;
using System.Text;

namespace fake;
/// <summary>
/// Provides generated text operations and Unicode-safe text mechanics.
/// </summary>
public class TextProvider : ITextProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal TextProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("text");
    }

    /// <summary>
    /// Counts extended grapheme clusters using the platform Unicode text-element implementation.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to inspect.</param>
    /// <returns>The number of user-perceived text elements.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    public int GraphemeCount(string value)
    {
        return UnicodeText.GraphemeCount(value);
    }

    /// <summary>
    /// Returns at most the requested number of extended grapheme clusters from the start of a string.
    /// </summary>
    /// <remarks>The result never splits a surrogate pair, combining sequence, emoji sequence, or other platform-recognized text element.</remarks>
    /// <param name="value">The well-formed UTF-16 string to truncate.</param>
    /// <param name="maximumGraphemes">The maximum number of grapheme clusters to retain.</param>
    /// <returns>The original string when within the limit; otherwise a prefix ending at a grapheme boundary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is negative or the result exceeds the configured output limit.</exception>
    public string Truncate(string value, int maximumGraphemes)
    {
        return UnicodeText.Truncate(value, maximumGraphemes, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Clamps a string to an inclusive range of extended grapheme-cluster counts.
    /// </summary>
    /// <remarks>Overlong input is truncated at a grapheme boundary. Short input is padded with deterministic random ASCII letters from the isolated text stream.</remarks>
    /// <param name="value">The well-formed UTF-16 string to clamp.</param>
    /// <param name="minimumGraphemes">The minimum grapheme count, supplied by letter padding when needed.</param>
    /// <param name="maximumGraphemes">The maximum grapheme count, enforced by truncation.</param>
    /// <returns>A string whose grapheme count is within the requested inclusive range.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is invalid or the result exceeds the configured output limit.</exception>
    public string Clamp(string value, int minimumGraphemes, int maximumGraphemes)
    {
        return UnicodeText.Clamp(RandomValue, value, minimumGraphemes, maximumGraphemes, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Normalizes a well-formed UTF-16 string using the mode captured by this generator.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>The normalized string, or the original string when the captured mode is <c>None</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    public string Normalize(string value)
    {
        return NormalizeFromValueAndMode(value, Context.NormalizationModeValue);
    }

    /// <summary>
    /// Normalizes a well-formed UTF-16 string using an explicit Unicode normalization mode.
    /// </summary>
    /// <param name="value">The string to normalize.</param>
    /// <param name="mode">The normalization mode to apply.</param>
    /// <returns>The normalized string, or the original string for <c>None</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>mode</c> is unsupported or the output exceeds the configured limit.</exception>
    public string NormalizeFromValueAndMode(string value, NormalizationMode mode)
    {
        return UnicodeText.Normalize(value, mode, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Reports whether a string satisfies the normalization mode captured by this generator.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to inspect.</param>
    /// <returns>True when the string satisfies the captured mode; <c>None</c> always returns true.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    public bool IsNormalized(string value)
    {
        return IsNormalizedFromValueAndMode(value, Context.NormalizationModeValue);
    }

    /// <summary>
    /// Reports whether a string satisfies an explicit Unicode normalization mode.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to inspect.</param>
    /// <param name="mode">The normalization mode to inspect.</param>
    /// <returns>True when the string satisfies the requested mode; <c>None</c> always returns true.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>mode</c> is unsupported.</exception>
    public bool IsNormalizedFromValueAndMode(string value, NormalizationMode mode)
    {
        return UnicodeText.IsNormalized(value, mode);
    }

    /// <summary>
    /// Converts text to lowercase using the locale captured by this generator.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <returns>The locale-cased string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> is malformed or the captured locale is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the input or output exceeds the configured limit.</exception>
    public string Lower(string value)
    {
        return LowerFromValueAndLocale(value, Context.CapturedOptions.Locale);
    }

    /// <summary>
    /// Converts text to lowercase using an explicit BCP 47 locale.
    /// </summary>
    /// <remarks>A platform culture is used when available; otherwise casing falls back to the invariant culture.</remarks>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <param name="locale">The BCP 47 locale that supplies casing rules.</param>
    /// <returns>The locale-cased string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the input or output exceeds the configured limit.</exception>
    public string LowerFromValueAndLocale(string value, string locale)
    {
        return TextTransforms.Lower(value, locale, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Converts text to uppercase using the locale captured by this generator.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <returns>The locale-cased string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> is malformed or the captured locale is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the input or output exceeds the configured limit.</exception>
    public string Upper(string value)
    {
        return UpperFromValueAndLocale(value, Context.CapturedOptions.Locale);
    }

    /// <summary>
    /// Converts text to uppercase using an explicit BCP 47 locale.
    /// </summary>
    /// <remarks>A platform culture is used when available; otherwise casing falls back to the invariant culture.</remarks>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <param name="locale">The BCP 47 locale that supplies casing rules.</param>
    /// <returns>The locale-cased string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the input or output exceeds the configured limit.</exception>
    public string UpperFromValueAndLocale(string value, string locale)
    {
        return TextTransforms.Upper(value, locale, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Converts words to title case using the locale captured by this generator.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <returns>The locale title-cased string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> is malformed or the captured locale is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the input or output exceeds the configured limit.</exception>
    public string TitleCase(string value)
    {
        return TitleCaseFromValueAndLocale(value, Context.CapturedOptions.Locale);
    }

    /// <summary>
    /// Converts words to title case using an explicit BCP 47 locale.
    /// </summary>
    /// <remarks>A platform culture is used when available; otherwise casing falls back to the invariant culture.</remarks>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <param name="locale">The BCP 47 locale that supplies casing rules.</param>
    /// <returns>The locale title-cased string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the input or output exceeds the configured limit.</exception>
    public string TitleCaseFromValueAndLocale(string value, string locale)
    {
        return TextTransforms.TitleCase(value, locale, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Compatibility-decomposes Latin text, removes combining marks, and returns ASCII characters only.
    /// </summary>
    /// <remarks>Characters without a declared Latin-to-ASCII mapping are omitted.</remarks>
    /// <param name="value">The well-formed UTF-16 string to fold.</param>
    /// <returns>The ASCII-folded string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    public string FoldAscii(string value)
    {
        return TextTransforms.FoldAscii(value, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns the stable identifiers of the explicitly declared transliteration tables.
    /// </summary>
    /// <returns>A new array containing the declared table identifiers.</returns>
    public string[] TransliterationTables()
    {
        return TextTransforms.Tables();
    }

    /// <summary>
    /// Applies one explicitly declared, narrow transliteration table.
    /// </summary>
    /// <remarks>Unmapped characters are preserved. Combining marks are removed, including diacritics decomposed from mapped letters. The tables are deterministic project mappings and do not claim conformance to a national or international romanization standard.</remarks>
    /// <param name="value">The well-formed UTF-16 string to transliterate.</param>
    /// <param name="table">A table identifier returned by <c>TransliterationTables</c>.</param>
    /// <returns>The transliterated string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or the table is not declared.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    public string Transliterate(string value, string table)
    {
        return TextTransforms.Transliterate(value, table, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Produces a lowercase ASCII slug separated by hyphens.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <returns>The slug, or an empty string when no declared mapping produces letters or digits.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when intermediate or final output exceeds the configured limit.</exception>
    public string Slug(string value)
    {
        return SlugFromValueAndSeparator(value, '-');
    }

    /// <summary>
    /// Produces a lowercase ASCII slug with an explicit safe separator.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <param name="separator">Either a hyphen or underscore.</param>
    /// <returns>The slug, or an empty string when no declared mapping produces letters or digits.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or the separator is unsupported.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when intermediate or final output exceeds the configured limit.</exception>
    public string SlugFromValueAndSeparator(string value, char separator)
    {
        return TextTransforms.Slug(value, separator, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Selects one locale word according to its authored frequency weight.
    /// </summary>
    /// <returns>A word from the resolved locale word data.</returns>
    /// <exception cref="FakeException">Thrown when no permitted locale source contains usable word data.</exception>
    public string Word()
    {
        return LoremGenerator.Catalog(Context).ChooseWord(RandomValue);
    }

    /// <summary>
    /// Selects one weighted locale word having a requested part of speech.
    /// </summary>
    /// <param name="partOfSpeech">A part identifier returned by <c>PartsOfSpeech</c>.</param>
    /// <returns>A word from the requested category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>partOfSpeech</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the category is empty or unavailable.</exception>
    /// <exception cref="FakeException">Thrown when no permitted locale source contains usable word data.</exception>
    public string WordFromPartOfSpeech(string partOfSpeech)
    {
        Guard.NotNullOrWhiteSpace(partOfSpeech, "partOfSpeech");
        return LoremGenerator.Catalog(Context).ChooseWordFromRandomAndPartOfSpeech(RandomValue, partOfSpeech);
    }

    /// <summary>
    /// Selects one weighted locale word having a requested part of speech.
    /// </summary>
    /// <param name="partOfSpeech">A part identifier returned by <c>PartsOfSpeech</c>.</param>
    /// <returns>A word from the requested category.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>partOfSpeech</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the category is empty or unavailable.</exception>
    /// <exception cref="FakeException">Thrown when no permitted locale source contains usable word data.</exception>
    public string PartOfSpeech(string partOfSpeech)
    {
        return WordFromPartOfSpeech(partOfSpeech);
    }

    /// <summary>
    /// Selects a part-of-speech identifier according to the combined weights of its words.
    /// </summary>
    /// <returns>A part identifier present in the resolved locale word data.</returns>
    /// <exception cref="FakeException">Thrown when no permitted locale source contains usable word data.</exception>
    public string PartOfSpeechFromNoArguments()
    {
        return LoremGenerator.Catalog(Context).SelectPart(RandomValue);
    }

    /// <summary>
    /// Returns the available part-of-speech identifiers in stable ordinal order.
    /// </summary>
    /// <returns>A new array containing the available identifiers.</returns>
    /// <exception cref="FakeException">Thrown when no permitted locale source contains usable word data.</exception>
    public string[] PartsOfSpeech()
    {
        return LoremGenerator.Catalog(Context).AvailableParts();
    }

    /// <summary>
    /// Generates space-separated weighted locale words.
    /// </summary>
    /// <param name="count">The number of words to generate.</param>
    /// <returns>The generated words.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string Words(int count)
    {
        return WordsFromCountAndSeparator(count, " ");
    }

    /// <summary>
    /// Generates weighted locale words separated by caller-supplied text.
    /// </summary>
    /// <param name="count">The number of words to generate.</param>
    /// <param name="separator">The text placed between words.</param>
    /// <returns>The generated words.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>separator</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string WordsFromCountAndSeparator(int count, string separator)
    {
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        Guard.NotNull(separator, "separator");
        if (count == 0)
        {
            return "";
        }

        return LoremGenerator.Words(RandomValue, LoremGenerator.Catalog(Context), count, separator, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates a punctuated sentence containing six through twelve words.
    /// </summary>
    /// <returns>A locale-word sentence ending in a period.</returns>
    public string Sentence()
    {
        return SentenceFromMinimumWordsAndMaximumWords(6, 12);
    }

    /// <summary>
    /// Generates a punctuated sentence with an exact word count.
    /// </summary>
    /// <param name="wordCount">The positive number of words.</param>
    /// <returns>A locale-word sentence ending in a period.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string SentenceFromWordCount(int wordCount)
    {
        Guard.Positive(wordCount, "wordCount");
        return SentenceValue(LoremGenerator.Catalog(Context), wordCount);
    }

    /// <summary>
    /// Generates a punctuated sentence with a word count in an inclusive range.
    /// </summary>
    /// <param name="minimumWords">The positive minimum word count.</param>
    /// <param name="maximumWords">The maximum word count.</param>
    /// <returns>A locale-word sentence ending in a period.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the range or output exceeds a configured limit.</exception>
    public string SentenceFromMinimumWordsAndMaximumWords(int minimumWords, int maximumWords)
    {
        ValidateRange(minimumWords, maximumWords, "minimumWords", "maximumWords", true);
        LoremCatalog catalog = LoremGenerator.Catalog(Context);
        LoremGenerator.EnsureSentences(catalog, 1, maximumWords, "", Context.CapturedOptions.Locale, Context.CapturedOptions.MaximumOutputSize, "maximumWords");
        return SentenceValue(catalog, RandomValue.IntFromMinimumAndMaximumAndMaximumInclusive(minimumWords, maximumWords, true));
    }

    /// <summary>
    /// Generates a requested number of sentences separated by spaces.
    /// </summary>
    /// <param name="count">The number of sentences.</param>
    /// <returns>The generated sentences.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string Sentences(int count)
    {
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        if (count == 0)
        {
            return "";
        }

        return SentenceCollection(LoremGenerator.Catalog(Context), count, " ");
    }

    /// <summary>
    /// Generates a paragraph containing three through six sentences.
    /// </summary>
    /// <returns>The generated paragraph.</returns>
    public string Paragraph()
    {
        return ParagraphFromMinimumSentencesAndMaximumSentences(3, 6);
    }

    /// <summary>
    /// Generates a paragraph with an exact sentence count.
    /// </summary>
    /// <param name="sentenceCount">The number of sentences.</param>
    /// <returns>The generated paragraph.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string ParagraphFromSentenceCount(int sentenceCount)
    {
        Guard.Count(sentenceCount, Context.CapturedOptions.MaximumCount, "sentenceCount");
        if (sentenceCount == 0)
        {
            return "";
        }

        return SentenceCollection(LoremGenerator.Catalog(Context), sentenceCount, " ");
    }

    /// <summary>
    /// Generates a paragraph with a sentence count in an inclusive range.
    /// </summary>
    /// <param name="minimumSentences">The nonnegative minimum sentence count.</param>
    /// <param name="maximumSentences">The maximum sentence count.</param>
    /// <returns>The generated paragraph.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the range or output exceeds a configured limit.</exception>
    public string ParagraphFromMinimumSentencesAndMaximumSentences(int minimumSentences, int maximumSentences)
    {
        ValidateRange(minimumSentences, maximumSentences, "minimumSentences", "maximumSentences", false);
        if (maximumSentences == 0)
        {
            return "";
        }

        LoremCatalog catalog = LoremGenerator.Catalog(Context);
        LoremGenerator.EnsureSentences(catalog, maximumSentences, 12, " ", Context.CapturedOptions.Locale, Context.CapturedOptions.MaximumOutputSize, "maximumSentences");
        int count = RandomValue.IntFromMinimumAndMaximumAndMaximumInclusive(minimumSentences, maximumSentences, true);
        return SentenceCollection(catalog, count, " ");
    }

    /// <summary>
    /// Generates paragraphs separated by one empty line.
    /// </summary>
    /// <param name="count">The number of paragraphs.</param>
    /// <returns>The generated paragraphs using line-feed separators.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string Paragraphs(int count)
    {
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        if (count == 0)
        {
            return "";
        }

        LoremCatalog catalog = LoremGenerator.Catalog(Context);
        LoremGenerator.EnsureParagraphs(catalog, count, Context.CapturedOptions.Locale, Context.CapturedOptions.MaximumOutputSize);
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < count)
        {
            if (index > 0)
            {
                builder.Append("\n\n");
            }

            int sentences = RandomValue.IntFromMinimumAndMaximumAndMaximumInclusive(3, 6, true);
            builder.Append(SentenceCollection(catalog, sentences, " "));
            Guard.OutputSize(builder.Length, Context.CapturedOptions.MaximumOutputSize, "count");
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates sentence lines separated by line-feed characters.
    /// </summary>
    /// <param name="count">The number of lines.</param>
    /// <returns>The generated lines.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string Lines(int count)
    {
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        if (count == 0)
        {
            return "";
        }

        return SentenceCollection(LoremGenerator.Catalog(Context), count, "\n");
    }

    /// <summary>
    /// Generates a lowercase ASCII slug from an exact number of locale words.
    /// </summary>
    /// <param name="wordCount">The number of source words.</param>
    /// <returns>The generated slug.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    public string SlugFromWordCount(int wordCount)
    {
        return Slug(Words(wordCount));
    }

    /// <summary>
    /// Generates locale filler text having an exact UTF-16 length.
    /// </summary>
    /// <remarks>Words are separated when space permits. A final word may be shortened at a Unicode text-element boundary, with periods filling any remaining UTF-16 units.</remarks>
    /// <param name="length">The exact output length.</param>
    /// <returns>Text whose string length equals the requested length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length exceeds a configured limit.</exception>
    public string Text(int length)
    {
        Guard.NonNegative(length, "length");
        Guard.OutputSize(length, Context.CapturedOptions.MaximumOutputSize, "length");
        if (length == 0)
        {
            return "";
        }

        return LoremGenerator.Text(RandomValue, LoremGenerator.Catalog(Context), length, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates locale filler text with a UTF-16 length in an inclusive range.
    /// </summary>
    /// <param name="minimumLength">The nonnegative minimum length.</param>
    /// <param name="maximumLength">The maximum length.</param>
    /// <returns>Text whose string length is within the requested bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound exceeds a configured limit.</exception>
    public string TextFromMinimumLengthAndMaximumLength(int minimumLength, int maximumLength)
    {
        Guard.NonNegative(minimumLength, "minimumLength");
        Guard.NonNegative(maximumLength, "maximumLength");
        if (maximumLength < minimumLength)
        {
            throw new ArgumentOutOfRangeException("maximumLength", maximumLength, "maximumLength must be greater than or equal to minimumLength.");
        }

        Guard.OutputSize(maximumLength, Context.CapturedOptions.MaximumOutputSize, "maximumLength");
        int length = RandomValue.IntFromMinimumAndMaximumAndMaximumInclusive(minimumLength, maximumLength, true);
        if (length == 0)
        {
            return "";
        }

        return LoremGenerator.Text(RandomValue, LoremGenerator.Catalog(Context), length, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    string SentenceValue(LoremCatalog catalog, int wordCount)
    {
        return LoremGenerator.Sentence(RandomValue, catalog, wordCount, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.Locale);
    }

    string SentenceCollection(LoremCatalog catalog, int count, string separator)
    {
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        LoremGenerator.EnsureSentences(catalog, count, 12, separator, Context.CapturedOptions.Locale, Context.CapturedOptions.MaximumOutputSize, "count");
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < count)
        {
            if (index > 0)
            {
                builder.Append(separator);
            }

            builder.Append(SentenceValue(catalog, RandomValue.IntFromMinimumAndMaximumAndMaximumInclusive(6, 12, true)));
            Guard.OutputSize(builder.Length, Context.CapturedOptions.MaximumOutputSize, "count");
            index = index + 1;
        }

        return builder.ToString();
    }

    void ValidateRange(int minimum, int maximum, string minimumName, string maximumName, bool positiveMinimum)
    {
        if (positiveMinimum)
        {
            Guard.Positive(minimum, minimumName);
        }
        else
        {
            Guard.NonNegative(minimum, minimumName);
        }

        Guard.Count(maximum, Context.CapturedOptions.MaximumCount, maximumName);
        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(maximumName, maximum, $"{maximumName} must be greater than or equal to {minimumName}.");
        }
    }

    /// <summary>
    /// Produces an ASCII identifier containing letters, digits, and single underscores.
    /// </summary>
    /// <remarks>A leading underscore is inserted when the result would be empty or begin with a digit. Language-specific reserved words are not modified.</remarks>
    /// <param name="value">The well-formed UTF-16 string to convert.</param>
    /// <returns>The identifier-safe text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when intermediate or final output exceeds the configured limit.</exception>
    public string Identifier(string value)
    {
        return TextTransforms.Identifier(value, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Classifies the strong writing direction of a string.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to inspect.</param>
    /// <returns>The detected writing direction.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    public WritingDirection Direction(string value)
    {
        return TextDirection.Direction(value);
    }

    /// <summary>
    /// Reports whether letters from more than one broad Unicode script group occur in a string.
    /// </summary>
    /// <remarks>Common and inherited characters are ignored. Han, Hiragana, and Katakana are treated as one Japanese text group.</remarks>
    /// <param name="value">The well-formed UTF-16 string to inspect.</param>
    /// <returns>True when more than one recognized script group is present.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    public bool HasMixedScripts(string value)
    {
        return TextDirection.HasMixedScripts(value);
    }

    /// <summary>
    /// Wraps nonempty text in Unicode directional isolate controls selected from its strong direction.
    /// </summary>
    /// <remarks>Left-to-right and right-to-left text use explicit isolates. Mixed or neutral text uses the first-strong isolate.</remarks>
    /// <param name="value">The well-formed UTF-16 string to isolate.</param>
    /// <returns>The isolated text, or the original empty string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    public string BidiIsolate(string value)
    {
        return TextDirection.Isolate(value, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Removes Unicode bidirectional marks, embeddings, overrides, and isolate controls.
    /// </summary>
    /// <param name="value">The well-formed UTF-16 string to sanitize.</param>
    /// <returns>The text without bidirectional controls.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    public string StripBidiControls(string value)
    {
        return TextDirection.StripControls(value, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Replaces mask tokens with random ASCII characters while preserving literal text.
    /// </summary>
    /// <remarks>
    /// A <c>#</c> token produces a decimal digit, <c>?</c> produces an ASCII letter,
    /// and <c>*</c> produces an ASCII letter or digit. Any other unescaped character is
    /// copied literally. A backslash escapes one token or another backslash.
    /// </remarks>
    /// <param name="mask">The well-formed UTF-16 mask to evaluate.</param>
    /// <returns>The evaluated mask.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>mask</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed or contains a trailing or unsupported escape.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the evaluated output exceeds the configured limit.</exception>
    public string Format(string mask)
    {
        return MaskFormatter.Format(RandomValue, mask, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Evaluates a mask into an exactly sized destination span.
    /// </summary>
    /// <remarks>
    /// A <c>#</c> token produces a decimal digit, <c>?</c> produces an ASCII letter,
    /// and <c>*</c> produces an ASCII letter or digit. Other unescaped characters are
    /// copied literally. A backslash escapes one token or another backslash.
    /// </remarks>
    /// <param name="destination">The destination whose length must equal the evaluated mask length.</param>
    /// <param name="mask">The well-formed UTF-16 mask to evaluate.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>mask</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed, an escape is unsupported, or the destination length is not exact.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the evaluated output exceeds the configured limit.</exception>
    public int WriteFormat(Span<char> destination, string mask)
    {
        return MaskFormatter.Write(RandomValue, destination, mask, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates text from the documented bounded regular-expression subset.
    /// </summary>
    /// <remarks>
    /// Supported constructs are literal Unicode scalars, grouping, alternation, start and
    /// end anchors, dot, ASCII character classes and ranges with optional negation,
    /// <c>\d</c>, <c>\w</c>, <c>\s</c> and their uppercase complements, common escaped
    /// control characters and metacharacters, plus <c>?</c>, <c>*</c>, <c>+</c>,
    /// <c>{n}</c>, <c>{n,m}</c>, and <c>{n,}</c> quantifiers. Unbounded quantifiers use a
    /// repetition maximum of ten. Alternation branches and characters within a class are
    /// selected uniformly. Negated classes and uppercase shorthand complements use
    /// printable ASCII as their universe.
    /// </remarks>
    /// <param name="pattern">The bounded pattern to parse and generate.</param>
    /// <returns>A generated string that matches the supported pattern.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>pattern</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the pattern is malformed or uses an unsupported construct.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured count, repetition, or worst-case output limit is exceeded.</exception>
    public string FromPattern(string pattern)
    {
        return FromPatternFromPatternAndMaximumRepetitions(pattern, 10);
    }

    /// <summary>
    /// Generates text from the documented bounded regular-expression subset with an explicit repetition ceiling.
    /// </summary>
    /// <remarks>
    /// The ceiling bounds <c>*</c>, <c>+</c>, and open upper bounds, and explicit finite
    /// bounds may not exceed it. Parsing depth is limited to 64, parser complexity is
    /// limited by the configured maximum count, and worst-case UTF-16 output is checked
    /// before random generation begins.
    /// </remarks>
    /// <param name="pattern">The bounded pattern to parse and generate.</param>
    /// <param name="maximumRepetitions">The positive inclusive ceiling for every quantified atom.</param>
    /// <returns>A generated string that matches the supported pattern.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>pattern</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the pattern is malformed or uses an unsupported construct.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the repetition ceiling or another configured limit is invalid.</exception>
    public string FromPatternFromPatternAndMaximumRepetitions(string pattern, int maximumRepetitions)
    {
        return PatternGenerator.Generate(RandomValue, pattern, maximumRepetitions, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns the built-in placeholder names accepted by template evaluation.
    /// </summary>
    /// <remarks>The returned names have stable ordinal ordering and the returned array is independent.</remarks>
    /// <returns>A new array containing the built-in template token names.</returns>
    public string[] TemplateTokens()
    {
        return TemplateEngine.Tokens();
    }

    /// <summary>
    /// Evaluates a template using the built-in named provider catalog.
    /// </summary>
    /// <remarks>
    /// Placeholders use <c>{{name}}</c> or <c>{{name:argument}}</c>. Built-in token names
    /// begin with <c>primitive.</c> or <c>text.</c>. A backslash escapes a backslash or
    /// brace in literal template text. Evaluation proceeds left-to-right and depth-first.
    /// </remarks>
    /// <param name="template">The well-formed UTF-16 template to evaluate.</param>
    /// <returns>The evaluated template.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>template</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, a token name, or an argument is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured complexity or output limit is exceeded.</exception>
    public string Template(string template)
    {
        return TemplateFromTemplateAndValuesAndMaximumDepth(template, new Dictionary<string, string>(), 16);
    }

    /// <summary>
    /// Evaluates a template using built-in tokens and caller-supplied nested template values.
    /// </summary>
    /// <remarks>
    /// Caller values are resolved only when referenced and may contain further placeholders.
    /// Built-in catalog names take precedence over supplied values. Cycles are rejected.
    /// </remarks>
    /// <param name="template">The well-formed UTF-16 template to evaluate.</param>
    /// <param name="values">The case-sensitive or caller-comparer-based named template values.</param>
    /// <returns>The evaluated template.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, or nested evaluation fails.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured complexity, recursion, or output limit is exceeded.</exception>
    public string TemplateFromTemplateAndValues(string template, Dictionary<string, string> values)
    {
        return TemplateFromTemplateAndValuesAndMaximumDepth(template, values, 16);
    }

    /// <summary>
    /// Evaluates a template with an explicit nested-value recursion limit.
    /// </summary>
    /// <remarks>
    /// The built-in catalog includes digit, digits, alpha, alphanumeric, hex, hash, token,
    /// PIN, password, passphrase, recovery-code, mask-format, and bounded-pattern tokens.
    /// Numeric arguments select output length or passphrase word count. The mask and pattern
    /// tokens accept their mask or bounded pattern after the colon.
    /// </remarks>
    /// <param name="template">The well-formed UTF-16 template to evaluate.</param>
    /// <param name="values">The named nested template values.</param>
    /// <param name="maximumDepth">The positive maximum number of nested caller-value expansions.</param>
    /// <returns>The evaluated template.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a reference argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, or nested evaluation fails.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured complexity, recursion, or output limit is exceeded.</exception>
    public string TemplateFromTemplateAndValuesAndMaximumDepth(string template, Dictionary<string, string> values, int maximumDepth)
    {
        return TemplateEngine.Generate(RandomValue, template, values, maximumDepth, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }
}
