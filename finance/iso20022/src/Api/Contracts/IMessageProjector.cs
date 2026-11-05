namespace iso20022;

/// <summary>Projects exact ISO 20022 messages into stable task-oriented views.</summary>
public interface IMessageProjector
{
    /// <summary>Projects one raw, generic, or generated source message.</summary>
    /// <param name="request">The source, exact identity, projection, and validation policy.</param>
    /// <returns>The projected view with source paths, unmapped data, and validation.</returns>
    ProjectionResult ProjectMessage(ProjectionRequest? request);
}
