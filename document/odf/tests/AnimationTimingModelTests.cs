using System.Text;

namespace odf.Tests;

public class AnimationTimingModelTests
{
    [Test]
    public void ResolvesTargetsReferencesAndParsesSmilTimingSemantics()
    {
        OdfDocument document = Read(SemanticAnimationXml());
        OdfAnimation master = document.Animations[0];
        OdfAnimation iterate = document.Animations[1];
        OdfAnimationTiming syncbase = iterate.BeginTimes[0];
        OdfAnimationTiming shapeEvent = iterate.BeginTimes[1];

        Assert.Multiple(() =>
        {
            Assert.That(iterate.TargetObject,
                Is.SameAs(document.Slides[0].DrawingObjects[0]));
            Assert.That(iterate.IsTargetValid, Is.True);
            Assert.That(iterate.IsTargetAmbiguous, Is.False);
            Assert.That(iterate.MasterAnimation, Is.SameAs(master));
            Assert.That(iterate.IsMasterElementValid, Is.True);
            Assert.That(iterate.BeginTimes.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfAnimationTimingKind.Syncbase,
                    OdfAnimationTimingKind.Event,
                    OdfAnimationTimingKind.AccessKey,
                    OdfAnimationTimingKind.WallClock,
                    OdfAnimationTimingKind.Indefinite
                }));
            Assert.That(syncbase.ReferenceId, Is.EqualTo("master"));
            Assert.That(syncbase.EventName, Is.EqualTo("end"));
            Assert.That(syncbase.OffsetSeconds, Is.EqualTo(1.5));
            Assert.That(syncbase.ReferencedAnimation, Is.SameAs(master));
            Assert.That(shapeEvent.ReferenceId, Is.EqualTo("shape1"));
            Assert.That(shapeEvent.EventName, Is.EqualTo("click"));
            Assert.That(shapeEvent.OffsetSeconds, Is.EqualTo(-0.25));
            Assert.That(shapeEvent.ReferencedDrawingObject,
                Is.SameAs(iterate.TargetObject));
            Assert.That(iterate.BeginTimes[2].EventName, Is.EqualTo("a"));
            Assert.That(iterate.BeginTimes[2].OffsetSeconds,
                Is.EqualTo(2));
            Assert.That(iterate.BeginTimes[3].EventName,
                Is.EqualTo("2026-09-02T10:30:00Z"));
            Assert.That(iterate.IsBeginValid, Is.True);
            Assert.That(iterate.EndTimes.Single().Kind,
                Is.EqualTo(OdfAnimationTimingKind.Repeat));
            Assert.That(iterate.EndTimes[0].RepeatIndex, Is.EqualTo(2));
            Assert.That(iterate.EndTimes[0].ReferencedAnimation,
                Is.SameAs(master));
            Assert.That(iterate.IsEndValid, Is.True);
            Assert.That(iterate.DurationSeconds, Is.EqualTo(3723.5));
            Assert.That(iterate.IsDurationValid, Is.True);
            Assert.That(iterate.MinimumSeconds, Is.EqualTo(0.5));
            Assert.That(iterate.IsMinimumValid, Is.True);
            Assert.That(iterate.IsMaximumMedia, Is.True);
            Assert.That(iterate.IsMaximumValid, Is.True);
            Assert.That(iterate.RepeatCountValue, Is.EqualTo(2.5m));
            Assert.That(iterate.IsRepeatCountValid, Is.True);
            Assert.That(iterate.IsRepeatDurationIndefinite, Is.True);
            Assert.That(iterate.IsRepeatDurationValid, Is.True);
            Assert.That(iterate.EffectiveFillDefault,
                Is.EqualTo("freeze"));
            Assert.That(iterate.EffectiveFill, Is.EqualTo("freeze"));
            Assert.That(iterate.EffectiveRestartDefault,
                Is.EqualTo("never"));
            Assert.That(iterate.EffectiveRestart, Is.EqualTo("never"));
            Assert.That(iterate.SpeedValue, Is.EqualTo(-2));
            Assert.That(iterate.IsSpeedValid, Is.True);
            Assert.That(iterate.AccelerateValue, Is.EqualTo(0.25m));
            Assert.That(iterate.DecelerateValue, Is.EqualTo(0.5m));
            Assert.That(iterate.IsAccelerationValid, Is.True);
            Assert.That(iterate.AutoReverseEnabled, Is.True);
            Assert.That(iterate.IsAutoReverseValid, Is.True);
            Assert.That(iterate.IterateIntervalSeconds,
                Is.EqualTo(1.25));
            Assert.That(iterate.IsIterateIntervalValid, Is.True);
            Assert.That(iterate.IsVersionValid, Is.True);
        });
    }

    [Test]
    public void ReportsBrokenAmbiguousAndInvalidAnimationSemantics()
    {
        OdfDocument document = Read(InvalidAnimationXml());
        OdfAnimation invalid = document.Animations[0];
        OdfAnimation ambiguous = document.Animations[1];

        Assert.Multiple(() =>
        {
            Assert.That(invalid.IsTargetValid, Is.False);
            Assert.That(invalid.IsTargetAmbiguous, Is.False);
            Assert.That(invalid.IsMasterElementValid, Is.False);
            Assert.That(invalid.IsBeginValid, Is.False);
            Assert.That(invalid.IsEndValid, Is.False);
            Assert.That(invalid.IsDurationValid, Is.False);
            Assert.That(invalid.IsMinimumValid, Is.False);
            Assert.That(invalid.IsMaximumValid, Is.False);
            Assert.That(invalid.IsRepeatCountValid, Is.False);
            Assert.That(invalid.IsRepeatDurationValid, Is.False);
            Assert.That(invalid.IsFillValid, Is.False);
            Assert.That(invalid.IsFillDefaultValid, Is.False);
            Assert.That(invalid.IsRestartValid, Is.False);
            Assert.That(invalid.IsRestartDefaultValid, Is.False);
            Assert.That(invalid.IsSpeedValid, Is.False);
            Assert.That(invalid.IsAccelerateValid, Is.True);
            Assert.That(invalid.IsDecelerateValid, Is.True);
            Assert.That(invalid.IsAccelerationValid, Is.False);
            Assert.That(invalid.IsAutoReverseValid, Is.False);
            Assert.That(invalid.IsIterateIntervalValid, Is.False);
            Assert.That(ambiguous.IsTargetValid, Is.False);
            Assert.That(ambiguous.IsTargetAmbiguous, Is.True);
            Assert.That(ambiguous.TargetObject, Is.Null);
        });
    }

    [Test]
    public void AppliesVersionSpecificIdentifierAndRepeatRules()
    {
        OdfDocument version11 = Read(VersionAnimationXml(
            "1.1",
            " anim:id='legacy' smil:repeatCount='2'"));
        OdfDocument fractional11 = Read(VersionAnimationXml(
            "1.1",
            " anim:id='legacy' smil:repeatCount='2.5'"));
        OdfDocument xmlId11 = Read(VersionAnimationXml(
            "1.1",
            " xml:id='modern'"));
        OdfDocument version13 = Read(VersionAnimationXml(
            "1.3",
            " xml:id='modern' anim:id='alias'"
                + " smil:repeatCount='2.5'"));
        OdfDocument missingXml13 = Read(VersionAnimationXml(
            "1.3",
            " anim:id='alias'"));

        Assert.Multiple(() =>
        {
            Assert.That(version11.Animations.Single().IsVersionValid,
                Is.True);
            Assert.That(fractional11.Animations.Single().IsVersionValid,
                Is.False);
            Assert.That(xmlId11.Animations.Single().IsVersionValid,
                Is.False);
            Assert.That(version13.Animations.Single().IsVersionValid,
                Is.True);
            Assert.That(missingXml13.Animations.Single().IsVersionValid,
                Is.False);
        });
    }

    [Test]
    public void RecomputesTimingAndResolutionAfterLexicalEdits()
    {
        OdfDocument document = Read(SemanticAnimationXml());
        OdfAnimation iterate = document.Animations[1];
        iterate.TargetElement = "shape2";
        iterate.Begin = "3min";
        iterate.Duration = "1500ms";
        iterate.RepeatCount = "4";
        iterate.Fill = "remove";
        iterate.Restart = "always";
        iterate.Accelerate = "0.4";
        iterate.Decelerate = "0.6";

        OdfDocument reread = ReadBytes(Write(document));
        OdfAnimation changed = reread.Animations[1];

        Assert.Multiple(() =>
        {
            Assert.That(changed.TargetObject,
                Is.SameAs(reread.Slides[0].DrawingObjects[1]));
            Assert.That(changed.BeginTimes.Single().OffsetSeconds,
                Is.EqualTo(180));
            Assert.That(changed.DurationSeconds, Is.EqualTo(1.5));
            Assert.That(changed.RepeatCountValue, Is.EqualTo(4));
            Assert.That(changed.EffectiveFill, Is.EqualTo("remove"));
            Assert.That(changed.EffectiveRestart, Is.EqualTo("always"));
            Assert.That(changed.IsAccelerationValid, Is.True);
        });
    }

    static OdfDocument Read(string xml)
    {
        return ReadBytes(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "animation-timing.fodp",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string SemanticAnimationXml()
    {
        return StartDocument("1.3")
            + "<draw:rect draw:id='shape1' draw:name='First'/>"
            + "<draw:rect draw:id='shape2' draw:name='Second'/>"
            + "<anim:par xml:id='master' anim:id='masterAlias'"
            + " smil:fillDefault='freeze' smil:restartDefault='never'>"
            + "<anim:iterate xml:id='iterate' anim:id='iterateAlias'"
            + " presentation:master-element='master'"
            + " smil:targetElement='shape1'"
            + " smil:begin='master.end+1.5s; shape1.click-250ms;"
            + " accessKey(a)+2s;"
            + " wallclock(2026-09-02T10:30:00Z); indefinite'"
            + " smil:end='master.repeat(2)+1s'"
            + " smil:dur='01:02:03.5' smil:min='500ms'"
            + " smil:max='media' smil:repeatCount='2.5'"
            + " smil:repeatDur='indefinite' smil:fill='default'"
            + " smil:restart='default' smil:speed='-2'"
            + " smil:accelerate='0.25' smil:decelerate='0.5'"
            + " smil:autoReverse='1' anim:iterate-interval='PT1.25S'>"
            + "<anim:set smil:targetElement='shape1'"
            + " smil:attributeName='presentation:visibility'"
            + " smil:to='visible'/>"
            + "</anim:iterate></anim:par>"
            + EndDocument();
    }

    static string InvalidAnimationXml()
    {
        return StartDocument("1.3")
            + "<draw:rect draw:id='duplicate'/><draw:ellipse"
            + " draw:id='duplicate'/>"
            + "<anim:iterate xml:id='bad' smil:targetElement='missing'"
            + " presentation:master-element='missingMaster'"
            + " smil:begin='missing.begin; bad(' smil:end='master.repeat(x)'"
            + " smil:dur='-2s' smil:min='indefinite' smil:max='broken'"
            + " smil:repeatCount='-1' smil:repeatDur='media'"
            + " smil:fill='invalid' smil:fillDefault='default'"
            + " smil:restart='sometimes' smil:restartDefault='default'"
            + " smil:speed='0' smil:accelerate='0.8'"
            + " smil:decelerate='0.4' smil:autoReverse='yes'"
            + " anim:iterate-interval='P1Y'/>"
            + "<anim:set xml:id='ambiguous' smil:targetElement='duplicate'"
            + " smil:attributeName='svg:x' smil:to='1'/>"
            + EndDocument();
    }

    static string VersionAnimationXml(string version, string attributes)
    {
        return StartDocument(version)
            + "<anim:par" + attributes + "/>"
            + EndDocument();
    }

    static string StartDocument(string version)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + Namespaces()
            + " office:version='" + version + "' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:body><office:presentation>"
            + "<draw:page draw:name='Animated'>";
    }

    static string EndDocument()
    {
        return "</draw:page></office:presentation></office:body>"
            + "</office:document>";
    }

    static string Namespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:anim='" + OdfNamespaces.Animation + "'"
            + " xmlns:smil='" + OdfNamespaces.Smil + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'";
    }
}
