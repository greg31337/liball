using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
internal class EmailOpenPgpMpiRead
{
    bool success;
    int position;
    byte[]? value;

    EmailOpenPgpMpiRead(bool success, int position, byte[]? value)
    {
        this.success = success;
        this.position = position;
        this.value = value;
    }

    internal static EmailOpenPgpMpiRead Success(int position, byte[] value) { return new EmailOpenPgpMpiRead(true, position, value); }
    internal static EmailOpenPgpMpiRead Failure(int position) { return new EmailOpenPgpMpiRead(false, position, null); }
    internal bool IsSuccess() { return success; }
    internal int GetPosition() { return position; }
    internal byte[]? GetValue() { return value; }
}
