namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfTransformReportAggregationTests
{
    [Test]
    public void TransformReport_AggregatesUniqueChangesReasonsCodecsAndIssuesDeterministically()
    {
        GltfDocument source = new GltfDocument();
        source.Nodes.Add(new GltfNode());
        source.ExtensionsUsed.Add(GltfExtensionNames.KhrLightsPunctual);
        source.ExtensionsRequired.Add(GltfExtensionNames.KhrLightsPunctual);
        ReportingHook hook = new ReportingHook();
        GltfTransformPlan plan = new GltfTransformPlan
        {
            Steps =
            [
                new GltfTransformStep
                {
                    Kind = GltfTransformKind.Custom,
                    Name = "remove"
                },
                new GltfTransformStep
                {
                    Kind = GltfTransformKind.Custom,
                    Name = "disabled",
                    Enabled = false
                },
                new GltfTransformStep
                {
                    Kind = GltfTransformKind.Custom,
                    Name = "add"
                }
            ]
        };
        GltfTransformOptions options = CreateOptions(hook);

        GltfTransformReport report = new GltfTransformer().TransformDocument(
            source,
            plan,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(report.Steps, Has.Count.EqualTo(3));
            Assert.That(report.ChangedObjects,
                Is.EqualTo(new[] { "materials/1", "nodes/0", "nodes/2" }));
            Assert.That(report.ChangedResources,
                Is.EqualTo(new[] { "resources/1", "resources/2" }));
            Assert.That(report.Codecs,
                Is.EqualTo(new[] { "a.codec", "m.codec", "z.codec" }));
            Assert.That(report.SkippedReasons,
                Is.EqualTo(new[] { "The transform step is disabled." }));
            Assert.That(report.Diagnostics.Select(issue => issue.Code),
                Is.EqualTo(new[]
                {
                    "REPORT_A",
                    "REPORT_A",
                    "REPORT_A",
                    "REPORT_A",
                    "REPORT_B"
                }));
            Assert.That(report.Steps[0].Codecs,
                Is.EqualTo(new[] { "a.codec", "z.codec" }));
            Assert.That(report.Steps[2].Codecs,
                Is.EqualTo(new[] { "a.codec", "m.codec" }));
        });

        AssertExtensionChanges(
            report.ExtensionChanges,
            new[]
            {
                "KHR_lights_punctual|required=False|added=False",
                "KHR_lights_punctual|required=True|added=False",
                "KHR_materials_unlit|required=False|added=True",
                "KHR_materials_unlit|required=True|added=True"
            });
        AssertExtensionChanges(
            report.Steps[0].ExtensionChanges,
            new[]
            {
                "KHR_lights_punctual|required=False|added=False",
                "KHR_lights_punctual|required=True|added=False"
            });
        AssertExtensionChanges(
            report.Steps[2].ExtensionChanges,
            new[]
            {
                "KHR_materials_unlit|required=False|added=True",
                "KHR_materials_unlit|required=True|added=True"
            });
    }

    [Test]
    public void TransformReport_AggregateOrderingIsIndependentOfProviderOrder()
    {
        GltfDocument source = new GltfDocument();
        source.Nodes.Add(new GltfNode());
        ReportingHook forwardHook = new ReportingHook();
        ReportingHook reverseHook = new ReportingHook();
        reverseHook.ReverseChanges = true;
        GltfTransformPlan plan = new GltfTransformPlan
        {
            Steps = [new GltfTransformStep
            {
                Kind = GltfTransformKind.Custom,
                Name = "add"
            }]
        };

        GltfTransformReport forward = new GltfTransformer().TransformDocument(
            source,
            plan,
            CreateOptions(forwardHook));
        GltfTransformReport reverse = new GltfTransformer().TransformDocument(
            source,
            plan,
            CreateOptions(reverseHook));

        Assert.Multiple(() =>
        {
            Assert.That(reverse.ChangedObjects,
                Is.EqualTo(forward.ChangedObjects));
            Assert.That(reverse.ChangedResources,
                Is.EqualTo(forward.ChangedResources));
            Assert.That(reverse.Codecs, Is.EqualTo(forward.Codecs));
            Assert.That(reverse.Diagnostics.Select(issue => issue.Code),
                Is.EqualTo(forward.Diagnostics.Select(issue => issue.Code)));
        });
        AssertExtensionChanges(
            reverse.ExtensionChanges,
            DescribeExtensionChanges(forward.ExtensionChanges));
    }

    [Test]
    public void TransformReport_RecordsFailedExtensionChangesOnlyForPartialOutput()
    {
        GltfDocument rolledBackSource = new GltfDocument();
        rolledBackSource.Nodes.Add(new GltfNode());
        FailingExtensionHook rolledBackHook = new FailingExtensionHook();
        GltfTransformOptions rolledBackOptions = CreateOptions(rolledBackHook);

        GltfTransformReport rolledBack = new GltfTransformer().TransformDocument(
            rolledBackSource,
            CreateFailurePlan(),
            rolledBackOptions);

        GltfDocument partialSource = new GltfDocument();
        partialSource.Nodes.Add(new GltfNode());
        FailingExtensionHook partialHook = new FailingExtensionHook();
        GltfTransformOptions partialOptions = CreateOptions(partialHook);
        partialOptions.RollbackOnFailure = false;
        GltfTransformReport partial = new GltfTransformer().TransformDocument(
            partialSource,
            CreateFailurePlan(),
            partialOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rolledBack.ChangedObjects, Is.Empty);
            Assert.That(rolledBack.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "nodes/0" }));
            Assert.That(rolledBack.ExtensionChanges, Is.Empty);
            Assert.That(rolledBack.Steps[0].ExtensionChanges, Is.Empty);
            Assert.That(rolledBackSource.ExtensionsUsed, Is.Empty);
            Assert.That(partial.ChangedObjects,
                Is.EqualTo(new[] { "nodes/0" }));
            Assert.That(partial.Document!.ExtensionsUsed,
                Is.EqualTo(new[] { GltfExtensionNames.KhrMaterialsUnlit }));
            Assert.That(partial.ExtensionChanges, Has.Count.EqualTo(1));
            Assert.That(partial.ExtensionChanges[0].Name,
                Is.EqualTo(GltfExtensionNames.KhrMaterialsUnlit));
            Assert.That(partial.ExtensionChanges[0].Added, Is.True);
        });
    }

    private static GltfTransformOptions CreateOptions(IGltfTransformHook hook)
    {
        GltfTransformOptions result = new GltfTransformOptions
        {
            ValidateBefore = false,
            ValidateAfter = false
        };
        result.Hooks.Add(hook);
        return result;
    }

    private static GltfTransformPlan CreateFailurePlan()
    {
        return new GltfTransformPlan
        {
            Steps = [new GltfTransformStep
            {
                Kind = GltfTransformKind.Custom,
                Name = "fail"
            }]
        };
    }

    private static void AssertExtensionChanges(
        List<GltfTransformExtensionChange> actual,
        string[] expected)
    {
        Assert.That(DescribeExtensionChanges(actual), Is.EqualTo(expected));
    }

    private static string[] DescribeExtensionChanges(
        List<GltfTransformExtensionChange> changes)
    {
        return changes.Select(change =>
            $"{change.Name}|required={change.Required}|added={change.Added}")
            .ToArray();
    }

    private sealed class ReportingHook : IGltfTransformHook
    {
        public bool ReverseChanges;

        public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
        {
            GltfTransformStepResult result = new GltfTransformStepResult();
            if (context.Step!.Name == "remove")
            {
                context.Document!.ExtensionsUsed.Remove(
                    GltfExtensionNames.KhrLightsPunctual);
                context.Document.ExtensionsRequired.Remove(
                    GltfExtensionNames.KhrLightsPunctual);
                AddReportValues(
                    result,
                    new[] { "nodes/2", "nodes/0", "nodes/2" },
                    new[] { "resources/2", "resources/1", "resources/2" },
                    new[] { "z.codec", "a.codec", "z.codec" });
            }
            else
            {
                context.Document!.ExtensionsUsed.Add(
                    GltfExtensionNames.KhrMaterialsUnlit);
                context.Document.ExtensionsRequired.Add(
                    GltfExtensionNames.KhrMaterialsUnlit);
                AddReportValues(
                    result,
                    new[] { "materials/1", "nodes/0" },
                    new[] { "resources/1" },
                    new[] { "m.codec", "a.codec" });
            }

            result.Diagnostics.Add(CreateIssue("REPORT_A"));
            result.Diagnostics.Add(CreateIssue("REPORT_A"));
            if (context.Step.Name == "add")
            {
                result.Diagnostics.Add(CreateIssue("REPORT_B"));
            }

            return result;
        }

        private void AddReportValues(
            GltfTransformStepResult result,
            string[] objects,
            string[] resources,
            string[] codecs)
        {
            if (ReverseChanges)
            {
                Array.Reverse(objects);
                Array.Reverse(resources);
                Array.Reverse(codecs);
            }

            result.ChangedObjects.AddRange(objects);
            result.ChangedResources.AddRange(resources);
            result.Codecs.AddRange(codecs);
        }

        private static GltfValidationIssue CreateIssue(string code)
        {
            return new GltfValidationIssue
            {
                Code = code,
                Severity = GltfValidationSeverity.Warning,
                Message = code,
                JsonPointer = "/nodes/0"
            };
        }
    }

    private sealed class FailingExtensionHook : IGltfTransformHook
    {
        public GltfTransformStepResult ApplyTransformHook(GltfTransformContext context)
        {
            context.Document!.ExtensionsUsed.Add(
                GltfExtensionNames.KhrMaterialsUnlit);
            GltfTransformStepResult result = new GltfTransformStepResult
            {
                Status = GltfOperationStatus.Failed
            };
            result.ChangedObjects.Add("nodes/0");
            return result;
        }
    }
}
