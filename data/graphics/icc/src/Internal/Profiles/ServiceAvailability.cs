namespace icc;

internal class ServiceAvailability
{
    internal string ServiceName;

    internal ServiceAvailability(string serviceName)
    {
        ServiceName = serviceName;
    }

    internal IccOperationResult CreateUnsupportedResult()
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = IccOperationStatus.UnsupportedFeature;
        result.Message = $"{ServiceName} is not available for this input.";
        return result;
    }

    internal IccUnsupportedFeatureException CreateUnsupportedException()
    {
        return new IccUnsupportedFeatureException($"{ServiceName} is not available for this input.");
    }

    internal int GetUnavailableChannelCount()
    {
        return 0;
    }

    internal IccEmbeddedFormat GetUnknownEmbeddedFormat()
    {
        return IccEmbeddedFormat.Unknown;
    }
}
