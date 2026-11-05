using System.Text;

namespace odf.Tests;

public class ScriptDocumentModelTests
{
    [Test]
    public void PreservesExecutableLookingScriptsWithoutExecutingThem()
    {
        string source = "throw new Error('script descriptions are inert');";
        string xml = ScriptXml().Replace(
            "const answer = 42;",
            source,
            StringComparison.Ordinal);

        OdfDocument document = ReadFlat(xml);
        OdfScript script = document.Scripts.Single(item =>
            item.Language == "javascript");
        OdfDocument reread = ReadFlat(Encoding.UTF8.GetString(
            Write(document)));
        OdfScript preserved = reread.Scripts.Single(item =>
            item.Language == "javascript");

        Assert.Multiple(() =>
        {
            Assert.That(script.Text, Is.EqualTo(source));
            Assert.That(script.Classification,
                Is.EqualTo(OdfActiveContentClassification.EmbeddedCode));
            Assert.That(script.IsActiveContent, Is.True);
            Assert.That(preserved.Text, Is.EqualTo(source));
            Assert.That(preserved.Classification,
                Is.EqualTo(OdfActiveContentClassification.EmbeddedCode));
        });
    }

    [Test]
    public void InspectsScriptsListenersAndSecurityClassifications()
    {
        OdfDocument document = ReadFlat(ScriptXml());
        OdfScript basic = document.Scripts.Single(
            item => item.Language == "ooo:Basic");
        OdfScript javascript = document.Scripts.Single(
            item => item.Language == "javascript");
        OdfEventListener macro = Listener(document, "dom:load");
        OdfEventListener external = Listener(document, "dom:save");
        OdfEventListener linked = Listener(document, "dom:focus");
        OdfEventListener action = Listener(document, "dom:click");
        OdfEventListener inert = Listener(document, "dom:blur");

        Assert.Multiple(() =>
        {
            Assert.That(document.Scripts, Has.Length.EqualTo(2));
            Assert.That(basic.Kind, Is.EqualTo(OdfScriptKind.Embedded));
            Assert.That(basic.Classification,
                Is.EqualTo(OdfActiveContentClassification.EmbeddedCode));
            Assert.That(basic.IsActiveContent, Is.True);
            Assert.That(basic.ChildNodes.Single().Name?.LocalName,
                Is.EqualTo("libraries"));
            Assert.That(javascript.Text, Is.EqualTo("const answer = 42;"));
            Assert.That(macro.Classification,
                Is.EqualTo(OdfActiveContentClassification.MacroInvocation));
            Assert.That(external.Classification,
                Is.EqualTo(OdfActiveContentClassification.ExternalReference));
            Assert.That(external.IsExternal, Is.True);
            Assert.That(linked.Classification,
                Is.EqualTo(OdfActiveContentClassification.LinkedScript));
            Assert.That(linked.IsExternal, Is.False);
            Assert.That(action.Classification,
                Is.EqualTo(OdfActiveContentClassification.PresentationAction));
            Assert.That(inert.Classification,
                Is.EqualTo(OdfActiveContentClassification.Inert));
            Assert.That(inert.IsActiveContent, Is.False);
        });
    }

    [Test]
    public void AddsUpdatesAndRemovesEmbeddedScriptsAndListeners()
    {
        OdfDocument document = ReadFlat(ScriptXml());
        OdfScript javascript = document.Scripts.Single(
            item => item.Language == "javascript");
        javascript.Language = "application/javascript";
        javascript.Text = "const answer = 43;";
        OdfScript added = new OdfScript();
        added.Kind = OdfScriptKind.Embedded;
        added.Language = "python";
        added.Text = "result = 7";
        document.Scripts = [javascript, added];

        OdfEventListener macro = Listener(document, "dom:load");
        macro.MacroName = "Standard.Changed";
        OdfEventListener action = Listener(document, "dom:click");
        OdfEventListener addedListener = new OdfEventListener();
        addedListener.Kind = OdfEventListenerKind.Script;
        addedListener.EventName = "dom:close";
        addedListener.Language = "ooo:script";
        addedListener.Href = "#cleanup";
        addedListener.LinkType = "simple";
        addedListener.Actuate = "onRequest";
        addedListener.OwnerNode = javascript.Node?.Parent;
        document.EventListeners = [macro, action, addedListener];

        OdfDocument reread = ReadFlat(Encoding.UTF8.GetString(
            Write(document)));

        Assert.Multiple(() =>
        {
            Assert.That(reread.Scripts, Has.Length.EqualTo(2));
            Assert.That(reread.Scripts.Any(
                item => item.Language == "ooo:Basic"), Is.False);
            Assert.That(reread.Scripts.Single(
                    item => item.Language == "application/javascript").Text,
                Is.EqualTo("const answer = 43;"));
            Assert.That(reread.Scripts.Single(
                    item => item.Language == "python").Text,
                Is.EqualTo("result = 7"));
            Assert.That(reread.EventListeners, Has.Length.EqualTo(3));
            Assert.That(Listener(reread, "dom:load").MacroName,
                Is.EqualTo("Standard.Changed"));
            Assert.That(Listener(reread, "dom:close").Classification,
                Is.EqualTo(OdfActiveContentClassification.LinkedScript));
            Assert.That(reread.EventListeners.Any(
                item => item.EventName == "dom:save"), Is.False);
            Assert.That(reread.EventListeners.Any(
                item => item.EventName == "dom:focus"), Is.False);
        });
    }

    [Test]
    public void AddsUpdatesAndRemovesPackageScriptEntries()
    {
        OdfDocument document = ReadPackageWithScript();
        OdfScript existing = document.Scripts.Single();
        Assert.Multiple(() =>
        {
            Assert.That(existing.Kind,
                Is.EqualTo(OdfScriptKind.PackageEntry));
            Assert.That(existing.PackagePath, Is.EqualTo("Scripts/main.js"));
            Assert.That(existing.MediaType,
                Is.EqualTo("application/javascript"));
            Assert.That(Encoding.UTF8.GetString(existing.Data),
                Is.EqualTo("let value = 1;"));
            Assert.That(existing.IsActiveContent, Is.True);
        });

        existing.Data = Encoding.UTF8.GetBytes("let value = 2;");
        OdfScript added = new OdfScript();
        added.Kind = OdfScriptKind.PackageEntry;
        added.PackagePath = "Scripts/helper.py";
        added.MediaType = "text/x-python";
        added.Data = Encoding.UTF8.GetBytes("value = 3");
        document.Scripts = [existing, added];

        OdfDocument changed = ReadPackage(Write(document));
        Assert.Multiple(() =>
        {
            Assert.That(changed.Scripts, Has.Length.EqualTo(2));
            Assert.That(Encoding.UTF8.GetString(changed.Scripts.Single(
                    item => item.PackagePath == "Scripts/main.js").Data),
                Is.EqualTo("let value = 2;"));
            Assert.That(changed.Scripts.Single(
                    item => item.PackagePath == "Scripts/helper.py").MediaType,
                Is.EqualTo("text/x-python"));
        });

        OdfScript helper = changed.Scripts.Single(
            item => item.PackagePath == "Scripts/helper.py");
        changed.Scripts = [helper];
        OdfDocument removed = ReadPackage(Write(changed));

        Assert.Multiple(() =>
        {
            Assert.That(removed.Scripts, Has.Length.EqualTo(1));
            Assert.That(removed.Scripts[0].PackagePath,
                Is.EqualTo("Scripts/helper.py"));
            Assert.That(removed.PackageEntries.Any(
                item => item.Path == "Scripts/main.js"), Is.False);
        });
    }

    static OdfEventListener Listener(OdfDocument document, string eventName)
    {
        return document.EventListeners.Single(
            item => item.EventName == eventName);
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "scripts.fodt",
            new OdfLoadOptions());
    }

    static OdfDocument ReadPackageWithScript()
    {
        OdfDocument document = new OdfDocumentFactory().CreatePackageDocument(
            OdfDocumentKind.Text);
        OdfPackage package = document.Package
            ?? throw new InvalidOperationException();
        OdfPackageWriter writer = new OdfPackageWriter();
        OdfEditResult result = writer.AddPackageEntry(
            package,
            "Scripts/main.js",
            Encoding.UTF8.GetBytes("let value = 1;"),
            OdfCompressionMethod.Deflated);
        Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
        OdfManifestEntry manifest = package.Manifest?.Entries.Single(
            item => item.FullPath == "Scripts/main.js")
            ?? throw new InvalidOperationException();
        manifest.MediaType = "application/javascript";
        byte[] bytes = writer.WritePackageBytes(
            package,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return ReadPackage(bytes);
    }

    static OdfDocument ReadPackage(byte[] bytes)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "scripts.odt",
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

    static string ScriptXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:script='" + OdfNamespaces.Script
            + "' xmlns:presentation='" + OdfNamespaces.Presentation
            + "' xmlns:xlink='" + OdfNamespaces.XLink
            + "' xmlns:ooo='urn:example:script' office:version='1.4' "
            + "office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:scripts>"
            + "<office:script script:language='ooo:Basic'>"
            + "<ooo:libraries><ooo:module>Sub Main\nEnd Sub"
            + "</ooo:module></ooo:libraries></office:script>"
            + "<office:script script:language='javascript'>"
            + "const answer = 42;</office:script>"
            + "<office:event-listeners>"
            + "<script:event-listener script:event-name='dom:load' "
            + "script:language='ooo:script' "
            + "script:macro-name='Standard.Main'/>"
            + "<script:event-listener script:event-name='dom:save' "
            + "script:language='javascript' xlink:type='simple' "
            + "xlink:href='https://example.test/save.js' "
            + "xlink:actuate='onRequest'/>"
            + "<script:event-listener script:event-name='dom:focus' "
            + "script:language='javascript' xlink:type='simple' "
            + "xlink:href='#local-script'/>"
            + "<presentation:event-listener script:event-name='dom:click' "
            + "presentation:action='next-page'/>"
            + "<presentation:event-listener script:event-name='dom:blur' "
            + "presentation:action='none'/>"
            + "</office:event-listeners></office:scripts>"
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
