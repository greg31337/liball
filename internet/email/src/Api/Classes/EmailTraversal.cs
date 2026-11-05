using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with traversal.
/// </summary>
public class EmailTraversal : IEmailTraversalContract
{
    /// <summary>
    /// The default maximum depth used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumDepth = EmailParsingLimitsConstants.DefaultMaxNestingDepth;
    /// <summary>
    /// The default maximum entity count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumEntityCount = EmailParsingLimitsConstants.DefaultMaxPartCount;
    internal static List<MimeEntity> DepthFirstImplementationWithRoot(MimeEntity? root)
    {
        return TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalMode.DepthFirst, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> DepthFirstWithRoot(MimeEntity? root)
    {
        return EmailTraversal.DepthFirstImplementationWithRoot(root);
    }

    internal static List<MimeEntity> DepthFirstImplementationWithRootAndMaximumDepth(MimeEntity? root, int maximumDepth)
    {
        return TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalMode.DepthFirst, maximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> DepthFirstWithRootAndMaximumDepth(MimeEntity? root, int maximumDepth)
    {
        return EmailTraversal.DepthFirstImplementationWithRootAndMaximumDepth(root, maximumDepth);
    }

    internal static List<MimeEntity> DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalMode.DepthFirst, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> DepthFirstImplementationWithMessage(EmailMessage message)
    {
        return DepthFirstImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> DepthFirstWithMessage(EmailMessage message)
    {
        return EmailTraversal.DepthFirstImplementationWithMessage(message);
    }

    internal static List<MimeEntity> DepthFirstImplementationWithMessageAndMaximumDepth(EmailMessage message, int maximumDepth)
    {
        return DepthFirstImplementationWithRootAndMaximumDepth(GetMessageRoot(message), maximumDepth);
    }

    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> DepthFirstWithMessageAndMaximumDepth(EmailMessage message, int maximumDepth)
    {
        return EmailTraversal.DepthFirstImplementationWithMessageAndMaximumDepth(message, maximumDepth);
    }

    internal static List<MimeEntity> DepthFirstImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> DepthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.DepthFirstImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> BreadthFirstImplementationWithRoot(MimeEntity? root)
    {
        return TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalMode.BreadthFirst, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> BreadthFirstWithRoot(MimeEntity? root)
    {
        return EmailTraversal.BreadthFirstImplementationWithRoot(root);
    }

    internal static List<MimeEntity> BreadthFirstImplementationWithRootAndMaximumDepth(MimeEntity? root, int maximumDepth)
    {
        return TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalMode.BreadthFirst, maximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> BreadthFirstWithRootAndMaximumDepth(MimeEntity? root, int maximumDepth)
    {
        return EmailTraversal.BreadthFirstImplementationWithRootAndMaximumDepth(root, maximumDepth);
    }

    internal static List<MimeEntity> BreadthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalMode.BreadthFirst, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> BreadthFirstWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.BreadthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> BreadthFirstImplementationWithMessage(EmailMessage message)
    {
        return BreadthFirstImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> BreadthFirstWithMessage(EmailMessage message)
    {
        return EmailTraversal.BreadthFirstImplementationWithMessage(message);
    }

    internal static List<MimeEntity> BreadthFirstImplementationWithMessageAndMaximumDepth(EmailMessage message, int maximumDepth)
    {
        return BreadthFirstImplementationWithRootAndMaximumDepth(GetMessageRoot(message), maximumDepth);
    }

    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> BreadthFirstWithMessageAndMaximumDepth(EmailMessage message, int maximumDepth)
    {
        return EmailTraversal.BreadthFirstImplementationWithMessageAndMaximumDepth(message, maximumDepth);
    }

    internal static List<MimeEntity> BreadthFirstImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return BreadthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> BreadthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.BreadthFirstImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, EmailTraversalMode mode, int maximumDepth, int maximumEntityCount)
    {
        Validate(root, mode, maximumDepth, maximumEntityCount);
        if (mode == EmailTraversalMode.DepthFirst)
        {
            return TraverseDepthFirst(root, maximumDepth, maximumEntityCount);
        }

        return TraverseBreadthFirst(root, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree using the requested order and resource limits.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> TraverseWithRootAndModeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, EmailTraversalMode mode, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(root, mode, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> TraverseImplementationWithMessageAndModeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, EmailTraversalMode mode, int maximumDepth, int maximumEntityCount)
    {
        return TraverseImplementationWithRootAndModeAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), mode, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Traverses a MIME tree using the requested order and resource limits.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> TraverseWithMessageAndModeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, EmailTraversalMode mode, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.TraverseImplementationWithMessageAndModeAndMaximumDepthAndMaximumEntityCount(message, mode, maximumDepth, maximumEntityCount);
    }

    internal static MimeEntity? GetAtPathImplementationWithRootAndPath(MimeEntity? root, EmailPath path)
    {
        return GetAtPathImplementationWithRootAndPathAndMaximumDepth(root, path, DefaultMaximumDepth);
    }

    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The current at path.</returns>
    public MimeEntity? GetAtPathWithRootAndPath(MimeEntity? root, EmailPath path)
    {
        return EmailTraversal.GetAtPathImplementationWithRootAndPath(root, path);
    }

    internal static MimeEntity? GetAtPathImplementationWithRootAndPathAndMaximumDepth(MimeEntity? root, EmailPath? path, int maximumDepth)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        ValidateMaximumDepth(maximumDepth);
        if (path.GetDepth() > maximumDepth)
        {
            throw new EmailLimitException("MaxTraversalDepth", maximumDepth, path.GetDepth());
        }

        MimeEntity? current = root;
        int depth = 0;
        while (depth < path.GetDepth())
        {
            int childIndex = path.GetAt(depth);
            if (childIndex >= GetTraversalChildCount(current))
            {
                return null;
            }

            current = GetTraversalChild(current, childIndex);
            depth++;
        }

        return current;
    }

    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>The current at path.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public MimeEntity? GetAtPathWithRootAndPathAndMaximumDepth(MimeEntity? root, EmailPath? path, int maximumDepth)
    {
        return EmailTraversal.GetAtPathImplementationWithRootAndPathAndMaximumDepth(root, path, maximumDepth);
    }

    internal static MimeEntity? GetAtPathImplementationWithMessageAndPath(EmailMessage message, EmailPath path)
    {
        return GetAtPathImplementationWithRootAndPath(GetMessageRoot(message), path);
    }

    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The current at path.</returns>
    public MimeEntity? GetAtPathWithMessageAndPath(EmailMessage message, EmailPath path)
    {
        return EmailTraversal.GetAtPathImplementationWithMessageAndPath(message, path);
    }

    internal static MimeEntity? GetAtPathImplementationWithMessageAndPathAndMaximumDepth(EmailMessage message, EmailPath path, int maximumDepth)
    {
        return GetAtPathImplementationWithRootAndPathAndMaximumDepth(GetMessageRoot(message), path, maximumDepth);
    }

    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>The current at path.</returns>
    public MimeEntity? GetAtPathWithMessageAndPathAndMaximumDepth(EmailMessage message, EmailPath path, int maximumDepth)
    {
        return EmailTraversal.GetAtPathImplementationWithMessageAndPathAndMaximumDepth(message, path, maximumDepth);
    }

    internal static EmailPath? GetPathImplementationWithRootAndTarget(MimeEntity? root, MimeEntity target)
    {
        return GetPathImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(root, target, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current path.</returns>
    public EmailPath? GetPathWithRootAndTarget(MimeEntity? root, MimeEntity target)
    {
        return EmailTraversal.GetPathImplementationWithRootAndTarget(root, target);
    }

    internal static EmailPath? GetPathImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        Validate(root, EmailTraversalMode.DepthFirst, maximumDepth, maximumEntityCount);
        Stack<EmailTraversalNode> pending = new Stack<EmailTraversalNode>();
        pending.Push(new EmailTraversalNode(root, new EmailPathOperations().CreateRoot()));
        int visited = 0;
        while (pending.Count > 0)
        {
            EmailTraversalNode node = pending.Pop();
            ValidateNode(node, maximumDepth, maximumEntityCount, visited);
            visited++;
            if (object.ReferenceEquals(node.GetEntity(), target))
            {
                return node.GetPath();
            }

            PushChildren(node, pending);
        }

        return null;
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current path.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPath? GetPathWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.GetPathImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(root, target, maximumDepth, maximumEntityCount);
    }

    internal static EmailPath? GetPathImplementationWithMessageAndTarget(EmailMessage message, MimeEntity target)
    {
        return GetPathImplementationWithRootAndTarget(GetMessageRoot(message), target);
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current path.</returns>
    public EmailPath? GetPathWithMessageAndTarget(EmailMessage message, MimeEntity target)
    {
        return EmailTraversal.GetPathImplementationWithMessageAndTarget(message, target);
    }

    internal static EmailPath? GetPathImplementationWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        return GetPathImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), target, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current path.</returns>
    public EmailPath? GetPathWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.GetPathImplementationWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(message, target, maximumDepth, maximumEntityCount);
    }

    internal static MimeEntity? GetParentImplementationWithRootAndTarget(MimeEntity? root, MimeEntity target)
    {
        return GetParentImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(root, target, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current parent.</returns>
    public MimeEntity? GetParentWithRootAndTarget(MimeEntity? root, MimeEntity target)
    {
        return EmailTraversal.GetParentImplementationWithRootAndTarget(root, target);
    }

    internal static MimeEntity? GetParentImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        EmailPath? path = GetPathImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(root, target, maximumDepth, maximumEntityCount);
        if (path == null || !path.HasParent())
        {
            return null;
        }

        return GetAtPathImplementationWithRootAndPathAndMaximumDepth(root, path.GetParent(), maximumDepth);
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current parent.</returns>
    public MimeEntity? GetParentWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.GetParentImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(root, target, maximumDepth, maximumEntityCount);
    }

    internal static MimeEntity? GetParentImplementationWithMessageAndTarget(EmailMessage message, MimeEntity target)
    {
        return GetParentImplementationWithRootAndTarget(GetMessageRoot(message), target);
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current parent.</returns>
    public MimeEntity? GetParentWithMessageAndTarget(EmailMessage message, MimeEntity target)
    {
        return EmailTraversal.GetParentImplementationWithMessageAndTarget(message, target);
    }

    internal static MimeEntity? GetParentImplementationWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        return GetParentImplementationWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), target, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current parent.</returns>
    public MimeEntity? GetParentWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntity target, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.GetParentImplementationWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(message, target, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByMediaTypeImplementationWithRootAndTopLevelTypeAndSubtype(MimeEntity? root, string topLevelType, string subtype)
    {
        return FindByMediaTypeImplementationWithRootAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(root, topLevelType, subtype, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByMediaTypeWithRootAndTopLevelTypeAndSubtype(MimeEntity? root, string topLevelType, string subtype)
    {
        return EmailTraversal.FindByMediaTypeImplementationWithRootAndTopLevelTypeAndSubtype(root, topLevelType, subtype);
    }

    internal static List<MimeEntity> FindByMediaTypeImplementationWithRootAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string topLevelType, string subtype, int maximumDepth, int maximumEntityCount)
    {
        ValidateSearchText(topLevelType, "topLevelType");
        ValidateSearchText(subtype, "subtype");
        List<MimeEntity> entities = DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        List<MimeEntity> matches = new List<MimeEntity>();
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if ((entity.GetMediaType() ?? throw new InvalidOperationException()).IsType(topLevelType, subtype))
            {
                matches.Add(entity);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByMediaTypeWithRootAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string topLevelType, string subtype, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByMediaTypeImplementationWithRootAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(root, topLevelType, subtype, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByMediaTypeImplementationWithMessageAndTopLevelTypeAndSubtype(EmailMessage message, string topLevelType, string subtype)
    {
        return FindByMediaTypeImplementationWithRootAndTopLevelTypeAndSubtype(GetMessageRoot(message), topLevelType, subtype);
    }

    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByMediaTypeWithMessageAndTopLevelTypeAndSubtype(EmailMessage message, string topLevelType, string subtype)
    {
        return EmailTraversal.FindByMediaTypeImplementationWithMessageAndTopLevelTypeAndSubtype(message, topLevelType, subtype);
    }

    internal static List<MimeEntity> FindByMediaTypeImplementationWithMessageAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string topLevelType, string subtype, int maximumDepth, int maximumEntityCount)
    {
        return FindByMediaTypeImplementationWithRootAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), topLevelType, subtype, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByMediaTypeWithMessageAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string topLevelType, string subtype, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByMediaTypeImplementationWithMessageAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(message, topLevelType, subtype, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByDispositionImplementationWithRootAndDispositionType(MimeEntity? root, string dispositionType)
    {
        return FindByDispositionImplementationWithRootAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(root, dispositionType, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByDispositionWithRootAndDispositionType(MimeEntity? root, string dispositionType)
    {
        return EmailTraversal.FindByDispositionImplementationWithRootAndDispositionType(root, dispositionType);
    }

    internal static List<MimeEntity> FindByDispositionImplementationWithRootAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string dispositionType, int maximumDepth, int maximumEntityCount)
    {
        ValidateSearchText(dispositionType, "dispositionType");
        List<MimeEntity> entities = DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        List<MimeEntity> matches = new List<MimeEntity>();
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.HasDisposition() && string.Equals((entity.GetDisposition() ?? throw new InvalidOperationException()).GetDispositionType(), dispositionType, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(entity);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByDispositionWithRootAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string dispositionType, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByDispositionImplementationWithRootAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(root, dispositionType, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByDispositionImplementationWithMessageAndDispositionType(EmailMessage message, string dispositionType)
    {
        return FindByDispositionImplementationWithRootAndDispositionType(GetMessageRoot(message), dispositionType);
    }

    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByDispositionWithMessageAndDispositionType(EmailMessage message, string dispositionType)
    {
        return EmailTraversal.FindByDispositionImplementationWithMessageAndDispositionType(message, dispositionType);
    }

    internal static List<MimeEntity> FindByDispositionImplementationWithMessageAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string dispositionType, int maximumDepth, int maximumEntityCount)
    {
        return FindByDispositionImplementationWithRootAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), dispositionType, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByDispositionWithMessageAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string dispositionType, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByDispositionImplementationWithMessageAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(message, dispositionType, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByFilenameImplementationWithRootAndFilename(MimeEntity? root, string filename)
    {
        return FindByFilenameImplementationWithRootAndFilenameAndMaximumDepthAndMaximumEntityCount(root, filename, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByFilenameWithRootAndFilename(MimeEntity? root, string filename)
    {
        return EmailTraversal.FindByFilenameImplementationWithRootAndFilename(root, filename);
    }

    internal static List<MimeEntity> FindByFilenameImplementationWithRootAndFilenameAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string filename, int maximumDepth, int maximumEntityCount)
    {
        ValidateSearchText(filename, "filename");
        List<MimeEntity> entities = DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        List<MimeEntity> matches = new List<MimeEntity>();
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            string? entityFilename = GetFilename(entity);
            if (entityFilename != null && string.Equals(entityFilename, filename, StringComparison.Ordinal))
            {
                matches.Add(entity);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByFilenameWithRootAndFilenameAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string filename, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByFilenameImplementationWithRootAndFilenameAndMaximumDepthAndMaximumEntityCount(root, filename, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByFilenameImplementationWithMessageAndFilename(EmailMessage message, string filename)
    {
        return FindByFilenameImplementationWithRootAndFilename(GetMessageRoot(message), filename);
    }

    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByFilenameWithMessageAndFilename(EmailMessage message, string filename)
    {
        return EmailTraversal.FindByFilenameImplementationWithMessageAndFilename(message, filename);
    }

    internal static List<MimeEntity> FindByFilenameImplementationWithMessageAndFilenameAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string filename, int maximumDepth, int maximumEntityCount)
    {
        return FindByFilenameImplementationWithRootAndFilenameAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), filename, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByFilenameWithMessageAndFilenameAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string filename, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByFilenameImplementationWithMessageAndFilenameAndMaximumDepthAndMaximumEntityCount(message, filename, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByContentIdImplementationWithRootAndContentId(MimeEntity? root, string contentId)
    {
        return FindByContentIdImplementationWithRootAndContentIdAndMaximumDepthAndMaximumEntityCount(root, contentId, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentIdWithRootAndContentId(MimeEntity? root, string contentId)
    {
        return EmailTraversal.FindByContentIdImplementationWithRootAndContentId(root, contentId);
    }

    internal static List<MimeEntity> FindByContentIdImplementationWithRootAndContentIdAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string contentId, int maximumDepth, int maximumEntityCount)
    {
        EmailMessageId? expected = ParseContentIdSearch(contentId);
        List<MimeEntity> entities = DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        List<MimeEntity> matches = new List<MimeEntity>();
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.HasContentId() && ContentIdsEqual(entity.GetContentId(), expected))
            {
                matches.Add(entity);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentIdWithRootAndContentIdAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string contentId, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByContentIdImplementationWithRootAndContentIdAndMaximumDepthAndMaximumEntityCount(root, contentId, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByContentIdImplementationWithMessageAndContentId(EmailMessage message, string contentId)
    {
        return FindByContentIdImplementationWithRootAndContentId(GetMessageRoot(message), contentId);
    }

    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentIdWithMessageAndContentId(EmailMessage message, string contentId)
    {
        return EmailTraversal.FindByContentIdImplementationWithMessageAndContentId(message, contentId);
    }

    internal static List<MimeEntity> FindByContentIdImplementationWithMessageAndContentIdAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string contentId, int maximumDepth, int maximumEntityCount)
    {
        return FindByContentIdImplementationWithRootAndContentIdAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), contentId, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentIdWithMessageAndContentIdAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string contentId, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByContentIdImplementationWithMessageAndContentIdAndMaximumDepthAndMaximumEntityCount(message, contentId, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByContentLocationImplementationWithRootAndContentLocation(MimeEntity? root, string contentLocation)
    {
        return FindByContentLocationImplementationWithRootAndContentLocationAndMaximumDepthAndMaximumEntityCount(root, contentLocation, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentLocationWithRootAndContentLocation(MimeEntity? root, string contentLocation)
    {
        return EmailTraversal.FindByContentLocationImplementationWithRootAndContentLocation(root, contentLocation);
    }

    internal static List<MimeEntity> FindByContentLocationImplementationWithRootAndContentLocationAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string contentLocation, int maximumDepth, int maximumEntityCount)
    {
        ValidateSearchText(contentLocation, "contentLocation");
        List<MimeEntity> entities = DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        List<MimeEntity> matches = new List<MimeEntity>();
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.HasContentLocation() && string.Equals(entity.GetContentLocation(), contentLocation, StringComparison.Ordinal))
            {
                matches.Add(entity);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentLocationWithRootAndContentLocationAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string contentLocation, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByContentLocationImplementationWithRootAndContentLocationAndMaximumDepthAndMaximumEntityCount(root, contentLocation, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByContentLocationImplementationWithMessageAndContentLocation(EmailMessage message, string contentLocation)
    {
        return FindByContentLocationImplementationWithRootAndContentLocation(GetMessageRoot(message), contentLocation);
    }

    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentLocationWithMessageAndContentLocation(EmailMessage message, string contentLocation)
    {
        return EmailTraversal.FindByContentLocationImplementationWithMessageAndContentLocation(message, contentLocation);
    }

    internal static List<MimeEntity> FindByContentLocationImplementationWithMessageAndContentLocationAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string contentLocation, int maximumDepth, int maximumEntityCount)
    {
        return FindByContentLocationImplementationWithRootAndContentLocationAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), contentLocation, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByContentLocationWithMessageAndContentLocationAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string contentLocation, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByContentLocationImplementationWithMessageAndContentLocationAndMaximumDepthAndMaximumEntityCount(message, contentLocation, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByKindImplementationWithRootAndKind(MimeEntity? root, MimeEntityKind kind)
    {
        return FindByKindImplementationWithRootAndKindAndMaximumDepthAndMaximumEntityCount(root, kind, DefaultMaximumDepth, DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByKindWithRootAndKind(MimeEntity? root, MimeEntityKind kind)
    {
        return EmailTraversal.FindByKindImplementationWithRootAndKind(root, kind);
    }

    internal static List<MimeEntity> FindByKindImplementationWithRootAndKindAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntityKind kind, int maximumDepth, int maximumEntityCount)
    {
        ValidateEntityKind(kind);
        List<MimeEntity> entities = DepthFirstImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        List<MimeEntity> matches = new List<MimeEntity>();
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.GetKind() == kind)
            {
                matches.Add(entity);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByKindWithRootAndKindAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntityKind kind, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByKindImplementationWithRootAndKindAndMaximumDepthAndMaximumEntityCount(root, kind, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> FindByKindImplementationWithMessageAndKind(EmailMessage message, MimeEntityKind kind)
    {
        return FindByKindImplementationWithRootAndKind(GetMessageRoot(message), kind);
    }

    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByKindWithMessageAndKind(EmailMessage message, MimeEntityKind kind)
    {
        return EmailTraversal.FindByKindImplementationWithMessageAndKind(message, kind);
    }

    internal static List<MimeEntity> FindByKindImplementationWithMessageAndKindAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntityKind kind, int maximumDepth, int maximumEntityCount)
    {
        return FindByKindImplementationWithRootAndKindAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), kind, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<MimeEntity> FindByKindWithMessageAndKindAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntityKind kind, int maximumDepth, int maximumEntityCount)
    {
        return EmailTraversal.FindByKindImplementationWithMessageAndKindAndMaximumDepthAndMaximumEntityCount(message, kind, maximumDepth, maximumEntityCount);
    }

    static List<MimeEntity> TraverseDepthFirst(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        List<MimeEntity> result = new List<MimeEntity>();
        Stack<EmailTraversalNode> pending = new Stack<EmailTraversalNode>();
        pending.Push(new EmailTraversalNode(root, new EmailPathOperations().CreateRoot()));
        while (pending.Count > 0)
        {
            EmailTraversalNode node = pending.Pop();
            ValidateNode(node, maximumDepth, maximumEntityCount, result.Count);
            result.Add((node.GetEntity() ?? throw new InvalidOperationException()));
            PushChildren(node, pending);
        }

        return result;
    }

    static List<MimeEntity> TraverseBreadthFirst(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        List<MimeEntity> result = new List<MimeEntity>();
        Queue<EmailTraversalNode> pending = new Queue<EmailTraversalNode>();
        pending.Enqueue(new EmailTraversalNode(root, new EmailPathOperations().CreateRoot()));
        while (pending.Count > 0)
        {
            EmailTraversalNode node = pending.Dequeue();
            ValidateNode(node, maximumDepth, maximumEntityCount, result.Count);
            result.Add((node.GetEntity() ?? throw new InvalidOperationException()));
            EnqueueChildren(node, pending);
        }

        return result;
    }

    static void PushChildren(EmailTraversalNode node, Stack<EmailTraversalNode> pending)
    {
        int childIndex = GetTraversalChildCount(node.GetEntity()) - 1;
        while (childIndex >= 0)
        {
            MimeEntity? child = GetTraversalChild(node.GetEntity(), childIndex);
            pending.Push(new EmailTraversalNode(child, node.GetPath().Append(childIndex)));
            childIndex--;
        }
    }

    static void EnqueueChildren(EmailTraversalNode node, Queue<EmailTraversalNode> pending)
    {
        int childIndex = 0;
        while (childIndex < GetTraversalChildCount(node.GetEntity()))
        {
            MimeEntity? child = GetTraversalChild(node.GetEntity(), childIndex);
            pending.Enqueue(new EmailTraversalNode(child, node.GetPath().Append(childIndex)));
            childIndex++;
        }
    }

    static int GetTraversalChildCount(MimeEntity? entity)
    {
        if ((entity ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Message)
        {
            return (entity.GetNestedMessage() ?? throw new InvalidOperationException()).HasBody() ? 1 : 0;
        }

        return entity.GetChildCount();
    }

    static MimeEntity? GetTraversalChild(MimeEntity? entity, int childIndex)
    {
        if ((entity ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Message)
        {
            return childIndex == 0 && (entity.GetNestedMessage() ?? throw new InvalidOperationException()).HasBody() ? (entity.GetNestedMessage() ?? throw new InvalidOperationException()).GetBody() : null;
        }

        return entity.GetChildAt(childIndex);
    }

    static MimeEntity? GetMessageRoot(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (!message.HasBody())
        {
            throw new ArgumentException("A message without a MIME body has no traversal root.", "message");
        }

        return message.GetBody();
    }

    static void Validate(MimeEntity? root, EmailTraversalMode mode, int maximumDepth, int maximumEntityCount)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (mode < EmailTraversalMode.DepthFirst || mode > EmailTraversalMode.BreadthFirst)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        ValidateMaximumDepth(maximumDepth);
        if (maximumEntityCount < 1)
        {
            throw new ArgumentOutOfRangeException("maximumEntityCount");
        }
    }

    static void ValidateMaximumDepth(int maximumDepth)
    {
        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }
    }

    static void ValidateSearchText(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A search value cannot be empty.", parameterName);
        }
    }

    static void ValidateEntityKind(MimeEntityKind kind)
    {
        if (kind < MimeEntityKind.Leaf || kind > MimeEntityKind.Encrypted)
        {
            throw new ArgumentOutOfRangeException("kind");
        }
    }

    static string? GetFilename(MimeEntity entity)
    {
        return new EmailFilename().Decode(entity);
    }

    static EmailMessageId? ParseContentIdSearch(string contentId)
    {
        ValidateSearchText(contentId, "contentId");
        string source = contentId.Trim();
        if (source.Length == 0)
        {
            throw new ArgumentException("A content ID search value cannot be blank.", "contentId");
        }

        if (source[0] != '<' || source[source.Length - 1] != '>')
        {
            source = "<" + source + ">";
        }

        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseContentIdWithInputAndMode(Encoding.UTF8.GetBytes(source), EmailMessageIdParseMode.Strict);
        if (!result.IsSuccess() || !result.HasSingleIdentifier())
        {
            throw new ArgumentException("A valid content ID search value is required.", "contentId");
        }

        return result.GetIdentifier();
    }

    static bool ContentIdsEqual(EmailMessageId? left, EmailMessageId? right)
    {
        if (!string.Equals((left ?? throw new InvalidOperationException()).GetIdLeft(), (right ?? throw new InvalidOperationException()).GetIdLeft(), StringComparison.Ordinal))
        {
            return false;
        }

        if (left.IsRightDomainLiteral() != right.IsRightDomainLiteral())
        {
            return false;
        }

        StringComparison comparison = left.IsRightDomainLiteral() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return string.Equals(left.GetIdRight(), right.GetIdRight(), comparison);
    }

    static void ValidateNode(EmailTraversalNode node, int maximumDepth, int maximumEntityCount, int visitedCount)
    {
        if (node.GetPath().GetDepth() > maximumDepth)
        {
            throw new EmailLimitException("MaxTraversalDepth", maximumDepth, node.GetPath().GetDepth());
        }

        if (visitedCount >= maximumEntityCount)
        {
            throw new EmailLimitException("MaxTraversalEntityCount", maximumEntityCount, (long)visitedCount + 1);
        }
    }
}
