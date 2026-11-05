using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class DeterministicRandomSourceTests
{
    [Test]
    public void RandomIndex_WithInjectedSource_DeterministicallyExercisesRejectionSampling()
    {
        SequenceRandomSource source = new SequenceRandomSource(new byte[] { 255, 2 });

        int index = RandomTools.RandomIndex(3, source);

        Assert.That(index, Is.EqualTo(2));
        Assert.That(source.BytesRead, Is.EqualTo(2));
    }

    [Test]
    public void NanoId_WithInjectedSource_ProducesRepeatableText()
    {
        SequenceRandomSource source = new SequenceRandomSource(new byte[] { 0, 1, 2, 0 });
        NanoIdSettings settings = new NanoIdSettings("abc", 4);

        string value = NanoId.NewWithSettings(settings, source);

        Assert.That(value, Is.EqualTo("abca"));
    }

    [Test]
    public void NanoId_WithInjectedSource_RejectsNullEvenForEmptyOutput()
    {
        NanoIdSettings settings = new NanoIdSettings("ab", 0);

        Assert.That(() => NanoId.NewWithSettings(settings, null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Generators_WithInjectedSource_ProduceRepeatablePayloads()
    {
        SequenceRandomSource uuidSource = new SequenceRandomSource(new byte[16]);
        SequenceRandomSource ulidSource = new SequenceRandomSource(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        SequenceRandomSource ksuidSource = new SequenceRandomSource(new byte[16]);

        UuidValue uuid = UuidGenerator.NewV4(uuidSource);
        UlidValue ulid = UlidGenerator.NewAtUnixMilliseconds(1, ulidSource);
        KsuidValue ksuid = KsuidGenerator.NewAtUnixSeconds(KsuidLimits.EpochUnixSeconds, ksuidSource);

        Assert.That(uuid.ToCanonicalString(), Is.EqualTo("00000000-0000-4000-8000-000000000000"));
        Assert.That(ulid.ToBytes(), Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
        Assert.That(ksuid.ToBytes(), Is.EqualTo(new byte[20]));
    }

    [Test]
    public void ObjectIdGenerator_WithInjectedSource_ControlsProcessBytesAndInitialCounter()
    {
        SequenceRandomSource source = new SequenceRandomSource(new byte[] { 1, 2, 3, 4, 5, 0, 0, 7 });
        ObjectIdGenerator generator = ObjectIdGenerator.Create(source);

        ObjectIdValue value = generator.NextAtUnixSeconds(0);

        Assert.That(value.ToBytes(), Is.EqualTo(new byte[] { 0, 0, 0, 0, 1, 2, 3, 4, 5, 0, 0, 7 }));
    }

    class SequenceRandomSource : IRandomSource
    {
        byte[] Bytes;
        public int BytesRead;

        public SequenceRandomSource(byte[] bytes)
        {
            Bytes = bytes;
            BytesRead = 0;
        }

        public void FillBytes(byte[] destination)
        {
            int index = 0;
            while (index < destination.Length)
            {
                if (BytesRead >= Bytes.Length)
                {
                    throw new InvalidOperationException("scripted random bytes exhausted");
                }

                destination[index] = Bytes[BytesRead];
                BytesRead = BytesRead + 1;
                index = index + 1;
            }
        }
    }
}
