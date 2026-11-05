using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceCryptocurrencyTests
{
    [Test]
    public void AddressesAreDeterministicValidRecognizableShapes()
    {
        const long seed = 9979;
        string bitcoin = Fake.Create(seed).Finance().BitcoinAddress();
        string litecoin = Fake.Create(seed).Finance().LitecoinAddress();
        string ethereum = Fake.Create(seed).Finance().EthereumAddress();
        FinanceProvider validator = Fake.Create(seed).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(bitcoin, Is.EqualTo(Fake.Create(seed).Finance().BitcoinAddress()));
            Assert.That(litecoin, Is.EqualTo(Fake.Create(seed).Finance().LitecoinAddress()));
            Assert.That(ethereum, Is.EqualTo(Fake.Create(seed).Finance().EthereumAddress()));
            Assert.That(bitcoin, Does.Match("^1[1-9A-HJ-NP-Za-km-z]{25,34}$"));
            Assert.That(litecoin, Does.Match("^L[1-9A-HJ-NP-Za-km-z]{25,34}$"));
            Assert.That(ethereum, Does.Match("^0x[0-9A-Fa-f]{40}$"));
            Assert.That(validator.IsBitcoinAddressValid(bitcoin), Is.True);
            Assert.That(validator.IsLitecoinAddressValid(litecoin), Is.True);
            Assert.That(validator.IsEthereumAddressValid(ethereum), Is.True);
            Assert.That(Fake.Create(seed).Finance().CryptocurrencyAddress(Cryptocurrency.Bitcoin),
                Is.EqualTo(bitcoin));
        });
    }

    [Test]
    public void Base58CheckValidationRejectsChecksumAndAlphabetMutations()
    {
        FinanceProvider finance = Fake.Create(9980).Finance();
        string bitcoin = finance.BitcoinAddress();
        string litecoin = finance.LitecoinAddress();

        Assert.Multiple(() =>
        {
            Assert.That(finance.IsBitcoinAddressValid(MutateLastBase58Character(bitcoin)),
                Is.False);
            Assert.That(finance.IsLitecoinAddressValid(MutateLastBase58Character(litecoin)),
                Is.False);
            Assert.That(finance.IsBitcoinAddressValid(bitcoin.Substring(0, 5) + "0" +
                bitcoin.Substring(6)), Is.False);
            Assert.That(finance.IsLitecoinAddressValid(bitcoin), Is.False);
            Assert.That(finance.IsBitcoinAddressValid(litecoin), Is.False);
        });
    }

    [Test]
    public void EthereumValidationImplementsMixedCaseChecksumPresentation()
    {
        FinanceProvider finance = Fake.Create(9981).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(finance.IsEthereumAddressValid(
                "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed"), Is.True);
            Assert.That(finance.IsEthereumAddressValid(
                "0x5AAeb6053F3E94C9b9A09f33669435E7Ef1BeAed"), Is.False);
            Assert.That(finance.IsEthereumAddressValid(
                "0xde709f2102306220921060314715629080e2fb77"), Is.True);
            Assert.That(finance.IsEthereumAddressValid(
                "0X5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed"), Is.False);
            Assert.That(finance.IsEthereumAddressValid("0x1234"), Is.False);
            Assert.That(finance.IsEthereumAddressValid(
                "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAez"), Is.False);
        });
    }

    [Test]
    public void AddressRecordsAreChecksummedSyntheticAndKeyless()
    {
        foreach (Cryptocurrency cryptocurrency in Enum.GetValues<Cryptocurrency>())
        {
            FinanceProvider finance = Fake.Create(9982).Finance();
            CryptoAddressRecord record = finance.CryptoAddressFromCryptocurrency(cryptocurrency);

            Assert.Multiple(() =>
            {
                Assert.That(record.Cryptocurrency, Is.EqualTo(cryptocurrency));
                Assert.That(finance.IsCryptocurrencyAddressValid(
                    cryptocurrency,
                    record.Address), Is.True);
                Assert.That(record.IsChecksumValid, Is.True);
                Assert.That(record.IsSynthetic, Is.True);
                Assert.That(record.TestMarker, Is.EqualTo("fake.synthetic-crypto.v1"));
                Assert.That(record.ContainsPrivateKey, Is.False);
                Assert.That(record.IsSpendable, Is.False);
                Assert.That(record.RepresentsActiveWallet, Is.False);
                Assert.That(record.Network,
                    cryptocurrency == Cryptocurrency.Ethereum
                        ? Is.EqualTo("network-unspecified")
                        : Is.EqualTo("mainnet-shaped"));
            });
        }

        CryptoAddressRecord first = Fake.Create(9983).Finance().CryptoAddress();
        CryptoAddressRecord second = Fake.Create(9983).Finance().CryptoAddress();
        Assert.That(first.Cryptocurrency, Is.EqualTo(second.Cryptocurrency));
        Assert.That(first.Address, Is.EqualTo(second.Address));
    }

    [Test]
    public void HashesUseDeterministicConventionalHexadecimalShapes()
    {
        const long seed = 9984;
        string generic = Fake.Create(seed).Finance().CryptoHash();
        string bitcoin = Fake.Create(seed).Finance().CryptoHashFromCryptocurrency(Cryptocurrency.Bitcoin);
        string ethereum = Fake.Create(seed).Finance().CryptoHashFromCryptocurrency(Cryptocurrency.Ethereum);
        string litecoin = Fake.Create(seed).Finance().CryptoHashFromCryptocurrency(Cryptocurrency.Litecoin);

        Assert.Multiple(() =>
        {
            Assert.That(generic, Is.EqualTo(Fake.Create(seed).Finance().CryptoHash()));
            Assert.That(generic, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(bitcoin, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(ethereum, Does.Match("^0x[0-9a-f]{64}$"));
            Assert.That(litecoin, Does.Match("^[0-9a-f]{64}$"));
        });
    }

    [Test]
    public void CryptocurrencyApisRejectInvalidInputsAndHonorLimitsBeforeEntropy()
    {
        const long seed = 9985;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 19;
        FakeOptions hashCountOptions = FakeOptions.Create(seed);
        hashCountOptions.MaximumCount = 31;
        FakeOptions addressOutputOptions = FakeOptions.Create(seed);
        addressOutputOptions.MaximumOutputSize = 34;
        FakeOptions ethereumOutputOptions = FakeOptions.Create(seed);
        ethereumOutputOptions.MaximumOutputSize = 41;
        FakeOptions hashOutputOptions = FakeOptions.Create(seed);
        hashOutputOptions.MaximumOutputSize = 63;
        FakeOptions recordOutputOptions = FakeOptions.Create(seed);
        recordOutputOptions.MaximumOutputSize = 111;

        Assert.That(() => inspected.CryptocurrencyAddress((Cryptocurrency)999),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("cryptocurrency"));
        Assert.That(inspected.BitcoinAddress(), Is.EqualTo(untouched.BitcoinAddress()));
        Assert.That(() => inspected.IsBitcoinAddressValid(null),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => inspected.IsLitecoinAddressValid(null),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => inspected.IsEthereumAddressValid(null),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Fake.Create(countOptions).Finance().BitcoinAddress(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(hashCountOptions).Finance().CryptoHash(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(addressOutputOptions).Finance().BitcoinAddress(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(ethereumOutputOptions).Finance().EthereumAddress(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(hashOutputOptions).Finance().CryptoHash(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(recordOutputOptions).Finance().CryptoAddress(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static string MutateLastBase58Character(string value)
    {
        char replacement = value[value.Length - 1] == '1' ? '2' : '1';
        return value.Substring(0, value.Length - 1) + replacement;
    }
}
