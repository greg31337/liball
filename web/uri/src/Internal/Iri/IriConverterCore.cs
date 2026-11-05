using System;

namespace uri;

internal static class IriConverterCore
{
    internal static UriTextResult ConvertIriToUri(
        ReadOnlySpan<char> text,
        OperationLimits? limits)
    {
        if (limits == null)
        {
            return CreateFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            text.Length,
            limits,
            DiagnosticDomain.Iri);
        if (inputLimit != null)
        {
            return CreateFailure(inputLimit);
        }

        IriParseResult parsed = IriParserCore.ParseReference(text);
        if (!parsed.Success)
        {
            return CreateFailure(parsed.Diagnostic);
        }

        return WriteReference(parsed.Reference, limits.MaxOutputCharacters);
    }

    internal static UriTextResult ConvertUriToIri(
        ReadOnlySpan<char> text,
        OperationLimits? limits)
    {
        if (limits == null)
        {
            return CreateFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri));
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            text.Length,
            limits,
            DiagnosticDomain.Iri);
        if (inputLimit != null)
        {
            return CreateFailure(inputLimit);
        }

        UriParseResult parsed = UriParserCore.ParseReference(text);
        if (!parsed.Success)
        {
            return CreateFailure(parsed.Diagnostic);
        }

        return WriteIriReference(parsed.Reference, limits.MaxOutputCharacters);
    }

    internal static UriTextResult WriteReference(
        IriReference reference,
        int maximumOutputCharacters)
    {
        TextOutput output = new TextOutput(maximumOutputCharacters);
        UriDiagnostic? diagnostic;
        if (reference.HasScheme)
        {
            output.AppendText(reference.Scheme);
            output.AppendCharacter(':');
        }

        if (reference.HasAuthority)
        {
            output.AppendText("//");
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                diagnostic = WriteEncodedComponent(
                    authority.UserInfo.AsSpan(),
                    PercentEncodeSet.UserInfo,
                    UriComponent.UserInfo,
                    output);
                if (diagnostic != null)
                {
                    return CreateFailure(diagnostic);
                }

                output.AppendCharacter('@');
            }

            bool ipLiteral = UriComposerCore.IsIpLiteral(authority.HostKind);
            if (ipLiteral)
            {
                output.AppendCharacter('[');
            }

            if (authority.HostKind == UriHostKind.RegisteredName)
            {
                diagnostic = WriteEncodedComponent(
                    authority.Host.AsSpan(),
                    PercentEncodeSet.RegisteredName,
                    UriComponent.Host,
                    output);
                if (diagnostic != null)
                {
                    return CreateFailure(diagnostic);
                }
            }
            else
            {
                output.AppendText(authority.Host);
            }

            if (ipLiteral)
            {
                output.AppendCharacter(']');
            }

            if (authority.HasPort)
            {
                output.AppendCharacter(':');
                output.AppendText(authority.Port);
            }
        }

        diagnostic = WriteEncodedComponent(
            reference.Path.AsSpan(),
            PercentEncodeSet.Path,
            UriComponent.Path,
            output);
        if (diagnostic != null)
        {
            return CreateFailure(diagnostic);
        }

        if (reference.HasQuery)
        {
            output.AppendCharacter('?');
            diagnostic = WriteEncodedComponent(
                reference.Query.AsSpan(),
                PercentEncodeSet.Query,
                UriComponent.Query,
                output);
            if (diagnostic != null)
            {
                return CreateFailure(diagnostic);
            }
        }

        if (reference.HasFragment)
        {
            output.AppendCharacter('#');
            diagnostic = WriteEncodedComponent(
                reference.Fragment.AsSpan(),
                PercentEncodeSet.Fragment,
                UriComponent.Fragment,
                output);
            if (diagnostic != null)
            {
                return CreateFailure(diagnostic);
            }
        }

        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return CreateOutputFailure(status.ErrorOffset, status.ErrorLength);
        }

        return new UriTextResult(true, output.GetText(), output.GetLength(), null);
    }

    internal static UriTextResult WriteIriReference(
        UriReference reference,
        int maximumOutputCharacters)
    {
        TextOutput output = new TextOutput(maximumOutputCharacters);
        if (reference.HasScheme)
        {
            output.AppendText(reference.Scheme);
            output.AppendCharacter(':');
        }

        if (reference.HasAuthority)
        {
            output.AppendText("//");
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                WriteDecodedComponent(
                    authority.UserInfo.AsSpan(),
                    PercentEncodeSet.UserInfo,
                    output);
                output.AppendCharacter('@');
            }

            bool ipLiteral = UriComposerCore.IsIpLiteral(authority.HostKind);
            if (ipLiteral)
            {
                output.AppendCharacter('[');
            }

            if (authority.HostKind == UriHostKind.RegisteredName)
            {
                WriteDecodedComponent(
                    authority.Host.AsSpan(),
                    PercentEncodeSet.RegisteredName,
                    output);
            }
            else
            {
                output.AppendText(authority.Host);
            }

            if (ipLiteral)
            {
                output.AppendCharacter(']');
            }

            if (authority.HasPort)
            {
                output.AppendCharacter(':');
                output.AppendText(authority.Port);
            }
        }

        WriteDecodedComponent(
            reference.Path.AsSpan(),
            PercentEncodeSet.Path,
            output);
        if (reference.HasQuery)
        {
            output.AppendCharacter('?');
            WriteDecodedComponent(
                reference.Query.AsSpan(),
                PercentEncodeSet.Query,
                output);
        }

        if (reference.HasFragment)
        {
            output.AppendCharacter('#');
            WriteDecodedComponent(
                reference.Fragment.AsSpan(),
                PercentEncodeSet.Fragment,
                output);
        }

        TextOutputResult status = output.GetStatus();
        if (!status.Success)
        {
            return CreateOutputFailure(status.ErrorOffset, status.ErrorLength);
        }

        return new UriTextResult(true, output.GetText(), output.GetLength(), null);
    }

    internal static UriDiagnostic? WriteEncodedComponent(
        ReadOnlySpan<char> text,
        PercentEncodeSet encodeSet,
        UriComponent component,
        TextOutput output)
    {
        byte[] scalarBytes = new byte[4];
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    0,
                    component);
                if (!triplet.Success)
                {
                    return IriComponentScanner.MapDiagnostic(triplet.Diagnostic);
                }

                output.AppendSpan(text.Slice(offset, 3));
                offset = triplet.NextOffset;
                continue;
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            if (!scalar.Success)
            {
                return DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.Iri,
                    component,
                    scalar.ErrorOffset,
                    scalar.ErrorLength);
            }

            if (scalar.ScalarValue <= 127 &&
                PercentAllowlist.IsAllowed((char)scalar.ScalarValue, encodeSet))
            {
                output.AppendCharacter((char)scalar.ScalarValue);
            }
            else
            {
                int byteCount = Utf8CodecCore.WriteScalar(
                    scalar.ScalarValue,
                    scalarBytes,
                    0);
                int byteOffset = 0;
                while (byteOffset < byteCount)
                {
                    AppendEncodedByte(scalarBytes[byteOffset], output);
                    byteOffset = byteOffset + 1;
                }
            }

            offset = offset + scalar.CodeUnitLength;
        }

        return null;
    }

    internal static void WriteDecodedComponent(
        ReadOnlySpan<char> text,
        PercentEncodeSet encodeSet,
        TextOutput output)
    {
        byte[] bytes = new byte[4];
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] != '%')
            {
                output.AppendCharacter(text[offset]);
                offset = offset + 1;
                continue;
            }

            PercentTripletResult first = PercentScanner.ScanTriplet(
                text,
                offset,
                0,
                UriComponent.Input);
            bytes[0] = (byte)first.Value;
            int expectedLength = GetUtf8SequenceLength(bytes[0]);
            int byteCount = 1;
            int nextOffset = first.NextOffset;
            while (byteCount < expectedLength &&
                nextOffset < text.Length &&
                text[nextOffset] == '%')
            {
                PercentTripletResult next = PercentScanner.ScanTriplet(
                    text,
                    nextOffset,
                    0,
                    UriComponent.Input);
                bytes[byteCount] = (byte)next.Value;
                byteCount = byteCount + 1;
                nextOffset = next.NextOffset;
            }

            if (byteCount == expectedLength)
            {
                Utf8ScalarResult scalar = Utf8CodecCore.DecodeScalar(
                    bytes.AsSpan(0, byteCount),
                    0);
                if (scalar.Success && IsDecodedScalarSafe(scalar.ScalarValue, encodeSet))
                {
                    AppendDecodedScalar(scalar.ScalarValue, output);
                    offset = nextOffset;
                    continue;
                }
            }

            output.AppendSpan(text.Slice(offset, 3));
            offset = first.NextOffset;
        }
    }

    internal static int GetUtf8SequenceLength(byte firstByte)
    {
        if (firstByte <= 127)
        {
            return 1;
        }

        if (firstByte >= 194 && firstByte <= 223)
        {
            return 2;
        }

        if (firstByte >= 224 && firstByte <= 239)
        {
            return 3;
        }

        if (firstByte >= 240 && firstByte <= 244)
        {
            return 4;
        }

        return 1;
    }

    internal static bool IsDecodedScalarSafe(
        int scalarValue,
        PercentEncodeSet encodeSet)
    {
        if (IsBidiFormattingCharacter(scalarValue))
        {
            return false;
        }

        if (scalarValue >= 0 && scalarValue <= 127)
        {
            return Ascii.IsUnreserved((char)scalarValue);
        }

        if (encodeSet == PercentEncodeSet.UserInfo)
        {
            return IriGrammar.IsUserInfoCharacter(scalarValue);
        }

        if (encodeSet == PercentEncodeSet.RegisteredName)
        {
            return IriGrammar.IsRegisteredNameCharacter(scalarValue);
        }

        if (encodeSet == PercentEncodeSet.Path)
        {
            return IriGrammar.IsPathCharacter(scalarValue);
        }

        if (encodeSet == PercentEncodeSet.Query)
        {
            return IriGrammar.IsQueryCharacter(scalarValue);
        }

        return IriGrammar.IsFragmentCharacter(scalarValue);
    }

    internal static bool IsBidiFormattingCharacter(int scalarValue)
    {
        if (scalarValue == 8206 || scalarValue == 8207)
        {
            return true;
        }

        return scalarValue >= 8234 && scalarValue <= 8238;
    }

    internal static void AppendDecodedScalar(int scalarValue, TextOutput output)
    {
        int characterCount = UnicodeScalar.GetCodeUnitLength(scalarValue);
        TextOutputResult available = output.CheckAvailable(characterCount);
        if (!available.Success)
        {
            return;
        }

        output.EnsureCapacity(characterCount);
        output.Length = PercentDecoderCore.WriteScalar(
            scalarValue,
            output.Characters,
            output.Length);
    }

    internal static void AppendEncodedByte(byte value, TextOutput output)
    {
        TextOutputResult available = output.CheckAvailable(3);
        if (!available.Success)
        {
            return;
        }

        output.EnsureCapacity(3);
        output.Length = PercentEncoderCore.WritePercentByte(
            value,
            output.Characters,
            output.Length);
    }

    internal static UriTextResult CreateOutputFailure(int offset, int length)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            offset,
            length,
            LimitGuard.OutputLimitMessage);
        return CreateFailure(diagnostic);
    }

    internal static UriTextResult CreateFailure(UriDiagnostic? diagnostic)
    {
        return new UriTextResult(
            false,
            "",
            0,
            IriComponentScanner.MapDiagnostic(diagnostic));
    }
}
