using System;
using System.Collections.Generic;

namespace gltf;

internal class ExtensionSemanticValidator
{
    internal ExtensionUsageCollector Usage = new ExtensionUsageCollector();
    internal ExtensionHandlerDispatcher Dispatcher = new ExtensionHandlerDispatcher();
    internal ExtensionContextLocator Locations = new ExtensionContextLocator();

    internal GltfValidationReport ValidateDocumentExtensions(
        GltfDocument document,
        List<IGltfExtensionHandler> handlers)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (handlers == null)
        {
            throw new ArgumentNullException("handlers");
        }

        GltfValidationReport result = new GltfValidationReport();
        List<EncounteredExtension> extensions = Usage.CollectEncounteredExtensionLocations(document);
        int extensionIndex = 0;
        while (extensionIndex < extensions.Count)
        {
            EncounteredExtension extension = extensions[extensionIndex];
            string objectPointer = GetObjectPointer(extension.JsonPointer);
            GltfExtensionContext context = Locations.CreateContext(
                document,
                extension.Name,
                extension.Json,
                objectPointer,
                document.Resources);
            GltfExtensionResult validation = Dispatcher.DispatchValidation(context, handlers);
            AppendDiagnostics(result, validation.Diagnostics);
            if (validation.Status == GltfOperationStatus.Invalid
                || validation.Status == GltfOperationStatus.Failed)
            {
                result.IsValid = false;
                if (validation.Diagnostics.Count == 0)
                {
                    AddStatusIssue(result, extension, validation.Status);
                }
            }
            else if (validation.Status == GltfOperationStatus.Unavailable
                && validation.Diagnostics.Count == 0)
            {
                AddUnavailableIssue(result, extension);
            }

            extensionIndex++;
        }

        return result;
    }

    internal string GetObjectPointer(string extensionPointer)
    {
        int marker = extensionPointer.LastIndexOf("/extensions/", StringComparison.Ordinal);
        if (marker < 0)
        {
            return "";
        }

        return extensionPointer.Substring(0, marker);
    }

    internal void AppendDiagnostics(
        GltfValidationReport result,
        List<GltfValidationIssue> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            GltfValidationIssue issue = diagnostics[index];
            result.Issues.Add(issue);
            if (issue.Severity == GltfValidationSeverity.Error)
            {
                result.IsValid = false;
            }

            index++;
        }
    }

    internal void AddStatusIssue(
        GltfValidationReport result,
        EncounteredExtension extension,
        GltfOperationStatus status)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = GltfIssueCodes.ExtensionHandlerUnavailable;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = $"Extension handler validation for '{extension.Name}' ended with status {status}.";
        issue.JsonPointer = extension.JsonPointer;
        result.Issues.Add(issue);
    }

    internal void AddUnavailableIssue(
        GltfValidationReport result,
        EncounteredExtension extension)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = GltfIssueCodes.ExtensionHandlerUnavailable;
        issue.Severity = GltfValidationSeverity.Warning;
        issue.Message = $"Extension handler validation is unavailable for '{extension.Name}'.";
        issue.JsonPointer = extension.JsonPointer;
        result.Issues.Add(issue);
    }
}
