using System.Xml.Linq;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PackageMetadataTests
{
    [Test]
    public void LibraryProject_DefinesPackageMetadata()
    {
        XDocument project = ReadLibraryProject();

        AssertElementValue(project, "IsPackable", "true");
        AssertElementValue(project, "PackageId", "template");
        AssertElementValue(project, "Version", "0.1.0");
        AssertElementValue(project, "Authors", "template contributors");
        AssertElementValue(project, "PackageReadmeFile", "README.md");
        Assert.That(GetElementValue(project, "Description"), Does.Contain(".NET template engine"));
        Assert.That(GetElementValue(project, "PackageTags"), Does.Contain("mustache"));
    }

    [Test]
    public void LibraryProject_PacksReadmeAtPackageRoot()
    {
        XDocument project = ReadLibraryProject();
        XElement? itemGroup = FindReadmeItemGroup(project);

        Assert.That(itemGroup, Is.Not.Null);
        XElement readme = itemGroup!.Element("None")!;
        Assert.That(readme.Attribute("Include")!.Value, Is.EqualTo("..\\README.md"));
        Assert.That(readme.Attribute("Pack")!.Value, Is.EqualTo("true"));
        Assert.That(readme.Attribute("PackagePath")!.Value, Is.EqualTo("\\"));
    }

    [Test]
    public void TestProject_ExcludesNestedBuildOutputSources()
    {
        XDocument project = ReadTestProject();
        List<string> compileRemoves = GetCompileRemoves(project);

        Assert.That(compileRemoves, Does.Contain("**\\bin\\**\\*.cs"));
        Assert.That(compileRemoves, Does.Contain("**\\obj\\**\\*.cs"));
    }

    static XDocument ReadLibraryProject()
    {
        string root = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(root, "src", "template.csproj"));
    }

    static XDocument ReadTestProject()
    {
        string root = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(root, "tests", "template.Tests.csproj"));
    }

    static XElement? FindReadmeItemGroup(XDocument project)
    {
        foreach (XElement itemGroup in project.Root!.Elements("ItemGroup"))
        {
            XElement? none = itemGroup.Element("None");
            if (none != null && none.Attribute("Include") != null && none.Attribute("Include")!.Value == "..\\README.md")
            {
                return itemGroup;
            }
        }

        return null;
    }

    static void AssertElementValue(XDocument project, string name, string expected)
    {
        Assert.That(GetElementValue(project, name), Is.EqualTo(expected));
    }

    static string GetElementValue(XDocument project, string name)
    {
        foreach (XElement propertyGroup in project.Root!.Elements("PropertyGroup"))
        {
            XElement? element = propertyGroup.Element(name);
            if (element != null)
            {
                return element.Value;
            }
        }

        return "";
    }

    static List<string> GetCompileRemoves(XDocument project)
    {
        List<string> removes = new List<string>();
        foreach (XElement itemGroup in project.Root!.Elements("ItemGroup"))
        {
            foreach (XElement compile in itemGroup.Elements("Compile"))
            {
                XAttribute? remove = compile.Attribute("Remove");
                if (remove != null)
                {
                    removes.Add(remove.Value);
                }
            }
        }

        return removes;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "template.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
