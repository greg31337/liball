using System;

namespace websocket;

internal class WebSocketExtensionOfferFormatResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int RequiredLength;
    internal int ProducedCount;
    internal int OfferCount;
    internal int ParameterCount;
    internal int MaximumTokenLength;
    internal string Value = "";
}

internal static class WebSocketExtensionOfferFormatter
{
    internal static WebSocketExtensionOfferFormatResult FormatOffers(
        WebSocketExtensionOffer[]? offers)
    {
        WebSocketExtensionOfferFormatResult measurement = MeasureOffers(offers);
        if (measurement.Status != WebSocketParseStatus.Complete)
        {
            return measurement;
        }

        char[] output = new char[measurement.RequiredLength];
        WebSocketExtensionOfferFormatResult write = WriteOffers(offers, output);
        if (write.Status != WebSocketParseStatus.Complete)
        {
            return write;
        }

        write.Value = new string(output);
        return write;
    }

    internal static WebSocketExtensionOfferFormatResult MeasureOffers(
        WebSocketExtensionOffer[]? offers)
    {
        WebSocketExtensionOfferFormatResult result =
            new WebSocketExtensionOfferFormatResult();
        if (offers == null)
        {
            return result;
        }

        int length = 0;
        int offerIndex = 0;
        while (offerIndex < offers.Length)
        {
            WebSocketExtensionOffer offer = offers[offerIndex];
            if (offer == null ||
                offer.Name == null ||
                offer.Parameters == null ||
                !AsciiToken.IsValid(offer.Name))
            {
                return result;
            }

            if (offerIndex != 0)
            {
                length = AddLength(length, 2);
            }

            length = AddLength(length, offer.Name.Length);
            if (length < 0)
            {
                result.Status = WebSocketParseStatus.LimitRejected;
                return result;
            }

            UpdateMaximumTokenLength(result, offer.Name.Length);
            int parameterIndex = 0;
            while (parameterIndex < offer.Parameters.Length)
            {
                WebSocketExtensionParameter parameter = offer.Parameters[parameterIndex];
                if (parameter == null ||
                    parameter.Name == null ||
                    parameter.Value == null ||
                    !AsciiToken.IsValid(parameter.Name) ||
                    (parameter.HasValue && !AsciiToken.IsValid(parameter.Value)) ||
                    (!parameter.HasValue && parameter.Value.Length != 0))
                {
                    return result;
                }

                length = AddLength(length, 2);
                length = AddLength(length, parameter.Name.Length);
                if (parameter.HasValue)
                {
                    length = AddLength(length, 1);
                    length = AddLength(length, parameter.Value.Length);
                }

                if (length < 0 || result.ParameterCount == int.MaxValue)
                {
                    result.Status = WebSocketParseStatus.LimitRejected;
                    return result;
                }

                result.ParameterCount = result.ParameterCount + 1;
                UpdateMaximumTokenLength(result, parameter.Name.Length);
                if (parameter.HasValue)
                {
                    UpdateMaximumTokenLength(result, parameter.Value.Length);
                }

                parameterIndex = parameterIndex + 1;
            }

            offerIndex = offerIndex + 1;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.RequiredLength = length;
        result.OfferCount = offers.Length;
        return result;
    }

    internal static WebSocketExtensionOfferFormatResult WriteOffers(
        WebSocketExtensionOffer[]? offers,
        Span<char> destination)
    {
        WebSocketExtensionOfferFormatResult result = MeasureOffers(offers);
        if (result.Status != WebSocketParseStatus.Complete)
        {
            return result;
        }

        if (destination.Length < result.RequiredLength)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            return result;
        }

        if (offers == null)
        {
            return result;
        }

        int offset = 0;
        int offerIndex = 0;
        while (offerIndex < offers.Length)
        {
            if (offerIndex != 0)
            {
                destination[offset] = ',';
                destination[offset + 1] = ' ';
                offset = offset + 2;
            }

            WebSocketExtensionOffer offer = offers[offerIndex];
            offset = WriteText(offer.Name, destination, offset);
            int parameterIndex = 0;
            while (parameterIndex < offer.Parameters.Length)
            {
                WebSocketExtensionParameter parameter = offer.Parameters[parameterIndex];
                destination[offset] = ';';
                destination[offset + 1] = ' ';
                offset = offset + 2;
                offset = WriteText(parameter.Name, destination, offset);
                if (parameter.HasValue)
                {
                    destination[offset] = '=';
                    offset = offset + 1;
                    offset = WriteText(parameter.Value, destination, offset);
                }

                parameterIndex = parameterIndex + 1;
            }

            offerIndex = offerIndex + 1;
        }

        result.ProducedCount = offset;
        return result;
    }

    static int AddLength(int current, int addition)
    {
        if (current < 0 || addition < 0 || current > int.MaxValue - addition)
        {
            return -1;
        }

        return current + addition;
    }

    static void UpdateMaximumTokenLength(
        WebSocketExtensionOfferFormatResult result,
        int length)
    {
        if (length > result.MaximumTokenLength)
        {
            result.MaximumTokenLength = length;
        }
    }

    static int WriteText(string value, Span<char> destination, int offset)
    {
        value.AsSpan().CopyTo(destination.Slice(offset));
        return offset + value.Length;
    }
}
