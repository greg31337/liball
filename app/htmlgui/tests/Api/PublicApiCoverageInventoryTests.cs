using System.Reflection;
using System.Text.RegularExpressions;
using htmlgui;

namespace htmlgui.Tests;

public class PublicApiCoverageInventoryTests
{
    [Test]
    public void EveryExportedApiMemberMapsToFunctionalTestEvidence()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] testFiles = Directory.GetFiles(Path.Combine(repositoryRoot, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path) != "PublicApiCoverageInventoryTests.cs")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Dictionary<string, string[]> coverage = new Dictionary<string, string[]>();
        List<string> uncovered = new List<string>();
        Type[] apiTypes = typeof(UiApplication).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "htmlgui")
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (Type type in apiTypes)
        {
            InventoryType(type, testFiles, coverage, uncovered);
        }

        Assert.Multiple(() =>
        {
            Assert.That(apiTypes, Is.Not.Empty);
            Assert.That(coverage, Is.Not.Empty);
            Assert.That(uncovered, Is.Empty, string.Join(Environment.NewLine, uncovered));
            Assert.That(coverage.Values, Is.All.Not.Empty);
        });
    }

    private static void InventoryType(Type type, string[] testFiles, Dictionary<string, string[]> coverage, List<string> uncovered)
    {
        string typeKey = "type:" + type.FullName;
        if (type.IsEnum)
        {
            string[] evidence = FindEvidence(testFiles, "AssertMembers\\s*<\\s*" + Regex.Escape(type.Name) + "\\s*>");
            MapEvidence(typeKey, evidence, coverage, uncovered);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                MapEvidence(MemberKey(type, field), evidence, coverage, uncovered);
            }

            return;
        }

        if (type.IsAbstract && type.IsSealed)
        {
            string[] catalogEvidence = FindEvidence(testFiles, "typeof\\s*\\(\\s*" + Regex.Escape(type.Name) + "\\s*\\)");
            string[] typeEvidence = catalogEvidence.Length > 0
                ? catalogEvidence
                : FindEvidence(testFiles, Regex.Escape(type.Name) + "\\.");
            MapEvidence(typeKey, typeEvidence, coverage, uncovered);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                string[] fieldEvidence = catalogEvidence.Length > 0
                    ? catalogEvidence
                    : FindEvidence(testFiles, Regex.Escape(type.Name) + "\\." + Regex.Escape(field.Name) + "\\b");
                MapEvidence(MemberKey(type, field), fieldEvidence, coverage, uncovered);
            }

            return;
        }

        if (typeof(Exception).IsAssignableFrom(type))
        {
            string[] typeEvidence = FindEvidence(testFiles, "new\\s+" + Regex.Escape(type.Name) + "\\s*\\(");
            MapEvidence(typeKey, typeEvidence, coverage, uncovered);
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                MapEvidence(ConstructorKey(type, constructor), typeEvidence, coverage, uncovered);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                string[] fieldEvidence = FindEvidence(testFiles, "\\." + Regex.Escape(field.Name) + "\\b");
                MapEvidence(MemberKey(type, field), fieldEvidence, coverage, uncovered);
            }

            return;
        }

        if (type.IsInterface)
        {
            AddCoverage(coverage, typeKey, "interface contract tests");
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                MapInvocation(type, method, testFiles, coverage, uncovered);
            }

            return;
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();
        if (fields.Length > 0 && methods.Length == 0)
        {
            string[] evidence = FindEvidence(testFiles, "new\\s+" + Regex.Escape(type.Name) + "\\s*\\(\\s*\\)")
                .Where(name => name == "PublicDataModelCoverageTests")
                .ToArray();
            MapEvidence(typeKey, evidence, coverage, uncovered);
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                MapEvidence(ConstructorKey(type, constructor), evidence, coverage, uncovered);
            }

            foreach (FieldInfo field in fields)
            {
                MapEvidence(MemberKey(type, field), evidence, coverage, uncovered);
            }

            return;
        }

        AddCoverage(coverage, typeKey, "implementation contract tests");
        foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            string[] evidence = FindEvidence(testFiles, "new\\s+" + Regex.Escape(type.Name) + "\\s*\\(");
            MapEvidence(ConstructorKey(type, constructor), evidence, coverage, uncovered);
        }

        foreach (MethodInfo method in methods)
        {
            MapInvocation(type, method, testFiles, coverage, uncovered);
        }

        foreach (FieldInfo field in fields)
        {
            uncovered.Add(MemberKey(type, field));
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            uncovered.Add(MemberKey(type, property));
        }

        foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            uncovered.Add(MemberKey(type, eventInfo));
        }
    }

    private static void MapInvocation(Type type, MethodInfo method, string[] testFiles, Dictionary<string, string[]> coverage, List<string> uncovered)
    {
        string[] evidence = FindEvidence(testFiles, "\\." + Regex.Escape(method.Name) + "\\s*\\(");
        MapEvidence(MemberKey(type, method), evidence, coverage, uncovered);
    }

    private static string[] FindEvidence(string[] testFiles, string pattern)
    {
        Regex expression = new Regex(pattern, RegexOptions.CultureInvariant);
        return testFiles
            .Where(path => expression.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetFileNameWithoutExtension(path) ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static void MapEvidence(string key, string[] evidence, Dictionary<string, string[]> coverage, List<string> uncovered)
    {
        if (evidence.Length == 0)
        {
            uncovered.Add(key);
            return;
        }

        coverage.Add(key, evidence);
    }

    private static void AddCoverage(Dictionary<string, string[]> coverage, string key, params string[] evidence)
    {
        coverage.Add(key, evidence);
    }

    private static string MemberKey(Type type, MemberInfo member)
    {
        return "member:" + type.FullName + "." + member.Name;
    }

    private static string ConstructorKey(Type type, ConstructorInfo constructor)
    {
        return "constructor:" + type.FullName + "(" + string.Join(",", constructor.GetParameters().Select(parameter => parameter.ParameterType.Name)) + ")";
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "Api"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests", "Api")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The repository root could not be located from the test output directory.");
    }
}
