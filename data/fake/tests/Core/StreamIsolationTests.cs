using fake;

namespace fake.Tests;

[TestFixture]
public class StreamIsolationTests
{
    [Test]
    public void EqualNamesAndSeedsProduceEqualIndependentStreams()
    {
        FakeRandom parent = Fake.Create(42).Random();

        FakeRandom first = parent.Fork("orders");
        FakeRandom second = parent.Fork("orders");
        ulong[] expected = Take(first, 20);

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(Take(second, 20), Is.EqualTo(expected));
    }

    [Test]
    public void ForkDoesNotConsumeParentAndIgnoresParentPosition()
    {
        FakeRandom parent = Fake.Create(73).Random();
        FakeRandom expectedParent = Fake.Create(73).Random();
        FakeRandom freshParent = Fake.Create(73).Random();
        Take(parent, 11);
        Take(expectedParent, 11);

        FakeRandom child = parent.Fork("invoices");

        Assert.That(Take(parent, 16), Is.EqualTo(Take(expectedParent, 16)));
        Assert.That(Take(child, 16), Is.EqualTo(Take(freshParent.Fork("invoices"), 16)));
    }

    [Test]
    public void DifferentNamesProduceDifferentStreams()
    {
        FakeRandom parent = Fake.Create(42).Random();

        ulong first = parent.Fork("orders").ULong();
        ulong second = parent.Fork("customers").ULong();

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void HierarchicalForksAreStableAndPathSensitive()
    {
        FakeRandom first = Fake.Create(42).Random().Fork("tenant-a").Fork("orders");
        FakeRandom second = Fake.Create(42).Random().Fork("tenant-a").Fork("orders");
        FakeRandom reversed = Fake.Create(42).Random().Fork("orders").Fork("tenant-a");

        ulong[] expected = Take(first, 16);

        Assert.That(Take(second, 16), Is.EqualTo(expected));
        Assert.That(Take(reversed, 16), Is.Not.EqualTo(expected));
    }

    [Test]
    public void RandomForkUsesCurrentResetSeed()
    {
        FakeRandom first = Fake.Create(1).Random();
        FakeRandom second = Fake.Create(999).Random();
        first.ReseedFromSeedUsingString("shared-parent");
        second.ReseedFromSeedUsingString("shared-parent");
        Take(first, 9);

        Assert.That(Take(first.Fork("child"), 16), Is.EqualTo(Take(second.Fork("child"), 16)));
    }

    [Test]
    public void RootForkPreservesConfigurationAndProducesStableChildren()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", "en-CA", 91);
        options.ReferenceTime = new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.FromHours(-4));
        options.GenerationMode = GenerationMode.Boundary;
        Fake parent = Fake.Create(options);

        Fake first = parent.Fork("tenant-a");
        Fake second = parent.Fork("tenant-a");
        FakeOptions captured = first.OptionsSnapshot();

        Assert.That(captured.Locale, Is.EqualTo("fr-CA"));
        Assert.That(captured.FallbackLocale, Is.EqualTo("en-CA"));
        Assert.That(captured.ReferenceTime, Is.EqualTo(options.ReferenceTime));
        Assert.That(captured.GenerationMode, Is.EqualTo(GenerationMode.Boundary));
        Assert.That(Take(first.Random(), 20), Is.EqualTo(Take(second.Random(), 20)));
    }

    [Test]
    public void RootForkMatchesRandomForkAndDoesNotConsumeParent()
    {
        Fake parent = Fake.Create(2048);
        FakeRandom expectedParent = Fake.Create(2048).Random();

        Fake child = parent.Fork("reports");
        FakeRandom randomChild = expectedParent.Fork("reports");

        Assert.That(Take(child.Random(), 16), Is.EqualTo(Take(randomChild, 16)));
        Assert.That(Take(parent.Random(), 16), Is.EqualTo(Take(Fake.Create(2048).Random(), 16)));
    }

    [Test]
    public void ProviderResultsAreIndependentOfProviderCallOrder()
    {
        Fake first = Fake.Create(8080);
        Fake second = Fake.Create(8080);

        ulong[] firstPeople = Take(first.Person().RandomValue, 18);
        ulong[] firstInternet = Take(first.Internet().RandomValue, 18);
        ulong[] secondInternet = Take(second.Internet().RandomValue, 18);
        ulong[] secondPeople = Take(second.Person().RandomValue, 18);

        Assert.That(secondPeople, Is.EqualTo(firstPeople));
        Assert.That(secondInternet, Is.EqualTo(firstInternet));
    }

    [Test]
    public void UnrelatedProviderActivityDoesNotPerturbAnotherProvider()
    {
        Fake first = Fake.Create(-77);
        Fake second = Fake.Create(-77);
        Take(first.Person().RandomValue, 40);
        Take(first.Date().RandomValue, 12);

        Assert.That(Take(first.Address().RandomValue, 24), Is.EqualTo(Take(second.Address().RandomValue, 24)));
    }

    [Test]
    public void EveryProviderOwnsADistinctStream()
    {
        FakeRandom[] streams = AllProviderStreams(Fake.Create(42));
        HashSet<ulong> seeds = new HashSet<ulong>();

        foreach (FakeRandom stream in streams)
        {
            Assert.That(stream, Is.Not.Null);
            seeds.Add(stream.Snapshot().InitialSeed);
        }

        Assert.That(seeds.Count, Is.EqualTo(streams.Length));
    }

    [Test]
    public void ProviderAndPublicChildNamesUseSeparateDomains()
    {
        Fake value = Fake.Create(42);

        ulong providerValue = value.Person().RandomValue.ULong();
        ulong publicValue = value.Random().Fork("person").ULong();

        Assert.That(publicValue, Is.Not.EqualTo(providerValue));
    }

    [Test]
    public void NamedStreamGoldenSequencesAreStable()
    {
        ulong[] childExpected = new ulong[]
        {
            17143376261512326701UL,
            11286852023588824552UL,
            3979022379669514520UL,
            1291597870772618524UL,
            16525584564794153175UL,
            5080746396524392723UL
        };
        ulong[] providerExpected = new ulong[]
        {
            6815350964714033237UL,
            9464082615999041385UL,
            6667674114021759219UL,
            14156007534272242285UL,
            4237287411077751437UL,
            393173703808410904UL
        };
        Fake value = Fake.Create(42);

        Assert.That(Take(value.Random().Fork("orders"), 6), Is.EqualTo(childExpected));
        Assert.That(Take(value.Person().RandomValue, 6), Is.EqualTo(providerExpected));
    }

    [Test]
    public void ForkRejectsNullName()
    {
        Fake value = Fake.Create();

        ArgumentNullException randomException = Assert.Throws<ArgumentNullException>(() => value.Random().Fork(null));
        ArgumentNullException rootException = Assert.Throws<ArgumentNullException>(() => value.Fork(null));

        Assert.That(randomException.ParamName, Is.EqualTo("name"));
        Assert.That(rootException.ParamName, Is.EqualTo("name"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ForkRejectsEmptyOrWhitespaceName(string name)
    {
        Fake value = Fake.Create();

        ArgumentException randomException = Assert.Throws<ArgumentException>(() => value.Random().Fork(name));
        ArgumentException rootException = Assert.Throws<ArgumentException>(() => value.Fork(name));

        Assert.That(randomException.ParamName, Is.EqualTo("name"));
        Assert.That(rootException.ParamName, Is.EqualTo("name"));
    }

    static FakeRandom[] AllProviderStreams(Fake value)
    {
        return new FakeRandom[]
        {
            value.Primitive().RandomValue,
            value.Text().RandomValue,
            value.Checksum().RandomValue,
            value.Identifier().RandomValue,
            value.Date().RandomValue,
            value.Locale().RandomValue,
            value.Person().RandomValue,
            value.Address().RandomValue,
            value.Internet().RandomValue,
            value.Phone().RandomValue,
            value.Device().RandomValue,
            value.Finance().RandomValue,
            value.Company().RandomValue,
            value.Commerce().RandomValue,
            value.System().RandomValue,
            value.Database().RandomValue,
            value.Git().RandomValue,
            value.Color().RandomValue,
            value.Vehicle().RandomValue,
            value.Airline().RandomValue,
            value.Book().RandomValue,
            value.Media().RandomValue,
            value.Food().RandomValue,
            value.Animal().RandomValue,
            value.Science().RandomValue,
            value.Health().RandomValue,
            value.Education().RandomValue,
            value.Culture().RandomValue,
            value.Profile().RandomValue,
            value.Schema().RandomValue,
            value.Edge().RandomValue,
            value.Export().RandomValue
        };
    }

    static ulong[] Take(FakeRandom random, int count)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            values[index] = random.ULong();
            index = index + 1;
        }
        return values;
    }
}
