using System.Security.Cryptography;
using System.Text;
using token;

namespace token.Tests;

public class TokenAlgorithmServicePublicTests
{
    [Test]
    public void PublicServiceMatchesPublishedHmacAesWrapGcmPbes2AndCoseKdfVectors()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] hmacData = Encoding.ASCII.GetBytes(
            "Test Using Larger Than Block-Size Key - Hash Key First");
        TokenKey hmacKey = SymmetricBytes(
            Enumerable.Repeat((byte)0xAA, 131).ToArray(),
            JoseAlgorithms.HmacSha256);
        TokenBytesResult hmac = service.SignJoseHmac(
            hmacData,
            hmacKey,
            JoseAlgorithms.HmacSha256);

        byte[] keyMaterial = Convert.FromHexString(
            "00112233445566778899AABBCCDDEEFF");
        TokenKey wrapKey = SymmetricBytes(
            Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
            JoseAlgorithms.Aes128KeyWrap);
        TokenBytesResult wrapped = service.WrapAesKey(
            keyMaterial,
            wrapKey,
            JoseAlgorithms.Aes128KeyWrap);

        TokenKey gcmKey = SymmetricBytes(new byte[16], JoseAlgorithms.Aes128Gcm);
        AesGcmContentEncryptionResult gcm =
            service.EncryptAesGcmWithInitializationVector(
                new byte[16],
                new byte[12],
                Array.Empty<byte>(),
                gcmKey,
                JoseAlgorithms.Aes128Gcm);

        byte[] password = Encoding.UTF8.GetBytes(
            "Thus from my lips, by yours, my sin is purged.");
        TokenBytesResult pbes2 = service.UnwrapPbes2Key(
            Convert.FromHexString(
                "4EBA973B0B8D51F0D5F553D335BC86BC427D24C8DE7C0567F93475B88C51F69E"
                + "A1B11421F5393B04"),
            password,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            Convert.FromHexString("D9609370967546F77F089B89AE2A50D7"),
            4096);

        TokenKey coseKdfKey = CoseSymmetricBytes(
            Convert.FromHexString(
                "849B57219DAE48DE646D07DBB533566E976686457C1491BE3A76DCEA6C427188"),
            CoseAlgorithms.DirectHkdfSha256);
        CoseKdfContextParameters context = new CoseKdfContextParameters(
            CoseAlgorithms.AesCcm16_64_128,
            128);
        context.ProtectedParameters = Convert.FromHexString("A10129");
        CoseDirectKeyDistributionResult coseKdf =
            service.DistributeCoseDirectKey(
                coseKdfKey,
                CoseAlgorithms.DirectHkdfSha256,
                Convert.FromHexString("61616262636364646565666667676868"),
                context);

        Assert.Multiple(() =>
        {
            Assert.That(hmac.Bytes, Is.EqualTo(Convert.FromHexString(
                "60E431591EE0B67F0D8A26AACBF5B77F8E0BC6213728C5140546040F0EE37F54")));
            Assert.That(wrapped.Bytes, Is.EqualTo(Convert.FromHexString(
                "1FA68B0A8112B447AEF34BD8FB5A7B829D3E862371D2CFE5")));
            Assert.That(gcm.Ciphertext, Is.EqualTo(Convert.FromHexString(
                "0388DACE60B6A392F328C2B971B2FE78")));
            Assert.That(gcm.AuthenticationTag, Is.EqualTo(Convert.FromHexString(
                "AB6E47D42CEC13BDF53A67B21257BDDF")));
            Assert.That(pbes2.Bytes, Is.EqualTo(Convert.FromHexString(
                "6F1B1934421D144E5CB038F041D05270A1832437CAECB9AC811799C2C330FDB6")));
            Assert.That(coseKdf.ContentKey, Is.EqualTo(Convert.FromHexString(
                "32547753D1E24F41579D770BA852D4C9")));
        });
    }

    [Test]
    public void PublicServiceSupportsEveryBuiltInSignatureAndMacVariant()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] data = Encoding.ASCII.GetBytes("all public signature algorithms");
        string[] hmacAlgorithms = new string[]
        {
            JoseAlgorithms.HmacSha256,
            JoseAlgorithms.HmacSha384,
            JoseAlgorithms.HmacSha512
        };
        int[] hmacSizes = new int[] { 32, 48, 64 };
        int index = 0;
        while (index < hmacAlgorithms.Length)
        {
            TokenKey key = Symmetric(hmacSizes[index], 11, hmacAlgorithms[index]);
            TokenBytesResult signature = service.SignJoseHmac(data, key, hmacAlgorithms[index]);
            Assert.That(
                service.VerifyJoseHmac(data, signature.Bytes!, key, hmacAlgorithms[index]).Success,
                Is.True,
                hmacAlgorithms[index]);
            index++;
        }

        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKey(rsa);
        string[] joseRsaAlgorithms = new string[]
        {
            JoseAlgorithms.RsaSha256,
            JoseAlgorithms.RsaSha384,
            JoseAlgorithms.RsaSha512,
            JoseAlgorithms.RsaPssSha256,
            JoseAlgorithms.RsaPssSha384,
            JoseAlgorithms.RsaPssSha512
        };
        foreach (string algorithm in joseRsaAlgorithms)
        {
            TokenBytesResult signature = service.SignJoseRsa(data, rsaKey, algorithm);
            Assert.That(
                service.VerifyJoseRsa(data, signature.Bytes!, rsaKey, algorithm).Success,
                Is.True,
                algorithm);
        }

        int[] coseRsaAlgorithms = new int[]
        {
            CoseAlgorithms.RsaSha256,
            CoseAlgorithms.RsaSha384,
            CoseAlgorithms.RsaSha512,
            CoseAlgorithms.RsaPssSha256,
            CoseAlgorithms.RsaPssSha384,
            CoseAlgorithms.RsaPssSha512
        };
        foreach (int algorithm in coseRsaAlgorithms)
        {
            TokenBytesResult signature = service.SignCoseRsa(data, rsaKey, algorithm);
            Assert.That(
                service.VerifyCoseRsa(data, signature.Bytes!, rsaKey, algorithm).Success,
                Is.True,
                algorithm.ToString());
        }

        string[] joseEcAlgorithms = new string[]
        {
            JoseAlgorithms.EcdsaSha256,
            JoseAlgorithms.EcdsaSha384,
            JoseAlgorithms.EcdsaSha512
        };
        int[] coseEcAlgorithms = new int[]
        {
            CoseAlgorithms.EcdsaP256Sha256,
            CoseAlgorithms.EcdsaP384Sha384,
            CoseAlgorithms.EcdsaP521Sha512
        };
        string[] curves = new string[] { "P-256", "P-384", "P-521" };
        index = 0;
        while (index < curves.Length)
        {
            using ECDsa ecdsa = CreateCurve(curves[index]);
            TokenKey key = EcKeyForCurve(ecdsa, curves[index]);
            TokenBytesResult jose = service.SignJoseEcdsa(data, key, joseEcAlgorithms[index]);
            TokenBytesResult cose = service.SignCoseEcdsa(
                data,
                key,
                coseEcAlgorithms[index],
                false);
            Assert.Multiple(() =>
            {
                Assert.That(service.VerifyJoseEcdsa(
                    data,
                    jose.Bytes!,
                    key,
                    joseEcAlgorithms[index]).Success,
                    Is.True,
                    joseEcAlgorithms[index]);
                Assert.That(service.VerifyCoseEcdsa(
                    data,
                    cose.Bytes!,
                    key,
                    coseEcAlgorithms[index],
                    false).Success,
                    Is.True,
                    coseEcAlgorithms[index].ToString());
            });
            index++;
        }

        int[] coseHmacAlgorithms = new int[]
        {
            CoseAlgorithms.HmacSha256Truncated64,
            CoseAlgorithms.HmacSha256,
            CoseAlgorithms.HmacSha384,
            CoseAlgorithms.HmacSha512
        };
        int[] coseHmacSizes = new int[] { 32, 32, 48, 64 };
        int[] coseTagSizes = new int[] { 8, 32, 48, 64 };
        index = 0;
        while (index < coseHmacAlgorithms.Length)
        {
            TokenKey key = CoseSymmetric(
                coseHmacSizes[index],
                21,
                coseHmacAlgorithms[index]);
            TokenBytesResult tag = service.CreateCoseHmac(
                data,
                key,
                coseHmacAlgorithms[index]);
            Assert.Multiple(() =>
            {
                Assert.That(tag.Bytes, Has.Length.EqualTo(coseTagSizes[index]));
                Assert.That(service.VerifyCoseHmac(
                    data,
                    tag.Bytes!,
                    key,
                    coseHmacAlgorithms[index]).Success,
                    Is.True,
                    coseHmacAlgorithms[index].ToString());
            });
            index++;
        }
    }

    [Test]
    public void JoseAndCoseSignatureAndMacOperationsRoundTrip()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] data = Encoding.ASCII.GetBytes("public algorithm service");
        TokenKey hmacKey = Symmetric(32, 7, JoseAlgorithms.HmacSha256);
        TokenBytesResult hmac = service.SignJoseHmac(
            data,
            hmacKey,
            JoseAlgorithms.HmacSha256);
        TokenResult hmacVerified = service.VerifyJoseHmac(
            data,
            hmac.Bytes!,
            hmacKey,
            JoseAlgorithms.HmacSha256);

        TokenKey coseMacKey = CoseSymmetric(
            32,
            9,
            CoseAlgorithms.HmacSha256);
        TokenBytesResult tag = service.CreateCoseHmac(
            data,
            coseMacKey,
            CoseAlgorithms.HmacSha256);
        TokenResult tagVerified = service.VerifyCoseHmac(
            data,
            tag.Bytes!,
            coseMacKey,
            CoseAlgorithms.HmacSha256);

        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKey(rsa);
        TokenBytesResult joseRsa = service.SignJoseRsa(
            data,
            rsaKey,
            JoseAlgorithms.RsaSha256);
        TokenResult joseRsaVerified = service.VerifyJoseRsa(
            data,
            joseRsa.Bytes!,
            rsaKey,
            JoseAlgorithms.RsaSha256);
        TokenBytesResult coseRsa = service.SignCoseRsa(
            data,
            rsaKey,
            CoseAlgorithms.RsaSha256);
        TokenResult coseRsaVerified = service.VerifyCoseRsa(
            data,
            coseRsa.Bytes!,
            rsaKey,
            CoseAlgorithms.RsaSha256);

        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey ecKey = EcKey(ecdsa);
        TokenBytesResult joseEc = service.SignJoseEcdsa(
            data,
            ecKey,
            JoseAlgorithms.EcdsaSha256);
        TokenResult joseEcVerified = service.VerifyJoseEcdsa(
            data,
            joseEc.Bytes!,
            ecKey,
            JoseAlgorithms.EcdsaSha256);
        TokenBytesResult coseEc = service.SignCoseEcdsa(
            data,
            ecKey,
            CoseAlgorithms.EcdsaP256Sha256,
            false);
        TokenResult coseEcVerified = service.VerifyCoseEcdsa(
            data,
            coseEc.Bytes!,
            ecKey,
            CoseAlgorithms.EcdsaP256Sha256,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(hmac.Success, Is.True);
            Assert.That(hmacVerified.Success, Is.True);
            Assert.That(tag.Success, Is.True);
            Assert.That(tagVerified.Success, Is.True);
            Assert.That(joseRsaVerified.Success, Is.True);
            Assert.That(coseRsaVerified.Success, Is.True);
            Assert.That(joseEcVerified.Success, Is.True);
            Assert.That(coseEcVerified.Success, Is.True);
        });
    }

    [Test]
    public void JoseKeyManagementOperationsRoundTrip()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] contentKey = Sequence(16, 1);

        TokenKey aesKey = Symmetric(16, 40, JoseAlgorithms.Aes128KeyWrap);
        TokenBytesResult wrapped = service.WrapAesKey(
            contentKey,
            aesKey,
            JoseAlgorithms.Aes128KeyWrap);
        TokenBytesResult unwrapped = service.UnwrapAesKey(
            wrapped.Bytes!,
            aesKey,
            JoseAlgorithms.Aes128KeyWrap);

        TokenKey gcmKey = Symmetric(
            16,
            50,
            JoseAlgorithms.Aes128GcmKeyWrap);
        AesGcmKeyWrapResult gcmWrapped = service.WrapAesGcmKey(
            contentKey,
            gcmKey,
            JoseAlgorithms.Aes128GcmKeyWrap);
        TokenBytesResult gcmUnwrapped = service.UnwrapAesGcmKey(
            gcmWrapped.EncryptedKey!,
            gcmWrapped.InitializationVector!,
            gcmWrapped.AuthenticationTag!,
            gcmKey,
            JoseAlgorithms.Aes128GcmKeyWrap);

        TokenKey directKey = Symmetric(16, 60, JoseAlgorithms.Direct);
        DirectKeyManagementResult direct = service.ManageDirectKey(
            directKey,
            JoseAlgorithms.Aes128Gcm);
        DirectKeyManagementResult recoveredDirect = service.RecoverDirectKey(
            direct.EncryptedKey!,
            directKey,
            JoseAlgorithms.Aes128Gcm);

        byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");
        Pbes2KeyWrapResult pbes2 = service.WrapPbes2Key(
            contentKey,
            password,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            Pbes2Algorithms.DefaultIterationCount);
        TokenBytesResult pbes2Unwrapped = service.UnwrapPbes2Key(
            pbes2.EncryptedKey!,
            password,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            pbes2.SaltInput!,
            pbes2.IterationCount);

        Assert.Multiple(() =>
        {
            Assert.That(unwrapped.Bytes, Is.EqualTo(contentKey));
            Assert.That(gcmUnwrapped.Bytes, Is.EqualTo(contentKey));
            Assert.That(direct.Success, Is.True);
            Assert.That(recoveredDirect.ContentEncryptionKey, Is.EqualTo(directKey.SymmetricKey));
            Assert.That(pbes2Unwrapped.Bytes, Is.EqualTo(contentKey));
        });
    }

    [Test]
    public void PublicServiceSupportsEveryBuiltInSymmetricEncryptionVariant()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] keyMaterial = Sequence(16, 1);
        string[] aesWrapAlgorithms = new string[]
        {
            JoseAlgorithms.Aes128KeyWrap,
            JoseAlgorithms.Aes192KeyWrap,
            JoseAlgorithms.Aes256KeyWrap
        };
        string[] aesGcmWrapAlgorithms = new string[]
        {
            JoseAlgorithms.Aes128GcmKeyWrap,
            JoseAlgorithms.Aes192GcmKeyWrap,
            JoseAlgorithms.Aes256GcmKeyWrap
        };
        int[] aesSizes = new int[] { 16, 24, 32 };
        int index = 0;
        while (index < aesSizes.Length)
        {
            TokenKey wrapKey = Symmetric(
                aesSizes[index],
                20,
                aesWrapAlgorithms[index]);
            TokenBytesResult wrapped = service.WrapAesKey(
                keyMaterial,
                wrapKey,
                aesWrapAlgorithms[index]);
            TokenBytesResult unwrapped = service.UnwrapAesKey(
                wrapped.Bytes!,
                wrapKey,
                aesWrapAlgorithms[index]);
            TokenKey gcmWrapKey = Symmetric(
                aesSizes[index],
                30,
                aesGcmWrapAlgorithms[index]);
            AesGcmKeyWrapResult gcmWrapped = service.WrapAesGcmKey(
                keyMaterial,
                gcmWrapKey,
                aesGcmWrapAlgorithms[index]);
            TokenBytesResult gcmUnwrapped = service.UnwrapAesGcmKey(
                gcmWrapped.EncryptedKey!,
                gcmWrapped.InitializationVector!,
                gcmWrapped.AuthenticationTag!,
                gcmWrapKey,
                aesGcmWrapAlgorithms[index]);
            Assert.Multiple(() =>
            {
                Assert.That(unwrapped.Bytes, Is.EqualTo(keyMaterial));
                Assert.That(gcmUnwrapped.Bytes, Is.EqualTo(keyMaterial));
            });
            index++;
        }

        string[] directContentAlgorithms = new string[]
        {
            JoseAlgorithms.Aes128Gcm,
            JoseAlgorithms.Aes192Gcm,
            JoseAlgorithms.Aes256Gcm,
            JoseAlgorithms.Aes128CbcHmacSha256,
            JoseAlgorithms.Aes192CbcHmacSha384,
            JoseAlgorithms.Aes256CbcHmacSha512
        };
        int[] directSizes = new int[] { 16, 24, 32, 32, 48, 64 };
        index = 0;
        while (index < directContentAlgorithms.Length)
        {
            TokenKey directKey = Symmetric(
                directSizes[index],
                40,
                JoseAlgorithms.Direct);
            DirectKeyManagementResult managed = service.ManageDirectKey(
                directKey,
                directContentAlgorithms[index]);
            DirectKeyManagementResult recovered = service.RecoverDirectKey(
                Array.Empty<byte>(),
                directKey,
                directContentAlgorithms[index]);
            Assert.Multiple(() =>
            {
                Assert.That(managed.ContentEncryptionKey, Is.EqualTo(directKey.SymmetricKey));
                Assert.That(recovered.ContentEncryptionKey, Is.EqualTo(directKey.SymmetricKey));
            });
            index++;
        }

        string[] pbes2Algorithms = new string[]
        {
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
            JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap
        };
        foreach (string algorithm in pbes2Algorithms)
        {
            byte[] password = Encoding.UTF8.GetBytes("public PBES2 matrix password");
            Pbes2KeyWrapResult wrapped = service.WrapPbes2Key(
                keyMaterial,
                password,
                algorithm,
                Pbes2Algorithms.MinimumIterationCount);
            TokenBytesResult unwrapped = service.UnwrapPbes2Key(
                wrapped.EncryptedKey!,
                password,
                algorithm,
                wrapped.SaltInput!,
                wrapped.IterationCount);
            Assert.That(unwrapped.Bytes, Is.EqualTo(keyMaterial), algorithm);
        }

        byte[] plaintext = Sequence(23, 61);
        byte[] aad = Sequence(9, 91);
        string[] gcmAlgorithms = new string[]
        {
            JoseAlgorithms.Aes128Gcm,
            JoseAlgorithms.Aes192Gcm,
            JoseAlgorithms.Aes256Gcm
        };
        index = 0;
        while (index < gcmAlgorithms.Length)
        {
            TokenKey key = Symmetric(aesSizes[index], 70, gcmAlgorithms[index]);
            AesGcmContentEncryptionResult encrypted = service.EncryptAesGcm(
                plaintext,
                aad,
                key,
                gcmAlgorithms[index]);
            TokenBytesResult decrypted = service.DecryptAesGcm(
                encrypted.Ciphertext!,
                encrypted.InitializationVector!,
                encrypted.AuthenticationTag!,
                aad,
                key,
                gcmAlgorithms[index]);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext), gcmAlgorithms[index]);
            index++;
        }

        string[] cbcAlgorithms = new string[]
        {
            JoseAlgorithms.Aes128CbcHmacSha256,
            JoseAlgorithms.Aes192CbcHmacSha384,
            JoseAlgorithms.Aes256CbcHmacSha512
        };
        int[] cbcSizes = new int[] { 32, 48, 64 };
        index = 0;
        while (index < cbcAlgorithms.Length)
        {
            TokenKey key = Symmetric(cbcSizes[index], 80, cbcAlgorithms[index]);
            AesCbcHmacContentEncryptionResult encrypted = service.EncryptAesCbcHmac(
                plaintext,
                aad,
                key,
                cbcAlgorithms[index]);
            TokenBytesResult decrypted = service.DecryptAesCbcHmac(
                encrypted.Ciphertext!,
                encrypted.InitializationVector!,
                encrypted.AuthenticationTag!,
                aad,
                key,
                cbcAlgorithms[index]);
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext), cbcAlgorithms[index]);
            index++;
        }

        int[] coseWrapAlgorithms = new int[]
        {
            CoseAlgorithms.Aes128KeyWrap,
            CoseAlgorithms.Aes192KeyWrap,
            CoseAlgorithms.Aes256KeyWrap
        };
        int[] coseGcmAlgorithms = new int[]
        {
            CoseAlgorithms.Aes128Gcm,
            CoseAlgorithms.Aes192Gcm,
            CoseAlgorithms.Aes256Gcm
        };
        index = 0;
        while (index < aesSizes.Length)
        {
            TokenKey wrapKey = CoseSymmetric(
                aesSizes[index],
                100,
                coseWrapAlgorithms[index]);
            TokenBytesResult wrapped = service.WrapCoseAesKey(
                keyMaterial,
                wrapKey,
                coseWrapAlgorithms[index]);
            TokenBytesResult unwrapped = service.UnwrapCoseAesKey(
                wrapped.Bytes!,
                wrapKey,
                coseWrapAlgorithms[index]);
            TokenKey gcmKey = CoseSymmetric(
                aesSizes[index],
                110,
                coseGcmAlgorithms[index]);
            CoseAesGcmResult encrypted = service.EncryptCoseAesGcm(
                plaintext,
                aad,
                gcmKey,
                coseGcmAlgorithms[index]);
            TokenBytesResult decrypted = service.DecryptCoseAesGcm(
                encrypted.Ciphertext!,
                encrypted.InitializationVector!,
                encrypted.AuthenticationTag!,
                aad,
                gcmKey,
                coseGcmAlgorithms[index]);
            Assert.Multiple(() =>
            {
                Assert.That(unwrapped.Bytes, Is.EqualTo(keyMaterial));
                Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
            });
            index++;
        }

        int[] directAlgorithms = new int[]
        {
            CoseAlgorithms.DirectHkdfSha256,
            CoseAlgorithms.DirectHkdfSha512,
            CoseAlgorithms.DirectHkdfAes128,
            CoseAlgorithms.DirectHkdfAes256
        };
        int[] directKeySizes = new int[] { 32, 64, 16, 32 };
        index = 0;
        while (index < directAlgorithms.Length)
        {
            TokenKey key = CoseSymmetric(
                directKeySizes[index],
                120,
                directAlgorithms[index]);
            CoseKdfContextParameters context = new CoseKdfContextParameters(
                CoseAlgorithms.Aes128Gcm,
                128);
            CoseDirectKeyDistributionResult distributed =
                service.DistributeCoseDirectKey(
                    key,
                    directAlgorithms[index],
                    new byte[] { 1, 2, 3 },
                    context);
            CoseDirectKeyDistributionResult recovered =
                service.RecoverCoseDirectKey(
                    Array.Empty<byte>(),
                    key,
                    directAlgorithms[index],
                    new byte[] { 1, 2, 3 },
                    context);
            Assert.That(recovered.ContentKey, Is.EqualTo(distributed.ContentKey));
            index++;
        }
    }


    [Test]
    public void JoseContentEncryptionKdfAndCompressionRoundTrip()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] plaintext = Encoding.UTF8.GetBytes(
            "compressible public plaintext compressible public plaintext");
        byte[] aad = Encoding.ASCII.GetBytes("protected");

        TokenKey gcmKey = Symmetric(16, 71, JoseAlgorithms.Aes128Gcm);
        AesGcmContentEncryptionResult gcm =
            service.EncryptAesGcmWithInitializationVector(
                plaintext,
                Sequence(12, 1),
                aad,
                gcmKey,
                JoseAlgorithms.Aes128Gcm);
        TokenBytesResult gcmPlaintext = service.DecryptAesGcm(
            gcm.Ciphertext!,
            gcm.InitializationVector!,
            gcm.AuthenticationTag!,
            aad,
            gcmKey,
            JoseAlgorithms.Aes128Gcm);

        TokenKey cbcKey = Symmetric(
            32,
            81,
            JoseAlgorithms.Aes128CbcHmacSha256);
        AesCbcHmacContentEncryptionResult cbc =
            service.EncryptAesCbcHmacWithInitializationVector(
                plaintext,
                Sequence(16, 2),
                aad,
                cbcKey,
                JoseAlgorithms.Aes128CbcHmacSha256);
        TokenBytesResult cbcPlaintext = service.DecryptAesCbcHmac(
            cbc.Ciphertext!,
            cbc.InitializationVector!,
            cbc.AuthenticationTag!,
            aad,
            cbcKey,
            JoseAlgorithms.Aes128CbcHmacSha256);

        TokenBytesResult derived = service.DeriveJoseConcatKey(
            Sequence(32, 3),
            JoseAlgorithms.Aes128Gcm,
            new byte[] { 1 },
            new byte[] { 2 },
            128);
        TokenPolicy policy = new TokenPolicy();
        policy.AllowCompression = true;
        TokenBytesResult compressed = service.CompressJose(
            plaintext,
            JoseCompressionAlgorithms.Deflate,
            policy);
        TokenBytesResult decompressed = service.DecompressJose(
            compressed.Bytes!,
            JoseCompressionAlgorithms.Deflate,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(gcmPlaintext.Bytes, Is.EqualTo(plaintext));
            Assert.That(cbcPlaintext.Bytes, Is.EqualTo(plaintext));
            Assert.That(derived.Success, Is.True);
            Assert.That(derived.Bytes, Has.Length.EqualTo(16));
            Assert.That(decompressed.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void CoseKeyManagementAndContentEncryptionRoundTrip()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] contentKey = Sequence(16, 12);
        TokenKey wrapKey = CoseSymmetric(
            16,
            30,
            CoseAlgorithms.Aes128KeyWrap);
        TokenBytesResult wrapped = service.WrapCoseAesKey(
            contentKey,
            wrapKey,
            CoseAlgorithms.Aes128KeyWrap);
        TokenBytesResult unwrapped = service.UnwrapCoseAesKey(
            wrapped.Bytes!,
            wrapKey,
            CoseAlgorithms.Aes128KeyWrap);

        TokenKey directKey = CoseSymmetric(
            16,
            44,
            CoseAlgorithms.Direct);
        CoseKdfContextParameters context = new CoseKdfContextParameters(
            CoseAlgorithms.Aes128Gcm,
            128);
        CoseDirectKeyDistributionResult distributed =
            service.DistributeCoseDirectKey(
                directKey,
                CoseAlgorithms.Direct,
                null,
                context);
        CoseDirectKeyDistributionResult recovered =
            service.RecoverCoseDirectKey(
                Array.Empty<byte>(),
                directKey,
                CoseAlgorithms.Direct,
                null,
                context);

        TokenKey encryptionKey = CoseSymmetric(
            16,
            55,
            CoseAlgorithms.Aes128Gcm);
        byte[] plaintext = new byte[] { 1, 3, 5, 7, 9 };
        byte[] aad = new byte[] { 2, 4, 6 };
        CoseAesGcmResult encrypted =
            service.EncryptCoseAesGcmWithInitializationVector(
                plaintext,
                Sequence(12, 7),
                aad,
                encryptionKey,
                CoseAlgorithms.Aes128Gcm);
        TokenBytesResult decrypted = service.DecryptCoseAesGcm(
            encrypted.Ciphertext!,
            encrypted.InitializationVector!,
            encrypted.AuthenticationTag!,
            aad,
            encryptionKey,
            CoseAlgorithms.Aes128Gcm);

        Assert.Multiple(() =>
        {
            Assert.That(unwrapped.Bytes, Is.EqualTo(contentKey));
            Assert.That(distributed.ContentKey, Is.EqualTo(directKey.SymmetricKey));
            Assert.That(recovered.ContentKey, Is.EqualTo(directKey.SymmetricKey));
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void GeneratedNonceContentEncryptionAndRsaManagementArePublic()
    {
        ITokenAlgorithmOperations service =
            new TokenAlgorithmService();
        byte[] plaintext = new byte[] { 1, 2, 3, 4 };
        byte[] aad = new byte[] { 5, 6 };
        TokenKey gcmKey = Symmetric(
            16,
            10,
            JoseAlgorithms.Aes128Gcm);
        AesGcmContentEncryptionResult gcm =
            service.EncryptAesGcm(
                plaintext,
                aad,
                gcmKey,
                JoseAlgorithms.Aes128Gcm);
        TokenKey cbcKey = Symmetric(
            32,
            30,
            JoseAlgorithms.Aes128CbcHmacSha256);
        AesCbcHmacContentEncryptionResult cbc =
            service.EncryptAesCbcHmac(
                plaintext,
                aad,
                cbcKey,
                JoseAlgorithms.Aes128CbcHmacSha256);
        TokenKey coseGcmKey = CoseSymmetric(
            16,
            70,
            CoseAlgorithms.Aes128Gcm);
        CoseAesGcmResult coseGcm =
            service.EncryptCoseAesGcm(
                plaintext,
                aad,
                coseGcmKey,
                CoseAlgorithms.Aes128Gcm);
        TokenKey ccmKey = CoseSymmetric(
            16,
            90,
            CoseAlgorithms.AesCcm16_64_128);
        CoseAesCcmResult ccm = service.EncryptCoseAesCcm(
            plaintext,
            aad,
            ccmKey,
            CoseAlgorithms.AesCcm16_64_128);
        CoseAesCcmResult explicitCcm =
            service.EncryptCoseAesCcmWithInitializationVector(
                plaintext,
                new byte[13],
                aad,
                ccmKey,
                CoseAlgorithms.AesCcm16_64_128);

        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKey(rsa);
        byte[] contentKey = Sequence(16, 110);
        RsaKeyManagementResult wrapped = service.WrapRsaKey(
            contentKey,
            rsaKey,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes128Gcm,
            false);
        RsaKeyManagementResult unwrapped = service.UnwrapRsaKey(
            wrapped.EncryptedKey!,
            rsaKey,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.Aes128Gcm,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(gcm.Success, Is.True);
            Assert.That(gcm.InitializationVector,
                Has.Length.EqualTo(12));
            Assert.That(cbc.Success, Is.True);
            Assert.That(cbc.InitializationVector,
                Has.Length.EqualTo(16));
            Assert.That(coseGcm.Success, Is.True);
            Assert.That(coseGcm.InitializationVector,
                Has.Length.EqualTo(12));
            Assert.That(wrapped.Success, Is.True);
            Assert.That(unwrapped.ContentEncryptionKey,
                Is.EqualTo(contentKey));
            if (AesCcm.IsSupported)
            {
                Assert.That(ccm.Success, Is.True);
                Assert.That(explicitCcm.Success, Is.True);
                TokenBytesResult decrypted =
                    service.DecryptCoseAesCcm(
                        ccm.Ciphertext!,
                        ccm.InitializationVector!,
                        ccm.AuthenticationTag!,
                        aad,
                        ccmKey,
                        CoseAlgorithms.AesCcm16_64_128);
                Assert.That(decrypted.Bytes,
                    Is.EqualTo(plaintext));
            }
            else
            {
                AssertFailure(ccm.Error, "unsupported_algorithm");
                AssertFailure(
                    explicitCcm.Error,
                    "unsupported_algorithm");
            }
        });
    }

    [TestCase(CoseAlgorithms.AesCcm16_64_128, 16, 13, 8)]
    [TestCase(CoseAlgorithms.AesCcm16_64_256, 32, 13, 8)]
    [TestCase(CoseAlgorithms.AesCcm64_64_128, 16, 7, 8)]
    [TestCase(CoseAlgorithms.AesCcm64_64_256, 32, 7, 8)]
    [TestCase(CoseAlgorithms.AesCcm16_128_128, 16, 13, 16)]
    [TestCase(CoseAlgorithms.AesCcm16_128_256, 32, 13, 16)]
    [TestCase(CoseAlgorithms.AesCcm64_128_128, 16, 7, 16)]
    [TestCase(CoseAlgorithms.AesCcm64_128_256, 32, 7, 16)]
    public void PublicServiceSupportsEveryCoseAesCcmVariant(
        int algorithm,
        int keySize,
        int nonceSize,
        int tagSize)
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        TokenKey key = CoseSymmetric(keySize, 3, algorithm);
        byte[] plaintext = Sequence(19, 31);
        byte[] aad = Sequence(7, 61);
        CoseAesCcmResult encrypted =
            service.EncryptCoseAesCcmWithInitializationVector(
                plaintext,
                Sequence(nonceSize, 91),
                aad,
                key,
                algorithm);

        if (!AesCcm.IsSupported)
        {
            AssertFailure(encrypted.Error, "unsupported_algorithm");
            return;
        }

        TokenBytesResult decrypted = service.DecryptCoseAesCcm(
            encrypted.Ciphertext!,
            encrypted.InitializationVector!,
            encrypted.AuthenticationTag!,
            aad,
            key,
            algorithm);
        Assert.Multiple(() =>
        {
            Assert.That(encrypted.Success, Is.True);
            Assert.That(encrypted.InitializationVector, Has.Length.EqualTo(nonceSize));
            Assert.That(encrypted.AuthenticationTag, Has.Length.EqualTo(tagSize));
            Assert.That(decrypted.Bytes, Is.EqualTo(plaintext));
        });
    }

    [Test]
    public void PublicServiceSupportsEveryJoseRsaAndEcdhKeyManagementVariant()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] contentKey = Sequence(16, 11);
        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKey(rsa);
        string[] rsaAlgorithms = new string[]
        {
            JoseAlgorithms.RsaOaep,
            JoseAlgorithms.RsaOaepSha256,
            JoseAlgorithms.RsaOaepSha384,
            JoseAlgorithms.RsaOaepSha512
        };
        foreach (string algorithm in rsaAlgorithms)
        {
            RsaKeyManagementResult wrapped = service.WrapRsaKey(
                contentKey,
                rsaKey,
                algorithm,
                JoseAlgorithms.Aes128Gcm,
                false);
            RsaKeyManagementResult unwrapped = service.UnwrapRsaKey(
                wrapped.EncryptedKey!,
                rsaKey,
                algorithm,
                JoseAlgorithms.Aes128Gcm,
                false);
            Assert.That(unwrapped.ContentEncryptionKey, Is.EqualTo(contentKey), algorithm);
        }

        using ECDsa recipientPrivateNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = ECDsa.Create(
            recipientPrivateNative.ExportParameters(false));
        TokenKey recipientPrivate = EcKeyForCurve(recipientPrivateNative, "P-256");
        TokenKey recipientPublic = EcKeyForCurve(recipientPublicNative, "P-256");
        recipientPublic.NativePrivateMaterial = false;
        string[] ecdhAlgorithms = new string[]
        {
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.EcdhEsAes128KeyWrap,
            JoseAlgorithms.EcdhEsAes192KeyWrap,
            JoseAlgorithms.EcdhEsAes256KeyWrap
        };
        foreach (string algorithm in ecdhAlgorithms)
        {
            EcdhKeyManagementResult managed = service.ManageEcdhKey(
                recipientPublic,
                algorithm,
                JoseAlgorithms.Aes128Gcm);
            EcdhKeyManagementResult recovered = service.RecoverEcdhKey(
                managed.EncryptedKey!,
                recipientPrivate,
                managed.EphemeralPublicKey!,
                algorithm,
                JoseAlgorithms.Aes128Gcm);
            Assert.Multiple(() =>
            {
                Assert.That(managed.Success, Is.True, algorithm);
                Assert.That(recovered.ContentEncryptionKey,
                    Is.EqualTo(managed.ContentEncryptionKey),
                    algorithm);
            });
            managed.EphemeralPublicKey!.ReleaseOwnedMaterial();
        }
    }

    [Test]
    public void PublicServiceSupportsEveryCoseEcdhVariant()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        using ECDsa recipientPrivateNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa recipientPublicNative = ECDsa.Create(
            recipientPrivateNative.ExportParameters(false));
        using ECDsa senderPrivateNative = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa senderPublicNative = ECDsa.Create(
            senderPrivateNative.ExportParameters(false));
        TokenKey recipientPrivate = EcKeyForCurve(recipientPrivateNative, "P-256");
        TokenKey recipientPublic = EcKeyForCurve(recipientPublicNative, "P-256");
        recipientPublic.NativePrivateMaterial = false;
        TokenKey senderPrivate = EcKeyForCurve(senderPrivateNative, "P-256");
        TokenKey senderPublic = EcKeyForCurve(senderPublicNative, "P-256");
        senderPublic.NativePrivateMaterial = false;
        int[] directAlgorithms = new int[]
        {
            CoseAlgorithms.EcdhEsHkdfSha256,
            CoseAlgorithms.EcdhEsHkdfSha512,
            CoseAlgorithms.EcdhSsHkdfSha256,
            CoseAlgorithms.EcdhSsHkdfSha512
        };
        foreach (int algorithm in directAlgorithms)
        {
            bool staticMode = IsStaticCoseEcdh(algorithm);
            byte[]? salt = staticMode ? new byte[] { 1, 2, 3, 4 } : null;
            CoseKdfContextParameters context = new CoseKdfContextParameters(
                CoseAlgorithms.Aes128Gcm,
                128);
            CoseEcdhResult derived = service.DeriveCoseEcdhKey(
                recipientPublic,
                staticMode ? senderPrivate : null,
                algorithm,
                salt,
                context);
            TokenKey sender = staticMode ? senderPublic : derived.SenderPublicKey!;
            CoseEcdhResult recovered = service.RecoverCoseEcdhKey(
                derived.EncryptedKey!,
                recipientPrivate,
                sender,
                algorithm,
                salt,
                context);
            Assert.That(recovered.ContentKey, Is.EqualTo(derived.ContentKey), algorithm.ToString());
            if (derived.SenderPublicKey != null)
            {
                derived.SenderPublicKey.ReleaseOwnedMaterial();
            }
        }

        int[] wrapAlgorithms = new int[]
        {
            CoseAlgorithms.EcdhEsAes128KeyWrap,
            CoseAlgorithms.EcdhEsAes192KeyWrap,
            CoseAlgorithms.EcdhEsAes256KeyWrap,
            CoseAlgorithms.EcdhSsAes128KeyWrap,
            CoseAlgorithms.EcdhSsAes192KeyWrap,
            CoseAlgorithms.EcdhSsAes256KeyWrap
        };
        int[] wrapKeyAlgorithms = new int[]
        {
            CoseAlgorithms.Aes128KeyWrap,
            CoseAlgorithms.Aes192KeyWrap,
            CoseAlgorithms.Aes256KeyWrap,
            CoseAlgorithms.Aes128KeyWrap,
            CoseAlgorithms.Aes192KeyWrap,
            CoseAlgorithms.Aes256KeyWrap
        };
        int[] wrapKeySizes = new int[] { 128, 192, 256, 128, 192, 256 };
        int index = 0;
        while (index < wrapAlgorithms.Length)
        {
            bool staticMode = IsStaticCoseEcdh(wrapAlgorithms[index]);
            byte[]? salt = staticMode ? new byte[] { 8, 6, 7, 5 } : null;
            CoseKdfContextParameters context = new CoseKdfContextParameters(
                wrapKeyAlgorithms[index],
                (ulong)wrapKeySizes[index]);
            CoseEcdhResult wrapped = service.WrapCoseEcdhKey(
                Sequence(16, 41),
                recipientPublic,
                staticMode ? senderPrivate : null,
                wrapAlgorithms[index],
                salt,
                context);
            TokenKey sender = staticMode ? senderPublic : wrapped.SenderPublicKey!;
            CoseEcdhResult recovered = service.RecoverCoseEcdhKey(
                wrapped.EncryptedKey!,
                recipientPrivate,
                sender,
                wrapAlgorithms[index],
                salt,
                context);
            Assert.That(recovered.ContentKey, Is.EqualTo(wrapped.ContentKey));
            if (wrapped.SenderPublicKey != null)
            {
                wrapped.SenderPublicKey.ReleaseOwnedMaterial();
            }
            index++;
        }
    }


    [Test]
    public void EcdhEntryPointsExposePredictableNullContracts()
    {
        ITokenAlgorithmOperations service =
            new TokenAlgorithmService();
        CoseKdfContextParameters context =
            new CoseKdfContextParameters(
                CoseAlgorithms.Aes128Gcm,
                128);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => service.ManageEcdhKey(
                    null!,
                    JoseAlgorithms.EcdhEs,
                    JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.RecoverEcdhKey(
                    Array.Empty<byte>(),
                    null!,
                    new TokenKey(),
                    JoseAlgorithms.EcdhEs,
                    JoseAlgorithms.Aes128Gcm),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.DeriveCoseEcdhKey(
                    null!,
                    null,
                    CoseAlgorithms.EcdhEsHkdfSha256,
                    null,
                    context),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.WrapCoseEcdhKey(
                    null!,
                    new TokenKey(),
                    null,
                    CoseAlgorithms.EcdhEsAes128KeyWrap,
                    null,
                    context),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.RecoverCoseEcdhKey(
                    Array.Empty<byte>(),
                    null!,
                    new TokenKey(),
                    CoseAlgorithms.EcdhSsHkdfSha256,
                    null,
                    context),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void UnsupportedInvalidAndAuthenticationFailuresRemainStable()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        TokenKey key = Symmetric(32, 5, JoseAlgorithms.HmacSha256);
        TokenBytesResult unknown = service.SignJoseHmac(
            Array.Empty<byte>(),
            key,
            "unknown-public-algorithm");
        TokenBytesResult prohibited = service.SignJoseHmac(
            Array.Empty<byte>(),
            key,
            JoseAlgorithms.HmacSha1);
        TokenBytesResult signature = service.SignJoseHmac(
            new byte[] { 1 },
            key,
            JoseAlgorithms.HmacSha256);
        signature.Bytes![0] ^= 1;
        TokenResult mismatch = service.VerifyJoseHmac(
            new byte[] { 1 },
            signature.Bytes,
            key,
            JoseAlgorithms.HmacSha256);
        TokenBytesResult optional = service.SignJoseEcdsa(
            Array.Empty<byte>(),
            key,
            JoseAlgorithms.Ed448);

        Assert.Multiple(() =>
        {
            AssertFailure(unknown.Error, "unknown_algorithm");
            AssertFailure(prohibited.Error, "prohibited_algorithm");
            AssertFailure(mismatch.Error, "signature_mismatch");
            Assert.That(
                optional.Error!.Code,
                Is.EqualTo("unsupported_algorithm").Or.EqualTo("invalid_algorithm"));
        });
    }

    [Test]
    public void PublicServiceRejectsWrongKeysAlgorithmsCorruptionAndInvalidLengths()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] data = Sequence(17, 1);
        TokenKey hmacKey = Symmetric(32, 20, JoseAlgorithms.HmacSha256);
        TokenBytesResult hmac = service.SignJoseHmac(
            data,
            hmacKey,
            JoseAlgorithms.HmacSha256);
        byte[] changedHmac = hmac.Bytes!.ToArray();
        changedHmac[0] ^= 1;
        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKey(rsa);
        TokenBytesResult rsaSignature = service.SignJoseRsa(
            data,
            rsaKey,
            JoseAlgorithms.RsaSha256);

        TokenKey gcmKey = Symmetric(16, 40, JoseAlgorithms.Aes128Gcm);
        AesGcmContentEncryptionResult gcm = service.EncryptAesGcm(
            data,
            Array.Empty<byte>(),
            gcmKey,
            JoseAlgorithms.Aes128Gcm);
        byte[] changedGcmTag = gcm.AuthenticationTag!.ToArray();
        changedGcmTag[0] ^= 1;
        TokenKey cbcKey = Symmetric(
            32,
            60,
            JoseAlgorithms.Aes128CbcHmacSha256);
        AesCbcHmacContentEncryptionResult cbc = service.EncryptAesCbcHmac(
            data,
            Array.Empty<byte>(),
            cbcKey,
            JoseAlgorithms.Aes128CbcHmacSha256);
        byte[] changedCbcTag = cbc.AuthenticationTag!.ToArray();
        changedCbcTag[0] ^= 1;
        TokenKey wrapKey = Symmetric(
            16,
            80,
            JoseAlgorithms.Aes128GcmKeyWrap);
        AesGcmKeyWrapResult wrapped = service.WrapAesGcmKey(
            Sequence(16, 90),
            wrapKey,
            JoseAlgorithms.Aes128GcmKeyWrap);
        byte[] changedWrapTag = wrapped.AuthenticationTag!.ToArray();
        changedWrapTag[0] ^= 1;

        Assert.Multiple(() =>
        {
            AssertFailure(service.VerifyJoseHmac(
                data,
                changedHmac,
                hmacKey,
                JoseAlgorithms.HmacSha256).Error,
                "signature_mismatch");
            AssertFailure(service.SignJoseHmac(
                data,
                rsaKey,
                JoseAlgorithms.HmacSha256).Error,
                "invalid_key_type");
            AssertFailure(service.VerifyJoseRsa(
                data,
                rsaSignature.Bytes!,
                rsaKey,
                JoseAlgorithms.RsaPssSha256).Error,
                "signature_mismatch");
            AssertFailure(service.DecryptAesGcm(
                gcm.Ciphertext!,
                gcm.InitializationVector!,
                changedGcmTag,
                Array.Empty<byte>(),
                gcmKey,
                JoseAlgorithms.Aes128Gcm).Error,
                "decryption_failed");
            AssertFailure(service.EncryptAesGcmWithInitializationVector(
                data,
                new byte[11],
                Array.Empty<byte>(),
                gcmKey,
                JoseAlgorithms.Aes128Gcm).Error,
                "invalid_initialization_vector");
            AssertFailure(service.DecryptAesCbcHmac(
                cbc.Ciphertext!,
                cbc.InitializationVector!,
                changedCbcTag,
                Array.Empty<byte>(),
                cbcKey,
                JoseAlgorithms.Aes128CbcHmacSha256).Error,
                "decryption_failed");
            AssertFailure(service.EncryptAesCbcHmacWithInitializationVector(
                data,
                new byte[15],
                Array.Empty<byte>(),
                cbcKey,
                JoseAlgorithms.Aes128CbcHmacSha256).Error,
                "invalid_initialization_vector");
            AssertFailure(service.UnwrapAesGcmKey(
                wrapped.EncryptedKey!,
                wrapped.InitializationVector!,
                changedWrapTag,
                wrapKey,
                JoseAlgorithms.Aes128GcmKeyWrap).Error,
                "unwrap_failed");
            AssertFailure(service.WrapAesKey(
                new byte[15],
                Symmetric(16, 1, JoseAlgorithms.Aes128KeyWrap),
                JoseAlgorithms.Aes128KeyWrap).Error,
                "invalid_key_size");
        });
    }

    [Test]
    public void PublicServiceRequiresExplicitCompatibilityForLegacyAlgorithms()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        byte[] data = new byte[] { 1, 3, 5 };
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey ecKey = EcKeyForCurve(ecdsa, "P-256");
        TokenBytesResult conservativeEcdsa = service.SignCoseEcdsa(
            data,
            ecKey,
            CoseAlgorithms.EcdsaSha256,
            false);
        TokenBytesResult compatibleEcdsa = service.SignCoseEcdsa(
            data,
            ecKey,
            CoseAlgorithms.EcdsaSha256,
            true);
        TokenResult verifiedEcdsa = service.VerifyCoseEcdsa(
            data,
            compatibleEcdsa.Bytes!,
            ecKey,
            CoseAlgorithms.EcdsaSha256,
            true);

        using RSA rsa = RSA.Create(2048);
        TokenKey rsaKey = RsaKey(rsa);
        TokenBytesResult contentKey = new TokenBytesResult();
        contentKey.Bytes = Sequence(16, 21);
        contentKey.Success = true;
        RsaKeyManagementResult conservativeRsa = service.WrapRsaKey(
            contentKey.Bytes,
            rsaKey,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm,
            false);
        RsaKeyManagementResult compatibleRsa = service.WrapRsaKey(
            contentKey.Bytes,
            rsaKey,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm,
            true);
        RsaKeyManagementResult recoveredRsa = service.UnwrapRsaKey(
            compatibleRsa.EncryptedKey!,
            rsaKey,
            JoseAlgorithms.RsaPkcs1,
            JoseAlgorithms.Aes128Gcm,
            true);

        Assert.Multiple(() =>
        {
            AssertFailure(conservativeEcdsa.Error, "compatibility_required");
            Assert.That(compatibleEcdsa.Success, Is.True);
            Assert.That(verifiedEcdsa.Success, Is.True);
            AssertFailure(conservativeRsa.Error, "compatibility_required");
            Assert.That(recoveredRsa.ContentEncryptionKey, Is.EqualTo(contentKey.Bytes));
        });
    }

    [Test]
    public void NullReferencesRemainProgrammerErrors()
    {
        ITokenAlgorithmOperations service = new TokenAlgorithmService();
        TokenKey key = Symmetric(32, 1, JoseAlgorithms.HmacSha256);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => service.SignJoseHmac(null!, key, JoseAlgorithms.HmacSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.VerifyCoseRsa(
                    Array.Empty<byte>(),
                    null!,
                    key,
                    CoseAlgorithms.RsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => service.CompressJose(
                    Array.Empty<byte>(),
                    JoseCompressionAlgorithms.Deflate,
                    null!),
                Throws.ArgumentNullException);
        });
    }

    static TokenKey Symmetric(int length, byte start, string algorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = Sequence(length, start);
        key.Algorithm = algorithm;
        return key;
    }

    static TokenKey SymmetricBytes(byte[] bytes, string algorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = bytes.ToArray();
        key.Algorithm = algorithm;
        return key;
    }

    static TokenKey CoseSymmetric(int length, byte start, int algorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = Sequence(length, start);
        key.CoseAlgorithm = algorithm;
        key.HasCoseAlgorithm = true;
        return key;
    }

    static TokenKey CoseSymmetricBytes(byte[] bytes, int algorithm)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = bytes.ToArray();
        key.CoseAlgorithm = algorithm;
        key.HasCoseAlgorithm = true;
        return key;
    }

    static TokenKey RsaKey(RSA rsa)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Rsa;
        key.Rsa = rsa;
        key.NativePrivateMaterial = true;
        key.NativePublicMaterial = true;
        return key;
    }

    static TokenKey EcKey(ECDsa ecdsa)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.EllipticCurve;
        key.Curve = "P-256";
        key.CoseCurve = CoseKeyLabels.CurveP256;
        key.Ecdsa = ecdsa;
        key.NativePrivateMaterial = true;
        key.NativePublicMaterial = true;
        return key;
    }

    static TokenKey EcKeyForCurve(ECDsa ecdsa, string curve)
    {
        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.EllipticCurve;
        key.Curve = curve;
        if (curve == "P-384")
        {
            key.CoseCurve = CoseKeyLabels.CurveP384;
        }
        else if (curve == "P-521")
        {
            key.CoseCurve = CoseKeyLabels.CurveP521;
        }
        else
        {
            key.CoseCurve = CoseKeyLabels.CurveP256;
        }

        key.Ecdsa = ecdsa;
        key.NativePrivateMaterial = true;
        key.NativePublicMaterial = true;
        return key;
    }

    static ECDsa CreateCurve(string curve)
    {
        if (curve == "P-384")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP384);
        }

        if (curve == "P-521")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP521);
        }

        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    static bool IsStaticCoseEcdh(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    static byte[] Sequence(int length, byte start)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(start + index);
            index++;
        }

        return result;
    }

    static void AssertFailure(TokenError? error, string code)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error!.Code, Is.EqualTo(code));
    }
}
