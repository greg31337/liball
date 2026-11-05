using System.Text;

namespace odf.Tests;

public class AnimationDocumentModelTests
{
    [Test]
    public void MaterializesTimingTreesAndEveryAnimationConstruct()
    {
        OdfDocument document = Read(AnimationXml());
        OdfSlide slide = document.Slides.Single();
        OdfAnimation sequence = slide.Animations.Single();
        OdfAnimation parallel = sequence.Children.Single();
        OdfAnimation iterate = parallel.Children[0];
        OdfAnimation animate = iterate.Children[0];
        OdfAnimation command = document.Animations.Single(item =>
            item.Kind == OdfAnimationKind.Command);

        Assert.Multiple(() =>
        {
            Assert.That(document.Animations.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfAnimationKind.Sequence,
                    OdfAnimationKind.Parallel,
                    OdfAnimationKind.Iterate,
                    OdfAnimationKind.Animate,
                    OdfAnimationKind.Set,
                    OdfAnimationKind.AnimateColor,
                    OdfAnimationKind.AnimateMotion,
                    OdfAnimationKind.AnimateTransform,
                    OdfAnimationKind.TransitionFilter,
                    OdfAnimationKind.Audio,
                    OdfAnimationKind.Command
                }));
            Assert.That(sequence.Id, Is.EqualTo("root"));
            Assert.That(sequence.XmlId, Is.EqualTo("sequenceXml"));
            Assert.That(sequence.NodeType, Is.EqualTo("timing-root"));
            Assert.That(sequence.PresetId, Is.EqualTo("preset-1"));
            Assert.That(sequence.PresetSubType, Is.EqualTo("sub-1"));
            Assert.That(sequence.PresetClass, Is.EqualTo("entrance"));
            Assert.That(sequence.MasterElement, Is.EqualTo("master-1"));
            Assert.That(sequence.GroupId, Is.EqualTo("group-1"));
            Assert.That(sequence.Begin, Is.EqualTo("0s"));
            Assert.That(sequence.End, Is.EqualTo("20s"));
            Assert.That(sequence.Duration, Is.EqualTo("15s"));
            Assert.That(sequence.Minimum, Is.EqualTo("1s"));
            Assert.That(sequence.Maximum, Is.EqualTo("18s"));
            Assert.That(sequence.RepeatCount, Is.EqualTo("2"));
            Assert.That(sequence.RepeatDuration, Is.EqualTo("30s"));
            Assert.That(sequence.Restart, Is.EqualTo("whenNotActive"));
            Assert.That(sequence.RestartDefault, Is.EqualTo("always"));
            Assert.That(sequence.Fill, Is.EqualTo("freeze"));
            Assert.That(sequence.FillDefault, Is.EqualTo("remove"));
            Assert.That(sequence.EndSync, Is.EqualTo("last"));
            Assert.That(sequence.Speed, Is.EqualTo("1.5"));
            Assert.That(sequence.Accelerate, Is.EqualTo("0.1"));
            Assert.That(sequence.Decelerate, Is.EqualTo("0.2"));
            Assert.That(sequence.AutoReverse, Is.EqualTo("true"));
            Assert.That(sequence.Slide, Is.SameAs(slide));
            Assert.That(sequence.Parent, Is.Null);
            Assert.That(parallel.Parent, Is.SameAs(sequence));
            Assert.That(iterate.Parent, Is.SameAs(parallel));
            Assert.That(iterate.TargetElement, Is.EqualTo("shape1"));
            Assert.That(iterate.SubItem, Is.EqualTo("text"));
            Assert.That(iterate.IterateType, Is.EqualTo("by-paragraph"));
            Assert.That(iterate.IterateInterval, Is.EqualTo("PT0.5S"));
            Assert.That(animate.AttributeName, Is.EqualTo("svg:x"));
            Assert.That(animate.Values, Is.EqualTo("1;2;3"));
            Assert.That(animate.Formula, Is.EqualTo("$value*2"));
            Assert.That(animate.From, Is.EqualTo("1"));
            Assert.That(animate.To, Is.EqualTo("3"));
            Assert.That(animate.By, Is.EqualTo("2"));
            Assert.That(animate.CalculationMode, Is.EqualTo("spline"));
            Assert.That(animate.KeyTimes, Is.EqualTo("0;0.5;1"));
            Assert.That(animate.KeySplines,
                Is.EqualTo("0.1 0.2 0.3 0.4"));
            Assert.That(animate.Additive, Is.EqualTo("sum"));
            Assert.That(animate.Accumulate, Is.EqualTo("sum"));
            Assert.That(document.Animations[5].ColorInterpolation,
                Is.EqualTo("hsl"));
            Assert.That(document.Animations[5]
                .ColorInterpolationDirection,
                Is.EqualTo("clockwise"));
            Assert.That(document.Animations[6].MotionPath,
                Is.EqualTo("M 0 0 L 10 10"));
            Assert.That(document.Animations[6].MotionOrigin,
                Is.EqualTo("default"));
            Assert.That(document.Animations[7].TransformType,
                Is.EqualTo("rotate"));
            Assert.That(document.Animations[8].TransitionType,
                Is.EqualTo("barWipe"));
            Assert.That(document.Animations[8].TransitionSubtype,
                Is.EqualTo("leftToRight"));
            Assert.That(document.Animations[8].TransitionDirection,
                Is.EqualTo("reverse"));
            Assert.That(document.Animations[8].FadeColor,
                Is.EqualTo("#112233"));
            Assert.That(document.Animations[8].TransitionMode,
                Is.EqualTo("out"));
            Assert.That(document.Animations[9].Href,
                Is.EqualTo("Sounds/cue.wav"));
            Assert.That(document.Animations[9].AudioLevel,
                Is.EqualTo("0.8"));
            Assert.That(command.Command, Is.EqualTo("play"));
            Assert.That(command.Parameters.Select(item => item.Name),
                Is.EqualTo(new[] { "verb", "direction" }));
            Assert.That(command.Parameters.Select(item => item.Value),
                Is.EqualTo(new[] { "start", "forward" }));
        });
    }

    [Test]
    public void WritesAnimationAttributesAndCommandParameters()
    {
        OdfDocument document = Read(AnimationXml());
        OdfAnimation sequence = document.Animations[0];
        OdfAnimation animate = document.Animations[3];
        OdfAnimation color = document.Animations[5];
        OdfAnimation motion = document.Animations[6];
        OdfAnimation transform = document.Animations[7];
        OdfAnimation transition = document.Animations[8];
        OdfAnimation audio = document.Animations[9];
        OdfAnimation command = document.Animations[10];
        sequence.Begin = "2s";
        sequence.Fill = "remove";
        animate.Values = "4;5;6";
        animate.Formula = "$value+1";
        animate.Additive = "replace";
        color.ColorInterpolation = "rgb";
        color.ColorInterpolationDirection = "counter-clockwise";
        motion.MotionPath = "M 5 5 L 20 20";
        motion.MotionOrigin = "layout";
        transform.TransformType = "scale";
        transition.TransitionType = "fade";
        transition.TransitionMode = "in";
        audio.Href = "Sounds/changed.wav";
        audio.AudioLevel = "0.5";
        command.Command = "stop";
        command.Parameters[1].Value = "reverse";
        command.Parameters =
        [
            command.Parameters[1],
            new OdfAnimationParameter
            {
                Name = "repeat",
                Value = "3"
            }
        ];

        OdfDocument reread = ReadBytes(Write(document));
        OdfAnimation changedSequence = reread.Animations[0];
        OdfAnimation changedAnimate = reread.Animations[3];
        OdfAnimation changedColor = reread.Animations[5];
        OdfAnimation changedMotion = reread.Animations[6];
        OdfAnimation changedTransform = reread.Animations[7];
        OdfAnimation changedTransition = reread.Animations[8];
        OdfAnimation changedAudio = reread.Animations[9];
        OdfAnimation changedCommand = reread.Animations[10];

        Assert.Multiple(() =>
        {
            Assert.That(changedSequence.Begin, Is.EqualTo("2s"));
            Assert.That(changedSequence.Fill, Is.EqualTo("remove"));
            Assert.That(changedAnimate.Values, Is.EqualTo("4;5;6"));
            Assert.That(changedAnimate.Formula, Is.EqualTo("$value+1"));
            Assert.That(changedAnimate.Additive,
                Is.EqualTo("replace"));
            Assert.That(changedColor.ColorInterpolation,
                Is.EqualTo("rgb"));
            Assert.That(changedColor.ColorInterpolationDirection,
                Is.EqualTo("counter-clockwise"));
            Assert.That(changedMotion.MotionPath,
                Is.EqualTo("M 5 5 L 20 20"));
            Assert.That(changedMotion.MotionOrigin,
                Is.EqualTo("layout"));
            Assert.That(changedTransform.TransformType,
                Is.EqualTo("scale"));
            Assert.That(changedTransition.TransitionType,
                Is.EqualTo("fade"));
            Assert.That(changedTransition.TransitionMode,
                Is.EqualTo("in"));
            Assert.That(changedAudio.Href,
                Is.EqualTo("Sounds/changed.wav"));
            Assert.That(changedAudio.AudioLevel, Is.EqualTo("0.5"));
            Assert.That(changedCommand.Command, Is.EqualTo("stop"));
            Assert.That(changedCommand.Parameters.Select(item => item.Name),
                Is.EqualTo(new[] { "direction", "repeat" }));
            Assert.That(changedCommand.Parameters.Select(item => item.Value),
                Is.EqualTo(new[] { "reverse", "3" }));
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
            "animations.fodp",
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

    static string AnimationXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + Namespaces()
            + " office:version='1.3' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:body><office:presentation>"
            + "<draw:page draw:name='Animated Slide'>"
            + "<anim:seq anim:id='root' xml:id='sequenceXml'"
            + " presentation:node-type='timing-root'"
            + " presentation:preset-id='preset-1'"
            + " presentation:preset-sub-type='sub-1'"
            + " presentation:preset-class='entrance'"
            + " presentation:master-element='master-1'"
            + " presentation:group-id='group-1'"
            + " smil:begin='0s' smil:end='20s' smil:dur='15s'"
            + " smil:min='1s' smil:max='18s' smil:repeatCount='2'"
            + " smil:repeatDur='30s' smil:restart='whenNotActive'"
            + " smil:restartDefault='always' smil:fill='freeze'"
            + " smil:fillDefault='remove' smil:endsync='last'"
            + " smil:speed='1.5' smil:accelerate='0.1'"
            + " smil:decelerate='0.2' smil:autoReverse='true'>"
            + "<anim:par>"
            + "<anim:iterate smil:targetElement='shape1'"
            + " anim:sub-item='text' anim:iterate-type='by-paragraph'"
            + " anim:iterate-interval='PT0.5S'>"
            + "<anim:animate smil:attributeName='svg:x'"
            + " smil:values='1;2;3' anim:formula='$value*2'"
            + " smil:from='1' smil:to='3' smil:by='2'"
            + " smil:calcMode='spline' smil:keyTimes='0;0.5;1'"
            + " smil:keySplines='0.1 0.2 0.3 0.4'"
            + " smil:additive='sum' smil:accumulate='sum'/>"
            + "<anim:set smil:attributeName='presentation:visibility'"
            + " smil:to='visible'/>"
            + "</anim:iterate>"
            + "<anim:animateColor anim:color-interpolation='hsl'"
            + " anim:color-interpolation-direction='clockwise'/>"
            + "<anim:animateMotion svg:path='M 0 0 L 10 10'"
            + " svg:origin='default'/>"
            + "<anim:animateTransform svg:type='rotate'/>"
            + "<anim:transitionFilter smil:type='barWipe'"
            + " smil:subtype='leftToRight' smil:direction='reverse'"
            + " smil:fadeColor='#112233' smil:mode='out'/>"
            + "<anim:audio xlink:href='Sounds/cue.wav'"
            + " anim:audio-level='0.8'/>"
            + "<anim:command anim:command='play'"
            + " smil:targetElement='shape1'>"
            + "<anim:param anim:name='verb' anim:value='start'/>"
            + "<anim:param anim:name='direction' anim:value='forward'/>"
            + "</anim:command>"
            + "</anim:par>"
            + "</anim:seq>"
            + "</draw:page>"
            + "</office:presentation></office:body>"
            + "</office:document>";
    }

    static string Namespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:anim='" + OdfNamespaces.Animation + "'"
            + " xmlns:smil='" + OdfNamespaces.Smil + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'";
    }
}
