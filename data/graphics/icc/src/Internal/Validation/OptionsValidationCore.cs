namespace icc;

internal class OptionsValidationCore
{
    internal OptionsValidationCore()
    {
    }

    internal IccOperationResult ValidateRead(IccReadOptions options)
    {
        if (options == null)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "Profile read options cannot be null.");
        }
        if (options.ResourceLimits == null)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "Profile read resource limits cannot be null.");
        }
        if (options.Mode != IccReadMode.Strict
            && options.Mode != IccReadMode.Conforming
            && options.Mode != IccReadMode.Recovery)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "The profile read mode is not defined.");
        }
        if (options.ValidationMode != IccValidationMode.Tolerant
            && options.ValidationMode != IccValidationMode.Conforming
            && options.ValidationMode != IccValidationMode.SecurityStrict)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "The read validation mode is not defined.");
        }
        return ValidateLimits(options.ResourceLimits, "reading");
    }

    internal IccOperationResult ValidateWrite(IccWriteOptions options)
    {
        if (options == null)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "Profile write options cannot be null.");
        }
        if (options.ResourceLimits == null)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "Profile write resource limits cannot be null.");
        }
        if (options.Mode != IccWriteMode.Canonical
            && options.Mode != IccWriteMode.Preservation)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                "The profile write mode is not defined.");
        }
        return ValidateLimits(options.ResourceLimits, "writing");
    }

    internal IccOperationResult ValidateLimits(IccResourceLimits limits,
        string operation)
    {
        if (limits.MaximumProfileBytes < IccConstants.MinimumProfileSize)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                $"The maximum profile size for {operation} must be at least {IccConstants.MinimumProfileSize} bytes.");
        }
        if (limits.MaximumCarrierBytes < 0)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                $"The maximum carrier size for {operation} cannot be negative.");
        }
        if (limits.MaximumTagCount < 0 || limits.MaximumTagBytes < 0
            || limits.MaximumTotalTagBytes < 0
            || limits.MaximumNestingDepth < 0
            || limits.MaximumDecompressedBytes < 0
            || limits.MaximumCalculatorInstructions < 0
            || limits.MaximumCalculatorStackDepth < 0
            || limits.MaximumCalculatorStorageValues < 0
            || limits.MaximumDiagnosticCount < 0)
        {
            return CreateResult(IccOperationStatus.InvalidData,
                $"Tag, payload, nesting, decompression, calculator, and diagnostic limits for {operation} cannot be negative.");
        }
        return CreateResult(IccOperationStatus.Success,
            $"The profile {operation} options and resource limits are valid.");
    }

    internal IccOperationResult CreateResult(IccOperationStatus status,
        string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }
}
