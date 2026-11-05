using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace email;
internal class EmailOpenPgpDecryptionAttempt
{
    byte[]? plaintext;
    EmailOpenPgpDecryptionStatus failureStatus;

    internal EmailOpenPgpDecryptionAttempt(byte[]? plaintext, EmailOpenPgpDecryptionStatus failureStatus) { this.plaintext = plaintext; this.failureStatus = failureStatus; }
    internal static EmailOpenPgpDecryptionAttempt Success(byte[] plaintext) { return new EmailOpenPgpDecryptionAttempt(plaintext, EmailOpenPgpDecryptionStatus.Success); }
    internal static EmailOpenPgpDecryptionAttempt Failure(EmailOpenPgpDecryptionStatus status) { return new EmailOpenPgpDecryptionAttempt(null, status); }
    internal bool IsSuccess() { return plaintext != null; }
    internal byte[]? GetPlaintext() { return plaintext; }
    internal EmailOpenPgpDecryptionStatus GetFailureStatus() { return failureStatus; }
}
