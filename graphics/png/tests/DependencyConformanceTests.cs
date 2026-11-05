using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class DependencyConformanceTests
{
    [Test]
    public void LibraryProject_DoesNotDeclareExternalRuntimeDependencies()
    {
        string projectPath = Path.Combine(FindRepositoryRoot(), "src", "png.csproj");
        XDocument project = XDocument.Load(projectPath);

        Assert.That(FindItems(project, "PackageReference"), Is.Empty);
        Assert.That(FindItems(project, "PackageDownload"), Is.Empty);
        Assert.That(FindItems(project, "PackageVersion"), Is.Empty);
        Assert.That(FindItems(project, "ProjectReference"), Is.Empty);
        Assert.That(FindItems(project, "Reference"), Is.Empty);
        Assert.That(FindItems(project, "FrameworkReference"), Is.Empty);
    }

    [Test]
    public void LibraryAssembly_ReferencesOnlyFrameworkAssemblies()
    {
        AssemblyName[] references = typeof(Png).Assembly.GetReferencedAssemblies();

        int index = 0;
        while (index < references.Length)
        {
            string name = references[index].Name ?? "";
            Assert.That(IsAllowedFrameworkAssembly(name), Is.True, name);
            index += 1;
        }
    }

    static XElement[] FindItems(XDocument document, string itemName)
    {
        return document.Descendants()
            .Where(element => element.Name.LocalName == itemName)
            .ToArray();
    }

    static bool IsAllowedFrameworkAssembly(string assemblyName)
    {
        return assemblyName == "mscorlib"
            || assemblyName == "netstandard"
            || assemblyName.StartsWith("System", StringComparison.Ordinal)
            || assemblyName.StartsWith("Microsoft", StringComparison.Ordinal);
    }

    static string FindRepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "png.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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
