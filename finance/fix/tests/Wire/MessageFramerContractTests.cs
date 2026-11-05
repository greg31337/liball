using System;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class MessageFramerContractTests
{
    [TestCase("tag-value")]
    [TestCase("sofh")]
    public void ImplementationsShareIncrementalAndNonMutatingReadBehavior(string framing)
    {
        IMessageFramer framer = CreateFramer(framing);
        byte[] frame = CreateFrame(framing);
        byte[] original = frame.ToArray();

        for (int length = 0; length < frame.Length; length++)
        {
            FrameResult partial = framer.ReadFrame(frame.AsSpan(0, length), new FrameOptions());
            Assert.That(partial.Status, Is.EqualTo(FrameStatus.Incomplete), framing + " prefix " + length);
            Assert.That(partial.Required, Is.GreaterThan(length), framing + " prefix " + length);
        }

        FrameResult complete = framer.ReadFrame(frame, new FrameOptions());

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(complete.FrameOffset, Is.Zero);
            Assert.That(complete.FrameLength, Is.EqualTo(frame.Length));
            Assert.That(complete.Consumed, Is.EqualTo(frame.Length));
            Assert.That(frame, Is.EqualTo(original));
        });
    }

    [TestCase("tag-value")]
    [TestCase("sofh")]
    public void ImplementationsShareStableInvalidLimitResults(string framing)
    {
        IMessageFramer framer = CreateFramer(framing);
        byte[] frame = CreateFrame(framing);

        FrameResult first = framer.ReadFrame(frame, new FrameOptions { MaxFrameSize = -1 });
        FrameResult second = framer.ReadFrame(frame, new FrameOptions { MaxFrameSize = -1 });

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(first.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(second.Status, Is.EqualTo(first.Status));
            Assert.That(second.ErrorCode, Is.EqualTo(first.ErrorCode));
            Assert.That(second.ErrorOffset, Is.EqualTo(first.ErrorOffset));
        });
    }

    private static IMessageFramer CreateFramer(string framing)
    {
        if (framing == "tag-value") return new TagValueMessageFramer();
        return new SofhMessageFramer();
    }

    private static byte[] CreateFrame(string framing)
    {
        if (framing == "tag-value") return CreateTagValueFrame();
        return [0, 0, 0, 7, (byte)(FixEncodingTypes.SbeVersion10BigEndian >> 8), (byte)(FixEncodingTypes.SbeVersion10BigEndian & 255), 42];
    }

    private static byte[] CreateTagValueFrame()
    {
        string prefix = "8=FIX.4.4\u00019=5\u000135=0\u0001";
        byte[] bytes = Encoding.ASCII.GetBytes(prefix);
        int sum = 0;
        for (int index = 0; index < bytes.Length; index++) sum += bytes[index];
        return Encoding.ASCII.GetBytes(prefix + "10=" + (sum % 256).ToString("D3") + "\u0001");
    }
}
