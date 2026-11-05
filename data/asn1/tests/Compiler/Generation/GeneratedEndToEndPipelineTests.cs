using asn1;

namespace asn1.Tests;

public class GeneratedEndToEndPipelineTests
{
    [Test]
    public void GeneratedEncoderFeedsEveryCompatibleDynamicRuntime()
    {
        AsnCompilationResult newer = Compile(true, true);
        AsnCompilationResult older = Compile(false, false);
        System.Collections.Generic.Dictionary<AsnEncodingRules, byte[]> encoded =
            RunGeneratedEncoder(newer.GeneratedFiles);
        AsnReflectionCatalog newerCatalog = RequireCatalog(newer);
        AsnReflectionCatalog olderCatalog = RequireCatalog(older);
        AsnTypeDescriptor olderType = EnvelopeType(olderCatalog);

        AsnEncodingRules[] rules = CompatibleRules();
        int ruleIndex = 0;
        while (ruleIndex < rules.Length)
        {
            AsnEncodingRules rule = rules[ruleIndex];
            byte[] data = encoded[rule];
            AsnCodecOptions options = Options(rule);
            IAsnCodec olderCodec = new DefaultAsnCodecProvider().CreateCodec(
                olderCatalog,
                options);
            IAsnIncrementalDecoder incremental = olderCodec
                .CreateIncrementalDecoder("Client.Envelope", options);
            AsnIncrementalResult result = DecodeIncrementally(
                incremental,
                data,
                237 + (int)rule);
            AsnRuntimeValue preserved = result.Value
                ?? throw new AssertionException(
                    RuntimeContext(rule, result.Diagnostics));
            AsnDiagnostic[] validation = new AsnRuntimeConstraintEvaluator()
                .ValidateRuntimeValue(preserved, olderType);
            AsnEncodeResult reencoded = olderCodec.Encode(
                new AsnEncodeRequest
                {
                    TypeName = "Client.Envelope",
                    Value = preserved
                });
            IAsnCodec newerCodec = new DefaultAsnCodecProvider().CreateCodec(
                newerCatalog,
                options);
            AsnDecodeResult recovered = newerCodec.Decode(
                new AsnDecodeRequest
                {
                    TypeName = "Client.Envelope",
                    Data = reencoded.Data
                });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done),
                    RuntimeContext(rule, result.Diagnostics));
                Assert.That(
                    preserved.UnknownExtensions.Length
                        + preserved.UnknownFields.Length,
                    Is.EqualTo(1),
                    "rule=" + rule);
                Assert.That(validation, Is.Empty,
                    RuntimeContext(rule, validation));
                Assert.That(reencoded.Success, Is.True,
                    RuntimeContext(rule, reencoded.Diagnostics));
                Assert.That(reencoded.Data, Is.EqualTo(data),
                    "rule=" + rule);
                Assert.That(recovered.Status, Is.EqualTo(AsnDecodeStatus.Done),
                    RuntimeContext(rule, recovered.Diagnostics));
                Assert.That(FutureText(recovered), Is.EqualTo("future-value"),
                    "rule=" + rule);
            });
            ruleIndex = ruleIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(newer.ValidationResult?.Success, Is.True);
            Assert.That(older.ValidationResult?.Success, Is.True);
            Assert.That(newer.ReflectionCatalog?.Modules.Select(item => item.Name),
                Is.EqualTo(new[] { "Library", "Client" }));
            Assert.That(newer.GeneratedFiles, Is.Not.Empty);
            Assert.That(encoded.Keys.OrderBy(item => item),
                Is.EqualTo(rules.OrderBy(item => item)));
        });
    }

    private static AsnCompilationResult Compile(bool newer, bool generate)
    {
        AsnCompilationInput input = new AsnCompilationInput
        {
            SourceUnits =
            [
                new AsnSourceUnit
                {
                    Name = newer ? "client-new.asn" : "client-old.asn",
                    Text = ClientSource(newer)
                },
                new AsnSourceUnit
                {
                    Name = "library.asn",
                    Text = LibrarySource()
                }
            ],
            CompilerOptions = new AsnCompilerOptions
            {
                EncodingRules = AsnEncodingRules.Ber,
                UnknownDataPolicy = AsnUnknownDataPolicy.Preserve
            }
        };
        if (generate)
        {
            input.GenerationOptions = new AsnGenerationOptions
            {
                NamespaceName = "Generated.EndToEnd",
                Target = AsnCodeGenerationTarget.Combined,
                IncludeSourceMappings = true
            };
        }

        AsnCompilationResult result = new CompilerImplementation().Compile(input);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Format(result.Diagnostics));
            Assert.That(result.ParserResults.All(item => item.Complete), Is.True);
            Assert.That(result.ValidationResult?.Success, Is.True,
                Format(result.Diagnostics));
            Assert.That(result.ReflectionCatalog, Is.Not.Null);
        });
        return result;
    }

    private static string LibrarySource()
    {
        return "Library DEFINITIONS AUTOMATIC TAGS ::= BEGIN\n"
            + "EXPORTS Code, BaseMessage;\n"
            + "Code ::= INTEGER (0..100)\n"
            + "BaseMessage ::= SEQUENCE {\n"
            + "  code Code,\n"
            + "  label UTF8String (SIZE (1..20))\n"
            + "}\n"
            + "END";
    }

    private static string ClientSource(bool newer)
    {
        return "Client DEFINITIONS AUTOMATIC TAGS ::= BEGIN\n"
            + "IMPORTS BaseMessage FROM Library;\n"
            + "Envelope ::= SEQUENCE {\n"
            + "  message BaseMessage,\n"
            + "  ..."
            + (newer ? ",\n  future UTF8String OPTIONAL\n" : "\n")
            + "}\n"
            + "END";
    }

    private static System.Collections.Generic.Dictionary<
        AsnEncodingRules,
        byte[]> RunGeneratedEncoder(AsnGeneratedFile[] files)
    {
        string directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "asn1-end-to-end-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            int index = 0;
            while (index < files.Length)
            {
                string path = System.IO.Path.Combine(
                    directory,
                    files[index].RelativePath);
                string? parent = System.IO.Path.GetDirectoryName(path);
                if (parent != null)
                {
                    System.IO.Directory.CreateDirectory(parent);
                }
                System.IO.File.WriteAllText(path, files[index].Content);
                index = index + 1;
            }

            string assemblyPath = typeof(AsnType).Assembly.Location;
            string escaped = System.Security.SecurityElement.Escape(
                assemblyPath) ?? assemblyPath;
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(directory, "EndToEnd.csproj"),
                ProjectText(escaped));
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
            start.ArgumentList.Add("EndToEnd.csproj");
            using System.Diagnostics.Process process =
                System.Diagnostics.Process.Start(start)
                    ?? throw new AssertionException(
                        "The generated end-to-end process did not start.");
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
            });

            System.Collections.Generic.Dictionary<AsnEncodingRules, byte[]>
                result = new System.Collections.Generic.Dictionary<
                    AsnEncodingRules,
                    byte[]>();
            string[] lines = output.Split(
                ['\r', '\n'],
                System.StringSplitOptions.RemoveEmptyEntries);
            index = 0;
            while (index < lines.Length)
            {
                if (lines[index].StartsWith(
                    "encoded|",
                    System.StringComparison.Ordinal))
                {
                    string[] parts = lines[index].Split('|');
                    AsnEncodingRules rule = (AsnEncodingRules)int.Parse(
                        parts[1],
                        System.Globalization.CultureInfo.InvariantCulture);
                    result[rule] = System.Convert.FromBase64String(parts[2]);
                }
                index = index + 1;
            }
            return result;
        }
        finally
        {
            System.IO.Directory.Delete(directory, true);
        }
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
            using Generated.EndToEnd.Client;

            ClientSchema schema = new ClientSchema();
            ClientValidator validator = new ClientValidator();
            AsnTypeDescriptor envelopeType = schema.Catalog.Assignments.Single(
                item => item.Name == "Envelope").Type
                ?? throw new System.InvalidOperationException("Envelope type missing.");
            AsnRuntimeValue valid = Envelope(42, true);
            AsnRuntimeValue invalid = Envelope(101, true);
            if (validator.ValidateRuntimeValue(valid, envelopeType).Length != 0
                || validator.ValidateRuntimeValue(invalid, envelopeType).Length == 0)
            {
                throw new System.InvalidOperationException(
                    "Generated constraint validation failed.");
            }

            AsnEncodingRules[] rules =
            [
                AsnEncodingRules.Ber,
                AsnEncodingRules.Uper,
                AsnEncodingRules.Oer,
                AsnEncodingRules.ExtendedXer
            ];
            foreach (AsnEncodingRules rule in rules)
            {
                AsnCodecOptions options = new AsnCodecOptions
                {
                    EncodingRules = rule,
                    UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
                    ValidateValues = true
                };
                ClientCodec codec = new ClientCodec(
                    new DefaultAsnCodecProvider(),
                    schema.Catalog,
                    options);
                AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
                {
                    TypeName = "Client.Envelope",
                    Value = valid
                });
                if (!encoded.Success)
                {
                    throw new System.InvalidOperationException(string.Join(
                        " | ",
                        encoded.Diagnostics.Select(item =>
                            item.Code + ":" + item.Message)));
                }
                System.Console.WriteLine(
                    "encoded|" + ((int)rule).ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + "|" + System.Convert.ToBase64String(encoded.Data));
            }

            static AsnRuntimeValue Envelope(int code, bool future)
            {
                AsnRuntimeValue message = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields =
                    [
                        Present("code", new AsnRuntimeValue
                        {
                            Kind = AsnRuntimeValueKind.Integer,
                            HasIntegerValue = true,
                            IntegerValue = code
                        }),
                        Present("label", new AsnRuntimeValue
                        {
                            Kind = AsnRuntimeValueKind.CharacterString,
                            TextValue = "ready"
                        })
                    ]
                };
                return new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields =
                    [
                        Present("message", message),
                        new AsnRuntimeFieldValue
                        {
                            Name = "future",
                            Present = future,
                            Value = future ? new AsnRuntimeValue
                            {
                                Kind = AsnRuntimeValueKind.CharacterString,
                                TextValue = "future-value"
                            } : null
                        }
                    ]
                };
            }

            static AsnRuntimeFieldValue Present(
                string name,
                AsnRuntimeValue value)
            {
                return new AsnRuntimeFieldValue
                {
                    Name = name,
                    Present = true,
                    Value = value
                };
            }
            """;
    }

    private static AsnIncrementalResult DecodeIncrementally(
        IAsnIncrementalDecoder decoder,
        byte[] data,
        int seed)
    {
        System.Random random = new System.Random(seed);
        AsnIncrementalResult result = new AsnIncrementalResult();
        int offset = 0;
        while (offset < data.Length)
        {
            int count = random.Next(
                1,
                System.Math.Min(4, data.Length - offset) + 1);
            result = decoder.Consume(new AsnIncrementalInput
            {
                Data = data,
                Offset = offset,
                HasCount = true,
                Count = count,
                IsFinalBlock = offset + count == data.Length
            });
            Assert.That(result.ConsumedCount, Is.EqualTo(count));
            offset = offset + count;
        }
        return result;
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = true
        };
    }

    private static AsnEncodingRules[] CompatibleRules()
    {
        return
        [
            AsnEncodingRules.Ber,
            AsnEncodingRules.Uper,
            AsnEncodingRules.Oer,
            AsnEncodingRules.ExtendedXer
        ];
    }

    private static AsnReflectionCatalog RequireCatalog(
        AsnCompilationResult result)
    {
        return result.ReflectionCatalog
            ?? throw new AssertionException("Reflection catalog is missing.");
    }

    private static AsnTypeDescriptor EnvelopeType(
        AsnReflectionCatalog catalog)
    {
        return catalog.Assignments.Single(item =>
                item.QualifiedName == "Client.Envelope").Type
            ?? throw new AssertionException("Envelope type is missing.");
    }

    private static string FutureText(AsnDecodeResult result)
    {
        AsnRuntimeValue value = result.Value
            ?? throw new AssertionException(Format(result.Diagnostics));
        AsnRuntimeFieldValue field = value.Fields.Single(item =>
            item.Name == "future" && item.Present);
        return field.Value?.TextValue
            ?? throw new AssertionException("Future value is missing.");
    }

    private static string DotNetHost()
    {
        string? configured = System.Environment.GetEnvironmentVariable(
            "DOTNET_HOST_PATH");
        return configured == null || configured.Length == 0
            ? "dotnet"
            : configured;
    }

    private static string RuntimeContext(
        AsnEncodingRules rules,
        AsnDiagnostic[] diagnostics)
    {
        return "rule=" + rules + ": " + Format(diagnostics);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(
            " | ",
            diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
