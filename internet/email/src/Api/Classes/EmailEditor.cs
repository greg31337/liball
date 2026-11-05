using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Applies high-level edits to an existing message and tracks the resulting structural changes.
/// </summary>
public class EmailEditor : IEmailEditorContract
{
    internal static MimeEntity WrapInMultipartImplementationWithMessageAndSubtype(EmailMessage message, string subtype)
    {
        ValidateMessageBody(message);
        MimeEntity? body = message.GetBody();
        List<MimeEntity> children = new List<MimeEntity>();
        children.Add((body ?? throw new InvalidOperationException()));
        MimeEntity wrapper = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren(subtype, children);
        message.SetBody(wrapper);
        return wrapper;
    }

    /// <summary>
    /// Wraps the in multipart.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity WrapInMultipartWithMessageAndSubtype(EmailMessage message, string subtype)
    {
        return EmailEditor.WrapInMultipartImplementationWithMessageAndSubtype(message, subtype);
    }

    internal static MimeEntity WrapInMultipartImplementationWithParentAndPartIndexAndSubtype(MimeEntity? parent, int partIndex, string subtype)
    {
        ValidatePartContainer(parent);
        MimeEntity part = (parent ?? throw new InvalidOperationException()).GetChildAt(partIndex);
        MimeEntity wrapper = new EmailEntityFactory().CreateMultipartWithSubtype(subtype);
        parent.ReplaceChildAt(partIndex, wrapper);
        wrapper.AddChild(part);
        return wrapper;
    }

    /// <summary>
    /// Wraps the in multipart.
    /// </summary>
    /// <param name = "parent">The parent used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity WrapInMultipartWithParentAndPartIndexAndSubtype(MimeEntity? parent, int partIndex, string subtype)
    {
        return EmailEditor.WrapInMultipartImplementationWithParentAndPartIndexAndSubtype(parent, partIndex, subtype);
    }

    internal static MimeEntity UnwrapMultipartImplementationWithMessage(EmailMessage message)
    {
        ValidateMessageBody(message);
        MimeEntity? wrapper = message.GetBody();
        ValidateUnwrappable(wrapper);
        MimeEntity part = (wrapper ?? throw new InvalidOperationException()).RemoveChildAt(0);
        message.SetBody(part);
        return part;
    }

    /// <summary>
    /// Unwraps the multipart.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity UnwrapMultipartWithMessage(EmailMessage message)
    {
        return EmailEditor.UnwrapMultipartImplementationWithMessage(message);
    }

    internal static MimeEntity UnwrapMultipartImplementationWithParentAndPartIndex(MimeEntity parent, int partIndex)
    {
        ValidatePartContainer(parent);
        MimeEntity wrapper = parent.GetChildAt(partIndex);
        ValidateUnwrappable(wrapper);
        MimeEntity part = wrapper.RemoveChildAt(0);
        parent.ReplaceChildAt(partIndex, part);
        return part;
    }

    /// <summary>
    /// Unwraps the multipart.
    /// </summary>
    /// <param name = "parent">The parent used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity UnwrapMultipartWithParentAndPartIndex(MimeEntity parent, int partIndex)
    {
        return EmailEditor.UnwrapMultipartImplementationWithParentAndPartIndex(parent, partIndex);
    }

    internal static MimeEntity AddPartImplementation(MimeEntity multipart, MimeEntity part)
    {
        ValidatePartContainer(multipart);
        multipart.AddChild(part);
        return part;
    }

    /// <summary>
    /// Adds a part to the current collection or model.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "part">The part used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity AddPart(MimeEntity multipart, MimeEntity part)
    {
        return EmailEditor.AddPartImplementation(multipart, part);
    }

    internal static MimeEntity InsertPartImplementation(MimeEntity multipart, int partIndex, MimeEntity part)
    {
        ValidatePartContainer(multipart);
        multipart.InsertChild(partIndex, part);
        return part;
    }

    /// <summary>
    /// Inserts a part at the requested position.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <param name = "part">The part used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity InsertPart(MimeEntity multipart, int partIndex, MimeEntity part)
    {
        return EmailEditor.InsertPartImplementation(multipart, partIndex, part);
    }

    internal static MimeEntity RemovePartAtImplementation(MimeEntity multipart, int partIndex)
    {
        ValidatePartContainer(multipart);
        return multipart.RemoveChildAt(partIndex);
    }

    /// <summary>
    /// Removes the matching part at value or values.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity RemovePartAt(MimeEntity multipart, int partIndex)
    {
        return EmailEditor.RemovePartAtImplementation(multipart, partIndex);
    }

    internal static MimeEntity MovePartImplementation(MimeEntity multipart, int fromIndex, int toIndex)
    {
        ValidatePartContainer(multipart);
        MimeEntity part = multipart.GetChildAt(fromIndex);
        multipart.MoveChild(fromIndex, toIndex);
        return part;
    }

    /// <summary>
    /// Moves the part.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "fromIndex">The zero-based from index.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity MovePart(MimeEntity multipart, int fromIndex, int toIndex)
    {
        return EmailEditor.MovePartImplementation(multipart, fromIndex, toIndex);
    }

    internal static MimeEntity ReplacePartAtImplementation(MimeEntity multipart, int partIndex, MimeEntity replacement)
    {
        ValidatePartContainer(multipart);
        return multipart.ReplaceChildAt(partIndex, replacement);
    }

    /// <summary>
    /// Replaces the matching part at value or values.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "partIndex">The zero-based part index.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity ReplacePartAt(MimeEntity multipart, int partIndex, MimeEntity replacement)
    {
        return EmailEditor.ReplacePartAtImplementation(multipart, partIndex, replacement);
    }

    internal static MimeEntity AddAlternativeImplementation(EmailMessage message, MimeEntity alternative)
    {
        ValidateMessageBody(message);
        ValidateDetachedContent(message, alternative, "alternative");
        MimeEntity primary = FindPrimaryBody(message, true);
        MimeEntity alternatives;
        if (primary.GetKind() == MimeEntityKind.Multipart && (primary.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "alternative"))
        {
            alternatives = primary;
        }
        else
        {
            alternatives = WrapPrimaryBody(message, primary, "alternative");
        }

        alternatives.AddChild(alternative);
        return alternative;
    }

    /// <summary>
    /// Adds an alternative to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "alternative">The alternative used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity AddAlternative(EmailMessage message, MimeEntity alternative)
    {
        return EmailEditor.AddAlternativeImplementation(message, alternative);
    }

    internal static MimeEntity SetAlternativesImplementation(EmailMessage message, List<MimeEntity> alternatives)
    {
        ValidateMessageBody(message);
        ValidateDetachedContents(message, alternatives, "alternatives");
        if (alternatives.Count == 0)
        {
            throw new ArgumentException("At least one alternative is required.", "alternatives");
        }

        MimeEntity primary = FindPrimaryBody(message, true);
        if (primary.GetKind() == MimeEntityKind.Multipart && (primary.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related") && HasExplicitRelatedRoot(primary))
        {
            throw new InvalidOperationException("Alternatives cannot replace the root of multipart/related content selected by an explicit start parameter.");
        }

        MimeEntity replacement = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", alternatives);
        ReplacePrimaryBody(message, primary, replacement);
        return replacement;
    }

    /// <summary>
    /// Sets the alternatives used by this instance.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "alternatives">The alternatives used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public MimeEntity SetAlternatives(EmailMessage message, List<MimeEntity> alternatives)
    {
        return EmailEditor.SetAlternativesImplementation(message, alternatives);
    }

    internal static MimeEntity AddRelatedResourceImplementation(EmailMessage message, MimeEntity resource)
    {
        ValidateMessageBody(message);
        ValidateDetachedContent(message, resource, "resource");
        if (!resource.HasContentId() && !resource.HasContentLocation())
        {
            throw new ArgumentException("A related resource requires a content ID or content location.", "resource");
        }

        MimeEntity? related = FindRelatedBody(message);
        if (related == null)
        {
            MimeEntity primary = FindPrimaryBody(message, false);
            related = WrapPrimaryBody(message, primary, "related");
        }

        related.AddChild(resource);
        return resource;
    }

    /// <summary>
    /// Adds a related resource to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "resource">The resource used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public MimeEntity AddRelatedResource(EmailMessage message, MimeEntity resource)
    {
        return EmailEditor.AddRelatedResourceImplementation(message, resource);
    }

    internal static MimeEntity AddAttachmentImplementation(EmailMessage message, MimeEntity attachment)
    {
        ValidateMessageBody(message);
        ValidateDetachedContent(message, attachment, "attachment");
        MimeEntity? root = message.GetBody();
        MimeEntity mixed;
        if ((root ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Multipart && (root.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "mixed"))
        {
            mixed = root;
        }
        else
        {
            mixed = WrapInMultipartImplementationWithMessageAndSubtype(message, "mixed");
        }

        mixed.AddChild(attachment);
        return attachment;
    }

    /// <summary>
    /// Adds an attachment to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity AddAttachment(EmailMessage message, MimeEntity attachment)
    {
        return EmailEditor.AddAttachmentImplementation(message, attachment);
    }

    internal static MimeEntity DetachContentImplementation(MimeEntity content)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (!content.HasParent())
        {
            throw new InvalidOperationException("The MIME content is not attached to a parent entity.");
        }

        MimeEntity? parent = content.GetParent();
        int partIndex = FindChildIndex(parent, content);
        return (parent ?? throw new InvalidOperationException()).RemoveChildAt(partIndex);
    }

    /// <summary>
    /// Detaches the content.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public MimeEntity DetachContent(MimeEntity content)
    {
        return EmailEditor.DetachContentImplementation(content);
    }

    internal static MimeEntity EmbedMessageImplementation(EmailMessage message, EmailMessage embeddedMessage)
    {
        if (embeddedMessage == null)
        {
            throw new ArgumentNullException("embeddedMessage");
        }

        MimeEntity embedded = new EmailEntityFactory().CreateNestedMessage(embeddedMessage);
        AddAttachmentImplementation(message, embedded);
        return embedded;
    }

    /// <summary>
    /// Embeds the message.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "embeddedMessage">The embedded message used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public MimeEntity EmbedMessage(EmailMessage message, EmailMessage embeddedMessage)
    {
        return EmailEditor.EmbedMessageImplementation(message, embeddedMessage);
    }

    static void ValidateMessageBody(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (!message.HasBody())
        {
            throw new InvalidOperationException("The message has no MIME body to edit.");
        }
    }

    static void ValidatePartContainer(MimeEntity? multipart)
    {
        if (multipart == null)
        {
            throw new ArgumentNullException("multipart");
        }

        if (!multipart.IsContainer())
        {
            throw new InvalidOperationException("Parts can only be edited within a multipart MIME entity.");
        }
    }

    static void ValidateUnwrappable(MimeEntity? wrapper)
    {
        if ((wrapper ?? throw new InvalidOperationException()).GetKind() != MimeEntityKind.Multipart)
        {
            throw new InvalidOperationException("Only an ordinary multipart MIME entity can be unwrapped.");
        }

        if (wrapper.GetChildCount() != 1)
        {
            throw new InvalidOperationException("A multipart MIME entity must contain exactly one part to be unwrapped without data loss.");
        }
    }

    static void ValidateDetachedContent(EmailMessage message, MimeEntity content, string parameterName)
    {
        if (content == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (content.HasParent())
        {
            throw new ArgumentException("MIME content must be detached before it can be added.", parameterName);
        }

        if (ContainsEntity(message, content))
        {
            throw new ArgumentException("MIME content already belongs to the message being edited.", parameterName);
        }

        if (ContainsMessage(content, message))
        {
            throw new ArgumentException("Adding the MIME content would create a nested-message cycle.", parameterName);
        }
    }

    static void ValidateDetachedContents(EmailMessage message, List<MimeEntity> contents, string parameterName)
    {
        if (contents == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < contents.Count)
        {
            MimeEntity content = contents[index];
            ValidateDetachedContent(message, content, parameterName);
            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                if (object.ReferenceEquals(contents[duplicateIndex], content))
                {
                    throw new ArgumentException("MIME content cannot be added more than once.", parameterName);
                }

                duplicateIndex++;
            }

            index++;
        }
    }

    static bool ContainsEntity(EmailMessage message, MimeEntity expected)
    {
        if (!message.HasBody())
        {
            return false;
        }

        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithMessage(message);
        int index = 0;
        while (index < entities.Count)
        {
            if (object.ReferenceEquals(entities[index], expected))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool ContainsMessage(MimeEntity content, EmailMessage expected)
    {
        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithRoot(content);
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.HasNestedMessage() && object.ReferenceEquals(entity.GetNestedMessage(), expected))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static MimeEntity FindPrimaryBody(EmailMessage message, bool includeRelated)
    {
        MimeEntity? current = message.GetBody();
        while ((current ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Multipart && current.GetChildCount() > 0)
        {
            bool mixed = (current.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "mixed");
            bool related = (current.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related");
            if (!mixed && (!includeRelated || !related))
            {
                break;
            }

            if (related && HasExplicitRelatedRoot(current))
            {
                break;
            }

            current = current.GetChildAt(0);
        }

        return current;
    }

    static MimeEntity? FindRelatedBody(EmailMessage message)
    {
        EmailBodySelection selection = new EmailBodySelector().Choose(message.GetBody(), EmailBodyPreference.Html, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
        if (selection.HasBody())
        {
            MimeEntity? selected = selection.GetBody();
            while ((selected ?? throw new InvalidOperationException()).HasParent())
            {
                MimeEntity? parent = selected.GetParent();
                if ((parent ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Multipart && (parent.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related"))
                {
                    return parent;
                }

                selected = parent;
            }
        }

        MimeEntity? current = message.GetBody();
        while ((current ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Multipart)
        {
            if ((current.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related"))
            {
                return current;
            }

            if ((current.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "mixed") && current.GetChildCount() > 0)
            {
                current = current.GetChildAt(0);
                continue;
            }

            if ((current.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "alternative"))
            {
                int index = 0;
                while (index < current.GetChildCount())
                {
                    MimeEntity candidate = current.GetChildAt(index);
                    if (candidate.GetKind() == MimeEntityKind.Multipart && (candidate.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "related"))
                    {
                        return candidate;
                    }

                    index++;
                }
            }

            break;
        }

        return null;
    }

    static MimeEntity WrapPrimaryBody(EmailMessage message, MimeEntity primary, string subtype)
    {
        if (!primary.HasParent())
        {
            return WrapInMultipartImplementationWithMessageAndSubtype(message, subtype);
        }

        MimeEntity? parent = primary.GetParent();
        return WrapInMultipartImplementationWithParentAndPartIndexAndSubtype(parent, FindChildIndex(parent, primary), subtype);
    }

    static void ReplacePrimaryBody(EmailMessage message, MimeEntity primary, MimeEntity replacement)
    {
        if (!primary.HasParent())
        {
            message.SetBody(replacement);
            return;
        }

        MimeEntity? parent = primary.GetParent();
        (parent ?? throw new InvalidOperationException()).ReplaceChildAt(FindChildIndex(parent, primary), replacement);
    }

    static int FindChildIndex(MimeEntity? parent, MimeEntity child)
    {
        int index = 0;
        while (index < (parent ?? throw new InvalidOperationException()).GetChildCount())
        {
            if (object.ReferenceEquals(parent.GetChildAt(index), child))
            {
                return index;
            }

            index++;
        }

        throw new InvalidOperationException("The MIME parent link is inconsistent with its child collection.");
    }

    static bool HasExplicitRelatedRoot(MimeEntity related)
    {
        return (related.GetMediaType() ?? throw new InvalidOperationException()).GetParameters().GetDecoded("start") != null;
    }
}
