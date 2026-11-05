using System.Text;

namespace sdp.Tests;

public static class SdpFixtureLosslessAssert
{
    public static void AssertExact(string input, bool expectedParseSuccess)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(input);
        SdpParseResult parsed = new SdpParser().ParseUtf8Bytes(expectedBytes, LosslessParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.EqualTo(expectedParseSuccess), ParseDiagnostics(parsed));
            Assert.That(parsed.Description, Is.Not.Null, ParseDiagnostics(parsed));
            Assert.That(parsed.BytesConsumed, Is.EqualTo(expectedBytes.Length));
        });

        SdpWriteOptions writeOptions = new SdpWriteOptions { Mode = SdpWriteMode.Lossless };
        SdpWriteResult textResult = new SdpWriter().WriteText(parsed.Description!, writeOptions);
        SdpWriteResult byteResult = new SdpWriter().WriteUtf8Bytes(parsed.Description!, writeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(textResult.Success, Is.True, WriteDiagnostics(textResult));
            Assert.That(textResult.Text, Is.EqualTo(input));
            Assert.That(textResult.CharactersWritten, Is.EqualTo(input.Length));
            Assert.That(byteResult.Success, Is.True, WriteDiagnostics(byteResult));
            Assert.That(byteResult.Utf8Bytes, Is.EqualTo(expectedBytes));
            Assert.That(byteResult.BytesWritten, Is.EqualTo(expectedBytes.Length));
        });
    }

    static SdpParseOptions LosslessParseOptions()
    {
        return new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowLfLineEndings = true,
            AllowCrLineEndings = true
        };
    }

    static string ParseDiagnostics(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string WriteDiagnostics(SdpWriteResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
