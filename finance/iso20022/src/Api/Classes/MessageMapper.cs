namespace iso20022;

/// <summary>Executes explicit inspectable mappings without implicit reflection-based conversion.</summary>
public class MessageMapper : IMessageMapper
{
    /// <summary>Initializes a deterministic message mapper.</summary>
    public MessageMapper()
    {
    }

    /// <summary>Maps one raw, generic, or explicitly represented canonical source.</summary>
    /// <param name="request">The source representation, mapping definition, target, and validation policy.</param>
    /// <returns>The mapped output and complete rule-application report.</returns>
    public MappingResult MapMessage(MappingRequest? request)
    {
        if (request == null)
        {
            MappingResult missing = new MappingResult();
            missing.ErrorCode = "mapping.request_missing";
            missing.ErrorMessage = "A mapping execution request is required.";
            return missing;
        }

        return MappingExecutor.ExecuteMapping(request);
    }
}
