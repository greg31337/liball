using System;
using System.Collections.Generic;

namespace uri;

internal static class PercentScanner
{
    internal static PercentTripletResult ScanTriplet(
        ReadOnlySpan<char> text,
        int tripletOffset,
        int baseOffset,
        UriComponent component)
    {
        if (tripletOffset < 0)
        {
            UriDiagnostic invalidOffset = DiagnosticFactory.CreateInvalidPercentEncoding(
                component,
                GetAbsoluteOffset(baseOffset, 0),
                0);
            return new PercentTripletResult(false, 0, tripletOffset, tripletOffset, invalidOffset);
        }

        if (tripletOffset >= text.Length)
        {
            UriDiagnostic missingTriplet = DiagnosticFactory.CreateInvalidPercentEncoding(
                component,
                GetAbsoluteOffset(baseOffset, text.Length),
                0);
            return new PercentTripletResult(false, 0, tripletOffset, tripletOffset, missingTriplet);
        }

        if (text[tripletOffset] != '%')
        {
            UriDiagnostic missingMarker = DiagnosticFactory.CreateInvalidPercentEncoding(
                component,
                GetAbsoluteOffset(baseOffset, tripletOffset),
                1);
            return new PercentTripletResult(false, 0, tripletOffset, tripletOffset, missingMarker);
        }

        if (text.Length - tripletOffset < 3)
        {
            UriDiagnostic incomplete = DiagnosticFactory.CreateInvalidPercentEncoding(
                component,
                GetAbsoluteOffset(baseOffset, tripletOffset),
                1);
            return new PercentTripletResult(false, 0, tripletOffset, tripletOffset, incomplete);
        }

        int high = Ascii.GetHexValue(text[tripletOffset + 1]);
        if (high < 0)
        {
            UriDiagnostic invalidHigh = DiagnosticFactory.CreateInvalidPercentEncoding(
                component,
                GetAbsoluteOffset(baseOffset, tripletOffset + 1),
                1);
            return new PercentTripletResult(false, 0, tripletOffset, tripletOffset, invalidHigh);
        }

        int low = Ascii.GetHexValue(text[tripletOffset + 2]);
        if (low < 0)
        {
            UriDiagnostic invalidLow = DiagnosticFactory.CreateInvalidPercentEncoding(
                component,
                GetAbsoluteOffset(baseOffset, tripletOffset + 2),
                1);
            return new PercentTripletResult(false, 0, tripletOffset, tripletOffset, invalidLow);
        }

        return new PercentTripletResult(
            true,
            high * 16 + low,
            tripletOffset,
            tripletOffset + 3,
            null);
    }

    internal static UriDiagnostic? ValidateFirstError(
        ReadOnlySpan<char> text,
        int baseOffset,
        UriComponent component)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = ScanTriplet(text, offset, baseOffset, component);
                if (!triplet.Success)
                {
                    return triplet.Diagnostic;
                }

                offset = triplet.NextOffset;
            }
            else
            {
                offset = offset + 1;
            }
        }

        return null;
    }

    internal static UriDiagnostic[] ValidateAllErrors(
        ReadOnlySpan<char> text,
        int baseOffset,
        UriComponent component)
    {
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] != '%')
            {
                offset = offset + 1;
                continue;
            }

            if (text.Length - offset < 3)
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidPercentEncoding(
                    component,
                    GetAbsoluteOffset(baseOffset, offset),
                    1));
                offset = text.Length;
                continue;
            }

            if (!Ascii.IsHexDigit(text[offset + 1]))
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidPercentEncoding(
                    component,
                    GetAbsoluteOffset(baseOffset, offset + 1),
                    1));
            }

            if (!Ascii.IsHexDigit(text[offset + 2]))
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidPercentEncoding(
                    component,
                    GetAbsoluteOffset(baseOffset, offset + 2),
                    1));
            }

            offset = offset + 3;
        }

        return diagnostics.ToArray();
    }

    internal static int GetAbsoluteOffset(int baseOffset, int relativeOffset)
    {
        if (baseOffset < 0)
        {
            return relativeOffset;
        }

        if (relativeOffset > int.MaxValue - baseOffset)
        {
            return int.MaxValue;
        }

        return baseOffset + relativeOffset;
    }
}
