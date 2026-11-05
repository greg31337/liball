using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailParser"/>.
/// </summary>
public interface IEmailParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseWithInputUsingValueArray(byte[] input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseWithInputUsingStream(Stream input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseMessageWithInputUsingValueArray(byte[] input);
    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailParseResult ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions? options);
    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseMessageWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input);
    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseMessageWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions? options);
    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailParseResult ParseMessageWithInputUsingStream(Stream input);
    /// <summary>
    /// Parses the supplied message data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailParseResult ParseMessageWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream? input, EmailParseOptions? options);
    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseEntityWithInputUsingValueArray(byte[] input);
    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseEntityWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input);
    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseEntityWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseEntityWithInputUsingStream(Stream input);
    /// <summary>
    /// Parses the supplied entity data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseEntityWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options);
}
