using System;

namespace fake;

internal static class BookGenerator
{
    const int IsbnLength = 13;
    const int IsbnPayloadLength = 12;
    const int MaximumSeriesNumber = 12;
    const double PublicationWindowDays = 18262.0;

    internal static string Title(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTitleData(context);
        Guard.OutputSize(
            MaximumTitleLength(),
            context.CapturedOptions.MaximumOutputSize,
            "title");
        return TitleUnchecked(
            random.IntFromMaximumExclusive(BookData.TitleAdjectives.Length),
            random.IntFromMaximumExclusive(BookData.TitleSubjects.Length));
    }

    internal static string Author(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAuthorData(context);
        Guard.OutputSize(
            MaximumAuthorLength(),
            context.CapturedOptions.MaximumOutputSize,
            "author");
        return AuthorUnchecked(random);
    }

    internal static string Publisher(
        GenerationContext context,
        FakeRandom random)
    {
        ValidatePublisherData(context);
        Guard.OutputSize(
            MaximumPublisherLength(),
            context.CapturedOptions.MaximumOutputSize,
            "publisher");
        return PublisherUnchecked(random);
    }

    internal static string Series(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateSeriesData(context);
        Guard.OutputSize(
            MaximumSeriesLength(),
            context.CapturedOptions.MaximumOutputSize,
            "series");
        return SeriesUnchecked(
            random.IntFromMaximumExclusive(BookData.TitleSubjects.Length),
            random.IntFromMaximumExclusive(BookData.SeriesLabels.Length));
    }

    internal static string Genre(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, BookData.Genres, "genreData");
        return Value(context, random, BookData.Genres, "genre");
    }

    internal static string Format(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, BookData.Formats, "formatData");
        return Value(context, random, BookData.Formats, "format");
    }

    internal static string Isbn(
        GenerationContext context,
        FakeRandom random)
    {
        return PublicationIdentifierGenerator.GenerateIsbn13(
            random,
            BookIdentifierStyle.Compact,
            context.CapturedOptions.MaximumCount,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static DateTimeOffset PublicationDate(
        GenerationContext context,
        FakeRandom random)
    {
        return RelativeDateGenerator.Recent(
            random,
            context.ReferenceTime(),
            TimeSpan.FromDays(PublicationWindowDays));
    }

    internal static BookRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTitleData(context);
        ValidateAuthorData(context);
        ValidatePublisherData(context);
        ValidateSeriesData(context);
        ValidateValues(context, BookData.Genres, "genreData");
        ValidateValues(context, BookData.Formats, "formatData");
        Guard.Count(
            IsbnPayloadLength,
            context.CapturedOptions.MaximumCount,
            "bookRecord");
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "bookRecord");
        int subjectIndex = random.IntFromMaximumExclusive(BookData.TitleSubjects.Length);
        int adjectiveIndex = random.IntFromMaximumExclusive(BookData.TitleAdjectives.Length);
        int seriesLabelIndex = random.IntFromMaximumExclusive(BookData.SeriesLabels.Length);
        BookRecord result = new BookRecord();
        result.Title = TitleUnchecked(adjectiveIndex, subjectIndex);
        result.Author = AuthorUnchecked(random);
        result.Publisher = PublisherUnchecked(random);
        result.Series = SeriesUnchecked(subjectIndex, seriesLabelIndex);
        result.SeriesNumber = random.IntFromMinimumAndMaximumAndMaximumInclusive(1, MaximumSeriesNumber, true);
        result.Genre = BookData.Genres[random.IntFromMaximumExclusive(BookData.Genres.Length)];
        result.Format = BookData.Formats[random.IntFromMaximumExclusive(BookData.Formats.Length)];
        result.Isbn = PublicationIdentifierGenerator.GenerateIsbn13(
            random,
            BookIdentifierStyle.Compact,
            context.CapturedOptions.MaximumCount,
            context.CapturedOptions.MaximumOutputSize);
        result.PublicationDate = PublicationDate(context, random);
        result.IsSynthetic = true;
        result.IsbnIsAssigned = false;
        result.RepresentsPublishedWork = false;
        return result;
    }

    static string TitleUnchecked(int adjectiveIndex, int subjectIndex)
    {
        return "The " + BookData.TitleAdjectives[adjectiveIndex] + " " +
            BookData.TitleSubjects[subjectIndex];
    }

    static string AuthorUnchecked(FakeRandom random)
    {
        return BookData.AuthorGivenNames[
            random.IntFromMaximumExclusive(BookData.AuthorGivenNames.Length)] + " " +
            BookData.AuthorSurnames[
                random.IntFromMaximumExclusive(BookData.AuthorSurnames.Length)];
    }

    static string PublisherUnchecked(FakeRandom random)
    {
        return BookData.PublisherStems[
            random.IntFromMaximumExclusive(BookData.PublisherStems.Length)] + " " +
            BookData.PublisherKinds[
                random.IntFromMaximumExclusive(BookData.PublisherKinds.Length)];
    }

    static string SeriesUnchecked(int subjectIndex, int labelIndex)
    {
        return "The " + BookData.TitleSubjects[subjectIndex] + " " +
            BookData.SeriesLabels[labelIndex];
    }

    static string Value(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string outputName)
    {
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static void ValidateTitleData(GenerationContext context)
    {
        ValidateValues(context, BookData.TitleAdjectives, "titleData");
        ValidateValues(context, BookData.TitleSubjects, "titleData");
    }

    static void ValidateAuthorData(GenerationContext context)
    {
        ValidateValues(context, BookData.AuthorGivenNames, "authorData");
        ValidateValues(context, BookData.AuthorSurnames, "authorData");
    }

    static void ValidatePublisherData(GenerationContext context)
    {
        ValidateValues(context, BookData.PublisherStems, "publisherData");
        ValidateValues(context, BookData.PublisherKinds, "publisherData");
    }

    static void ValidateSeriesData(GenerationContext context)
    {
        ValidateValues(context, BookData.TitleSubjects, "seriesData");
        ValidateValues(context, BookData.SeriesLabels, "seriesData");
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored publication data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumTitleLength()
    {
        return 4 + MaximumLength(BookData.TitleAdjectives) + 1 +
            MaximumLength(BookData.TitleSubjects);
    }

    static int MaximumAuthorLength()
    {
        return MaximumLength(BookData.AuthorGivenNames) + 1 +
            MaximumLength(BookData.AuthorSurnames);
    }

    static int MaximumPublisherLength()
    {
        return MaximumLength(BookData.PublisherStems) + 1 +
            MaximumLength(BookData.PublisherKinds);
    }

    static int MaximumSeriesLength()
    {
        return 4 + MaximumLength(BookData.TitleSubjects) + 1 +
            MaximumLength(BookData.SeriesLabels);
    }

    static int MaximumRecordSize()
    {
        return MaximumTitleLength() + MaximumAuthorLength() +
            MaximumPublisherLength() + MaximumSeriesLength() +
            MaximumLength(BookData.Genres) +
            MaximumLength(BookData.Formats) + IsbnLength;
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
