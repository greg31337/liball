using asn1;

namespace asn1.Tests;

public class CodecFacadeEmitterTests
{
    [Test]
    public void FacadeConfiguresOneCodecAndForwardsInterfaceOperations()
    {
        LoweredModuleIr module = Module("Telemetry");

        CSharpCodecFacadeEmission emission = new CodecFacadeEmitter()
            .EmitFacade(
                module,
                "Generated.Telemetry",
                new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(emission.FacadeName.GeneratedName,
                Is.EqualTo("TelemetryCodec"));
            Assert.That(emission.MethodNames, Is.EqualTo(new string[]
            {
                "Encode",
                "Decode",
                "TryDecodeSpan",
                "EncodeToSpan",
                "CheckCanonicality",
                "Transcode",
                "CreateStreamingCodec",
                "CreateIncrementalDecoder"
            }));
            Assert.That(emission.MethodNames.Distinct().Count(),
                Is.EqualTo(emission.MethodNames.Length));
            Assert.That(emission.Content,
                Does.Contain("public class TelemetryCodec : IAsnCodec"));
            Assert.That(emission.Content,
                Does.Contain("this.Implementation = provider.CreateCodec(catalog, options);"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.Encode(request);"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.Decode(request);"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.TryDecodeSpan(input, typeName, options);"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.EncodeToSpan(request, destination);"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.CheckCanonicality(request);"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.Transcode(request);"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.CreateStreamingCodec();"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.CreateIncrementalDecoder(typeName, options);"));
        });
    }

    [Test]
    public void FacadeNameSharesTheGenerationTypeScope()
    {
        CSharpNameScope scope = new CSharpNameScope(false);
        scope.ReserveName("TelemetryCodec");

        CSharpCodecFacadeEmission emission = new CodecFacadeEmitter()
            .EmitFacade(Module("Telemetry"), "Generated", scope);

        Assert.That(emission.FacadeName.GeneratedName,
            Is.EqualTo("TelemetryCodec_2"));
    }

    [Test]
    public void FacadeEmissionIsDeterministic()
    {
        LoweredModuleIr module = Module("Stable");
        string first = new CodecFacadeEmitter().EmitFacade(
            module,
            "Generated.Stable",
            new CSharpNameScope(false)).Content;
        string second = new CodecFacadeEmitter().EmitFacade(
            module,
            "Generated.Stable",
            new CSharpNameScope(false)).Content;

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void CodecFacadeEmitterRejectsEmptyNamespaces()
    {
        Assert.That(() => new CodecFacadeEmitter().EmitFacade(
            Module("Telemetry"),
            "",
            new CSharpNameScope(false)), Throws.ArgumentException);
    }

    private static LoweredModuleIr Module(string name)
    {
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules = [new LoweredModuleIr { Name = name }]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        return compilation.Modules[0];
    }
}
