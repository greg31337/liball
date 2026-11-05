using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class CssMotionCompilerTests
{
    [Test]
    public void CreatesCanonicalTransitionsWithTimingDurationAndDelay()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        CssTransitionValue transition = new CssTransitionValue
        {
            Properties = new string[] { CssProperties.Transform, CssProperties.Opacity, CssProperties.Transform },
            DurationMilliseconds = 180,
            DelayMilliseconds = -20,
            TimingFunction = " EASE-IN-OUT "
        };

        UiCssDeclaration declaration = compiler.CreateTransitionDeclaration(transition, false);
        string css = new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { declaration });

        Assert.That(css, Is.EqualTo("transition:opacity 180ms ease-in-out -20ms,transform 180ms ease-in-out -20ms;"));
    }

    [Test]
    public void DisablesTransitionsForReducedMotionOrZeroDuration()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        CssTransitionValue transition = new CssTransitionValue
        {
            Properties = new string[] { CssProperties.Opacity },
            DurationMilliseconds = 120
        };
        CssTransitionValue immediate = new CssTransitionValue
        {
            Properties = new string[] { CssProperties.Transform },
            DurationMilliseconds = 0
        };

        string reduced = new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { compiler.CreateTransitionDeclaration(transition, true) });
        string zero = new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { compiler.CreateTransitionDeclaration(immediate, false) });

        Assert.Multiple(() =>
        {
            Assert.That(reduced, Is.EqualTo("transition:none;"));
            Assert.That(zero, Is.EqualTo("transition:none;"));
        });
    }

    [TestCase("linear")]
    [TestCase("ease")]
    [TestCase("ease-in")]
    [TestCase("ease-out")]
    [TestCase("ease-in-out")]
    [TestCase("step-start")]
    [TestCase("step-end")]
    public void SupportsPortableTimingFunctions(string timingFunction)
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        CssTransitionValue transition = new CssTransitionValue
        {
            Properties = new string[] { CssProperties.Opacity },
            DurationMilliseconds = 1,
            TimingFunction = timingFunction
        };

        UiCssDeclaration declaration = compiler.CreateTransitionDeclaration(transition, false);

        Assert.That(declaration.Value, Does.Contain(" " + timingFunction + " "));
    }

    [Test]
    public void SerializesCanonicalKeyframesIndependentOfInputOrder()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        CssKeyframeValue[] frames = new CssKeyframeValue[]
        {
            Frame(100, Declaration(CssProperties.Transform, "translateX(0)"), Declaration(CssProperties.Opacity, "1")),
            Frame(50, Declaration(CssProperties.Opacity, ".5")),
            Frame(0, Declaration(CssProperties.Transform, "translateX(-1rem)"), Declaration(CssProperties.Opacity, "0"))
        };

        string css = compiler.SerializeKeyframes(" slide in ", frames);
        string reordered = compiler.SerializeKeyframes("slide in", new CssKeyframeValue[] { frames[2], frames[0], frames[1] });

        Assert.Multiple(() =>
        {
            Assert.That(css, Is.EqualTo("@keyframes slide\\20 in{0%{opacity:0;transform:translateX(-1rem);}50%{opacity:.5;}100%{opacity:1;transform:translateX(0);}}"));
            Assert.That(reordered, Is.EqualTo(css));
        });
    }

    [TestCase(UiAnimationStage.Enter, "enter")]
    [TestCase(UiAnimationStage.Leave, "leave")]
    [TestCase(UiAnimationStage.Move, "move")]
    public void StartsAndCompletesEveryActiveAnimationStage(UiAnimationStage stage, string stateToken)
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        UiAnimation animation = Animation("item", "fade", stage, 160, true);

        CssAnimationPlan plan = compiler.StartAnimation(animation, FadeFrames(), false);
        string declarations = new CssSerializer().SerializeDeclarations(plan.Declarations);
        AnimationLifecycleMessage completed = compiler.CompleteAnimation(" item ", "fade");

        Assert.Multiple(() =>
        {
            Assert.That(plan.KeyframesCss, Is.EqualTo("@keyframes fade{0%{opacity:0;}100%{opacity:1;}}"));
            Assert.That(declarations, Is.EqualTo("animation:fade 160ms ease both;"));
            Assert.That(plan.StateToken, Is.EqualTo(stateToken));
            Assert.That(plan.RequiresCompletion, Is.True);
            Assert.That(plan.Messages, Has.Length.EqualTo(1));
            Assert.That(plan.Messages[0].Kind, Is.EqualTo(UiBridgeMessageKind.Lifecycle));
            Assert.That(plan.Messages[0].Stage, Is.EqualTo(stage));
            Assert.That(plan.Messages[0].ReducedMotion, Is.False);
            Assert.That(completed.Stage, Is.EqualTo(UiAnimationStage.Completed));
            Assert.That(compiler.GetActiveAnimationCount(), Is.Zero);
        });
    }

    [Test]
    public void CancelsAnActiveAnimationWhenItsKeyIsReplaced()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        compiler.StartAnimation(Animation("panel", "enter-panel", UiAnimationStage.Enter, 100, true), FadeFrames(), false);

        CssAnimationPlan replacement = compiler.StartAnimation(Animation("panel", "move-panel", UiAnimationStage.Move, 200, true), FadeFrames(), false);

        Assert.Multiple(() =>
        {
            Assert.That(replacement.Messages, Has.Length.EqualTo(2));
            Assert.That(replacement.Messages[0].Name, Is.EqualTo("enter-panel"));
            Assert.That(replacement.Messages[0].Stage, Is.EqualTo(UiAnimationStage.Cancelled));
            Assert.That(replacement.Messages[1].Name, Is.EqualTo("move-panel"));
            Assert.That(replacement.Messages[1].Stage, Is.EqualTo(UiAnimationStage.Move));
            Assert.That(compiler.GetActiveAnimationCount(), Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => compiler.CompleteAnimation("panel", "enter-panel"));
            Assert.That(compiler.GetActiveAnimationCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ExplicitCancellationEmitsLifecycleMessageAndClearsState()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        compiler.StartAnimation(Animation("toast", "leave-toast", UiAnimationStage.Leave, 100, true), FadeFrames(), false);

        AnimationLifecycleMessage message = compiler.CancelAnimation("toast");

        Assert.Multiple(() =>
        {
            Assert.That(message.Kind, Is.EqualTo(UiBridgeMessageKind.Lifecycle));
            Assert.That(message.Key, Is.EqualTo("toast"));
            Assert.That(message.Name, Is.EqualTo("leave-toast"));
            Assert.That(message.Stage, Is.EqualTo(UiAnimationStage.Cancelled));
            Assert.That(compiler.GetActiveAnimationCount(), Is.Zero);
            Assert.Throws<InvalidOperationException>(() => compiler.CancelAnimation("toast"));
        });
    }

    [Test]
    public void ReducedMotionCompletesImmediatelyWithoutKeyframesOrActiveState()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        UiAnimation animation = Animation("dialog", "enter-dialog", UiAnimationStage.Enter, 200, true);

        CssAnimationPlan plan = compiler.StartAnimation(animation, FadeFrames(), true);

        Assert.Multiple(() =>
        {
            Assert.That(plan.KeyframesCss, Is.Empty);
            Assert.That(new CssSerializer().SerializeDeclarations(plan.Declarations), Is.EqualTo("animation:none;"));
            Assert.That(plan.RequiresCompletion, Is.False);
            Assert.That(plan.Messages, Has.Length.EqualTo(2));
            Assert.That(plan.Messages[0].Stage, Is.EqualTo(UiAnimationStage.Enter));
            Assert.That(plan.Messages[0].ReducedMotion, Is.True);
            Assert.That(plan.Messages[1].Stage, Is.EqualTo(UiAnimationStage.Completed));
            Assert.That(plan.Messages[1].ReducedMotion, Is.True);
            Assert.That(compiler.GetActiveAnimationCount(), Is.Zero);
        });
    }

    [Test]
    public void ReducedMotionPreferenceDoesNotDisableOptedInMotion()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        UiAnimation animation = Animation("essential", "move-essential", UiAnimationStage.Move, 200, false);

        CssAnimationPlan plan = compiler.StartAnimation(animation, FadeFrames(), true);

        Assert.Multiple(() =>
        {
            Assert.That(plan.KeyframesCss, Is.Not.Empty);
            Assert.That(plan.RequiresCompletion, Is.True);
            Assert.That(plan.Messages, Has.Length.EqualTo(1));
            Assert.That(plan.Messages[0].ReducedMotion, Is.False);
            Assert.That(compiler.GetActiveAnimationCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ZeroDurationAnimationCompletesImmediatelyWithoutWaitingForEvents()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();

        CssAnimationPlan plan = compiler.StartAnimation(Animation("instant", "instant", UiAnimationStage.Enter, 0, true), FadeFrames(), false);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Messages[1].Stage, Is.EqualTo(UiAnimationStage.Completed));
            Assert.That(plan.RequiresCompletion, Is.False);
            Assert.That(compiler.GetActiveAnimationCount(), Is.Zero);
        });
    }

    [Test]
    public void RejectsInvalidTransitionsKeyframesAndAnimationState()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        CssTransitionValue emptyProperties = new CssTransitionValue();
        CssTransitionValue negativeDuration = new CssTransitionValue { Properties = new string[] { CssProperties.Opacity }, DurationMilliseconds = -1 };
        CssTransitionValue unsafeTiming = new CssTransitionValue { Properties = new string[] { CssProperties.Opacity }, DurationMilliseconds = 1, TimingFunction = "ease;opacity:0" };
        CssTransitionValue recursiveProperty = new CssTransitionValue { Properties = new string[] { CssProperties.Animation }, DurationMilliseconds = 1 };
        CssKeyframeValue[] duplicateFrames = new CssKeyframeValue[] { Frame(0, Declaration(CssProperties.Opacity, "0")), Frame(0, Declaration(CssProperties.Opacity, "1")), Frame(100, Declaration(CssProperties.Opacity, "1")) };
        CssKeyframeValue[] missingEndpoint = new CssKeyframeValue[] { Frame(10, Declaration(CssProperties.Opacity, "0")), Frame(100, Declaration(CssProperties.Opacity, "1")) };
        CssKeyframeValue[] invalidOffset = new CssKeyframeValue[] { Frame(0, Declaration(CssProperties.Opacity, "0")), Frame(101, Declaration(CssProperties.Opacity, "1")) };
        CssKeyframeValue[] unsafeDeclaration = new CssKeyframeValue[] { Frame(0, Declaration(CssProperties.Opacity, "0")), Frame(100, Declaration(CssProperties.Background, "url(https://example.test)")) };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => compiler.CreateTransitionDeclaration(null, false));
            Assert.Throws<ArgumentException>(() => compiler.CreateTransitionDeclaration(emptyProperties, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => compiler.CreateTransitionDeclaration(negativeDuration, false));
            Assert.Throws<ArgumentException>(() => compiler.CreateTransitionDeclaration(unsafeTiming, false));
            Assert.Throws<ArgumentException>(() => compiler.CreateTransitionDeclaration(recursiveProperty, false));
            Assert.Throws<ArgumentNullException>(() => compiler.SerializeKeyframes("fade", null));
            Assert.Throws<ArgumentException>(() => compiler.SerializeKeyframes("none", FadeFrames()));
            Assert.Throws<ArgumentException>(() => compiler.SerializeKeyframes("fade", new CssKeyframeValue[] { FadeFrames()[0] }));
            Assert.Throws<ArgumentException>(() => compiler.SerializeKeyframes("fade", new CssKeyframeValue[] { null!, FadeFrames()[1] }));
            Assert.Throws<ArgumentException>(() => compiler.SerializeKeyframes("fade", duplicateFrames));
            Assert.Throws<ArgumentException>(() => compiler.SerializeKeyframes("fade", missingEndpoint));
            Assert.Throws<ArgumentOutOfRangeException>(() => compiler.SerializeKeyframes("fade", invalidOffset));
            Assert.Throws<ArgumentException>(() => compiler.SerializeKeyframes("fade", unsafeDeclaration));
            Assert.Throws<ArgumentNullException>(() => compiler.StartAnimation(null, FadeFrames(), false));
            Assert.Throws<ArgumentException>(() => compiler.StartAnimation(Animation(" ", "fade", UiAnimationStage.Enter, 1, true), FadeFrames(), false));
            Assert.Throws<ArgumentException>(() => compiler.StartAnimation(Animation("item", " ", UiAnimationStage.Enter, 1, true), FadeFrames(), false));
            Assert.Throws<ArgumentException>(() => compiler.StartAnimation(Animation("item", "fade", UiAnimationStage.Idle, 1, true), FadeFrames(), false));
            Assert.Throws<ArgumentOutOfRangeException>(() => compiler.StartAnimation(Animation("item", "fade", UiAnimationStage.Enter, -1, true), FadeFrames(), false));
            Assert.Throws<ArgumentException>(() => compiler.CompleteAnimation(" ", "fade"));
            Assert.Throws<InvalidOperationException>(() => compiler.CompleteAnimation("missing", "fade"));
        });
    }

    private static UiAnimation Animation(string key, string name, UiAnimationStage stage, int duration, bool disableForReducedMotion)
    {
        return new UiAnimation
        {
            Key = key,
            Name = name,
            Stage = stage,
            DurationMilliseconds = duration,
            DisableForReducedMotion = disableForReducedMotion
        };
    }

    private static CssKeyframeValue[] FadeFrames()
    {
        return new CssKeyframeValue[]
        {
            Frame(0, Declaration(CssProperties.Opacity, "0")),
            Frame(100, Declaration(CssProperties.Opacity, "1"))
        };
    }

    private static CssKeyframeValue Frame(int offset, params UiCssDeclaration[] declarations)
    {
        return new CssKeyframeValue { OffsetPercent = offset, Declarations = declarations };
    }

    private static UiCssDeclaration Declaration(string property, string value)
    {
        return new UiCssDeclaration { Property = property, Value = value };
    }
}
