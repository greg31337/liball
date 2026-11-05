namespace svg.Tests;

public class SvgMotionAnimationTests
{
    [Test]
    public void InlinePathAutoRotationMapsTheRequestedOriginOntoThePath()
    {
        SvgMotionAnimationPlan plan = Plan("<animateMotion path='M0 0L100 0' values='0,0;1,1' rotate='auto'/>");

        SvgMotionAnimationResult sample = new SvgMotionAnimation().SampleWithPlanAndProgressAndRepeatIterationAndOriginAndBaseTransformAndOptions(plan, 0.5, 0, new SvgPoint(10, 5), null, new SvgMotionAnimationOptions());
        SvgTransformMatrixResult matrix = sample.MotionTransform.ToMatrix();
        double mappedX = matrix.Matrix.A * 10 + matrix.Matrix.C * 5 + matrix.Matrix.E;
        double mappedY = matrix.Matrix.B * 10 + matrix.Matrix.D * 5 + matrix.Matrix.F;

        Assert.Multiple(() =>
        {
            Assert.That(plan.SourceKind, Is.EqualTo(SvgMotionPathSourceKind.Path));
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(sample.Success, Is.True);
            Assert.That(sample.Point.X, Is.EqualTo(50).Within(0.00001));
            Assert.That(sample.Point.Y, Is.EqualTo(0).Within(0.00001));
            Assert.That(sample.RotationDegrees, Is.EqualTo(0).Within(0.00001));
            Assert.That(mappedX, Is.EqualTo(50).Within(0.00001));
            Assert.That(mappedY, Is.EqualTo(0).Within(0.00001));
        });
    }

    [Test]
    public void AutoReverseAndFixedAnglesProduceExpectedRotation()
    {
        SvgMotionAnimationPlan reverse = Plan("<animateMotion path='M0 0L0 20' rotate='auto-reverse'/>");
        SvgMotionAnimationPlan fixedAngle = Plan("<animateMotion path='M0 0L20 0' rotate='.5turn'/>");

        SvgMotionAnimationResult reverseSample = new SvgMotionAnimation().SampleWithPlanAndProgress(reverse, 0.25);
        SvgMotionAnimationResult fixedSample = new SvgMotionAnimation().SampleWithPlanAndProgress(fixedAngle, 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(reverse.RotateKind, Is.EqualTo(SvgMotionRotateKind.AutoReverse));
            Assert.That(reverseSample.RotationDegrees, Is.EqualTo(270).Within(0.00001));
            Assert.That(fixedAngle.RotateKind, Is.EqualTo(SvgMotionRotateKind.Angle));
            Assert.That(fixedAngle.RotateAngle, Is.EqualTo(180).Within(0.00001));
            Assert.That(fixedSample.RotationDegrees, Is.EqualTo(180).Within(0.00001));
        });
    }

    [Test]
    public void MPathTakesPrecedenceAndUsesReferencedPathLengthCalibration()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><path id='route' d='M0 0L100 0' pathLength='10'/><rect><animateMotion path='M0 0L1 0'><mpath href='#route'/></animateMotion></rect></svg>";
        SvgDocument document = Load(source);
        SvgNode animationNode = document.DocumentElement.Children[1].Children[0];
        SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSource(animationNode);

        SvgMotionAnimationPlan plan = new SvgMotionAnimation().ResolveWithAnimation(animation);
        SvgMotionAnimationResult sample = new SvgMotionAnimation().SampleWithPlanAndProgress(plan, 0.25);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(plan.SourceKind, Is.EqualTo(SvgMotionPathSourceKind.MPath));
            Assert.That(plan.PathNode, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(plan.PathLength.Value, Is.EqualTo(10));
            Assert.That(plan.Metrics.ActualLength, Is.EqualTo(100).Within(0.00001));
            Assert.That(plan.Metrics.CalibratedLength, Is.EqualTo(10).Within(0.00001));
            Assert.That(sample.CalibratedDistance, Is.EqualTo(2.5).Within(0.00001));
            Assert.That(sample.ActualDistance, Is.EqualTo(25).Within(0.00001));
            Assert.That(sample.Point.X, Is.EqualTo(25).Within(0.00001));
        });
    }

    [Test]
    public void KeyPointsMapSimpleTimeToPathDistance()
    {
        SvgMotionAnimationPlan linear = Plan("<animateMotion path='M0 0L100 0' calcMode='linear' keyTimes='0;.5;1' keyPoints='0;.2;1'/>");
        SvgMotionAnimationPlan discrete = Plan("<animateMotion path='M0 0L100 0' calcMode='discrete' keyTimes='0;.5;.9' keyPoints='0;.4;1'/>");

        SvgMotionAnimationResult linearMiddle = new SvgMotionAnimation().SampleWithPlanAndProgress(linear, 0.5);
        SvgMotionAnimationResult linearLater = new SvgMotionAnimation().SampleWithPlanAndProgress(linear, 0.75);
        SvgMotionAnimationResult beforeSwitch = new SvgMotionAnimation().SampleWithPlanAndProgress(discrete, 0.49);
        SvgMotionAnimationResult afterSwitch = new SvgMotionAnimation().SampleWithPlanAndProgress(discrete, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(linearMiddle.PathProgress, Is.EqualTo(0.2).Within(0.00001));
            Assert.That(linearMiddle.Point.X, Is.EqualTo(20).Within(0.00001));
            Assert.That(linearLater.PathProgress, Is.EqualTo(0.6).Within(0.00001));
            Assert.That(linearLater.Point.X, Is.EqualTo(60).Within(0.00001));
            Assert.That(beforeSwitch.Point.X, Is.EqualTo(0).Within(0.00001));
            Assert.That(afterSwitch.Point.X, Is.EqualTo(40).Within(0.00001));
        });
    }

    [Test]
    public void SplineKeyTimingEasesPathDistance()
    {
        SvgMotionAnimationPlan plan = Plan("<animateMotion path='M0 0L100 0' calcMode='spline' keyTimes='0;1' keyPoints='0;1' keySplines='0 0 1 1'/>");

        SvgMotionAnimationResult sample = new SvgMotionAnimation().SampleWithPlanAndProgress(plan, 0.4);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(plan.KeySplines, Has.Count.EqualTo(1));
            Assert.That(sample.PathProgress, Is.EqualTo(0.4).Within(0.0001));
            Assert.That(sample.Point.X, Is.EqualTo(40).Within(0.01));
        });
    }

    [Test]
    public void PointValuesProvideMotionWhenNoPathIsPresent()
    {
        SvgMotionAnimationPlan plan = Plan("<animateMotion values='0,0;20,10' calcMode='linear' rotate='auto'/>");

        SvgMotionAnimationResult sample = new SvgMotionAnimation().SampleWithPlanAndProgress(plan, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Success, Is.True, Message(plan));
            Assert.That(plan.SourceKind, Is.EqualTo(SvgMotionPathSourceKind.Values));
            Assert.That(sample.Point.X, Is.EqualTo(10).Within(0.00001));
            Assert.That(sample.Point.Y, Is.EqualTo(5).Within(0.00001));
            Assert.That(sample.RotationDegrees, Is.EqualTo(System.Math.Atan2(10, 20) * 180 / System.Math.PI).Within(0.00001));
        });
    }

    [Test]
    public void RepeatAccumulationAndBaseTransformAreComposedDeterministically()
    {
        SvgMotionAnimationPlan plan = Plan("<animateMotion path='M0 0L10 0' accumulate='sum'/>");
        SvgTransformList baseTransform = Transform(SvgTransformKind.Scale, 2.0);

        SvgMotionAnimationResult sample = new SvgMotionAnimation().SampleWithPlanAndProgressAndRepeatIterationAndOriginAndBaseTransformAndOptions(plan, 0.5, 2, new SvgPoint(0, 0), baseTransform, new SvgMotionAnimationOptions());
        SvgTransformMatrixResult matrix = sample.ComposedTransform.ToMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(sample.Success, Is.True);
            Assert.That(sample.Point.X, Is.EqualTo(25).Within(0.00001));
            Assert.That(sample.RepeatIteration, Is.EqualTo(2));
            Assert.That(sample.ComposedTransform.Items[0].Kind, Is.EqualTo(SvgTransformKind.Scale));
            Assert.That(sample.ComposedTransform.Items[1].Kind, Is.EqualTo(SvgTransformKind.Translate));
            Assert.That(matrix.Matrix.E, Is.EqualTo(50).Within(0.00001));
        });
    }

    [Test]
    public void TimelineSamplingUsesTimelineProgressAndIteration()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect><animateMotion path='M0 0L10 0' dur='2s' repeatCount='3' accumulate='sum'/></rect></svg>";
        SvgDocument document = Load(source);
        SvgAnimationTimelineEntry entry = new SvgAnimationTimeline().EvaluateWithDocumentAndTime(document, 3).Animations.Single();
        SvgMotionAnimationPlan plan = new SvgMotionAnimation().ResolveWithAnimation(entry.Animation);

        SvgMotionAnimationResult sample = new SvgMotionAnimation().SampleWithPlanAndTimelineAndOriginAndBaseTransformAndOptions(plan, entry, new SvgPoint(0, 0), null, new SvgMotionAnimationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(entry.SimpleProgress, Is.EqualTo(0.5));
            Assert.That(entry.RepeatIteration, Is.EqualTo(1));
            Assert.That(sample.Point.X, Is.EqualTo(15).Within(0.00001));
        });
    }

    [Test]
    public void InvalidMotionSourcesRotationAndKeysFailWithStableErrors()
    {
        SvgMotionAnimationPlan missing = PlanWithoutSuccess("<animateMotion/>");
        SvgMotionAnimationPlan rotate = PlanWithoutSuccess("<animateMotion path='M0 0L1 0' rotate='sideways'/>");
        SvgMotionAnimationPlan keys = PlanWithoutSuccess("<animateMotion path='M0 0L1 0' calcMode='linear' keyTimes='0;1' keyPoints='0;.5;1'/>");
        SvgMotionAnimationPlan keyPointsWithoutTimes = PlanWithoutSuccess("<animateMotion path='M0 0L1 0' keyPoints='0;1'/>");
        string wrongTargetSource = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='route'/><g><animateMotion><mpath href='#route'/></animateMotion></g></svg>";
        SvgDocument wrongTargetDocument = Load(wrongTargetSource);
        SvgAnimation wrongTargetAnimation = new SvgAnimationTargetResolver().ResolveWithSource(wrongTargetDocument.DocumentElement.Children[1].Children[0]);
        SvgMotionAnimationPlan wrongTarget = new SvgMotionAnimation().ResolveWithAnimation(wrongTargetAnimation);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Error, Is.EqualTo(SvgMotionAnimationError.MissingPath));
            Assert.That(rotate.Error, Is.EqualTo(SvgMotionAnimationError.InvalidRotate));
            Assert.That(keys.Error, Is.EqualTo(SvgMotionAnimationError.InvalidKeys));
            Assert.That(keyPointsWithoutTimes.Error, Is.EqualTo(SvgMotionAnimationError.InvalidKeys));
            Assert.That(wrongTarget.Error, Is.EqualTo(SvgMotionAnimationError.InvalidMPath));
        });
    }

    static SvgMotionAnimationPlan Plan(string animationElement)
    {
        SvgMotionAnimationPlan plan = PlanWithoutSuccess(animationElement);
        Assert.That(plan.Success, Is.True, Message(plan));
        return plan;
    }

    static SvgMotionAnimationPlan PlanWithoutSuccess(string animationElement)
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect>" + animationElement + "</rect></svg>");
        SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSource(document.DocumentElement.Children[0].Children[0]);
        Assert.That(animation.Success, Is.True, string.Join(" | ", animation.Diagnostics.Select(item => item.Message)));
        return new SvgMotionAnimation().ResolveWithAnimation(animation);
    }

    static SvgTransformList Transform(SvgTransformKind kind, double value)
    {
        List<SvgTransform> transforms = new List<SvgTransform>();
        transforms.Add(new SvgTransform(kind, new[] { value }));
        return new SvgTransformList(transforms);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Message(SvgMotionAnimationPlan plan)
    {
        return plan.Error + ": " + string.Join(" | ", plan.Diagnostics.Select(item => item.Message));
    }
}
