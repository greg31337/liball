using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace pdf;

internal class PdfEncryption
{
    internal static PdfEncryptionData Create(string userPassword, string ownerPassword, int permissions, string documentIdHex)
    {
        PdfEncryptionData data = new PdfEncryptionData();
        byte[] ownerKey = CreateOwnerKey(userPassword, ownerPassword);
        byte[] fileKey = CreateFileKey(userPassword, ownerKey, permissions, documentIdHex);
        byte[] userKey = Rc4(fileKey, GetPasswordPadding());

        data.FileKey = fileKey;
        data.OwnerKey = ownerKey;
        data.UserKey = userKey;
        data.Permissions = permissions;
        return data;
    }

    internal static byte[] EncryptObjectBytes(byte[] bytes, PdfEncryptionData data, int objectId)
    {
        byte[] objectKey = CreateObjectKey(data.FileKey, objectId);
        return Rc4(objectKey, bytes);
    }

    internal static string ToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes);
    }

    internal static byte[] CreateOwnerKey(string userPassword, string ownerPassword)
    {
        string effectiveOwnerPassword = ownerPassword.Length == 0 ? userPassword : ownerPassword;
        byte[] paddedOwner = PadPassword(effectiveOwnerPassword);
        byte[] ownerHash = Md5(paddedOwner);
        byte[] ownerKey = CopyPrefix(ownerHash, 5);
        byte[] paddedUser = PadPassword(userPassword);

        return Rc4(ownerKey, paddedUser);
    }

    internal static byte[] CreateFileKey(string userPassword, byte[] ownerKey, int permissions, string documentIdHex)
    {
        byte[] paddedUser = PadPassword(userPassword);
        byte[] permissionBytes = Int32LittleEndian(permissions);
        byte[] documentId = HexToBytes(documentIdHex);
        byte[] input = new byte[paddedUser.Length + ownerKey.Length + permissionBytes.Length + documentId.Length];
        int offset = 0;

        CopyBytes(paddedUser, input, offset);
        offset += paddedUser.Length;
        CopyBytes(ownerKey, input, offset);
        offset += ownerKey.Length;
        CopyBytes(permissionBytes, input, offset);
        offset += permissionBytes.Length;
        CopyBytes(documentId, input, offset);

        return CopyPrefix(Md5(input), 5);
    }

    internal static byte[] CreateObjectKey(byte[] fileKey, int objectId)
    {
        byte[] input = new byte[fileKey.Length + 5];
        CopyBytes(fileKey, input, 0);
        input[fileKey.Length] = (byte)(objectId & 255);
        input[fileKey.Length + 1] = (byte)((objectId >> 8) & 255);
        input[fileKey.Length + 2] = (byte)((objectId >> 16) & 255);
        input[fileKey.Length + 3] = 0;
        input[fileKey.Length + 4] = 0;

        return CopyPrefix(Md5(input), fileKey.Length + 5);
    }

    internal static byte[] PadPassword(string password)
    {
        byte[] padding = GetPasswordPadding();
        byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
        byte[] padded = new byte[32];
        int index = 0;

        while (index < 32)
        {
            if (index < passwordBytes.Length)
            {
                padded[index] = passwordBytes[index];
            }
            else
            {
                padded[index] = padding[index - passwordBytes.Length];
            }

            index++;
        }

        return padded;
    }

    internal static byte[] GetPasswordPadding()
    {
        return new byte[]
        {
            40, 191, 78, 94, 78, 117, 138, 65,
            100, 0, 78, 86, 255, 250, 1, 8,
            46, 46, 0, 182, 208, 104, 62, 128,
            47, 12, 169, 254, 100, 83, 105, 122
        };
    }

    internal static byte[] Md5(byte[] bytes)
    {
        using (MD5 md5 = MD5.Create())
        {
            return md5.ComputeHash(bytes);
        }
    }

    internal static byte[] Rc4(byte[] key, byte[] data)
    {
        byte[] state = new byte[256];
        byte[] output = new byte[data.Length];
        int index = 0;

        while (index < 256)
        {
            state[index] = (byte)index;
            index++;
        }

        int j = 0;
        index = 0;
        while (index < 256)
        {
            j = (j + state[index] + key[index % key.Length]) & 255;
            Swap(state, index, j);
            index++;
        }

        int i = 0;
        j = 0;
        index = 0;
        while (index < data.Length)
        {
            i = (i + 1) & 255;
            j = (j + state[i]) & 255;
            Swap(state, i, j);
            int keyIndex = (state[i] + state[j]) & 255;
            output[index] = (byte)(data[index] ^ state[keyIndex]);
            index++;
        }

        return output;
    }

    internal static void Swap(byte[] bytes, int first, int second)
    {
        byte value = bytes[first];
        bytes[first] = bytes[second];
        bytes[second] = value;
    }

    internal static byte[] CopyPrefix(byte[] bytes, int count)
    {
        byte[] output = new byte[count];
        int index = 0;

        while (index < count)
        {
            output[index] = bytes[index];
            index++;
        }

        return output;
    }

    internal static void CopyBytes(byte[] source, byte[] destination, int offset)
    {
        for (int index = 0; index < source.Length; index++)
        {
            destination[offset + index] = source[index];
        }
    }

    internal static byte[] Int32LittleEndian(int value)
    {
        byte[] bytes = new byte[4];
        bytes[0] = (byte)(value & 255);
        bytes[1] = (byte)((value >> 8) & 255);
        bytes[2] = (byte)((value >> 16) & 255);
        bytes[3] = (byte)((value >> 24) & 255);
        return bytes;
    }

    internal static byte[] HexToBytes(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int index = 0; index < bytes.Length; index++)
        {
            string value = hex.Substring(index * 2, 2);
            bytes[index] = byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }
}
