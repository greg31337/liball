namespace gltf;

/// <summary>Provides built-in glTF extension metadata and lossless fallback behavior.</summary>
public class GltfExtensionHandler : IGltfExtensionHandler
{
    internal ExtensionRegistry Registry;
    internal ExtensionFallback Fallback;
    internal SceneExtensionCodec SceneExtensions;
    internal MaterialExtensionCodec MaterialExtensions;
    internal CompressionExtensionCodec CompressionExtensions;
    internal ImageExtensionCodec ImageExtensions;
    internal XmpJsonLdExtensionCodec XmpJsonLdExtensions;
    internal AdvancedExtensionCodec AdvancedExtensions;
    internal ExtensionSchemaValidator ExtensionSchemas;
    internal ArchivedExtensionPolicy ArchivedExtensions;

    /// <summary>Initializes a built-in extension handler.</summary>
    public GltfExtensionHandler()
    {
        Registry = new ExtensionRegistry();
        Fallback = new ExtensionFallback();
        SceneExtensions = new SceneExtensionCodec();
        MaterialExtensions = new MaterialExtensionCodec();
        CompressionExtensions = new CompressionExtensionCodec();
        ImageExtensions = new ImageExtensionCodec();
        XmpJsonLdExtensions = new XmpJsonLdExtensionCodec();
        AdvancedExtensions = new AdvancedExtensionCodec();
        ExtensionSchemas = new ExtensionSchemaValidator();
        ArchivedExtensions = new ArchivedExtensionPolicy();
    }

    /// <summary>Describes the supported extension.</summary>
    /// <returns>The extension descriptor.</returns>
    public GltfExtensionDescriptor DescribeExtension()
    {
        GltfExtensionDescriptor result = new GltfExtensionDescriptor();
        result.Name = "*";
        result.Version = "2.0";
        result.ObjectLocations.Add("glTFProperty");
        result.AllowsFallback = true;
        return result;
    }

    /// <summary>Reads extension data.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <returns>The extension operation result.</returns>
    public GltfExtensionResult ReadExtension(GltfExtensionContext context)
    {
        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }

        GltfExtensionResult result;
        if (SceneExtensions.HandlesSceneExtension(context.Name))
        {
            result = SceneExtensions.ReadTypedSceneExtension(context, Registry);
        }
        else if (MaterialExtensions.HandlesMaterialExtension(context.Name))
        {
            result = MaterialExtensions.ReadTypedMaterialExtension(context, Registry);
        }
        else if (CompressionExtensions.HandlesCompressionExtension(context.Name))
        {
            result = CompressionExtensions.ReadTypedCompressionExtension(context, Registry);
        }
        else if (ImageExtensions.HandlesImageExtension(context.Name))
        {
            result = ImageExtensions.ReadTypedImageExtension(context, Registry);
        }
        else if (XmpJsonLdExtensions.HandlesXmpJsonLdExtension(context.Name))
        {
            result = XmpJsonLdExtensions.ReadTypedXmpJsonLdExtension(context, Registry);
        }
        else if (AdvancedExtensions.HandlesAdvancedExtension(context.Name))
        {
            result = AdvancedExtensions.ReadTypedAdvancedExtension(context, Registry);
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, Registry);
        }

        return ArchivedExtensions.AppendArchivedWarning(context, result);
    }

    /// <summary>Writes extension data.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <returns>The extension operation result.</returns>
    public GltfExtensionResult WriteExtension(GltfExtensionContext context)
    {
        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }

        GltfExtensionResult result;
        if (SceneExtensions.HandlesSceneExtension(context.Name))
        {
            result = SceneExtensions.WriteTypedSceneExtension(context, Registry);
        }
        else if (MaterialExtensions.HandlesMaterialExtension(context.Name))
        {
            result = MaterialExtensions.WriteTypedMaterialExtension(context, Registry);
        }
        else if (CompressionExtensions.HandlesCompressionExtension(context.Name))
        {
            result = CompressionExtensions.WriteTypedCompressionExtension(context, Registry);
        }
        else if (ImageExtensions.HandlesImageExtension(context.Name))
        {
            result = ImageExtensions.WriteTypedImageExtension(context, Registry);
        }
        else if (XmpJsonLdExtensions.HandlesXmpJsonLdExtension(context.Name))
        {
            result = XmpJsonLdExtensions.WriteTypedXmpJsonLdExtension(context, Registry);
        }
        else if (AdvancedExtensions.HandlesAdvancedExtension(context.Name))
        {
            result = AdvancedExtensions.WriteTypedAdvancedExtension(context, Registry);
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, Registry);
        }

        return ArchivedExtensions.AppendArchivedWarning(context, result);
    }

    /// <summary>Validates extension data.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <returns>The extension operation result.</returns>
    public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
    {
        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }

        GltfExtensionResult result;
        if (SceneExtensions.HandlesSceneExtension(context.Name))
        {
            result = SceneExtensions.ValidateTypedSceneExtension(context, Registry);
        }
        else if (MaterialExtensions.HandlesMaterialExtension(context.Name))
        {
            result = MaterialExtensions.ValidateTypedMaterialExtension(context, Registry);
        }
        else if (CompressionExtensions.HandlesCompressionExtension(context.Name))
        {
            result = CompressionExtensions.ValidateTypedCompressionExtension(context, Registry);
        }
        else if (ImageExtensions.HandlesImageExtension(context.Name))
        {
            result = ImageExtensions.ValidateTypedImageExtension(context, Registry);
        }
        else if (XmpJsonLdExtensions.HandlesXmpJsonLdExtension(context.Name))
        {
            result = XmpJsonLdExtensions.ValidateTypedXmpJsonLdExtension(context, Registry);
        }
        else if (AdvancedExtensions.HandlesAdvancedExtension(context.Name))
        {
            result = AdvancedExtensions.ValidateTypedAdvancedExtension(context, Registry);
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, Registry);
        }

        result = ExtensionSchemas.ApplySchemaValidation(context, result, Registry);
        return ArchivedExtensions.AppendArchivedWarning(context, result);
    }

    /// <summary>Remaps extension index references after a document edit.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <param name="remap">The index remap.</param>
    /// <returns>The extension operation result.</returns>
    public GltfExtensionResult RemapExtensionReferences(GltfExtensionContext context, GltfIndexRemap remap)
    {
        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }

        if (remap == null)
        {
            throw new System.ArgumentNullException("remap");
        }

        GltfExtensionResult result;
        if (SceneExtensions.HandlesSceneExtension(context.Name))
        {
            result = SceneExtensions.RemapTypedSceneExtension(context, remap, Registry);
        }
        else if (MaterialExtensions.HandlesMaterialExtension(context.Name))
        {
            result = MaterialExtensions.RemapTypedMaterialExtension(context, remap, Registry);
        }
        else if (CompressionExtensions.HandlesCompressionExtension(context.Name))
        {
            result = CompressionExtensions.RemapTypedCompressionExtension(
                context,
                remap,
                Registry);
        }
        else if (ImageExtensions.HandlesImageExtension(context.Name))
        {
            result = ImageExtensions.RemapTypedImageExtension(context, remap, Registry);
        }
        else if (XmpJsonLdExtensions.HandlesXmpJsonLdExtension(context.Name))
        {
            result = XmpJsonLdExtensions.RemapTypedXmpJsonLdExtension(
                context,
                remap,
                Registry);
        }
        else if (AdvancedExtensions.HandlesAdvancedExtension(context.Name))
        {
            result = AdvancedExtensions.RemapTypedAdvancedExtension(context, Registry);
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, Registry);
            result.Status = GltfOperationStatus.Unavailable;
            GltfValidationIssue issue = new GltfValidationIssue();
            issue.Code = GltfIssueCodes.ExtensionHandlerUnavailable;
            issue.Severity = GltfValidationSeverity.Warning;
            issue.Message = $"Extension '{context.Name}' has no handler that can remap its index references.";
            issue.JsonPointer = Fallback.CreateExtensionPointer(context);
            result.Diagnostics.Add(issue);
        }

        return ArchivedExtensions.AppendArchivedWarning(context, result);
    }
}
