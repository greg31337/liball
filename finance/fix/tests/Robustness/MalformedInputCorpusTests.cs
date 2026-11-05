using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class MalformedInputCorpusTests
{
    [Test]
    [CancelAfter(10000)]
    public void TagValueDecoderAndFramerReturnDeterministicResultsForMutationsAndRandomPrefixes()
    {
        byte[] valid = BuildTagValueFrame("35=D\u000111=ORDER-1\u0001");
        List<byte[]> corpus = MutateBytes(valid, 32, 130);
        corpus.Add([]);
        corpus.Add("8=FIX.4.4\u00019=x\u000135=D\u0001"u8.ToArray());
        TagValueMessageFramer framer = new TagValueMessageFramer();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        FixDictionary dictionary = DecoderDictionary();
        Random chunks = new Random(130);

        for (int corpusIndex = 0; corpusIndex < corpus.Count; corpusIndex++)
        {
            byte[] input = corpus[corpusIndex];
            string firstFrame = FrameText(framer.ReadFrame(input, new FrameOptions()));
            string secondFrame = FrameText(framer.ReadFrame(input, new FrameOptions()));
            string firstDecode = DecodeText(decoder.DecodeMessage(input, dictionary, new DecodeOptions()));
            string secondDecode = DecodeText(decoder.DecodeMessage(input, dictionary, new DecodeOptions()));
            Assert.That(secondFrame, Is.EqualTo(firstFrame), "frame corpus " + corpusIndex);
            Assert.That(secondDecode, Is.EqualTo(firstDecode), "decode corpus " + corpusIndex);
            int received = 0;
            while (received < input.Length)
            {
                received = Math.Min(input.Length, received + chunks.Next(1, 8));
                ReadOnlySpan<byte> prefix = input.AsSpan(0, received);
                string firstPrefixFrame = FrameText(framer.ReadFrame(prefix, new FrameOptions()));
                string secondPrefixFrame = FrameText(framer.ReadFrame(prefix, new FrameOptions()));
                string firstPrefixDecode = DecodeText(decoder.DecodeMessage(prefix, dictionary, new DecodeOptions()));
                string secondPrefixDecode = DecodeText(decoder.DecodeMessage(prefix, dictionary, new DecodeOptions()));
                Assert.That(secondPrefixFrame, Is.EqualTo(firstPrefixFrame), "frame prefix " + corpusIndex + ":" + received);
                Assert.That(secondPrefixDecode, Is.EqualTo(firstPrefixDecode), "decode prefix " + corpusIndex + ":" + received);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(framer.ReadFrame(valid, new FrameOptions()).Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(decoder.DecodeMessage(valid, dictionary, new DecodeOptions()).Status, Is.EqualTo(DecodeStatus.Success));
        });
    }

    [Test]
    [CancelAfter(10000)]
    public void SofhFramerReturnsDeterministicResultsForMutationsAndRandomPrefixes()
    {
        byte[] valid = BuildSofhFrame([1, 2, 3, 4, 5]);
        List<byte[]> corpus = MutateBytes(valid, 48, 131);
        corpus.Add([]);
        corpus.Add([0, 0, 0, 5, 0, 1]);
        SofhMessageFramer framer = new SofhMessageFramer();
        Random chunks = new Random(131);

        for (int corpusIndex = 0; corpusIndex < corpus.Count; corpusIndex++)
        {
            byte[] input = corpus[corpusIndex];
            string firstFrame = FrameText(framer.ReadFrame(input, new FrameOptions()));
            string secondFrame = FrameText(framer.ReadFrame(input, new FrameOptions()));
            Assert.That(secondFrame, Is.EqualTo(firstFrame), "corpus " + corpusIndex);
            int received = 0;
            while (received < input.Length)
            {
                received = Math.Min(input.Length, received + chunks.Next(1, 4));
                ReadOnlySpan<byte> prefix = input.AsSpan(0, received);
                string firstPrefix = FrameText(framer.ReadFrame(prefix, new FrameOptions()));
                string secondPrefix = FrameText(framer.ReadFrame(prefix, new FrameOptions()));
                Assert.That(secondPrefix, Is.EqualTo(firstPrefix), "prefix " + corpusIndex + ":" + received);
            }
        }

        Assert.That(framer.ReadFrame(valid, new FrameOptions()).Status, Is.EqualTo(FrameStatus.Complete));
    }

    [Test]
    [CancelAfter(10000)]
    public void DictionaryLoadersReturnStableDiagnosticsForMalformedXmlAndChunkedReads()
    {
        byte[] quickFix = Encoding.UTF8.GetBytes("<fix type=\"FIX\" major=\"4\" minor=\"4\"><header/><messages/><trailer/><components/><fields/></fix>");
        byte[] orchestra = Encoding.UTF8.GetBytes("<repository xmlns=\"http://fixprotocol.io/2026/orchestra/repository\" name=\"Corpus\" version=\"FIX.4.4\"><datatypes/><codeSets/><fields/></repository>");
        List<byte[]> quickFixCorpus = MutateBytes(quickFix, 24, 132);
        List<byte[]> orchestraCorpus = MutateBytes(orchestra, 24, 133);
        quickFixCorpus.Add("<fix><fields>"u8.ToArray());
        quickFixCorpus.Add([60, 102, 105, 120, 255, 62]);
        orchestraCorpus.Add("<repository>"u8.ToArray());
        orchestraCorpus.Add([60, 114, 101, 112, 111, 255, 62]);
        QuickFixDictionaryLoader quickFixLoader = new QuickFixDictionaryLoader();
        OrchestraDictionaryLoader orchestraLoader = new OrchestraDictionaryLoader();

        for (int index = 0; index < quickFixCorpus.Count; index++)
        {
            string contiguous = LoadDictionaryText(quickFixLoader, new MemoryStream(quickFixCorpus[index], false));
            string chunked = LoadDictionaryText(quickFixLoader, new RandomChunkStream(quickFixCorpus[index], 132 + index));
            Assert.That(chunked, Is.EqualTo(contiguous), "QuickFIX corpus " + index);
            Assert.That(LoadDictionaryText(quickFixLoader, new MemoryStream(quickFixCorpus[index], false)), Is.EqualTo(contiguous), "QuickFIX repeat " + index);
        }
        for (int index = 0; index < orchestraCorpus.Count; index++)
        {
            string contiguous = LoadDictionaryText(orchestraLoader, new MemoryStream(orchestraCorpus[index], false));
            string chunked = LoadDictionaryText(orchestraLoader, new RandomChunkStream(orchestraCorpus[index], 133 + index));
            Assert.That(chunked, Is.EqualTo(contiguous), "Orchestra corpus " + index);
            Assert.That(LoadDictionaryText(orchestraLoader, new MemoryStream(orchestraCorpus[index], false)), Is.EqualTo(contiguous), "Orchestra repeat " + index);
        }

        Assert.Multiple(() =>
        {
            Assert.That(LoadDictionaryText(quickFixLoader, new MemoryStream("<fix>"u8.ToArray(), false)), Does.Contain("invalid-quickfix-xml"));
            Assert.That(LoadDictionaryText(orchestraLoader, new MemoryStream("<repository>"u8.ToArray(), false)), Does.Contain("invalid-orchestra-xml"));
        });
    }

    [Test]
    [CancelAfter(10000)]
    public void OrchestraReaderReturnsStableDiagnosticsForMalformedXmlAndChunkedReads()
    {
        byte[] valid = Encoding.UTF8.GetBytes("<repository xmlns=\"http://fixprotocol.io/2026/orchestra/repository\" name=\"Corpus\" version=\"1.1\"><datatypes/><codeSets/><fields/></repository>");
        List<byte[]> corpus = MutateBytes(valid, 32, 134);
        corpus.Add("<!DOCTYPE repository [<!ENTITY x SYSTEM 'file:///tmp/nope'>]><repository>&x;</repository>"u8.ToArray());
        corpus.Add("<repository>"u8.ToArray());
        OrchestraReader reader = new OrchestraReader();
        OrchestraReadOptions options = new OrchestraReadOptions { ValidateSchema = false, BaseUri = "corpus.xml" };

        for (int index = 0; index < corpus.Count; index++)
        {
            string contiguous = OrchestraReadText(reader.ReadRepository(new MemoryStream(corpus[index], false), options));
            string chunked = OrchestraReadText(reader.ReadRepository(new RandomChunkStream(corpus[index], 134 + index), options));
            Assert.That(chunked, Is.EqualTo(contiguous), "corpus " + index);
            Assert.That(OrchestraReadText(reader.ReadRepository(new MemoryStream(corpus[index], false), options)), Is.EqualTo(contiguous), "repeat " + index);
        }

        OrchestraReadResult prohibited = reader.ReadRepository(new MemoryStream(corpus[corpus.Count - 2], false), options);
        Assert.That(prohibited.Diagnostics.Select(item => item.Code), Does.Contain("external-entities-prohibited"));
    }

    [Test]
    [CancelAfter(10000)]
    public void SessionSettingsLoaderHasStableOutcomesForMalformedTextAndChunkedReads()
    {
        byte[] valid = Encoding.UTF8.GetBytes("[SESSION]\nBeginString=FIX.4.4\nSenderCompID=BUY\nTargetCompID=SELL\nConnectionType=acceptor\nSocketAcceptPort=5000\n");
        List<byte[]> corpus = MutateAscii(valid, 32, 135);
        corpus.Add("[SESSION\nBeginString=FIX.4.4"u8.ToArray());
        corpus.Add("outside=value"u8.ToArray());
        TextSessionSettingsLoader loader = new TextSessionSettingsLoader();

        for (int index = 0; index < corpus.Count; index++)
        {
            string contiguous = SettingsText(loader, new MemoryStream(corpus[index], false));
            string chunked = SettingsText(loader, new RandomChunkStream(corpus[index], 135 + index));
            Assert.That(chunked, Is.EqualTo(contiguous), "corpus " + index);
            Assert.That(SettingsText(loader, new MemoryStream(corpus[index], false)), Is.EqualTo(contiguous), "repeat " + index);
            Assert.That(contiguous, Does.Not.StartWith("unexpected:"), "corpus " + index);
        }

        Assert.That(SettingsText(loader, new MemoryStream("[SESSION"u8.ToArray(), false)), Does.StartWith("fix:settings:malformed-section-header"));
    }

    [Test]
    [CancelAfter(10000)]
    public void PublicValidatorsReturnStableIssueSequencesForMalformedModels()
    {
        FixMessageValidator messageValidator = new FixMessageValidator();
        FixDictionary dictionary = ValidatorDictionary();
        Random random = new Random(136);
        for (int index = 0; index < 64; index++)
        {
            FixMessage message = MalformedMessage(random, index);
            string first = ValidationText(messageValidator.ValidateMessage(message, dictionary, new ValidationOptions(), new ValidationContext()));
            string second = ValidationText(messageValidator.ValidateMessage(message, dictionary, new ValidationOptions(), new ValidationContext()));
            Assert.That(second, Is.EqualTo(first), "message " + index);
        }

        OrchestraRepository repository = MalformedRepository();
        OrchestraValidator orchestraValidator = new OrchestraValidator();
        string firstRepository = OrchestraReadText(orchestraValidator.ValidateRepository(repository));
        string secondRepository = OrchestraReadText(orchestraValidator.ValidateRepository(repository));
        string missingRepository = OrchestraReadText(orchestraValidator.ValidateRepository(null!));
        Assert.Multiple(() =>
        {
            Assert.That(secondRepository, Is.EqualTo(firstRepository));
            Assert.That(firstRepository, Does.Contain("duplicate-field-id"));
            Assert.That(firstRepository, Does.Contain("unresolved-member-reference"));
            Assert.That(missingRepository, Does.Contain("missing-orchestra-repository"));
        });
    }

    private static List<byte[]> MutateBytes(byte[] seed, int count, int randomSeed)
    {
        List<byte[]> corpus = [seed];
        Random random = new Random(randomSeed);
        for (int index = 0; index < count; index++)
        {
            int length = random.Next(seed.Length + 1);
            byte[] mutation = seed.AsSpan(0, length).ToArray();
            int edits = random.Next(1, 5);
            for (int edit = 0; edit < edits && mutation.Length != 0; edit++)
            {
                int position = random.Next(mutation.Length);
                mutation[position] = (byte)random.Next(256);
            }
            corpus.Add(mutation);
        }
        return corpus;
    }

    private static List<byte[]> MutateAscii(byte[] seed, int count, int randomSeed)
    {
        List<byte[]> corpus = [seed];
        byte[] replacements = "[]=_#\nABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"u8.ToArray();
        Random random = new Random(randomSeed);
        for (int index = 0; index < count; index++)
        {
            int length = random.Next(seed.Length + 1);
            byte[] mutation = seed.AsSpan(0, length).ToArray();
            int edits = random.Next(1, 5);
            for (int edit = 0; edit < edits && mutation.Length != 0; edit++) mutation[random.Next(mutation.Length)] = replacements[random.Next(replacements.Length)];
            corpus.Add(mutation);
        }
        return corpus;
    }

    private static byte[] BuildTagValueFrame(string bodyText)
    {
        byte[] body = Encoding.ASCII.GetBytes(bodyText);
        byte[] header = Encoding.ASCII.GetBytes("8=FIX.4.4\u00019=" + body.Length + "\u0001");
        byte[] withoutChecksum = Combine(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(withoutChecksum);
        return Combine(withoutChecksum, Encoding.ASCII.GetBytes("10=" + checksum.ToString("000") + "\u0001"));
    }

    private static byte[] BuildSofhFrame(byte[] payload)
    {
        int length = payload.Length + FixDefaults.SofhHeaderSize;
        byte[] header = [(byte)(length >> 24), (byte)(length >> 16), (byte)(length >> 8), (byte)length, (byte)(FixEncodingTypes.SbeVersion10BigEndian >> 8), (byte)(FixEncodingTypes.SbeVersion10BigEndian & 255)];
        return Combine(header, payload);
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    private static FixDictionary DecoderDictionary()
    {
        return new FixDictionary
        {
            Fields =
            [
                FieldDefinition(FixTags.BeginString, "BeginString", FixFieldType.String),
                FieldDefinition(FixTags.BodyLength, "BodyLength", FixFieldType.Length),
                FieldDefinition(FixTags.MsgType, "MsgType", FixFieldType.String),
                FieldDefinition(FixTags.CheckSum, "CheckSum", FixFieldType.String),
                FieldDefinition(11, "ClOrdID", FixFieldType.String)
            ],
            Header = [Member(FixTags.BeginString), Member(FixTags.BodyLength), Member(FixTags.MsgType)],
            Trailer = [Member(FixTags.CheckSum)],
            Messages = [new FixMessageDefinition { Name = "NewOrderSingle", MsgType = "D", Members = [Member(11)] }]
        };
    }

    private static FixDictionary ValidatorDictionary()
    {
        FixDictionary dictionary = DecoderDictionary();
        dictionary.Fields.Add(FieldDefinition(54, "Side", FixFieldType.Char));
        dictionary.Messages[0].Members.Add(new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 54, Presence = FixPresence.Required });
        return dictionary;
    }

    private static FixFieldDefinition FieldDefinition(int tag, string name, FixFieldType type)
    {
        return new FixFieldDefinition { Tag = tag, Name = name, FieldType = type };
    }

    private static FixMemberDefinition Member(int tag)
    {
        return new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = tag, Presence = FixPresence.Required };
    }

    private static FixMessage MalformedMessage(Random random, int index)
    {
        FixMessage message = new FixMessage { BeginString = index % 2 == 0 ? "" : "BAD", MsgType = index % 3 == 0 ? "D" : random.Next(100).ToString() };
        int fieldCount = random.Next(1, 12);
        for (int fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
        {
            int tag = random.Next(-2, 80);
            int valueLength = random.Next(0, 8);
            byte[] value = new byte[valueLength];
            random.NextBytes(value);
            message.BodyFields.Add(new FixField { Tag = tag, Value = value, WireIndex = random.Next(-1, 5) });
        }
        message.BodyGroups.Add(new FixGroup { CountTag = random.Next(-2, 80), DelimiterTag = random.Next(-2, 80), DeclaredCount = random.Next(-2, 4) });
        return message;
    }

    private static OrchestraRepository MalformedRepository()
    {
        return new OrchestraRepository
        {
            Datatypes = [new OrchestraDatatypeDefinition { Name = "String" }],
            Fields =
            [
                new OrchestraFieldDefinition { Id = "1", Name = "First", Type = "String" },
                new OrchestraFieldDefinition { Id = "1", Name = "Second", Type = "Missing" }
            ],
            Messages =
            [
                new OrchestraMessageDefinition
                {
                    Id = "10",
                    Name = "Broken",
                    MessageType = "Z",
                    Members = [new OrchestraMemberDefinition { Kind = "fieldRef", ReferenceId = "999", Position = -1 }]
                }
            ]
        };
    }

    private static string FrameText(FrameResult result)
    {
        return result.Status + ":" + result.FrameOffset + ":" + result.FrameLength + ":" + result.Consumed + ":" + result.Required + ":" + result.IsSofh + ":" + result.EncodingType + ":" + result.ErrorOffset + ":" + result.ErrorCode;
    }

    private static string DecodeText(DecodeResult result)
    {
        int fields = result.Message == null ? -1 : result.Message.HeaderFields.Count + result.Message.BodyFields.Count + result.Message.TrailerFields.Count;
        return result.Status + ":" + result.Consumed + ":" + result.Required + ":" + result.ErrorOffset + ":" + result.ErrorCode + ":" + fields + ":" + result.FieldResults.Count;
    }

    private static string LoadDictionaryText(IDictionaryLoader loader, Stream source)
    {
        using (source)
        {
            DictionaryLoadResult result = loader.LoadDictionary(source, new FixDictionaryOptions());
            int fields = result.Dictionary == null ? -1 : result.Dictionary.Fields.Count;
            int messages = result.Dictionary == null ? -1 : result.Dictionary.Messages.Count;
            return result.Success + ":" + fields + ":" + messages + ":" + string.Join(",", result.Diagnostics.ConvertAll(item => item.Code));
        }
    }

    private static string OrchestraReadText(OrchestraReadResult result)
    {
        int fields = result.Repository == null ? -1 : result.Repository.Fields.Count;
        return result.Success + ":" + fields + ":" + string.Join(",", result.Diagnostics.ConvertAll(item => item.Code));
    }

    private static string SettingsText(ISessionSettingsLoader loader, Stream source)
    {
        using (source)
        {
            try
            {
                SessionSettings[] settings = loader.LoadSessionSettings(source);
                return "ok:" + settings.Length + ":" + string.Join(",", settings.Select(item => item.Id.SenderCompID));
            }
            catch (FixSessionException exception)
            {
                return "fix:" + exception.Message;
            }
            catch (Exception exception)
            {
                return "unexpected:" + exception.GetType().Name + ":" + exception.Message;
            }
        }
    }

    private static string ValidationText(ValidationResult result)
    {
        return result.IsValid + ":" + result.IssueLimitReached + ":" + string.Join(",", result.Issues.ConvertAll(item => item.Code + "/" + item.Tag + "/" + item.GroupPath));
    }

    private class RandomChunkStream : MemoryStream
    {
        private Random RandomChunks;

        internal RandomChunkStream(byte[] bytes, int seed) : base(bytes, false)
        {
            RandomChunks = new Random(seed);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, Math.Min(count, RandomChunks.Next(1, 8)));
        }

        public override int Read(Span<byte> buffer)
        {
            return base.Read(buffer.Slice(0, Math.Min(buffer.Length, RandomChunks.Next(1, 8))));
        }
    }
}
