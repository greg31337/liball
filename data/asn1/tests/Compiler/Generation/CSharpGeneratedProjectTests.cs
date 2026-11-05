using asn1;

namespace asn1.Tests;

public class CSharpGeneratedProjectTests
{
    [Test]
    public void RepresentativeShapesAndTargetsMatchGoldenManifest()
    {
        LoweredCompilationIr compilation = Compilation();
        AsnCodeGenerationTarget[] targets =
        [
            AsnCodeGenerationTarget.ModelOnly,
            AsnCodeGenerationTarget.DescriptorOnly,
            AsnCodeGenerationTarget.EncodeOnly,
            AsnCodeGenerationTarget.DecodeOnly,
            AsnCodeGenerationTarget.Combined
        ];
        System.Text.StringBuilder actual = new System.Text.StringBuilder();
        int index = 0;
        while (index < targets.Length)
        {
            CSharpGenerationResult generated = new CSharpGenerator()
                .GenerateCompilation(compilation, Options(targets[index]));
            System.Text.StringBuilder targetManifest =
                new System.Text.StringBuilder();
            int sourceIndex = 0;
            while (sourceIndex < generated.Sources.Length)
            {
                CSharpGeneratedSource source = generated.Sources[sourceIndex];
                targetManifest.Append(source.Partition.RelativePath);
                targetManifest.Append('|');
                targetManifest.AppendLine(Fingerprint(source.Content));
                sourceIndex = sourceIndex + 1;
            }

            actual.Append(targets[index].ToString());
            actual.Append('|');
            actual.AppendLine(Fingerprint(targetManifest.ToString()));
            index = index + 1;
        }

        const string expected = """
            ModelOnly|63E5B017E1EF14F8BBCA8AE486923E008ED7F6BA60C4A6CAE11EF55E3272E52C
            DescriptorOnly|9E9D58AF36C6F026713080887C08BDA14596058DC6B93C5F8BE98D412A5552FA
            EncodeOnly|DB78C6FAC53725AFD71FDD820B47E44EFDF96F61BAF4C9D9A7B6C0082CCFD06C
            DecodeOnly|DB78C6FAC53725AFD71FDD820B47E44EFDF96F61BAF4C9D9A7B6C0082CCFD06C
            Combined|DB78C6FAC53725AFD71FDD820B47E44EFDF96F61BAF4C9D9A7B6C0082CCFD06C
            """;
        Assert.That(actual.ToString().TrimEnd(), Is.EqualTo(expected));
    }

    [Test]
    public void CombinedMultiModuleOutputCompilesAndRunsInTemporaryProject()
    {
        CSharpGenerationResult generated = new CSharpGenerator()
            .GenerateCompilation(
                Compilation(),
                Options(AsnCodeGenerationTarget.Combined));
        string directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "asn1-generated-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            int index = 0;
            while (index < generated.Sources.Length)
            {
                CSharpGeneratedSource source = generated.Sources[index];
                string path = System.IO.Path.Combine(
                    directory,
                    source.Partition.RelativePath);
                string? parent = System.IO.Path.GetDirectoryName(path);
                if (parent != null)
                {
                    System.IO.Directory.CreateDirectory(parent);
                }

                System.IO.File.WriteAllText(path, source.Content);
                index = index + 1;
            }

            string assemblyPath = typeof(AsnType).Assembly.Location;
            string escapedAssemblyPath = System.Security.SecurityElement.Escape(
                assemblyPath) ?? assemblyPath;
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(directory, "GeneratedSmoke.csproj"),
                ProjectText(escapedAssemblyPath));
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(directory, "Program.cs"),
                ProgramText());

            System.Diagnostics.ProcessStartInfo start =
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = DotNetHost(),
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
            start.ArgumentList.Add("run");
            start.ArgumentList.Add("--configuration");
            start.ArgumentList.Add("Release");
            start.ArgumentList.Add("--nologo");
            start.ArgumentList.Add("--project");
            start.ArgumentList.Add("GeneratedSmoke.csproj");
            using System.Diagnostics.Process process =
                System.Diagnostics.Process.Start(start)
                    ?? throw new InvalidOperationException(
                        "The generated-project process did not start.");
            System.Threading.Tasks.Task<string> outputTask =
                process.StandardOutput.ReadToEndAsync();
            System.Threading.Tasks.Task<string> errorTask =
                process.StandardError.ReadToEndAsync();
            bool exited = process.WaitForExit(120000);
            if (!exited)
            {
                process.Kill(true);
            }

            string output = outputTask.GetAwaiter().GetResult();
            string error = errorTask.GetAwaiter().GetResult();
            Assert.Multiple(() =>
            {
                Assert.That(exited, Is.True, "Generated project timed out.");
                Assert.That(process.ExitCode, Is.EqualTo(0), output + error);
                Assert.That(output, Does.Contain("generated-smoke-ok"), error);
            });
        }
        finally
        {
            System.IO.Directory.Delete(directory, true);
        }
    }

    private static AsnGenerationOptions Options(
        AsnCodeGenerationTarget target)
    {
        return new AsnGenerationOptions
        {
            NamespaceName = "Generated.Integration",
            Target = target,
            IncludeSourceMappings = true
        };
    }

    private static string Fingerprint(string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return System.Convert.ToHexString(hash);
    }

    private static string DotNetHost()
    {
        string? configured = System.Environment.GetEnvironmentVariable(
            "DOTNET_HOST_PATH");
        return configured == null || configured.Length == 0
            ? "dotnet"
            : configured;
    }

    private static string ProjectText(string assemblyPath)
    {
        return "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
            + "  <PropertyGroup>\n"
            + "    <OutputType>Exe</OutputType>\n"
            + "    <TargetFramework>net10.0</TargetFramework>\n"
            + "    <LangVersion>12.0</LangVersion>\n"
            + "    <ImplicitUsings>enable</ImplicitUsings>\n"
            + "    <Nullable>enable</Nullable>\n"
            + "    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n"
            + "  </PropertyGroup>\n"
            + "  <ItemGroup>\n"
            + "    <Reference Include=\"asn1\">\n"
            + "      <HintPath>" + assemblyPath + "</HintPath>\n"
            + "    </Reference>\n"
            + "  </ItemGroup>\n"
            + "</Project>\n";
    }

    private static string ProgramText()
    {
        return """
            using asn1;
            using Generated.Integration.Client;
            using Generated.Integration.Library;

            Counter counter = new Counter();
            counter.Value = new System.Numerics.BigInteger(4);
            Message message = new Message();
            message.Count = counter.Value;
            message.HasLabel = true;
            message.Label = "ready";
            Envelope envelope = new Envelope();
            envelope.Message = message;
            Outcome outcome = new Outcome();
            outcome.Selection = OutcomeSelection.Text;
            outcome.Text = "done";
            MessageList list = new MessageList();
            list.Items = [message];
            MessageSet set = new MessageSet();
            set.Items = [message];
            EmptyRecord empty = new EmptyRecord();
            empty.Present = true;
            if (LibraryValues.DefaultCount != new System.Numerics.BigInteger(3)
                || envelope.Message.Count != counter.Value
                || outcome.Text != "done"
                || list.Items.Length != 1
                || set.Items.Length != 1
                || !empty.Present)
            {
                throw new System.InvalidOperationException("Generated model/value failure.");
            }

            LibrarySchema librarySchema = new LibrarySchema();
            ClientSchema clientSchema = new ClientSchema();
            AsnTypeDescriptor counterType = librarySchema.Module.Types.Single(
                item => item.Name == "Counter");
            if (librarySchema.Catalog.Assignments.Length != 4
                || clientSchema.Module.Types.Length != 4
                || clientSchema.Module.Imports.Single().ModuleName != "Library")
            {
                throw new System.InvalidOperationException("Generated descriptor failure.");
            }

            LibraryValidator validator = new LibraryValidator();
            AsnRuntimeValue valid = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 4
            };
            AsnRuntimeValue invalid = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 11
            };
            if (validator.ValidateRuntimeValue(valid, counterType).Length != 0
                || validator.ValidateRuntimeValue(invalid, counterType).Length == 0)
            {
                throw new System.InvalidOperationException("Generated validator failure.");
            }

            FakeProvider provider = new FakeProvider();
            ClientCodec codec = new ClientCodec(
                provider,
                clientSchema.Catalog,
                new AsnCodecOptions());
            AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest());
            if (!encoded.Success || encoded.WrittenCount != 1
                || provider.Codec.EncodeCalls != 1
                || provider.Catalog != clientSchema.Catalog)
            {
                throw new System.InvalidOperationException("Generated codec facade failure.");
            }

            System.Console.WriteLine("generated-smoke-ok");

            public class FakeProvider : IAsnCodecProvider
            {
                public FakeCodec Codec = new FakeCodec();
                public AsnReflectionCatalog? Catalog;

                public IAsnCodec CreateCodec(
                    AsnReflectionCatalog catalog,
                    AsnCodecOptions options)
                {
                    this.Catalog = catalog;
                    return this.Codec;
                }
            }

            public class FakeCodec : IAsnCodec
            {
                public int EncodeCalls;

                public AsnEncodeResult Encode(AsnEncodeRequest request)
                {
                    this.EncodeCalls = this.EncodeCalls + 1;
                    return new AsnEncodeResult
                    {
                        Success = true,
                        Data = [42],
                        WrittenCount = 1
                    };
                }

                public AsnDecodeResult Decode(AsnDecodeRequest request)
                {
                    return new AsnDecodeResult { Status = AsnDecodeStatus.Done };
                }

                public AsnDecodeResult TryDecodeSpan(
                    System.ReadOnlySpan<byte> input,
                    string typeName,
                    AsnCodecOptions options)
                {
                    return new AsnDecodeResult { Status = AsnDecodeStatus.Done };
                }

                public AsnEncodeResult EncodeToSpan(
                    AsnEncodeRequest request,
                    System.Span<byte> destination)
                {
                    return this.Encode(request);
                }

                public AsnCanonicalityResult CheckCanonicality(
                    AsnDecodeRequest request)
                {
                    return new AsnCanonicalityResult
                    {
                        Status = AsnCanonicalityStatus.Canonical
                    };
                }

                public AsnTranscodeResult Transcode(AsnTranscodeRequest request)
                {
                    return new AsnTranscodeResult { Success = true };
                }

                public IAsnStreamingCodec CreateStreamingCodec()
                {
                    return null!;
                }

                public IAsnIncrementalDecoder CreateIncrementalDecoder(
                    string typeName,
                    AsnCodecOptions options)
                {
                    return null!;
                }
            }
            """;
    }

    private static LoweredCompilationIr Compilation()
    {
        LoweredTypeIr counter = new LoweredTypeIr
        {
            Name = "Counter",
            Kind = AsnTypeKind.Integer,
            Constraints =
            [
                new LoweredConstraintIr
                {
                    DomainKind = AsnTypeKind.Integer,
                    Root = new LoweredConstraintNodeIr
                    {
                        Kind = AsnConstraintKind.ValueRange,
                        HasLowerBound = true,
                        LowerBound = 0,
                        HasUpperBound = true,
                        UpperBound = 10
                    },
                    RootIntervals =
                    [
                        new LoweredIntervalIr
                        {
                            HasLowerBound = true,
                            LowerBound = 0,
                            HasUpperBound = true,
                            UpperBound = 10
                        }
                    ]
                }
            ]
        };
        LoweredTypeIr message = new LoweredTypeIr
        {
            Name = "Message",
            Kind = AsnTypeKind.Sequence,
            Extensible = true,
            Fields =
            [
                new LoweredFieldIr { Name = "count", Type = counter },
                new LoweredFieldIr
                {
                    Name = "label",
                    Type = new LoweredTypeIr { Kind = AsnTypeKind.Utf8String },
                    Optional = true
                },
                new LoweredFieldIr
                {
                    Name = "future",
                    Type = new LoweredTypeIr { Kind = AsnTypeKind.Boolean },
                    Optional = true,
                    Extension = true
                }
            ]
        };
        message.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = [message.Fields[0], message.Fields[1]],
            AdditionalFields = [message.Fields[2]]
        };
        LoweredTypeIr empty = new LoweredTypeIr
        {
            Name = "EmptyRecord",
            Kind = AsnTypeKind.Set
        };
        LoweredTypeIr envelope = new LoweredTypeIr
        {
            Name = "Envelope",
            Kind = AsnTypeKind.Sequence,
            Fields = [new LoweredFieldIr { Name = "message", Type = message }]
        };
        envelope.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = envelope.Fields
        };
        LoweredTypeIr outcome = new LoweredTypeIr
        {
            Name = "Outcome",
            Kind = AsnTypeKind.Choice,
            Fields =
            [
                new LoweredFieldIr { Name = "number", Type = counter },
                new LoweredFieldIr
                {
                    Name = "text",
                    Type = new LoweredTypeIr { Kind = AsnTypeKind.Utf8String }
                }
            ]
        };
        outcome.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = outcome.Fields
        };
        LoweredTypeIr list = new LoweredTypeIr
        {
            Name = "MessageList",
            Kind = AsnTypeKind.SequenceOf,
            ElementType = message
        };
        LoweredTypeIr set = new LoweredTypeIr
        {
            Name = "MessageSet",
            Kind = AsnTypeKind.SetOf,
            ElementType = message
        };
        LoweredModuleIr library = new LoweredModuleIr
        {
            Name = "Library",
            EmissionOrder = 0,
            Assignments =
            [
                TypeAssignment("Library", "Counter", counter, 0),
                TypeAssignment("Library", "Message", message, 1),
                TypeAssignment("Library", "EmptyRecord", empty, 2),
                new LoweredAssignmentIr
                {
                    Name = "default-count",
                    QualifiedName = "Library.default-count",
                    Kind = AsnAssignmentKind.Value,
                    EmissionOrder = 3,
                    Type = counter,
                    Value = new LoweredValueIr
                    {
                        Kind = AsnValueKind.Integer,
                        HasIntegerValue = true,
                        IntegerValue = 3
                    }
                }
            ]
        };
        LoweredModuleIr client = new LoweredModuleIr
        {
            Name = "Client",
            EmissionOrder = 1,
            Imports = [new LoweredImportIr
            {
                ModuleName = "Library",
                Symbols = ["Message"]
            }],
            Assignments =
            [
                TypeAssignment("Client", "Envelope", envelope, 0),
                TypeAssignment("Client", "Outcome", outcome, 1),
                TypeAssignment("Client", "MessageList", list, 2),
                TypeAssignment("Client", "MessageSet", set, 3)
            ]
        };
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules = [library, client]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        return compilation;
    }

    private static LoweredAssignmentIr TypeAssignment(
        string module,
        string name,
        LoweredTypeIr type,
        int order)
    {
        return new LoweredAssignmentIr
        {
            Name = name,
            QualifiedName = module + "." + name,
            Kind = AsnAssignmentKind.Type,
            Exported = true,
            EmissionOrder = order,
            Type = type
        };
    }
}
