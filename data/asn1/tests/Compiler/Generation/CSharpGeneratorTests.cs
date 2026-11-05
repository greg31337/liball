using asn1;

namespace asn1.Tests;

public class CSharpGeneratorTests
{
    [Test]
    public void TargetsSelectTheRequiredArtifactPartitions()
    {
        AssertKinds(
            AsnCodeGenerationTarget.ModelOnly,
            [
                CSharpFilePartitionKind.Model,
                CSharpFilePartitionKind.Constants
            ]);
        AssertKinds(
            AsnCodeGenerationTarget.DescriptorOnly,
            [CSharpFilePartitionKind.Descriptors]);
        CSharpFilePartitionKind[] codecKinds =
        [
            CSharpFilePartitionKind.Model,
            CSharpFilePartitionKind.Constants,
            CSharpFilePartitionKind.Descriptors,
            CSharpFilePartitionKind.Validator,
            CSharpFilePartitionKind.Codec
        ];
        AssertKinds(AsnCodeGenerationTarget.EncodeOnly, codecKinds);
        AssertKinds(AsnCodeGenerationTarget.DecodeOnly, codecKinds);
        AssertKinds(AsnCodeGenerationTarget.Combined, codecKinds);
    }

    [Test]
    public void CodecTargetsRetainDescriptorsAndShareWireFacingSource()
    {
        CSharpGenerationResult encode = Generate(
            AsnCodeGenerationTarget.EncodeOnly);
        CSharpGenerationResult decode = Generate(
            AsnCodeGenerationTarget.DecodeOnly);
        CSharpGenerationResult combined = Generate(
            AsnCodeGenerationTarget.Combined);

        Assert.Multiple(() =>
        {
            Assert.That(encode.Capabilities.Encode, Is.True);
            Assert.That(encode.Capabilities.Decode, Is.False);
            Assert.That(decode.Capabilities.Encode, Is.False);
            Assert.That(decode.Capabilities.Decode, Is.True);
            Assert.That(combined.Capabilities.Encode, Is.True);
            Assert.That(combined.Capabilities.Decode, Is.True);
            Assert.That(ContentFor(
                encode,
                CSharpFilePartitionKind.Descriptors), Is.EqualTo(ContentFor(
                    decode,
                    CSharpFilePartitionKind.Descriptors)));
            Assert.That(ContentFor(
                encode,
                CSharpFilePartitionKind.Codec), Is.EqualTo(ContentFor(
                    decode,
                    CSharpFilePartitionKind.Codec)));
            Assert.That(ContentFor(
                combined,
                CSharpFilePartitionKind.Codec), Is.EqualTo(ContentFor(
                    encode,
                    CSharpFilePartitionKind.Codec)));
        });
    }

    [Test]
    public void PublicGeneratorLowersValidatedModelsAndReturnsSelectedFiles()
    {
        AsnType integer = new AsnType
        {
            Name = "Count",
            Kind = AsnTypeKind.Integer
        };
        AsnValidationResult validation = new AsnValidationResult
        {
            Success = true,
            Modules =
            [
                new AsnModule
                {
                    Name = "Public",
                    Assignments =
                    [
                        new AsnAssignment
                        {
                            Name = "Count",
                            Kind = AsnAssignmentKind.Type,
                            Type = integer
                        }
                    ]
                }
            ]
        };

        AsnGeneratedFile[] files = new AsnCodeGenerator(
            new CSharpGenerator()).GenerateFiles(
                validation,
                new AsnGenerationOptions
                {
                    NamespaceName = "Generated.PublicApi",
                    Target = AsnCodeGenerationTarget.DescriptorOnly
                });

        Assert.Multiple(() =>
        {
            Assert.That(files, Has.Length.EqualTo(1));
            Assert.That(files[0].RelativePath,
                Is.EqualTo("Generated/PublicApi/PublicValue/PublicSchema.cs"));
            Assert.That(files[0].Content,
                Does.Contain("public class PublicSchema"));
            Assert.That(files[0].Content,
                Does.Contain("public AsnReflectionCatalog Catalog"));
        });
    }

    [Test]
    public void PublicGeneratorRejectsUnsuccessfulValidation()
    {
        AsnDiagnostic[] diagnostics = [new AsnDiagnostic()];
        AsnValidationResult validation = new AsnValidationResult
        {
            Diagnostics = diagnostics
        };

        AsnCodeGenerationException? error = Assert.Throws<
            AsnCodeGenerationException>(() => new CSharpGenerator()
                .GenerateFiles(
                    validation,
                    new AsnGenerationOptions
                    {
                        NamespaceName = "Generated"
                    }));

        Assert.Multiple(() =>
        {
            Assert.That(error?.Code, Is.EqualTo("ASNGEN001"));
            Assert.That(error?.Diagnostics, Is.SameAs(diagnostics));
        });
    }

    [Test]
    public void GeneratorRejectsUnknownTargets()
    {
        AsnGenerationOptions options = Options(
            (AsnCodeGenerationTarget)99);

        Assert.That(() => new CSharpGenerator().GenerateCompilation(
            Compilation(),
            options), Throws.ArgumentException);
    }

    [Test]
    public void EveryAssignmentCategoryParticipatesInTheTargetMatrix()
    {
        LoweredTypeIr integer = new LoweredTypeIr
        {
            Name = "Number",
            Kind = AsnTypeKind.Integer
        };
        LoweredObjectClassIr objectClass = new LoweredObjectClassIr
        {
            Name = "ITEM"
        };
        LoweredObjectIr informationObject = new LoweredObjectIr
        {
            Name = "item",
            ObjectClassName = "ITEM"
        };
        LoweredObjectSetIr informationObjectSet = new LoweredObjectSetIr
        {
            Name = "Items",
            ObjectClassName = "ITEM",
            RootObjects = [informationObject]
        };
        LoweredModuleIr module = new LoweredModuleIr
        {
            Name = "Categories",
            Assignments =
            [
                Assignment("Number", AsnAssignmentKind.Type, integer),
                new LoweredAssignmentIr
                {
                    Name = "answer",
                    QualifiedName = "Categories.answer",
                    Kind = AsnAssignmentKind.Value,
                    Type = integer,
                    Value = new LoweredValueIr
                    {
                        Kind = AsnValueKind.Integer,
                        HasIntegerValue = true,
                        IntegerValue = 42
                    }
                },
                Assignment("Numbers", AsnAssignmentKind.ValueSet, integer),
                new LoweredAssignmentIr
                {
                    Name = "ITEM",
                    QualifiedName = "Categories.ITEM",
                    Kind = AsnAssignmentKind.ObjectClass,
                    ObjectClass = objectClass
                },
                new LoweredAssignmentIr
                {
                    Name = "item",
                    QualifiedName = "Categories.item",
                    Kind = AsnAssignmentKind.Object,
                    Object = informationObject
                },
                new LoweredAssignmentIr
                {
                    Name = "Items",
                    QualifiedName = "Categories.Items",
                    Kind = AsnAssignmentKind.ObjectSet,
                    ObjectSet = informationObjectSet
                }
            ]
        };
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules = [module]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        CSharpGenerator generator = new CSharpGenerator();
        CSharpGenerationResult models = generator.GenerateCompilation(
            compilation,
            CategoryOptions(AsnCodeGenerationTarget.ModelOnly));
        CSharpGenerationResult descriptors = generator.GenerateCompilation(
            compilation,
            CategoryOptions(AsnCodeGenerationTarget.DescriptorOnly));
        CSharpGenerationResult encode = generator.GenerateCompilation(
            compilation,
            CategoryOptions(AsnCodeGenerationTarget.EncodeOnly));
        CSharpGenerationResult decode = generator.GenerateCompilation(
            compilation,
            CategoryOptions(AsnCodeGenerationTarget.DecodeOnly));
        CSharpGenerationResult combined = generator.GenerateCompilation(
            compilation,
            CategoryOptions(AsnCodeGenerationTarget.Combined));
        string descriptorContent = descriptors.Sources.Single().Content;

        Assert.Multiple(() =>
        {
            Assert.That(models.Sources.Select(item => item.Partition.Kind),
                Is.EqualTo(new[]
                {
                    CSharpFilePartitionKind.Model,
                    CSharpFilePartitionKind.Constants
                }));
            Assert.That(descriptors.Sources.Select(item => item.Partition.Kind),
                Is.EqualTo(new[] { CSharpFilePartitionKind.Descriptors }));
            Assert.That(encode.Sources.Select(item => item.Partition.Kind),
                Is.EqualTo(decode.Sources.Select(item => item.Partition.Kind)));
            Assert.That(combined.Sources.Select(item => item.Partition.Kind),
                Is.EqualTo(encode.Sources.Select(item => item.Partition.Kind)));
            Assert.That(descriptorContent,
                Does.Contain("Kind = AsnAssignmentKind.Type"));
            Assert.That(descriptorContent,
                Does.Contain("Kind = AsnAssignmentKind.Value"));
            Assert.That(descriptorContent,
                Does.Contain("Kind = AsnAssignmentKind.ValueSet"));
            Assert.That(descriptorContent,
                Does.Contain("Kind = AsnAssignmentKind.ObjectClass"));
            Assert.That(descriptorContent,
                Does.Contain("Kind = AsnAssignmentKind.Object"));
            Assert.That(descriptorContent,
                Does.Contain("Kind = AsnAssignmentKind.ObjectSet"));
        });
    }

    private static LoweredAssignmentIr Assignment(
        string name,
        AsnAssignmentKind kind,
        LoweredTypeIr type)
    {
        return new LoweredAssignmentIr
        {
            Name = name,
            QualifiedName = "Categories." + name,
            Kind = kind,
            Type = type
        };
    }

    private static AsnGenerationOptions CategoryOptions(
        AsnCodeGenerationTarget target)
    {
        return new AsnGenerationOptions
        {
            NamespaceName = "Generated.Categories",
            Target = target
        };
    }

    private static void AssertKinds(
        AsnCodeGenerationTarget target,
        CSharpFilePartitionKind[] expected)
    {
        CSharpGenerationResult result = Generate(target);

        Assert.That(result.Sources.Select(item => item.Partition.Kind),
            Is.EqualTo(expected));
    }

    private static CSharpGenerationResult Generate(
        AsnCodeGenerationTarget target)
    {
        return new CSharpGenerator().GenerateCompilation(
            Compilation(),
            Options(target));
    }

    private static AsnGenerationOptions Options(
        AsnCodeGenerationTarget target)
    {
        return new AsnGenerationOptions
        {
            NamespaceName = "Generated.Targets",
            Target = target
        };
    }

    private static string ContentFor(
        CSharpGenerationResult result,
        CSharpFilePartitionKind kind)
    {
        return result.Sources.Single(item => item.Partition.Kind == kind)
            .Content;
    }

    private static LoweredCompilationIr Compilation()
    {
        LoweredTypeIr integer = new LoweredTypeIr
        {
            Kind = AsnTypeKind.Integer
        };
        LoweredTypeIr message = new LoweredTypeIr
        {
            Name = "Message",
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                new LoweredFieldIr
                {
                    Name = "id",
                    Type = integer
                }
            ]
        };
        message.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = message.Fields
        };
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules =
            [
                new LoweredModuleIr
                {
                    Name = "Messages",
                    Assignments =
                    [
                        new LoweredAssignmentIr
                        {
                            Name = "Message",
                            QualifiedName = "Messages.Message",
                            Kind = AsnAssignmentKind.Type,
                            Type = message
                        },
                        new LoweredAssignmentIr
                        {
                            Name = "default-id",
                            QualifiedName = "Messages.default-id",
                            Kind = AsnAssignmentKind.Value,
                            Type = integer,
                            Value = new LoweredValueIr
                            {
                                Kind = AsnValueKind.Integer,
                                HasIntegerValue = true,
                                IntegerValue = 7
                            }
                        }
                    ]
                }
            ]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        return compilation;
    }
}
