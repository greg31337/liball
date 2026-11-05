namespace gltf;

internal class ArchivedExtensionPolicy
{
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal bool IsArchivedExtension(string name)
    {
        return name == GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness
            || name == GltfExtensionNames.KhrTechniquesWebgl
            || name == GltfExtensionNames.KhrXmp;
    }

    internal GltfExtensionResult AppendArchivedWarning(
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        if (!IsArchivedExtension(context.Name))
        {
            return result;
        }

        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = GltfIssueCodes.ArchivedExtension;
        issue.Severity = GltfValidationSeverity.Warning;
        issue.Message = $"Extension '{context.Name}' is archived and is retained for compatibility only.";
        issue.JsonPointer = Fallback.CreateExtensionPointer(context);
        result.Diagnostics.Add(issue);
        return result;
    }
}
