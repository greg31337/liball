namespace avif.Tests;

public class TimedSequenceTests
{
    [Test]
    public void QueriesTimingKeyframesAndAllTrackEncodedRanges()
    {
        AvifImageSource source = CreateSource(true, true, false, false, 0);

        AvifFrameTiming? timing = source.GetFrameTiming(2);
        AvifByteRange? encoded = source.GetFrameEncodedRange(3);
        AvifByteRange? dependency = source.GetFrameDecodeRange(2);

        Assert.Multiple(() =>
        {
            Assert.That(source.GetFrameCount(), Is.EqualTo(4));
            Assert.That(source.IsFrameKeyFrame(0), Is.True);
            Assert.That(source.IsFrameKeyFrame(2), Is.False);
            Assert.That(source.IsFrameKeyFrame(3), Is.True);
            Assert.That(source.GetNearestKeyFrameIndex(2), Is.Zero);
            Assert.That(source.GetNearestKeyFrameIndex(3), Is.EqualTo(3));
            Assert.That(source.GetNearestKeyFrameIndex(4), Is.EqualTo(-1));
            Assert.That((timing ?? throw new InvalidOperationException("Invalid internal state.")).GetTimescale(), Is.EqualTo(1000));
            Assert.That(timing.GetPresentationTime(), Is.EqualTo(300));
            Assert.That(timing.GetPresentationTimeSeconds(), Is.EqualTo(0.3));
            Assert.That(timing.GetDuration(), Is.EqualTo(100));
            Assert.That(timing.GetDurationSeconds(), Is.EqualTo(0.1));
            Assert.That((encoded ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(), Is.EqualTo(180));
            Assert.That(encoded.GetEndOffset(), Is.EqualTo(490));
            Assert.That((dependency ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(), Is.EqualTo(100));
            Assert.That(dependency.GetLength(), Is.EqualTo(360));
        });
    }

    [Test]
    public void KeyframeRequiresSynchronizationOnAssociatedAlphaTrack()
    {
        AvifImageSource source = CreateSource(true, false, false, false, 0);

        Assert.Multiple(() =>
        {
            Assert.That(((source.GetSequenceTrack() ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(3) ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.True);
            Assert.That(((source.GetAuxiliaryTrack(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(3) ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.False);
            Assert.That(source.IsFrameKeyFrame(3), Is.False);
            Assert.That(source.GetNearestKeyFrameIndex(3), Is.Zero);
        });
    }

    [Test]
    public void IncompleteAssociatedAlphaSamplesInvalidateFrameQueries()
    {
        ParsedMovieTrack color = CreateTrack(1, false, true);
        ParsedMovieTrack alpha = CreateTrack(2, true, true);
        alpha.SampleTable.Samples.RemoveAt(3);
        alpha.References.Add(new ParsedTrackReference("auxl", 1));
        ParsedMovie parsedMovie = new ParsedMovie();
        parsedMovie.Timescale = 1000;
        parsedMovie.Duration = 500;
        parsedMovie.NextTrackId = 3;
        parsedMovie.Tracks.Add(color);
        parsedMovie.Tracks.Add(alpha);
        AvifMovie movie = new AvifMovie(parsedMovie);
        AvifImageSource source = new AvifImageSource(movie, movie.FindTrack(1));

        Assert.Multiple(() =>
        {
            Assert.That(source.GetFrameCount(), Is.EqualTo(4));
            Assert.That(source.IsFrameKeyFrame(3), Is.False);
            Assert.That(source.GetFrameEncodedRange(3), Is.Null);
            Assert.That(source.GetFrameDecodeRange(3), Is.Null);
        });
    }

    [Test]
    public void SequentialAndIndexedDecodeScheduleOnlyRequiredFrames()
    {
        AvifSequence? sequence = CreateSource(
            true, true, false, false, 0).CreateSequence();

        Assert.That((sequence ?? throw new InvalidOperationException("Invalid internal state.")).DecodeNextFrame(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.GetCurrentFrameIndex(), Is.Zero);
            Assert.That(sequence.GetLastDecodeStartIndex(), Is.Zero);
            Assert.That(sequence.GetLastDecodeFrameCount(), Is.EqualTo(1));
        });

        Assert.That(sequence.DecodeNextFrame(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.GetCurrentFrameIndex(), Is.EqualTo(1));
            Assert.That(sequence.GetLastDecodeStartIndex(), Is.EqualTo(1));
            Assert.That(sequence.GetLastDecodeFrameCount(), Is.EqualTo(1));
        });

        Assert.That(sequence.DecodeFrame(3), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.GetLastDecodeStartIndex(), Is.EqualTo(3));
            Assert.That(sequence.GetLastDecodeFrameCount(), Is.EqualTo(1));
            Assert.That((sequence.GetCurrentFrameTiming() ?? throw new InvalidOperationException("Invalid internal state.")).GetPresentationTime(),
                Is.EqualTo(400));
        });

        Assert.That(sequence.DecodeFrame(2), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.GetLastDecodeStartIndex(), Is.Zero);
            Assert.That(sequence.GetLastDecodeFrameCount(), Is.EqualTo(3));
            Assert.That((sequence.GetCurrentFrameDecodeRange() ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(), Is.EqualTo(100));
        });

        Assert.That(sequence.DecodeFrame(2), Is.EqualTo(AvifResultCode.Success));
        Assert.That(sequence.GetLastDecodeFrameCount(), Is.Zero);
        Assert.That(sequence.DecodeFrame(4), Is.EqualTo(AvifResultCode.NotFound));
        Assert.That(sequence.Reset(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(sequence.GetCurrentFrameIndex(), Is.EqualTo(-1));
    }

    [Test]
    public void SequentialDecodeHonorsFiniteInfiniteAndUnknownRepetition()
    {
        AvifSequence? finite = CreateSource(
            false, true, true, false, 1).CreateSequence();
        AvifSequence? infinite = CreateSource(
            false, true, true, true, 0).CreateSequence();
        AvifSequence? unknown = CreateSource(
            false, true, false, false, 0).CreateSequence();

        DecodeCompleteIteration(finite);
        Assert.That((finite ?? throw new InvalidOperationException("Invalid internal state.")).DecodeNextFrame(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(finite.HasKnownRepetitionCount(), Is.True);
            Assert.That(finite.IsRepetitionInfinite(), Is.False);
            Assert.That(finite.GetRepetitionCount(), Is.EqualTo(1));
            Assert.That(finite.GetIterationIndex(), Is.EqualTo(1));
            Assert.That(finite.GetCurrentFrameIndex(), Is.Zero);
        });
        finite.DecodeNextFrame();
        finite.DecodeNextFrame();
        finite.DecodeNextFrame();
        Assert.That(finite.DecodeNextFrame(), Is.EqualTo(AvifResultCode.EndOfData));

        DecodeCompleteIteration(infinite);
        Assert.That((infinite ?? throw new InvalidOperationException("Invalid internal state.")).DecodeNextFrame(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(infinite.IsRepetitionInfinite(), Is.True);
            Assert.That(infinite.GetIterationIndex(), Is.EqualTo(1));
        });
        Assert.That(infinite.Reset(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(infinite.GetIterationIndex(), Is.Zero);
            Assert.That(infinite.GetCurrentFrameIndex(), Is.EqualTo(-1));
        });

        DecodeCompleteIteration(unknown);
        Assert.Multiple(() =>
        {
            Assert.That((unknown ?? throw new InvalidOperationException("Invalid internal state.")).HasKnownRepetitionCount(), Is.False);
            Assert.That(unknown.DecodeNextFrame(), Is.EqualTo(AvifResultCode.EndOfData));
        });
    }

    static void DecodeCompleteIteration(AvifSequence? sequence)
    {
        int index = 0;
        while (index < (sequence ?? throw new InvalidOperationException("Invalid internal state.")).GetFrameCount())
        {
            Assert.That(sequence.DecodeNextFrame(), Is.EqualTo(AvifResultCode.Success));
            index++;
        }
    }

    static AvifImageSource CreateSource(
        bool includeAlpha,
        bool alphaLastSync,
        bool repetitionKnown,
        bool repetitionInfinite,
        ulong repetitionCount)
    {
        ParsedMovieTrack color = CreateTrack(1, false, true);
        color.RepetitionKnown = repetitionKnown;
        color.RepetitionInfinite = repetitionInfinite;
        color.RepetitionCount = repetitionCount;
        ParsedMovie movie = new ParsedMovie();
        movie.Timescale = 1000;
        movie.Duration = 500;
        movie.NextTrackId = includeAlpha ? 3U : 2U;
        movie.Tracks.Add(color);
        if (includeAlpha)
        {
            ParsedMovieTrack alpha = CreateTrack(2, true, alphaLastSync);
            alpha.References.Add(new ParsedTrackReference("auxl", 1));
            movie.Tracks.Add(alpha);
        }
        AvifMovie publicMovie = new AvifMovie(movie);
        return new AvifImageSource(publicMovie, publicMovie.FindTrack(1));
    }

    static ParsedMovieTrack CreateTrack(
        uint trackId,
        bool auxiliary,
        bool lastSync)
    {
        ParsedMovieTrack track = new ParsedMovieTrack();
        track.TrackId = trackId;
        track.HandlerType = auxiliary ? "auxv" : "pict";
        track.MediaTimescale = 1000;
        track.MediaDuration = 500;
        ulong[] offsets = auxiliary
            ? new ulong[] { 400, 420, 450, 480 }
            : new ulong[] { 100, 120, 150, 180 };
        ulong[] decodeTimes = new ulong[] { 0, 100, 300, 400 };
        uint[] durations = new uint[] { 100, 200, 100, 100 };
        int index = 0;
        while (index < offsets.Length)
        {
            ParsedTrackSample sample = new ParsedTrackSample(index, 10);
            sample.Offset = offsets[index];
            sample.DecodeTime = decodeTimes[index];
            sample.Duration = durations[index];
            sample.Sync = index == 0 || index == 3 && lastSync;
            track.SampleTable.Samples.Add(sample);
            index++;
        }
        return track;
    }
}
