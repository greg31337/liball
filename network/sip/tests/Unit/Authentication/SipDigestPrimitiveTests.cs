namespace sip.Tests;

public class SipDigestPrimitiveTests
{
    static readonly object[] GeneratedDigestVectors = new object[]
    {
        Vector(SipDigestAlgorithm.Md5, SipDigestQuality.None,
            "7050e9fbb3b1e3705309822e2ae864ef",
            "ff6b5eb9fe75cbe1905155f66f48aad7",
            "6b4e8af76f1c9f75eb73049407c94b3e"),
        Vector(SipDigestAlgorithm.Md5, SipDigestQuality.Auth,
            "bcc99cf643692cc7bc23c8594e4e05b2",
            "910a19084ea04f2de51cd73a0fc7bd8d",
            "6b4e8af76f1c9f75eb73049407c94b3e"),
        Vector(SipDigestAlgorithm.Md5, SipDigestQuality.AuthInt,
            "bdf3b73a7bb595bb7cbef6884e6cbb81",
            "07f353bf1b920858d30cca0457b774c4",
            "6b4e8af76f1c9f75eb73049407c94b3e"),
        Vector(SipDigestAlgorithm.Md5Sess, SipDigestQuality.None,
            "f11e51af6b665bc614eef7220b3a1128",
            "8dcf2671aa1125514c1ea54a0458af24",
            "6b4e8af76f1c9f75eb73049407c94b3e"),
        Vector(SipDigestAlgorithm.Md5Sess, SipDigestQuality.Auth,
            "a3fbf77bf691d3d9067e32a3d1a5c2de",
            "4e0ce12768b9bcdd897bbd2a5cda8467",
            "6b4e8af76f1c9f75eb73049407c94b3e"),
        Vector(SipDigestAlgorithm.Md5Sess, SipDigestQuality.AuthInt,
            "51563d8a0f6b798fe6b8357e4803b019",
            "fe191f1fbe5db16ce6dc5bbb2e4c8330",
            "6b4e8af76f1c9f75eb73049407c94b3e"),
        Vector(SipDigestAlgorithm.Sha256, SipDigestQuality.None,
            "c49839c32f260ab05551b2a3170990f0c782342b8bff172ea54df8ccb9c0bfe9",
            "796c27350fd49f2382017b3bce73cb790ae8751fcc3b86d1b186aa1f2724c479",
            "8f60068d4f86c9856fc8ea0fbfa42642a71cde0ec054cd958c901864e86a7be0"),
        Vector(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
            "0a056541becebf42fb90e51a7f5528f3fb7402e1af865391aadd79d4474235b0",
            "4673099211f0de3f53cc5c849e6d02cbba924f1f581c0e3466a2c9626be41de3",
            "8f60068d4f86c9856fc8ea0fbfa42642a71cde0ec054cd958c901864e86a7be0"),
        Vector(SipDigestAlgorithm.Sha256, SipDigestQuality.AuthInt,
            "01ea55bc52febcbb7ab64daa7ef077c4c9c2ddbcfa5012ace442a9bf936ef8c8",
            "98944add5e8cf8c57e6fb7a5d928b73801dcce7e58796d4b85214cd87ba1cd2b",
            "8f60068d4f86c9856fc8ea0fbfa42642a71cde0ec054cd958c901864e86a7be0"),
        Vector(SipDigestAlgorithm.Sha256Sess, SipDigestQuality.None,
            "14b4d5028d3d7cc09331a25af069b2826483de5a8105cdb5f611f8347386980c",
            "cf7aa7ed552769e963796a6ccd9cf8e1257ba100d890fea407274208eb2cc61e",
            "8f60068d4f86c9856fc8ea0fbfa42642a71cde0ec054cd958c901864e86a7be0"),
        Vector(SipDigestAlgorithm.Sha256Sess, SipDigestQuality.Auth,
            "94ccd7b954c24205d757f8b46c2ae79e81bf9c9786083d1980c40ec2a7119a6d",
            "2a56d0c5fea42603f36c9f89aa3f2aacff7bdfc0dc9ce9aeb0f7efeebb503576",
            "8f60068d4f86c9856fc8ea0fbfa42642a71cde0ec054cd958c901864e86a7be0"),
        Vector(SipDigestAlgorithm.Sha256Sess, SipDigestQuality.AuthInt,
            "cd4c88789f40ec721db5b33b8c164b6eb3b933fc37fb19f62fa59861e43d0b01",
            "6d1b6c92789ca5e463005884d6328d3fea563af2ac7eecad596ff7981f439ebe",
            "8f60068d4f86c9856fc8ea0fbfa42642a71cde0ec054cd958c901864e86a7be0"),
        Vector(SipDigestAlgorithm.Sha512256, SipDigestQuality.None,
            "835c450f03354d80bff0b5257b96f24a12c315be2e9cb2c6c4e1889aeba81220",
            "f00d48c179e596073611152d6b86b1647ee3dc7bd16f01e0ba28bb913b0a3751",
            "a363b792f84a7a96070ab5b8bbfa7ab270f12b9bf81b2b35f16251b15fb91625"),
        Vector(SipDigestAlgorithm.Sha512256, SipDigestQuality.Auth,
            "b2fdac661dd611b16bdfdd0e8f33978a1979150e7b11e9f45a7ece41b7ebbd5f",
            "d619a8dea50b7d17a58805c615efcd881b10971731181b52b178aff5867cd955",
            "a363b792f84a7a96070ab5b8bbfa7ab270f12b9bf81b2b35f16251b15fb91625"),
        Vector(SipDigestAlgorithm.Sha512256, SipDigestQuality.AuthInt,
            "487bddca7698720e2aa780baec70b32b1fd3a8a8a6d9d7d7354a07ea7389a96f",
            "ba4bcad473994e09c7aa3d84b20c28dd55581f37ef6d26baf0d77d1a469d7acc",
            "a363b792f84a7a96070ab5b8bbfa7ab270f12b9bf81b2b35f16251b15fb91625"),
        Vector(SipDigestAlgorithm.Sha512256Sess, SipDigestQuality.None,
            "896818535afbacaff65e01deb4fac44d72cc07d7313d88fe65e2d717c1332ac8",
            "5ee9ea8c0ac828a99a3bc0438ed84ff38d1fa22542a45263a6e70b9233729c2e",
            "a363b792f84a7a96070ab5b8bbfa7ab270f12b9bf81b2b35f16251b15fb91625"),
        Vector(SipDigestAlgorithm.Sha512256Sess, SipDigestQuality.Auth,
            "b7d6a7594d0172befc2aef0eccadc5631cf71a77c138fd9079be88f4e6165f82",
            "f198331e8e30cc90b933aa0625cd3952a662d80bf3c76fec228a9fd76f798afe",
            "a363b792f84a7a96070ab5b8bbfa7ab270f12b9bf81b2b35f16251b15fb91625"),
        Vector(SipDigestAlgorithm.Sha512256Sess, SipDigestQuality.AuthInt,
            "c49e796543e696ac007ad43102e8f05a51cc4ec52bcc62caf20c144b9e95ef8f",
            "b086955dd76e767248526970dfc7c32dd4b305fd4248605eb9480b63a9ebedf7",
            "a363b792f84a7a96070ab5b8bbfa7ab270f12b9bf81b2b35f16251b15fb91625")
    };

    [Test]
    public void MatchesRfc7616Md5AndSha256ExampleResponses()
    {
        SipDigestCalculation md5 = CreateRfc7616Mufasa(
            SipDigestAlgorithm.Md5);
        SipDigestCalculation sha256 = CreateRfc7616Mufasa(
            SipDigestAlgorithm.Sha256);

        SipDigestCalculationResult md5Result =
            SipDigestCalculator.Calculate(md5);
        SipDigestCalculationResult sha256Result =
            SipDigestCalculator.Calculate(sha256);

        Assert.Multiple(() =>
        {
            Assert.That(md5Result.Response,
                Is.EqualTo("8ca523f5e9506fed4657c9700eebdbec"));
            Assert.That(sha256Result.Response,
                Is.EqualTo("753927fa0e85d155564e2e272a28d1802ca10daf4496794697cf8db5856cb6c1"));
            Assert.That(md5Result.NonceCountText, Is.EqualTo("00000001"));
            Assert.That(sha256Result.NonceCountText, Is.EqualTo("00000001"));
        });
    }

    [Test]
    public void MatchesCorrectedRfc7616Sha512256Utf8UserhashExample()
    {
        SipDigestCalculation calculation = new SipDigestCalculation();
        calculation.Algorithm = SipDigestAlgorithm.Sha512256;
        calculation.Quality = SipDigestQuality.Auth;
        calculation.UserName = "Jäsøn Doe";
        calculation.Realm = "api@example.org";
        calculation.Secret = System.Text.Encoding.UTF8.GetBytes(
            "Secret, or not?");
        calculation.Nonce = "5TsQWLVdgBdmrQ0XsxbDODV+57QdFR34I9HAbC/RVvkK";
        calculation.NonceCount = 1;
        calculation.ClientNonce =
            "NTg6RKcb9boFIAS3KrFK9BGeh+iDa/sm6jUMp2wds69v";
        calculation.Method = "GET";
        calculation.DigestUri = "/doe.json";
        calculation.Utf8 = true;

        SipDigestCalculationResult result =
            SipDigestCalculator.Calculate(calculation);

        Assert.Multiple(() =>
        {
            Assert.That(result.UserHash,
                Is.EqualTo("793263caabb707a56211940d90411ea4a575adeccb7e360aeb624ed06ece9b0b"));
            Assert.That(result.Response,
                Is.EqualTo("3798d4131c277846293534c3edc11bd8a5e4cdcbff78b05db9d95eeb1cec68a5"));
        });
    }

    [Test]
    public void MatchesRfc8760EmptyEntityBodyHash()
    {
        string result = SipDigestHash.HashHex(
            SipDigestAlgorithm.Sha256, new byte[0]);

        Assert.That(result,
            Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
    }

    [TestCaseSource(nameof(GeneratedDigestVectors))]
    public void IndependentVectorsCoverEveryAlgorithmSessionModeAndQuality(
        SipDigestAlgorithm algorithm, SipDigestQuality quality,
        string response, string responseAuthentication, string userHash)
    {
        SipDigestCalculation calculation = CreateGeneratedCalculation(
            algorithm, quality);

        SipDigestCalculationResult result =
            SipDigestCalculator.Calculate(calculation);

        Assert.Multiple(() =>
        {
            Assert.That(result.Response, Is.EqualTo(response));
            Assert.That(result.ResponseAuthentication,
                Is.EqualTo(responseAuthentication));
            Assert.That(result.UserHash, Is.EqualTo(userHash));
            Assert.That(result.NonceCountText,
                Is.EqualTo(quality == SipDigestQuality.None
                    ? null : "001a2b3c"));
        });
    }

    [Test]
    public void HonorsLegacyLatin1AndUtf8Normalization()
    {
        SipDigestCalculation latin1 = CreateCharsetCalculation(false);
        latin1.UserName = "Jörg";
        latin1.Realm = "réalm@example";
        latin1.Secret = System.Text.Encoding.Latin1.GetBytes("sëcret");
        SipDigestCalculation utf8 = CreateCharsetCalculation(true);
        utf8.UserName = "Ja\u0308son";
        utf8.Secret = System.Text.Encoding.UTF8.GetBytes("pa\u0308ss");

        SipDigestCalculationResult latin1Result =
            SipDigestCalculator.Calculate(latin1);
        SipDigestCalculationResult utf8Result =
            SipDigestCalculator.Calculate(utf8);

        Assert.Multiple(() =>
        {
            Assert.That(latin1Result.Response,
                Is.EqualTo("b22918aff2033ca09f1b4d21fbc2415869ed585647e742f06caebc5a3c2eb36c"));
            Assert.That(latin1Result.ResponseAuthentication,
                Is.EqualTo("f60e6446692765bbc1210aa674152754b8a0113eea551300b1b39f1178accd5e"));
            Assert.That(latin1Result.UserHash,
                Is.EqualTo("de49af4475833909967f76ab7db2d8db0d7d2367619c9f1e7ab37d75388fed6d"));
            Assert.That(utf8Result.Response,
                Is.EqualTo("a9832210db28f4ae61414698d35bbdd8b7a8fe2ee68c36e2065b3f2f22e367d8"));
            Assert.That(utf8Result.ResponseAuthentication,
                Is.EqualTo("eed34ba2645518bfd4477416877742f909f0258bee9817ba1ff18013521c2cd6"));
            Assert.That(utf8Result.UserHash,
                Is.EqualTo("9b783cf6429b1f1f56f1b778f4302d5bfa0c3733dafbea67909dba0f4f1bc83b"));
        });
    }

    [Test]
    public void HashPrimitivesUseCorrectLengthsAndLowercaseHex()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SipDigestHash.HashTextHex(
                SipDigestAlgorithm.Md5, "abc"),
                Is.EqualTo("900150983cd24fb0d6963f7d28e17f72"));
            Assert.That(SipDigestHash.HashTextHex(
                SipDigestAlgorithm.Sha256, "abc"),
                Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
            Assert.That(SipDigestHash.HashTextHex(
                SipDigestAlgorithm.Sha512256, "abc"),
                Is.EqualTo("53048e2681941ef99b2e29b76b4c7dabe4c2d0c634fc6d46e0e2f13107e7af23"));
            Assert.That(SipDigestHash.HashByteCount(
                SipDigestAlgorithm.Md5Sess), Is.EqualTo(16));
            Assert.That(SipDigestHash.HashByteCount(
                SipDigestAlgorithm.Sha256Sess), Is.EqualTo(32));
            Assert.That(SipDigestHash.HashByteCount(
                SipDigestAlgorithm.Sha512256Sess), Is.EqualTo(32));
        });
    }

    [Test]
    public void RejectsMalformedDigestCalculationInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => SipDigestCalculator.Calculate(null),
                Throws.ArgumentNullException);
            AssertInvalid((SipDigestAlgorithm)99, SipDigestQuality.Auth,
                null, null);
            AssertInvalid(SipDigestAlgorithm.Sha256, (SipDigestQuality)99,
                null, null);
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "UserName", null);
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "UserName", "");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "UserName", "name:part");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "Realm", null);
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "Nonce", null);
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "Nonce", "");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "Nonce", "nönce");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "Method", null);
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "Method", "BAD METHOD");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "DigestUri", null);
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "DigestUri", "");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "DigestUri", "sip:josé@example.com");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "ClientNonce", null);
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "ClientNonce", "cliënt");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.None,
                "NonceCount", "1");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "NonceCount", "0");
            AssertInvalid(SipDigestAlgorithm.Sha256, SipDigestQuality.Auth,
                "NonceCount", "4294967296");
            AssertInvalid(SipDigestAlgorithm.Sha256,
                SipDigestQuality.AuthInt, "RequestBody", null);
            AssertInvalid(SipDigestAlgorithm.Sha256,
                SipDigestQuality.AuthInt, "ResponseBody", null);
        });
    }

    [Test]
    public void FixedTimeComparisonRejectsMalformedDigestText()
    {
        string value = SipDigestHash.HashTextHex(
            SipDigestAlgorithm.Sha256, "comparison");

        Assert.Multiple(() =>
        {
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, value, value), Is.True);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, value, value.ToUpperInvariant()),
                Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, value, value.Substring(1)),
                Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsHex(
                SipDigestAlgorithm.Sha256, value, null), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsBytes(
                new byte[] { 1, 2 }, new byte[] { 1, 2 }), Is.True);
            Assert.That(SipDigestHash.FixedTimeEqualsBytes(
                new byte[] { 1, 2 }, new byte[] { 1, 3 }), Is.False);
            Assert.That(SipDigestHash.FixedTimeEqualsBytes(null,
                new byte[] { 1 }), Is.False);
        });
    }

    static object[] Vector(SipDigestAlgorithm algorithm,
        SipDigestQuality quality, string response,
        string responseAuthentication, string userHash)
    {
        return new object[]
        {
            algorithm, quality, response, responseAuthentication, userHash
        };
    }

    static SipDigestCalculation CreateRfc7616Mufasa(
        SipDigestAlgorithm algorithm)
    {
        SipDigestCalculation calculation = new SipDigestCalculation();
        calculation.Algorithm = algorithm;
        calculation.Quality = SipDigestQuality.Auth;
        calculation.UserName = "Mufasa";
        calculation.Realm = "http-auth@example.org";
        calculation.Secret = System.Text.Encoding.ASCII.GetBytes(
            "Circle of Life");
        calculation.Nonce =
            "7ypf/xlj9XXwfDPEoM4URrv/xwf94BcCAzFZH4GiTo0v";
        calculation.NonceCount = 1;
        calculation.ClientNonce =
            "f2/wE4q74E6zIJEtWaHKaf5wv/H5QzzpXusqGemxURZJ";
        calculation.Method = "GET";
        calculation.DigestUri = "/dir/index.html";
        return calculation;
    }

    static SipDigestCalculation CreateGeneratedCalculation(
        SipDigestAlgorithm algorithm, SipDigestQuality quality)
    {
        SipDigestCalculation calculation = new SipDigestCalculation();
        calculation.Algorithm = algorithm;
        calculation.Quality = quality;
        calculation.UserName = "Jäsøn Doe";
        calculation.Realm = "sip@example.net";
        calculation.Secret = System.Text.Encoding.UTF8.GetBytes("pässword Δ");
        calculation.Nonce = "nonce-123";
        calculation.NonceCount = quality == SipDigestQuality.None
            ? -1 : 0x1a2b3c;
        calculation.ClientNonce = "client-456";
        calculation.Method = "MESSAGE";
        calculation.DigestUri = "sip:bob@example.com;transport=tcp";
        calculation.RequestBody = new byte[]
            { 111, 102, 102, 101, 114, 61, 0, 97, 117, 100, 105, 111, 13, 10 };
        calculation.ResponseBody = new byte[]
            { 97, 110, 115, 119, 101, 114, 61, 255, 118, 105, 100, 101, 111 };
        calculation.Utf8 = true;
        return calculation;
    }

    static SipDigestCalculation CreateCharsetCalculation(bool utf8)
    {
        SipDigestCalculation calculation = new SipDigestCalculation();
        calculation.Algorithm = SipDigestAlgorithm.Sha256;
        calculation.Quality = SipDigestQuality.Auth;
        calculation.UserName = "Jason";
        calculation.Realm = "realm@example";
        calculation.Secret = System.Text.Encoding.ASCII.GetBytes("password");
        calculation.Nonce = "latin-nonce";
        calculation.NonceCount = 2;
        calculation.ClientNonce = "latin-client";
        calculation.Method = "INVITE";
        calculation.DigestUri = "sip:service@example.com";
        calculation.Utf8 = utf8;
        return calculation;
    }

    static void AssertInvalid(SipDigestAlgorithm algorithm,
        SipDigestQuality quality, string? field, string? value)
    {
        SipDigestCalculation calculation = CreateGeneratedCalculation(
            algorithm, quality);
        switch (field)
        {
            case "UserName": calculation.UserName = value; break;
            case "Realm": calculation.Realm = value; break;
            case "Nonce": calculation.Nonce = value; break;
            case "Method": calculation.Method = value; break;
            case "DigestUri": calculation.DigestUri = value; break;
            case "ClientNonce": calculation.ClientNonce = value; break;
            case "NonceCount":
                calculation.NonceCount = long.Parse(value == null ? "0" : value,
                    System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "RequestBody": calculation.RequestBody = null; break;
            case "ResponseBody": calculation.ResponseBody = null; break;
        }
        Assert.That(() => SipDigestCalculator.Calculate(calculation),
            Throws.Exception);
    }
}
