namespace gltf;

/// <summary>Receives strongly typed glTF document and enum values from generic inspection workflows.</summary>
public interface IGltfEnumVisitor
{
    /// <summary>Visits a glTF document.</summary>
    /// <param name="value">The document to visit.</param>
    void VisitDocument(GltfDocument value);

    /// <summary>Visits a container format value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitContainerFormat(GltfContainerFormat value);

    /// <summary>Visits an accessor component type value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitComponentType(GltfComponentType value);

    /// <summary>Visits an accessor shape value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitAccessorType(GltfAccessorType value);

    /// <summary>Visits a primitive topology value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitPrimitiveMode(GltfPrimitiveMode value);

    /// <summary>Visits a buffer view target value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitBufferViewTarget(GltfBufferViewTarget value);

    /// <summary>Visits a material alpha mode value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitAlphaMode(GltfAlphaMode value);

    /// <summary>Visits a sampler filter value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitSamplerFilter(GltfSamplerFilter value);

    /// <summary>Visits a sampler wrapping value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitSamplerWrap(GltfSamplerWrap value);

    /// <summary>Visits a camera type value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitCameraType(GltfCameraType value);

    /// <summary>Visits an animation interpolation value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitAnimationInterpolation(GltfAnimationInterpolation value);

    /// <summary>Visits an animation target path value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitAnimationPath(GltfAnimationPath value);

    /// <summary>Visits a validation severity value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitValidationSeverity(GltfValidationSeverity value);

    /// <summary>Visits a URI classification value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitUriKind(GltfUriKind value);

    /// <summary>Visits a resource kind value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitResourceKind(GltfResourceKind value);

    /// <summary>Visits an image format value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitImageFormat(GltfImageFormat value);

    /// <summary>Visits a resource representation value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitResourceMode(GltfResourceMode value);

    /// <summary>Visits an extension status value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitExtensionStatus(GltfExtensionStatus value);

    /// <summary>Visits a transform kind value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitTransformKind(GltfTransformKind value);

    /// <summary>Visits an animation wrap mode value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitAnimationWrapMode(GltfAnimationWrapMode value);

    /// <summary>Visits an operation status value.</summary>
    /// <param name="value">The value to visit.</param>
    void VisitOperationStatus(GltfOperationStatus value);
}
