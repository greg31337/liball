using System.Text;

namespace odf.Tests;

public class PresentationShowModelTests
{
    [Test]
    public void MaterializesTransitionsEventsSoundsShowsAndSettings()
    {
        OdfDocument document = Read(PresentationXml());
        OdfSlide first = document.Slides[0];
        OdfSlideTransition transition = document.SlideTransitions.Single();
        OdfPresentationSettings settings = document.PresentationSettings;
        OdfPresentationShow show = document.PresentationShows.Single();
        OdfDrawingEvent drawingEvent = first.DrawingObjects.Single(item =>
            item.Name == "Action Shape").Events.Single();

        Assert.Multiple(() =>
        {
            Assert.That(transition.Slide, Is.SameAs(first));
            Assert.That(first.Transition, Is.SameAs(transition));
            Assert.That(transition.Kind,
                Is.EqualTo(OdfTransitionKind.BarWipe));
            Assert.That(transition.Subtype, Is.EqualTo("leftToRight"));
            Assert.That(transition.Direction, Is.EqualTo("reverse"));
            Assert.That(transition.Speed, Is.EqualTo("fast"));
            Assert.That(transition.Duration, Is.EqualTo("PT4.5S"));
            Assert.That(transition.IsDurationValid, Is.True);
            Assert.That(transition.IsRehearsalTiming, Is.True);
            Assert.That(transition.SoundHref,
                Is.EqualTo("Sounds/transition.wav"));
            Assert.That(transition.SoundPlayFull, Is.True);
            Assert.That(transition.IsSoundPlayFullValid, Is.True);
            Assert.That(show.Name, Is.EqualTo("Executive"));
            Assert.That(show.SlideNames,
                Is.EqualTo(new[] { "Intro", "Second" }));
            Assert.That(show.Slides,
                Is.EqualTo(new[] { document.Slides[0], document.Slides[1] }));
            Assert.That(show.IsPageListValid, Is.True);
            Assert.That(settings.StartSlide, Is.SameAs(first));
            Assert.That(settings.SelectedShow, Is.SameAs(show));
            Assert.That(settings.FullScreen, Is.True);
            Assert.That(settings.Endless, Is.True);
            Assert.That(settings.Pause, Is.EqualTo("PT2S"));
            Assert.That(settings.IsPauseValid, Is.True);
            Assert.That(settings.AnimationsEnabled, Is.False);
            Assert.That(settings.TransitionOnClickEnabled, Is.True);
            Assert.That(settings.MouseVisible, Is.True);
            Assert.That(drawingEvent.Kind,
                Is.EqualTo(OdfDrawingEventKind.Presentation));
            Assert.That(drawingEvent.Action, Is.EqualTo("show"));
            Assert.That(drawingEvent.Href, Is.EqualTo("#Second"));
            Assert.That(drawingEvent.Effect, Is.EqualTo("fade"));
            Assert.That(drawingEvent.SoundHref,
                Is.EqualTo("Sounds/click.wav"));
            Assert.That(drawingEvent.SoundType, Is.EqualTo("simple"));
            Assert.That(drawingEvent.SoundPlayFull, Is.False);
            Assert.That(drawingEvent.IsSoundPlayFullValid, Is.True);
            Assert.That(drawingEvent.IsSoundReferenceValid, Is.True);
        });
    }

    [Test]
    public void WritesTransitionTimingShowSettingsAndEventSoundChanges()
    {
        OdfDocument document = Read(PresentationXml());
        OdfSlideTransition transition = document.SlideTransitions.Single();
        transition.Kind = OdfTransitionKind.Fade;
        transition.Duration = "PT8S";
        transition.Speed = "slow";
        transition.SoundHref = "Sounds/changed.wav";
        transition.SoundPlayFull = false;
        OdfPresentationShow show = document.PresentationShows.Single();
        show.Name = "Reordered";
        show.Slides = [document.Slides[1], document.Slides[0]];
        OdfPresentationSettings settings = document.PresentationSettings;
        settings.StartSlide = document.Slides[1];
        settings.FullScreen = false;
        settings.AnimationsEnabled = true;
        settings.MouseAsPen = true;
        OdfDrawingEvent drawingEvent = document.Slides[0]
            .DrawingObjects.Single(item => item.Name == "Action Shape")
            .Events.Single();
        drawingEvent.Action = "next-page";
        drawingEvent.SoundHref = "Sounds/event-changed.wav";
        drawingEvent.SoundPlayFull = true;

        OdfDocument reread = ReadBytes(Write(document));
        OdfSlideTransition changed = reread.SlideTransitions.Single();
        OdfPresentationShow changedShow =
            reread.PresentationShows.Single();
        OdfPresentationSettings changedSettings =
            reread.PresentationSettings;
        OdfDrawingEvent changedEvent = reread.Slides[0]
            .DrawingObjects.Single(item => item.Name == "Action Shape")
            .Events.Single();

        Assert.Multiple(() =>
        {
            Assert.That(changed.Kind, Is.EqualTo(OdfTransitionKind.Fade));
            Assert.That(changed.Type, Is.EqualTo("fade"));
            Assert.That(changed.Duration, Is.EqualTo("PT8S"));
            Assert.That(changed.IsRehearsalTiming, Is.True);
            Assert.That(changed.Speed, Is.EqualTo("slow"));
            Assert.That(changed.SoundHref,
                Is.EqualTo("Sounds/changed.wav"));
            Assert.That(changed.SoundPlayFull, Is.False);
            Assert.That(changedShow.Name, Is.EqualTo("Reordered"));
            Assert.That(changedShow.SlideNames,
                Is.EqualTo(new[] { "Second", "Intro" }));
            Assert.That(changedShow.Slides,
                Is.EqualTo(new[] { reread.Slides[1], reread.Slides[0] }));
            Assert.That(changedSettings.StartSlide,
                Is.SameAs(reread.Slides[1]));
            Assert.That(changedSettings.SelectedShow,
                Is.SameAs(changedShow));
            Assert.That(changedSettings.ShowName,
                Is.EqualTo("Reordered"));
            Assert.That(changedSettings.FullScreen, Is.False);
            Assert.That(changedSettings.AnimationsEnabled, Is.True);
            Assert.That(changedSettings.MouseAsPen, Is.True);
            Assert.That(changedEvent.Action, Is.EqualTo("next-page"));
            Assert.That(changedEvent.SoundHref,
                Is.EqualTo("Sounds/event-changed.wav"));
            Assert.That(changedEvent.SoundPlayFull, Is.True);
        });
    }

    [Test]
    public void CreatesCustomShowAndPlaybackSettings()
    {
        OdfDocument document = Read(MinimalPresentationXml());
        OdfPresentationShow show = new OdfPresentationShow
        {
            Name = "Added Show",
            Slides = [document.Slides[1], document.Slides[0]]
        };
        document.PresentationShows = [show];
        document.PresentationSettings.StartSlide = document.Slides[1];
        document.PresentationSettings.SelectedShow = show;
        document.PresentationSettings.FullScreen = true;
        document.PresentationSettings.AnimationsEnabled = true;

        OdfDocument reread = ReadBytes(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(reread.PresentationShows.Single().SlideNames,
                Is.EqualTo(new[] { "Second", "Intro" }));
            Assert.That(reread.PresentationSettings.StartSlide,
                Is.SameAs(reread.Slides[1]));
            Assert.That(reread.PresentationSettings.SelectedShow,
                Is.SameAs(reread.PresentationShows[0]));
            Assert.That(reread.PresentationSettings.FullScreen, Is.True);
            Assert.That(reread.PresentationSettings.AnimationsEnabled,
                Is.True);
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
            "shows.fodp",
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

    static string PresentationXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + Namespaces()
            + " office:version='1.3' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:automatic-styles>"
            + "<style:style style:name='SlideStyle' "
            + "style:family='drawing-page'>"
            + "<style:drawing-page-properties "
            + "presentation:transition-type='automatic' "
            + "presentation:transition-style='fade-from-left' "
            + "presentation:transition-speed='fast' "
            + "presentation:duration='PT4.5S' smil:type='barWipe' "
            + "smil:subtype='leftToRight' smil:direction='reverse' "
            + "smil:fadeColor='#102030'>"
            + "<presentation:sound xlink:href='Sounds/transition.wav' "
            + "xlink:type='simple' xlink:show='new' "
            + "xlink:actuate='onRequest' presentation:play-full='true'/>"
            + "</style:drawing-page-properties></style:style>"
            + "</office:automatic-styles>"
            + "<office:body><office:presentation>"
            + "<draw:page draw:name='Intro' draw:style-name='SlideStyle'>"
            + "<draw:rect draw:name='Action Shape'>"
            + "<office:event-listeners>"
            + "<presentation:event-listener script:event-name='dom:click' "
            + "presentation:action='show' presentation:effect='fade' "
            + "presentation:direction='from-left' "
            + "presentation:speed='medium' xlink:href='#Second' "
            + "xlink:type='simple' xlink:show='embed' "
            + "xlink:actuate='onRequest'>"
            + "<presentation:sound xlink:href='Sounds/click.wav' "
            + "xlink:type='simple' xlink:show='new' "
            + "xlink:actuate='onRequest' presentation:play-full='false'/>"
            + "</presentation:event-listener>"
            + "</office:event-listeners></draw:rect></draw:page>"
            + "<draw:page draw:name='Second'/>"
            + "<presentation:settings presentation:start-page='Intro' "
            + "presentation:show='Executive' presentation:full-screen='true' "
            + "presentation:endless='true' presentation:pause='PT2S' "
            + "presentation:show-logo='true' presentation:force-manual='false' "
            + "presentation:mouse-visible='true' presentation:mouse-as-pen='false' "
            + "presentation:start-with-navigator='true' "
            + "presentation:animations='disabled' "
            + "presentation:transition-on-click='enabled' "
            + "presentation:stay-on-top='false' "
            + "presentation:show-end-of-presentation-slide='true'>"
            + "<presentation:show presentation:name='Executive' "
            + "presentation:pages='Intro, Second'/>"
            + "</presentation:settings>"
            + "</office:presentation></office:body></office:document>";
    }

    static string MinimalPresentationXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + Namespaces()
            + " office:version='1.3' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:body><office:presentation>"
            + "<draw:page draw:name='Intro'/>"
            + "<draw:page draw:name='Second'/>"
            + "</office:presentation></office:body></office:document>";
    }

    static string Namespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:smil='" + OdfNamespaces.Smil + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:script='" + OdfNamespaces.Script + "'";
    }
}
