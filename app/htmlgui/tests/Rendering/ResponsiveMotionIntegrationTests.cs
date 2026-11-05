using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class ResponsiveMotionIntegrationTests
{
    [TestCase(UiResponsiveOperator.MinWidth, null, "min-width: 48rem", true)]
    [TestCase(UiResponsiveOperator.MaxWidth, null, "max-width: 48rem", true)]
    [TestCase(UiResponsiveOperator.MinHeight, null, "min-height: 48rem", true)]
    [TestCase(UiResponsiveOperator.MaxHeight, null, "max-height: 48rem", true)]
    [TestCase(UiResponsiveOperator.Orientation, "portrait", "orientation: portrait", false)]
    [TestCase(UiResponsiveOperator.Pointer, "fine", "pointer: fine", false)]
    [TestCase(UiResponsiveOperator.Hover, "hover", "hover: hover", false)]
    [TestCase(UiResponsiveOperator.ColorScheme, "dark", "prefers-color-scheme: dark", false)]
    [TestCase(UiResponsiveOperator.Contrast, "more", "prefers-contrast: more", false)]
    [TestCase(UiResponsiveOperator.Motion, "reduce", "prefers-reduced-motion: reduce", false)]
    [TestCase(UiResponsiveOperator.ForcedColors, "active", "forced-colors: active", false)]
    public void CompilesEveryResponsiveOperatorThroughThePublicPipeline(UiResponsiveOperator responsiveOperator, string? value, string expression, bool usesLength)
    {
        UiResponsiveRule responsive = new UiResponsiveRule
        {
            Operator = responsiveOperator,
            Value = value,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Opacity, ".6") }
        };
        if (usesLength)
        {
            responsive.Length = Length(48, UiLengthUnit.Rem);
        }

        IUiCssCompiler compiler = new UiCssCompiler();

        UiCssCompilation generated = compiler.GenerateResponsiveRules(new UiResponsiveRule[] { responsive }, ".target");
        UiCssCompilation serialized = compiler.SerializeStylesheet(generated.Rules);

        Assert.Multiple(() =>
        {
            Assert.That(generated.Success, Is.True);
            Assert.That(generated.Rules[0].Condition!.Kind, Is.EqualTo("media"));
            Assert.That(generated.Rules[0].Condition!.Expression, Is.EqualTo("(" + expression + ")"));
            Assert.That(serialized.Css, Is.EqualTo("@media (" + expression + "){.target{opacity:.6;}}"));
        });
    }

    [Test]
    public void DistinguishesViewportNamedContainerAndOrientationContainerOutput()
    {
        IUiCssCompiler compiler = new UiCssCompiler();
        UiResponsiveRule viewport = SizeRule(false, null);
        UiResponsiveRule namedContainer = SizeRule(true, "sidebar");
        UiResponsiveRule orientationContainer = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.Orientation,
            Value = "landscape",
            IsContainer = true,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Display, "grid") }
        };

        string viewportCss = compiler.SerializeStylesheet(compiler.GenerateResponsiveRules(new UiResponsiveRule[] { viewport }, ".card").Rules).Css;
        string namedCss = compiler.SerializeStylesheet(compiler.GenerateResponsiveRules(new UiResponsiveRule[] { namedContainer }, ".card").Rules).Css;
        string orientationCss = compiler.SerializeStylesheet(compiler.GenerateResponsiveRules(new UiResponsiveRule[] { orientationContainer }, ".card").Rules).Css;

        Assert.Multiple(() =>
        {
            Assert.That(viewportCss, Is.EqualTo("@media (min-width: 30rem){.card{display:grid;}}"));
            Assert.That(namedCss, Is.EqualTo("@container sidebar (min-width: 30rem){.card{display:grid;}}"));
            Assert.That(orientationCss, Is.EqualTo("@container (orientation: landscape){.card{display:grid;}}"));
        });
    }

    [TestCase(UiResponsiveOperator.Pointer, "fine")]
    [TestCase(UiResponsiveOperator.Hover, "hover")]
    [TestCase(UiResponsiveOperator.ColorScheme, "dark")]
    [TestCase(UiResponsiveOperator.Contrast, "more")]
    [TestCase(UiResponsiveOperator.Motion, "reduce")]
    [TestCase(UiResponsiveOperator.ForcedColors, "active")]
    public void RejectsViewportOnlyFeaturesAsContainerQueries(UiResponsiveOperator responsiveOperator, string value)
    {
        UiResponsiveRule responsive = new UiResponsiveRule
        {
            Operator = responsiveOperator,
            Value = value,
            IsContainer = true,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Display, "block") }
        };

        Assert.Throws<ArgumentException>(() => new UiCssCompiler().GenerateResponsiveRules(new UiResponsiveRule[] { responsive }, ".target"));
    }

    [Test]
    public void EmitsSafeResponsiveRtlAndLtrDirectionOverrides()
    {
        ResponsiveRuleCompiler responsiveCompiler = new ResponsiveRuleCompiler();
        UiCssDeclaration rtl = responsiveCompiler.CreateDirectionOverride(true);
        UiCssDeclaration ltr = responsiveCompiler.CreateDirectionOverride(false);
        UiCssDeclaration untrustedRtl = responsiveCompiler.CreateValueOverride(CssProperties.Direction, "rtl", false);
        UiResponsiveRule responsive = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.MaxWidth,
            Length = Length(40, UiLengthUnit.Rem),
            Declarations = new UiCssDeclaration[] { rtl }
        };
        IUiCssCompiler compiler = new UiCssCompiler();

        string css = compiler.SerializeStylesheet(compiler.GenerateResponsiveRules(new UiResponsiveRule[] { responsive }, ".navigation").Rules).Css;

        Assert.Multiple(() =>
        {
            Assert.That(new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { rtl }), Is.EqualTo("direction:rtl;"));
            Assert.That(new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { ltr }), Is.EqualTo("direction:ltr;"));
            Assert.That(new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { untrustedRtl }), Is.EqualTo("direction:rtl;"));
            Assert.That(CssEscaper.IsAllowedUntrustedProperty(CssProperties.Direction), Is.True);
            Assert.That(css, Is.EqualTo("@media (max-width: 40rem){.navigation{direction:rtl;}}"));
        });
    }

    [TestCase("active")]
    [TestCase("none")]
    public void CompilesBothForcedColorModes(string mode)
    {
        UiResponsiveRule responsive = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.ForcedColors,
            Value = mode,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Border, "1px solid currentcolor") }
        };
        IUiCssCompiler compiler = new UiCssCompiler();

        string css = compiler.SerializeStylesheet(compiler.GenerateResponsiveRules(new UiResponsiveRule[] { responsive }, ".control").Rules).Css;

        Assert.That(css, Is.EqualTo("@media (forced-colors: " + mode + "){.control{border:1px solid currentcolor;}}"));
    }

    [Test]
    public void ReducedMotionRuleAndRuntimeFallbackCompleteWithoutAnimationEvents()
    {
        UiResponsiveRule reducedRule = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.Motion,
            Value = "reduce",
            Declarations = new UiCssDeclaration[]
            {
                new UiCssDeclaration { Property = CssProperties.Animation, Value = "none", IsTrusted = true },
                new UiCssDeclaration { Property = CssProperties.Transition, Value = "none", IsTrusted = true }
            }
        };
        IUiCssCompiler cssCompiler = new UiCssCompiler();
        CssMotionCompiler motionCompiler = new CssMotionCompiler();
        UiAnimation animation = Animation("dialog", "dialog-enter", UiAnimationStage.Enter, true);
        CssTransitionValue transition = new CssTransitionValue
        {
            Properties = new string[] { CssProperties.Opacity },
            DurationMilliseconds = 150
        };

        string css = cssCompiler.SerializeStylesheet(cssCompiler.GenerateResponsiveRules(new UiResponsiveRule[] { reducedRule }, ".dialog").Rules).Css;
        CssAnimationPlan plan = motionCompiler.StartAnimation(animation, FadeFrames(), true);
        UiCssDeclaration reducedTransition = motionCompiler.CreateTransitionDeclaration(transition, true);

        Assert.Multiple(() =>
        {
            Assert.That(css, Is.EqualTo("@media (prefers-reduced-motion: reduce){.dialog{animation:none;transition:none;}}"));
            Assert.That(plan.KeyframesCss, Is.Empty);
            Assert.That(plan.RequiresCompletion, Is.False);
            Assert.That(plan.Messages[0].Stage, Is.EqualTo(UiAnimationStage.Enter));
            Assert.That(plan.Messages[0].ReducedMotion, Is.True);
            Assert.That(plan.Messages[1].Stage, Is.EqualTo(UiAnimationStage.Completed));
            Assert.That(plan.Messages[1].ReducedMotion, Is.True);
            Assert.That(motionCompiler.GetActiveAnimationCount(), Is.Zero);
            Assert.That(new CssSerializer().SerializeDeclarations(new UiCssDeclaration[] { reducedTransition }), Is.EqualTo("transition:none;"));
        });
    }

    [Test]
    public void ReplacementCancellationIgnoresStaleCompletionAndCanCancelCurrentMotion()
    {
        CssMotionCompiler compiler = new CssMotionCompiler();
        compiler.StartAnimation(Animation("panel", "panel-enter", UiAnimationStage.Enter, true), FadeFrames(), false);

        CssAnimationPlan replacement = compiler.StartAnimation(Animation("panel", "panel-leave", UiAnimationStage.Leave, true), FadeFrames(), false);

        Assert.Multiple(() =>
        {
            Assert.That(replacement.Messages, Has.Length.EqualTo(2));
            Assert.That(replacement.Messages[0].Name, Is.EqualTo("panel-enter"));
            Assert.That(replacement.Messages[0].Stage, Is.EqualTo(UiAnimationStage.Cancelled));
            Assert.That(replacement.Messages[1].Name, Is.EqualTo("panel-leave"));
            Assert.That(replacement.Messages[1].Stage, Is.EqualTo(UiAnimationStage.Leave));
            Assert.Throws<InvalidOperationException>(() => compiler.CompleteAnimation("panel", "panel-enter"));
            Assert.That(compiler.GetActiveAnimationCount(), Is.EqualTo(1));
        });

        AnimationLifecycleMessage cancelled = compiler.CancelAnimation("panel");

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Name, Is.EqualTo("panel-leave"));
            Assert.That(cancelled.Stage, Is.EqualTo(UiAnimationStage.Cancelled));
            Assert.That(cancelled.Kind, Is.EqualTo(UiBridgeMessageKind.Lifecycle));
            Assert.That(compiler.GetActiveAnimationCount(), Is.Zero);
        });
    }

    [Test]
    public void RejectsInvalidForcedColorsModes()
    {
        UiResponsiveRule invalid = new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.ForcedColors,
            Value = "enabled;display:none",
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Display, "block") }
        };

        Assert.Throws<ArgumentException>(() => new UiCssCompiler().GenerateResponsiveRules(new UiResponsiveRule[] { invalid }, ".target"));
    }

    private static UiResponsiveRule SizeRule(bool container, string? name)
    {
        return new UiResponsiveRule
        {
            Operator = UiResponsiveOperator.MinWidth,
            Length = Length(30, UiLengthUnit.Rem),
            Value = name,
            IsContainer = container,
            Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Display, "grid") }
        };
    }

    private static UiAnimation Animation(string key, string name, UiAnimationStage stage, bool disableForReducedMotion)
    {
        return new UiAnimation
        {
            Key = key,
            Name = name,
            Stage = stage,
            DurationMilliseconds = 150,
            DisableForReducedMotion = disableForReducedMotion
        };
    }

    private static CssKeyframeValue[] FadeFrames()
    {
        return new CssKeyframeValue[]
        {
            new CssKeyframeValue
            {
                OffsetPercent = 0,
                Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Opacity, "0") }
            },
            new CssKeyframeValue
            {
                OffsetPercent = 100,
                Declarations = new UiCssDeclaration[] { Declaration(CssProperties.Opacity, "1") }
            }
        };
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }

    private static UiCssDeclaration Declaration(string property, string value)
    {
        return new UiCssDeclaration { Property = property, Value = value };
    }
}
