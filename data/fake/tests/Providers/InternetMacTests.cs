using System.Net.NetworkInformation;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetMacTests
{
    [Test]
    public void DefaultMacIsLocallyAdministeredUnicastAndDeterministic()
    {
        InternetProvider first = Fake.Create(8401).Internet();
        InternetProvider second = Fake.Create(8401).Internet();

        int index = 0;
        while (index < 512)
        {
            string value = first.Mac();
            byte[] bytes = Bytes(value);

            Assert.That(value, Is.EqualTo(second.Mac()));
            Assert.That(value, Does.Match("^[0-9a-f]{2}(:[0-9a-f]{2}){5}$"));
            Assert.That(bytes.Length, Is.EqualTo(6));
            Assert.That(bytes[0] & 1, Is.EqualTo(0));
            Assert.That(bytes[0] & 2, Is.EqualTo(2));
            index++;
        }
    }

    [TestCase(false, false, 0)]
    [TestCase(false, true, 2)]
    [TestCase(true, false, 1)]
    [TestCase(true, true, 3)]
    public void ExplicitFlagsSetOnlyTheIndividualGroupAndUniversalLocalBits(
        bool multicast,
        bool locallyAdministered,
        int expectedLowBits)
    {
        InternetProvider first = Fake.Create(8402).Internet();
        InternetProvider second = Fake.Create(8402).Internet();

        int index = 0;
        while (index < 256)
        {
            string value = first.MacFromMulticastAndLocallyAdministered(multicast, locallyAdministered);
            byte[] bytes = Bytes(value);

            Assert.That(value, Is.EqualTo(second.MacFromMulticastAndLocallyAdministered(multicast, locallyAdministered)));
            Assert.That(bytes[0] & 3, Is.EqualTo(expectedLowBits));
            index++;
        }
    }

    [Test]
    public void PhysicalAddressFormMatchesTextFormAtTheSameSeed()
    {
        InternetProvider textInternet = Fake.Create(8403).Internet();
        InternetProvider addressInternet = Fake.Create(8403).Internet();

        int index = 0;
        while (index < 256)
        {
            string text = textInternet.MacFromMulticastAndLocallyAdministered(true, false);
            PhysicalAddress address = addressInternet.MacAddressFromMulticastAndLocallyAdministered(true, false);

            Assert.That(address.GetAddressBytes(), Is.EqualTo(Bytes(text)));
            Assert.That(address.GetAddressBytes().Length, Is.EqualTo(6));
            index++;
        }
    }

    [Test]
    public void DefaultInterfaceRecordsKeepEveryMacRepresentationAndFlagCorrelated()
    {
        InternetProvider first = Fake.Create(8404).Internet();
        InternetProvider second = Fake.Create(8404).Internet();

        int index = 0;
        while (index < 512)
        {
            NetworkInterfaceRecord record = first.NetworkInterface();
            NetworkInterfaceRecord duplicate = second.NetworkInterface();
            byte[] bytes = record.PhysicalAddress.GetAddressBytes();
            string suffix = Compact(bytes, 3);

            Assert.Multiple(() =>
            {
                Assert.That(record.Name, Is.EqualTo(duplicate.Name));
                Assert.That(record.InterfaceType, Is.EqualTo(duplicate.InterfaceType));
                Assert.That(record.MacAddress, Is.EqualTo(duplicate.MacAddress));
                Assert.That(record.PhysicalAddress, Is.EqualTo(duplicate.PhysicalAddress));
                Assert.That(record.IsUnicast, Is.True);
                Assert.That(record.IsMulticast, Is.False);
                Assert.That(record.IsLocallyAdministered, Is.True);
                Assert.That(record.IsUniversallyAdministered, Is.False);
                Assert.That(bytes, Is.EqualTo(Bytes(record.MacAddress)));
                Assert.That(bytes[0] & 3, Is.EqualTo(2));
                Assert.That(record.Name, Does.EndWith(suffix));
                Assert.That(record.InterfaceType, Is.AnyOf(
                    NetworkInterfaceType.Ethernet,
                    NetworkInterfaceType.Wireless80211));
                Assert.That(
                    record.Name,
                    record.InterfaceType == NetworkInterfaceType.Ethernet
                        ? Does.StartWith("eth-")
                        : Does.StartWith("wlan-"));
            });
            index++;
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ExplicitInterfaceFlagsMatchTheEncodedMacBits(
        bool multicast,
        bool locallyAdministered)
    {
        InternetProvider internet = Fake.Create(8405).Internet();

        int index = 0;
        while (index < 128)
        {
            NetworkInterfaceRecord record = internet.NetworkInterfaceFromMulticastAndLocallyAdministered(
                multicast,
                locallyAdministered);
            byte first = record.PhysicalAddress.GetAddressBytes()[0];

            Assert.Multiple(() =>
            {
                Assert.That(record.IsMulticast, Is.EqualTo(multicast));
                Assert.That(record.IsUnicast, Is.EqualTo(!multicast));
                Assert.That(record.IsLocallyAdministered, Is.EqualTo(locallyAdministered));
                Assert.That(record.IsUniversallyAdministered, Is.EqualTo(!locallyAdministered));
                Assert.That((first & 1) != 0, Is.EqualTo(multicast));
                Assert.That((first & 2) != 0, Is.EqualTo(locallyAdministered));
                Assert.That(record.PhysicalAddress.GetAddressBytes(), Is.EqualTo(Bytes(record.MacAddress)));
            });
            index++;
        }
    }

    [Test]
    public void MacOutputsHonorConfiguredOutputLimits()
    {
        FakeOptions exactOptions = FakeOptions.Create(8406);
        exactOptions.MaximumOutputSize = 17;
        InternetProvider exact = Fake.Create(exactOptions).Internet();
        FakeOptions shortOptions = FakeOptions.Create(8407);
        shortOptions.MaximumOutputSize = 16;
        InternetProvider shortOutput = Fake.Create(shortOptions).Internet();

        Assert.That(exact.Mac(), Has.Length.EqualTo(17));
        Assert.That(exact.MacAddress().GetAddressBytes(), Has.Length.EqualTo(6));
        Assert.That(exact.NetworkInterface().MacAddress, Has.Length.EqualTo(17));
        Assert.That(() => shortOutput.Mac(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => shortOutput.MacAddress(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => shortOutput.NetworkInterface(), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static byte[] Bytes(string value)
    {
        string[] pieces = value.Split(':');
        byte[] result = new byte[pieces.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Convert.ToByte(pieces[index], 16);
            index++;
        }
        return result;
    }

    static string Compact(byte[] bytes, int start)
    {
        return string.Concat(bytes
            .Skip(start)
            .Select(value => value.ToString("x2")));
    }
}
