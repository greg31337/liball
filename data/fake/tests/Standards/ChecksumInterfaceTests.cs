using System;
using fake;

namespace fake.Tests;

[TestFixture]
public class ChecksumInterfaceTests
{
    [Test]
    public void KindBasedLookupReturnsTheCachedNamedEngines()
    {
        ChecksumProvider checksums = Fake.Create(4320).Checksum();

        Assert.That(checksums.Engine(ChecksumKind.Luhn), Is.SameAs(checksums.Luhn()));
        Assert.That(checksums.Engine(ChecksumKind.Verhoeff), Is.SameAs(checksums.Verhoeff()));
        Assert.That(checksums.Engine(ChecksumKind.Damm), Is.SameAs(checksums.Damm()));
        Assert.That(checksums.Engine(ChecksumKind.Modulo10), Is.SameAs(checksums.Modulo10()));
        Assert.That(checksums.Engine(ChecksumKind.Modulo11), Is.SameAs(checksums.Modulo11()));
        Assert.That(checksums.Engine(ChecksumKind.Mod97), Is.SameAs(checksums.Mod97()));
        Assert.That(checksums.Engine(ChecksumKind.Isbn10), Is.SameAs(checksums.Isbn10()));
        Assert.That(checksums.Engine(ChecksumKind.Isbn13), Is.SameAs(checksums.Isbn13()));
        Assert.That(checksums.Engine(ChecksumKind.Issn), Is.SameAs(checksums.Issn()));
        Assert.That(checksums.Engine(ChecksumKind.Ean8), Is.SameAs(checksums.Ean8()));
        Assert.That(checksums.Engine(ChecksumKind.UpcA), Is.SameAs(checksums.UpcA()));
        Assert.That(checksums.Engine(ChecksumKind.Gtin), Is.SameAs(checksums.Gtin()));
    }

    [Test]
    public void EveryAppendStyleEngineSatisfiesTheCommonContract()
    {
        ChecksumProvider checksums = Fake.Create(4321).Checksum();
        IChecksum[] engines = new IChecksum[]
        {
            checksums.Luhn(),
            checksums.Verhoeff(),
            checksums.Damm(),
            checksums.Modulo10(),
            checksums.Modulo11(),
            checksums.Mod97(),
            checksums.Isbn10(),
            checksums.Isbn13(),
            checksums.Issn(),
            checksums.Ean8(),
            checksums.UpcA(),
            checksums.Gtin()
        };
        string[] payloads = new string[]
        {
            "7992739871",
            "236",
            "572",
            "1234567",
            "123456789",
            "123456789",
            "030640615",
            "978030640615",
            "0317847",
            "5512345",
            "03600029145",
            "1234567890123"
        };

        int index = 0;
        while (index < engines.Length)
        {
            IChecksum engine = engines[index];
            string payload = payloads[index];
            string complete = engine.Append(payload);
            char[] destination = new char[payload.Length + 2];
            int written = engine.AppendFromValueAndDestination(payload.AsSpan(), destination);

            Assert.That(engine.Calculate(payload), Is.EqualTo(engine.CalculateFromValue(payload.AsSpan())));
            Assert.That(engine.Validate(complete), Is.True, $"engine index {index}");
            Assert.That(engine.ValidateFromValue(complete.AsSpan()), Is.True, $"engine index {index}");
            Assert.That(new string(destination, 0, written), Is.EqualTo(complete));
            index++;
        }
    }

    [Test]
    public void KindBasedLookupRejectsUnknownKinds()
    {
        ChecksumProvider checksums = Fake.Create(4322).Checksum();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => checksums.Engine((ChecksumKind)int.MaxValue));

        Assert.That(error.ParamName, Is.EqualTo("kind"));
    }
}
