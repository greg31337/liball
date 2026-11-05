using System.Text;

namespace http.Tests;

public class PriorityCodecTests
{
    [Test]
    public void ParserAppliesRequestDefaultsWhenKnownParametersAreAbsent()
    {
        HttpPriority priority = FieldValueParser.ParsePriority(Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(priority.HasUrgency, Is.False);
            Assert.That(priority.Urgency, Is.EqualTo(3));
            Assert.That(priority.HasIncremental, Is.False);
            Assert.That(priority.Incremental, Is.False);
            Assert.That(priority.StructuredValue, Is.Not.Null);
            Assert.That(priority.StructuredValue!.IsDictionary, Is.True);
            Assert.That(priority.UnknownParameters, Is.Empty);
        });
    }

    [Test]
    public void ParserRecognizesUrgencyAndIncrementalValues()
    {
        HttpPriority enabled = FieldValueParser.ParsePriority("u=0, i"u8.ToArray());
        HttpPriority disabled = FieldValueParser.ParsePriority("u=7, i=?0"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(enabled.HasUrgency, Is.True);
            Assert.That(enabled.Urgency, Is.Zero);
            Assert.That(enabled.HasIncremental, Is.True);
            Assert.That(enabled.Incremental, Is.True);
            Assert.That(disabled.HasUrgency, Is.True);
            Assert.That(disabled.Urgency, Is.EqualTo(7));
            Assert.That(disabled.HasIncremental, Is.True);
            Assert.That(disabled.Incremental, Is.False);
        });
    }

    [Test]
    public void ParserIgnoresInvalidKnownValuesAndUsesLastDictionaryMember()
    {
        HttpPriority invalid = FieldValueParser.ParsePriority(
            "u=(1), i=true, extension=ok"u8.ToArray());
        HttpPriority belowRange = FieldValueParser.ParsePriority("u=-1"u8.ToArray());
        HttpPriority aboveRange = FieldValueParser.ParsePriority("u=8"u8.ToArray());
        HttpPriority duplicate = FieldValueParser.ParsePriority("u=1, u=6, i=?0, i"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(invalid.HasUrgency, Is.False);
            Assert.That(invalid.Urgency, Is.EqualTo(3));
            Assert.That(invalid.HasIncremental, Is.False);
            Assert.That(invalid.UnknownParameters, Has.Count.EqualTo(1));
            Assert.That(invalid.UnknownParameters[0].DictionaryKey, Is.EqualTo("extension"));
            Assert.That(belowRange.HasUrgency, Is.False);
            Assert.That(belowRange.Urgency, Is.EqualTo(3));
            Assert.That(aboveRange.HasUrgency, Is.False);
            Assert.That(aboveRange.Urgency, Is.EqualTo(3));
            Assert.That(duplicate.Urgency, Is.EqualTo(6));
            Assert.That(duplicate.Incremental, Is.True);
        });
    }

    [Test]
    public void UnknownParametersRetainStructuredValuesAndOrder()
    {
        HttpPriority priority = FieldValueParser.ParsePriority(
            "colour=red;level=2, group=(1 \"two\");active, flag=?0"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(priority.UnknownParameters, Has.Count.EqualTo(3));
            Assert.That(priority.UnknownParameters[0].DictionaryKey, Is.EqualTo("colour"));
            Assert.That(priority.UnknownParameters[0].TextValue, Is.EqualTo("red"));
            Assert.That(priority.UnknownParameters[0].Parameters, Has.Count.EqualTo(1));
            Assert.That(priority.UnknownParameters[1].IsInnerList, Is.True);
            Assert.That(priority.UnknownParameters[1].InnerList, Has.Count.EqualTo(2));
            Assert.That(priority.UnknownParameters[2].BooleanValue, Is.False);
        });

        Assert.That(
            Encoding.ASCII.GetString(FieldValueWriter.SerializePriority(priority)),
            Is.EqualTo("colour=red;level=2, group=(1 \"two\");active, flag=?0"));
    }

    [Test]
    public void SerializerProducesCanonicalKnownParametersBeforeExtensions()
    {
        HttpPriority priority = new HttpPriority();
        priority.HasUrgency = true;
        priority.Urgency = 3;
        priority.HasIncremental = true;
        priority.Incremental = true;
        priority.UnknownParameters.Add(
            FieldValueParser.ParsePriority("custom=\"value\""u8.ToArray()).UnknownParameters[0]);

        byte[] serialized = FieldValueWriter.SerializePriority(priority);
        HttpPriority reparsed = FieldValueParser.ParsePriority(serialized);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(serialized), Is.EqualTo("u=3, i, custom=\"value\""));
            Assert.That(reparsed.HasUrgency, Is.True);
            Assert.That(reparsed.Urgency, Is.EqualTo(3));
            Assert.That(reparsed.HasIncremental, Is.True);
            Assert.That(reparsed.Incremental, Is.True);
            Assert.That(reparsed.UnknownParameters, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SerializerOmitsImplicitDefaultsAndPreservesExplicitFalse()
    {
        HttpPriority defaults = new HttpPriority();
        HttpPriority explicitFalse = new HttpPriority();
        explicitFalse.HasIncremental = true;

        Assert.Multiple(() =>
        {
            Assert.That(FieldValueWriter.SerializePriority(defaults), Is.Empty);
            Assert.That(
                Encoding.ASCII.GetString(FieldValueWriter.SerializePriority(explicitFalse)),
                Is.EqualTo("i=?0"));
        });
    }

    [Test]
    public void SerializerRejectsInconsistentOrInvalidPriorityState()
    {
        HttpPriority implicitUrgencyChanged = new HttpPriority();
        implicitUrgencyChanged.Urgency = 2;
        HttpPriority outOfRange = new HttpPriority();
        outOfRange.HasUrgency = true;
        outOfRange.Urgency = 8;
        HttpPriority implicitIncrementalChanged = new HttpPriority();
        implicitIncrementalChanged.Incremental = true;
        HttpPriority reservedUnknown = new HttpPriority();
        reservedUnknown.UnknownParameters.Add(
            FieldValueParser.ParsePriority("u=1"u8.ToArray()).StructuredValue!.Members[0]);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpFieldException>(() =>
                FieldValueWriter.SerializePriority(implicitUrgencyChanged));
            Assert.Throws<HttpFieldException>(() => FieldValueWriter.SerializePriority(outOfRange));
            Assert.Throws<HttpFieldException>(() =>
                FieldValueWriter.SerializePriority(implicitIncrementalChanged));
            Assert.Throws<HttpFieldException>(() =>
                FieldValueWriter.SerializePriority(reservedUnknown));
        });
    }

    [Test]
    public void ParserRejectsMalformedStructuredDictionaries()
    {
        string[] malformed = ["u=", "U=1", "u=1,", "x=??"];

        Assert.Multiple(() =>
        {
            foreach (string value in malformed)
            {
                Assert.Throws<HttpParseException>(() =>
                    FieldValueParser.ParsePriority(Encoding.ASCII.GetBytes(value)), value);
            }
        });
    }

    [Test]
    public void ServerMergeOverridesOnlyParametersItExplicitlySupplies()
    {
        HttpPriority client = FieldValueParser.ParsePriority(
            "u=5, i, shared=client, client-only=1"u8.ToArray());
        HttpPriority server = FieldValueParser.ParsePriority(
            "u=1, shared=server, server-only=?0"u8.ToArray());

        HttpPriority merged = PriorityCodec.MergeClientAndServer(client, server);

        Assert.Multiple(() =>
        {
            Assert.That(merged.HasUrgency, Is.True);
            Assert.That(merged.Urgency, Is.EqualTo(1));
            Assert.That(merged.HasIncremental, Is.True);
            Assert.That(merged.Incremental, Is.True);
            Assert.That(merged.StructuredValue, Is.Null);
            Assert.That(merged.UnknownParameters, Has.Count.EqualTo(3));
            Assert.That(merged.UnknownParameters[0].DictionaryKey, Is.EqualTo("shared"));
            Assert.That(merged.UnknownParameters[0].TextValue, Is.EqualTo("server"));
            Assert.That(merged.UnknownParameters[1].DictionaryKey, Is.EqualTo("client-only"));
            Assert.That(merged.UnknownParameters[2].DictionaryKey, Is.EqualTo("server-only"));
        });
    }

    [Test]
    public void EmptyServerSignalLeavesClientValuesAndRequestDefaultsUnchanged()
    {
        HttpPriority client = FieldValueParser.ParsePriority("u=7, i=?0"u8.ToArray());
        HttpPriority empty = FieldValueParser.ParsePriority(Array.Empty<byte>());
        HttpPriority defaults = PriorityCodec.MergeClientAndServer(
            new HttpPriority(),
            new HttpPriority());

        HttpPriority preserved = PriorityCodec.MergeClientAndServer(client, empty);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.HasUrgency, Is.True);
            Assert.That(preserved.Urgency, Is.EqualTo(7));
            Assert.That(preserved.HasIncremental, Is.True);
            Assert.That(preserved.Incremental, Is.False);
            Assert.That(defaults.HasUrgency, Is.False);
            Assert.That(defaults.Urgency, Is.EqualTo(3));
            Assert.That(defaults.HasIncremental, Is.False);
            Assert.That(defaults.Incremental, Is.False);
        });
    }

    [Test]
    public void ExplicitServerFalseOverridesClientIncrementalPreference()
    {
        HttpPriority client = FieldValueParser.ParsePriority("i"u8.ToArray());
        HttpPriority server = FieldValueParser.ParsePriority("i=?0"u8.ToArray());

        HttpPriority merged = PriorityCodec.MergeClientAndServer(client, server);

        Assert.Multiple(() =>
        {
            Assert.That(merged.HasIncremental, Is.True);
            Assert.That(merged.Incremental, Is.False);
            Assert.That(
                Encoding.ASCII.GetString(FieldValueWriter.SerializePriority(merged)),
                Is.EqualTo("i=?0"));
        });
    }
}
