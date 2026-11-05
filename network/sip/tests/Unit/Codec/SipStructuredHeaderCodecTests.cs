namespace sip.Tests;

public class SipStructuredHeaderCodecTests
{
    [Test]
    public void ParsesRouteAndRecordRouteLists()
    {
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult route = Parse(codec,
            "Route: <sip:Proxy1.EXAMPLE.com;lr>, \"Edge\" <sips:Proxy2.EXAMPLE.com>;foo=bar",
            new SipParseOptions());
        SipHeaderParseResult recordRoute = Parse(codec,
            "Record-Route: <sip:recorder.example.com;transport=tcp>", new SipParseOptions());

        Assert.That(route.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(route.RouteValues, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(route.RouteValues[0].LooseRouting, Is.True);
            Assert.That(route.RouteValues[0].Address!.Uri!.Host, Is.EqualTo("Proxy1.EXAMPLE.com"));
            Assert.That(route.RouteValues[1].LooseRouting, Is.False);
            Assert.That(route.RouteValues[1].Address!.DisplayName, Is.EqualTo("Edge"));
            Assert.That(route.RouteValues[1].Address!.Parameters[0].Name, Is.EqualTo("foo"));
            Assert.That(recordRoute.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(recordRoute.RouteValues, Has.Length.EqualTo(1));
            Assert.That(recordRoute.Header!.Name, Is.EqualTo("Record-Route"));
        });
    }

    [Test]
    public void CanonicallyFormatsRouteLists()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "route: <sip:Proxy1.EXAMPLE.com;lr>, \"Edge\" <sips:Proxy2.EXAMPLE.com>;foo=bar",
            new SipParseOptions());

        SipWriteResult result = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(result.Bytes), Is.EqualTo(
            "Route: <sip:proxy1.example.com;lr>, Edge <sips:proxy2.example.com>;foo=bar\r\n"));
    }

    [Test]
    public void RouteRequiresNameAddressAndHonorsListLimit()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions options = new SipParseOptions();
        options.MaxListValueCount = 1;

        SipHeaderParseResult bare = Parse(codec, "Route: sip:proxy.example.com;lr", new SipParseOptions());
        SipHeaderParseResult tooMany = Parse(codec,
            "Record-Route: <sip:one.example.com>, <sip:two.example.com>", options);

        Assert.That(bare.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(bare.ErrorMessage, Does.Contain("name-address"));
        Assert.That(tooMany.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooMany.ErrorMessage, Does.Contain("item count"));
    }

    [Test]
    public void ParsesWildcardAndCompactContactValues()
    {
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult wildcard = Parse(codec, "Contact: *", new SipParseOptions());
        SipHeaderParseResult values = Parse(codec,
            "m: \"Alice\" <sip:alice@EXAMPLE.com>;q=0.750;expires=00300;foo=\"bar baz\", "
                + "<sips:bob@[2001:DB8::1]>;q=1", new SipParseOptions());

        Assert.That(wildcard.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(wildcard.ContactValues, Has.Length.EqualTo(1));
        Assert.That(wildcard.ContactValues[0].Wildcard, Is.True);
        Assert.That(wildcard.ContactValues[0].Address, Is.Null);
        Assert.That(values.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(values.ContactValues, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(values.ContactValues[0].QualityThousandths, Is.EqualTo(750));
            Assert.That(values.ContactValues[0].ExpiresSeconds, Is.EqualTo(300));
            Assert.That(values.ContactValues[0].Address!.Parameters[2].Value, Is.EqualTo("bar baz"));
            Assert.That(values.ContactValues[1].QualityThousandths, Is.EqualTo(1000));
            Assert.That(values.ContactValues[1].ExpiresSeconds, Is.EqualTo(-1));
            Assert.That(values.ContactValues[1].Address!.Uri!.Host, Is.EqualTo("2001:DB8::1"));
        });
    }

    [Test]
    public void BareContactSeparatesHeaderParametersFromTheUri()
    {
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult result = Parse(codec,
            "Contact: sip:alice@example.com;q=0.5;expires=60", new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.ContactValues[0].Address!.Uri!.Parameters, Is.Empty);
        Assert.That(result.ContactValues[0].Address!.Parameters.Select(parameter => parameter.Name),
            Is.EqualTo(new string[] { "q", "expires" }));
        Assert.That(result.ContactValues[0].QualityThousandths, Is.EqualTo(500));
        Assert.That(result.ContactValues[0].ExpiresSeconds, Is.EqualTo(60));
    }

    [Test]
    public void CanonicallyFormatsContactValuesAndCompactName()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Contact: \"Alice\" <sip:alice@EXAMPLE.com>;q=0.750;expires=00300;foo=\"bar baz\"",
            new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult result = codec.FormatHeader(parsed.Header!, options);

        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(result.Bytes), Is.EqualTo(
            "m: Alice <sip:alice@example.com>;q=0.75;expires=300;foo=\"bar baz\"\r\n"));
    }

    [Test]
    public void FormatsWildcardContact()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec, "m: *", new SipParseOptions());

        SipWriteResult result = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes), Is.EqualTo("Contact: *\r\n"));
    }

    [TestCase("Contact: *, <sip:alice@example.com>")]
    [TestCase("Contact: *;expires=0")]
    [TestCase("Contact:")]
    [TestCase("Contact: <sip:alice@example.com>;q")]
    [TestCase("Contact: <sip:alice@example.com>;q=\"0.5\"")]
    [TestCase("Contact: <sip:alice@example.com>;q=1.001")]
    [TestCase("Contact: <sip:alice@example.com>;q=0.5;Q=0.6")]
    [TestCase("Contact: <sip:alice@example.com>;expires")]
    [TestCase("Contact: <sip:alice@example.com>;expires=\"60\"")]
    [TestCase("Contact: <sip:alice@example.com>;expires=-1")]
    [TestCase("Contact: <sip:alice@example.com>;expires=2147483648")]
    [TestCase("Contact: <sip:alice@example.com>;expires=1;Expires=2")]
    public void RejectsInvalidContactValues(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ContactValues, Is.Empty);
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    [Test]
    public void ContactHonorsListAndParameterLimits()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions listOptions = new SipParseOptions();
        listOptions.MaxListValueCount = 1;
        SipParseOptions parameterOptions = new SipParseOptions();
        parameterOptions.MaxParameterCount = 1;

        SipHeaderParseResult tooManyValues = Parse(codec,
            "Contact: <sip:one@example.com>, <sip:two@example.com>", listOptions);
        SipHeaderParseResult tooManyParameters = Parse(codec,
            "Contact: <sip:one@example.com>;q=0.5;expires=60", parameterOptions);

        Assert.That(tooManyValues.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyValues.ErrorMessage, Does.Contain("item count"));
        Assert.That(tooManyParameters.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyParameters.ErrorMessage, Does.Contain("parameter count"));
    }

    [Test]
    public void ParsesFromAndToIncludingCompactAndBareForms()
    {
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult from = Parse(codec,
            "f: \"Alice A.\" <sip:alice@EXAMPLE.com>;tag=a1;foo=bar", new SipParseOptions());
        SipHeaderParseResult to = Parse(codec,
            "t: sip:bob@example.com;tag=b2", new SipParseOptions());

        Assert.That(from.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(from.NameAddress!.DisplayName, Is.EqualTo("Alice A."));
        Assert.That(from.NameAddress.Uri!.Host, Is.EqualTo("EXAMPLE.com"));
        Assert.That(from.NameAddress.Parameters.Select(parameter => parameter.Name),
            Is.EqualTo(new string[] { "tag", "foo" }));
        Assert.That(to.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(to.NameAddress!.Uri!.OriginalText, Is.EqualTo("sip:bob@example.com"));
        Assert.That(to.NameAddress.Uri.Parameters, Is.Empty);
        Assert.That(to.NameAddress.Parameters[0].Value, Is.EqualTo("b2"));
    }

    [Test]
    public void CanonicallyFormatsFromAndToWithCompactNames()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult from = Parse(codec,
            "From: \"Alice A.\" <sip:alice@EXAMPLE.com>;tag=a1", new SipParseOptions());
        SipHeaderParseResult to = Parse(codec,
            "To: sip:bob@EXAMPLE.com;tag=b2", new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult formattedFrom = codec.FormatHeader(from.Header!, options);
        SipWriteResult formattedTo = codec.FormatHeader(to.Header!, options);

        Assert.That(System.Text.Encoding.UTF8.GetString(formattedFrom.Bytes),
            Is.EqualTo("f: \"Alice A.\" <sip:alice@example.com>;tag=a1\r\n"));
        Assert.That(System.Text.Encoding.UTF8.GetString(formattedTo.Bytes),
            Is.EqualTo("t: <sip:bob@example.com>;tag=b2\r\n"));
    }

    [TestCase("From: <sip:alice@example.com>, <sip:bob@example.com>")]
    [TestCase("To: <sip:bob@example.com>;tag=one;Tag=two")]
    [TestCase("From: <sip:alice@example.com>;tag")]
    [TestCase("To: <sip:bob@example.com>;tag=\"quoted\"")]
    [TestCase("From: sip:alice;day=tuesday@example.com")]
    public void RejectsInvalidFromAndToValues(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.NameAddress, Is.Null);
    }

    [Test]
    public void ParsesAndFormatsCallIdIncludingCompactName()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "i: A84b4c76/66710?x@PC33.Atlanta.com", new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult result = codec.FormatHeader(parsed.Header!, options);

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.CallId, Is.EqualTo("A84b4c76/66710?x@PC33.Atlanta.com"));
        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes),
            Is.EqualTo("i: A84b4c76/66710?x@PC33.Atlanta.com\r\n"));
    }

    [TestCase("Call-ID:")]
    [TestCase("Call-ID: local id@example.com")]
    [TestCase("Call-ID: @example.com")]
    [TestCase("Call-ID: local@")]
    [TestCase("Call-ID: local@example@com")]
    [TestCase("Call-ID: local,other")]
    public void RejectsInvalidCallIds(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.CallId, Is.Null);
    }

    [Test]
    public void ParsesKnownAndExtensionCSeqMethodsAndFormatsNumber()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult known = Parse(codec, "CSeq: 00042 INVITE", new SipParseOptions());
        SipHeaderParseResult extension = Parse(codec, "CSeq: 43 X-CUSTOM", new SipParseOptions());
        SipHeaderParseResult lowerCase = Parse(codec, "CSeq: 44 invite", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(known.Header!, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(known.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(known.CSeq!.SequenceNumber, Is.EqualTo(42));
            Assert.That(known.CSeq.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(known.CSeq.MethodToken, Is.EqualTo("INVITE"));
            Assert.That(extension.CSeq!.Method, Is.EqualTo(SipMethod.Unknown));
            Assert.That(extension.CSeq.MethodToken, Is.EqualTo("X-CUSTOM"));
            Assert.That(lowerCase.CSeq!.Method, Is.EqualTo(SipMethod.Unknown));
            Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes), Is.EqualTo("CSeq: 42 INVITE\r\n"));
        });
    }

    [TestCase("CSeq:")]
    [TestCase("CSeq: 42")]
    [TestCase("CSeq: 42INVITE")]
    [TestCase("CSeq: -1 INVITE")]
    [TestCase("CSeq: 2147483648 INVITE")]
    [TestCase("CSeq: 42 INVITE X")]
    public void RejectsInvalidCSeqValues(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.CSeq, Is.Null);
    }

    [TestCase("Max-Forwards: 0", 0)]
    [TestCase("Max-Forwards: 070", 70)]
    [TestCase("Max-Forwards: 255", 255)]
    public void ParsesAndCanonicallyFormatsMaxForwards(string text, int expected)
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec, text, new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.MaxForwards, Is.EqualTo(expected));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("Max-Forwards: " + expected + "\r\n"));
    }

    [TestCase("Max-Forwards:")]
    [TestCase("Max-Forwards: 256")]
    [TestCase("Max-Forwards: -1")]
    [TestCase("Max-Forwards: +1")]
    [TestCase("Max-Forwards: seventy")]
    public void RejectsInvalidMaxForwardsValues(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.MaxForwards, Is.EqualTo(-1));
    }

    [Test]
    public void StructuredHeadersHonorPreservationAndOutputLimits()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.PreserveOriginal = false;
        SipHeaderParseResult parsed = Parse(codec,
            "From: \"Alice\" <sip:alice@example.com>;tag=a1;foo=bar", parseOptions);
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.MaxHeaderLineBytes = 10;

        SipWriteResult limited = codec.FormatHeader(parsed.Header!, writeOptions);

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.Header!.OriginalBytes, Is.Empty);
        Assert.That(parsed.NameAddress!.OriginalText, Is.Null);
        Assert.That(parsed.NameAddress.Uri!.OriginalText, Is.Null);
        Assert.That(parsed.NameAddress.Parameters.Select(parameter => parameter.OriginalText), Is.All.Null);
        Assert.That(limited.Success, Is.False);
        Assert.That(limited.RequiredBytes, Is.GreaterThan(10));
    }

    [Test]
    public void UnrelatedHeadersDoNotPopulateStructuredFields()
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), "Subject: hello", new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.NameAddress, Is.Null);
        Assert.That(result.RouteValues, Is.Empty);
        Assert.That(result.ContactValues, Is.Empty);
        Assert.That(result.CallId, Is.Null);
        Assert.That(result.CSeq, Is.Null);
        Assert.That(result.MaxForwards, Is.EqualTo(-1));
    }

    static SipHeaderParseResult Parse(SipHeaderCodec codec, string text, SipParseOptions options)
    {
        return codec.ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), options);
    }
}
