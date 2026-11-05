using System;
using System.Collections.Generic;

namespace gltf;

internal class ExtensionHandlerDispatcher
{
    internal IGltfExtensionHandler BuiltInHandler = new GltfExtensionHandler();

    internal GltfExtensionResult DispatchReadExtension(
        GltfExtensionContext context,
        List<IGltfExtensionHandler> handlers)
    {
        ExtensionHandlerSelection selection = SelectHandler(context, handlers);
        GltfExtensionResult? result = selection.Handler.ReadExtension(context);
        return RequireResult(result, context, selection.Descriptor);
    }

    internal GltfExtensionResult DispatchWriteExtension(
        GltfExtensionContext context,
        List<IGltfExtensionHandler> handlers)
    {
        ExtensionHandlerSelection selection = SelectHandler(context, handlers);
        GltfExtensionResult? result = selection.Handler.WriteExtension(context);
        return RequireResult(result, context, selection.Descriptor);
    }

    internal GltfExtensionResult DispatchValidation(
        GltfExtensionContext context,
        List<IGltfExtensionHandler> handlers)
    {
        ExtensionHandlerSelection selection = SelectHandler(context, handlers);
        GltfExtensionResult? result = selection.Handler.ValidateExtension(context);
        return RequireResult(result, context, selection.Descriptor);
    }

    internal GltfResourceSet EnumerateExtensionResources(
        GltfExtensionContext context,
        List<IGltfExtensionHandler> handlers)
    {
        return DispatchValidation(context, handlers).Resources;
    }

    internal List<GltfDependency> EnumerateExtensionReferences(
        GltfExtensionContext context,
        List<IGltfExtensionHandler> handlers)
    {
        return DispatchValidation(context, handlers).References;
    }

    internal GltfExtensionResult DispatchReferenceRemap(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        List<IGltfExtensionHandler> handlers)
    {
        if (remap == null)
        {
            throw new ArgumentNullException("remap");
        }

        ExtensionHandlerSelection selection = SelectHandler(context, handlers);
        GltfExtensionResult? result = selection.Handler.RemapExtensionReferences(context, remap);
        return RequireResult(result, context, selection.Descriptor);
    }

    internal ExtensionHandlerSelection SelectHandler(
        GltfExtensionContext context,
        List<IGltfExtensionHandler> handlers)
    {
        ValidateInputs(context, handlers);
        ExtensionHandlerSelection? result = null;
        int index = 0;
        while (index < handlers.Count)
        {
            IGltfExtensionHandler handler = handlers[index];
            GltfExtensionDescriptor? descriptor = handler.DescribeExtension();
            if (descriptor == null)
            {
                throw new GltfUnsupportedFeatureException(
                    $"Extension handler {index} returned a null descriptor.");
            }

            if (descriptor.Name == null || descriptor.Name.Length == 0)
            {
                throw new GltfUnsupportedFeatureException(
                    $"Extension handler {index} returned an empty extension name.");
            }

            if (string.Equals(descriptor.Name, context.Name, StringComparison.Ordinal))
            {
                if (result != null)
                {
                    throw new GltfUnsupportedFeatureException(
                        $"More than one extension handler is registered for '{context.Name}'.");
                }

                result = new ExtensionHandlerSelection(handler, descriptor);
            }

            index++;
        }

        if (result == null)
        {
            result = new ExtensionHandlerSelection(
                BuiltInHandler,
                BuiltInHandler.DescribeExtension());
        }

        return result;
    }

    internal void ValidateInputs(
        GltfExtensionContext context,
        List<IGltfExtensionHandler> handlers)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (handlers == null)
        {
            throw new ArgumentNullException("handlers");
        }

        if (context.Name == null || context.Name.Length == 0)
        {
            throw new ArgumentException("The extension context name must not be empty.", "context");
        }

        int index = 0;
        while (index < handlers.Count)
        {
            if (handlers[index] == null)
            {
                throw new ArgumentException(
                    $"Extension handler {index} must not be null.",
                    "handlers");
            }

            index++;
        }
    }

    internal GltfExtensionResult RequireResult(
        GltfExtensionResult? result,
        GltfExtensionContext context,
        GltfExtensionDescriptor descriptor)
    {
        if (result == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"The extension handler for '{context.Name}' returned a null result.");
        }

        if (result.Resources == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"The extension handler for '{context.Name}' returned null resources.");
        }

        if (result.References == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"The extension handler for '{context.Name}' returned null references.");
        }

        if (result.Diagnostics == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"The extension handler for '{context.Name}' returned null diagnostics.");
        }

        if (result.Descriptor == null)
        {
            result.Descriptor = descriptor;
        }

        return result;
    }
}

internal class ExtensionHandlerSelection
{
    internal IGltfExtensionHandler Handler;
    internal GltfExtensionDescriptor Descriptor;

    internal ExtensionHandlerSelection(
        IGltfExtensionHandler handler,
        GltfExtensionDescriptor descriptor)
    {
        Handler = handler;
        Descriptor = descriptor;
    }
}
