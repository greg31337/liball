using fake;

namespace fake.Tests;

[TestFixture]
public class SecretGenerationTests
{
    [Test]
    public void BytesAndSpanWritingAreExactAndDeterministic()
    {
        PrimitiveProvider allocating = Fake.Create(3701).Primitive();
        PrimitiveProvider writing = Fake.Create(3701).Primitive();
        byte[] destination = new byte[37];

        byte[] expected = allocating.Bytes(37);
        int written = writing.WriteBytes(destination);

        Assert.That(written, Is.EqualTo(37));
        Assert.That(destination, Is.EqualTo(expected));
        Assert.That(expected, Has.Some.Not.EqualTo((byte)0));
        Assert.That(allocating.Bytes(0), Is.Empty);
    }

    [Test]
    public void HexHashTokenAndPinHaveExactDocumentedShapes()
    {
        PrimitiveProvider first = Fake.Create(3702).Primitive();
        PrimitiveProvider second = Fake.Create(3702).Primitive();

        string hex = first.HexFromCount(31);
        string hash = first.Hash(33);
        string token = first.Token(35);
        string pin = first.Pin(9);

        Assert.Multiple(() =>
        {
            Assert.That(hex, Does.Match("^[0-9a-f]{31}$"));
            Assert.That(hash, Does.Match("^[0-9a-f]{33}$"));
            Assert.That(token, Does.Match("^[A-Za-z0-9_-]{35}$"));
            Assert.That(pin, Does.Match("^[0-9]{9}$"));
            Assert.That(second.HexFromCount(31), Is.EqualTo(hex));
            Assert.That(second.Hash(33), Is.EqualTo(hash));
            Assert.That(second.Token(35), Is.EqualTo(token));
            Assert.That(second.Pin(9), Is.EqualTo(pin));
        });
    }

    [Test]
    public void FixedCharacterFormatsHaveEquivalentSpanWriters()
    {
        PrimitiveProvider allocating = Fake.Create(3703).Primitive();
        PrimitiveProvider writing = Fake.Create(3703).Primitive();
        char[] hex = new char[17];
        char[] hash = new char[19];
        char[] token = new char[23];
        char[] pin = new char[7];

        string expectedHex = allocating.HexFromCount(hex.Length);
        string expectedHash = allocating.Hash(hash.Length);
        string expectedToken = allocating.Token(token.Length);
        string expectedPin = allocating.Pin(pin.Length);

        Assert.Multiple(() =>
        {
            Assert.That(writing.WriteHexFromDestination(hex), Is.EqualTo(hex.Length));
            Assert.That(writing.WriteHash(hash), Is.EqualTo(hash.Length));
            Assert.That(writing.WriteToken(token), Is.EqualTo(token.Length));
            Assert.That(writing.WritePin(pin), Is.EqualTo(pin.Length));
            Assert.That(new string(hex), Is.EqualTo(expectedHex));
            Assert.That(new string(hash), Is.EqualTo(expectedHash));
            Assert.That(new string(token), Is.EqualTo(expectedToken));
            Assert.That(new string(pin), Is.EqualTo(expectedPin));
        });
    }

    [Test]
    public void RecoveryCodesUseUnambiguousCharactersAndExactGroups()
    {
        PrimitiveProvider allocating = Fake.Create(3704).Primitive();
        PrimitiveProvider writing = Fake.Create(3704).Primitive();
        char[] destination = new char[14];

        string standard = allocating.RecoveryCode();
        string expected = allocating.RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(3, 4, '.');
        int written = writing.WriteRecoveryCode(new char[19], 4, 4, '-');
        written = writing.WriteRecoveryCode(destination, 3, 4, '.');

        Assert.Multiple(() =>
        {
            Assert.That(standard, Does.Match("^[2-9A-HJ-NP-Z]{4}(-[2-9A-HJ-NP-Z]{4}){3}$"));
            Assert.That(expected, Does.Match("^[2-9A-HJ-NP-Z]{4}(\\.[2-9A-HJ-NP-Z]{4}){2}$"));
            Assert.That(written, Is.EqualTo(destination.Length));
            Assert.That(new string(destination), Is.EqualTo(expected));
            Assert.That(allocating.RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(0, 4, '-'), Is.Empty);
        });
    }

    [Test]
    public void PasswordGuaranteesEveryEnabledCharacterClass()
    {
        PrimitiveProvider primitive = Fake.Create(3705).Primitive();

        string password = primitive.Password(40);
        string minimal = primitive.PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(2, false, true, true, false);
        string lowercase = primitive.PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(20, true, false, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(password, Has.Length.EqualTo(40));
            Assert.That(password.Any(character => character >= 'a' && character <= 'z'), Is.True);
            Assert.That(password.Any(character => character >= 'A' && character <= 'Z'), Is.True);
            Assert.That(password.Any(character => character >= '0' && character <= '9'), Is.True);
            Assert.That(password.Any(character => "!#$%&()*+,-./:;<=>?@[]^_{|}~".Contains(character)), Is.True);
            Assert.That(minimal, Does.Match("^(?=.*[A-Z])(?=.*[0-9])[A-Z0-9]{2}$"));
            Assert.That(lowercase, Does.Match("^[a-z]{20}$"));
        });
    }

    [Test]
    public void PasswordSpanWritingMatchesAllocation()
    {
        PrimitiveProvider allocating = Fake.Create(3706).Primitive();
        PrimitiveProvider writing = Fake.Create(3706).Primitive();
        char[] standard = new char[24];
        char[] custom = new char[16];

        string expectedStandard = allocating.Password(standard.Length);
        string expectedCustom = allocating.PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(custom.Length, true, false, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(writing.WritePassword(standard), Is.EqualTo(standard.Length));
            Assert.That(writing.WritePasswordFromDestinationAndLowercaseAndUppercaseAndDigitsAndSymbols(custom, true, false, true, false), Is.EqualTo(custom.Length));
            Assert.That(new string(standard), Is.EqualTo(expectedStandard));
            Assert.That(new string(custom), Is.EqualTo(expectedCustom));
        });
    }

    [Test]
    public void PassphraseUsesAuthoredFiveLetterWordsAndExactSeparators()
    {
        PrimitiveProvider allocating = Fake.Create(3707).Primitive();
        PrimitiveProvider writing = Fake.Create(3707).Primitive();
        char[] destination = new char[19];

        string standard = allocating.Passphrase(4);
        string expected = allocating.PassphraseFromWordCountAndSeparator(3, "🙂");
        int written = writing.WritePassphrase(new char[23], 4, "-");
        written = writing.WritePassphrase(destination, 3, "🙂");

        Assert.Multiple(() =>
        {
            Assert.That(standard, Does.Match("^[a-z]{5}(-[a-z]{5}){3}$"));
            Assert.That(expected, Does.Match("^[a-z]{5}🙂[a-z]{5}🙂[a-z]{5}$"));
            Assert.That(written, Is.EqualTo(destination.Length));
            Assert.That(new string(destination), Is.EqualTo(expected));
            Assert.That(allocating.Passphrase(0), Is.Empty);
        });
    }

    [Test]
    public void SecretGenerationValidatesCountsClassesShapesAndSeparators()
    {
        PrimitiveProvider primitive = Fake.Create(3708).Primitive();
        string malformed = new string(new[] { '\uD800' });

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Bytes(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.HexFromCount(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Hash(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Token(-1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Pin(-1)).ParamName, Is.EqualTo("digits"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(-1, 4, '-')).ParamName, Is.EqualTo("groupCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(2, 0, '-')).ParamName, Is.EqualTo("groupLength"));
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(2, 4, 'A')).ParamName, Is.EqualTo("separator"));
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.WriteRecoveryCode(new char[8], 2, 4, '-')).ParamName, Is.EqualTo("destination"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Password(3)).ParamName, Is.EqualTo("length"));
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(10, false, false, false, false)), Is.Not.Null);
            Assert.That(Assert.Throws<ArgumentNullException>(() => primitive.PassphraseFromWordCountAndSeparator(2, null)).ParamName, Is.EqualTo("separator"));
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.PassphraseFromWordCountAndSeparator(2, malformed)).ParamName, Is.EqualTo("separator"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Passphrase(-1)).ParamName, Is.EqualTo("wordCount"));
            Assert.That(Assert.Throws<ArgumentException>(() => primitive.WritePassphrase(new char[10], 2, "-")).ParamName, Is.EqualTo("destination"));
        });
    }

    [Test]
    public void SecretGenerationHonorsConfiguredLimits()
    {
        FakeOptions options = FakeOptions.Create(3709L);
        options.MaximumCount = 2;
        options.MaximumOutputSize = 8;
        PrimitiveProvider primitive = Fake.Create(options).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(primitive.Bytes(8), Has.Length.EqualTo(8));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Bytes(9)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.HexFromCount(9)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Hash(9)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Token(9)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Pin(9)).ParamName, Is.EqualTo("digits"));
            Assert.That(primitive.RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(2, 3, '-'), Has.Length.EqualTo(7));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(3, 1, '-')).ParamName, Is.EqualTo("groupCount"));
            Assert.That(primitive.Password(8), Has.Length.EqualTo(8));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Password(9)).ParamName, Is.EqualTo("length"));
            Assert.That(primitive.Passphrase(1), Has.Length.EqualTo(5));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.Passphrase(2)).ParamName, Is.EqualTo("wordCount"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => primitive.PassphraseFromWordCountAndSeparator(3, "")).ParamName, Is.EqualTo("wordCount"));
        });
    }

    [Test]
    public void CryptographicModeSupportsEverySecretShapeWithoutSeedReplay()
    {
        PrimitiveProvider first = Cryptographic(3710L);
        PrimitiveProvider second = Cryptographic(3710L);

        byte[] firstBytes = first.Bytes(32);
        byte[] secondBytes = second.Bytes(32);
        string firstToken = first.Token(64);
        string secondToken = second.Token(64);

        Assert.Multiple(() =>
        {
            Assert.That(firstBytes, Is.Not.EqualTo(secondBytes));
            Assert.That(firstToken, Is.Not.EqualTo(secondToken));
            Assert.That(first.HexFromCount(20), Does.Match("^[0-9a-f]{20}$"));
            Assert.That(first.Hash(20), Does.Match("^[0-9a-f]{20}$"));
            Assert.That(first.Pin(6), Does.Match("^[0-9]{6}$"));
            Assert.That(first.RecoveryCode(), Does.Match("^[2-9A-HJ-NP-Z-]{19}$"));
            Assert.That(first.Password(20), Has.Length.EqualTo(20));
            Assert.That(first.Passphrase(4), Does.Match("^[a-z-]{23}$"));
        });
    }

    static PrimitiveProvider Cryptographic(long seed)
    {
        FakeOptions options = FakeOptions.Create(seed);
        options.RandomMode = RandomMode.Cryptographic;
        return Fake.Create(options).Primitive();
    }
}
