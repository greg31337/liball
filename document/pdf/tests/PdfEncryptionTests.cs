using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfEncryptionTests
{
    [Test]
    public void UseStandardEncryption_StoresPasswordsAndPermissions()
    {
        PdfDocument document = new PdfDocument();
        PdfPermissions permissions = new PdfPermissions();

        permissions.DisallowModification();
        permissions.DisallowCopying();
        document.UseStandardEncryptionWithPermissions("user", "owner", permissions);

        Assert.That(document.HasEncryption, Is.True);
        Assert.That(document.EncryptionOptions.UserPassword, Is.EqualTo("user"));
        Assert.That(document.EncryptionOptions.OwnerPassword, Is.EqualTo("owner"));
        Assert.That(document.EncryptionOptions.Permissions.ToPdfPermissionValue(), Is.EqualTo(-28));
    }

    [Test]
    public void PermissionsComputeExpectedPdfPermissionValue()
    {
        PdfPermissions permissions = new PdfPermissions();

        permissions.DisallowPrinting();
        permissions.DisallowModification();
        permissions.DisallowCopying();
        permissions.DisallowAnnotations();

        Assert.That(permissions.ToPdfPermissionValue(), Is.EqualTo(-64));
    }

    [Test]
    public void PermissionsComputeEverySupportedCombination()
    {
        for (int mask = 0; mask < 16; mask++)
        {
            PdfPermissions permissions = new PdfPermissions();
            if ((mask & 1) == 0)
            {
                permissions.DisallowPrinting();
            }

            if ((mask & 2) == 0)
            {
                permissions.DisallowModification();
            }

            if ((mask & 4) == 0)
            {
                permissions.DisallowCopying();
            }

            if ((mask & 8) == 0)
            {
                permissions.DisallowAnnotations();
            }

            int expected = -64;
            if ((mask & 1) != 0)
            {
                expected = expected | 4;
            }

            if ((mask & 2) != 0)
            {
                expected = expected | 8;
            }

            if ((mask & 4) != 0)
            {
                expected = expected | 16;
            }

            if ((mask & 8) != 0)
            {
                expected = expected | 32;
            }

            Assert.That(permissions.ToPdfPermissionValue(), Is.EqualTo(expected), "permission mask " + mask.ToString());
        }
    }

    [Test]
    public void Save_WritesEncryptionDictionaryTrailerReferenceAndDocumentId()
    {
        PdfDocument document = CreateEncryptedDocument();
        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        string encryptionBody = GetObjectBody(pdf, 5);

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/Type /Catalog /Pages 2 0 R"));
        Assert.That(encryptionBody, Does.Contain("/Filter /Standard /V 1 /R 2 /Length 40"));
        Assert.That(encryptionBody, Does.Contain("/P -28"));
        Assert.That(Regex.IsMatch(encryptionBody, @"/O <[0-9A-F]{64}>"), Is.True);
        Assert.That(Regex.IsMatch(encryptionBody, @"/U <[0-9A-F]{64}>"), Is.True);
        Assert.That(pdf, Does.Contain("trailer\n<< /Size 8 /Root 1 0 R /Encrypt 5 0 R /ID [<00112233445566778899AABBCCDDEEFF><00112233445566778899AABBCCDDEEFF>] >>"));
    }

    [Test]
    public void Save_EncryptsContentStreams()
    {
        PdfDocument plain = new PdfDocument();
        PdfPage plainPage = plain.AddPage(200, 100);
        plainPage.Canvas.DrawText("Secret text", 10, 50, 12);

        PdfDocument encrypted = CreateEncryptedDocument();
        string plainPdf = Encoding.ASCII.GetString(plain.ToBytes());
        string encryptedPdf = Encoding.ASCII.GetString(encrypted.ToBytes());

        Assert.That(plainPdf, Does.Contain("Secret text"));
        Assert.That(encryptedPdf, Does.Not.Contain("Secret text"));
        Assert.That(GetObjectBody(encryptedPdf, 7), Does.Contain("stream\n"));
    }

    [Test]
    public void Save_EncryptionProducesStableBytesForDeterministicDocuments()
    {
        PdfDocument first = CreateEncryptedDocument();
        PdfDocument second = CreateEncryptedDocument();

        Assert.That(second.ToBytes(), Is.EqualTo(first.ToBytes()));
    }

    [Test]
    public void Save_EncryptionUsesFreshDocumentIdsOutsideDeterministicMode()
    {
        PdfDocument first = CreateEncryptedDocumentWithoutDeterministicMode();
        PdfDocument second = CreateEncryptedDocumentWithoutDeterministicMode();

        byte[] firstBytes = first.ToBytes();
        byte[] secondBytes = second.ToBytes();
        string firstPdf = Encoding.ASCII.GetString(firstBytes);
        string secondPdf = Encoding.ASCII.GetString(secondBytes);

        Assert.That(secondBytes, Is.Not.EqualTo(firstBytes));
        Assert.That(GetDocumentId(secondPdf), Is.Not.EqualTo(GetDocumentId(firstPdf)));
        Assert.That(GetDocumentId(firstPdf), Does.Match("^[0-9A-F]{32}$"));
    }

    [Test]
    public void EmptyOwnerPasswordUsesUserPasswordForOwnerAuthenticationKey()
    {
        PdfDocument emptyOwner = CreateEncryptedDocumentWithPasswords("user", "");
        PdfDocument repeatedUser = CreateEncryptedDocumentWithPasswords("user", "user");

        Assert.That(emptyOwner.ToBytes(), Is.EqualTo(repeatedUser.ToBytes()));
    }

    [Test]
    public void EncryptionOptionsValidateSupportedPasswordsAndMutableState()
    {
        PdfPermissions permissions = new PdfPermissions();

        Assert.Throws<ArgumentException>(() => new PdfEncryptionOptions(new string('a', 33), "owner", permissions));
        Assert.Throws<ArgumentException>(() => new PdfEncryptionOptions("user", "caf\u00e9", permissions));

        PdfEncryptionOptions valid = new PdfEncryptionOptions(new string('u', 32), string.Empty, permissions);
        PdfDocument document = new PdfDocument();
        document.AddPage(100, 100);
        document.UseStandardEncryptionWithOptions(valid);
        Assert.DoesNotThrow(() => document.ToBytes());

        valid.OwnerPassword = null!;
        Assert.Throws<ArgumentNullException>(() => document.ToBytes());

        PdfEncryptionOptions invalidBeforeUse = new PdfEncryptionOptions("user", "owner", permissions);
        invalidBeforeUse.Permissions = null!;
        Assert.Throws<ArgumentNullException>(() => new PdfDocument().UseStandardEncryptionWithOptions(invalidBeforeUse));
    }

    [Test]
    public void EncryptionRejectsInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPermissions permissions = new PdfPermissions();

        Assert.Throws<ArgumentNullException>(() => document.UseStandardEncryptionWithPermissions(null!, "owner", permissions));
        Assert.Throws<ArgumentNullException>(() => document.UseStandardEncryptionWithPermissions("user", null!, permissions));
        Assert.Throws<ArgumentNullException>(() => document.UseStandardEncryptionWithPermissions("user", "owner", null!));
        Assert.Throws<ArgumentNullException>(() => document.UseStandardEncryptionWithOptions(null!));
        Assert.Throws<ArgumentException>(() => document.UseStandardEncryption(new string('x', 33), "owner"));
        Assert.Throws<ArgumentException>(() => document.UseStandardEncryption("user", "\u2603"));
    }

    static PdfDocument CreateEncryptedDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfPermissions permissions = new PdfPermissions();
        PdfPage page = document.AddPage(200, 100);

        permissions.DisallowModification();
        permissions.DisallowCopying();
        document.UseDeterministicWriterModeWithId("00112233445566778899aabbccddeeff");
        document.UseStandardEncryptionWithPermissions("user", "owner", permissions);
        page.Canvas.DrawText("Secret text", 10, 50, 12);
        return document;
    }

    static PdfDocument CreateEncryptedDocumentWithoutDeterministicMode()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        page.Canvas.DrawText("Secret text", 10, 50, 12);
        document.UseStandardEncryption("user", "owner");
        return document;
    }

    static PdfDocument CreateEncryptedDocumentWithPasswords(string userPassword, string ownerPassword)
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        page.Canvas.DrawText("Secret text", 10, 50, 12);
        document.UseDeterministicWriterModeWithId("00112233445566778899aabbccddeeff");
        document.UseStandardEncryption(userPassword, ownerPassword);
        return document;
    }

    static string GetDocumentId(string pdf)
    {
        Match match = Regex.Match(pdf, @"/ID \[<(?<id>[0-9A-F]{32})><[0-9A-F]{32}>\]");
        Assert.That(match.Success, Is.True);
        return match.Groups["id"].Value;
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
