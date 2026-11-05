namespace fix;

/// <summary>Receives dictionary-identified administrative and application messages.</summary>
public interface IMessageCrackHandler
{
    /// <summary>Handles an identified administrative message.</summary>
    /// <param name="context">The identified message and dictionary metadata.</param>
    /// <returns>The handler's processing decision.</returns>
    ApplicationDecision HandleAdministrativeMessage(MessageCrackContext context);
    /// <summary>Handles an identified application message.</summary>
    /// <param name="context">The identified message and dictionary metadata.</param>
    /// <returns>The handler's processing decision.</returns>
    ApplicationDecision HandleApplicationMessage(MessageCrackContext context);
}
