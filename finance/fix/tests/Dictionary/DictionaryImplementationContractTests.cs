using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class DictionaryImplementationContractTests
{
    [TestCase("quickfix")]
    [TestCase("orchestra")]
    public void LoadersShareDeterministicSuccessAndCallerStreamOwnership(string format)
    {
        IDictionaryLoader loader = CreateLoader(format);
        byte[] bytes = Encoding.UTF8.GetBytes(SourceXml(format));
        using MemoryStream firstSource = new MemoryStream(bytes);
        using MemoryStream secondSource = new MemoryStream(bytes);

        DictionaryLoadResult first = loader.LoadDictionary(firstSource, new FixDictionaryOptions());
        DictionaryLoadResult second = loader.LoadDictionary(secondSource, new FixDictionaryOptions());

        Assert.That(first.Dictionary, Is.Not.Null);
        Assert.That(second.Dictionary, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Codes(first));
            Assert.That(second.Success, Is.True, Codes(second));
            Assert.That(firstSource.CanRead, Is.True);
            Assert.That(secondSource.CanRead, Is.True);
            Assert.That(first.Dictionary!.Fields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 11 }));
            Assert.That(second.Dictionary!.Fields.ConvertAll(field => field.Tag), Is.EqualTo(first.Dictionary.Fields.ConvertAll(field => field.Tag)));
            Assert.That(second.Dictionary.Messages.ConvertAll(message => message.Name), Is.EqualTo(first.Dictionary.Messages.ConvertAll(message => message.Name)));
            Assert.That(second.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(first.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
        });
    }

    [TestCase("quickfix")]
    [TestCase("orchestra")]
    public void LoadersShareStructuredMalformedXmlFailures(string format)
    {
        IDictionaryLoader loader = CreateLoader(format);
        byte[] bytes = Encoding.UTF8.GetBytes("<broken");
        using MemoryStream firstSource = new MemoryStream(bytes);
        using MemoryStream secondSource = new MemoryStream(bytes);

        DictionaryLoadResult first = loader.LoadDictionary(firstSource, new FixDictionaryOptions());
        DictionaryLoadResult second = loader.LoadDictionary(secondSource, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Diagnostics, Is.Not.Empty);
            Assert.That(first.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(second.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(firstSource.CanRead, Is.True);
            Assert.That(secondSource.CanRead, Is.True);
        });
    }

    [Test]
    public void WriterAndLoaderShareAStableCallerOwnedRoundTrip()
    {
        FixDictionary dictionary = CanonicalDictionary();
        using MemoryStream destination = new MemoryStream();
        IDictionaryWriter writer = new QuickFixDictionaryWriter();

        DictionaryLoadResult written = writer.WriteDictionary(destination, dictionary, new FixDictionaryOptions());
        byte[] firstBytes = destination.ToArray();
        destination.Position = 0;
        DictionaryLoadResult loaded = new QuickFixDictionaryLoader().LoadDictionary(destination, new FixDictionaryOptions());
        using MemoryStream repeatDestination = new MemoryStream();
        DictionaryLoadResult repeated = writer.WriteDictionary(repeatDestination, dictionary, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, Codes(written));
            Assert.That(repeated.Success, Is.True, Codes(repeated));
            Assert.That(loaded.Success, Is.True, Codes(loaded));
            Assert.That(destination.CanRead, Is.True);
            Assert.That(destination.CanWrite, Is.True);
            Assert.That(firstBytes, Is.EqualTo(repeatDestination.ToArray()));
            Assert.That(written.Dictionary, Is.SameAs(dictionary));
            Assert.That(dictionary.Messages.Single().Name, Is.EqualTo("Order"));
            Assert.That(loaded.Dictionary!.Messages.Single().Name, Is.EqualTo("Order"));
        });
    }

    internal static FixDictionary CanonicalDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX44",
                BeginString = FixBeginStrings.Fix44,
                ApplicationVersion = FixApplicationVersions.Fix44
            },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }],
            Fields = [new FixFieldDefinition { Tag = 11, Name = "ClOrdID", DatatypeName = "String", FieldType = FixFieldType.String }],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "Order",
                    MsgType = "D",
                    Category = "",
                    Scenario = "base",
                    Members = [new FixMemberDefinition { Name = "ClOrdID", Kind = FixMemberKind.Field, ReferenceId = 11, Position = 0, Presence = FixPresence.Required }]
                }
            ]
        };
    }

    private static IDictionaryLoader CreateLoader(string format)
    {
        if (format == "quickfix") return new QuickFixDictionaryLoader();
        return new OrchestraDictionaryLoader();
    }

    private static string SourceXml(string format)
    {
        if (format == "quickfix")
        {
            return "<fix type='FIX' major='4' minor='4' servicepack='0'><header/><messages><message name='Order' msgtype='D' msgcat='app'><field name='ClOrdID' required='Y'/></message></messages><trailer/><components/><fields><field number='11' name='ClOrdID' type='STRING'/></fields></fix>";
        }
        return "<fixr:repository xmlns:fixr='http://fixprotocol.io/2020/orchestra/repository' name='Venue' version='FIX.4.4'><fixr:metadata/><fixr:datatypes><fixr:datatype name='String'/></fixr:datatypes><fixr:codeSets/><fixr:fields><fixr:field id='11' name='ClOrdID' type='String'/></fixr:fields><fixr:components/><fixr:groups/><fixr:messages><fixr:message id='1' name='Order' msgType='D' category='Orders'><fixr:structure><fixr:fieldRef id='11' presence='required'/></fixr:structure></fixr:message></fixr:messages></fixr:repository>";
    }

    private static string Codes(DictionaryLoadResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code));
    }
}
