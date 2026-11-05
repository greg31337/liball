namespace svg.Tests;

public class SvgAnimationCompositionTests
{
    [Test]
    public void ValuesTakePrecedenceAndPopulateAnimationKeyframeFields()
    {
        SvgAnimation animation = Animation("<animate attributeName='opacity' values='0;1;0' from='9' to='8'/>");

        SvgAnimationValuePlan plan = new SvgAnimationComposer().ResolveWithAnimation(animation);
        SvgAnimationCompositionResult sample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(plan, null, 0.25, 0, new SvgAnimationCompositionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(plan.SourceKind, Is.EqualTo(SvgAnimationValueSourceKind.Values));
            Assert.That(plan.ValueKind, Is.EqualTo(SvgAnimationValueKind.Number));
            Assert.That(plan.Values.Select(item => item.Number), Is.EqualTo(new[] { 0.0, 1.0, 0.0 }));
            Assert.That(plan.KeyTimes, Is.EqualTo(new[] { 0.0, 0.5, 1.0 }));
            Assert.That(sample.Success, Is.True);
            Assert.That(sample.Value.Number, Is.EqualTo(0.5));
            Assert.That(animation.Values, Has.Count.EqualTo(3));
            Assert.That(animation.FromValue, Is.Null);
            Assert.That(animation.CalcMode, Is.EqualTo(SvgAnimationCalcMode.Linear));
        });
    }

    [Test]
    public void FromToFromByToAndByCombinationsSelectAndSampleCorrectly()
    {
        SvgAnimationValue underlying = SvgAnimationValue.FromNumber(100);
        SvgAnimationCompositionOptions options = new SvgAnimationCompositionOptions();
        SvgAnimationValuePlan fromTo = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' from='10' to='30'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimationValuePlan fromBy = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' from='10' by='20'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimationValuePlan to = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' to='200'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimationValuePlan by = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' by='20'/>", "opacity", false), SvgAnimationValueKind.Number, options);

        SvgAnimationCompositionResult fromToSample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(fromTo, underlying, 0.5, 0, options);
        SvgAnimationCompositionResult fromBySample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(fromBy, underlying, 0.5, 0, options);
        SvgAnimationCompositionResult toSample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(to, underlying, 0.5, 0, options);
        SvgAnimationCompositionResult bySample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(by, underlying, 0.5, 0, options);

        Assert.Multiple(() =>
        {
            Assert.That(fromTo.SourceKind, Is.EqualTo(SvgAnimationValueSourceKind.FromTo));
            Assert.That(fromBy.SourceKind, Is.EqualTo(SvgAnimationValueSourceKind.FromBy));
            Assert.That(to.SourceKind, Is.EqualTo(SvgAnimationValueSourceKind.To));
            Assert.That(by.SourceKind, Is.EqualTo(SvgAnimationValueSourceKind.By));
            Assert.That(fromToSample.Value.Number, Is.EqualTo(20));
            Assert.That(fromBySample.Value.Number, Is.EqualTo(20));
            Assert.That(toSample.Value.Number, Is.EqualTo(150));
            Assert.That(bySample.EffectValue.Number, Is.EqualTo(10));
            Assert.That(bySample.Value.Number, Is.EqualTo(110));
        });
    }

    [Test]
    public void CustomKeyTimesAndSplinesSelectTheCorrectSegment()
    {
        string element = "<animate attributeName='opacity' values='0;10;20' calcMode='spline' keyTimes='0;.2;1' keySplines='0 0 1 1;0 0 1 1'/>";
        SvgAnimationValuePlan plan = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation(element, "opacity", false), SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());

        SvgAnimationCompositionResult sample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(plan, null, 0.6, 0, new SvgAnimationCompositionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(plan.KeySplines, Has.Count.EqualTo(2));
            Assert.That(sample.Success, Is.True);
            Assert.That(sample.SegmentIndex, Is.EqualTo(1));
            Assert.That(sample.EffectiveProgress, Is.EqualTo(0.5).Within(0.00001));
            Assert.That(sample.Value.Number, Is.EqualTo(15).Within(0.00001));
        });
    }

    [Test]
    public void DiscreteDefaultsAndPacedDistanceUseTheirModeSpecificTiming()
    {
        SvgAnimationValuePlan discrete = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;10;20' calcMode='discrete'/>", "opacity", false), SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());
        SvgAnimationValuePlan paced = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;10;30' calcMode='paced' keyTimes='0;1'/>", "opacity", false), SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());

        SvgAnimationCompositionResult beforeSwitch = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(discrete, null, 0.32, 0, new SvgAnimationCompositionOptions());
        SvgAnimationCompositionResult afterSwitch = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(discrete, null, 0.34, 0, new SvgAnimationCompositionOptions());
        SvgAnimationCompositionResult pacedHalf = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(paced, null, 0.5, 0, new SvgAnimationCompositionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(discrete.KeyTimes, Is.EqualTo(new[] { 0.0, 1.0 / 3.0, 2.0 / 3.0 }));
            Assert.That(beforeSwitch.Value.Number, Is.EqualTo(0));
            Assert.That(afterSwitch.Value.Number, Is.EqualTo(10));
            Assert.That(paced.Success, Is.True, Message(paced));
            Assert.That(pacedHalf.Value.Number, Is.EqualTo(15));
            Assert.That(pacedHalf.SegmentIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void AdditiveAndRepeatAccumulationComposeWithUnderlyingValues()
    {
        string element = "<animate attributeName='opacity' values='0;10' additive='sum' accumulate='sum'/>";
        SvgAnimationValuePlan plan = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation(element, "opacity", false), SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());

        SvgAnimationCompositionResult first = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(plan, SvgAnimationValue.FromNumber(5), 0.5, 0, new SvgAnimationCompositionOptions());
        SvgAnimationCompositionResult third = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(plan, SvgAnimationValue.FromNumber(5), 0.5, 2, new SvgAnimationCompositionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(first.EffectValue.Number, Is.EqualTo(5));
            Assert.That(first.Value.Number, Is.EqualTo(10));
            Assert.That(third.EffectValue.Number, Is.EqualTo(25));
            Assert.That(third.Value.Number, Is.EqualTo(30));
            Assert.That(third.RepeatIteration, Is.EqualTo(2));
        });
    }

    [Test]
    public void ToOnlyIgnoresAdditiveSumAndSetDoesNotRequireUnderlyingValue()
    {
        SvgAnimationCompositionOptions options = new SvgAnimationCompositionOptions();
        SvgAnimationValuePlan to = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' to='20' additive='sum'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimation setAnimation = Animation("<set attributeName='opacity' values='1;2' to='7'/>", "opacity", false);
        SvgAnimationValuePlan set = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(setAnimation, SvgAnimationValueKind.Number, options);

        SvgAnimationCompositionResult toSample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(to, SvgAnimationValue.FromNumber(10), 0.5, 0, options);
        SvgAnimationCompositionResult setSample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(set, null, 0.25, 0, options);

        Assert.Multiple(() =>
        {
            Assert.That(toSample.Value.Number, Is.EqualTo(15));
            Assert.That(set.Success, Is.True, Message(set));
            Assert.That(set.CalcMode, Is.EqualTo(SvgAnimationCalcMode.Discrete));
            Assert.That(set.KeyTimes, Is.EqualTo(new[] { 0.0 }));
            Assert.That(setSample.Value.Number, Is.EqualTo(7));
        });
    }

    [Test]
    public void AdditiveTransformsComposeAfterTheUnderlyingTransformList()
    {
        SvgAnimation animation = Animation("<animateTransform attributeName='transform' from='translate(0)' to='translate(10)' additive='sum' type='translate'/>", "transform", false);
        SvgAnimationCompositionOptions options = new SvgAnimationCompositionOptions();
        SvgAnimationValuePlan plan = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(animation, SvgAnimationValueKind.Transform, options);
        SvgAnimationValue underlying = new SvgAnimationInterpolator().ParseWithKindAndText(SvgAnimationValueKind.Transform, "scale(2)").Value;

        SvgAnimationCompositionResult sample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(plan, underlying, 0.5, 0, options);

        Assert.Multiple(() =>
        {
            Assert.That(sample.Success, Is.True);
            Assert.That(sample.EffectValue.ToText(), Is.EqualTo("translate(5)"));
            Assert.That(sample.Value.ToText(), Is.EqualTo("scale(2) translate(5)"));
        });
    }

    [Test]
    public void TimelineEntrySamplingUsesCurrentProgressAndRepeatIteration()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='opacity' values='0;10' dur='2s' repeatCount='3' accumulate='sum'/></rect></svg>");
        SvgAnimationTimelineEntry entry = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(document, 3).Animations.Single();
        SvgAnimationValuePlan plan = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(entry.Animation, SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());

        SvgAnimationCompositionResult sample = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndTimelineAndOptions(plan, null, entry, new SvgAnimationCompositionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(entry.RepeatIteration, Is.EqualTo(1));
            Assert.That(entry.SimpleProgress, Is.EqualTo(0.5));
            Assert.That(sample.Success, Is.True);
            Assert.That(sample.Value.Number, Is.EqualTo(15));
        });
    }

    [Test]
    public void InvalidKeyMetadataAndUnsupportedCompositionReturnStablePlans()
    {
        SvgAnimationCompositionOptions options = new SvgAnimationCompositionOptions();
        SvgAnimationValuePlan keyCount = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;1;2' keyTimes='0;1'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimationValuePlan endpoint = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;1' keyTimes='.1;1'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimationValuePlan splines = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;1;2' calcMode='spline' keySplines='0 0 1 1'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimationValuePlan keyPoints = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;1' keyPoints='0;1'/>", "opacity", false), SvgAnimationValueKind.Number, options);
        SvgAnimationValuePlan stringBy = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='visibility' by='hidden'/>", "visibility", false), SvgAnimationValueKind.String, options);
        SvgAnimationValuePlan paintSum = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='fill' values='none;context-fill' additive='sum'/>", "fill", false), SvgAnimationValueKind.Paint, options);

        Assert.Multiple(() =>
        {
            Assert.That(keyCount.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidKeyTimes));
            Assert.That(endpoint.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidKeyTimes));
            Assert.That(splines.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidKeySplines));
            Assert.That(keyPoints.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidKeyPoints));
            Assert.That(stringBy.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidComposition));
            Assert.That(paintSum.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidComposition));
        });
    }

    [Test]
    public void MotionKeyPointsRequireAndMatchAuthoredKeyTimes()
    {
        SvgDocument validDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animateMotion values='0,0;10,20' keyTimes='0;1' keyPoints='0;.8'/></rect></svg>");
        SvgDocument missingTimesDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animateMotion values='0,0;10,20' keyPoints='0;1'/></rect></svg>");
        SvgAnimation validAnimation = new SvgAnimationTargetResolver().ResolveWithSource(validDocument.DocumentElement.Children[0].Children[0]);
        SvgAnimation missingTimesAnimation = new SvgAnimationTargetResolver().ResolveWithSource(missingTimesDocument.DocumentElement.Children[0].Children[0]);

        SvgAnimationValuePlan valid = new SvgAnimationComposer().ResolveWithAnimation(validAnimation);
        SvgAnimationValuePlan missingTimes = new SvgAnimationComposer().ResolveWithAnimation(missingTimesAnimation);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, Message(valid));
            Assert.That(valid.ValueKind, Is.EqualTo(SvgAnimationValueKind.Points));
            Assert.That(valid.KeyPoints, Is.EqualTo(new[] { 0.0, 0.8 }));
            Assert.That(missingTimes.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidKeyPoints));
        });
    }

    [Test]
    public void MissingValuesUnderlyingAndConfiguredLimitsFailWithoutPartialSamples()
    {
        SvgAnimationCompositionOptions oneValue = new SvgAnimationCompositionOptions();
        oneValue.MaxValues = 1;
        SvgAnimationValuePlan missing = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity'/>", "opacity", false), SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());
        SvgAnimationValuePlan limited = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;1'/>", "opacity", false), SvgAnimationValueKind.Number, oneValue);
        SvgAnimationValuePlan to = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' to='1'/>", "opacity", false), SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());
        SvgAnimationCompositionResult noUnderlying = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(to, null, 0.5, 0, new SvgAnimationCompositionOptions());
        SvgAnimationCompositionOptions repeatLimit = new SvgAnimationCompositionOptions();
        repeatLimit.MaxRepeatAccumulations = 1;
        SvgAnimationValuePlan accumulated = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(Animation("<animate attributeName='opacity' values='0;1' accumulate='sum'/>", "opacity", false), SvgAnimationValueKind.Number, repeatLimit);
        SvgAnimationCompositionResult repeat = new SvgAnimationComposer().SampleWithPlanAndUnderlyingAndProgressAndRepeatIterationAndOptions(accumulated, null, 0.5, 2, repeatLimit);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Error, Is.EqualTo(SvgAnimationCompositionError.MissingValues));
            Assert.That(limited.Error, Is.EqualTo(SvgAnimationCompositionError.LimitExceeded));
            Assert.That(noUnderlying.Error, Is.EqualTo(SvgAnimationCompositionError.MissingUnderlyingValue));
            Assert.That(repeat.Error, Is.EqualTo(SvgAnimationCompositionError.LimitExceeded));
        });
    }

    [Test]
    public void NullAnimationReturnsAStableInvalidPlan()
    {
        SvgAnimationValuePlan plan = new SvgAnimationComposer().ResolveWithAnimationAndKindAndOptions(null, SvgAnimationValueKind.Number, new SvgAnimationCompositionOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.False);
            Assert.That(plan.Error, Is.EqualTo(SvgAnimationCompositionError.InvalidArgument));
            Assert.That(plan.Animation.Success, Is.False);
            Assert.That(plan.Animation.Source, Is.Null);
            Assert.That(plan.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(plan.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });
    }

    static SvgAnimation Animation(string animationElement)
    {
        return Animation(animationElement, "opacity", true);
    }

    static SvgAnimation Animation(string animationElement, string property, bool infer)
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect>" + animationElement + "</rect></svg>";
        SvgDocument document = Load(source);
        SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSource(document.DocumentElement.Children[0].Children[0]);
        Assert.That(animation.Success, Is.True, string.Join(" | ", animation.Diagnostics.Select(item => item.Message)));
        Assert.That(animation.LocalName, Is.EqualTo(property));
        return animation;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Message(SvgAnimationValuePlan plan)
    {
        return string.Join(" | ", plan.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }
}
