namespace iso20022;

/// <summary>Executes explicit inspectable mappings between ISO messages and canonical models.</summary>
public interface IMessageMapper
{
    /// <summary>Maps one raw, generic, or typed source through explicit ordered rules.</summary>
    /// <param name="request">The source representation, mapping, target selection, and validation policy.</param>
    /// <returns>The mapped output and complete rule-application report.</returns>
    MappingResult MapMessage(MappingRequest? request);
}
