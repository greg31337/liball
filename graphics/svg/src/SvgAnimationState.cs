namespace svg;

internal class SvgAnimationStateCacheEntry
{
    internal SvgNode? Source;
    internal long SourceVersion;
    internal SvgNode? Target;
    internal long TargetVersion;
    internal SvgAnimationValuePlan? ValuePlan;
    internal SvgMotionAnimationPlan? MotionPlan;

    internal SvgAnimationStateCacheEntry(SvgAnimation animation)
    {
        Source = animation.Source; SourceVersion = (animation.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Version; Target = animation.Target; TargetVersion = (animation.Target ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Version; ValuePlan = null; MotionPlan = null;
    }

    internal bool IsCurrent()
    {
        return Source != null && Source.Version == SourceVersion && Target != null && Target.Version == TargetVersion;
    }
}
