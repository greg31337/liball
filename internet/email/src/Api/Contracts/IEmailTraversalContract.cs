using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTraversal"/>.
/// </summary>
public interface IEmailTraversalContract
{
    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> DepthFirstWithRoot(MimeEntity? root);
    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> DepthFirstWithRootAndMaximumDepth(MimeEntity? root, int maximumDepth);
    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> DepthFirstWithMessage(EmailMessage message);
    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> DepthFirstWithMessageAndMaximumDepth(EmailMessage message, int maximumDepth);
    /// <summary>
    /// Traverses a MIME tree in depth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> DepthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> BreadthFirstWithRoot(MimeEntity? root);
    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> BreadthFirstWithRootAndMaximumDepth(MimeEntity? root, int maximumDepth);
    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> BreadthFirstWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> BreadthFirstWithMessage(EmailMessage message);
    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> BreadthFirstWithMessageAndMaximumDepth(EmailMessage message, int maximumDepth);
    /// <summary>
    /// Traverses a MIME tree in breadth-first order and returns the visited entities.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> BreadthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Traverses a MIME tree using the requested order and resource limits.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> TraverseWithRootAndModeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, EmailTraversalMode mode, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Traverses a MIME tree using the requested order and resource limits.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> TraverseWithMessageAndModeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, EmailTraversalMode mode, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The current at path.</returns>
    MimeEntity? GetAtPathWithRootAndPath(MimeEntity? root, EmailPath path);
    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>The current at path.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    MimeEntity? GetAtPathWithRootAndPathAndMaximumDepth(MimeEntity? root, EmailPath? path, int maximumDepth);
    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The current at path.</returns>
    MimeEntity? GetAtPathWithMessageAndPath(EmailMessage message, EmailPath path);
    /// <summary>
    /// Gets the at path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <returns>The current at path.</returns>
    MimeEntity? GetAtPathWithMessageAndPathAndMaximumDepth(EmailMessage message, EmailPath path, int maximumDepth);
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current path.</returns>
    EmailPath? GetPathWithRootAndTarget(MimeEntity? root, MimeEntity target);
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current path.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailPath? GetPathWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntity target, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current path.</returns>
    EmailPath? GetPathWithMessageAndTarget(EmailMessage message, MimeEntity target);
    /// <summary>
    /// Gets the path.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current path.</returns>
    EmailPath? GetPathWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntity target, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current parent.</returns>
    MimeEntity? GetParentWithRootAndTarget(MimeEntity? root, MimeEntity target);
    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current parent.</returns>
    MimeEntity? GetParentWithRootAndTargetAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntity target, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <returns>The current parent.</returns>
    MimeEntity? GetParentWithMessageAndTarget(EmailMessage message, MimeEntity target);
    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "target">The target used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current parent.</returns>
    MimeEntity? GetParentWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntity target, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByMediaTypeWithRootAndTopLevelTypeAndSubtype(MimeEntity? root, string topLevelType, string subtype);
    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByMediaTypeWithRootAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string topLevelType, string subtype, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByMediaTypeWithMessageAndTopLevelTypeAndSubtype(EmailMessage message, string topLevelType, string subtype);
    /// <summary>
    /// Finds the matching by media type.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByMediaTypeWithMessageAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string topLevelType, string subtype, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByDispositionWithRootAndDispositionType(MimeEntity? root, string dispositionType);
    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByDispositionWithRootAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string dispositionType, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByDispositionWithMessageAndDispositionType(EmailMessage message, string dispositionType);
    /// <summary>
    /// Finds the matching by disposition.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByDispositionWithMessageAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string dispositionType, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByFilenameWithRootAndFilename(MimeEntity? root, string filename);
    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByFilenameWithRootAndFilenameAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string filename, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByFilenameWithMessageAndFilename(EmailMessage message, string filename);
    /// <summary>
    /// Finds the matching by filename.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByFilenameWithMessageAndFilenameAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string filename, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentIdWithRootAndContentId(MimeEntity? root, string contentId);
    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentIdWithRootAndContentIdAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string contentId, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentIdWithMessageAndContentId(EmailMessage message, string contentId);
    /// <summary>
    /// Finds the matching by content ID.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentIdWithMessageAndContentIdAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string contentId, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentLocationWithRootAndContentLocation(MimeEntity? root, string contentLocation);
    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentLocationWithRootAndContentLocationAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, string contentLocation, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentLocationWithMessageAndContentLocation(EmailMessage message, string contentLocation);
    /// <summary>
    /// Finds the matching by content location.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByContentLocationWithMessageAndContentLocationAndMaximumDepthAndMaximumEntityCount(EmailMessage message, string contentLocation, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByKindWithRootAndKind(MimeEntity? root, MimeEntityKind kind);
    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByKindWithRootAndKindAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, MimeEntityKind kind, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByKindWithMessageAndKind(EmailMessage message, MimeEntityKind kind);
    /// <summary>
    /// Finds the matching by kind.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<MimeEntity> FindByKindWithMessageAndKindAndMaximumDepthAndMaximumEntityCount(EmailMessage message, MimeEntityKind kind, int maximumDepth, int maximumEntityCount);
}
