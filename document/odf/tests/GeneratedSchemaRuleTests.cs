namespace odf.Tests;

public class GeneratedSchemaRuleTests
{
    static readonly OdfVersion[] Versions =
    [
        OdfVersion.Version10,
        OdfVersion.Version11,
        OdfVersion.Version12,
        OdfVersion.Version13,
        OdfVersion.Version14
    ];

    static IEnumerable<TestCaseData> EveryRuleSet()
    {
        foreach (OdfVersion version in Versions)
        {
            foreach (SchemaRuleSet set in OdfSchemaRuleCatalog.GetVersionRuleSets(
                version))
            {
                yield return new TestCaseData(version, set.Profile)
                    .SetName("SchemaSet_" + version + "_" + set.Profile);
            }
        }
    }

    static IEnumerable<TestCaseData> EveryElementRule()
    {
        foreach (OdfVersion version in Versions)
        {
            foreach (SchemaRuleSet set in OdfSchemaRuleCatalog.GetVersionRuleSets(
                version))
            {
                int index = 0;
                while (index < set.Elements.Length)
                {
                    yield return new TestCaseData(version, set.Profile, index)
                        .SetName("SchemaElement_"
                            + version
                            + "_"
                            + set.Profile
                            + "_"
                            + index);
                    index++;
                }
            }
        }
    }

    static IEnumerable<TestCaseData> EveryAttributeRule()
    {
        foreach (OdfVersion version in Versions)
        {
            foreach (SchemaRuleSet set in OdfSchemaRuleCatalog.GetVersionRuleSets(
                version))
            {
                int index = 0;
                while (index < set.Attributes.Length)
                {
                    yield return new TestCaseData(version, set.Profile, index)
                        .SetName("SchemaAttribute_"
                            + version
                            + "_"
                            + set.Profile
                            + "_"
                            + index);
                    index++;
                }
            }
        }
    }

    static IEnumerable<TestCaseData> EveryDatatypeRule()
    {
        foreach (OdfVersion version in Versions)
        {
            foreach (SchemaRuleSet set in OdfSchemaRuleCatalog.GetVersionRuleSets(
                version))
            {
                int index = 0;
                while (index < set.Datatypes.Length)
                {
                    yield return new TestCaseData(version, set.Profile, index)
                        .SetName("SchemaDatatype_"
                            + version
                            + "_"
                            + set.Profile
                            + "_"
                            + index);
                    index++;
                }
            }
        }
    }

    static IEnumerable<TestCaseData> EveryOccurrenceRule()
    {
        foreach (OdfVersion version in Versions)
        {
            foreach (SchemaRuleSet set in OdfSchemaRuleCatalog.GetVersionRuleSets(
                version))
            {
                int index = 0;
                while (index < set.Occurrences.Length)
                {
                    yield return new TestCaseData(version, set.Profile, index)
                        .SetName("SchemaOccurrence_"
                            + version
                            + "_"
                            + set.Profile
                            + "_"
                            + index);
                    index++;
                }
            }
        }
    }

    [TestCaseSource(nameof(EveryRuleSet))]
    public void BundledRuleSetMetadataAndGraphAreComplete(
        OdfVersion version,
        string profile)
    {
        SchemaRuleSet set = RequiredSet(version, profile);

        Assert.Multiple(() =>
        {
            Assert.That(set.Version, Is.EqualTo(version));
            Assert.That(set.Profile, Is.EqualTo(profile));
            Assert.That(set.SourceName, Is.Not.Empty);
            Assert.That(set.Rules, Is.Not.Empty);
            Assert.That(set.Patterns, Is.Not.Empty);
            Assert.That(
                set.Rules.Any(rule => rule.IsStart)
                    || set.Patterns.Any(pattern =>
                        pattern.Kind == SchemaPatternKind.Include),
                Is.True);
        });

        int ruleIndex = 0;
        while (ruleIndex < set.Rules.Length)
        {
            AssertPatternIndex(set, set.Rules[ruleIndex].PatternIndex);
            ruleIndex++;
        }
        int patternIndex = 0;
        while (patternIndex < set.Patterns.Length)
        {
            SchemaPattern pattern = set.Patterns[patternIndex];
            Assert.That(pattern.OwnerRuleIndex,
                Is.InRange(0, set.Rules.Length - 1));
            Assert.That(pattern.FirstChildIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(pattern.ChildCount, Is.GreaterThanOrEqualTo(0));
            Assert.That(pattern.FirstChildIndex + pattern.ChildCount,
                Is.LessThanOrEqualTo(set.PatternChildIndexes.Length));
            patternIndex++;
        }
        int childIndex = 0;
        while (childIndex < set.PatternChildIndexes.Length)
        {
            AssertPatternIndex(set, set.PatternChildIndexes[childIndex]);
            childIndex++;
        }
    }

    [TestCaseSource(nameof(EveryElementRule))]
    public void EveryElementRuleMatchesItsPatternGraph(
        OdfVersion version,
        string profile,
        int elementIndex)
    {
        SchemaRuleSet set = RequiredSet(version, profile);
        SchemaElementRule rule = set.Elements[elementIndex];
        SchemaPattern pattern = set.Patterns[rule.PatternIndex];
        SchemaPattern? namePattern = DirectNamePattern(set, pattern);
        int[] expectedContent = ExpectedContentPatterns(set, pattern);
        SchemaRuleIndex index = new SchemaRuleIndex(set);

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Kind, Is.EqualTo(SchemaPatternKind.Element));
            Assert.That(rule.OwnerRuleIndex,
                Is.InRange(0, set.Rules.Length - 1));
            Assert.That(rule.OwnerRuleIndex, Is.EqualTo(pattern.OwnerRuleIndex));
            Assert.That(rule.LocalName,
                Is.EqualTo(pattern.Name.Length > 0
                    ? pattern.Name
                    : namePattern?.Name ?? ""));
            Assert.That(rule.NamespaceName,
                Is.EqualTo(pattern.Name.Length > 0
                    ? pattern.NamespaceName
                    : namePattern?.NamespaceName ?? pattern.NamespaceName));
            Assert.That(rule.ContentPatternIndexes, Is.EqualTo(expectedContent));
            Assert.That(index.FindElementRules(
                rule.NamespaceName,
                rule.LocalName), Does.Contain(rule));
        });
        AssertIndexes(set, rule.ContentPatternIndexes);
    }

    [TestCaseSource(nameof(EveryAttributeRule))]
    public void EveryAttributeRuleMatchesItsPatternGraph(
        OdfVersion version,
        string profile,
        int attributeIndex)
    {
        SchemaRuleSet set = RequiredSet(version, profile);
        SchemaAttributeRule rule = set.Attributes[attributeIndex];
        SchemaPattern pattern = set.Patterns[rule.PatternIndex];
        SchemaPattern? namePattern = DirectNamePattern(set, pattern);
        int[] expectedValues = ExpectedContentPatterns(set, pattern);
        SchemaRuleIndex index = new SchemaRuleIndex(set);

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Kind, Is.EqualTo(SchemaPatternKind.Attribute));
            Assert.That(rule.OwnerRuleIndex,
                Is.InRange(0, set.Rules.Length - 1));
            Assert.That(rule.OwnerRuleIndex, Is.EqualTo(pattern.OwnerRuleIndex));
            Assert.That(rule.LocalName,
                Is.EqualTo(pattern.Name.Length > 0
                    ? pattern.Name
                    : namePattern?.Name ?? ""));
            Assert.That(rule.NamespaceName,
                Is.EqualTo(pattern.Name.Length > 0
                    ? pattern.NamespaceName
                    : namePattern?.NamespaceName ?? pattern.NamespaceName));
            Assert.That(rule.ValuePatternIndexes, Is.EqualTo(expectedValues));
            Assert.That(index.FindAttributeRules(
                rule.NamespaceName,
                rule.LocalName), Does.Contain(rule));
        });
        AssertIndexes(set, rule.ValuePatternIndexes);
    }

    [TestCaseSource(nameof(EveryDatatypeRule))]
    public void EveryDatatypeRuleRetainsItsValueLibraryAndParameters(
        OdfVersion version,
        string profile,
        int datatypeIndex)
    {
        SchemaRuleSet set = RequiredSet(version, profile);
        SchemaDatatype datatype = set.Datatypes[datatypeIndex];
        SchemaPattern pattern = set.Patterns[datatype.PatternIndex];
        SchemaPattern[] parameters = DirectParameterPatterns(set, pattern);

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Kind,
                Is.EqualTo(datatype.IsValue
                    ? SchemaPatternKind.Value
                    : SchemaPatternKind.Data));
            Assert.That(datatype.Name, Is.EqualTo(pattern.Name));
            Assert.That(datatype.Library, Is.EqualTo(pattern.DatatypeLibrary));
            Assert.That(datatype.Value, Is.EqualTo(pattern.Value));
            Assert.That(datatype.ParameterNames,
                Is.EqualTo(parameters.Select(parameter => parameter.Name)));
            Assert.That(datatype.ParameterValues,
                Is.EqualTo(parameters.Select(parameter => parameter.Value)));
        });
    }

    [TestCaseSource(nameof(EveryOccurrenceRule))]
    public void EveryOccurrenceRuleRetainsItsBoundsAndChildPatterns(
        OdfVersion version,
        string profile,
        int occurrenceIndex)
    {
        SchemaRuleSet set = RequiredSet(version, profile);
        SchemaOccurrenceRule occurrence = set.Occurrences[occurrenceIndex];
        SchemaPattern pattern = set.Patterns[occurrence.PatternIndex];

        Assert.Multiple(() =>
        {
            Assert.That(pattern.Kind, Is.AnyOf(
                SchemaPatternKind.Optional,
                SchemaPatternKind.ZeroOrMore,
                SchemaPatternKind.OneOrMore));
            Assert.That(occurrence.Minimum,
                Is.EqualTo(pattern.Kind == SchemaPatternKind.OneOrMore ? 1 : 0));
            Assert.That(occurrence.Maximum,
                Is.EqualTo(pattern.Kind == SchemaPatternKind.Optional ? 1 : -1));
            Assert.That(pattern.ChildCount, Is.GreaterThan(0));
        });
        int[] children = ChildPatternIndexes(set, pattern);
        Assert.That(children, Has.Length.EqualTo(pattern.ChildCount));
        AssertIndexes(set, children);
    }

    static SchemaRuleSet RequiredSet(OdfVersion version, string profile)
    {
        SchemaRuleSet? result = OdfSchemaRuleCatalog.GetRuleSet(
            version,
            profile);
        Assert.That(result, Is.Not.Null);
        return result!;
    }

    static SchemaPattern? DirectNamePattern(
        SchemaRuleSet set,
        SchemaPattern pattern)
    {
        int index = 0;
        while (index < pattern.ChildCount)
        {
            int childIndex = set.PatternChildIndexes[
                pattern.FirstChildIndex + index];
            SchemaPattern child = set.Patterns[childIndex];
            if (child.Kind == SchemaPatternKind.Name)
            {
                return child;
            }
            index++;
        }
        return null;
    }

    static int[] ExpectedContentPatterns(
        SchemaRuleSet set,
        SchemaPattern pattern)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < pattern.ChildCount)
        {
            int childIndex = set.PatternChildIndexes[
                pattern.FirstChildIndex + index];
            SchemaPatternKind kind = set.Patterns[childIndex].Kind;
            if (kind != SchemaPatternKind.Name
                && kind != SchemaPatternKind.AnyName
                && kind != SchemaPatternKind.NamespaceName)
            {
                result.Add(childIndex);
            }
            index++;
        }
        return result.ToArray();
    }

    static SchemaPattern[] DirectParameterPatterns(
        SchemaRuleSet set,
        SchemaPattern pattern)
    {
        List<SchemaPattern> result = new List<SchemaPattern>();
        int index = 0;
        while (index < pattern.ChildCount)
        {
            int childIndex = set.PatternChildIndexes[
                pattern.FirstChildIndex + index];
            SchemaPattern child = set.Patterns[childIndex];
            if (child.Kind == SchemaPatternKind.Param)
            {
                result.Add(child);
            }
            index++;
        }
        return result.ToArray();
    }

    static int[] ChildPatternIndexes(
        SchemaRuleSet set,
        SchemaPattern pattern)
    {
        int[] result = new int[pattern.ChildCount];
        int index = 0;
        while (index < pattern.ChildCount)
        {
            result[index] = set.PatternChildIndexes[
                pattern.FirstChildIndex + index];
            index++;
        }
        return result;
    }

    static void AssertIndexes(SchemaRuleSet set, int[] indexes)
    {
        int index = 0;
        while (index < indexes.Length)
        {
            AssertPatternIndex(set, indexes[index]);
            index++;
        }
    }

    static void AssertPatternIndex(SchemaRuleSet set, int patternIndex)
    {
        Assert.That(patternIndex, Is.InRange(0, set.Patterns.Length - 1));
    }
}
