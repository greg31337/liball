using System.Buffers.Binary;

namespace avif.Tests;

public class SequenceAuthoringTests
{
    [Test]
    public void TimedSampleEntryColorAndIccMetadataReachDecodedFrames()
    {
        byte[] iccBytes = BuildMinimalIccProfile();
        AvifIccProfile profile = AvifIccProfile.Create(iccBytes);
        AvifColorInfo color = AvifColorInfo.Create(
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Smpte2084,
            AvifMatrixCoefficients.Bt2020NonConstantLuminance,
            AvifRange.Limited);
        AvifImage firstImage = CreateImage(2, 2, 41);
        AvifImage secondImage = CreateImage(2, 2, 93);
        firstImage.SetColorInfo(color);
        secondImage.SetColorInfo(color);
        firstImage.SetIccProfile(profile);
        secondImage.SetIccProfile(profile);
        AvifSequence sequence = AvifSequence.Create(1000);
        sequence.AddFrame(AvifFrame.Create(firstImage, 100));
        sequence.AddFrame(AvifFrame.Create(secondImage, 100));
        AvifWriter writer = AvifWriter.Create();
        writer.AddSequence(sequence);
        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDecodeResult decoded = reader.DecodeFrame(1);

        Assert.Multiple(() =>
        {
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries,
                Is.EqualTo(AvifColorPrimaries.Bt2020));
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).TransferCharacteristics,
                Is.EqualTo(AvifTransferCharacteristics.Smpte2084));
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).MatrixCoefficients,
                Is.EqualTo(AvifMatrixCoefficients.Bt2020NonConstantLuminance));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetRange(), Is.EqualTo(AvifRange.Limited));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetIccProfile(), Is.Not.Null);
            Assert.That(((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetIccProfile() ?? throw new InvalidOperationException("Invalid internal state.")).GetData(),
                Is.EqualTo(iccBytes));
        });

        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
        firstImage.Close();
        secondImage.Close();
    }

    [Test]
    public void AuthorsVariableDurationsFiniteRepetitionAndDecodableStillFallback()
    {
        AvifImage firstImage = CreateImage(3, 2, 29);
        AvifImage secondImage = CreateImage(3, 2, 173);
        AvifSequence sequence = AvifSequence.Create(1000);
        AvifResultCode firstAdded = sequence.AddFrame(
            AvifFrame.Create(firstImage, 100));
        AvifResultCode secondAdded = sequence.AddFrame(
            AvifFrame.Create(secondImage, 250));
        AvifResultCode repetition = sequence.SetRepetitionCount(2);
        AvifWriter writer = AvifWriter.Create();
        uint trackId = writer.AddSequence(sequence);
        firstImage.Close();
        secondImage.Close();

        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDocument? document = reader.GetDocument();
        AvifMovie? movie = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie();
        AvifTrack? track = movie == null ? null : movie.FindTrack(trackId);
        AvifDecodeResult fallback = reader.DecodePrimaryImage();
        AvifDecodeResult first = reader.DecodeFrame(0);
        AvifDecodeResult second = reader.DecodeFrame(1);

        Assert.Multiple(() =>
        {
            Assert.That(firstAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(repetition, Is.EqualTo(AvifResultCode.Success));
            Assert.That(trackId, Is.EqualTo(1));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("avis"), Is.True);
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("msf1"), Is.True);
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("avio"), Is.True);
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(), Is.EqualTo(1));
            Assert.That((movie ?? throw new InvalidOperationException("Invalid internal state.")).GetTimescale(), Is.EqualTo(1000));
            Assert.That(movie.GetDuration(), Is.EqualTo(1050));
            Assert.That((track ?? throw new InvalidOperationException("Invalid internal state.")).GetHandlerType(), Is.EqualTo("pict"));
            Assert.That(track.GetMediaTimescale(), Is.EqualTo(1000));
            Assert.That(track.GetMediaDuration(), Is.EqualTo(350));
            Assert.That(track.GetDuration(), Is.EqualTo(1050));
            Assert.That(track.GetSampleDescriptionCount(), Is.EqualTo(1));
            Assert.That(track.GetSampleCount(), Is.EqualTo(2));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(100));
            Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(250));
            Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDecodeTime(), Is.EqualTo(100));
            Assert.That(track.HasKnownRepetitionCount(), Is.True);
            Assert.That(track.IsRepetitionInfinite(), Is.False);
            Assert.That(track.GetRepetitionCount(), Is.EqualTo(2));
            Assert.That(fallback.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(second.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(fallback.GetImage(), 29);
        AssertPlaneValue(first.GetImage(), 29);
        AssertPlaneValue(second.GetImage(), 173);

        (fallback.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (first.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (second.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsMatchedAlphaTrackAndInfiniteRepetition()
    {
        AvifImage firstColor = CreateImage(2, 2, 31);
        AvifImage secondColor = CreateImage(2, 2, 97);
        AvifImage firstAlpha = CreateImage(2, 2, 201);
        AvifImage secondAlpha = CreateImage(2, 2, 113);
        AvifSequence sequence = AvifSequence.Create(600);
        sequence.AddFrame(AvifFrame.Create(
            firstColor, firstAlpha, 200, true));
        sequence.AddFrame(AvifFrame.Create(
            secondColor, secondAlpha, 400, true));
        sequence.SetInfiniteRepetition();
        AvifWriter writer = AvifWriter.Create();
        uint trackId = writer.AddSequence(sequence);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifMovie? movie = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie();
        AvifTrack? colorTrack = (movie ?? throw new InvalidOperationException("Invalid internal state.")).FindTrack(trackId);
        AvifTrack? alphaTrack = movie.FindTrack(2);
        AvifImageItem? alphaFallback = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(2);

        Assert.Multiple(() =>
        {
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(movie.GetTrackCount(), Is.EqualTo(2));
            Assert.That(movie.IsDurationIndefinite(), Is.True);
            Assert.That((colorTrack ?? throw new InvalidOperationException("Invalid internal state.")).IsRepetitionInfinite(), Is.True);
            Assert.That((alphaTrack ?? throw new InvalidOperationException("Invalid internal state.")).IsAuxiliaryFor(trackId), Is.True);
            Assert.That(alphaTrack.GetAuxiliaryKind(),
                Is.EqualTo(AvifAuxiliaryKind.Alpha));
            Assert.That(alphaTrack.GetSampleCount(), Is.EqualTo(2));
            Assert.That((alphaTrack.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(),
                Is.EqualTo((colorTrack.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration()));
            Assert.That((alphaTrack.GetSampleDescription(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth(),
                Is.EqualTo((colorTrack.GetSampleDescription(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth()));
            Assert.That((alphaFallback ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That(alphaFallback.IsAuxiliaryImage(), Is.True);
            Assert.That(alphaFallback.GetAuxiliaryKind(),
                Is.EqualTo(AvifAuxiliaryKind.Alpha));
        });

        firstColor.Close();
        secondColor.Close();
        firstAlpha.Close();
        secondAlpha.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsKeyframeDependencyAndAv1SampleGroups()
    {
        AvifImage firstImage = CreateImage(2, 2, 7);
        AvifImage secondImage = CreateImage(2, 2, 71);
        AvifImage thirdImage = CreateImage(2, 2, 199);
        AvifFrame firstFrame = AvifFrame.Create(firstImage, 10, true);
        AvifFrame secondFrame = AvifFrame.Create(secondImage, 20, false);
        AvifFrame thirdFrame = AvifFrame.Create(thirdImage, 30, true);
        firstFrame.SetMultipleFrames(true);
        firstFrame.AddMetadataGroup(0x04112233);
        secondFrame.SetForwardKeyFrameDistance(0);
        secondFrame.AddMetadataGroup(0x04112233);
        thirdFrame.SetSwitchFrame(true);
        thirdFrame.AddMetadataGroup(0x01000000);
        AvifSequence sequence = AvifSequence.Create(100);
        sequence.AddFrame(firstFrame);
        sequence.AddFrame(secondFrame);
        sequence.AddFrame(thirdFrame);
        AvifWriter writer = AvifWriter.Create();
        uint trackId = writer.AddSequence(sequence);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifTrack? track = ((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetMovie() ?? throw new InvalidOperationException("Invalid internal state.")).FindTrack(trackId);
        AvifSample? first = (track ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(0);
        AvifSample? second = track.GetSample(1);
        AvifSample? third = track.GetSample(2);

        Assert.Multiple(() =>
        {
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstFrame.IsKeyFrame(), Is.True);
            Assert.That(secondFrame.IsKeyFrame(), Is.False);
            Assert.That(firstFrame.GetMetadataGroupParameter(0),
                Is.EqualTo(0x04112233));
            Assert.That(firstFrame.GetMetadataGroupParameter(1), Is.Zero);
            Assert.That(((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("avio"), Is.False);
            Assert.That((first ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.True);
            Assert.That(first.GetDependsOn(), Is.EqualTo(2));
            Assert.That(first.HasMultipleFrames(), Is.True);
            Assert.That(first.GetMetadataGroupCount(), Is.EqualTo(1));
            Assert.That((second ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.False);
            Assert.That(second.GetDependsOn(), Is.EqualTo(1));
            Assert.That(second.IsForwardKeyFrame(), Is.True);
            Assert.That(second.GetForwardKeyFrameDistance(), Is.Zero);
            Assert.That(second.GetRecoverySampleIndex(), Is.EqualTo(2));
            Assert.That(second.GetMetadataGroupCount(), Is.EqualTo(1));
            Assert.That((third ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.True);
            Assert.That(third.IsSwitchFrame(), Is.True);
            Assert.That(third.GetMetadataGroupCount(), Is.EqualTo(1));
            Assert.That((third.GetMetadataGroup(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetParameter(),
                Is.EqualTo(0x01000000));
        });

        firstImage.Close();
        secondImage.Close();
        thirdImage.Close();
        writer.Close();
    }

    [Test]
    public void RejectsEmptyAndIncompatibleSequencesWithoutConsumingItemIds()
    {
        AvifWriter writer = AvifWriter.Create();
        AvifSequence empty = AvifSequence.Create();
        uint emptyTrack = writer.AddSequence(empty);
        AvifImage first = CreateImage(2, 2, 9);
        AvifImage wrongSize = CreateImage(3, 2, 11);
        AvifSequence incompatible = AvifSequence.Create();
        AvifResultCode firstAdded = incompatible.AddFrame(
            AvifFrame.Create(first, 1));
        AvifResultCode secondAdded = incompatible.AddFrame(
            AvifFrame.Create(wrongSize, 1));
        uint nextItemId = writer.AddImage(first);

        Assert.Multiple(() =>
        {
            Assert.That(emptyTrack, Is.Zero);
            Assert.That(firstAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondAdded,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(nextItemId, Is.EqualTo(1));
        });

        first.Close();
        wrongSize.Close();
        writer.Close();
    }

    [Test]
    public void CountsEveryTimedSampleAgainstTheFrameLimit()
    {
        AvifEncoderOptions options = new AvifEncoderOptions();
        (options.Limits ?? throw new InvalidOperationException("Invalid internal state.")).MaxFrameCount = 2;
        AvifWriter writer = AvifWriter.Create(options);
        AvifImage first = CreateImage(2, 2, 15);
        AvifImage second = CreateImage(2, 2, 27);
        AvifSequence sequence = AvifSequence.Create();
        sequence.AddFrame(AvifFrame.Create(first, 1));
        sequence.AddFrame(AvifFrame.Create(second, 1));

        uint trackId = writer.AddSequence(sequence);
        uint extraItemId = writer.AddImage(first);

        Assert.Multiple(() =>
        {
            Assert.That(trackId, Is.EqualTo(1));
            Assert.That(extraItemId, Is.Zero);
            Assert.That(writer.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
        });

        first.Close();
        second.Close();
        writer.Close();
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static void AssertPlaneValue(AvifImage? image, byte expected)
    {
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            int column = 0;
            while (column < plane.GetWidth())
            {
                Assert.That(plane.GetRowSpan(row)[column], Is.EqualTo(expected));
                column++;
            }
            row++;
        }
    }

    static byte[] BuildMinimalIccProfile()
    {
        byte[] data = new byte[132];
        BinaryPrimitives.WriteUInt32BigEndian(data, 132);
        data[8] = 4;
        data[9] = 64;
        WriteFourCc(data, 12, "mntr");
        WriteFourCc(data, 16, "RGB ");
        WriteFourCc(data, 20, "XYZ ");
        WriteFourCc(data, 36, "acsp");
        return data;
    }

    static void WriteFourCc(byte[] data, int offset, string value)
    {
        data[offset] = (byte)value[0];
        data[offset + 1] = (byte)value[1];
        data[offset + 2] = (byte)value[2];
        data[offset + 3] = (byte)value[3];
    }
}
