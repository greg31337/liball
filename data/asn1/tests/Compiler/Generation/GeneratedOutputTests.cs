using asn1;

namespace asn1.Tests;

public class GeneratedOutputTests
{
    [Test]
    public void PublicFilesCarryStableContentMappingsAndCollisionDiagnostics()
    {
        AsnSourceUnit source = new AsnSourceUnit
        {
            Name = "collisions.asn",
            Text = new string('x', 80)
        };
        AsnValidationResult validation = new AsnValidationResult
        {
            Success = true,
            Modules =
            [
                new AsnModule
                {
                    Name = "Collisions",
                    Span = source.GetSpan(0, 80),
                    Assignments =
                    [
                        PublicTypeAssignment(
                            "foo-bar",
                            source.GetSpan(5, 15)),
                        PublicTypeAssignment(
                            "foo_bar",
                            source.GetSpan(25, 35))
                    ]
                }
            ]
        };
        AsnGenerationOptions options = new AsnGenerationOptions
        {
            NamespaceName = "Generated.Outputs",
            Target = AsnCodeGenerationTarget.ModelOnly,
            IncludeSourceMappings = true
        };

        AsnGeneratedFile[] first = new CSharpGenerator().GenerateFiles(
            validation,
            options);
        AsnGeneratedFile[] second = new CSharpGenerator().GenerateFiles(
            validation,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(item => item.RelativePath),
                Is.EqualTo(second.Select(item => item.RelativePath)));
            Assert.That(first.Select(item => item.Content),
                Is.EqualTo(second.Select(item => item.Content)));
            Assert.That(first.Select(item => item.RelativePath),
                Is.EqualTo(new string[]
                {
                    "Generated/Outputs/Collisions/FooBar.cs",
                    "Generated/Outputs/Collisions/FooBar_2.cs"
                }));
            Assert.That(first[1].SourceSpans.Select(item => item.Start.Offset),
                Does.Contain(25));
            Assert.That(first[1].Diagnostics, Has.Length.EqualTo(1));
            Assert.That(first[1].Diagnostics[0].Severity,
                Is.EqualTo(AsnDiagnosticSeverity.Warning));
            Assert.That(first[1].Diagnostics[0].Category,
                Is.EqualTo(AsnDiagnosticCategory.Generation));
            Assert.That(first[1].Diagnostics[0].Code,
                Is.EqualTo("ASNGEN0001"));
            Assert.That(first[1].Diagnostics[0].Message,
                Does.Contain("FooBar_2"));
            Assert.That(first[1].Diagnostics[0].Span?.Start.Offset,
                Is.EqualTo(25));
        });
    }

    [Test]
    public void SourceMappingsContainContributorsAndCloneInputSpans()
    {
        LoweredAssignmentIr modelAssignment = new LoweredAssignmentIr
        {
            Name = "Message",
            QualifiedName = "Mapped.Message",
            Kind = AsnAssignmentKind.Type,
            SourceSpan = Span(10, 20),
            Type = new LoweredTypeIr
            {
                Name = "Message",
                Kind = AsnTypeKind.Sequence,
                SourceSpan = Span(11, 19),
                Fields =
                [
                    new LoweredFieldIr
                    {
                        Name = "id",
                        SourceSpan = Span(12, 14),
                        Type = new LoweredTypeIr
                        {
                            Kind = AsnTypeKind.Integer,
                            SourceSpan = Span(13, 14)
                        }
                    }
                ]
            }
        };
        modelAssignment.Type.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = modelAssignment.Type.Fields
        };
        LoweredAssignmentIr valueAssignment = new LoweredAssignmentIr
        {
            Name = "default-id",
            QualifiedName = "Mapped.default-id",
            Kind = AsnAssignmentKind.Value,
            SourceSpan = Span(30, 40),
            Type = modelAssignment.Type.Fields[0].Type,
            Value = new LoweredValueIr
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 3,
                SourceSpan = Span(31, 32)
            }
        };
        LoweredCompilationIr compilation = LoweredCompilation(
            [modelAssignment, valueAssignment]);

        CSharpGenerationResult result = new CSharpGenerator()
            .GenerateCompilation(
                compilation,
                new AsnGenerationOptions
                {
                    NamespaceName = "Generated.Mapped",
                    Target = AsnCodeGenerationTarget.ModelOnly,
                    IncludeSourceMappings = true
                });
        CSharpGeneratedSource model = result.Sources.Single(item =>
            item.Partition.Kind == CSharpFilePartitionKind.Model);
        CSharpGeneratedSource constants = result.Sources.Single(item =>
            item.Partition.Kind == CSharpFilePartitionKind.Constants);
        modelAssignment.SourceSpan.Start.Offset = 70;

        Assert.Multiple(() =>
        {
            Assert.That(model.SourceSpans.Select(item => item.Start.Offset),
                Is.EqualTo(new int[] { 10, 11, 12, 13 }));
            Assert.That(constants.SourceSpans.Select(item => item.Start.Offset),
                Does.Contain(30));
            Assert.That(constants.SourceSpans.Select(item => item.Start.Offset),
                Does.Contain(31));
            Assert.That(constants.SourceSpans.Select(item => item.Start.Offset),
                Does.Not.Contain(10));
            Assert.That(model.SourceSpans[0].Start.Offset, Is.EqualTo(10));
        });
    }

    [Test]
    public void SourceMappingsCanBeOmittedWithoutSuppressingDiagnostics()
    {
        LoweredAssignmentIr first = LoweredTypeAssignment(
            "foo-bar",
            Span(5, 10));
        LoweredAssignmentIr second = LoweredTypeAssignment(
            "foo_bar",
            Span(15, 20));
        CSharpGenerationResult result = new CSharpGenerator()
            .GenerateCompilation(
                LoweredCompilation([first, second]),
                new AsnGenerationOptions
                {
                    NamespaceName = "Generated.NoMaps",
                    Target = AsnCodeGenerationTarget.ModelOnly
                });
        CSharpGeneratedSource collision = result.Sources[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Sources.All(item =>
                item.SourceSpans.Length == 0), Is.True);
            Assert.That(collision.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(collision.Diagnostics[0].Span, Is.Null);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void HostileRecursiveOutputIsStableAcrossInputReordering()
    {
        LoweredTypeIr recursive = new LoweredTypeIr
        {
            Name = "class",
            Kind = AsnTypeKind.Sequence
        };
        recursive.Fields =
        [
            new LoweredFieldIr
            {
                Name = "required",
                Type = recursive,
                Optional = true
            },
            new LoweredFieldIr
            {
                Name = "caf\u00e9",
                Type = new LoweredTypeIr { Kind = AsnTypeKind.Utf8String }
            },
            new LoweredFieldIr
            {
                Name = "cafe\u0301",
                Type = new LoweredTypeIr { Kind = AsnTypeKind.Utf8String }
            }
        ];
        recursive.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = recursive.Fields
        };
        LoweredAssignmentIr recursiveAssignment = new LoweredAssignmentIr
        {
            Name = "class",
            QualifiedName = "hostile/class.class",
            Kind = AsnAssignmentKind.Type,
            EmissionOrder = 0,
            Type = recursive
        };
        LoweredAssignmentIr reservedFileAssignment = new LoweredAssignmentIr
        {
            Name = "CON",
            QualifiedName = "hostile/class.CON",
            Kind = AsnAssignmentKind.Type,
            EmissionOrder = 1,
            Type = new LoweredTypeIr
            {
                Name = "CON",
                Kind = AsnTypeKind.Integer
            }
        };
        LoweredAssignmentIr collidingFileAssignment = new LoweredAssignmentIr
        {
            Name = "con-file",
            QualifiedName = "hostile/class.con-file",
            Kind = AsnAssignmentKind.Type,
            EmissionOrder = 2,
            Type = new LoweredTypeIr
            {
                Name = "con-file",
                Kind = AsnTypeKind.Boolean
            }
        };
        LoweredAssignmentIr valueAssignment = new LoweredAssignmentIr
        {
            Name = "namespace",
            QualifiedName = "hostile/class.namespace",
            Kind = AsnAssignmentKind.Value,
            EmissionOrder = 3,
            Type = reservedFileAssignment.Type,
            Value = new LoweredValueIr
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 9
            }
        };
        LoweredModuleIr hostile = new LoweredModuleIr
        {
            Name = "hostile/class",
            EmissionOrder = 0,
            Assignments =
            [
                recursiveAssignment,
                reservedFileAssignment,
                collidingFileAssignment,
                valueAssignment
            ]
        };
        LoweredModuleIr unicode = new LoweredModuleIr
        {
            Name = "caf\u00e9",
            EmissionOrder = 1,
            Assignments = [new LoweredAssignmentIr
            {
                Name = "9-value",
                QualifiedName = "caf\u00e9.9-value",
                Kind = AsnAssignmentKind.Type,
                Type = new LoweredTypeIr
                {
                    Name = "9-value",
                    Kind = AsnTypeKind.OctetString
                }
            }]
        };
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules = [hostile, unicode]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        AsnGenerationOptions options = new AsnGenerationOptions
        {
            NamespaceName = "generated.namespace",
            Target = AsnCodeGenerationTarget.Combined,
            IncludeSourceMappings = true
        };
        AsnGeneratedFile[] original = new CSharpGenerator().CreateFilesFromResult(
            new CSharpGenerator().GenerateCompilation(compilation, options));

        hostile.Assignments =
        [
            valueAssignment,
            collidingFileAssignment,
            reservedFileAssignment,
            recursiveAssignment
        ];
        compilation.Modules = [unicode, hostile];
        AsnGeneratedFile[] reordered = new CSharpGenerator().CreateFilesFromResult(
            new CSharpGenerator().GenerateCompilation(compilation, options));

        Assert.Multiple(() =>
        {
            Assert.That(reordered.Select(item => item.RelativePath),
                Is.EqualTo(original.Select(item => item.RelativePath)));
            Assert.That(reordered.Select(item => item.Content),
                Is.EqualTo(original.Select(item => item.Content)));
            Assert.That(original.Select(item => item.RelativePath),
                Does.Contain("Generated/NamespaceValue/HostileClass/CONFile.cs"));
            Assert.That(original.Select(item => item.RelativePath),
                Does.Contain("Generated/NamespaceValue/HostileClass/ConFile_2.cs"));
            Assert.That(original.Single(item => item.RelativePath.EndsWith(
                "/ClassValue.cs", StringComparison.Ordinal)).Content,
                Does.Contain("public ClassValue? RequiredValue = null;"));
            Assert.That(original.Single(item => item.RelativePath.EndsWith(
                "/ClassValue.cs", StringComparison.Ordinal)).Content,
                Does.Contain("public string CafU00E9_2 = \"\";"));
        });
    }

    private static AsnAssignment PublicTypeAssignment(
        string name,
        AsnSourceSpan span)
    {
        return new AsnAssignment
        {
            Name = name,
            Kind = AsnAssignmentKind.Type,
            Type = new AsnType
            {
                Name = name,
                Kind = AsnTypeKind.Integer
            },
            Span = span
        };
    }

    private static LoweredCompilationIr LoweredCompilation(
        LoweredAssignmentIr[] assignments)
    {
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules =
            [
                new LoweredModuleIr
                {
                    Name = "Mapped",
                    SourceSpan = Span(0, 50),
                    Assignments = assignments
                }
            ]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        return compilation;
    }

    private static LoweredAssignmentIr LoweredTypeAssignment(
        string name,
        AsnSourceSpan span)
    {
        return new LoweredAssignmentIr
        {
            Name = name,
            QualifiedName = "Mapped." + name,
            Kind = AsnAssignmentKind.Type,
            SourceSpan = span,
            Type = new LoweredTypeIr
            {
                Name = name,
                Kind = AsnTypeKind.Integer,
                SourceSpan = span
            }
        };
    }

    private static AsnSourceSpan Span(int start, int end)
    {
        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = "mapped.asn",
                Offset = start,
                Line = 1,
                Column = start + 1
            },
            End = new AsnSourcePosition
            {
                SourceName = "mapped.asn",
                Offset = end,
                Line = 1,
                Column = end + 1
            }
        };
    }
}
