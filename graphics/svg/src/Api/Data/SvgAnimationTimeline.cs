namespace svg;

/// <summary>Represents one caller-injected event at a finite timeline time.</summary>
public class SvgAnimationEvent
{
    /// <summary>The injected event kind.</summary>
    public SvgAnimationEventKind Kind;
    /// <summary>The event target node, or <c>null</c> when identified by ID.</summary>
    public SvgNode? Target;
    /// <summary>The event target ID, or an empty string.</summary>
    public string BaseId;
    /// <summary>The event name, access key, or wallclock payload.</summary>
    public string Symbol;
    /// <summary>The finite caller timeline time in seconds.</summary>
    public double Time;

    /// <summary>Creates one validated injected animation event.</summary>
    /// <param name="kind">The event kind.</param>
    /// <param name="target">The event target, or <c>null</c>.</param>
    /// <param name="baseId">The event target ID, or an empty string.</param>
    /// <param name="symbol">The nonempty event symbol.</param>
    /// <param name="time">The finite timeline time.</param>
    public SvgAnimationEvent(SvgAnimationEventKind kind, SvgNode? target, string baseId, string symbol, double time)
    {
        if (kind < SvgAnimationEventKind.Event || kind > SvgAnimationEventKind.Wallclock) throw new System.ArgumentOutOfRangeException("kind");
        if (baseId == null) throw new System.ArgumentNullException("baseId");
        if (symbol == null || symbol.Length == 0) throw new System.ArgumentOutOfRangeException("symbol");
        if (!SvgFiniteMath.IsFinite(time)) throw new System.ArgumentOutOfRangeException("time");
        Target = target;
        Kind = kind;
        BaseId = baseId;
        Symbol = symbol;
        Time = SvgFiniteMath.NormalizeZero(time);
    }
}

/// <summary>Stores caller-injected animation events without running an event loop.</summary>
public class SvgAnimationEventQueue
{
    /// <summary>The mutable injected events in injection order.</summary>
    public System.Collections.Generic.List<SvgAnimationEvent> Events;
    /// <summary>The monotonic queue version.</summary>
    public long Version;

    /// <summary>Creates an empty event queue.</summary>
    public SvgAnimationEventQueue()
    {
        Events = new System.Collections.Generic.List<SvgAnimationEvent>();
        Version = 0;
    }

    /// <summary>Injects an event identified by target ID.</summary>
    /// <param name="baseId">The exact target element ID, or an empty string.</param>
    /// <param name="eventName">The nonempty event name.</param>
    /// <param name="time">The finite caller timeline time.</param>
    internal void InjectEvent(string baseId, string eventName, double time)
    {
        Add(new SvgAnimationEvent(SvgAnimationEventKind.Event, null, baseId, eventName, time));
    }

    /// <summary>Injects an event identified by target node.</summary>
    /// <param name="target">The event target node.</param>
    /// <param name="eventName">The nonempty event name.</param>
    /// <param name="time">The finite caller timeline time.</param>
    internal void InjectNodeEvent(SvgNode target, string eventName, double time)
    {
        if (target == null) throw new System.ArgumentNullException("target");
        Add(new SvgAnimationEvent(SvgAnimationEventKind.Event, target, "", eventName, time));
    }

    /// <summary>Injects an access-key activation.</summary>
    /// <param name="accessKey">The decoded nonempty access-key scalar.</param>
    /// <param name="time">The finite caller timeline time.</param>
    internal void InjectAccessKey(string accessKey, double time)
    {
        Add(new SvgAnimationEvent(SvgAnimationEventKind.AccessKey, null, "", accessKey, time));
    }

    /// <summary>Injects a caller-resolved wallclock payload.</summary>
    /// <param name="wallclock">The exact wallclock payload from a timing specifier.</param>
    /// <param name="time">The corresponding finite caller timeline time.</param>
    internal void InjectWallclock(string wallclock, double time)
    {
        Add(new SvgAnimationEvent(SvgAnimationEventKind.Wallclock, null, "", wallclock, time));
    }

    /// <summary>Removes all injected events.</summary>
    internal void Clear()
    {
        Events.Clear();
        IncrementVersion();
    }

    /// <summary>Returns an independent injection-order snapshot.</summary>
    /// <returns>The injected events.</returns>
    internal System.Collections.Generic.List<SvgAnimationEvent> GetEvents()
    {
        return new System.Collections.Generic.List<SvgAnimationEvent>(Events);
    }

    internal void Add(SvgAnimationEvent item)
    {
        Events.Add(item);
        IncrementVersion();
    }

    internal void IncrementVersion()
    {
        Version = Version + 1;
    }
}

/// <summary>Configures bounded declarative animation timeline evaluation.</summary>
public class SvgAnimationTimelineOptions
{
    /// <summary>The animation target-resolution options.</summary>
    public SvgAnimationTargetOptions TargetOptions;
    /// <summary>The explicit caller-injected event queue.</summary>
    public SvgAnimationEventQueue Events;
    /// <summary>The maximum animation and discard element count.</summary>
    public int MaxAnimations;
    /// <summary>The maximum parsed timing-specifier count.</summary>
    public int MaxTimingSpecifiers;
    /// <summary>The maximum resolved instance-time and interval count.</summary>
    public int MaxInstances;
    /// <summary>The maximum dependency-resolution passes.</summary>
    public int MaxSyncPasses;
    /// <summary>The maximum injected event count considered per evaluation.</summary>
    public int MaxEvents;
    /// <summary>The maximum timing or numeric token character count.</summary>
    public int MaxTokenCharacters;

    /// <summary>Creates secure deterministic timeline limits.</summary>
    public SvgAnimationTimelineOptions()
    {
        TargetOptions = new SvgAnimationTargetOptions();
        Events = new SvgAnimationEventQueue();
        MaxAnimations = 10000;
        MaxTimingSpecifiers = 100000;
        MaxInstances = 100000;
        MaxSyncPasses = 64;
        MaxEvents = 100000;
        MaxTokenCharacters = 4096;
    }
}

/// <summary>Represents one resolved active animation interval.</summary>
public class SvgAnimationInterval
{
    /// <summary>The interval begin time.</summary>
    public double Begin;
    /// <summary>The interval end time, possibly positive infinity.</summary>
    public double End;
    /// <summary>The finite simple duration or positive infinity.</summary>
    public double SimpleDuration;

    /// <summary>Creates one resolved interval.</summary>
    /// <param name="begin">The finite begin time.</param>
    /// <param name="end">The end time at or after begin.</param>
    /// <param name="simpleDuration">The nonnegative simple duration.</param>
    public SvgAnimationInterval(double begin, double end, double simpleDuration)
    {
        if (!SvgFiniteMath.IsFinite(begin) || double.IsNaN(end) || end < begin || double.IsNaN(simpleDuration) || simpleDuration < 0) throw new System.ArgumentOutOfRangeException("begin");
        Begin = SvgFiniteMath.NormalizeZero(begin);
        End = end;
        SimpleDuration = simpleDuration;
    }

    /// <summary>Tests whether a finite caller time is inside the half-open interval.</summary>
    /// <param name="time">The finite caller time.</param>
    /// <returns><c>true</c> when active.</returns>
    internal bool Contains(double time)
    {
        return SvgFiniteMath.IsFinite(time) && time >= Begin && time < End;
    }
}

/// <summary>Contains the resolved timing and current phase of one animation element.</summary>
public class SvgAnimationTimelineEntry
{
    /// <summary>The target-resolved and timing-populated animation.</summary>
    public SvgAnimation Animation;
    /// <summary>The sorted unique resolved begin instance times.</summary>
    public System.Collections.Generic.List<double> BeginTimes;
    /// <summary>The sorted unique resolved end instance times.</summary>
    public System.Collections.Generic.List<double> EndTimes;
    /// <summary>The resolved active intervals.</summary>
    public System.Collections.Generic.List<SvgAnimationInterval> Intervals;
    /// <summary>Whether the animation is active at the evaluated time.</summary>
    public bool Active;
    /// <summary>Whether a completed animation is frozen at the evaluated time.</summary>
    public bool Frozen;
    /// <summary>Whether a discard has removed its target at the evaluated time.</summary>
    public bool Discarded;
    /// <summary>The selected interval begin, or zero when inactive and unfrozen.</summary>
    public double ActiveBegin;
    /// <summary>The selected interval end, or zero when inactive and unfrozen.</summary>
    public double ActiveEnd;
    /// <summary>The current simple time in seconds.</summary>
    public double SimpleTime;
    /// <summary>The zero-based repeat iteration.</summary>
    public int RepeatIteration;
    /// <summary>The normalized simple progress in the unit interval.</summary>
    public double SimpleProgress;
    /// <summary>Whether this entry parsed and resolved without an error.</summary>
    public bool Success;

    internal SvgAnimationTimelineEntry(SvgAnimation animation)
    {
        Animation = animation;
        BeginTimes = new System.Collections.Generic.List<double>();
        EndTimes = new System.Collections.Generic.List<double>();
        Intervals = new System.Collections.Generic.List<SvgAnimationInterval>();
        Active = false;
        Frozen = false;
        Discarded = false;
        ActiveBegin = 0;
        ActiveEnd = 0;
        SimpleTime = 0;
        RepeatIteration = 0;
        SimpleProgress = 0;
        Success = animation != null && animation.Success;
    }
}

/// <summary>Contains one complete bounded timeline evaluation.</summary>
public class SvgAnimationTimelineResult
{
    /// <summary>The finite evaluated caller time.</summary>
    public double Time;
    /// <summary>The document or subtree version captured by evaluation.</summary>
    public long SourceVersion;
    /// <summary>The animation entries in document order.</summary>
    public System.Collections.Generic.List<SvgAnimationTimelineEntry> Animations;
    /// <summary>The unique discarded target nodes in document order of discard activation.</summary>
    public System.Collections.Generic.List<SvgNode> DiscardedNodes;
    /// <summary>Located evaluation diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The number of timing dependency passes performed.</summary>
    public int SyncPasses;
    /// <summary>The number of resolved instance times.</summary>
    public int InstanceCount;
    /// <summary>Whether evaluation completed without an error.</summary>
    public bool Success;

    internal SvgAnimationTimelineResult(double time, long sourceVersion)
    {
        Time = time;
        SourceVersion = sourceVersion;
        Animations = new System.Collections.Generic.List<SvgAnimationTimelineEntry>();
        DiscardedNodes = new System.Collections.Generic.List<SvgNode>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        SyncPasses = 0;
        InstanceCount = 0;
        Success = false;
    }
}
