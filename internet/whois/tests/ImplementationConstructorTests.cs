using whois;

namespace whois.Tests;

public class ImplementationConstructorTests
{
    [Test]
    public void EveryParameterlessImplementationConstructorCreatesItsContract()
    {
        IWhoisServerSelector selector = new IanaWhoisServerSelector();
        IWhoisEndpointPolicy allowAllPolicy = new WhoisAllowAllEndpointPolicy();
        IWhoisHostResolver resolver = new WhoisDnsResolver();
        IWhoisEndpointPolicy publicPolicy = new WhoisPublicEndpointPolicy();
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        IWhoisQueryCodec codec = new WhoisQueryCodec();
        IWhoisQueryFormatter formatter = new WhoisQueryFormatter();
        IWhoisReferralParser referralParser = new WhoisReferralParser();
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        IWhoisResponseParser parser = new WhoisResponseParser();
        IWhoisConnectionFactory connectionFactory = new WhoisTcpConnectionFactory();

        Assert.That(selector, Is.TypeOf<IanaWhoisServerSelector>());
        Assert.That(allowAllPolicy, Is.TypeOf<WhoisAllowAllEndpointPolicy>());
        Assert.That(resolver, Is.TypeOf<WhoisDnsResolver>());
        Assert.That(publicPolicy, Is.TypeOf<WhoisPublicEndpointPolicy>());
        Assert.That(classifier, Is.TypeOf<WhoisQueryClassifier>());
        Assert.That(codec, Is.TypeOf<WhoisQueryCodec>());
        Assert.That(formatter, Is.TypeOf<WhoisQueryFormatter>());
        Assert.That(referralParser, Is.TypeOf<WhoisReferralParser>());
        Assert.That(decoder, Is.TypeOf<WhoisResponseDecoder>());
        Assert.That(parser, Is.TypeOf<WhoisResponseParser>());
        Assert.That(connectionFactory, Is.TypeOf<WhoisTcpConnectionFactory>());
    }

    [Test]
    public void ClientConstructorAcceptsAllValidInjectedDependencies()
    {
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        IWhoisQueryFormatter formatter = new WhoisQueryFormatter();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();
        IWhoisTransport transport = new FakeTransport();
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        IWhoisResponseParser parser = new WhoisResponseParser();
        IWhoisReferralParser referralParser = new WhoisReferralParser();

        IWhoisClient client = new WhoisClient(classifier, formatter, selector, transport, decoder, parser, referralParser);

        Assert.That(client, Is.TypeOf<WhoisClient>());
    }

    [Test]
    public void ClientConstructorRejectsEveryNullDependency()
    {
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        IWhoisQueryFormatter formatter = new WhoisQueryFormatter();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();
        IWhoisTransport transport = new FakeTransport();
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        IWhoisResponseParser parser = new WhoisResponseParser();
        IWhoisReferralParser referralParser = new WhoisReferralParser();

        ArgumentNullException classifierError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisClient(null!, formatter, selector, transport, decoder, parser, referralParser))!;
        ArgumentNullException formatterError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisClient(classifier, null!, selector, transport, decoder, parser, referralParser))!;
        ArgumentNullException selectorError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisClient(classifier, formatter, null!, transport, decoder, parser, referralParser))!;
        ArgumentNullException transportError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisClient(classifier, formatter, selector, null!, decoder, parser, referralParser))!;
        ArgumentNullException decoderError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisClient(classifier, formatter, selector, transport, null!, parser, referralParser))!;
        ArgumentNullException parserError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisClient(classifier, formatter, selector, transport, decoder, null!, referralParser))!;
        ArgumentNullException referralsError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisClient(classifier, formatter, selector, transport, decoder, parser, null!))!;

        Assert.That(classifierError.ParamName, Is.EqualTo("classifier"));
        Assert.That(formatterError.ParamName, Is.EqualTo("formatter"));
        Assert.That(selectorError.ParamName, Is.EqualTo("selector"));
        Assert.That(transportError.ParamName, Is.EqualTo("exchangeTransport"));
        Assert.That(decoderError.ParamName, Is.EqualTo("decoder"));
        Assert.That(parserError.ParamName, Is.EqualTo("parser"));
        Assert.That(referralsError.ParamName, Is.EqualTo("referrals"));
    }

    [TestCase(WhoisAddressPreference.System)]
    [TestCase(WhoisAddressPreference.IPv4First)]
    [TestCase(WhoisAddressPreference.IPv6First)]
    public void TransportConstructorAcceptsEveryValidAddressPreference(WhoisAddressPreference preference)
    {
        IWhoisQueryCodec codec = new WhoisQueryCodec();
        IWhoisHostResolver resolver = new FakeHostResolver();
        IWhoisEndpointPolicy directPolicy = new FakeEndpointPolicy();
        IWhoisEndpointPolicy automaticPolicy = new FakeEndpointPolicy();
        IWhoisConnectionFactory factory = new FakeConnectionFactory();

        IWhoisTransport transport = new WhoisTcpTransport(codec, resolver, directPolicy, automaticPolicy, factory, preference);

        Assert.That(transport, Is.TypeOf<WhoisTcpTransport>());
    }

    [Test]
    public void TransportConstructorRejectsEveryNullDependency()
    {
        IWhoisQueryCodec codec = new WhoisQueryCodec();
        IWhoisHostResolver resolver = new FakeHostResolver();
        IWhoisEndpointPolicy directPolicy = new FakeEndpointPolicy();
        IWhoisEndpointPolicy automaticPolicy = new FakeEndpointPolicy();
        IWhoisConnectionFactory factory = new FakeConnectionFactory();

        ArgumentNullException codecError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisTcpTransport(null!, resolver, directPolicy, automaticPolicy, factory, WhoisAddressPreference.System))!;
        ArgumentNullException resolverError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisTcpTransport(codec, null!, directPolicy, automaticPolicy, factory, WhoisAddressPreference.System))!;
        ArgumentNullException directPolicyError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisTcpTransport(codec, resolver, null!, automaticPolicy, factory, WhoisAddressPreference.System))!;
        ArgumentNullException automaticPolicyError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisTcpTransport(codec, resolver, directPolicy, null!, factory, WhoisAddressPreference.System))!;
        ArgumentNullException factoryError = Assert.Throws<ArgumentNullException>(() =>
            new WhoisTcpTransport(codec, resolver, directPolicy, automaticPolicy, null!, WhoisAddressPreference.System))!;

        Assert.That(codecError.ParamName, Is.EqualTo("codec"));
        Assert.That(resolverError.ParamName, Is.EqualTo("resolver"));
        Assert.That(directPolicyError.ParamName, Is.EqualTo("directPolicy"));
        Assert.That(automaticPolicyError.ParamName, Is.EqualTo("automaticPolicy"));
        Assert.That(factoryError.ParamName, Is.EqualTo("factory"));
    }

    [TestCase(-1)]
    [TestCase(3)]
    [TestCase(99)]
    public void TransportConstructorRejectsUndefinedAddressPreference(int preferenceValue)
    {
        IWhoisQueryCodec codec = new WhoisQueryCodec();
        IWhoisHostResolver resolver = new FakeHostResolver();
        IWhoisEndpointPolicy directPolicy = new FakeEndpointPolicy();
        IWhoisEndpointPolicy automaticPolicy = new FakeEndpointPolicy();
        IWhoisConnectionFactory factory = new FakeConnectionFactory();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WhoisTcpTransport(codec, resolver, directPolicy, automaticPolicy, factory, (WhoisAddressPreference)preferenceValue))!;

        Assert.That(exception.ParamName, Is.EqualTo("preference"));
    }
}
