using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class MediaResourceManagerTests
{
    [Test]
    public void DescribesMediaAndActiveContentWithoutLoadingIt()
    {
        OdfDocument document = ReadPackage(CreatePackage());
        OdfMediaResource audio = Resource(
            document,
            "Media/sound.ogg");
        OdfMediaResource video = Resource(
            document,
            "https://example.com/movie.mp4");
        OdfMediaResource plugin = Resource(
            document,
            "https://example.com/widget");
        OdfMediaResource applet = document.MediaResources.Single(item =>
            item.Kind == OdfMediaResourceKind.Applet);
        OdfMediaResource floating = document.MediaResources.Single(item =>
            item.Kind == OdfMediaResourceKind.FloatingFrame);

        Assert.Multiple(() =>
        {
            Assert.That(document.MediaResources, Has.Length.EqualTo(5));
            Assert.That(audio.Kind, Is.EqualTo(OdfMediaResourceKind.Audio));
            Assert.That(audio.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.Package));
            Assert.That(audio.PackagePath, Is.EqualTo("Media/sound.ogg"));
            Assert.That(audio.MediaType, Is.EqualTo("audio/ogg"));
            Assert.That(audio.PackageEntryExists, Is.True);
            Assert.That(audio.IsContentAvailable, Is.True);
            Assert.That(audio.IsEncrypted, Is.False);
            Assert.That(audio.IsActiveContent, Is.False);
            Assert.That(audio.Parameters[0].Name, Is.EqualTo("volume"));
            Assert.That(video.Kind, Is.EqualTo(OdfMediaResourceKind.Video));
            Assert.That(video.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.External));
            Assert.That(video.PackageEntryExists, Is.False);
            Assert.That(plugin.Kind,
                Is.EqualTo(OdfMediaResourceKind.Plugin));
            Assert.That(plugin.IsActiveContent, Is.True);
            Assert.That(plugin.Parameters[0].Value, Is.EqualTo("safe"));
            Assert.That(applet.Code, Is.EqualTo("ExampleApplet"));
            Assert.That(applet.ObjectName, Is.EqualTo("saved-state"));
            Assert.That(applet.Archive, Is.EqualTo("example.jar"));
            Assert.That(applet.MayScript, Is.True);
            Assert.That(applet.IsMayScriptValid, Is.True);
            Assert.That(applet.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.Unknown));
            Assert.That(floating.FrameName, Is.EqualTo("reference"));
            Assert.That(floating.IsActiveContent, Is.True);
            Assert.That(audio.Drawing?.MediaResource, Is.SameAs(audio));
        });
    }

    [Test]
    public void WritesMediaAppletParameterAndFloatingFrameChanges()
    {
        OdfDocument document = ReadPackage(CreatePackage());
        OdfMediaResource audio = Resource(document, "Media/sound.ogg");
        OdfMediaResource plugin = Resource(
            document,
            "https://example.com/widget");
        OdfMediaResource applet = document.MediaResources.Single(item =>
            item.Kind == OdfMediaResourceKind.Applet);
        OdfMediaResource floating = document.MediaResources.Single(item =>
            item.Kind == OdfMediaResourceKind.FloatingFrame);

        audio.Href = "https://cdn.example.com/replacement.webm";
        audio.MediaType = "video/webm";
        plugin.Parameters[0].Value = "changed";
        applet.Code = "ChangedApplet";
        applet.MayScript = false;
        applet.Parameters =
        [
            new OdfMediaParameter
            {
                Name = "replacement",
                Value = "inert"
            }
        ];
        floating.FrameName = "changed-frame";

        OdfDocument reread = ReadPackage(Write(document));
        OdfMediaResource changedVideo = Resource(
            reread,
            "https://cdn.example.com/replacement.webm");
        OdfMediaResource changedPlugin = Resource(
            reread,
            "https://example.com/widget");
        OdfMediaResource changedApplet = reread.MediaResources.Single(item =>
            item.Kind == OdfMediaResourceKind.Applet);
        OdfMediaResource changedFloating = reread.MediaResources.Single(item =>
            item.Kind == OdfMediaResourceKind.FloatingFrame);

        Assert.Multiple(() =>
        {
            Assert.That(changedVideo.Kind,
                Is.EqualTo(OdfMediaResourceKind.Video));
            Assert.That(changedVideo.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.External));
            Assert.That(changedPlugin.Parameters[0].Value,
                Is.EqualTo("changed"));
            Assert.That(changedApplet.Code, Is.EqualTo("ChangedApplet"));
            Assert.That(changedApplet.MayScriptValue, Is.EqualTo("false"));
            Assert.That(changedApplet.MayScript, Is.False);
            Assert.That(changedApplet.Parameters, Has.Length.EqualTo(1));
            Assert.That(changedApplet.Parameters[0].Name,
                Is.EqualTo("replacement"));
            Assert.That(changedFloating.FrameName,
                Is.EqualTo("changed-frame"));
        });
    }

    [Test]
    public void ReportsMalformedAndMissingResourceReferences()
    {
        OdfDocument document = ReadPackage(CreatePackage(
            "<draw:frame draw:name='Malformed'>"
            + "<draw:plugin xlink:href='bad percent%2' "
            + "xlink:type='simple' draw:mime-type='not a media type'/>"
            + "</draw:frame>"
            + "<draw:frame draw:name='Missing'>"
            + "<draw:plugin xlink:href='Media/missing.ogg' "
            + "xlink:type='simple' draw:mime-type='audio/ogg'/>"
            + "</draw:frame>"));
        OdfMediaResource malformed = Resource(document, "bad percent%2");
        OdfMediaResource missing = Resource(document, "Media/missing.ogg");

        Assert.Multiple(() =>
        {
            Assert.That(malformed.IsReferenceValid, Is.False);
            Assert.That(malformed.IsMediaTypeValid, Is.False);
            Assert.That(malformed.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.Unknown));
            Assert.That(missing.IsReferenceValid, Is.True);
            Assert.That(missing.SourceKind,
                Is.EqualTo(OdfResourceSourceKind.Package));
            Assert.That(missing.PackageEntryExists, Is.False);
            Assert.That(missing.IsContentAvailable, Is.False);
        });
    }

    static OdfMediaResource Resource(OdfDocument document, string href)
    {
        return document.MediaResources.Single(item => item.Href == href);
    }

    static OdfDocument ReadPackage(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "media.odp",
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

    static byte[] CreatePackage()
    {
        return CreatePackage("");
    }

    static byte[] CreatePackage(string additionalContent)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Presentation),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes(ContentXml(additionalContent)),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(ManifestXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Media/sound.ogg",
                [79, 103, 103, 83, 0, 2, 4, 8],
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] data,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }

    static string ContentXml(string additionalContent)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'>"
            + "<office:body><office:presentation>"
            + "<draw:page draw:name='Page'>"
            + "<draw:frame draw:name='Audio'>"
            + "<draw:plugin xlink:href='Media/sound.ogg' "
            + "xlink:type='simple'><draw:param draw:name='volume' "
            + "draw:value='75'/></draw:plugin></draw:frame>"
            + "<draw:frame draw:name='Video'>"
            + "<draw:plugin xlink:href='https://example.com/movie.mp4' "
            + "xlink:type='simple' draw:mime-type='video/mp4'/>"
            + "</draw:frame>"
            + "<draw:frame draw:name='Plugin'>"
            + "<draw:plugin xlink:href='https://example.com/widget' "
            + "xlink:type='simple' draw:mime-type='application/x-example'>"
            + "<draw:param draw:name='mode' draw:value='safe'/>"
            + "</draw:plugin></draw:frame>"
            + "<draw:frame draw:name='Applet'>"
            + "<draw:applet draw:code='ExampleApplet' "
            + "draw:object='saved-state' draw:archive='example.jar' "
            + "draw:may-script='true'><draw:param draw:name='theme' "
            + "draw:value='light'/></draw:applet></draw:frame>"
            + "<draw:frame draw:name='Floating'>"
            + "<draw:floating-frame draw:frame-name='reference' "
            + "xlink:href='https://example.com/reference' "
            + "xlink:type='simple'/></draw:frame>"
            + additionalContent
            + "</draw:page></office:presentation></office:body>"
            + "</office:document-content>";
    }

    static string ManifestXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest + "' manifest:version='1.4'>"
            + "<manifest:file-entry manifest:full-path='/' "
            + "manifest:media-type='" + OdfMediaTypes.Presentation + "'/>"
            + "<manifest:file-entry manifest:full-path='content.xml' "
            + "manifest:media-type='text/xml'/>"
            + "<manifest:file-entry manifest:full-path='Media/sound.ogg' "
            + "manifest:media-type='audio/ogg'/>"
            + "</manifest:manifest>";
    }
}
