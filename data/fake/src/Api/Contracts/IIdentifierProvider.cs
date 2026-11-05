using System;
using System.Collections.Generic;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="IdentifierProvider"/>.</summary>
public interface IIdentifierProvider
{
    /// <summary>
    /// Generates a canonical lowercase UUID version 4 string.
    /// </summary>
    /// <remarks>The UUID contains pseudorandom or cryptographic random bits according to the configured random mode and uses the RFC 9562 variant.</remarks>
    /// <returns>A 36-character UUID string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 36 characters.</exception>
    string Uuid4();
    /// <summary>
    /// Generates UUID version 4 bytes in RFC network byte order.
    /// </summary>
    /// <returns>A new 16-byte UUID array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] Uuid4Bytes();
    /// <summary>
    /// Writes a canonical lowercase UUID version 4 string to a destination.
    /// </summary>
    /// <param name="destination">A destination containing at least 36 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 36 characters.</exception>
    int WriteUuid4(Span<char> destination);
    /// <summary>
    /// Writes UUID version 4 bytes in RFC network byte order to a destination.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    int WriteUuid4Bytes(Span<byte> destination);
    /// <summary>
    /// Generates a monotonic canonical lowercase UUID version 7 string using the captured reference time.
    /// </summary>
    /// <remarks>Equal or regressed timestamps reuse the last emitted timestamp and increment a 42-bit counter so each subsequent value remains greater than the previous value.</remarks>
    /// <returns>A 36-character UUID string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 36 characters.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    string Uuid7();
    /// <summary>
    /// Generates a monotonic canonical lowercase UUID version 7 string for a timestamp.
    /// </summary>
    /// <remarks>The timestamp is truncated to Unix milliseconds. Equal or regressed timestamps reuse the last emitted timestamp and increment a 42-bit counter.</remarks>
    /// <param name="timestamp">The requested timestamp.</param>
    /// <returns>A 36-character UUID string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 36 characters.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    string Uuid7FromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Generates monotonic UUID version 7 bytes in RFC network byte order using the captured reference time.
    /// </summary>
    /// <returns>A new 16-byte UUID array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    byte[] Uuid7Bytes();
    /// <summary>
    /// Generates monotonic UUID version 7 bytes in RFC network byte order for a timestamp.
    /// </summary>
    /// <remarks>The timestamp is truncated to Unix milliseconds. Equal or regressed timestamps reuse the last emitted timestamp and increment a 42-bit counter.</remarks>
    /// <param name="timestamp">The requested timestamp.</param>
    /// <returns>A new 16-byte UUID array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    byte[] Uuid7BytesFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Writes a monotonic canonical lowercase UUID version 7 string using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 36 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 36 characters.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    int WriteUuid7(Span<char> destination);
    /// <summary>
    /// Writes a monotonic canonical lowercase UUID version 7 string for a timestamp.
    /// </summary>
    /// <remarks>The timestamp is truncated to Unix milliseconds. Equal or regressed timestamps reuse the last emitted timestamp and increment a 42-bit counter.</remarks>
    /// <param name="destination">A destination containing at least 36 characters.</param>
    /// <param name="timestamp">The requested timestamp.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 36 characters.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    int WriteUuid7FromDestinationAndTimestamp(Span<char> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Writes monotonic UUID version 7 bytes in RFC network byte order using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    int WriteUuid7Bytes(Span<byte> destination);
    /// <summary>
    /// Writes monotonic UUID version 7 bytes in RFC network byte order for a timestamp.
    /// </summary>
    /// <remarks>The timestamp is truncated to Unix milliseconds. Equal or regressed timestamps reuse the last emitted timestamp and increment a 42-bit counter.</remarks>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <param name="timestamp">The requested timestamp.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown if both the timestamp and monotonic counter are exhausted.</exception>
    int WriteUuid7BytesFromDestinationAndTimestamp(Span<byte> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Generates a random canonical ULID using the captured reference time.
    /// </summary>
    /// <returns>A 26-character uppercase Crockford Base32 ULID.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    string Ulid();
    /// <summary>
    /// Generates a random canonical ULID for a timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp, truncated to Unix milliseconds.</param>
    /// <returns>A 26-character uppercase Crockford Base32 ULID.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    string UlidFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Generates random ULID bytes in network byte order using the captured reference time.
    /// </summary>
    /// <returns>A new 16-byte ULID array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    byte[] UlidBytes();
    /// <summary>
    /// Generates random ULID bytes in network byte order for a timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp, truncated to Unix milliseconds.</param>
    /// <returns>A new 16-byte ULID array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    byte[] UlidBytesFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Writes a random canonical ULID using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 26 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    int WriteUlid(Span<char> destination);
    /// <summary>
    /// Writes a random canonical ULID for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 26 characters.</param>
    /// <param name="timestamp">The timestamp, truncated to Unix milliseconds.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    int WriteUlidFromDestinationAndTimestamp(Span<char> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Writes random ULID bytes in network byte order using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    int WriteUlidBytes(Span<byte> destination);
    /// <summary>
    /// Writes random ULID bytes in network byte order for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <param name="timestamp">The timestamp, truncated to Unix milliseconds.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    int WriteUlidBytesFromDestinationAndTimestamp(Span<byte> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Generates a monotonic ULID using the captured reference time.
    /// </summary>
    /// <remarks>Equal or regressed timestamps retain the last timestamp and increment the 80-bit randomness field by one.</remarks>
    /// <returns>A 26-character uppercase Crockford Base32 ULID.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    string MonotonicUlid();
    /// <summary>
    /// Generates a monotonic ULID for a timestamp.
    /// </summary>
    /// <remarks>Equal or regressed timestamps retain the last timestamp and increment the 80-bit randomness field by one.</remarks>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>A 26-character uppercase Crockford Base32 ULID.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    string MonotonicUlidFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Generates monotonic ULID bytes in network byte order using the captured reference time.
    /// </summary>
    /// <returns>A new 16-byte ULID array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    byte[] MonotonicUlidBytes();
    /// <summary>
    /// Generates monotonic ULID bytes in network byte order for a timestamp.
    /// </summary>
    /// <remarks>Equal or regressed timestamps retain the last timestamp and increment the 80-bit randomness field by one.</remarks>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>A new 16-byte ULID array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    byte[] MonotonicUlidBytesFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Writes a monotonic ULID using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 26 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    int WriteMonotonicUlid(Span<char> destination);
    /// <summary>
    /// Writes a monotonic ULID for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 26 characters.</param>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 26 characters.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    int WriteMonotonicUlidFromDestinationAndTimestamp(Span<char> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Writes monotonic ULID bytes in network byte order using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the reference timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    int WriteMonotonicUlidBytes(Span<byte> destination);
    /// <summary>
    /// Writes monotonic ULID bytes in network byte order for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp is before the Unix epoch or the configured output limit is smaller than 16 bytes.</exception>
    /// <exception cref="FakeException">Thrown when the 80-bit randomness field overflows within one millisecond.</exception>
    int WriteMonotonicUlidBytesFromDestinationAndTimestamp(Span<byte> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Generates a 21-character Nano ID-style value using the URL-safe default alphabet.
    /// </summary>
    /// <remarks>
    /// The default alphabet has 64 symbols. Collision probability depends on the alphabet size,
    /// output length, and number of generated values; no generated value is guaranteed unique.
    /// Deterministic mode is reproducible and predictable. Select cryptographic random mode when
    /// unpredictability is required.
    /// </remarks>
    /// <returns>A random identifier containing 21 characters.</returns>
    string NanoId();
    /// <summary>
    /// Generates a Nano ID-style value of an exact length using the URL-safe default alphabet.
    /// </summary>
    /// <remarks>Reducing the length increases collision probability. This API does not track or guarantee uniqueness.</remarks>
    /// <param name="length">The requested identifier length.</param>
    /// <returns>A random identifier of the requested length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length is negative or exceeds a configured limit.</exception>
    string NanoIdFromLength(int length);
    /// <summary>
    /// Generates a Nano ID-style value from a configurable alphabet.
    /// </summary>
    /// <remarks>
    /// Symbols are selected uniformly with replacement. Collision probability is governed by the
    /// size of the value space, which is the alphabet length raised to the output length. The method
    /// does not reserve values or guarantee uniqueness.
    /// </remarks>
    /// <param name="length">The requested identifier length.</param>
    /// <param name="alphabet">One through 256 distinct, non-surrogate UTF-16 characters.</param>
    /// <returns>A random identifier of the requested length.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the alphabet is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the alphabet is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length or alphabet exceeds a configured or algorithm limit.</exception>
    string NanoIdFromLengthAndAlphabet(int length, string alphabet);
    /// <summary>
    /// Fills a destination with a Nano ID-style value using the URL-safe default alphabet.
    /// </summary>
    /// <param name="destination">The destination whose length determines the identifier length.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds a configured limit.</exception>
    int WriteNanoId(Span<char> destination);
    /// <summary>
    /// Fills a destination with a Nano ID-style value from a configurable alphabet.
    /// </summary>
    /// <param name="destination">The destination whose length determines the identifier length.</param>
    /// <param name="alphabet">One through 256 distinct, non-surrogate UTF-16 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the alphabet is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the alphabet is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination or alphabet exceeds a configured or algorithm limit.</exception>
    int WriteNanoIdFromDestinationAndAlphabet(Span<char> destination, string alphabet);
    /// <summary>
    /// Generates a compact 10-character identifier using the Nano ID URL-safe alphabet.
    /// </summary>
    /// <remarks>
    /// This convenience value has a substantially smaller space than the 21-character Nano ID
    /// default and therefore a higher collision probability. It is not a uniqueness guarantee,
    /// allocation service, credential, or database sequence.
    /// </remarks>
    /// <returns>A random identifier containing 10 characters.</returns>
    string ShortId();
    /// <summary>
    /// Generates a compact identifier using the Nano ID URL-safe alphabet.
    /// </summary>
    /// <remarks>Shorter values have higher collision probability and are not guaranteed unique.</remarks>
    /// <param name="length">The requested identifier length.</param>
    /// <returns>A random identifier of the requested length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length is negative or exceeds a configured limit.</exception>
    string ShortIdFromLength(int length);
    /// <summary>
    /// Fills a destination with a compact identifier using the Nano ID URL-safe alphabet.
    /// </summary>
    /// <param name="destination">The destination whose length determines the identifier length.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds a configured limit.</exception>
    int WriteShortId(Span<char> destination);
    /// <summary>
    /// Returns the next value from the provider's default unsigned decimal identifier sequence.
    /// </summary>
    /// <returns>The next unpadded decimal value, beginning with zero.</returns>
    /// <exception cref="FakeException">Thrown when the sequence exceeds the unsigned 64-bit range.</exception>
    string SequenceId();
    /// <summary>
    /// Returns the next value from a named unsigned decimal identifier sequence.
    /// </summary>
    /// <remarks>Names isolate sequence positions within this provider. Generated values are local counters and make no distributed uniqueness claim.</remarks>
    /// <param name="name">The nonempty sequence name.</param>
    /// <returns>The next unpadded decimal value, beginning with zero.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the name is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the name is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when the sequence exceeds the unsigned 64-bit range.</exception>
    string SequenceIdFromName(string name);
    /// <summary>
    /// Returns the next value from the configured default unsigned decimal identifier sequence.
    /// </summary>
    /// <param name="start">The first sequence value.</param>
    /// <param name="increment">The positive increment between values.</param>
    /// <param name="minimumDigits">The minimum output width, padded with leading zeroes.</param>
    /// <returns>The next formatted decimal value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the increment is zero or a width exceeds a configured limit.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the default sequence was already created with different settings.</exception>
    /// <exception cref="FakeException">Thrown when the sequence exceeds the unsigned 64-bit range.</exception>
    string SequenceIdFromStartAndIncrementAndMinimumDigits(ulong start, ulong increment, int minimumDigits);
    /// <summary>
    /// Returns the next value from a named configured unsigned decimal identifier sequence.
    /// </summary>
    /// <remarks>Settings are fixed by the first use of each name. These process-local counters are deterministic but do not coordinate across providers, processes, or machines.</remarks>
    /// <param name="name">The nonempty sequence name.</param>
    /// <param name="start">The first sequence value.</param>
    /// <param name="increment">The positive increment between values.</param>
    /// <param name="minimumDigits">The minimum output width, padded with leading zeroes.</param>
    /// <returns>The next formatted decimal value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the name is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the name is empty or whitespace or was already configured differently.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the increment is zero or a width exceeds a configured limit.</exception>
    /// <exception cref="FakeException">Thrown when the sequence exceeds the unsigned 64-bit range.</exception>
    string SequenceIdFromNameAndStartAndIncrementAndMinimumDigits(string name, ulong start, ulong increment, int minimumDigits);
    /// <summary>
    /// Returns the canonical nil UUID string.
    /// </summary>
    /// <returns>The all-zero UUID.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 36 characters.</exception>
    string UuidNil();
    /// <summary>
    /// Returns nil UUID bytes in RFC network byte order.
    /// </summary>
    /// <returns>A new all-zero 16-byte array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] UuidNilBytes();
    /// <summary>
    /// Returns the canonical max UUID string.
    /// </summary>
    /// <returns>The all-one UUID in lowercase hexadecimal.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 36 characters.</exception>
    string UuidMax();
    /// <summary>
    /// Returns max UUID bytes in RFC network byte order.
    /// </summary>
    /// <returns>A new all-one 16-byte array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] UuidMaxBytes();
    /// <summary>
    /// Formats UUID bytes in RFC network byte order as canonical lowercase text.
    /// </summary>
    /// <param name="value">The 16 UUID bytes.</param>
    /// <returns>A 36-character UUID string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value does not contain exactly 16 bytes.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 36 characters.</exception>
    string FormatUuid(byte[] value);
    /// <summary>
    /// Formats UUID bytes in RFC network byte order as canonical lowercase text.
    /// </summary>
    /// <param name="value">The 16 UUID bytes.</param>
    /// <returns>A 36-character UUID string.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value does not contain exactly 16 bytes.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 36 characters.</exception>
    string FormatUuidFromValue(ReadOnlySpan<byte> value);
    /// <summary>
    /// Formats UUID bytes in RFC network byte order into a destination as canonical lowercase text.
    /// </summary>
    /// <param name="value">The 16 UUID bytes.</param>
    /// <param name="destination">A destination containing at least 36 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value length is invalid or the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 36 characters.</exception>
    int WriteUuid(ReadOnlySpan<byte> value, Span<char> destination);
    /// <summary>
    /// Parses a canonical UUID string into bytes in RFC network byte order.
    /// </summary>
    /// <param name="value">The canonical UUID string.</param>
    /// <returns>A new 16-byte UUID array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is not canonical UUID text.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] ParseUuid(string value);
    /// <summary>
    /// Parses canonical UUID text into bytes in RFC network byte order.
    /// </summary>
    /// <param name="value">The canonical UUID text.</param>
    /// <returns>A new 16-byte UUID array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is not canonical UUID text.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] ParseUuidFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Parses canonical UUID text into a byte destination in RFC network byte order.
    /// </summary>
    /// <param name="value">The canonical UUID text.</param>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the text is invalid or the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    int ParseUuidFromValueAndDestination(ReadOnlySpan<char> value, Span<byte> destination);
    /// <summary>
    /// Reports whether a string is canonical UUID text.
    /// </summary>
    /// <param name="value">The UUID string to validate.</param>
    /// <returns>True for canonical UUID text of any version or variant; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateUuid(string value);
    /// <summary>
    /// Reports whether text is canonical UUID text.
    /// </summary>
    /// <param name="value">The UUID text to validate.</param>
    /// <returns>True for canonical UUID text of any version or variant; otherwise false.</returns>
    bool ValidateUuidFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Reports whether a string is a canonical RFC-variant UUID version 4 value.
    /// </summary>
    /// <param name="value">The UUID string to validate.</param>
    /// <returns>True when the syntax, version, and variant are valid for UUID version 4; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateUuid4(string value);
    /// <summary>
    /// Reports whether text is a canonical RFC-variant UUID version 4 value.
    /// </summary>
    /// <param name="value">The UUID text to validate.</param>
    /// <returns>True when the syntax, version, and variant are valid for UUID version 4; otherwise false.</returns>
    bool ValidateUuid4FromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Reports whether bytes in RFC network byte order contain an RFC-variant UUID version 4 value.
    /// </summary>
    /// <param name="value">The bytes to validate.</param>
    /// <returns>True when the length, version, and variant are valid for UUID version 4; otherwise false.</returns>
    bool ValidateUuid4FromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Reports whether a string is a canonical RFC-variant UUID version 7 value.
    /// </summary>
    /// <param name="value">The UUID string to validate.</param>
    /// <returns>True when the syntax, version, and variant are valid for UUID version 7; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateUuid7(string value);
    /// <summary>
    /// Reports whether text is a canonical RFC-variant UUID version 7 value.
    /// </summary>
    /// <param name="value">The UUID text to validate.</param>
    /// <returns>True when the syntax, version, and variant are valid for UUID version 7; otherwise false.</returns>
    bool ValidateUuid7FromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Reports whether bytes in RFC network byte order contain an RFC-variant UUID version 7 value.
    /// </summary>
    /// <param name="value">The bytes to validate.</param>
    /// <returns>True when the length, version, and variant are valid for UUID version 7; otherwise false.</returns>
    bool ValidateUuid7FromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Extracts the unsigned 48-bit Unix millisecond timestamp from UUID version 7 bytes.
    /// </summary>
    /// <param name="value">The UUID version 7 bytes in RFC network byte order.</param>
    /// <returns>The Unix timestamp in milliseconds.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is not an RFC-variant UUID version 7.</exception>
    long Uuid7UnixMilliseconds(ReadOnlySpan<byte> value);
    /// <summary>
    /// Returns the largest valid canonical ULID string.
    /// </summary>
    /// <returns>The maximum 128-bit ULID value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 26 characters.</exception>
    string UlidMax();
    /// <summary>
    /// Returns the largest valid ULID as bytes in network byte order.
    /// </summary>
    /// <returns>A new all-one 16-byte array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] UlidMaxBytes();
    /// <summary>
    /// Formats ULID bytes in network byte order as canonical uppercase Crockford Base32.
    /// </summary>
    /// <param name="value">The 16 ULID bytes.</param>
    /// <returns>A 26-character ULID.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value does not contain exactly 16 bytes.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 26 characters.</exception>
    string FormatUlid(byte[] value);
    /// <summary>
    /// Formats ULID bytes in network byte order as canonical uppercase Crockford Base32.
    /// </summary>
    /// <param name="value">The 16 ULID bytes.</param>
    /// <returns>A 26-character ULID.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value does not contain exactly 16 bytes.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 26 characters.</exception>
    string FormatUlidFromValue(ReadOnlySpan<byte> value);
    /// <summary>
    /// Formats ULID bytes in network byte order into a destination.
    /// </summary>
    /// <param name="value">The 16 ULID bytes.</param>
    /// <param name="destination">A destination containing at least 26 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a length is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 26 characters.</exception>
    int WriteUlidFromValueAndDestination(ReadOnlySpan<byte> value, Span<char> destination);
    /// <summary>
    /// Parses a valid ULID string into bytes in network byte order.
    /// </summary>
    /// <param name="value">The case-insensitive ULID string.</param>
    /// <returns>A new 16-byte ULID array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed or exceeds 128 bits.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] ParseUlid(string value);
    /// <summary>
    /// Parses valid ULID text into bytes in network byte order.
    /// </summary>
    /// <param name="value">The case-insensitive ULID text.</param>
    /// <returns>A new 16-byte ULID array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed or exceeds 128 bits.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    byte[] ParseUlidFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Parses valid ULID text into a byte destination in network byte order.
    /// </summary>
    /// <param name="value">The case-insensitive ULID text.</param>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed, exceeds 128 bits, or the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is smaller than 16 bytes.</exception>
    int ParseUlidFromValueAndDestination(ReadOnlySpan<char> value, Span<byte> destination);
    /// <summary>
    /// Reports whether a string is a valid case-insensitive ULID representation.
    /// </summary>
    /// <param name="value">The ULID string to validate.</param>
    /// <returns>True when the text uses the permitted Crockford alphabet and fits 128 bits; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateUlid(string value);
    /// <summary>
    /// Reports whether text is a valid case-insensitive ULID representation.
    /// </summary>
    /// <param name="value">The ULID text to validate.</param>
    /// <returns>True when the text uses the permitted Crockford alphabet and fits 128 bits; otherwise false.</returns>
    bool ValidateUlidFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Reports whether bytes contain one complete ULID in network byte order.
    /// </summary>
    /// <param name="value">The bytes to validate.</param>
    /// <returns>True when the value contains exactly 16 bytes; otherwise false.</returns>
    bool ValidateUlidFromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Extracts the unsigned 48-bit Unix millisecond timestamp from a ULID string.
    /// </summary>
    /// <param name="value">The case-insensitive ULID string.</param>
    /// <returns>The Unix timestamp in milliseconds.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed or exceeds 128 bits.</exception>
    long UlidUnixMilliseconds(string value);
    /// <summary>
    /// Extracts the unsigned 48-bit Unix millisecond timestamp from ULID text.
    /// </summary>
    /// <param name="value">The case-insensitive ULID text.</param>
    /// <returns>The Unix timestamp in milliseconds.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed or exceeds 128 bits.</exception>
    long UlidUnixMillisecondsFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Extracts the unsigned 48-bit Unix millisecond timestamp from ULID bytes.
    /// </summary>
    /// <param name="value">The 16 ULID bytes in network byte order.</param>
    /// <returns>The Unix timestamp in milliseconds.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value does not contain exactly 16 bytes.</exception>
    long UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Generates a lowercase ObjectId-shaped value using the captured reference time.
    /// </summary>
    /// <remarks>This API follows only the documented 12-byte field and 24-character hexadecimal shape. It does not claim BSON-driver interoperability, database allocation, or machine-and-process uniqueness.</remarks>
    /// <returns>A 24-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    string ObjectId();
    /// <summary>
    /// Generates a lowercase ObjectId-shaped value for a timestamp.
    /// </summary>
    /// <remarks>The timestamp is stored as unsigned big-endian Unix seconds, followed by a provider-stable five-byte value and a wrapping big-endian 24-bit counter. This is a shape guarantee only.</remarks>
    /// <param name="timestamp">The timestamp, truncated to Unix seconds.</param>
    /// <returns>A 24-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    string ObjectIdFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Generates ObjectId-shaped bytes using the captured reference time.
    /// </summary>
    /// <returns>A new 12-byte array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    byte[] ObjectIdBytes();
    /// <summary>
    /// Generates ObjectId-shaped bytes for a timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp, truncated to Unix seconds.</param>
    /// <returns>A new 12-byte array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    byte[] ObjectIdBytesFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Writes an ObjectId-shaped value using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 24 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    int WriteObjectId(Span<char> destination);
    /// <summary>
    /// Writes an ObjectId-shaped value for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 24 characters.</param>
    /// <param name="timestamp">The timestamp, truncated to Unix seconds.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    int WriteObjectIdFromDestinationAndTimestamp(Span<char> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Writes ObjectId-shaped bytes using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 12 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    int WriteObjectIdBytes(Span<byte> destination);
    /// <summary>
    /// Writes ObjectId-shaped bytes for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 12 bytes.</param>
    /// <param name="timestamp">The timestamp, truncated to Unix seconds.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    int WriteObjectIdBytesFromDestinationAndTimestamp(Span<byte> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Formats ObjectId-shaped bytes as lowercase hexadecimal text.
    /// </summary>
    /// <param name="value">The 12 bytes to format.</param>
    /// <returns>A 24-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value length is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    string FormatObjectId(byte[] value);
    /// <summary>
    /// Formats ObjectId-shaped bytes as lowercase hexadecimal text.
    /// </summary>
    /// <param name="value">The 12 bytes to format.</param>
    /// <returns>A 24-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value length is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    string FormatObjectIdFromValue(ReadOnlySpan<byte> value);
    /// <summary>
    /// Formats ObjectId-shaped bytes into a character destination.
    /// </summary>
    /// <param name="value">The 12 bytes to format.</param>
    /// <param name="destination">A destination containing at least 24 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a length is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    int WriteObjectIdFromValueAndDestination(ReadOnlySpan<byte> value, Span<char> destination);
    /// <summary>
    /// Parses ObjectId-shaped hexadecimal text.
    /// </summary>
    /// <param name="value">The case-insensitive 24-character hexadecimal value.</param>
    /// <returns>A new 12-byte array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    byte[] ParseObjectId(string value);
    /// <summary>
    /// Parses ObjectId-shaped hexadecimal text.
    /// </summary>
    /// <param name="value">The case-insensitive 24-character hexadecimal value.</param>
    /// <returns>A new 12-byte array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    byte[] ParseObjectIdFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Parses ObjectId-shaped hexadecimal text into a byte destination.
    /// </summary>
    /// <param name="value">The case-insensitive 24-character hexadecimal value.</param>
    /// <param name="destination">A destination containing at least 12 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed or the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    int ParseObjectIdFromValueAndDestination(ReadOnlySpan<char> value, Span<byte> destination);
    /// <summary>
    /// Reports whether a string has the ObjectId-shaped hexadecimal form.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for exactly 24 hexadecimal characters; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateObjectId(string value);
    /// <summary>
    /// Reports whether text has the ObjectId-shaped hexadecimal form.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for exactly 24 hexadecimal characters; otherwise false.</returns>
    bool ValidateObjectIdFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Reports whether bytes have the ObjectId-shaped length.
    /// </summary>
    /// <param name="value">The bytes to validate.</param>
    /// <returns>True for exactly 12 bytes; otherwise false.</returns>
    bool ValidateObjectIdFromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Extracts unsigned Unix seconds from ObjectId-shaped text.
    /// </summary>
    /// <param name="value">The case-insensitive hexadecimal value.</param>
    /// <returns>The unsigned 32-bit timestamp represented as a long.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    long ObjectIdUnixSeconds(string value);
    /// <summary>
    /// Extracts unsigned Unix seconds from ObjectId-shaped text.
    /// </summary>
    /// <param name="value">The case-insensitive hexadecimal value.</param>
    /// <returns>The unsigned 32-bit timestamp represented as a long.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    long ObjectIdUnixSecondsFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Extracts unsigned Unix seconds from ObjectId-shaped bytes.
    /// </summary>
    /// <param name="value">The 12 ObjectId-shaped bytes.</param>
    /// <returns>The unsigned 32-bit timestamp represented as a long.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value length is invalid.</exception>
    long ObjectIdUnixSecondsFromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Generates a sortable timestamp identifier using the captured reference time.
    /// </summary>
    /// <remarks>This library-defined shape is 16 bytes encoded as 32 lowercase hexadecimal characters: a big-endian 48-bit Unix millisecond timestamp and an 80-bit monotonic tail. It does not claim compatibility with UUID, ULID, database, or allocation standards.</remarks>
    /// <returns>A 32-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    string SortableTimestampId();
    /// <summary>
    /// Generates a sortable timestamp identifier for a timestamp.
    /// </summary>
    /// <remarks>Equal or regressed timestamps retain the last emitted timestamp and increment the tail, preserving strict ordinal order.</remarks>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>A 32-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    string SortableTimestampIdFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Generates sortable timestamp identifier bytes using the captured reference time.
    /// </summary>
    /// <returns>A new 16-byte array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    byte[] SortableTimestampIdBytes();
    /// <summary>
    /// Generates sortable timestamp identifier bytes for a timestamp.
    /// </summary>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>A new 16-byte array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    byte[] SortableTimestampIdBytesFromTimestamp(DateTimeOffset timestamp);
    /// <summary>
    /// Writes a sortable timestamp identifier using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 32 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    int WriteSortableTimestampId(Span<char> destination);
    /// <summary>
    /// Writes a sortable timestamp identifier for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 32 characters.</param>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    int WriteSortableTimestampIdFromDestinationAndTimestamp(Span<char> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Writes sortable timestamp identifier bytes using the captured reference time.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    int WriteSortableTimestampIdBytes(Span<byte> destination);
    /// <summary>
    /// Writes sortable timestamp identifier bytes for a timestamp.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 bytes.</param>
    /// <param name="timestamp">The requested timestamp, truncated to Unix milliseconds.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the timestamp or configured output limit is outside the supported range.</exception>
    /// <exception cref="FakeException">Thrown if the 80-bit tail overflows within one millisecond.</exception>
    int WriteSortableTimestampIdBytesFromDestinationAndTimestamp(Span<byte> destination, DateTimeOffset timestamp);
    /// <summary>
    /// Formats sortable timestamp identifier bytes as lowercase hexadecimal text.
    /// </summary>
    /// <param name="value">The 16 bytes to format.</param>
    /// <returns>A 32-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value length is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    string FormatSortableTimestampId(byte[] value);
    /// <summary>
    /// Formats sortable timestamp identifier bytes as lowercase hexadecimal text.
    /// </summary>
    /// <param name="value">The 16 bytes to format.</param>
    /// <returns>A 32-character lowercase hexadecimal value.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value length is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    string FormatSortableTimestampIdFromValue(ReadOnlySpan<byte> value);
    /// <summary>
    /// Parses sortable timestamp identifier text.
    /// </summary>
    /// <param name="value">The case-insensitive 32-character hexadecimal value.</param>
    /// <returns>A new 16-byte array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    byte[] ParseSortableTimestampId(string value);
    /// <summary>
    /// Parses sortable timestamp identifier text.
    /// </summary>
    /// <param name="value">The case-insensitive 32-character hexadecimal value.</param>
    /// <returns>A new 16-byte array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    byte[] ParseSortableTimestampIdFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Reports whether a string has the sortable timestamp identifier form.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for exactly 32 hexadecimal characters; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateSortableTimestampId(string value);
    /// <summary>
    /// Reports whether text has the sortable timestamp identifier form.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for exactly 32 hexadecimal characters; otherwise false.</returns>
    bool ValidateSortableTimestampIdFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Reports whether bytes have the sortable timestamp identifier length.
    /// </summary>
    /// <param name="value">The bytes to validate.</param>
    /// <returns>True for exactly 16 bytes; otherwise false.</returns>
    bool ValidateSortableTimestampIdFromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Extracts Unix milliseconds from sortable timestamp identifier text.
    /// </summary>
    /// <param name="value">The case-insensitive hexadecimal value.</param>
    /// <returns>The unsigned 48-bit timestamp represented as a long.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    long SortableTimestampIdUnixMilliseconds(string value);
    /// <summary>
    /// Extracts Unix milliseconds from sortable timestamp identifier text.
    /// </summary>
    /// <param name="value">The case-insensitive hexadecimal value.</param>
    /// <returns>The unsigned 48-bit timestamp represented as a long.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is malformed.</exception>
    long SortableTimestampIdUnixMillisecondsFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Extracts Unix milliseconds from sortable timestamp identifier bytes.
    /// </summary>
    /// <param name="value">The 16 identifier bytes.</param>
    /// <returns>The unsigned 48-bit timestamp represented as a long.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value length is invalid.</exception>
    long SortableTimestampIdUnixMillisecondsFromValueUsingReadOnlySpanOfByte(ReadOnlySpan<byte> value);
    /// <summary>
    /// Generates a DOI-shaped value using the restricted default ASCII shape.
    /// </summary>
    /// <remarks>The result is syntactic test data only. It is not registered, resolvable, assigned to a registrant, or evidence of ownership.</remarks>
    /// <returns>A value containing <c>10.</c>, four random digits, a slash, and eight lowercase alphanumeric suffix characters.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string Doi();
    /// <summary>
    /// Generates a DOI-shaped value using a restricted URI-safe ASCII subset.
    /// </summary>
    /// <remarks>The result has no DOI registration or resolution guarantee. The lengths control synthetic shape only and do not represent an allocated prefix.</remarks>
    /// <param name="registrantDigits">The positive number of digits following <c>10.</c>.</param>
    /// <param name="suffixLength">The positive alphanumeric suffix length.</param>
    /// <returns>A synthetic DOI-shaped value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a length is zero or exceeds a configured limit.</exception>
    string DoiFromRegistrantDigitsAndSuffixLength(int registrantDigits, int suffixLength);
    /// <summary>
    /// Writes a default DOI-shaped value to a destination.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    int WriteDoi(Span<char> destination);
    /// <summary>
    /// Writes a configured DOI-shaped value to a destination.
    /// </summary>
    /// <param name="destination">The destination for the complete value.</param>
    /// <param name="registrantDigits">The positive number of digits following <c>10.</c>.</param>
    /// <param name="suffixLength">The positive alphanumeric suffix length.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a length is zero or exceeds a configured limit.</exception>
    int WriteDoiFromDestinationAndRegistrantDigitsAndSuffixLength(Span<char> destination, int registrantDigits, int suffixLength);
    /// <summary>
    /// Reports whether a string matches the library's restricted DOI shape.
    /// </summary>
    /// <remarks>This checks syntax only and does not query or imply registration.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the value uses <c>10.</c>, one or more digits, a slash, and the supported nonempty ASCII suffix.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateDoi(string value);
    /// <summary>
    /// Reports whether text matches the library's restricted DOI shape.
    /// </summary>
    /// <remarks>This checks syntax only and does not query or imply registration.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the restricted syntax is valid.</returns>
    bool ValidateDoiFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Formats restricted DOI-shaped text as an HTTPS DOI resolver URL.
    /// </summary>
    /// <remarks>The URL form does not imply that the synthetic value resolves.</remarks>
    /// <param name="value">A value accepted by the restricted DOI validator.</param>
    /// <returns>An HTTPS URL beginning with <c>https://doi.org/</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the restricted shape is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    string DoiUrl(string value);
    /// <summary>
    /// Writes restricted DOI-shaped text as an HTTPS DOI resolver URL.
    /// </summary>
    /// <remarks>The URL form does not imply that the synthetic value resolves.</remarks>
    /// <param name="value">A value accepted by the restricted DOI validator.</param>
    /// <param name="destination">The destination for the complete URL.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value is invalid or the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    int WriteDoiUrl(ReadOnlySpan<char> value, Span<char> destination);
    /// <summary>
    /// Generates a compact checksum-valid synthetic ISBN-10.
    /// </summary>
    /// <remarks>Checksum validity does not establish ISBN allocation, publication identity, or authoritative registration-group boundaries.</remarks>
    /// <returns>A ten-character compact ISBN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string Isbn10();
    /// <summary>
    /// Generates a checksum-valid synthetic ISBN-10 in a display style.
    /// </summary>
    /// <remarks>Noncompact styles use fixed readability groups of one, three, five, and one characters. These are not authoritative ISBN registrant segments.</remarks>
    /// <param name="style">The requested presentation.</param>
    /// <returns>A synthetic ISBN-10 shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or a configured limit cannot accommodate the result.</exception>
    string Isbn10FromStyle(BookIdentifierStyle style);
    /// <summary>
    /// Writes a generated checksum-valid synthetic ISBN-10.
    /// </summary>
    /// <param name="destination">The destination for the complete value.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or a configured limit cannot accommodate the result.</exception>
    int WriteIsbn10(Span<char> destination, BookIdentifierStyle style);
    /// <summary>
    /// Formats a compact checksum-valid ISBN-10 using synthetic readability groups.
    /// </summary>
    /// <param name="value">A compact checksum-valid ISBN-10.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the compact value is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style or output size is invalid.</exception>
    string FormatIsbn10(string value, BookIdentifierStyle style);
    /// <summary>
    /// Formats a compact checksum-valid ISBN-10 into a destination.
    /// </summary>
    /// <param name="value">A compact checksum-valid ISBN-10.</param>
    /// <param name="destination">The destination for the formatted value.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the input or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style or output size is invalid.</exception>
    int WriteIsbn10FromValueAndDestinationAndStyle(ReadOnlySpan<char> value, Span<char> destination, BookIdentifierStyle style);
    /// <summary>
    /// Generates a compact checksum-valid synthetic ISBN-13.
    /// </summary>
    /// <remarks>The value begins with 978 or 979, but checksum validity does not establish allocation or publication identity.</remarks>
    /// <returns>A thirteen-digit compact ISBN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string Isbn13();
    /// <summary>
    /// Generates a checksum-valid synthetic ISBN-13 in a display style.
    /// </summary>
    /// <remarks>Noncompact styles use fixed readability groups of three, one, three, five, and one characters. These are not authoritative ISBN registrant segments.</remarks>
    /// <param name="style">The requested presentation.</param>
    /// <returns>A synthetic ISBN-13 shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or a configured limit cannot accommodate the result.</exception>
    string Isbn13FromStyle(BookIdentifierStyle style);
    /// <summary>
    /// Writes a generated checksum-valid synthetic ISBN-13.
    /// </summary>
    /// <param name="destination">The destination for the complete value.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or a configured limit cannot accommodate the result.</exception>
    int WriteIsbn13(Span<char> destination, BookIdentifierStyle style);
    /// <summary>
    /// Formats a compact checksum-valid ISBN-13 using synthetic readability groups.
    /// </summary>
    /// <param name="value">A compact checksum-valid ISBN-13.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the compact value is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style or output size is invalid.</exception>
    string FormatIsbn13(string value, BookIdentifierStyle style);
    /// <summary>
    /// Formats a compact checksum-valid ISBN-13 into a destination.
    /// </summary>
    /// <param name="value">A compact checksum-valid ISBN-13.</param>
    /// <param name="destination">The destination for the formatted value.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the input or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style or output size is invalid.</exception>
    int WriteIsbn13FromValueAndDestinationAndStyle(ReadOnlySpan<char> value, Span<char> destination, BookIdentifierStyle style);
    /// <summary>
    /// Generates a compact checksum-valid synthetic ISSN.
    /// </summary>
    /// <remarks>Checksum validity does not establish ISSN assignment or identify a publication.</remarks>
    /// <returns>An eight-character compact ISSN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string Issn();
    /// <summary>
    /// Generates a checksum-valid synthetic ISSN in a display style.
    /// </summary>
    /// <param name="style">The requested presentation.</param>
    /// <returns>A synthetic ISSN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or a configured limit cannot accommodate the result.</exception>
    string IssnFromStyle(BookIdentifierStyle style);
    /// <summary>
    /// Writes a generated checksum-valid synthetic ISSN.
    /// </summary>
    /// <param name="destination">The destination for the complete value.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or a configured limit cannot accommodate the result.</exception>
    int WriteIssn(Span<char> destination, BookIdentifierStyle style);
    /// <summary>
    /// Formats a compact checksum-valid ISSN using its four-plus-four display grouping.
    /// </summary>
    /// <param name="value">A compact checksum-valid ISSN.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the compact value is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style or output size is invalid.</exception>
    string FormatIssn(string value, BookIdentifierStyle style);
    /// <summary>
    /// Formats a compact checksum-valid ISSN into a destination.
    /// </summary>
    /// <param name="value">A compact checksum-valid ISSN.</param>
    /// <param name="destination">The destination for the formatted value.</param>
    /// <param name="style">The requested presentation.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the input or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style or output size is invalid.</exception>
    int WriteIssnFromValueAndDestinationAndStyle(ReadOnlySpan<char> value, Span<char> destination, BookIdentifierStyle style);
    /// <summary>
    /// Returns the GS1 General Specifications release used for supported GTIN structural constants.
    /// </summary>
    /// <remarks>No GS1 allocation, company-prefix, or Application Identifier tables are bundled.</remarks>
    /// <returns>The release identifier <c>26.0</c>.</returns>
    string Gs1GeneralSpecificationsVersion();
    /// <summary>
    /// Returns the supported complete GTIN lengths.
    /// </summary>
    /// <returns>A new array containing 8, 12, 13, and 14.</returns>
    int[] GtinLengths();
    /// <summary>
    /// Generates a checksum-valid synthetic GTIN-8 suitable for an EAN-8-shaped display.
    /// </summary>
    /// <remarks>The value is not allocated, licensed, verified by GS1, or guaranteed usable in commerce.</remarks>
    /// <returns>An eight-digit GTIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string Ean8();
    /// <summary>
    /// Writes a checksum-valid synthetic GTIN-8 suitable for an EAN-8-shaped display.
    /// </summary>
    /// <param name="destination">A destination containing at least eight characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    int WriteEan8(Span<char> destination);
    /// <summary>
    /// Reports whether a string is a checksum-valid compact GTIN-8 shape.
    /// </summary>
    /// <remarks>This is structural validation only and does not establish GS1 assignment.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the value has eight ASCII digits and a valid check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateEan8(string value);
    /// <summary>
    /// Reports whether text is a checksum-valid compact GTIN-8 shape.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the value has eight ASCII digits and a valid check digit.</returns>
    bool ValidateEan8FromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Generates a checksum-valid synthetic GTIN-12 suitable for a UPC-A-shaped display.
    /// </summary>
    /// <remarks>The value is not allocated, licensed, verified by GS1, or guaranteed usable in commerce.</remarks>
    /// <returns>A twelve-digit GTIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string UpcA();
    /// <summary>
    /// Writes a checksum-valid synthetic GTIN-12 suitable for a UPC-A-shaped display.
    /// </summary>
    /// <param name="destination">A destination containing at least twelve characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    int WriteUpcA(Span<char> destination);
    /// <summary>
    /// Reports whether a string is a checksum-valid compact GTIN-12 shape.
    /// </summary>
    /// <remarks>This is structural validation only and does not establish GS1 assignment.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the value has twelve ASCII digits and a valid check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateUpcA(string value);
    /// <summary>
    /// Reports whether text is a checksum-valid compact GTIN-12 shape.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the value has twelve ASCII digits and a valid check digit.</returns>
    bool ValidateUpcAFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Generates a checksum-valid synthetic GTIN-14.
    /// </summary>
    /// <remarks>The value is not allocated, licensed, verified by GS1, or guaranteed usable in commerce.</remarks>
    /// <returns>A fourteen-digit GTIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string Gtin();
    /// <summary>
    /// Generates a checksum-valid synthetic GTIN with a supported length.
    /// </summary>
    /// <remarks>Supported shapes have 8, 12, 13, or 14 digits. Structural and checksum validity do not establish allocation or ownership.</remarks>
    /// <param name="length">The complete identifier length.</param>
    /// <returns>A compact GTIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length is unsupported or a configured limit cannot accommodate the value.</exception>
    string GtinFromLength(int length);
    /// <summary>
    /// Writes a generated checksum-valid synthetic GTIN-14.
    /// </summary>
    /// <param name="destination">A destination containing at least fourteen characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    int WriteGtin(Span<char> destination);
    /// <summary>
    /// Writes a generated checksum-valid synthetic GTIN with a supported length.
    /// </summary>
    /// <param name="destination">A destination containing at least the requested number of characters.</param>
    /// <param name="length">The complete identifier length: 8, 12, 13, or 14.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length is unsupported or a configured limit cannot accommodate the value.</exception>
    int WriteGtinFromDestinationAndLength(Span<char> destination, int length);
    /// <summary>
    /// Reports whether a string is a checksum-valid supported compact GTIN shape.
    /// </summary>
    /// <remarks>This is structural validation only and does not establish GS1 assignment.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for a checksum-valid GTIN-8, GTIN-12, GTIN-13, or GTIN-14 shape.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateGtin(string value);
    /// <summary>
    /// Reports whether text is a checksum-valid supported compact GTIN shape.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for a checksum-valid GTIN-8, GTIN-12, GTIN-13, or GTIN-14 shape.</returns>
    bool ValidateGtinFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Left-pads a checksum-valid supported GTIN shape to fourteen digits.
    /// </summary>
    /// <remarks>Padding is a storage representation only. It does not allocate a new GTIN or change the original identifier's data-carrier form.</remarks>
    /// <param name="value">A checksum-valid compact GTIN-8, GTIN-12, GTIN-13, or GTIN-14 shape.</param>
    /// <returns>The value represented with fourteen digits.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is structurally or checksum invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string Gtin14(string value);
    /// <summary>
    /// Writes a checksum-valid supported GTIN shape left-padded to fourteen digits.
    /// </summary>
    /// <param name="value">A checksum-valid compact GTIN shape.</param>
    /// <param name="destination">A destination containing at least fourteen characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the value or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    int WriteGtin14(ReadOnlySpan<char> value, Span<char> destination);
    /// <summary>
    /// Returns the GSMA TS.06 release used for IMEI structural rules.
    /// </summary>
    /// <returns>The release identifier <c>28.1</c>.</returns>
    string GsmaTs06Version();
    /// <summary>
    /// Generates a checksum-valid synthetic IMEI shape.
    /// </summary>
    /// <remarks>The eight-digit TAC portion is random test data, not a GSMA allocation. The result must not be treated as a device identity or used on a network.</remarks>
    /// <returns>A 15-digit value containing an eight-digit TAC shape, six-digit serial shape, and Luhn check digit.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string Imei();
    /// <summary>
    /// Generates a checksum-valid synthetic IMEI shape using a supplied TAC.
    /// </summary>
    /// <remarks>Supplying a TAC does not verify its allocation or authorize use.</remarks>
    /// <param name="tac">Exactly eight ASCII decimal digits.</param>
    /// <returns>A 15-digit IMEI shape with a random six-digit serial portion.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the TAC is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the TAC is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string ImeiFromTac(string tac);
    /// <summary>
    /// Composes a checksum-valid IMEI shape from supplied structural fields.
    /// </summary>
    /// <remarks>The method validates syntax and calculates Luhn only; it does not verify allocation, uniqueness, ownership, or network usability.</remarks>
    /// <param name="tac">Exactly eight ASCII decimal digits.</param>
    /// <param name="serialNumber">Exactly six ASCII decimal digits.</param>
    /// <returns>The fields followed by their Luhn check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an input is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an input is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string ImeiFromTacAndSerialNumber(string tac, string serialNumber);
    /// <summary>
    /// Writes a generated checksum-valid synthetic IMEI shape.
    /// </summary>
    /// <param name="destination">A destination containing at least 15 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    int WriteImei(Span<char> destination);
    /// <summary>
    /// Writes a checksum-valid IMEI shape from supplied structural fields.
    /// </summary>
    /// <param name="tac">Exactly eight ASCII decimal digits.</param>
    /// <param name="serialNumber">Exactly six ASCII decimal digits.</param>
    /// <param name="destination">A destination containing at least 15 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when an input or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    int WriteImeiFromTacAndSerialNumberAndDestination(ReadOnlySpan<char> tac, ReadOnlySpan<char> serialNumber, Span<char> destination);
    /// <summary>
    /// Reports whether a string has valid IMEI structure and Luhn handling.
    /// </summary>
    /// <remarks>This is offline structural validation only and does not query TAC allocation or device status.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for 15 ASCII digits with a valid Luhn check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateImei(string value);
    /// <summary>
    /// Reports whether text has valid IMEI structure and Luhn handling.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for 15 ASCII digits with a valid Luhn check digit.</returns>
    bool ValidateImeiFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Extracts the eight-digit TAC shape from a valid IMEI.
    /// </summary>
    /// <param name="value">A structurally valid IMEI.</param>
    /// <returns>The first eight digits.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the IMEI is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string ImeiTac(string value);
    /// <summary>
    /// Extracts the six-digit serial-number shape from a valid IMEI.
    /// </summary>
    /// <param name="value">A structurally valid IMEI.</param>
    /// <returns>Digits nine through fourteen.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the IMEI is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string ImeiSerialNumber(string value);
    /// <summary>
    /// Generates a synthetic IMEISV shape with a software version from 00 through 98.
    /// </summary>
    /// <remarks>The TAC is random test data rather than a GSMA allocation. IMEISV has no Luhn check digit.</remarks>
    /// <returns>A 16-digit TAC, serial, and software-version shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string ImeiSv();
    /// <summary>
    /// Generates a synthetic IMEISV shape with an explicit software version.
    /// </summary>
    /// <param name="softwareVersion">A value from 0 through 98; 99 is reserved.</param>
    /// <returns>A 16-digit TAC, serial, and software-version shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the software version or a configured limit is invalid.</exception>
    string ImeiSvFromSoftwareVersion(int softwareVersion);
    /// <summary>
    /// Composes a synthetic IMEISV shape from supplied structural fields.
    /// </summary>
    /// <remarks>Syntax does not verify TAC allocation, software provenance, or device identity.</remarks>
    /// <param name="tac">Exactly eight ASCII decimal digits.</param>
    /// <param name="serialNumber">Exactly six ASCII decimal digits.</param>
    /// <param name="softwareVersion">A value from 0 through 98.</param>
    /// <returns>A 16-digit IMEISV shape with no Luhn digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a text input is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a text input is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the software version or a configured limit is invalid.</exception>
    string ImeiSvFromTacAndSerialNumberAndSoftwareVersion(string tac, string serialNumber, int softwareVersion);
    /// <summary>
    /// Replaces a valid IMEI check digit with a two-digit software version.
    /// </summary>
    /// <param name="imei">A structurally valid IMEI.</param>
    /// <param name="softwareVersion">A value from 0 through 98.</param>
    /// <returns>The first 14 IMEI digits followed by the software version.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the IMEI is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the IMEI is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the software version or a configured limit is invalid.</exception>
    string ImeiSvFromImeiAndSoftwareVersion(string imei, int softwareVersion);
    /// <summary>
    /// Writes a generated synthetic IMEISV shape.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    int WriteImeiSv(Span<char> destination);
    /// <summary>
    /// Writes a generated synthetic IMEISV shape with an explicit software version.
    /// </summary>
    /// <param name="destination">A destination containing at least 16 characters.</param>
    /// <param name="softwareVersion">A value from 0 through 98.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the software version or a configured limit is invalid.</exception>
    int WriteImeiSvFromDestinationAndSoftwareVersion(Span<char> destination, int softwareVersion);
    /// <summary>
    /// Reports whether a string has valid IMEISV structure.
    /// </summary>
    /// <remarks>Validation requires 16 ASCII digits and software version 00 through 98. IMEISV does not use a Luhn digit.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the structure and software-version range are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateImeiSv(string value);
    /// <summary>
    /// Reports whether text has valid IMEISV structure.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True for 16 ASCII digits ending in software version 00 through 98.</returns>
    bool ValidateImeiSvFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Converts a valid IMEISV shape into an IMEI by replacing its software version with a Luhn digit.
    /// </summary>
    /// <param name="value">A structurally valid IMEISV.</param>
    /// <returns>The first 14 digits followed by their Luhn check digit.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the IMEISV is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string ImeiFromImeiSv(string value);
    /// <summary>
    /// Extracts the software-version number from a valid IMEISV.
    /// </summary>
    /// <param name="value">A structurally valid IMEISV.</param>
    /// <returns>A value from 0 through 98.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the IMEISV is invalid.</exception>
    int ImeiSvSoftwareVersion(string value);
    /// <summary>
    /// Generates an IMEI with the documented non-North-American test marker shape.
    /// </summary>
    /// <remarks>This is only a checksum-valid <c>0044</c> marker shape. It is not an allocated Test TAC and must not be placed in equipment or used on a network.</remarks>
    /// <returns>A 15-digit marker-shaped value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string InternationalTestImeiShape();
    /// <summary>
    /// Generates an IMEI with the documented North American test marker shape.
    /// </summary>
    /// <remarks>This is only a checksum-valid <c>001mmm00</c> TAC shape. It is not an allocated Test TAC and must not be placed in equipment or used on a network.</remarks>
    /// <returns>A 15-digit marker-shaped value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string NorthAmericanTestImeiShape();
    /// <summary>
    /// Reports whether an IMEI has the documented non-North-American test marker shape.
    /// </summary>
    /// <remarks>A matching marker does not prove Test TAC allocation.</remarks>
    /// <param name="value">The IMEI to inspect.</param>
    /// <returns>True when the IMEI is Luhn-valid and begins with <c>0044</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool IsInternationalTestImeiShape(string value);
    /// <summary>
    /// Reports whether an IMEI has the documented North American test marker shape.
    /// </summary>
    /// <remarks>A matching marker does not prove Test TAC allocation.</remarks>
    /// <param name="value">The IMEI to inspect.</param>
    /// <returns>True when the IMEI is Luhn-valid and its TAC matches <c>001mmm00</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool IsNorthAmericanTestImeiShape(string value);
    /// <summary>
    /// Generates a generic SKU shape from the resolved <c>identifier.sku.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated SKU-shaped value.</returns>
    string Sku();
    /// <summary>
    /// Generates a generic SKU shape from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated SKU-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string SkuFromMask(string mask);
    /// <summary>
    /// Generates a generic serial-number shape from the resolved <c>identifier.serial.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated serial-number-shaped value.</returns>
    string Serial();
    /// <summary>
    /// Generates a generic serial-number shape from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated serial-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string SerialFromMask(string mask);
    /// <summary>
    /// Generates a generic lot-number shape from the resolved <c>identifier.lot.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated lot-number-shaped value.</returns>
    string Lot();
    /// <summary>
    /// Generates a generic lot-number shape from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated lot-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string LotFromMask(string mask);
    /// <summary>
    /// Generates a generic order-number shape from the resolved <c>identifier.order.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated order-number-shaped value.</returns>
    string Order();
    /// <summary>
    /// Generates a generic order-number shape from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated order-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string OrderFromMask(string mask);
    /// <summary>
    /// Generates a generic invoice-number shape from the resolved <c>identifier.invoice.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <returns>A generated invoice-number-shaped value.</returns>
    string Invoice();
    /// <summary>
    /// Generates a generic invoice-number shape from an explicit format mask.
    /// </summary>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated invoice-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string InvoiceFromMask(string mask);
    /// <summary>
    /// Generates a generic tracking-number shape from the resolved <c>identifier.tracking.masks</c> locale field or the built-in fallback mask.
    /// </summary>
    /// <remarks>The result is synthetic and is not claimed to belong to or be accepted by any carrier.</remarks>
    /// <returns>A generated tracking-number-shaped value.</returns>
    string Tracking();
    /// <summary>
    /// Generates a generic tracking-number shape from an explicit format mask.
    /// </summary>
    /// <remarks>The result is synthetic and is not claimed to belong to or be accepted by any carrier.</remarks>
    /// <param name="mask">A mask using <c>#</c>, <c>?</c>, and <c>*</c> replacement tokens.</param>
    /// <returns>A generated tracking-number-shaped value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the mask is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the mask is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds a configured limit.</exception>
    string TrackingFromMask(string mask);
    /// <summary>
    /// Returns a documented valid-but-unusual identifier fixture for an edge-case category.
    /// </summary>
    /// <remarks>Fixtures exercise syntax and validation boundaries only and never imply allocation, ownership, or real-world use.</remarks>
    /// <param name="kind">The edge-case category.</param>
    /// <returns>A valid unusual checksum, length, character, prefix, special-version, or maximum-boundary fixture.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unsupported or the configured output limit is too small.</exception>
    string ValidUnusual(IdentifierMutationKind kind);
    /// <summary>
    /// Returns a deliberately invalid identifier fixture for an edge-case category.
    /// </summary>
    /// <remarks>The version fixture targets UUID version validation, and the overflow fixture targets ULID textual range validation.</remarks>
    /// <param name="kind">The invalidity category.</param>
    /// <returns>A deliberately invalid checksum, length, character, prefix, version, or overflow fixture.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unsupported or the configured output limit is too small.</exception>
    string Invalid(IdentifierMutationKind kind);
    /// <summary>
    /// Replaces one character with unsupported whitespace to make identifier text deliberately invalid.
    /// </summary>
    /// <param name="value">The nonempty identifier text to mutate.</param>
    /// <returns>A same-length deliberately invalid character mutation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is empty or malformed UTF-16.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the value.</exception>
    string InvalidFromValue(string value);
    /// <summary>
    /// Applies a deliberate identifier edge-case mutation.
    /// </summary>
    /// <remarks>Checksum mutation recognizes a valid VIN's position-nine digit. Prefix mutation preserves a valid compact ISBN-13 checksum while changing its required prefix. Version requires canonical UUID text, and overflow requires valid ULID text.</remarks>
    /// <param name="value">The nonempty identifier text to mutate.</param>
    /// <param name="kind">The requested mutation.</param>
    /// <returns>The mutated identifier text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is empty, malformed, or unsuitable for a specialized mutation.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or a configured limit is unsupported.</exception>
    string InvalidFromValueAndKind(string value, IdentifierMutationKind kind);
    /// <summary>
    /// Validates an identifier while reporting structure, checksum, constraints, and allocation knowledge separately.
    /// </summary>
    /// <remarks>Allocation status is descriptive only. Standard identifiers remain unknown because validation never queries a live allocation registry.</remarks>
    /// <param name="value">The identifier text to inspect.</param>
    /// <param name="kind">The structural contract to apply.</param>
    /// <returns>The detailed validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the kind or configured count limit is unsupported.</exception>
    ValidationResult ValidateIdentifier(string value, IdentifierKind kind);
    /// <summary>
    /// Attaches captured generation settings and detailed validation classifications to identifier text.
    /// </summary>
    /// <remarks>This method classifies supplied text without asserting that this instance generated it or that an external registry assigned it.</remarks>
    /// <param name="value">The identifier text to describe.</param>
    /// <param name="kind">The structural contract to apply.</param>
    /// <returns>Metadata containing the supplied value, captured settings, and validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the kind or configured count limit is unsupported.</exception>
    GenerationMetadata IdentifierMetadata(string value, IdentifierKind kind);
    /// <summary>Generates a locale-backed civil identifier selected by kind.</summary>
    /// <remarks>The result is structurally synthetic. No issuance, assignment, ownership, or registration is claimed.</remarks>
    /// <param name="kind">A passport, driving-licence, national/social identity, personal tax, business-registration, or vehicle-registration kind.</param>
    /// <returns>A value satisfying the authoritative rule resolved from the locale fallback chain.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the kind or a configured limit is unsupported.</exception>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string CivilIdentifierFromKind(IdentifierKind kind);
    /// <summary>Validates a civil identifier against its resolved locale rule.</summary>
    /// <param name="value">The identifier text to inspect.</param>
    /// <param name="kind">The civil identifier kind.</param>
    /// <returns>True when structure and every applicable checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind or a configured limit is unsupported.</exception>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    bool IsCivilIdentifierValid(string value, IdentifierKind kind);
    /// <summary>Applies a deterministic invalid mutation to a valid civil identifier.</summary>
    /// <param name="value">A value valid under the resolved locale rule.</param>
    /// <param name="kind">The civil identifier kind.</param>
    /// <param name="mutation">The requested checksum, length, character, or prefix mutation.</param>
    /// <returns>A deliberately invalid value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value is invalid or the rule has no requested checksum.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind, mutation, or a configured limit is unsupported.</exception>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string MutateCivilIdentifier(string value, IdentifierKind kind, IdentifierMutationKind mutation);
    /// <summary>Returns locale-rule, validation, safety, provenance, and fallback metadata for a civil identifier.</summary>
    /// <param name="value">The identifier text to describe.</param>
    /// <param name="kind">The civil identifier kind.</param>
    /// <returns>Metadata that never claims assignment.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind or a configured limit is unsupported.</exception>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    GenerationMetadata CivilIdentifierMetadata(string value, IdentifierKind kind);
    /// <summary>Generates a locale-backed passport identifier.</summary>
    /// <returns>A synthetic passport identifier shape.</returns>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string PassportIdentifier();
    /// <summary>Generates a locale-backed driving-licence identifier.</summary>
    /// <returns>A synthetic driving-licence identifier shape.</returns>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string DrivingLicenceIdentifier();
    /// <summary>Generates a locale-backed national identity identifier.</summary>
    /// <returns>A synthetic national identity identifier shape.</returns>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string NationalIdentityIdentifier();
    /// <summary>Generates a locale-backed social identity identifier.</summary>
    /// <returns>A synthetic social identity identifier shape.</returns>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string SocialIdentityIdentifier();
    /// <summary>Generates a locale-backed personal tax identifier.</summary>
    /// <returns>A synthetic personal tax identifier shape.</returns>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string PersonalTaxIdentifier();
    /// <summary>Generates a locale-backed business-registration identifier.</summary>
    /// <returns>A synthetic business-registration identifier shape.</returns>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string BusinessRegistrationIdentifier();
    /// <summary>Generates a locale-backed vehicle-registration identifier.</summary>
    /// <returns>A synthetic vehicle-registration identifier shape.</returns>
    /// <exception cref="FakeException">Thrown when no authoritative locale rule is available.</exception>
    string VehicleRegistrationIdentifier();
    /// <summary>
    /// Generates a checksum-valid synthetic VIN shape using the captured reference year.
    /// </summary>
    /// <remarks>The WMI is random test data and is not asserted to be assigned. The result does not identify an actual vehicle.</remarks>
    /// <returns>A 17-character VIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the captured year or a configured limit is unsupported.</exception>
    string Vin();
    /// <summary>
    /// Generates a checksum-valid synthetic VIN shape for a model year.
    /// </summary>
    /// <remarks>The generated shape uses the position-seven convention to distinguish the 1980-2009 and 2010-2039 model-year cycles. Its random WMI is not asserted to be assigned.</remarks>
    /// <param name="modelYear">A model year from 1980 through 2039.</param>
    /// <returns>A 17-character VIN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the model year or a configured limit is unsupported.</exception>
    string VinFromModelYear(int modelYear);
    /// <summary>
    /// Generates a checksum-valid synthetic VIN shape with a supplied WMI shape.
    /// </summary>
    /// <remarks>Supplying three characters does not verify WMI assignment or authorize their use.</remarks>
    /// <param name="wmi">Exactly three permitted uppercase VIN characters.</param>
    /// <param name="modelYear">A model year from 1980 through 2039.</param>
    /// <returns>A 17-character VIN shape beginning with the supplied characters.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the WMI is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the WMI is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the model year or a configured limit is unsupported.</exception>
    string VinFromWmiAndModelYear(string wmi, int modelYear);
    /// <summary>
    /// Writes a generated checksum-valid synthetic VIN shape using the captured reference year.
    /// </summary>
    /// <param name="destination">A destination containing at least 17 characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the captured year or a configured limit is unsupported.</exception>
    int WriteVin(Span<char> destination);
    /// <summary>
    /// Writes a generated checksum-valid synthetic VIN shape for a model year.
    /// </summary>
    /// <param name="destination">A destination containing at least 17 characters.</param>
    /// <param name="modelYear">A model year from 1980 through 2039.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the destination is too small.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the model year or a configured limit is unsupported.</exception>
    int WriteVinFromDestinationAndModelYear(Span<char> destination, int modelYear);
    /// <summary>
    /// Writes a generated checksum-valid synthetic VIN shape with a supplied WMI shape.
    /// </summary>
    /// <remarks>Supplying three characters does not verify WMI assignment or authorize their use.</remarks>
    /// <param name="wmi">Exactly three permitted uppercase VIN characters.</param>
    /// <param name="destination">A destination containing at least 17 characters.</param>
    /// <param name="modelYear">A model year from 1980 through 2039.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the WMI or destination is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the model year or a configured limit is unsupported.</exception>
    int WriteVinFromWmiAndDestinationAndModelYear(ReadOnlySpan<char> wmi, Span<char> destination, int modelYear);
    /// <summary>
    /// Reports whether a string has permitted VIN characters and a valid position-nine check digit.
    /// </summary>
    /// <remarks>This is structural validation only and does not verify WMI assignment, manufacture, registration, or vehicle existence.</remarks>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the 17-character shape and checksum are valid.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    bool ValidateVin(string value);
    /// <summary>
    /// Reports whether text has permitted VIN characters and a valid position-nine check digit.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True when the 17-character shape and checksum are valid.</returns>
    bool ValidateVinFromValue(ReadOnlySpan<char> value);
    /// <summary>
    /// Returns the 49 CFR Part 565 model-year code for a supported year.
    /// </summary>
    /// <param name="modelYear">A model year from 1980 through 2039.</param>
    /// <returns>The single VIN model-year code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the model year is unsupported.</exception>
    char VinModelYearCode(int modelYear);
    /// <summary>
    /// Decodes the model year from a checksum-valid VIN shape using its position-seven cycle marker.
    /// </summary>
    /// <remarks>The position-seven convention is defined for passenger cars, multipurpose passenger vehicles, and trucks of 10,000 pounds GVWR or less.</remarks>
    /// <param name="value">A checksum-valid VIN shape with a supported model-year code.</param>
    /// <returns>A model year from 1980 through 2039.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the VIN or model-year code is invalid.</exception>
    int VinModelYear(string value);
    /// <summary>
    /// Extracts the three-character WMI shape from a checksum-valid VIN.
    /// </summary>
    /// <remarks>The returned characters are not verified against an allocation registry.</remarks>
    /// <param name="value">A checksum-valid VIN shape.</param>
    /// <returns>The first three characters.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the VIN is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit cannot accommodate the result.</exception>
    string VinWmi(string value);
}
