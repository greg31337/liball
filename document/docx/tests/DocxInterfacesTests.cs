using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxInterfacesTests
{
    [Test]
    public void CoreServices_ExposeFocusedPublicInterfaces()
    {
        global::docx.IDocxPackageInspector package = global::docx.DocxPackage.Create();
        global::docx.IDocxTextContent content = global::docx.DocxDocument.Create();
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();

        Assert.That(package.PartCount(), Is.GreaterThan(0));
        Assert.That(content.GetText(), Is.EqualTo(""));
        Assert.That(validator.ValidatePackage(package).HasErrors(), Is.False);
    }

    [Test]
    public void TextBearingModels_ImplementSharedTextContentContract()
    {
        Type interfaceType = typeof(global::docx.IDocxTextContent);
        Type[] modelTypes = new Type[]
        {
            typeof(global::docx.DocxDocument),
            typeof(global::docx.DocxBody),
            typeof(global::docx.DocxBlock),
            typeof(global::docx.DocxParagraph),
            typeof(global::docx.DocxRun),
            typeof(global::docx.DocxBreak),
            typeof(global::docx.DocxHyperlink),
            typeof(global::docx.DocxField),
            typeof(global::docx.DocxContentControl),
            typeof(global::docx.DocxTable),
            typeof(global::docx.DocxTableCell),
            typeof(global::docx.DocxHeader),
            typeof(global::docx.DocxFooter),
            typeof(global::docx.DocxFootnotes),
            typeof(global::docx.DocxFootnote),
            typeof(global::docx.DocxEndnotes),
            typeof(global::docx.DocxEndnote),
            typeof(global::docx.DocxComments),
            typeof(global::docx.DocxComment)
        };

        foreach (Type modelType in modelTypes)
        {
            Assert.That(interfaceType.IsAssignableFrom(modelType), Is.True, modelType.FullName);
        }
    }

    [Test]
    public void TextExtractor_UsesTextContentInterface()
    {
        FakeTextContent content = new FakeTextContent("from fake");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        string text = global::docx.DocxTextExtractor.Extract(content, options);

        Assert.That(text, Is.EqualTo("from fake"));
        Assert.That(content.LastOptions, Is.SameAs(options));
    }

    [Test]
    public void Validator_AcceptsPackageInspectorTestDouble()
    {
        FakePackageInspector package = new FakePackageInspector();
        global::docx.IDocxValidator validator = new global::docx.DocxValidator();

        global::docx.DocxValidationReport report = validator.ValidatePackage(package);

        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingContentTypesCode), Is.True);
        Assert.That(report.ContainsCode(global::docx.DocxPackageValidator.MissingPackageRelationshipsCode), Is.True);
        Assert.That(package.ContentTypesReadCount, Is.GreaterThan(0));
    }

    class FakeTextContent : global::docx.IDocxTextContent
    {
        public string Text;
        public global::docx.DocxTextOptions LastOptions;

        public FakeTextContent(string text)
        {
            this.Text = text;
            this.LastOptions = null!;
        }

        public string GetText()
        {
            return this.Text;
        }

        public string GetTextWithOptions(global::docx.DocxTextOptions options)
        {
            this.LastOptions = options;
            return this.Text;
        }
    }

    class FakePackageInspector : global::docx.IDocxPackageInspector
    {
        public global::docx.DocxContentTypes ContentTypes;
        public int ContentTypesReadCount;

        public FakePackageInspector()
        {
            this.ContentTypes = new global::docx.DocxContentTypes();
            this.ContentTypesReadCount = 0;
        }

        public global::docx.DocxContentTypes GetContentTypes()
        {
            this.ContentTypesReadCount = this.ContentTypesReadCount + 1;
            return this.ContentTypes;
        }

        public int PartCount()
        {
            return 0;
        }

        public global::docx.DocxPart GetPart(int index)
        {
            return null!;
        }

        public global::docx.DocxPart FindPart(string partName)
        {
            return null!;
        }

        public bool ContainsPart(string partName)
        {
            return false;
        }

        public int RelationshipSetCount()
        {
            return 0;
        }

        public global::docx.DocxRelationshipSet GetRelationshipSet(int index)
        {
            return null!;
        }

        public global::docx.DocxRelationshipSet FindRelationshipSet(string sourcePartName)
        {
            return null!;
        }

        public global::docx.DocxRelationshipSet GetPackageRelationshipSet()
        {
            return null!;
        }

        public global::docx.DocxPart GetStylesPart()
        {
            return null!;
        }

        public global::docx.DocxPart GetNumberingPart()
        {
            return null!;
        }

        public global::docx.DocxPart GetFootnotesPart()
        {
            return null!;
        }

        public global::docx.DocxPart GetEndnotesPart()
        {
            return null!;
        }

        public global::docx.DocxPart GetCommentsPart()
        {
            return null!;
        }
    }
}

[TestFixture]
public class DocxPublicApiInventoryCoverageTests
{
    const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    [Test]
    public void PublicApiInventory_MatchesTheBehavioralCoverageBaseline()
    {
        Type[] types = PublicApiTypes();
        List<ConstructorInfo> constructors = new List<ConstructorInfo>();
        List<MethodInfo> interfaceMethods = new List<MethodInfo>();
        List<MethodInfo> implementationMethods = new List<MethodInfo>();
        List<FieldInfo> fields = new List<FieldInfo>();
        List<FieldInfo> enumValues = new List<FieldInfo>();

        foreach (Type type in types)
        {
            constructors.AddRange(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(field => !field.IsSpecialName));

            if (type.IsInterface)
            {
                interfaceMethods.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            }
            else if (type.IsEnum)
            {
                enumValues.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
            }
            else
            {
                implementationMethods.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
            }
        }

        string fingerprint = CreateInventoryFingerprint(types);

        Assert.Multiple(() =>
        {
            Assert.That(types, Has.Length.EqualTo(165));
            Assert.That(constructors, Has.Count.EqualTo(109));
            Assert.That(interfaceMethods, Has.Count.EqualTo(670));
            Assert.That(implementationMethods, Has.Count.EqualTo(706));
            Assert.That(fields, Has.Count.EqualTo(867));
            Assert.That(enumValues, Has.Count.EqualTo(118));
            Assert.That(fingerprint, Is.EqualTo("EA90B4C5D82ACE1B7B4FB80F9BF378E40A07775432100258E0ADD1B8B2739567"));
        });
    }

    [Test]
    public void PublicDataFieldsAndEnumValues_AreUsableThroughTheExportedApi()
    {
        Type[] types = PublicApiTypes();
        int instanceFieldCount = 0;
        int staticFieldCount = 0;
        int enumValueCount = 0;

        foreach (Type type in types)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(field => !field.IsSpecialName)
                .ToArray();

            foreach (FieldInfo field in fields)
            {
                if (field.IsStatic)
                {
                    object? value = field.GetValue(null);
                    Assert.That(value, Is.Not.Null, type.FullName + "." + field.Name);
                    staticFieldCount = staticFieldCount + 1;

                    if (type.IsEnum)
                    {
                        string? name = Enum.GetName(type, value!);
                        Assert.That(name, Is.EqualTo(field.Name));
                        enumValueCount = enumValueCount + 1;
                    }

                    continue;
                }

                object instance = RuntimeHelpers.GetUninitializedObject(type);
                object valueToSet = CreateFieldValue(field.FieldType);
                field.SetValue(instance, valueToSet);
                Assert.That(field.GetValue(instance), Is.EqualTo(valueToSet), type.FullName + "." + field.Name);
                instanceFieldCount = instanceFieldCount + 1;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(instanceFieldCount, Is.EqualTo(609));
            Assert.That(staticFieldCount, Is.EqualTo(258));
            Assert.That(enumValueCount, Is.EqualTo(118));
        });
    }

    [Test]
    public void PreviouslyUncoveredInterfaceMethods_ExerciseConsumerSuccessPaths()
    {
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.IDocxContentControlManagement controls = new global::docx.DocxContentControlManagement();
        global::docx.IDocxTableEditing tables = new global::docx.DocxTableEditing();
        global::docx.IDocxNoteManagement notes = new global::docx.DocxNoteManagement();
        global::docx.IDocxLinkBookmarkFieldManagement fields = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();

        List<global::docx.DocxBlock> originalBlocks = new List<global::docx.DocxBlock>();
        originalBlocks.Add(factory.CreateParagraphBlock(factory.CreateTextParagraph("Original")));
        global::docx.DocxContentControl richText = controls.CreateRichTextControl("Rich", "rich", originalBlocks);
        List<global::docx.DocxBlock> replacementBlocks = new List<global::docx.DocxBlock>();
        replacementBlocks.Add(factory.CreateParagraphBlock(factory.CreateTextParagraph("Replacement")));
        controls.SetRichTextBlocks(richText, replacementBlocks);

        global::docx.DocxContentControl picture = controls.CreatePictureControl("Picture", "picture", factory.CreateTextRun("old"));
        controls.SetPictureContent(picture, factory.CreateTextRun("new"));

        global::docx.DocxTable outerTable = tables.CreateTable(1, 1);
        global::docx.DocxTableCell cell = outerTable.GetRow(0).GetCell(0);
        global::docx.DocxTable nestedTable = tables.InsertNestedTable(cell, 0, 1, 1);
        tables.InsertParagraphIntoCell(cell, 0, factory.CreateTextParagraph("Moved"));
        tables.MoveCellBlock(cell, 0, 1);

        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxFootnote footnote = notes.CreateFootnoteWithId(document, "9", "Inventory footnote");
        global::docx.DocxField field = fields.CreatePageField("1");
        fields.SetFieldInstruction(field, "NUMPAGES");

        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        using MemoryStream imageStream = new MemoryStream(png);
        global::docx.DocxImage image = media.AddImageStreamToPart(document, "/word/document.xml", imageStream, global::docx.DocxConstants.ContentTypePng);

        global::docx.DocxFooter footer = new global::docx.DocxFooter("<w:ftr xmlns:w=\"" + WordNamespace + "\"><w:p><w:r><w:t>First</w:t></w:r></w:p><w:p><w:r><w:t>Second</w:t></w:r></w:p></w:ftr>");
        global::docx.DocxBlock removedFooterBlock = sections.RemoveFooterBlock(footer, 0);

        Assert.Multiple(() =>
        {
            Assert.That(richText.GetText(), Is.EqualTo("Replacement"));
            Assert.That(picture.GetText(), Is.EqualTo("new"));
            Assert.That(tables.GetCellBlocks(cell)[0].GetTable(), Is.SameAs(nestedTable));
            Assert.That(tables.GetCellBlocks(cell)[1].GetText(), Is.EqualTo("Moved"));
            Assert.That(footnote.Id, Is.EqualTo("9"));
            Assert.That(notes.FindFootnote(document, "9"), Is.SameAs(footnote));
            Assert.That(fields.GetFieldInstruction(field), Is.EqualTo("NUMPAGES"));
            Assert.That(image.SourcePartName, Is.EqualTo("/word/document.xml"));
            Assert.That(document.Package.ContainsPart(image.PartName), Is.True);
            Assert.That(removedFooterBlock.GetText(), Is.EqualTo("First"));
            Assert.That(sections.GetFooterBlocks(footer).Single().GetText(), Is.EqualTo("Second"));
        });
    }

    [Test]
    public void PreviouslyUncoveredInterfaceMethods_ExerciseDocumentedFailurePaths()
    {
        global::docx.IDocxContentControlManagement controls = new global::docx.DocxContentControlManagement();
        global::docx.IDocxTableEditing tables = new global::docx.DocxTableEditing();
        global::docx.IDocxNoteManagement notes = new global::docx.DocxNoteManagement();
        global::docx.IDocxLinkBookmarkFieldManagement fields = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.DocxContentControl plainText = controls.CreatePlainTextControl("Plain", "plain", "value");
        global::docx.DocxTableCell cell = tables.CreateTable(1, 1).GetRow(0).GetCell(0);
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxField field = fields.CreatePageField("1");
        global::docx.DocxFooter footer = new global::docx.DocxFooter("<w:ftr xmlns:w=\"" + WordNamespace + "\" />");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => controls.SetRichTextBlocks(null!, new List<global::docx.DocxBlock>()));
            Assert.Throws<ArgumentException>(() => controls.SetRichTextBlocks(plainText, new List<global::docx.DocxBlock>()));
            Assert.Throws<ArgumentNullException>(() => controls.SetPictureContent(null!, new global::docx.DocxRun("")));
            Assert.Throws<ArgumentException>(() => controls.SetPictureContent(plainText, new global::docx.DocxRun("")));
            Assert.Throws<ArgumentNullException>(() => tables.InsertNestedTable(null!, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => tables.InsertNestedTable(cell, -1, 1, 1));
            Assert.Throws<ArgumentNullException>(() => tables.MoveCellBlock(null!, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => tables.MoveCellBlock(cell, 1, 0));
            Assert.Throws<ArgumentNullException>(() => notes.CreateFootnoteWithId(null!, "1", "text"));
            Assert.Throws<ArgumentException>(() => notes.CreateFootnoteWithId(document, "", "text"));
            Assert.Throws<ArgumentNullException>(() => fields.SetFieldInstruction(null!, "PAGE"));
            Assert.Throws<ArgumentNullException>(() => fields.SetFieldInstruction(field, null!));
            Assert.Throws<ArgumentException>(() => fields.SetFieldInstruction(field, ""));
            Assert.Throws<ArgumentNullException>(() => media.AddImageStreamToPart(null!, "/word/document.xml", new MemoryStream(), global::docx.DocxConstants.ContentTypePng));
            Assert.Throws<ArgumentNullException>(() => media.AddImageStreamToPart(document, "/word/document.xml", null!, global::docx.DocxConstants.ContentTypePng));
            Assert.Throws<ArgumentNullException>(() => sections.RemoveFooterBlock(null!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => sections.RemoveFooterBlock(footer, 0));
        });
    }

    static Type[] PublicApiTypes()
    {
        return typeof(global::docx.DocxDocument).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "docx")
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    static object CreateFieldValue(Type type)
    {
        if (type == typeof(string))
        {
            return "inventory-value";
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(int))
        {
            return 73;
        }

        if (type == typeof(uint))
        {
            return (uint)73;
        }

        if (type == typeof(byte[]))
        {
            return new byte[] { 73 };
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1)!;
        }

        if (type.IsInterface)
        {
            return new global::docx.DocxParagraph("");
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 1);
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type)!;
        }

        ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);

        if (constructor != null)
        {
            return constructor.Invoke(Array.Empty<object>());
        }

        return RuntimeHelpers.GetUninitializedObject(type);
    }

    static string CreateInventoryFingerprint(Type[] types)
    {
        List<string> entries = new List<string>();

        foreach (Type type in types)
        {
            entries.Add("T|" + TypeName(type));

            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(value => MemberSignature(value), StringComparer.Ordinal))
            {
                entries.Add("C|" + TypeName(type) + "|" + MemberSignature(constructor));
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).OrderBy(value => MemberSignature(value), StringComparer.Ordinal))
            {
                entries.Add("M|" + TypeName(type) + "|" + MemberSignature(method));
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(value => !value.IsSpecialName).OrderBy(value => value.Name, StringComparer.Ordinal))
            {
                string entry = "F|" + TypeName(type) + "|" + TypeName(field.FieldType) + "|" + field.Name;

                if (field.IsLiteral)
                {
                    entry = entry + "|" + Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture);
                }

                entries.Add(entry);
            }
        }

        byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\n", entries));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    static string MemberSignature(MethodBase method)
    {
        string parameters = string.Join(",", method.GetParameters().Select(parameter => TypeName(parameter.ParameterType)));
        MethodInfo? ordinaryMethod = method as MethodInfo;
        string returnType = "";

        if (ordinaryMethod != null)
        {
            returnType = TypeName(ordinaryMethod.ReturnType) + "|";
        }

        return returnType + method.Name + "(" + parameters + ")";
    }

    static string TypeName(Type type)
    {
        if (type.IsArray)
        {
            return TypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericType)
        {
            string name = type.GetGenericTypeDefinition().FullName!;
            int tickIndex = name.IndexOf('`');

            if (tickIndex >= 0)
            {
                name = name.Substring(0, tickIndex);
            }

            return name + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
        }

        return type.FullName ?? type.Name;
    }
}
