using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class EncryptedPackageOperationsTests
{
    [TestCase(XlsxPackageEncryptionKind.Standard, XlsxPackageEncryptionAlgorithm.Aes128)]
    [TestCase(XlsxPackageEncryptionKind.Standard, XlsxPackageEncryptionAlgorithm.Aes192)]
    [TestCase(XlsxPackageEncryptionKind.Standard, XlsxPackageEncryptionAlgorithm.Aes256)]
    [TestCase(XlsxPackageEncryptionKind.Agile, XlsxPackageEncryptionAlgorithm.Aes128)]
    [TestCase(XlsxPackageEncryptionKind.Agile, XlsxPackageEncryptionAlgorithm.Aes192)]
    [TestCase(XlsxPackageEncryptionKind.Agile, XlsxPackageEncryptionAlgorithm.Aes256)]
    public void EncryptDetectDecrypt_RoundTripsPackageBytes(XlsxPackageEncryptionKind kind, XlsxPackageEncryptionAlgorithm algorithm)
    {
        byte[] package = PackageBytes(9237);
        char[] password = "correct horse battery staple".ToCharArray();
        char[] passwordSnapshot = (char[])password.Clone();
        XlsxPackageEncryptionOptions options = new XlsxPackageEncryptionOptions();
        options.Kind = kind;
        options.Algorithm = algorithm;
        options.Password = password;
        options.SpinCount = 1000;
        XlsxEncryptedPackageOperations operations = new XlsxEncryptedPackageOperations();

        XlsxEncryptedPackageResult encrypted = operations.Encrypt(package, options);
        XlsxEncryptedPackageDetectionResult detected = operations.Detect(encrypted.Bytes);
        XlsxEncryptedPackageResult decrypted = operations.Decrypt(encrypted.Bytes, password);

        Assert.That(encrypted.IsValid, Is.True, Diagnostics(encrypted.Validation));
        Assert.That(XlsxEncryptionDetector.IsEncryptedPackageBytes(encrypted.Bytes), Is.True);
        XlsxCompoundFileResult compound = XlsxCompoundFile.Read(encrypted.Bytes);
        Assert.That(compound.Streams.ContainsKey("Version"), Is.True);
        Assert.That(compound.Streams.ContainsKey("DataSpaceMap"), Is.True);
        Assert.That(compound.Streams.ContainsKey("StrongEncryptionDataSpace"), Is.True);
        Assert.That(compound.Streams.ContainsKey("\x06Primary"), Is.True);
        Assert.That(detected.IsValid, Is.True, Diagnostics(detected.Validation));
        Assert.That(detected.EncryptionInfo.Kind, Is.EqualTo(kind));
        Assert.That(detected.EncryptionInfo.Algorithm, Is.EqualTo(algorithm));
        Assert.That(detected.EncryptionInfo.IsSupported, Is.True);
        Assert.That(decrypted.IsValid, Is.True, Diagnostics(decrypted.Validation));
        Assert.That(decrypted.PasswordVerified, Is.True);
        Assert.That(decrypted.IntegrityVerified, Is.EqualTo(kind == XlsxPackageEncryptionKind.Agile));
        Assert.That(decrypted.Bytes, Is.EqualTo(package));
        Assert.That(password, Is.EqualTo(passwordSnapshot));
    }

    [Test]
    public void Decrypt_WithWrongPassword_ReturnsExplicitFailureWithoutPlaintext()
    {
        XlsxEncryptedPackageOperations operations = new XlsxEncryptedPackageOperations();
        XlsxPackageEncryptionOptions options = AgileOptions("right password".ToCharArray());
        XlsxEncryptedPackageResult encrypted = operations.Encrypt(PackageBytes(5000), options);

        XlsxEncryptedPackageResult decrypted = operations.Decrypt(encrypted.Bytes, "wrong password".ToCharArray());

        Assert.That(decrypted.IsValid, Is.False);
        Assert.That(decrypted.PasswordVerified, Is.False);
        Assert.That(decrypted.Bytes, Is.Empty);
        Assert.That(HasDiagnostic(decrypted.Validation, "encryption.passwordInvalid"), Is.True);
    }

    [Test]
    public void Detect_UnsupportedStandardAlgorithm_ReturnsExplicitDiagnostic()
    {
        XlsxEncryptedPackageOperations operations = new XlsxEncryptedPackageOperations();
        XlsxPackageEncryptionOptions options = new XlsxPackageEncryptionOptions();
        options.Kind = XlsxPackageEncryptionKind.Standard;
        options.Algorithm = XlsxPackageEncryptionAlgorithm.Aes128;
        options.Password = "password".ToCharArray();
        XlsxEncryptedPackageResult encrypted = operations.Encrypt(PackageBytes(5000), options);
        XlsxCompoundFileResult compound = XlsxCompoundFile.Read(encrypted.Bytes);
        byte[] encryptionInfo = XlsxCompoundFile.GetStream(compound, "EncryptionInfo");
        XlsxCompoundFile.WriteInt32(encryptionInfo, 20, 0x6801);
        Dictionary<string, byte[]> streams = new Dictionary<string, byte[]>();
        streams["EncryptionInfo"] = encryptionInfo;
        streams["EncryptedPackage"] = XlsxCompoundFile.GetStream(compound, "EncryptedPackage");

        XlsxEncryptedPackageDetectionResult detected = operations.Detect(XlsxCompoundFile.Write(streams));

        Assert.That(detected.IsValid, Is.False);
        Assert.That(detected.EncryptionInfo.IsSupported, Is.False);
        Assert.That(detected.EncryptionInfo.Algorithm, Is.EqualTo(XlsxPackageEncryptionAlgorithm.Aes128));
        Assert.That(HasDiagnostic(detected.Validation, "encryption.algorithmUnsupported"), Is.True);
    }

    [Test]
    public void Decrypt_TamperedAgilePackage_FailsIntegrityVerification()
    {
        XlsxEncryptedPackageOperations operations = new XlsxEncryptedPackageOperations();
        XlsxEncryptedPackageResult encrypted = operations.Encrypt(PackageBytes(6000), AgileOptions("password".ToCharArray()));
        XlsxCompoundFileResult compound = XlsxCompoundFile.Read(encrypted.Bytes);
        byte[] encryptedPackage = XlsxCompoundFile.GetStream(compound, "EncryptedPackage");
        encryptedPackage[encryptedPackage.Length - 1] = (byte)(encryptedPackage[encryptedPackage.Length - 1] ^ 0x40);
        Dictionary<string, byte[]> streams = new Dictionary<string, byte[]>();
        streams["EncryptionInfo"] = XlsxCompoundFile.GetStream(compound, "EncryptionInfo");
        streams["EncryptedPackage"] = encryptedPackage;

        XlsxEncryptedPackageResult decrypted = operations.Decrypt(XlsxCompoundFile.Write(streams), "password".ToCharArray());

        Assert.That(decrypted.IsValid, Is.False);
        Assert.That(decrypted.PasswordVerified, Is.True);
        Assert.That(decrypted.IntegrityVerified, Is.False);
        Assert.That(decrypted.Bytes, Is.Empty);
        Assert.That(HasDiagnostic(decrypted.Validation, "encryption.integrityInvalid"), Is.True);
    }

    [Test]
    public void WorkbookOpen_WithPassword_DecryptsAgilePackage()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorkbookSaveResult saved = workbook.SaveBytes();
        XlsxEncryptedPackageOperations operations = new XlsxEncryptedPackageOperations();
        XlsxEncryptedPackageResult encrypted = operations.Encrypt(saved.Bytes, AgileOptions("open sesame".ToCharArray()));
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.Password = "open sesame";

        XlsxWorkbookOpenResult opened = XlsxWorkbook.OpenBytes(encrypted.Bytes, readerOptions);

        Assert.That(saved.IsValid, Is.True, Diagnostics(saved.Validation));
        Assert.That(encrypted.IsValid, Is.True, Diagnostics(encrypted.Validation));
        Assert.That(opened.IsValid, Is.True, Diagnostics(opened.Validation));
        Assert.That(opened.Workbook, Is.Not.Null);
    }

    [TestCase(XlsxPackageEncryptionKind.Standard)]
    [TestCase(XlsxPackageEncryptionKind.Agile)]
    public void WorkbookOpenSaveReopen_ProducesPlainValidWorkbookAfterEncryptedInput(XlsxPackageEncryptionKind kind)
    {
        XlsxWorkbookSaveResult original = XlsxWorkbook.Create().SaveBytes();
        XlsxPackageEncryptionOptions encryptionOptions = EncryptionOptions(kind, "save and reopen".ToCharArray());
        XlsxEncryptedPackageResult encrypted = new XlsxEncryptedPackageOperations().Encrypt(original.Bytes, encryptionOptions);
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.Password = "save and reopen";

        XlsxWorkbookOpenResult opened = XlsxWorkbook.OpenBytes(encrypted.Bytes, readerOptions);
        XlsxWorkbookSaveResult saved = opened.Workbook!.SaveBytes();
        XlsxWorkbookOpenResult reopened = XlsxWorkbook.OpenBytes(saved.Bytes);

        Assert.That(encrypted.IsValid, Is.True, Diagnostics(encrypted.Validation));
        Assert.That(opened.IsValid, Is.True, Diagnostics(opened.Validation));
        Assert.That(saved.IsValid, Is.True, Diagnostics(saved.Validation));
        Assert.That(XlsxEncryptionDetector.IsEncryptedPackageBytes(saved.Bytes), Is.False);
        Assert.That(reopened.IsValid, Is.True, Diagnostics(reopened.Validation));
        Assert.That(reopened.Workbook, Is.Not.Null);
    }

    [Test]
    public void DetectAndDecrypt_CfbMissingEncryptionDataSpace_ReturnExplicitFailure()
    {
        XlsxEncryptedPackageOperations operations = new XlsxEncryptedPackageOperations();
        XlsxEncryptedPackageResult encrypted = operations.Encrypt(PackageBytes(5000), AgileOptions("password".ToCharArray()));
        byte[] malformed = (byte[])encrypted.Bytes.Clone();
        byte[] streamName = Encoding.Unicode.GetBytes("DataSpaceMap\0");
        int streamNameOffset = FindSequence(malformed, streamName);
        Assert.That(streamNameOffset, Is.GreaterThanOrEqualTo(0));
        malformed[streamNameOffset] = (byte)'X';

        XlsxEncryptedPackageDetectionResult detected = operations.Detect(malformed);
        XlsxEncryptedPackageResult decrypted = operations.Decrypt(malformed, "password".ToCharArray());

        Assert.That(detected.IsValid, Is.False);
        Assert.That(detected.EncryptionInfo.Kind, Is.EqualTo(XlsxPackageEncryptionKind.Agile));
        Assert.That(HasDiagnostic(detected.Validation, "cfb.encryptionDataSpacesMissing"), Is.True);
        Assert.That(decrypted.IsValid, Is.False);
        Assert.That(decrypted.Bytes, Is.Empty);
        Assert.That(HasDiagnostic(decrypted.Validation, "cfb.encryptionDataSpacesMissing"), Is.True);
    }

    [Test]
    public void Decrypt_TamperedStandardPackageVerifiesPasswordButCannotVerifyIntegrity()
    {
        XlsxEncryptedPackageOperations operations = new XlsxEncryptedPackageOperations();
        XlsxPackageEncryptionOptions options = EncryptionOptions(XlsxPackageEncryptionKind.Standard, "password".ToCharArray());
        byte[] package = PackageBytes(5000);
        XlsxEncryptedPackageResult encrypted = operations.Encrypt(package, options);
        XlsxCompoundFileResult compound = XlsxCompoundFile.Read(encrypted.Bytes);
        byte[] encryptedPackage = XlsxCompoundFile.GetStream(compound, "EncryptedPackage");
        encryptedPackage[8] = (byte)(encryptedPackage[8] ^ 0x40);
        Dictionary<string, byte[]> streams = new Dictionary<string, byte[]>();
        streams["EncryptionInfo"] = XlsxCompoundFile.GetStream(compound, "EncryptionInfo");
        streams["EncryptedPackage"] = encryptedPackage;

        XlsxEncryptedPackageResult decrypted = operations.Decrypt(XlsxCompoundFile.Write(streams), "password".ToCharArray());

        Assert.That(decrypted.IsValid, Is.True, Diagnostics(decrypted.Validation));
        Assert.That(decrypted.PasswordVerified, Is.True);
        Assert.That(decrypted.IntegrityVerified, Is.False);
        Assert.That(decrypted.Bytes, Is.Not.EqualTo(package));
    }

    [Test]
    public void CompoundFileWriter_RoundTripsStreamsThatRequireDifatSectors()
    {
        byte[] large = PackageBytes(8 * 1024 * 1024);
        Dictionary<string, byte[]> streams = new Dictionary<string, byte[]>();
        streams["LargeData"] = large;

        byte[] bytes = XlsxCompoundFile.Write(streams);
        XlsxCompoundFileResult read = XlsxCompoundFile.Read(bytes);

        Assert.That(read.IsValid, Is.True, Diagnostics(read.Validation));
        Assert.That(XlsxCompoundFile.ReadInt32(bytes, 72), Is.GreaterThan(0));
        Assert.That(XlsxCompoundFile.GetStream(read, "LargeData"), Is.EqualTo(large));
    }

    static XlsxPackageEncryptionOptions AgileOptions(char[] password)
    {
        XlsxPackageEncryptionOptions options = new XlsxPackageEncryptionOptions();
        options.Password = password;
        options.SpinCount = 1000;
        return options;
    }

    static XlsxPackageEncryptionOptions EncryptionOptions(XlsxPackageEncryptionKind kind, char[] password)
    {
        XlsxPackageEncryptionOptions options = new XlsxPackageEncryptionOptions();
        options.Kind = kind;
        options.Password = password;
        options.SpinCount = 1000;
        return options;
    }

    static int FindSequence(byte[] bytes, byte[] sequence)
    {
        int offset = 0;
        while (offset <= bytes.Length - sequence.Length)
        {
            int index = 0;
            while (index < sequence.Length && bytes[offset + index] == sequence[index])
            {
                index = index + 1;
            }
            if (index == sequence.Length)
            {
                return offset;
            }
            offset = offset + 1;
        }
        return -1;
    }

    static byte[] PackageBytes(int length)
    {
        byte[] bytes = new byte[length];
        bytes[0] = 0x50;
        bytes[1] = 0x4B;
        int index = 2;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(index * 31 + 7);
            index = index + 1;
        }
        return bytes;
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        string result = "";
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result = result + diagnostic.Code + ": " + diagnostic.Message + Environment.NewLine;
        }
        return result;
    }
}
