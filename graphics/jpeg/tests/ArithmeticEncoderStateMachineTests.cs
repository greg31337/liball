namespace jpeg.Tests;

internal class ArithmeticEncoderStateMachineTests
{
    [Test]
    public void StateMachine_StartsWithJpegArithmeticEncoderRegisters()
    {
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();

        Assert.That(encoder.A, Is.EqualTo(0x10000));
        Assert.That(encoder.C, Is.EqualTo(0));
        Assert.That(encoder.Ct, Is.EqualTo(11));
        Assert.That(encoder.Buffer, Is.EqualTo(-1));
        Assert.That(encoder.Sc, Is.EqualTo(0));
        Assert.That(encoder.Zc, Is.EqualTo(0));
    }

    [Test]
    public void StateMachine_EncodesMpsWithoutRenormalizationWhenIntervalRemainsLarge()
    {
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();
        int[] statistics = new int[1];

        encoder.EncodeBit(statistics, 0, 0);

        Assert.That(statistics[0], Is.EqualTo(0));
        Assert.That(encoder.A, Is.EqualTo(0xA5E3));
        Assert.That(encoder.C, Is.EqualTo(0));
        Assert.That(encoder.Ct, Is.EqualTo(11));
        Assert.That(encoder.ToArray().Length, Is.EqualTo(0));
    }

    [Test]
    public void StateMachine_EncodesLpsAndSwitchesMpsWhenTableRequiresIt()
    {
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();
        int[] statistics = new int[1];

        encoder.EncodeBit(statistics, 0, 1);

        Assert.That(statistics[0], Is.EqualTo(0x80 | 1));
        Assert.That(encoder.A, Is.GreaterThanOrEqualTo(0x8000));
        Assert.That(encoder.C, Is.GreaterThan(0));
        Assert.That(encoder.Ct, Is.EqualTo(10));
    }

    [Test]
    public void StateMachine_RoundTripsEncodedDecisionSequenceThroughDecoder()
    {
        int[] decisions = new int[] { 0, 1, 0, 1, 1, 0, 0, 1 };
        int[] encoderStatistics = new int[1];
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();
        int index = 0;
        while (index < decisions.Length)
        {
            encoder.EncodeBit(encoderStatistics, 0, decisions[index]);
            index = index + 1;
        }

        encoder.Finish();

        byte[] encoded = AppendEndOfImageMarker(encoder.ToArray());
        int[] decoderStatistics = new int[1];
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(encoded);

        index = 0;
        while (index < decisions.Length)
        {
            int bit = decoder.DecodeBit(decoderStatistics, 0);
            Assert.That(bit, Is.EqualTo(decisions[index]));
            index = index + 1;
        }

        Assert.That(encoded.Length, Is.GreaterThan(0));
        Assert.That(decoderStatistics[0], Is.EqualTo(encoderStatistics[0]));
    }

    [Test]
    public void StateMachine_FinishFlushesPendingBytesOnlyOnce()
    {
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();
        int[] statistics = new int[1];
        encoder.EncodeBit(statistics, 0, 1);

        encoder.Finish();
        byte[] first = encoder.ToArray();
        encoder.Finish();
        byte[] second = encoder.ToArray();

        Assert.That(first.Length, Is.GreaterThan(0));
        Assert.That(second, Is.EqualTo(first));
        Assert.That(encoder.Finished, Is.True);
    }

    [Test]
    public void StateMachine_EmitsPendingZerosAndStuffedStackedFFBytes()
    {
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();
        encoder.Zc = 2;
        encoder.Sc = 1;

        encoder.EmitStackedFF();

        Assert.That(encoder.ToArray(), Is.EqualTo(new byte[] { 0, 0, 0xFF, 0 }));
        Assert.That(encoder.Zc, Is.EqualTo(0));
        Assert.That(encoder.Sc, Is.EqualTo(0));
    }

    [Test]
    public void StateMachine_ResetStatisticsClearsContexts()
    {
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();
        int[] statistics = new int[] { 1, 2, 3 };

        encoder.ResetStatistics(statistics);

        Assert.That(statistics, Is.EqualTo(new int[] { 0, 0, 0 }));
    }

    [Test]
    public void StateMachine_RejectsInvalidInputs()
    {
        global::jpeg.JpegArithmeticEncoderStateMachine encoder = new global::jpeg.JpegArithmeticEncoderStateMachine();
        int[] statistics = new int[1];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegArithmeticEncoderStateMachine.FromWriter((global::jpeg.JpegByteWriter)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.EncodeBit(null!, 0, 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                encoder.EncodeBit(statistics, 1, 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                encoder.EncodeBit(statistics, 0, 2);
            });

        encoder.Finish();
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeBit(statistics, 0, 0);
            });
    }

    public byte[] AppendEndOfImageMarker(byte[] entropy)
    {
        byte[] result = new byte[entropy.Length + 2];
        int index = 0;
        while (index < entropy.Length)
        {
            result[index] = entropy[index];
            index = index + 1;
        }

        result[index] = 0xFF;
        result[index + 1] = 0xD9;
        return result;
    }
}
