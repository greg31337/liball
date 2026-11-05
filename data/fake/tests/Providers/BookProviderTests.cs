using fake;

namespace fake.Tests;

[TestFixture]
public class BookProviderTests
{
    static string[] TitleAdjectives = new string[]
    {
        "Amber", "Brisk", "Distant", "Gentle", "Hidden", "Quiet",
        "Silver", "Wandering"
    };

    static string[] TitleSubjects = new string[]
    {
        "Archive", "Bridge", "Garden", "Harbor", "Lantern", "Map",
        "Orchard", "Workshop"
    };

    static string[] AuthorGivenNames = new string[]
    {
        "Ari", "Bo", "Casey", "Dee", "Ember", "Fin", "Gray", "Hollis"
    };

    static string[] AuthorSurnames = new string[]
    {
        "Demo", "Example", "Fixture", "Mock", "Placeholder", "Sample",
        "Test", "Trial"
    };

    static string[] PublisherStems = new string[]
    {
        "Aster Example", "Beacon Sample", "Cedar Fixture", "Harbor Test",
        "Lantern Demo", "Northstar Mock"
    };

    static string[] PublisherKinds = new string[]
    {
        "Books", "Editions", "House", "Press"
    };

    static string[] SeriesLabels = new string[]
    {
        "Collection", "Cycle", "Library", "Sequence", "Series", "Set"
    };

    static string[] Genres = new string[]
    {
        "biography", "essays", "fantasy", "historical fiction", "mystery",
        "poetry", "reference", "romance", "science fiction", "thriller"
    };

    static string[] Formats = new string[]
    {
        "audiobook", "ebook", "hardcover", "large print", "paperback"
    };

    [Test]
    public void NamesAndTitlesAreDeterministicCompositions()
    {
        const long seed = 12500;
        BookProvider first = Fake.Create(seed).Book();
        BookProvider second = Fake.Create(seed).Book();
        HashSet<string> titles = new HashSet<string>();
        HashSet<string> authors = new HashSet<string>();
        HashSet<string> publishers = new HashSet<string>();
        HashSet<string> series = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            string title = first.Title();
            string author = first.Author();
            string publisher = first.Publisher();
            string seriesName = first.Series();

            Assert.Multiple(() =>
            {
                Assert.That(title, Is.EqualTo(second.Title()));
                Assert.That(author, Is.EqualTo(second.Author()));
                Assert.That(publisher, Is.EqualTo(second.Publisher()));
                Assert.That(seriesName, Is.EqualTo(second.Series()));
            });
            AssertTitle(title);
            AssertAuthor(author);
            AssertPublisher(publisher);
            AssertSeries(seriesName);
            titles.Add(title);
            authors.Add(author);
            publishers.Add(publisher);
            series.Add(seriesName);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(titles.Count, Is.GreaterThan(40));
            Assert.That(authors.Count, Is.GreaterThan(40));
            Assert.That(publishers.Count, Is.GreaterThan(18));
            Assert.That(series.Count, Is.GreaterThan(35));
        });
    }

    [Test]
    public void GenresAndFormatsUseReviewedGenericVocabulary()
    {
        const long seed = 12501;
        BookProvider first = Fake.Create(seed).Book();
        BookProvider second = Fake.Create(seed).Book();
        HashSet<string> genres = new HashSet<string>();
        HashSet<string> formats = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string genre = first.Genre();
            string format = first.Format();
            Assert.Multiple(() =>
            {
                Assert.That(genre, Is.EqualTo(second.Genre()));
                Assert.That(format, Is.EqualTo(second.Format()));
                Assert.That(genre, Is.AnyOf(Genres));
                Assert.That(format, Is.AnyOf(Formats));
            });
            genres.Add(genre);
            formats.Add(format);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(genres, Is.EquivalentTo(Genres));
            Assert.That(formats, Is.EquivalentTo(Formats));
        });
    }

    [Test]
    public void IsbnShapesAreValidDeterministicAndUnassignedByContract()
    {
        const long seed = 12502;
        BookProvider first = Fake.Create(seed).Book();
        BookProvider second = Fake.Create(seed).Book();
        ChecksumProvider checksum = Fake.Create(1).Checksum();

        int index = 0;
        while (index < 256)
        {
            string isbn = first.Isbn();
            Assert.Multiple(() =>
            {
                Assert.That(isbn, Is.EqualTo(second.Isbn()));
                Assert.That(isbn, Does.Match("^97[89][0-9]{10}$"));
                Assert.That(checksum.Isbn13().Validate(isbn), Is.True);
            });
            index++;
        }

        Assert.That(Fake.Create(seed).Book().Isbn13(),
            Is.EqualTo(Fake.Create(seed).Book().Isbn()));
    }

    [Test]
    public void PublicationDatesUseTheCapturedReferenceTime()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2038,
            4,
            5,
            6,
            7,
            8,
            TimeSpan.FromHours(-3.5));
        FakeOptions firstOptions = FakeOptions.Create(12503);
        firstOptions.ReferenceTime = reference;
        FakeOptions secondOptions = FakeOptions.Create(12503);
        secondOptions.ReferenceTime = reference;
        BookProvider first = Fake.Create(firstOptions).Book();
        BookProvider second = Fake.Create(secondOptions).Book();

        int index = 0;
        while (index < 256)
        {
            DateTimeOffset publicationDate = first.PublicationDate();
            Assert.Multiple(() =>
            {
                Assert.That(publicationDate,
                    Is.EqualTo(second.PublicationDate()));
                Assert.That(publicationDate.Offset, Is.EqualTo(TimeSpan.Zero));
                Assert.That(publicationDate, Is.LessThanOrEqualTo(reference));
                Assert.That(publicationDate,
                    Is.GreaterThanOrEqualTo(reference.AddDays(-18262.0)));
            });
            index++;
        }
    }

    [Test]
    public void RecordsCorrelateTitleSeriesAndPublicationMetadata()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2040,
            8,
            9,
            10,
            11,
            12,
            TimeSpan.Zero);
        FakeOptions firstOptions = FakeOptions.Create(12504);
        firstOptions.ReferenceTime = reference;
        FakeOptions secondOptions = FakeOptions.Create(12504);
        secondOptions.ReferenceTime = reference;
        BookProvider first = Fake.Create(firstOptions).Book();
        BookProvider second = Fake.Create(secondOptions).Book();
        ChecksumProvider checksum = Fake.Create(2).Checksum();

        int index = 0;
        while (index < 512)
        {
            BookRecord value = first.Record();
            BookRecord same = second.Record();
            string[] titleParts = value.Title.Split(' ');
            string[] seriesParts = value.Series.Split(' ');

            Assert.Multiple(() =>
            {
                AssertRecordEqual(value, same);
                Assert.That(titleParts[2], Is.EqualTo(seriesParts[1]));
                Assert.That(value.SeriesNumber, Is.InRange(1, 12));
                Assert.That(value.Genre, Is.AnyOf(Genres));
                Assert.That(value.Format, Is.AnyOf(Formats));
                Assert.That(checksum.Isbn13().Validate(value.Isbn), Is.True);
                Assert.That(value.PublicationDate,
                    Is.LessThanOrEqualTo(reference));
                Assert.That(value.PublicationDate,
                    Is.GreaterThanOrEqualTo(reference.AddDays(-18262.0)));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.IsbnIsAssigned, Is.False);
                Assert.That(value.RepresentsPublishedWork, Is.False);
            });
            AssertTitle(value.Title);
            AssertAuthor(value.Author);
            AssertPublisher(value.Publisher);
            AssertSeries(value.Series);
            index++;
        }
    }

    [Test]
    public void ExtremeReferenceTimesProduceRepresentablePublicationDates()
    {
        FakeOptions minimumOptions = FakeOptions.Create(12505);
        minimumOptions.ReferenceTime = DateTimeOffset.MinValue;
        FakeOptions maximumOptions = FakeOptions.Create(12505);
        maximumOptions.ReferenceTime = DateTimeOffset.MaxValue;

        DateTimeOffset minimumDate = Fake.Create(minimumOptions).Book()
            .PublicationDate();
        BookRecord minimumRecord = Fake.Create(minimumOptions).Book().Record();
        DateTimeOffset maximumDate = Fake.Create(maximumOptions).Book()
            .PublicationDate();
        BookRecord maximumRecord = Fake.Create(maximumOptions).Book().Record();

        Assert.Multiple(() =>
        {
            Assert.That(minimumDate, Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(minimumRecord.PublicationDate,
                Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(maximumDate,
                Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
            Assert.That(maximumRecord.PublicationDate,
                Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
        });
    }

    [Test]
    public void LimitsFailBeforeBookEntropy()
    {
        const long seed = 12506;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        BookProvider tinyInspected = Fake.Create(tinyOptions).Book();
        BookProvider tinyUntouched = Fake.Create(tinyOptions).Book();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 4;
        BookProvider countInspected = Fake.Create(countOptions).Book();
        BookProvider countUntouched = Fake.Create(countOptions).Book();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.Title(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Author(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Publisher(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Series(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Genre(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Format(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Isbn(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Title(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Author(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Publisher(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Series(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Genre(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Format(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Isbn(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void BookGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12507);
        Fake untouched = Fake.Create(12507);

        int index = 0;
        while (index < 32)
        {
            inspected.Book().Title();
            inspected.Book().Author();
            inspected.Book().Publisher();
            inspected.Book().Series();
            inspected.Book().Genre();
            inspected.Book().Format();
            inspected.Book().Isbn();
            inspected.Book().PublicationDate();
            inspected.Book().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertTitle(string value)
    {
        string[] parts = value.Split(' ');
        Assert.Multiple(() =>
        {
            Assert.That(parts, Has.Length.EqualTo(3));
            Assert.That(parts[0], Is.EqualTo("The"));
            Assert.That(parts[1], Is.AnyOf(TitleAdjectives));
            Assert.That(parts[2], Is.AnyOf(TitleSubjects));
        });
    }

    static void AssertAuthor(string value)
    {
        string[] parts = value.Split(' ');
        Assert.Multiple(() =>
        {
            Assert.That(parts, Has.Length.EqualTo(2));
            Assert.That(parts[0], Is.AnyOf(AuthorGivenNames));
            Assert.That(parts[1], Is.AnyOf(AuthorSurnames));
        });
    }

    static void AssertPublisher(string value)
    {
        int separator = value.LastIndexOf(' ');
        Assert.Multiple(() =>
        {
            Assert.That(separator, Is.GreaterThan(0));
            Assert.That(value.Substring(0, separator),
                Is.AnyOf(PublisherStems));
            Assert.That(value.Substring(separator + 1),
                Is.AnyOf(PublisherKinds));
        });
    }

    static void AssertSeries(string value)
    {
        string[] parts = value.Split(' ');
        Assert.Multiple(() =>
        {
            Assert.That(parts, Has.Length.EqualTo(3));
            Assert.That(parts[0], Is.EqualTo("The"));
            Assert.That(parts[1], Is.AnyOf(TitleSubjects));
            Assert.That(parts[2], Is.AnyOf(SeriesLabels));
        });
    }

    static void AssertRecordEqual(BookRecord first, BookRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.Title, Is.EqualTo(second.Title));
            Assert.That(first.Author, Is.EqualTo(second.Author));
            Assert.That(first.Publisher, Is.EqualTo(second.Publisher));
            Assert.That(first.Series, Is.EqualTo(second.Series));
            Assert.That(first.SeriesNumber, Is.EqualTo(second.SeriesNumber));
            Assert.That(first.Genre, Is.EqualTo(second.Genre));
            Assert.That(first.Format, Is.EqualTo(second.Format));
            Assert.That(first.Isbn, Is.EqualTo(second.Isbn));
            Assert.That(first.PublicationDate,
                Is.EqualTo(second.PublicationDate));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.IsbnIsAssigned,
                Is.EqualTo(second.IsbnIsAssigned));
            Assert.That(first.RepresentsPublishedWork,
                Is.EqualTo(second.RepresentsPublishedWork));
        });
    }
}
