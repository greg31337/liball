namespace asn1;

internal class CodecFacadeEmitter
{
    internal CSharpNameMapper NameMapper = new CSharpNameMapper();
    internal ModelEmitter ModelEmitter = new ModelEmitter();
    internal CSharpGenerationPlan? GenerationPlan;

    internal CSharpCodecFacadeEmission EmitFacade(
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
                "A generated codec facade requires a namespace.",
                "namespaceName");
        }

        CSharpModulePlan? modulePlan = this.GenerationPlan == null
            ? null
            : this.GenerationPlan.FindModulePlan(module);
        CSharpNameMapping facadeName = modulePlan == null
            ? this.NameMapper.MapTypeName(
                module.Name + "-codec",
                module.StableId + "/generated-codec",
                typeScope)
            : modulePlan.CodecName;
        CSharpTextWriter writer = new CSharpTextWriter();
        writer.WriteLine("using asn1;");
        writer.WriteLine("");
        writer.WriteLine("namespace " + namespaceName + ";");
        writer.WriteLine("");
        this.EmitFacadeClass(writer, module, facadeName);
        return new CSharpCodecFacadeEmission
        {
            Content = writer.GetText(),
            FacadeName = facadeName,
            MethodNames =
            [
                "Encode",
                "Decode",
                "TryDecodeSpan",
                "EncodeToSpan",
                "CheckCanonicality",
                "Transcode",
                "CreateStreamingCodec",
                "CreateIncrementalDecoder"
            ]
        };
    }

    internal void EmitFacadeClass(
        CSharpTextWriter writer,
        LoweredModuleIr module,
        CSharpNameMapping facadeName)
    {
        string escapedModuleName = this.ModelEmitter.EscapeXml(module.Name);
        this.ModelEmitter.WriteSummary(
            writer,
            "Provides configured codec operations for ASN.1 module <c>"
                + escapedModuleName + "</c>.");
        writer.WriteLine("public class " + facadeName.GeneratedName
            + " : IAsnCodec");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine("internal IAsnCodec Implementation;");
        writer.WriteLine("");
        this.EmitConstructor(writer, facadeName);
        writer.WriteLine("");
        this.EmitEncode(writer);
        writer.WriteLine("");
        this.EmitDecode(writer);
        writer.WriteLine("");
        this.EmitTryDecodeSpan(writer);
        writer.WriteLine("");
        this.EmitEncodeToSpan(writer);
        writer.WriteLine("");
        this.EmitCheckCanonicality(writer);
        writer.WriteLine("");
        this.EmitTranscode(writer);
        writer.WriteLine("");
        this.EmitCreateStreamingCodec(writer);
        writer.WriteLine("");
        this.EmitCreateIncrementalDecoder(writer);
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitConstructor(
        CSharpTextWriter writer,
        CSharpNameMapping facadeName)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Initializes a codec configured for the generated schema.");
        writer.WriteLine(
            "/// <param name=\"provider\">The configured codec provider.</param>");
        writer.WriteLine(
            "/// <param name=\"catalog\">The generated reflection catalog.</param>");
        writer.WriteLine(
            "/// <param name=\"options\">The codec options snapshot.</param>");
        writer.WriteLine("public " + facadeName.GeneratedName + "(");
        writer.IncreaseIndentation();
        writer.WriteLine("IAsnCodecProvider provider,");
        writer.WriteLine("AsnReflectionCatalog catalog,");
        writer.WriteLine("AsnCodecOptions options)");
        writer.DecreaseIndentation();
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine("if (provider == null)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "throw new System.ArgumentNullException(\"provider\");");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
        writer.WriteLine("");
        writer.WriteLine("if (catalog == null)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "throw new System.ArgumentNullException(\"catalog\");");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
        writer.WriteLine("");
        writer.WriteLine("if (options == null)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "throw new System.ArgumentNullException(\"options\");");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
        writer.WriteLine("");
        writer.WriteLine(
            "this.Implementation = provider.CreateCodec(catalog, options);");
        writer.WriteLine("if (this.Implementation == null)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "throw new System.InvalidOperationException(\"The codec provider returned no implementation.\");");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitEncode(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(writer, "Encodes one runtime value.");
        writer.WriteLine(
            "/// <param name=\"request\">The encode request.</param>");
        writer.WriteLine("/// <returns>The encode result.</returns>");
        writer.WriteLine(
            "public AsnEncodeResult Encode(AsnEncodeRequest request)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine("return this.Implementation.Encode(request);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitDecode(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(writer, "Decodes one encoded value.");
        writer.WriteLine(
            "/// <param name=\"request\">The decode request.</param>");
        writer.WriteLine("/// <returns>The decode result.</returns>");
        writer.WriteLine(
            "public AsnDecodeResult Decode(AsnDecodeRequest request)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine("return this.Implementation.Decode(request);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitTryDecodeSpan(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Attempts to decode one value from a read-only byte span.");
        writer.WriteLine(
            "/// <param name=\"input\">The encoded byte span.</param>");
        writer.WriteLine(
            "/// <param name=\"typeName\">The qualified schema type name.</param>");
        writer.WriteLine(
            "/// <param name=\"options\">The codec options.</param>");
        writer.WriteLine("/// <returns>The decode result.</returns>");
        writer.WriteLine(
            "public AsnDecodeResult TryDecodeSpan(System.ReadOnlySpan<byte> input, string typeName, AsnCodecOptions options)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "return this.Implementation.TryDecodeSpan(input, typeName, options);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitEncodeToSpan(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Encodes one value into a caller-provided byte span.");
        writer.WriteLine(
            "/// <param name=\"request\">The encode request.</param>");
        writer.WriteLine(
            "/// <param name=\"destination\">The destination byte span.</param>");
        writer.WriteLine("/// <returns>The encode result.</returns>");
        writer.WriteLine(
            "public AsnEncodeResult EncodeToSpan(AsnEncodeRequest request, System.Span<byte> destination)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "return this.Implementation.EncodeToSpan(request, destination);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitCheckCanonicality(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Checks whether one encoded value is canonical.");
        writer.WriteLine(
            "/// <param name=\"request\">The canonicality request.</param>");
        writer.WriteLine("/// <returns>The canonicality result.</returns>");
        writer.WriteLine(
            "public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "return this.Implementation.CheckCanonicality(request);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitTranscode(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Transcodes one encoded value through its normalized runtime representation.");
        writer.WriteLine(
            "/// <param name=\"request\">The transcode request.</param>");
        writer.WriteLine("/// <returns>The transcode result.</returns>");
        writer.WriteLine(
            "public AsnTranscodeResult Transcode(AsnTranscodeRequest request)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "return this.Implementation.Transcode(request);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitCreateStreamingCodec(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Creates an independent stream-operation adapter.");
        writer.WriteLine(
            "/// <returns>A streaming codec with operation-local buffering.</returns>");
        writer.WriteLine(
            "public IAsnStreamingCodec CreateStreamingCodec()");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "return this.Implementation.CreateStreamingCodec();");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitCreateIncrementalDecoder(CSharpTextWriter writer)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Creates an independent incremental decoder state machine.");
        writer.WriteLine(
            "/// <param name=\"typeName\">The qualified schema type name.</param>");
        writer.WriteLine(
            "/// <param name=\"options\">The decoder options snapshot.</param>");
        writer.WriteLine(
            "/// <returns>A new incremental decoder with isolated state.</returns>");
        writer.WriteLine(
            "public IAsnIncrementalDecoder CreateIncrementalDecoder(string typeName, AsnCodecOptions options)");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        writer.WriteLine(
            "return this.Implementation.CreateIncrementalDecoder(typeName, options);");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }
}
