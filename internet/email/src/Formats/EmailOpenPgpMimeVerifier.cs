using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
internal class EmailOpenPgpOnePassData
{
    int signatureType;
    int hashAlgorithm;
    int publicKeyAlgorithm;
    byte[] identifier;
    byte[] salt;
    int nested;
    internal EmailOpenPgpOnePassData(int signatureType, int hashAlgorithm, int publicKeyAlgorithm, byte[] identifier, byte[] salt, int nested) { this.signatureType = signatureType; this.hashAlgorithm = hashAlgorithm; this.publicKeyAlgorithm = publicKeyAlgorithm; this.identifier = identifier; this.salt = salt; this.nested = nested; }
    internal int GetSignatureType() { return signatureType; }
    internal int GetHashAlgorithm() { return hashAlgorithm; }
    internal int GetPublicKeyAlgorithm() { return publicKeyAlgorithm; }
    internal byte[] GetIdentifier() { return new EmailOpenPgpSigningCredentialOperations().Copy(identifier); }
    internal byte[] GetSalt() { return new EmailOpenPgpSigningCredentialOperations().Copy(salt); }
    internal int GetNested() { return nested; }
}
internal class PgpTextLine
{
    int offset;
    byte[] data;
    internal PgpTextLine(int offset, byte[] data) { this.offset = offset; this.data = data; }
    internal int GetOffset() { return offset; }
    internal byte[] GetData() { return new EmailOpenPgpSigningCredentialOperations().Copy(data); }
}
