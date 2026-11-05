using System;
using fix;

namespace fix.Tests;

[TestFixture]
public class ExceptionTests
{
    [Test]
    public void PublicExceptionsPreserveMessagesWhenThrown()
    {
        string[] messages =
        [
            "encoding failed",
            "dictionary failed",
            "validation failed",
            "session failed",
            "store failed",
            "transport failed",
            "orchestra failed",
            "generation failed"
        ];
        Exception[] exceptions =
        [
            new FixEncodingException(messages[0]),
            new FixDictionaryException(messages[1]),
            new FixValidationException(messages[2]),
            new FixSessionException(messages[3]),
            new FixStoreException(messages[4]),
            new FixTransportException(messages[5]),
            new OrchestraException(messages[6]),
            new ModelGenerationException(messages[7])
        ];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < exceptions.Length; index++)
            {
                Assert.That(exceptions[index].Message, Is.EqualTo(messages[index]));
                Assert.That(Capture(exceptions[index]), Is.SameAs(exceptions[index]));
            }
        });
    }

    private static Exception Capture(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }
}
