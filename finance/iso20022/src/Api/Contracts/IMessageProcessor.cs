namespace iso20022;

/// <summary>Composes caller-selected message processing stages without discarding stage results.</summary>
public interface IMessageProcessor
{
    /// <summary>Processes one raw, generic, generated, or canonical-model input.</summary>
    /// <param name="request">The input, selected stages, exact identities, and stage requests.</param>
    /// <returns>The complete reached-stage results and final representation.</returns>
    ProcessingResult ProcessMessage(ProcessingRequest? request);
}
