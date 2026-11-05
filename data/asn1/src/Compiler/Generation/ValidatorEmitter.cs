namespace asn1;

internal class ValidatorEmitter
{
    internal CSharpNameMapper NameMapper = new CSharpNameMapper();
    internal ModelEmitter ModelEmitter = new ModelEmitter();
    internal CSharpGenerationPlan? GenerationPlan;

    internal CSharpValidatorEmission EmitValidator(
        LoweredModuleIr module,
        string namespaceName,
        CSharpNameScope typeScope)
    {
        if (module == null)
        {
            throw new System.ArgumentNullException("module");
        }

        if (namespaceName == null)
        {
            throw new System.ArgumentNullException("namespaceName");
        }

        if (typeScope == null)
        {
            throw new System.ArgumentNullException("typeScope");
        }

        if (namespaceName.Length == 0)
        {
            throw new System.ArgumentException(
                "A generated validator requires a namespace.",
                "namespaceName");
        }

        CSharpModulePlan? modulePlan = this.GenerationPlan == null
            ? null
            : this.GenerationPlan.FindModulePlan(module);
        CSharpNameMapping validatorName = modulePlan == null
            ? this.NameMapper.MapTypeName(
                module.Name + "-validator",
                module.StableId + "/generated-validator",
                typeScope)
            : modulePlan.ValidatorName;
        CSharpTextWriter writer = new CSharpTextWriter();
        writer.WriteLine("using asn1;");
        writer.WriteLine("");
        writer.WriteLine("namespace " + namespaceName + ";");
        writer.WriteLine("");
        this.EmitValidatorClass(writer, module, validatorName);
        return new CSharpValidatorEmission
        {
            Content = writer.GetText(),
            ValidatorName = validatorName,
            MethodNames = ["ValidateRuntimeValue"]
        };
    }

    internal void EmitValidatorClass(
        CSharpTextWriter writer,
        LoweredModuleIr module,
        CSharpNameMapping validatorName)
    {
        string escapedModuleName = this.ModelEmitter.EscapeXml(module.Name);
        this.ModelEmitter.WriteSummary(
            writer,
            "Validates runtime values against generated constraints for ASN.1 module <c>"
                + escapedModuleName + "</c>.");
        writer.WriteLine("public class " + validatorName.GeneratedName
            + " : IAsnValueValidator");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "internal IAsnValueValidator Implementation;");
        writer.WriteLine("");
        this.ModelEmitter.WriteSummary(
            writer,
            "Initializes the generated constraint validator.");
        writer.WriteLine("public " + validatorName.GeneratedName + "()");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "this.Implementation = new AsnRuntimeConstraintEvaluator();");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
        writer.WriteLine("");
        this.ModelEmitter.WriteSummary(
            writer,
            "Validates one runtime value against a generated type descriptor.");
        writer.WriteLine("/// <param name=\"value\">The runtime value.</param>");
        writer.WriteLine(
            "/// <param name=\"type\">The generated governing type descriptor.</param>");
        writer.WriteLine("/// <returns>The validation diagnostics.</returns>");
        writer.WriteLine(
            "public AsnDiagnostic[] ValidateRuntimeValue(AsnRuntimeValue value, AsnTypeDescriptor type)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "return this.Implementation.ValidateRuntimeValue(value, type);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }
}
