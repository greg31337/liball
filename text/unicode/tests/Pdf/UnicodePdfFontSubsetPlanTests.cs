using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfFontSubsetPlanTests
{
    [Test]
    public void CreateSubsetPlan_MovesWholeUncoveredClustersToFallback()
    {
        IUnicodePdfFontCoverage coverage = new TestCoverage(
            new int[] { 0x0041, 0x0370 });

        UnicodePdfFontSubsetPlan plan = new UnicodePdfFontSubset().CreateSubsetPlan(
            "A\u0301\u0370",
            coverage);

        Assert.That(plan.HasFullCoverage, Is.False);
        Assert.That(plan.RequiredScalars, Is.EqualTo(new int[] { 0x0041, 0x0301, 0x0370 }));
        Assert.That(plan.CoveredScalars, Is.EqualTo(new int[] { 0x0370 }));
        Assert.That(plan.MissingScalars, Is.EqualTo(new int[] { 0x0041, 0x0301 }));

        Assert.That(plan.Clusters, Has.Length.EqualTo(2));
        AssertCluster(plan.Clusters[0], 0, 2, UnicodeScript.Latin, false, new int[] { 0x0041, 0x0301 });
        AssertCluster(plan.Clusters[1], 2, 3, UnicodeScript.Greek, true, new int[] { 0x0370 });

        Assert.That(plan.ScriptPlans, Has.Length.EqualTo(2));
        AssertScriptPlan(
            plan.ScriptPlans[0],
            UnicodeScript.Latin,
            false,
            new int[] { 0x0041, 0x0301 },
            new int[] { },
            new int[] { 0x0041, 0x0301 });
        AssertScriptPlan(
            plan.ScriptPlans[1],
            UnicodeScript.Greek,
            true,
            new int[] { 0x0370 },
            new int[] { 0x0370 },
            new int[] { });
    }

    [Test]
    public void CreateSubsetPlan_ReturnsFullCoverageAndEmptyPlansForEmptyText()
    {
        IUnicodePdfFontCoverage coverage = new TestCoverage(new int[] { 0x0041, 0x0301 });

        UnicodePdfFontSubsetPlan covered = new UnicodePdfFontSubset().CreateSubsetPlan("A\u0301", coverage);
        UnicodePdfFontSubsetPlan empty = new UnicodePdfFontSubset().CreateSubsetPlan("", coverage);

        Assert.That(covered.HasFullCoverage, Is.True);
        Assert.That(covered.CoveredScalars, Is.EqualTo(new int[] { 0x0041, 0x0301 }));
        Assert.That(covered.MissingScalars, Is.Empty);
        Assert.That(covered.Clusters, Has.Length.EqualTo(1));
        Assert.That(covered.Clusters[0].IsCovered, Is.True);

        Assert.That(empty.HasFullCoverage, Is.True);
        Assert.That(empty.RequiredScalars, Is.Empty);
        Assert.That(empty.ScriptPlans, Is.Empty);
        Assert.That(empty.Clusters, Is.Empty);
    }

    [Test]
    public void CreateSubsetPlan_UsesReplacementScalarForMalformedUtf16()
    {
        IUnicodePdfFontCoverage coverage = new TestCoverage(new int[] { UnicodeScalar.ReplacementCharacter });

        UnicodePdfFontSubsetPlan plan = new UnicodePdfFontSubset().CreateSubsetPlan("\uD800", coverage);

        Assert.That(plan.HasFullCoverage, Is.True);
        Assert.That(plan.RequiredScalars, Is.EqualTo(new int[] { UnicodeScalar.ReplacementCharacter }));
        Assert.That(plan.Clusters[0].Scalars, Is.EqualTo(new int[] { UnicodeScalar.ReplacementCharacter }));
    }

    [Test]
    public void CreateSubsetPlan_SeparatesCoverageGapsAcrossFallbackScripts()
    {
        IUnicodePdfFontCoverage coverage = new TestCoverage(new int[]
        {
            0x0041,
            0x0370,
            0x05D0,
        });

        UnicodePdfFontSubsetPlan plan = new UnicodePdfFontSubset().CreateSubsetPlan(
            "A\u0370\u0301\u05D0",
            coverage);

        Assert.That(plan.HasFullCoverage, Is.False);
        Assert.That(plan.RequiredScalars, Is.EqualTo(new int[] { 0x0041, 0x0301, 0x0370, 0x05D0 }));
        Assert.That(plan.CoveredScalars, Is.EqualTo(new int[] { 0x0041, 0x05D0 }));
        Assert.That(plan.MissingScalars, Is.EqualTo(new int[] { 0x0301, 0x0370 }));
        Assert.That(plan.ScriptPlans, Has.Length.EqualTo(3));
        AssertScriptPlan(
            plan.ScriptPlans[0],
            UnicodeScript.Latin,
            true,
            new int[] { 0x0041 },
            new int[] { 0x0041 },
            new int[] { });
        AssertScriptPlan(
            plan.ScriptPlans[1],
            UnicodeScript.Greek,
            false,
            new int[] { 0x0301, 0x0370 },
            new int[] { },
            new int[] { 0x0301, 0x0370 });
        AssertScriptPlan(
            plan.ScriptPlans[2],
            UnicodeScript.Hebrew,
            true,
            new int[] { 0x05D0 },
            new int[] { 0x05D0 },
            new int[] { });
    }

    [Test]
    public void CreateSubsetPlan_ResolvesCommonAndInheritedScalarsToNearbyScripts()
    {
        IUnicodePdfFontCoverage coverage = new TestCoverage(new int[]
        {
            0x0020,
            0x0028,
            0x0029,
            0x0041,
            0x0301,
            0x0370,
        });

        UnicodePdfFontSubsetPlan plan = new UnicodePdfFontSubset().CreateSubsetPlan(
            "(\u0301A) \u0370\u0301",
            coverage);

        Assert.That(plan.ScriptPlans, Has.Length.EqualTo(2));
        AssertScriptPlan(
            plan.ScriptPlans[0],
            UnicodeScript.Latin,
            true,
            new int[] { 0x0020, 0x0028, 0x0029, 0x0041, 0x0301 },
            new int[] { 0x0020, 0x0028, 0x0029, 0x0041, 0x0301 },
            new int[] { });
        AssertScriptPlan(
            plan.ScriptPlans[1],
            UnicodeScript.Greek,
            true,
            new int[] { 0x0301, 0x0370 },
            new int[] { 0x0301, 0x0370 },
            new int[] { });
        Assert.That(plan.Clusters[0].Script, Is.EqualTo(UnicodeScript.Latin));
        Assert.That(plan.Clusters[^1].Script, Is.EqualTo(UnicodeScript.Greek));
    }

    [Test]
    public void CreateSubsetPlan_KeepsEmojiZwjSequenceTogetherWhenCoverageIsIncomplete()
    {
        IUnicodePdfFontCoverage coverage = new TestCoverage(new int[] { 0x200D, 0x1F469 });

        UnicodePdfFontSubsetPlan plan = new UnicodePdfFontSubset().CreateSubsetPlan(
            "\U0001F469\u200D\U0001F4BB",
            coverage);

        Assert.That(plan.Clusters, Has.Length.EqualTo(1));
        AssertCluster(
            plan.Clusters[0],
            0,
            5,
            UnicodeScript.Common,
            false,
            new int[] { 0x200D, 0x1F469, 0x1F4BB });
        Assert.That(plan.CoveredScalars, Is.Empty);
        Assert.That(plan.MissingScalars, Is.EqualTo(new int[] { 0x200D, 0x1F469, 0x1F4BB }));
        Assert.That(plan.ScriptPlans, Has.Length.EqualTo(1));
        Assert.That(plan.ScriptPlans[0].Script, Is.EqualTo(UnicodeScript.Common));
        Assert.That(plan.ScriptPlans[0].HasFullCoverage, Is.False);
    }

    [Test]
    public void SubsetPlanning_RejectsNullInputsAndDefensivelyCopiesResultArrays()
    {
        UnicodePdfFontSubset subset = new UnicodePdfFontSubset();
        IUnicodePdfFontCoverage coverage = new TestCoverage(new int[] { 0x0041 });
        UnicodePdfFontSubsetPlan plan = subset.CreateSubsetPlan("A", coverage);

        int[] required = plan.RequiredScalars;
        required[0] = 0x005A;
        UnicodePdfFontSubsetPlan second = subset.CreateSubsetPlan("A", coverage);

        Assert.That(second.RequiredScalars, Is.EqualTo(new int[] { 0x0041 }));
        Assert.Throws<ArgumentNullException>(() => subset.CreateSubsetPlan(null!, coverage));
        Assert.Throws<ArgumentNullException>(() => subset.CreateSubsetPlan("A", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfClusterSubsetPlan(
            -1,
            0,
            UnicodeScript.Latin,
            new int[] { 0x0041 },
            true));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfScriptSubsetPlan(
            UnicodeScript.Latin,
            null!,
            new int[] { },
            new int[] { }));
    }

    static void AssertCluster(
        UnicodePdfClusterSubsetPlan cluster,
        int start,
        int end,
        UnicodeScript script,
        bool covered,
        int[] scalars)
    {
        Assert.That(cluster.StartUtf16Index, Is.EqualTo(start));
        Assert.That(cluster.EndUtf16Index, Is.EqualTo(end));
        Assert.That(cluster.Utf16Length, Is.EqualTo(end - start));
        Assert.That(cluster.Script, Is.EqualTo(script));
        Assert.That(cluster.IsCovered, Is.EqualTo(covered));
        Assert.That(cluster.Scalars, Is.EqualTo(scalars));
    }

    static void AssertScriptPlan(
        UnicodePdfScriptSubsetPlan plan,
        UnicodeScript script,
        bool fullCoverage,
        int[] required,
        int[] covered,
        int[] missing)
    {
        Assert.That(plan.Script, Is.EqualTo(script));
        Assert.That(plan.HasFullCoverage, Is.EqualTo(fullCoverage));
        Assert.That(plan.RequiredScalars, Is.EqualTo(required));
        Assert.That(plan.CoveredScalars, Is.EqualTo(covered));
        Assert.That(plan.MissingScalars, Is.EqualTo(missing));
    }

    class TestCoverage : IUnicodePdfFontCoverage
    {
        int[] coveredScalars;

        public TestCoverage(int[] coveredScalars)
        {
            this.coveredScalars = coveredScalars;
        }

        public bool CoversScalar(int codePoint)
        {
            int index = 0;
            while (index < coveredScalars.Length)
            {
                if (coveredScalars[index] == codePoint)
                {
                    return true;
                }

                index = index + 1;
            }

            return false;
        }
    }
}
