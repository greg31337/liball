using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Parses and serializes legacy HTTP field-value grammars.
/// </summary>
public interface IHttpFieldValueCodec
{
    /// <summary>Parses an HTTP date.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The normalized timestamp.</returns>
    DateTimeOffset ParseHttpDate(byte[] input);
    /// <summary>Serializes a timestamp as IMF-fixdate.</summary>
    /// <param name="value">The timestamp to serialize.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeHttpDate(DateTimeOffset value);
    /// <summary>Parses a media type or media range.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed media value.</returns>
    HttpMediaType ParseMediaType(byte[] input);
    /// <summary>Serializes a media type or range.</summary>
    /// <param name="value">The media value.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeMediaType(HttpMediaType value);
    /// <summary>Parses a weighted token list.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered weighted values.</returns>
    List<HttpWeightedValue> ParseWeightedValues(byte[] input);
    /// <summary>Serializes weighted values.</summary>
    /// <param name="values">The ordered weighted values.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeWeightedValues(List<HttpWeightedValue> values);
    /// <summary>Parses language ranges.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered language ranges.</returns>
    List<HttpLanguageRange> ParseLanguageRanges(byte[] input);
    /// <summary>Serializes language ranges.</summary>
    /// <param name="values">The ordered language ranges.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeLanguageRanges(List<HttpLanguageRange> values);
    /// <summary>Parses an entity-tag list.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered entity tags.</returns>
    List<HttpEntityTag> ParseEntityTags(byte[] input);
    /// <summary>Serializes an entity-tag list.</summary>
    /// <param name="values">The ordered entity tags.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeEntityTags(List<HttpEntityTag> values);
    /// <summary>Parses a Range field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed range request.</returns>
    HttpRangeRequest ParseRangeValue(byte[] input);
    /// <summary>Serializes a Range field value.</summary>
    /// <param name="value">The range request.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeRangeValue(HttpRangeRequest value);
    /// <summary>Parses a Content-Range field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed content range.</returns>
    HttpContentRange ParseContentRange(byte[] input);
    /// <summary>Serializes a Content-Range field value.</summary>
    /// <param name="value">The content range.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeContentRange(HttpContentRange value);
    /// <summary>Parses generic authentication values.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered authentication values.</returns>
    List<HttpAuthenticationValue> ParseAuthenticationValues(byte[] input);
    /// <summary>Serializes generic authentication values.</summary>
    /// <param name="values">The authentication values.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeAuthenticationValues(List<HttpAuthenticationValue> values);
    /// <summary>Parses Via entries.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The ordered Via entries.</returns>
    List<HttpViaEntry> ParseViaEntries(byte[] input);
    /// <summary>Serializes Via entries.</summary>
    /// <param name="values">The Via entries.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeViaEntries(List<HttpViaEntry> values);
    /// <summary>Parses Cache-Control directives.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed directives.</returns>
    HttpCacheControl ParseCacheControlValue(byte[] input);
    /// <summary>Serializes Cache-Control directives.</summary>
    /// <param name="value">The parsed directives.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializeCacheControlValue(HttpCacheControl value);
    /// <summary>Parses an extensible Priority field value.</summary>
    /// <param name="input">The field value octets.</param>
    /// <returns>The parsed priority.</returns>
    HttpPriority ParsePriority(byte[] input);
    /// <summary>Serializes an extensible Priority field value.</summary>
    /// <param name="value">The priority metadata.</param>
    /// <returns>The canonical field value.</returns>
    byte[] SerializePriority(HttpPriority value);
}
