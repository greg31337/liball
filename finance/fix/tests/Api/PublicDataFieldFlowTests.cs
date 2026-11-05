using System;
using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class PublicDataFieldFlowTests
{
    [Test]
    public void DictionaryFieldsDriveDeterministicWritingAndPrefixedGeneration()
    {
        FixDictionary dictionary = DictionaryImplementationContractTests.CanonicalDictionary();
        dictionary.Datatypes.Add(new FixDatatypeDefinition { Name = "VenueText", BaseTypeName = "String", FieldType = FixFieldType.String });
        dictionary.Fields[0].DatatypeName = "VenueText";
        dictionary.CodeSets.Add(new FixCodeSetDefinition
        {
            Name = "StatusCodeSet",
            FieldType = FixFieldType.Char,
            Codes =
            [
                new FixCodeDefinition { Name = "Second", Value = "B", SortOrder = 2 },
                new FixCodeDefinition { Name = "First", Value = "A", SortOrder = 1 }
            ]
        });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 39, Name = "OrdStatus", CodeSetName = "StatusCodeSet", FieldType = FixFieldType.Char });
        dictionary.Messages[0].Members.Add(new FixMemberDefinition { Name = "OrdStatus", Kind = FixMemberKind.Field, ReferenceId = 39, Position = 1 });
        using MemoryStream xmlDestination = new MemoryStream();
        DictionaryLoadResult written = new QuickFixDictionaryWriter().WriteDictionary(xmlDestination, dictionary, new FixDictionaryOptions { PreserveSourceOrder = false });
        ModelGenerationOptions generationOptions = new ModelGenerationOptions { Namespace = "Venue.Generated", TypePrefix = "Venue" };
        GenerationResult generated = new CSharpModelGenerator().GenerateModels(dictionary, generationOptions);
        string xml = Encoding.UTF8.GetString(xmlDestination.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, string.Join(",", written.Diagnostics.ConvertAll(item => item.Code)));
            Assert.That(generated.Success, Is.True, string.Join(",", generated.Diagnostics.ConvertAll(item => item.Code)));
            Assert.That(xml.IndexOf("enum=\"A\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("enum=\"B\"", StringComparison.Ordinal)));
            Assert.That(dictionary.Datatypes.Single(item => item.Name == "VenueText").BaseTypeName, Is.EqualTo("String"));
            Assert.That(dictionary.CodeSets.Single().Codes.Select(item => item.SortOrder), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(generationOptions.TypePrefix, Is.EqualTo("Venue"));
            Assert.That(generated.Files.Select(item => item.Path), Does.Contain("Messages/VenueOrder.cs"));
        });
    }

    [Test]
    public void OrchestraRepositoryFieldsRoundTripThroughReaderAndWriter()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2020/orchestra/repository"
                             xmlns:venue="urn:venue:fields"
                             name="Venue" version="FIX.4.4">
              <fixr:metadata/>
              <fixr:datatypes>
                <fixr:datatype name="int"/>
                <fixr:datatype name="String"/>
                <fixr:datatype name="NumInGroup" baseType="int"/>
              </fixr:datatypes>
              <fixr:codeSets/>
              <fixr:fields>
                <fixr:field id="11" name="ClOrdID" type="String"/>
                <fixr:field id="453" name="NoPartyIDs" type="NumInGroup"/>
              </fixr:fields>
              <fixr:components>
                <fixr:component id="100" name="OrderCore" rendering="inline">
                  <fixr:fieldRef id="11">
                    <fixr:rule name="requiredId" presence="required"><fixr:when>exists(11)</fixr:when></fixr:rule>
                  </fixr:fieldRef>
                </fixr:component>
              </fixr:components>
              <fixr:groups>
                <fixr:group id="200" name="Parties" rendering="block">
                  <fixr:numInGroup id="453"/>
                  <fixr:fieldRef id="11"/>
                </fixr:group>
              </fixr:groups>
              <fixr:messages>
                <fixr:message id="1" name="Order" msgType="D">
                  <fixr:structure><fixr:componentRef id="100"/><fixr:groupRef id="200"/></fixr:structure>
                </fixr:message>
              </fixr:messages>
              <venue:settings mode="strict"/>
            </fixr:repository>
            """;
        OrchestraReadOptions options = new OrchestraReadOptions { ValidateSchema = false, BaseUri = "field-flow.xml" };
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        OrchestraReadResult read = new OrchestraReader().ReadRepository(source, options);
        string behaviorXml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2026/orchestra/repository" name="Behavior" version="FIX.Latest">
              <fixr:datatypes/><fixr:codeSets/><fixr:fields/>
              <fixr:messages>
                <fixr:message id="1" name="Order" msgType="D"><fixr:structure/><fixr:when>true</fixr:when></fixr:message>
              </fixr:messages>
            </fixr:repository>
            """;
        using MemoryStream behaviorSource = new MemoryStream(Encoding.UTF8.GetBytes(behaviorXml));
        OrchestraReadResult behavior = new OrchestraReader().ReadRepository(behaviorSource, new OrchestraReadOptions { ValidateSchema = false });

        Assert.That(read.Repository, Is.Not.Null);
        OrchestraRepository repository = read.Repository!;
        Assert.Multiple(() =>
        {
            Assert.That(read.Success, Is.True, string.Join(",", read.Diagnostics.ConvertAll(item => item.Code)));
            Assert.That(behavior.Success, Is.True, string.Join(",", behavior.Diagnostics.ConvertAll(item => item.Code)));
            Assert.That(options.BaseUri, Is.EqualTo("field-flow.xml"));
            Assert.That(repository.Components.Single().Rendering, Is.EqualTo("inline"));
            Assert.That(repository.Groups.Single().Rendering, Is.EqualTo("block"));
            Assert.That(behavior.Repository!.Rules.Single().SourceKind, Is.EqualTo("message"));
            Assert.That(repository.Extensions.Single().Prefix, Is.EqualTo("venue"));
        });
    }

    [Test]
    public void OrchestraInterfaceDeprecationFieldsSurviveXmlRoundTrip()
    {
        OrchestraService service = new OrchestraService { Name = "Orders", Layer = "application", DeprecatedTime = "2027-01-01T00:00:00Z" };
        OrchestraProtocol transport = new OrchestraProtocol
        {
            Kind = "transport",
            Name = "TCP",
            Layer = "transport",
            Address = "fix.example:9876",
            DeprecatedTime = "2028-01-01T00:00:00Z"
        };
        OrchestraRepository repository = new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Interfaces = [new OrchestraInterface { Name = "Trading", Services = [service], Transports = [transport] }]
        };
        using MemoryStream destination = new MemoryStream();
        OrchestraReadResult written = new OrchestraWriter().WriteRepository(destination, repository, new OrchestraWriteOptions());
        destination.Position = 0;
        OrchestraReadResult read = new OrchestraReader().ReadRepository(destination, new OrchestraReadOptions { ValidateSchema = false });

        Assert.That(read.Repository, Is.Not.Null);
        OrchestraInterface roundTripped = read.Repository!.Interfaces.Single();
        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, string.Join(",", written.Diagnostics.ConvertAll(item => item.Code)));
            Assert.That(read.Success, Is.True, string.Join(",", read.Diagnostics.ConvertAll(item => item.Code)));
            Assert.That(service.DeprecatedTime, Is.EqualTo("2027-01-01T00:00:00Z"));
            Assert.That(transport.DeprecatedTime, Is.EqualTo("2028-01-01T00:00:00Z"));
            Assert.That(roundTripped.Services.Single().DeprecatedTime, Is.EqualTo(service.DeprecatedTime));
            Assert.That(roundTripped.Transports.Single().DeprecatedTime, Is.EqualTo(transport.DeprecatedTime));
        });
    }

    [Test]
    public void SessionSettingsAndRuntimeSnapshotsConsumePreviouslyWriteOnlyFields()
    {
        string text = """
            [SESSION]
            Role=initiator
            BeginString=FIX.4.4
            SenderCompID=LOCAL
            TargetCompID=REMOTE
            Endpoint=127.0.0.1,9876
            TlsCertificatePassword=secret
            ResetMode=None
            """;
        using MemoryStream settingsSource = new MemoryStream(Encoding.UTF8.GetBytes(text));
        SessionSettings[] loaded = new TextSessionSettingsLoader().LoadSessionSettings(settingsSource);
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        SessionSnapshot snapshot = fixture.Session.GetSessionSnapshot();
        ApplicationMessageContext throttleContext = new ApplicationMessageContext { PendingQueueCount = 0 };
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(fixture.Clock);
        ApplicationDecision throttleDecision = throttle.CheckMessageThrottle(throttleContext, new SessionThrottleSettings());

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Has.Length.EqualTo(1));
            Assert.That(loaded[0].Tls.CertificatePassword, Is.EqualTo("secret"));
            Assert.That(loaded[0].ResetMode, Is.EqualTo(SessionResetMode.None));
            Assert.That(snapshot.TransportState, Is.EqualTo(TransportState.Connected));
            Assert.That(snapshot.HasLastInboundTime, Is.True);
            Assert.That(snapshot.LastInboundTimeUtc, Is.EqualTo(FixSessionTests.Now));
            Assert.That(snapshot.HasLastOutboundTime, Is.True);
            Assert.That(snapshot.LastOutboundTimeUtc, Is.EqualTo(FixSessionTests.Now));
            Assert.That(throttleDecision.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(throttleContext.PendingQueueCount, Is.Zero);
        });
    }

    [Test]
    public void ValidationContextExpectedBeginStringDrivesEnvelopeValidation()
    {
        FixMessage message = new FixMessage { BeginString = FixBeginStrings.Fix44, MsgType = "D" };
        ValidationContext context = new ValidationContext { ExpectedBeginString = FixBeginStrings.Fixt11 };
        ValidationResult result = new FixMessageValidator().ValidateMessage(
            message,
            new FixDictionary(),
            new ValidationOptions { Layers = ValidationLayer.SessionHeader },
            context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Exists(item => item.Code == "begin-string-mismatch"), Is.True);
            Assert.That(context.ExpectedBeginString, Is.EqualTo(FixBeginStrings.Fixt11));
        });
    }
}
