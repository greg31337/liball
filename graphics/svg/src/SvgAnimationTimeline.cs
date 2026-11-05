namespace svg;

/// <summary>Stores mutable bounded work counters for one timeline evaluation.</summary>
internal class SvgAnimationTimelineWork
{
    internal SvgAnimationTimelineOptions Options;
    internal SvgAnimationTimelineResult Result;
    internal int TimingCount;
    internal int InstanceCount;
    internal bool LimitFailed;

    internal SvgAnimationTimelineWork(SvgAnimationTimelineOptions options, SvgAnimationTimelineResult result)
    {
        Options = options;
        Result = result;
        TimingCount = 0;
        InstanceCount = 0;
        LimitFailed = false;
    }
}
