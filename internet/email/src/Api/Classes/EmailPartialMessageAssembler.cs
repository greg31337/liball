using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Provides operations for working with partial message assembler.
/// </summary>
public class EmailPartialMessageAssembler : IEmailPartialMessageAssemblerContract
{
    internal static EmailPartialMessageAssemblyResult AssembleImplementationWithFragments(List<EmailMessage> fragments)
    {
        return AssembleImplementationWithFragmentsAndOptions(fragments, new EmailPartialMessageAssemblyOptions());
    }

    /// <summary>
    /// Assembles the supplied value.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    public EmailPartialMessageAssemblyResult AssembleWithFragments(List<EmailMessage> fragments)
    {
        return EmailPartialMessageAssembler.AssembleImplementationWithFragments(fragments);
    }

    internal static EmailPartialMessageAssemblyResult AssembleImplementationWithFragmentsAndOptions(List<EmailMessage> fragments, EmailPartialMessageAssemblyOptions options)
    {
        if (fragments == null)
            throw new ArgumentNullException("fragments");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailPartialMessageCollector collector = new EmailPartialMessageCollectorOperations().CreateWithOptions(options);
        int index = 0;
        while (index < fragments.Count)
        {
            if (fragments[index] == null)
                throw new ArgumentException("Fragment lists cannot contain null entries.", "fragments");
            collector.Add(fragments[index]);
            index++;
        }

        if (collector.GetGroupCount() != 1)
            throw new ArgumentException("A single assembly operation requires fragments from exactly one id group.", "fragments");
        return collector.Assemble(collector.GetGroupIdAt(0));
    }

    /// <summary>
    /// Assembles the supplied value.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPartialMessageAssemblyResult AssembleWithFragmentsAndOptions(List<EmailMessage> fragments, EmailPartialMessageAssemblyOptions options)
    {
        return EmailPartialMessageAssembler.AssembleImplementationWithFragmentsAndOptions(fragments, options);
    }

    internal static EmailPartialMessageAssemblyResult AssembleEntitiesImplementationWithFragments(List<MimeEntity> fragments)
    {
        return AssembleEntitiesImplementationWithFragmentsAndOptions(fragments, new EmailPartialMessageAssemblyOptions());
    }

    /// <summary>
    /// Assembles the entities.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    public EmailPartialMessageAssemblyResult AssembleEntitiesWithFragments(List<MimeEntity> fragments)
    {
        return EmailPartialMessageAssembler.AssembleEntitiesImplementationWithFragments(fragments);
    }

    internal static EmailPartialMessageAssemblyResult AssembleEntitiesImplementationWithFragmentsAndOptions(List<MimeEntity> fragments, EmailPartialMessageAssemblyOptions options)
    {
        if (fragments == null)
            throw new ArgumentNullException("fragments");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailPartialMessageCollector collector = new EmailPartialMessageCollectorOperations().CreateWithOptions(options);
        int index = 0;
        while (index < fragments.Count)
        {
            if (fragments[index] == null)
                throw new ArgumentException("Fragment lists cannot contain null entries.", "fragments");
            collector.AddEntity(fragments[index]);
            index++;
        }

        if (collector.GetGroupCount() != 1)
            throw new ArgumentException("A single assembly operation requires fragments from exactly one id group.", "fragments");
        return collector.Assemble(collector.GetGroupIdAt(0));
    }

    /// <summary>
    /// Assembles the entities.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPartialMessageAssemblyResult AssembleEntitiesWithFragmentsAndOptions(List<MimeEntity> fragments, EmailPartialMessageAssemblyOptions options)
    {
        return EmailPartialMessageAssembler.AssembleEntitiesImplementationWithFragmentsAndOptions(fragments, options);
    }
}
