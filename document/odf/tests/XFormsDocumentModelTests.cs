using System.Text;

namespace odf.Tests;

public class XFormsDocumentModelTests
{
    [Test]
    public void MaterializesModelsInstancesBindsSubmissionsAndSchemas()
    {
        OdfDocument document = ReadPackage();
        OdfXFormsModel model = document.XFormsModels.Single();
        OdfXFormsInstance inline = model.Instances.Single(
            item => item.Id == "order");
        OdfXFormsInstance external = model.Instances.Single(
            item => item.Id == "lookup");
        OdfXFormsBind root = model.Binds.Single();
        OdfXFormsSubmission submission = model.Submissions.Single();
        OdfXFormsSchemaReference packaged = model.SchemaReferences.Single(
            item => item.Location == "Schemas/order.xsd");
        OdfXFormsSchemaReference remote = model.SchemaReferences.Single(
            item => item.Location == "https://example.test/remote.xsd");
        OdfXFormsSchemaReference inlineSchema =
            model.SchemaReferences.Single(item => item.IsInline);

        Assert.Multiple(() =>
        {
            Assert.That(model.Id, Is.EqualTo("orders"));
            Assert.That(model.Functions, Is.EqualTo("ex:normalize"));
            Assert.That(model.Version, Is.EqualTo("1.1"));
            Assert.That(model.Instances, Has.Length.EqualTo(2));
            Assert.That(inline.InlineRoot?.Name?.LocalName,
                Is.EqualTo("order"));
            Assert.That(inline.IsExternal, Is.False);
            Assert.That(external.EffectiveSource,
                Is.EqualTo("Data/lookup.xml"));
            Assert.That(external.PackagePath,
                Is.EqualTo("Data/lookup.xml"));
            Assert.That(external.PackageEntryExists, Is.True);
            Assert.That(external.InlineRoot, Is.Null);
            Assert.That(root.NodeSet, Is.EqualTo("/d:order"));
            Assert.That(root.Type, Is.EqualTo("xsd:string"));
            Assert.That(root.ReadOnlyExpression, Is.EqualTo("false()"));
            Assert.That(root.RelevantExpression, Is.EqualTo("true()"));
            Assert.That(root.RequiredExpression, Is.EqualTo("true()"));
            Assert.That(root.ConstraintExpression,
                Is.EqualTo("string-length(.) > 0"));
            Assert.That(root.CalculateExpression, Is.EqualTo("../seed"));
            Assert.That(root.P3PType, Is.EqualTo("work"));
            Assert.That(root.Children[0].Parent, Is.SameAs(root));
            Assert.That(submission.Method, Is.EqualTo("post"));
            Assert.That(submission.Serialization,
                Is.EqualTo("application/xml"));
            Assert.That(submission.MediaType,
                Is.EqualTo("application/xml"));
            Assert.That(submission.Encoding, Is.EqualTo("UTF-8"));
            Assert.That(submission.Replace, Is.EqualTo("instance"));
            Assert.That(submission.TargetRef, Is.EqualTo("/d:order"));
            Assert.That(submission.ResourceNode, Is.Not.Null);
            Assert.That(packaged.PackagePath,
                Is.EqualTo("Schemas/order.xsd"));
            Assert.That(packaged.PackageEntryExists, Is.True);
            Assert.That(packaged.IsExternal, Is.False);
            Assert.That(remote.IsExternal, Is.True);
            Assert.That(remote.PackageEntryExists, Is.False);
            Assert.That(inlineSchema.Node?.Name?.NamespaceName,
                Is.EqualTo("http://www.w3.org/2001/XMLSchema"));
        });
    }

    [Test]
    public void ResolvesSubmissionAndOdfControlReferences()
    {
        OdfDocument document = ReadPackage();
        OdfXFormsModel model = document.XFormsModels[0];
        OdfXFormsBind name = model.Binds[0].Children[0];
        OdfXFormsSubmission submission = model.Submissions[0];
        OdfFormControl input = document.FormControls.Single(
            item => item.Name == "NameInput");
        OdfFormControl broken = document.FormControls.Single(
            item => item.Name == "BrokenInput");

        Assert.Multiple(() =>
        {
            Assert.That(submission.BindTarget, Is.SameAs(name));
            Assert.That(submission.InstanceTarget,
                Is.SameAs(model.Instances[0]));
            Assert.That(submission.IsBindReferenceBroken, Is.False);
            Assert.That(submission.IsInstanceReferenceBroken, Is.False);
            Assert.That(input.Binding.XFormsBindTarget, Is.SameAs(name));
            Assert.That(input.Binding.XFormsListSourceTarget,
                Is.SameAs(model.Binds[0]));
            Assert.That(input.Binding.XFormsSubmissionTarget,
                Is.SameAs(submission));
            Assert.That(input.Binding.IsXFormsBindBroken, Is.False);
            Assert.That(broken.Binding.IsXFormsBindBroken, Is.True);
            Assert.That(broken.Binding.IsXFormsListSourceBroken, Is.True);
            Assert.That(broken.Binding.IsXFormsSubmissionBroken, Is.True);
        });
    }

    [Test]
    public void WritesModelChangesAndRebuildsReferenceState()
    {
        OdfDocument document = ReadFlat();
        OdfXFormsModel model = document.XFormsModels[0];
        OdfXFormsInstance instance = model.Instances[0];
        OdfXFormsBind root = model.Binds[0];
        OdfXFormsBind name = root.Children[0];
        OdfXFormsSubmission submission = model.Submissions[0];
        model.Functions = "ex:trim";
        model.Version = "1.1 2.0";
        model.SchemaReferences[0].Location =
            "https://example.test/changed.xsd";
        instance.Id = "changed-order";
        root.RelevantExpression = "../enabled = true()";
        name.NodeSet = "d:customer-name";
        submission.Method = "put";
        submission.Bind = "root";
        submission.Instance = "changed-order";
        submission.Resource = "https://example.test/changed";

        OdfDocument reread = ReadFlatBytes(Write(document));
        OdfXFormsModel changed = reread.XFormsModels[0];
        OdfXFormsSubmission changedSubmission = changed.Submissions[0];

        Assert.Multiple(() =>
        {
            Assert.That(changed.Functions, Is.EqualTo("ex:trim"));
            Assert.That(changed.Version, Is.EqualTo("1.1 2.0"));
            Assert.That(changed.Schema,
                Is.EqualTo("https://example.test/changed.xsd https://example.test/remote.xsd"));
            Assert.That(changed.Instances[0].Id,
                Is.EqualTo("changed-order"));
            Assert.That(changed.Binds[0].RelevantExpression,
                Is.EqualTo("../enabled = true()"));
            Assert.That(changed.Binds[0].Children[0].NodeSet,
                Is.EqualTo("d:customer-name"));
            Assert.That(changedSubmission.Method, Is.EqualTo("put"));
            Assert.That(changedSubmission.BindTarget,
                Is.SameAs(changed.Binds[0]));
            Assert.That(changedSubmission.InstanceTarget,
                Is.SameAs(changed.Instances[0]));
            Assert.That(changedSubmission.Resource,
                Is.EqualTo("https://example.test/changed"));
        });
    }

    [Test]
    public void PreservesForeignContentAndExternalLocationsWithoutExecutingThem()
    {
        OdfDocument document = ReadFlat();
        OdfXFormsModel model = document.XFormsModels[0];
        OdfXFormsInstance external = model.Instances[1];
        OdfXFormsSchemaReference remote = model.SchemaReferences.Single(
            item => item.Location == "https://example.test/remote.xsd");
        string output = Encoding.UTF8.GetString(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(external.IsExternal, Is.True);
            Assert.That(external.PackagePath, Is.Empty);
            Assert.That(remote.IsExternal, Is.True);
            Assert.That(output, Does.Contain("ext:preserved"));
            Assert.That(output, Does.Contain("xforms:dispatch"));
            Assert.That(output, Does.Contain("https://example.test/submit"));
            Assert.That(output, Does.Contain("Data/lookup.xml"));
        });
    }

    static OdfDocument ReadFlat()
    {
        return ReadFlatBytes(Encoding.UTF8.GetBytes(FlatXml()));
    }

    static OdfDocument ReadFlatBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "xforms.fodt",
            new OdfLoadOptions());
    }

    static OdfDocument ReadPackage()
    {
        OdfDocument skeleton = new OdfDocumentFactory().CreatePackageDocument(
            OdfDocumentKind.Text);
        OdfPackage? package = skeleton.Package;
        Assert.That(package, Is.Not.Null);
        OdfPackageWriter writer = new OdfPackageWriter();
        OdfEditResult content = writer.ReplacePackageEntry(
            package,
            OdfPartNames.Content,
            Encoding.UTF8.GetBytes(ContentXml()));
        OdfEditResult schema = writer.AddPackageEntry(
            package,
            "Schemas/order.xsd",
            Encoding.UTF8.GetBytes(
                "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'/>"),
            OdfCompressionMethod.Deflated);
        OdfEditResult data = writer.AddPackageEntry(
            package,
            "Data/lookup.xml",
            Encoding.UTF8.GetBytes("<lookup><item>one</item></lookup>"),
            OdfCompressionMethod.Deflated);
        Assert.That(content.Status, Is.EqualTo(OdfEditStatus.Success));
        Assert.That(schema.Status, Is.EqualTo(OdfEditStatus.Success));
        Assert.That(data.Status, Is.EqualTo(OdfEditStatus.Success));
        byte[] bytes = writer.WritePackageBytes(
            package,
            new OdfSaveOptions());
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "xforms.odt",
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

    static string FlatXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + NamespaceDeclarations()
            + " office:version='1.4'>"
            + "<office:body><office:text>" + FormsXml()
            + "<text:p/></office:text></office:body></office:document>";
    }

    static string ContentXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content" + NamespaceDeclarations()
            + " office:version='1.4'>"
            + "<office:body><office:text>" + FormsXml()
            + "<text:p/></office:text></office:body>"
            + "</office:document-content>";
    }

    static string FormsXml()
    {
        return "<office:forms>"
            + "<xforms:model id='orders' functions='ex:normalize' "
            + "schema='Schemas/order.xsd https://example.test/remote.xsd' "
            + "version='1.1'>"
            + "<xforms:instance id='order'><d:order><d:name>Ada</d:name>"
            + "</d:order></xforms:instance>"
            + "<xforms:instance id='lookup' src='Data/lookup.xml' "
            + "resource='fallback.xml'/>"
            + "<xforms:bind id='root' nodeset='/d:order' type='xsd:string' "
            + "readonly='false()' relevant='true()' required='true()' "
            + "constraint='string-length(.) &gt; 0' calculate='../seed' "
            + "p3ptype='work'><xforms:bind id='name' nodeset='d:name'/>"
            + "</xforms:bind>"
            + "<xs:schema targetNamespace='urn:example:data'>"
            + "<xs:element name='order' type='xs:string'/></xs:schema>"
            + "<xforms:submission id='save' bind='name' instance='order' "
            + "ref='/d:order' resource='https://example.test/submit' "
            + "action='https://example.test/legacy' mode='synchronous' "
            + "method='post' validate='true' relevant='true' "
            + "serialization='application/xml' version='1.0' indent='true' "
            + "mediatype='application/xml' encoding='UTF-8' "
            + "omit-xml-declaration='false' standalone='false' "
            + "cdata-section-elements='d:notes' replace='instance' "
            + "targetref='/d:order' separator='&amp;' "
            + "includenamespaceprefixes='d ex'>"
            + "<xforms:resource value='concat(\"https://example.test/\", d:id)'/>"
            + "<xforms:dispatch name='audit' targetid='logger'/>"
            + "</xforms:submission><ext:preserved ext:value='yes'/>"
            + "</xforms:model>"
            + "<form:form form:name='Controls'>"
            + "<form:text form:name='NameInput' xml:id='name-input' "
            + "xforms:bind='name' form:xforms-list-source='root' "
            + "form:xforms-submission='save'/>"
            + "<form:text form:name='BrokenInput' xml:id='broken-input' "
            + "xforms:bind='missing-bind' "
            + "form:xforms-list-source='missing-list' "
            + "form:xforms-submission='missing-submission'/>"
            + "</form:form></office:forms>";
    }

    static string NamespaceDeclarations()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:form='" + OdfNamespaces.Form + "'"
            + " xmlns:xforms='" + OdfNamespaces.XForms + "'"
            + " xmlns:xs='http://www.w3.org/2001/XMLSchema'"
            + " xmlns:xsd='http://www.w3.org/2001/XMLSchema'"
            + " xmlns:d='urn:example:data'"
            + " xmlns:ex='urn:example:functions'"
            + " xmlns:ext='urn:example:extension'";
    }
}
