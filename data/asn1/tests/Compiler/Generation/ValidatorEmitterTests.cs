using asn1;

namespace asn1.Tests;

public class ValidatorEmitterTests
{
    [Test]
    public void ValidatorUsesSharedConstraintEvaluatorAndInterfaceOperation()
    {
        LoweredModuleIr module = Module("Telemetry");

        CSharpValidatorEmission emission = new ValidatorEmitter()
            .EmitValidator(
                module,
                "Generated.Telemetry",
                new CSharpNameScope(false));

        Assert.Multiple(() =>
        {
            Assert.That(emission.ValidatorName.GeneratedName,
                Is.EqualTo("TelemetryValidator"));
            Assert.That(emission.MethodNames,
                Is.EqualTo(new string[] { "ValidateRuntimeValue" }));
            Assert.That(emission.Content,
                Does.Contain("public class TelemetryValidator : IAsnValueValidator"));
            Assert.That(emission.Content,
                Does.Contain("this.Implementation = new AsnRuntimeConstraintEvaluator();"));
            Assert.That(emission.Content,
                Does.Contain("return this.Implementation.ValidateRuntimeValue(value, type);"));
        });
    }

    [Test]
    public void ValidatorNameSharesTheGenerationTypeScope()
    {
        CSharpNameScope scope = new CSharpNameScope(false);
        scope.ReserveName("TelemetryValidator");

        CSharpValidatorEmission emission = new ValidatorEmitter()
            .EmitValidator(Module("Telemetry"), "Generated", scope);

        Assert.That(emission.ValidatorName.GeneratedName,
            Is.EqualTo("TelemetryValidator_2"));
    }

    [Test]
    public void ValidatorEmitterRejectsEmptyNamespaces()
    {
        Assert.That(() => new ValidatorEmitter().EmitValidator(
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
