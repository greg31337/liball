namespace jmap.Tests;

public class WebPushEncryptorTests
{
    [Test]
    public void EncryptionMatchesPublishedRfc8291Vector()
    {
        WebPushEncryptor encryptor = new WebPushEncryptor();
        byte[] plaintext = System.Text.Encoding.ASCII.GetBytes("When I grow up, I want to be a watermelon");
        byte[] authenticationSecret = Base64Url("BTBZMqHH6r4Tts7J_aSIgg");
        byte[] receiverPublic = Base64Url("BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4");
        byte[] senderPrivate = Base64Url("yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw");
        byte[] senderPublic = Base64Url("BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8");
        byte[] salt = Base64Url("DGv6ra1nlYgDCS1FRnbzlw");
        string expected = "DGv6ra1nlYgDCS1FRnbzlwAAEABBBP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27ml" +
            "mlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A_yl95bQpu6cVPT" +
            "pK4Mqgkf1CXztLVBSt2Ks3oZwbuwXPXLWyouBWLVWGNWQexSgSxsj_Qulcy4a-fN";

        byte[] encrypted = encryptor.EncryptWithMaterial(
            plaintext,
            receiverPublic,
            authenticationSecret,
            salt,
            senderPrivate,
            senderPublic);

        Assert.That(EncodeBase64Url(encrypted), Is.EqualTo(expected));
    }

    [Test]
    public void EncryptionRejectsPayloadsThatCannotFitSinglePushRecord()
    {
        WebPushEncryptor encryptor = new WebPushEncryptor();
        byte[] receiverPublic = Base64Url("BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4");

        Assert.That(
            () => encryptor.Encrypt(new byte[WebPushEncryptor.MaximumPlaintextLength + 1], receiverPublic, new byte[16]),
            Throws.TypeOf<ArgumentException>());
    }

    private static byte[] Base64Url(string value)
    {
        string encoded = value.Replace('-', '+').Replace('_', '/');
        while (encoded.Length % 4 != 0)
        {
            encoded = encoded + "=";
        }

        return Convert.FromBase64String(encoded);
    }

    private static string EncodeBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
