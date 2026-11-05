namespace jpeg.Tests;

internal class ArithmeticDecoderStateMachineTests
{
    [Test]
    public void StateMachine_ExposesJpegProbabilityTransitionTable()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[0]);

        Assert.That(decoder.GetQeValue(0), Is.EqualTo(0x5A1D));
        Assert.That(decoder.GetNextLpsIndex(0), Is.EqualTo(1));
        Assert.That(decoder.GetNextMpsIndex(0), Is.EqualTo(1));
        Assert.That(decoder.ShouldSwitchMps(0), Is.True);
        Assert.That(decoder.GetQeValue(113), Is.EqualTo(0x5A1D));
        Assert.That(decoder.GetNextLpsIndex(113), Is.EqualTo(113));
        Assert.That(decoder.GetNextMpsIndex(113), Is.EqualTo(113));
        Assert.That(decoder.ShouldSwitchMps(113), Is.False);
    }

    [Test]
    public void StateMachine_DecodesMpsWithoutStateChangeWhenIntervalRemainsNormalized()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[0]);
        int[] statistics = new int[1];
        decoder.A = 0x10000;
        decoder.C = 0;
        decoder.Ct = 0;

        int bit = decoder.DecodeBit(statistics, 0);

        Assert.That(bit, Is.EqualTo(0));
        Assert.That(statistics[0], Is.EqualTo(0));
        Assert.That(decoder.A, Is.EqualTo(0xA5E3));
    }

    [Test]
    public void StateMachine_DecodesLpsAndSwitchesMpsWhenTableRequiresIt()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[0]);
        int[] statistics = new int[1];
        decoder.A = 0x10000;
        decoder.C = 0xA5E3;
        decoder.Ct = 0;

        int bit = decoder.DecodeBit(statistics, 0);

        Assert.That(bit, Is.EqualTo(1));
        Assert.That(statistics[0], Is.EqualTo(0x80 | 1));
        Assert.That(decoder.A, Is.EqualTo(0x5A1D));
        Assert.That(decoder.C, Is.EqualTo(0));
    }

    [Test]
    public void StateMachine_DecodesLpsWithoutSwitchingMpsForStableState()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[0]);
        int[] statistics = new int[] { 1 };
        decoder.A = 0x10000;
        decoder.C = 0xDA7A;
        decoder.Ct = 0;

        int bit = decoder.DecodeBit(statistics, 0);

        Assert.That(bit, Is.EqualTo(1));
        Assert.That(statistics[0], Is.EqualTo(14));
        Assert.That(decoder.A, Is.EqualTo(0x2586));
    }

    [Test]
    public void StateMachine_DecodesMpsExchangeWhenMpsIntervalIsSmaller()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[0]);
        int[] statistics = new int[1];
        decoder.A = 0x8000;
        decoder.C = 0;
        decoder.Ct = 0;

        int bit = decoder.DecodeBit(statistics, 0);

        Assert.That(bit, Is.EqualTo(1));
        Assert.That(statistics[0], Is.EqualTo(0x80 | 1));
    }

    [Test]
    public void StateMachine_RenormalizesInitialBytesAndUnstuffsEntropyByte()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[] { 0x12, 0xFF, 0x00 });

        decoder.Renormalize();

        Assert.That(decoder.A, Is.EqualTo(0x10000));
        Assert.That(decoder.C, Is.EqualTo(0x12FF));
        Assert.That(decoder.Ct, Is.EqualTo(0));
        Assert.That(decoder.StuffedByteCount, Is.EqualTo(1));
        Assert.That(decoder.HasMarker, Is.False);
    }

    [Test]
    public void StateMachine_RecordsMarkerLookaheadAndSuppliesZeroDataAfterMarker()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[] { 0x12, 0xFF, 0xD0 });

        decoder.Renormalize();
        int following = decoder.ReadArithmeticByte();
        global::jpeg.JpegScannedMarker marker = decoder.GetMarkerLookahead();

        Assert.That(decoder.A, Is.EqualTo(0x10000));
        Assert.That(decoder.C, Is.EqualTo(0x1200));
        Assert.That(decoder.HasMarker, Is.True);
        Assert.That(following, Is.EqualTo(0));
        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
        decoder.ConsumeMarkerLookahead();
        Assert.That(decoder.HasMarker, Is.False);
    }

    [Test]
    public void StateMachine_ResetsStatisticsAndRejectsInvalidInputs()
    {
        global::jpeg.JpegArithmeticDecoderStateMachine decoder = new global::jpeg.JpegArithmeticDecoderStateMachine(new byte[0]);
        int[] statistics = new int[] { 1, 2, 3 };

        decoder.ResetStatistics(statistics);

        Assert.That(statistics, Is.EqualTo(new int[] { 0, 0, 0 }));

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegArithmeticDecoderStateMachine.FromReader((global::jpeg.JpegByteReader)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeBit(null!, 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                decoder.DecodeBit(statistics, 3);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                decoder.GetQeValue(114);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.GetMarkerLookahead();
            });
    }
}
