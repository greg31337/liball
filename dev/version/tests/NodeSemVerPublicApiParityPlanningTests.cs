using System.Reflection;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerPublicApiParityPlanningTests
{
    [Test]
    public void ParityPlan_CoversEveryTask101ApiGroup()
    {
        NodeSemVerParityApiRow[] rows = NodeSemVerParityApiPlan.CreateRows();
        string[] requiredCategories = new string[]
        {
            "valid",
            "parse",
            "clean",
            "coerce",
            "comparison helpers",
            "range helpers",
            "candidate selectors",
            "sorting helpers",
            "version increment helpers"
        };

        int index = 0;
        while (index < requiredCategories.Length)
        {
            Assert.That(ContainsCategory(rows, requiredCategories[index]), Is.True, "Missing node-semver parity category: " + requiredCategories[index]);
            index = index + 1;
        }
    }

    [Test]
    public void ParityPlan_RowsDescribeNpmApiFacadeEntryExpectedBehaviorAndSyntaxNotes()
    {
        NodeSemVerParityApiRow[] rows = NodeSemVerParityApiPlan.CreateRows();
        int index = 0;

        while (index < rows.Length)
        {
            NodeSemVerParityApiRow row = rows[index];

            Assert.That(row.GetCategory(), Is.Not.Empty);
            Assert.That(row.GetNpmApiName(), Is.Not.Empty, row.GetCategory());
            Assert.That(row.GetFacadeEntry(), Is.Not.Empty, row.GetNpmApiName());
            Assert.That(row.GetExpectedBehavior(), Is.Not.Empty, row.GetNpmApiName());
            Assert.That(row.GetSyntaxNotes(), Does.Contain("syntax.md"), row.GetNpmApiName());
            Assert.That(row.GetSyntaxNotes(), Does.Contain("explicit"), row.GetNpmApiName());

            index = index + 1;
        }
    }

    [Test]
    public void ParityPlan_CurrentFacadeEntriesExist()
    {
        NodeSemVerParityApiRow[] rows = NodeSemVerParityApiPlan.CreateRows();
        int index = 0;

        while (index < rows.Length)
        {
            if (rows[index].IsCurrent())
            {
                AssertCurrentApiEntryExists(rows[index]);
            }

            index = index + 1;
        }
    }

    [Test]
    public void ParityPlan_PlannedEntriesReferenceImplementationTasks()
    {
        string todoText = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "todo.md"));
        NodeSemVerParityApiRow[] rows = NodeSemVerParityApiPlan.CreateRows();
        int index = 0;

        while (index < rows.Length)
        {
            if (!rows[index].IsCurrent())
            {
                string[] taskIds = rows[index].GetTaskIds();
                Assert.That(taskIds.Length, Is.GreaterThan(0), rows[index].GetFacadeEntry());

                int taskIndex = 0;
                while (taskIndex < taskIds.Length)
                {
                    Assert.That(todoText, Does.Contain(taskIds[taskIndex] + ":"), rows[index].GetFacadeEntry());
                    taskIndex = taskIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    [Test]
    public void ParityPlan_UsesFacadeMethodsInsteadOfOptionalParameterVariants()
    {
        NodeSemVerParityApiRow[] rows = NodeSemVerParityApiPlan.CreateRows();
        int index = 0;

        while (index < rows.Length)
        {
            Assert.That(rows[index].GetFacadeEntry(), Does.Not.Contain("="), rows[index].GetFacadeEntry());
            Assert.That(rows[index].GetSyntaxNotes(), Does.Contain("no optional parameters"), rows[index].GetFacadeEntry());
            Assert.That(rows[index].GetSyntaxNotes(), Does.Contain("no named arguments"), rows[index].GetFacadeEntry());
            index = index + 1;
        }
    }

    static void AssertCurrentApiEntryExists(NodeSemVerParityApiRow row)
    {
        Type? type = typeof(NodeSemVer).Assembly.GetType("version." + row.GetTypeName());
        Assert.That(type, Is.Not.Null, row.GetFacadeEntry());

        if (row.GetMemberName().Length == 0)
        {
            return;
        }

        MethodInfo[] methods = type!.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        bool found = false;
        int methodIndex = 0;

        while (methodIndex < methods.Length)
        {
            if (methods[methodIndex].Name == row.GetMemberName())
            {
                found = true;
                break;
            }

            methodIndex = methodIndex + 1;
        }

        Assert.That(found, Is.True, row.GetFacadeEntry());
    }

    static bool ContainsCategory(NodeSemVerParityApiRow[] rows, string category)
    {
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index].GetCategory() == category)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "version.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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

public static class NodeSemVerParityApiPlan
{
    public static NodeSemVerParityApiRow[] CreateRows()
    {
        return new NodeSemVerParityApiRow[]
        {
            NodeSemVerParityApiRow.Planned(
                "valid",
                "valid",
                "NodeSemVer.Valid",
                "Return normalized strict SemVer text for valid input and a null-equivalent result for invalid input.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Current(
                "parse",
                "parse",
                "NodeSemVer.Parse",
                "NodeSemVer",
                "Parse",
                "Return a VersionParseResult for node-compatible loose SemVer text without throwing for normal parse failures.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Planned(
                "clean",
                "clean",
                "NodeSemVer.Clean",
                "Trim whitespace, remove accepted leading v or = markers in the selected mode, preserve prerelease/build metadata, and report invalid input diagnostics.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Current(
                "coerce",
                "coerce",
                "NodeSemVer.Coerce",
                "NodeSemVer",
                "Coerce",
                "Extract the first coercible SemVer tuple from surrounding text and return a VersionParseResult.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Planned(
                "comparison helpers",
                "eq/neq/gt/gte/lt/lte/cmp/compare/rcompare/compareLoose/compareBuild",
                "NodeSemVer.Eq/Neq/Gt/Gte/Lt/Lte/Cmp/Compare/RCompare/CompareLoose/CompareBuild",
                "Expose npm-compatible primitive comparison helpers over existing SemVer precedence, loose parsing, reverse comparison, and build-aware identity comparison.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Planned(
                "range helpers",
                "satisfies/toComparators/minVersion/validRange/outside/gtr/ltr/intersects/subset/simplifyRange",
                "NodeSemVer.Satisfies/ToComparators/MinVersion/ValidRange/Outside/Gtr/Ltr/Intersects/Subset/SimplifyRange",
                "Expose range helpers through VersionRange, VersionComparatorSet, and range algebra without changing existing range semantics.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Planned(
                "candidate selectors",
                "minSatisfying/maxSatisfying",
                "NodeSemVer.MinSatisfying/MaxSatisfying",
                "Select minimum and maximum satisfying versions from finite candidate sets using node-semver range parsing and prerelease policy.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Current(
                "candidate selectors",
                "filter/minSatisfying/maxSatisfying/bestSatisfying",
                "VersionSelector.Filter/MinSatisfying/MaxSatisfying/BestSatisfying",
                "VersionSelector",
                "Filter",
                "Provide current candidate filtering and selection over parsed VersionRange and UniversalVersion arrays.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Planned(
                "sorting helpers",
                "sort/rsort",
                "NodeSemVer.Sort/RSort",
                "Sort version text under node-semver precedence rules and expose reverse sort as a separate explicit facade method.",
                new string[] { "T015", "T016" }),

            NodeSemVerParityApiRow.Planned(
                "version increment helpers",
                "inc/major/minor/patch/premajor/preminor/prepatch/prerelease/release",
                "NodeSemVer.Increment/Major/Minor/Patch/Premajor/Preminor/Prepatch/Prerelease/Release",
                "Increment versions with explicit option classes for release type, prerelease identifier, identifier base, loose mode, and prerelease inclusion.",
                new string[] { "T015", "T016" })
        };
    }
}

public class NodeSemVerParityApiRow
{
    string category;
    string npmApiName;
    string facadeEntry;
    string typeName;
    string memberName;
    bool current;
    string expectedBehavior;
    string[] taskIds;
    string syntaxNotes;

    NodeSemVerParityApiRow(
        string categoryValue,
        string npmApiNameValue,
        string facadeEntryValue,
        string typeNameValue,
        string memberNameValue,
        bool currentValue,
        string expectedBehaviorValue,
        string[] taskIdValues)
    {
        category = categoryValue;
        npmApiName = npmApiNameValue;
        facadeEntry = facadeEntryValue;
        typeName = typeNameValue;
        memberName = memberNameValue;
        current = currentValue;
        expectedBehavior = expectedBehaviorValue;
        taskIds = taskIdValues;
        syntaxNotes = "Use syntax.md-compatible classes, enums, explicit methods, explicit overloads, no optional parameters, no named arguments, no LINQ, and only built-in .NET APIs in production source.";
    }

    public static NodeSemVerParityApiRow Current(
        string category,
        string npmApiName,
        string facadeEntry,
        string typeName,
        string memberName,
        string expectedBehavior,
        string[] taskIds)
    {
        return new NodeSemVerParityApiRow(category, npmApiName, facadeEntry, typeName, memberName, true, expectedBehavior, taskIds);
    }

    public static NodeSemVerParityApiRow Planned(
        string category,
        string npmApiName,
        string facadeEntry,
        string expectedBehavior,
        string[] taskIds)
    {
        return new NodeSemVerParityApiRow(category, npmApiName, facadeEntry, "", "", false, expectedBehavior, taskIds);
    }

    public string GetCategory()
    {
        return category;
    }

    public string GetNpmApiName()
    {
        return npmApiName;
    }

    public string GetFacadeEntry()
    {
        return facadeEntry;
    }

    public string GetTypeName()
    {
        return typeName;
    }

    public string GetMemberName()
    {
        return memberName;
    }

    public bool IsCurrent()
    {
        return current;
    }

    public string GetExpectedBehavior()
    {
        return expectedBehavior;
    }

    public string[] GetTaskIds()
    {
        return taskIds;
    }

    public string GetSyntaxNotes()
    {
        return syntaxNotes;
    }
}
