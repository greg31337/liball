using System.Net.NetworkInformation;

namespace fake;

internal class MacAddressValue
{
    internal byte[] Bytes = new byte[0];
    internal string Text = string.Empty;
    internal PhysicalAddress Address = new PhysicalAddress(new byte[0]);

    internal MacAddressValue()
    {
    }
}

internal static class MacAddressGenerator
{
    const string Hexadecimal = "0123456789abcdef";

    internal static string Text(
        FakeRandom random,
        bool multicast,
        bool locallyAdministered,
        int maximumOutputSize)
    {
        return Value(
            random,
            multicast,
            locallyAdministered,
            maximumOutputSize).Text;
    }

    internal static PhysicalAddress Address(
        FakeRandom random,
        bool multicast,
        bool locallyAdministered,
        int maximumOutputSize)
    {
        return Value(
            random,
            multicast,
            locallyAdministered,
            maximumOutputSize).Address;
    }

    internal static NetworkInterfaceRecord Record(
        FakeRandom random,
        bool multicast,
        bool locallyAdministered,
        int maximumOutputSize)
    {
        MacAddressValue value = Value(
            random,
            multicast,
            locallyAdministered,
            maximumOutputSize);
        NetworkInterfaceType interfaceType = random.Bool()
            ? NetworkInterfaceType.Ethernet
            : NetworkInterfaceType.Wireless80211;
        string prefix = interfaceType == NetworkInterfaceType.Ethernet
            ? "eth-"
            : "wlan-";
        string name = prefix + CompactSuffix(value.Bytes);
        Guard.OutputSize(name.Length, maximumOutputSize, "networkInterface");
        NetworkInterfaceRecord result = new NetworkInterfaceRecord();
        result.Name = name;
        result.InterfaceType = interfaceType;
        result.MacAddress = value.Text;
        result.PhysicalAddress = value.Address;
        result.IsUnicast = !multicast;
        result.IsMulticast = multicast;
        result.IsLocallyAdministered = locallyAdministered;
        result.IsUniversallyAdministered = !locallyAdministered;
        return result;
    }

    static MacAddressValue Value(
        FakeRandom random,
        bool multicast,
        bool locallyAdministered,
        int maximumOutputSize)
    {
        ulong bits = random.ULong();
        byte[] bytes = new byte[6];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)((bits >> (index * 8)) & byte.MaxValue);
            index = index + 1;
        }
        bytes[0] = (byte)(bytes[0] & 0xfc);
        if (multicast)
        {
            bytes[0] = (byte)(bytes[0] | 1);
        }
        if (locallyAdministered)
        {
            bytes[0] = (byte)(bytes[0] | 2);
        }
        string text = Format(bytes);
        Guard.OutputSize(text.Length, maximumOutputSize, "mac");
        MacAddressValue result = new MacAddressValue();
        result.Bytes = bytes;
        result.Text = text;
        result.Address = new PhysicalAddress(bytes);
        return result;
    }

    static string Format(byte[] bytes)
    {
        char[] result = new char[17];
        int index = 0;
        while (index < bytes.Length)
        {
            int outputIndex = index * 3;
            if (index != 0)
            {
                result[outputIndex - 1] = ':';
            }
            result[outputIndex] = Hexadecimal[bytes[index] >> 4];
            result[outputIndex + 1] = Hexadecimal[bytes[index] & 15];
            index = index + 1;
        }
        return new string(result);
    }

    static string CompactSuffix(byte[] bytes)
    {
        char[] result = new char[6];
        int index = 3;
        while (index < bytes.Length)
        {
            int outputIndex = (index - 3) * 2;
            result[outputIndex] = Hexadecimal[bytes[index] >> 4];
            result[outputIndex + 1] = Hexadecimal[bytes[index] & 15];
            index = index + 1;
        }
        return new string(result);
    }
}
