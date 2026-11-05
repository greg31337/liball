namespace sip;

internal static class SipIceServiceCore
{
    internal static SipIceResult InspectDescription(
        SipSessionDescription? description)
    {
        if (description == null || description.Attributes == null
            || description.MediaDescriptions == null)
            return Failure("A complete SDP description is required.");
        SipIceDescription ice = new SipIceDescription();
        SipIceParseState state = new SipIceParseState();
        ReadSessionAttributes(description.Attributes, ice, state);
        if (state.ErrorMessage != null) return Failure(state.ErrorMessage);
        SipIceMedia[] media = new SipIceMedia[
            description.MediaDescriptions.Length];
        int index = 0;
        while (index < media.Length)
        {
            SipMediaDescription? source =
                description.MediaDescriptions[index];
            if (source == null || source.Attributes == null)
                return Failure("Every SDP media description must be complete.");
            SipIceMedia item = new SipIceMedia();
            ReadMediaAttributes(source.Attributes, item, state);
            if (state.ErrorMessage != null) return Failure(state.ErrorMessage);
            media[index] = item;
            index = index + 1;
        }
        ice.Media = media;
        string? validation = ValidateIceDescription(ice, false);
        if (validation != null) return Failure(validation);
        if (ContainsText(ice.Options, "trickle")
            && ice.Media.Length != description.MediaDescriptions.Length)
            return Failure(
                "Trickle ICE requires signaling for every SDP media section.");
        SipIceResult result = Success();
        result.Description = CopyIceDescription(ice);
        return result;
    }

    internal static SipIceResult ApplyDescription(
        SipSessionDescription? description, SipIceDescription? ice)
    {
        if (description == null || ice == null
            || description.MediaDescriptions == null
            || ice.Media == null
            || ice.Media.Length > description.MediaDescriptions.Length)
            return Failure("An SDP description and matching ICE signaling are required.");
        string? validation = ValidateIceDescription(ice, false);
        if (validation != null) return Failure(validation);
        SipSessionDescription? copy = CopySessionDescription(description);
        if (copy == null) return Failure("The SDP description cannot be copied.");
        copy.Attributes = WithoutIceAttributes(
            copy.Attributes, false, false);
        copy.Attributes = AddSessionIceAttributes(copy.Attributes, ice);
        int index = 0;
        while (index < copy.MediaDescriptions.Length)
        {
            SipMediaDescription media = copy.MediaDescriptions[index];
            bool replaceMid = index < ice.Media.Length
                && ice.Media[index].Mid != null;
            media.Attributes = WithoutIceAttributes(
                media.Attributes, true, replaceMid);
            if (index < ice.Media.Length)
                media.Attributes = AddMediaIceAttributes(
                    media.Attributes, ice.Media[index]);
            index = index + 1;
        }
        copy.OriginalBytes = new byte[0];
        SipValidationIssue[] issues =
            SipSdpValidator.ValidateDescriptionModel(copy, true);
        if (issues.Length > 0)
        {
            string? issueMessage = issues[0].Message;
            return Failure(issueMessage == null
                ? "The updated SDP description is invalid."
                : issueMessage);
        }
        SipIceResult inspected = InspectDescription(copy);
        if (!inspected.Success) return inspected;
        inspected.SessionDescription = copy;
        return inspected;
    }

    internal static SipIceResult CreateTrickleInfo(
        SipIceTrickleRequest? request)
    {
        if (request == null || request.Description == null
            || request.DialogId == null)
            return Failure("A dialog and Trickle ICE description are required.");
        string? validation = ValidateIceDescription(
            request.Description, true);
        if (validation != null) return Failure(validation);
        byte[] fragment = FormatTrickleFragment(request.Description);
        SipInfoRequest info = new SipInfoRequest();
        info.DialogId = CopyDialogId(request.DialogId);
        info.Package = SipOptionTags.TrickleIce;
        info.Body = CopyBytes(fragment);
        info.ContentType = new SipMediaType();
        info.ContentType.Type = "application";
        info.ContentType.Subtype = "trickle-ice-sdpfrag";
        info.CorrelationId = request.CorrelationId;
        SipIceResult result = Success();
        result.Description = CopyIceDescription(request.Description);
        result.Fragment = fragment;
        result.InfoRequest = info;
        result.TrickleIceSupported = true;
        return result;
    }

    internal static SipIceResult ParseTrickleFragment(byte[]? fragment)
    {
        if (fragment == null || fragment.Length == 0
            || fragment.Length > SipDefaults.MaxBodyBytes)
            return Failure("A bounded Trickle ICE SDP fragment is required.");
        string text = System.Text.Encoding.UTF8.GetString(fragment);
        string[] lines = text.Split('\n');
        SipIceDescription description = new SipIceDescription();
        System.Collections.Generic.List<SipSdpAttribute> sessionExtensions =
            new System.Collections.Generic.List<SipSdpAttribute>();
        System.Collections.Generic.List<SipIceMedia> media =
            new System.Collections.Generic.List<SipIceMedia>();
        SipIceMedia? current = null;
        SipIceParseState state = new SipIceParseState();
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.EndsWith("\r", System.StringComparison.Ordinal))
                line = line.Substring(0, line.Length - 1);
            if (line.Length == 0)
            {
                if (index != lines.Length - 1)
                    return Failure("A Trickle ICE fragment contains an empty line.");
            }
            else if (line.StartsWith("m=", System.StringComparison.Ordinal))
            {
                if (!ValidPseudoMediaLine(line.Substring(2)))
                    return Failure("A Trickle ICE pseudo media line is invalid.");
                current = new SipIceMedia();
                media.Add(current);
            }
            else if (line.StartsWith("a=", System.StringComparison.Ordinal))
            {
                SipSdpAttribute? attribute = ParseAttributeLine(
                    line.Substring(2));
                if (attribute == null)
                    return Failure("A Trickle ICE attribute line is invalid.");
                if (current == null)
                    ReadFragmentSessionAttribute(attribute, description,
                        sessionExtensions, state);
                else
                    ReadFragmentMediaAttribute(attribute, current, state);
                if (state.ErrorMessage != null)
                    return Failure(state.ErrorMessage);
            }
            else
            {
                return Failure(
                    "Only attributes and pseudo media lines are allowed in a Trickle ICE fragment.");
            }
            index = index + 1;
        }
        description.Media = media.ToArray();
        description.ExtensionAttributes = sessionExtensions.ToArray();
        string? validation = ValidateIceDescription(description, true);
        if (validation != null) return Failure(validation);
        SipIceResult result = Success();
        result.Description = CopyIceDescription(description);
        result.Fragment = CopyBytes(fragment);
        result.TrickleIceSupported = true;
        return result;
    }

    internal static SipIceResult ApplyTrickleFragment(
        SipSessionDescription? description, byte[]? fragment)
    {
        SipIceResult parsed = ParseTrickleFragment(fragment);
        if (!parsed.Success || parsed.Description == null) return parsed;
        SipIceResult current = InspectDescription(description);
        if (!current.Success || current.Description == null
            || description == null) return current;
        SipSessionDescription? copy = CopySessionDescription(description);
        if (copy == null) return Failure("The SDP description cannot be copied.");
        SipIceDescription update = parsed.Description;
        if (!CredentialsMatch(current.Description.UsernameFragment,
                current.Description.Password, update.UsernameFragment,
                update.Password))
            return Conflict("The Trickle ICE session credentials do not match the current generation.");
        int updateIndex = 0;
        while (updateIndex < update.Media.Length)
        {
            SipIceMedia incoming = update.Media[updateIndex];
            int targetIndex = FindMediaIndex(
                current.Description.Media, incoming.Mid);
            if (targetIndex < 0
                || targetIndex >= copy.MediaDescriptions.Length)
                return Conflict("A Trickle ICE media identification tag is unknown.");
            SipIceMedia existing = current.Description.Media[targetIndex];
            string? existingUser = existing.UsernameFragment == null
                ? current.Description.UsernameFragment
                : existing.UsernameFragment;
            string? existingPassword = existing.Password == null
                ? current.Description.Password : existing.Password;
            string? incomingUser = incoming.UsernameFragment == null
                ? update.UsernameFragment : incoming.UsernameFragment;
            string? incomingPassword = incoming.Password == null
                ? update.Password : incoming.Password;
            if (!CredentialsMatch(existingUser, existingPassword,
                    incomingUser, incomingPassword))
                return Conflict("The Trickle ICE media credentials do not match the current generation.");
            MergeMediaUpdate(copy.MediaDescriptions[targetIndex], incoming);
            updateIndex = updateIndex + 1;
        }
        if (update.EndOfCandidates
            && !HasAttribute(copy.Attributes, "end-of-candidates"))
            copy.Attributes = AppendAttribute(copy.Attributes,
                PropertyAttribute("end-of-candidates"));
        copy.Attributes = AppendAttributes(
            copy.Attributes, update.ExtensionAttributes);
        copy.OriginalBytes = new byte[0];
        SipIceResult inspected = InspectDescription(copy);
        if (!inspected.Success) return inspected;
        inspected.SessionDescription = copy;
        inspected.Fragment = fragment == null
            ? new byte[0] : CopyBytes(fragment);
        return inspected;
    }

    internal static SipIceResult InspectMessageOptions(SipMessage? message)
    {
        if (message == null || message.Headers == null)
            return Failure("A SIP message is required.");
        SipIceResult result = Success();
        result.IceSupported = HeaderHasToken(
            message, SipHeaderNames.Supported, SipOptionTags.Ice);
        result.TrickleIceSupported = HeaderHasToken(
            message, SipHeaderNames.Supported, SipOptionTags.TrickleIce)
            || HeaderHasToken(
                message, SipHeaderNames.Require, SipOptionTags.TrickleIce);
        result.TrickleIceRequired = HeaderHasToken(
            message, SipHeaderNames.Require, SipOptionTags.TrickleIce);
        return result;
    }

    internal static SipIceResult AddMessageOptions(SipMessage? message,
        bool supportTraditionalIce, bool requireTrickleIce)
    {
        if (message == null) return Failure("A SIP message is required.");
        SipMessage? copy = SipSnapshotCopy.CopyMessageModel(message);
        if (copy == null) return Failure("The SIP message cannot be copied.");
        System.Collections.Generic.List<string> supported =
            ReadHeaderTokens(copy, SipHeaderNames.Supported);
        if (supportTraditionalIce) AddUniqueToken(
            supported, SipOptionTags.Ice);
        AddUniqueToken(supported, SipOptionTags.TrickleIce);
        ReplaceTokenHeader(copy, SipHeaderNames.Supported, supported);
        if (requireTrickleIce)
        {
            System.Collections.Generic.List<string> required =
                ReadHeaderTokens(copy, SipHeaderNames.Require);
            AddUniqueToken(required, SipOptionTags.TrickleIce);
            ReplaceTokenHeader(copy, SipHeaderNames.Require, required);
        }
        SipIceResult result = InspectMessageOptions(copy);
        if (!result.Success) return result;
        result.Message = copy;
        return result;
    }

    static void ReadSessionAttributes(SipSdpAttribute[] attributes,
        SipIceDescription ice, SipIceParseState state)
    {
        System.Collections.Generic.List<string> options =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<SipSdpAttribute> extensions =
            new System.Collections.Generic.List<SipSdpAttribute>();
        int index = 0;
        while (index < attributes.Length && state.ErrorMessage == null)
        {
            SipSdpAttribute? attribute = attributes[index];
            if (attribute == null)
            {
                state.ErrorMessage = "An SDP attribute cannot be null.";
            }
            else if (NameIs(attribute, "ice-ufrag"))
            {
                if (ice.UsernameFragment != null
                    || !ValidCredential(attribute, 4))
                    state.ErrorMessage = "The session ice-ufrag attribute is invalid or repeated.";
                else ice.UsernameFragment = attribute.Value;
            }
            else if (NameIs(attribute, "ice-pwd"))
            {
                if (ice.Password != null || !ValidCredential(attribute, 22))
                    state.ErrorMessage = "The session ice-pwd attribute is invalid or repeated.";
                else ice.Password = attribute.Value;
            }
            else if (NameIs(attribute, "ice-options"))
            {
                if (!attribute.HasValue || attribute.Value == null
                    || !AddSpaceTokens(options, attribute.Value))
                    state.ErrorMessage = "The ice-options attribute is invalid.";
            }
            else if (NameIs(attribute, "ice-lite"))
            {
                if (ice.IceLite || attribute.HasValue)
                    state.ErrorMessage = "The ice-lite attribute is invalid or repeated.";
                else ice.IceLite = true;
            }
            else if (NameIs(attribute, "end-of-candidates"))
            {
                if (ice.EndOfCandidates || attribute.HasValue)
                    state.ErrorMessage = "The end-of-candidates attribute is invalid or repeated.";
                else ice.EndOfCandidates = true;
            }
            else if (IsIceExtension(attribute))
            {
                extensions.Add(CopyAttribute(attribute));
            }
            index = index + 1;
        }
        ice.Options = options.ToArray();
        ice.ExtensionAttributes = extensions.ToArray();
    }

    static void ReadMediaAttributes(SipSdpAttribute[] attributes,
        SipIceMedia ice, SipIceParseState state)
    {
        System.Collections.Generic.List<SipIceCandidate> candidates =
            new System.Collections.Generic.List<SipIceCandidate>();
        System.Collections.Generic.List<SipSdpAttribute> extensions =
            new System.Collections.Generic.List<SipSdpAttribute>();
        int index = 0;
        while (index < attributes.Length && state.ErrorMessage == null)
        {
            SipSdpAttribute? attribute = attributes[index];
            if (attribute == null)
                state.ErrorMessage = "An SDP media attribute cannot be null.";
            else if (NameIs(attribute, "mid"))
            {
                if (ice.Mid != null || !ValidAttributeValue(attribute))
                    state.ErrorMessage = "The media mid attribute is invalid or repeated.";
                else ice.Mid = attribute.Value;
            }
            else if (NameIs(attribute, "ice-ufrag"))
            {
                if (ice.UsernameFragment != null
                    || !ValidCredential(attribute, 4))
                    state.ErrorMessage = "The media ice-ufrag attribute is invalid or repeated.";
                else ice.UsernameFragment = attribute.Value;
            }
            else if (NameIs(attribute, "ice-pwd"))
            {
                if (ice.Password != null || !ValidCredential(attribute, 22))
                    state.ErrorMessage = "The media ice-pwd attribute is invalid or repeated.";
                else ice.Password = attribute.Value;
            }
            else if (NameIs(attribute, "candidate"))
            {
                SipIceCandidate? candidate = ParseCandidate(attribute.Value);
                if (!attribute.HasValue || candidate == null)
                    state.ErrorMessage = "An ICE candidate attribute is invalid.";
                else candidates.Add(candidate);
            }
            else if (NameIs(attribute, "end-of-candidates"))
            {
                if (ice.EndOfCandidates || attribute.HasValue)
                    state.ErrorMessage = "The media end-of-candidates attribute is invalid or repeated.";
                else ice.EndOfCandidates = true;
            }
            else if (IsIceExtension(attribute))
            {
                extensions.Add(CopyAttribute(attribute));
            }
            index = index + 1;
        }
        ice.Candidates = candidates.ToArray();
        ice.ExtensionAttributes = extensions.ToArray();
    }

    static void ReadFragmentSessionAttribute(SipSdpAttribute attribute,
        SipIceDescription description,
        System.Collections.Generic.List<SipSdpAttribute> extensions,
        SipIceParseState state)
    {
        if (NameIs(attribute, "ice-ufrag"))
        {
            if (description.UsernameFragment != null
                || !ValidCredential(attribute, 4))
                state.ErrorMessage = "The fragment ice-ufrag is invalid or repeated.";
            else description.UsernameFragment = attribute.Value;
        }
        else if (NameIs(attribute, "ice-pwd"))
        {
            if (description.Password != null
                || !ValidCredential(attribute, 22))
                state.ErrorMessage = "The fragment ice-pwd is invalid or repeated.";
            else description.Password = attribute.Value;
        }
        else if (NameIs(attribute, "ice-options"))
        {
            System.Collections.Generic.List<string> values =
                new System.Collections.Generic.List<string>();
            AddStrings(values, description.Options);
            if (!attribute.HasValue || attribute.Value == null
                || !AddSpaceTokens(values, attribute.Value))
                state.ErrorMessage = "The fragment ice-options attribute is invalid.";
            else description.Options = values.ToArray();
        }
        else if (NameIs(attribute, "ice-lite"))
        {
            if (description.IceLite || attribute.HasValue)
                state.ErrorMessage = "The fragment ice-lite attribute is invalid or repeated.";
            else description.IceLite = true;
        }
        else if (NameIs(attribute, "end-of-candidates"))
        {
            if (description.EndOfCandidates || attribute.HasValue)
                state.ErrorMessage = "The fragment end-of-candidates attribute is invalid or repeated.";
            else description.EndOfCandidates = true;
        }
        else if (NameIs(attribute, "candidate")
            || NameIs(attribute, "mid"))
        {
            state.ErrorMessage = "A media ICE attribute appears before a pseudo media line.";
        }
        else
        {
            extensions.Add(CopyAttribute(attribute));
        }
    }

    static void ReadFragmentMediaAttribute(SipSdpAttribute attribute,
        SipIceMedia media, SipIceParseState state)
    {
        System.Collections.Generic.List<SipIceCandidate> candidates =
            new System.Collections.Generic.List<SipIceCandidate>();
        AddCandidates(candidates, media.Candidates);
        System.Collections.Generic.List<SipSdpAttribute> extensions =
            new System.Collections.Generic.List<SipSdpAttribute>();
        AddAttributes(extensions, media.ExtensionAttributes);
        if (NameIs(attribute, "mid"))
        {
            if (media.Mid != null || !ValidAttributeValue(attribute))
                state.ErrorMessage = "The fragment mid attribute is invalid or repeated.";
            else media.Mid = attribute.Value;
        }
        else if (NameIs(attribute, "ice-ufrag"))
        {
            if (media.UsernameFragment != null
                || !ValidCredential(attribute, 4))
                state.ErrorMessage = "The fragment media ice-ufrag is invalid or repeated.";
            else media.UsernameFragment = attribute.Value;
        }
        else if (NameIs(attribute, "ice-pwd"))
        {
            if (media.Password != null || !ValidCredential(attribute, 22))
                state.ErrorMessage = "The fragment media ice-pwd is invalid or repeated.";
            else media.Password = attribute.Value;
        }
        else if (NameIs(attribute, "candidate"))
        {
            SipIceCandidate? candidate = ParseCandidate(attribute.Value);
            if (!attribute.HasValue || candidate == null)
                state.ErrorMessage = "A fragment ICE candidate is invalid.";
            else candidates.Add(candidate);
        }
        else if (NameIs(attribute, "end-of-candidates"))
        {
            if (media.EndOfCandidates || attribute.HasValue)
                state.ErrorMessage = "The fragment media end marker is invalid or repeated.";
            else media.EndOfCandidates = true;
        }
        else
        {
            extensions.Add(CopyAttribute(attribute));
        }
        media.Candidates = candidates.ToArray();
        media.ExtensionAttributes = extensions.ToArray();
    }

    static SipIceCandidate? ParseCandidate(string? value)
    {
        if (value == null) return null;
        string[] parts = SplitSpaces(value);
        if (parts.Length < 8 || (parts.Length - 8) % 2 != 0
            || !ValidFoundation(parts[0])
            || !SipAscii.IsToken(parts[2])
            || !SipAscii.EqualsIgnoreCase(parts[6], "typ")
            || !SipAscii.IsToken(parts[7])) return null;
        long component = ParseDecimal(parts[1], 256);
        long priority = ParseDecimal(parts[3], 4294967295);
        long port = ParseDecimal(parts[5], 65535);
        if (component < 1 || priority < 1 || port < 1
            || !SafeAttributeText(parts[4])) return null;
        string[] extensions = new string[parts.Length - 8];
        int index = 8;
        while (index < parts.Length)
        {
            if (!SipAscii.IsToken(parts[index])
                || !SafeAttributeText(parts[index + 1])) return null;
            extensions[index - 8] = parts[index];
            extensions[index - 7] = parts[index + 1];
            index = index + 2;
        }
        SipIceCandidate candidate = new SipIceCandidate();
        candidate.Foundation = parts[0];
        candidate.Component = (int)component;
        candidate.Transport = parts[2];
        candidate.Priority = priority;
        candidate.Address = parts[4];
        candidate.Port = (int)port;
        candidate.Type = parts[7];
        candidate.Extensions = extensions;
        return candidate;
    }

    static string? FormatCandidate(SipIceCandidate? candidate)
    {
        if (candidate == null || !ValidFoundation(candidate.Foundation)
            || candidate.Component < 1 || candidate.Component > 256
            || !SipAscii.IsToken(candidate.Transport)
            || candidate.Priority < 1 || candidate.Priority > 4294967295
            || !SafeAttributeText(candidate.Address)
            || candidate.Port < 1 || candidate.Port > 65535
            || !SipAscii.IsToken(candidate.Type)
            || candidate.Extensions == null
            || candidate.Extensions.Length % 2 != 0) return null;
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(candidate.Foundation);
        text.Append(' ');
        text.Append(candidate.Component.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append(' ');
        text.Append(candidate.Transport);
        text.Append(' ');
        text.Append(candidate.Priority.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append(' ');
        text.Append(candidate.Address);
        text.Append(' ');
        text.Append(candidate.Port.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        text.Append(" typ ");
        text.Append(candidate.Type);
        int index = 0;
        while (index < candidate.Extensions.Length)
        {
            if (!SipAscii.IsToken(candidate.Extensions[index])
                || !SafeAttributeText(candidate.Extensions[index + 1]))
                return null;
            text.Append(' ');
            text.Append(candidate.Extensions[index]);
            text.Append(' ');
            text.Append(candidate.Extensions[index + 1]);
            index = index + 2;
        }
        return text.ToString();
    }

    static string? ValidateIceDescription(
        SipIceDescription description, bool fragment)
    {
        if (description.Options == null || description.Media == null
            || description.ExtensionAttributes == null)
            return "ICE signaling collections cannot be null.";
        if (description.UsernameFragment == null
                && description.Password != null
            || description.UsernameFragment != null
                && description.Password == null)
            return "ICE session credentials must be supplied as a pair.";
        if (fragment && description.Media.Length == 0
            && (description.UsernameFragment == null
                || description.Password == null))
            return "A Trickle ICE fragment requires ICE generation credentials.";
        if (description.UsernameFragment != null
            && !ValidCredentialText(description.UsernameFragment, 4)
            || description.Password != null
                && !ValidCredentialText(description.Password, 22))
            return "The ICE session credentials are invalid.";
        if (!ValidExtensionAttributes(description.ExtensionAttributes))
            return "An ICE session extension attribute is invalid.";
        int optionIndex = 0;
        while (optionIndex < description.Options.Length)
        {
            if (!SipAscii.IsToken(description.Options[optionIndex]))
                return "An ICE option is not a token.";
            optionIndex = optionIndex + 1;
        }
        int mediaIndex = 0;
        while (mediaIndex < description.Media.Length)
        {
            SipIceMedia? media = description.Media[mediaIndex];
            if (media == null || media.Candidates == null
                || media.ExtensionAttributes == null)
                return "An ICE media section is incomplete.";
            if (fragment && (media.Mid == null
                    || !SipAscii.IsToken(media.Mid)))
                return "Every Trickle ICE media section requires a mid token.";
            if (media.Mid != null && !SipAscii.IsToken(media.Mid)
                || media.UsernameFragment != null
                    && !ValidCredentialText(media.UsernameFragment, 4)
                || media.Password != null
                    && !ValidCredentialText(media.Password, 22))
                return "ICE media identification or credentials are invalid.";
            if (media.UsernameFragment == null && media.Password != null
                || media.UsernameFragment != null && media.Password == null)
                return "ICE media credentials must be supplied as a pair.";
            if (!ValidExtensionAttributes(media.ExtensionAttributes))
                return "An ICE media extension attribute is invalid.";
            string? user = media.UsernameFragment == null
                ? description.UsernameFragment : media.UsernameFragment;
            string? password = media.Password == null
                ? description.Password : media.Password;
            if ((media.Candidates.Length > 0 || fragment)
                && (user == null || password == null))
                return "ICE candidates require a username fragment and password.";
            int candidateIndex = 0;
            while (candidateIndex < media.Candidates.Length)
            {
                if (FormatCandidate(media.Candidates[candidateIndex]) == null)
                    return "An ICE candidate is invalid.";
                candidateIndex = candidateIndex + 1;
            }
            mediaIndex = mediaIndex + 1;
        }
        if (ContainsText(description.Options, "trickle"))
        {
            mediaIndex = 0;
            while (mediaIndex < description.Media.Length)
            {
                if (description.Media[mediaIndex].Mid == null)
                    return "Trickle ICE requires a mid for every media section.";
                mediaIndex = mediaIndex + 1;
            }
        }
        return null;
    }

    static SipSdpAttribute[] AddSessionIceAttributes(
        SipSdpAttribute[] attributes, SipIceDescription ice)
    {
        System.Collections.Generic.List<SipSdpAttribute> result =
            new System.Collections.Generic.List<SipSdpAttribute>();
        AddAttributes(result, attributes);
        if (ice.UsernameFragment != null) result.Add(ValueAttribute(
            "ice-ufrag", ice.UsernameFragment));
        if (ice.Password != null) result.Add(ValueAttribute(
            "ice-pwd", ice.Password));
        if (ice.Options.Length > 0) result.Add(ValueAttribute(
            "ice-options", JoinStrings(ice.Options, " ")));
        if (ice.IceLite) result.Add(PropertyAttribute("ice-lite"));
        if (ice.EndOfCandidates)
            result.Add(PropertyAttribute("end-of-candidates"));
        AddAttributes(result, ice.ExtensionAttributes);
        return result.ToArray();
    }

    static SipSdpAttribute[] AddMediaIceAttributes(
        SipSdpAttribute[] attributes, SipIceMedia ice)
    {
        System.Collections.Generic.List<SipSdpAttribute> result =
            new System.Collections.Generic.List<SipSdpAttribute>();
        AddAttributes(result, attributes);
        if (ice.Mid != null) result.Add(ValueAttribute("mid", ice.Mid));
        if (ice.UsernameFragment != null) result.Add(ValueAttribute(
            "ice-ufrag", ice.UsernameFragment));
        if (ice.Password != null) result.Add(ValueAttribute(
            "ice-pwd", ice.Password));
        int index = 0;
        while (index < ice.Candidates.Length)
        {
            string? candidate = FormatCandidate(ice.Candidates[index]);
            if (candidate != null)
                result.Add(ValueAttribute("candidate", candidate));
            index = index + 1;
        }
        if (ice.EndOfCandidates)
            result.Add(PropertyAttribute("end-of-candidates"));
        AddAttributes(result, ice.ExtensionAttributes);
        return result.ToArray();
    }

    static SipSdpAttribute[] WithoutIceAttributes(
        SipSdpAttribute[]? attributes, bool media, bool removeMid)
    {
        if (attributes == null) return new SipSdpAttribute[0];
        System.Collections.Generic.List<SipSdpAttribute> retained =
            new System.Collections.Generic.List<SipSdpAttribute>();
        int index = 0;
        while (index < attributes.Length)
        {
            SipSdpAttribute? attribute = attributes[index];
            bool remove = attribute == null
                || NameIs(attribute, "ice-ufrag")
                || NameIs(attribute, "ice-pwd")
                || NameIs(attribute, "ice-options")
                || NameIs(attribute, "ice-lite")
                || NameIs(attribute, "end-of-candidates")
                || media && NameIs(attribute, "candidate")
                || removeMid && NameIs(attribute, "mid");
            if (!remove && attribute != null)
                retained.Add(CopyAttribute(attribute));
            index = index + 1;
        }
        return retained.ToArray();
    }

    static byte[] FormatTrickleFragment(SipIceDescription description)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        AddIceAttributeLine(text, "ice-ufrag", description.UsernameFragment);
        AddIceAttributeLine(text, "ice-pwd", description.Password);
        if (description.Options.Length > 0)
            AddIceAttributeLine(text, "ice-options",
                JoinStrings(description.Options, " "));
        if (description.IceLite) text.Append("a=ice-lite\r\n");
        if (description.EndOfCandidates)
            text.Append("a=end-of-candidates\r\n");
        AddFragmentExtensions(text, description.ExtensionAttributes);
        int mediaIndex = 0;
        while (mediaIndex < description.Media.Length)
        {
            SipIceMedia media = description.Media[mediaIndex];
            text.Append("m=audio 9 RTP/AVP 0\r\n");
            AddIceAttributeLine(text, "mid", media.Mid);
            AddIceAttributeLine(text, "ice-ufrag", media.UsernameFragment);
            AddIceAttributeLine(text, "ice-pwd", media.Password);
            int candidateIndex = 0;
            while (candidateIndex < media.Candidates.Length)
            {
                AddIceAttributeLine(text, "candidate",
                    FormatCandidate(media.Candidates[candidateIndex]));
                candidateIndex = candidateIndex + 1;
            }
            if (media.EndOfCandidates)
                text.Append("a=end-of-candidates\r\n");
            AddFragmentExtensions(text, media.ExtensionAttributes);
            mediaIndex = mediaIndex + 1;
        }
        return System.Text.Encoding.UTF8.GetBytes(text.ToString());
    }

    static void AddIceAttributeLine(System.Text.StringBuilder text,
        string name, string? value)
    {
        if (value == null) return;
        text.Append("a=");
        text.Append(name);
        text.Append(':');
        text.Append(value);
        text.Append("\r\n");
    }

    static void AddFragmentExtensions(System.Text.StringBuilder text,
        SipSdpAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            SipSdpAttribute attribute = attributes[index];
            text.Append("a=");
            text.Append(attribute.Name);
            if (attribute.HasValue)
            {
                text.Append(':');
                text.Append(attribute.Value);
            }
            text.Append("\r\n");
            index = index + 1;
        }
    }

    static void MergeMediaUpdate(
        SipMediaDescription target, SipIceMedia incoming)
    {
        System.Collections.Generic.List<SipSdpAttribute> attributes =
            new System.Collections.Generic.List<SipSdpAttribute>();
        AddAttributes(attributes, target.Attributes);
        int index = 0;
        while (index < incoming.Candidates.Length)
        {
            string? value = FormatCandidate(incoming.Candidates[index]);
            if (value != null && !HasAttributeValue(
                    target.Attributes, "candidate", value))
                attributes.Add(ValueAttribute("candidate", value));
            index = index + 1;
        }
        if (incoming.EndOfCandidates
            && !HasAttribute(target.Attributes, "end-of-candidates"))
            attributes.Add(PropertyAttribute("end-of-candidates"));
        AddAttributes(attributes, incoming.ExtensionAttributes);
        target.Attributes = attributes.ToArray();
    }

    static SipSessionDescription? CopySessionDescription(
        SipSessionDescription? source)
    {
        if (source == null) return null;
        SipSessionDescription copy = new SipSessionDescription();
        copy.ProtocolVersion = source.ProtocolVersion;
        copy.Origin = source.Origin;
        copy.SessionName = source.SessionName;
        copy.SessionInformation = source.SessionInformation;
        copy.Uri = source.Uri;
        copy.Emails = CopyStrings(source.Emails);
        copy.Phones = CopyStrings(source.Phones);
        copy.Connection = source.Connection;
        copy.Bandwidths = CopyStrings(source.Bandwidths);
        copy.Timings = CopyStrings(source.Timings);
        copy.RepeatTimes = CopyStrings(source.RepeatTimes);
        copy.TimeZones = source.TimeZones;
        copy.EncryptionKey = source.EncryptionKey;
        copy.Attributes = CopyAttributes(source.Attributes);
        copy.UnknownLines = CopyStrings(source.UnknownLines);
        copy.OriginalBytes = CopyBytes(source.OriginalBytes);
        if (source.MediaDescriptions == null)
            copy.MediaDescriptions = new SipMediaDescription[0];
        else
        {
            copy.MediaDescriptions = new SipMediaDescription[
                source.MediaDescriptions.Length];
            int index = 0;
            while (index < source.MediaDescriptions.Length)
            {
                SipMediaDescription? media = source.MediaDescriptions[index];
                if (media == null) return null;
                copy.MediaDescriptions[index] = CopyMediaDescription(media);
                index = index + 1;
            }
        }
        return copy;
    }

    static SipMediaDescription CopyMediaDescription(
        SipMediaDescription source)
    {
        SipMediaDescription copy = new SipMediaDescription();
        copy.Media = source.Media;
        copy.Port = source.Port;
        copy.PortCount = source.PortCount;
        copy.Protocol = source.Protocol;
        copy.Formats = CopyStrings(source.Formats);
        copy.Title = source.Title;
        copy.Connection = source.Connection;
        copy.Bandwidths = CopyStrings(source.Bandwidths);
        copy.EncryptionKey = source.EncryptionKey;
        copy.Attributes = CopyAttributes(source.Attributes);
        copy.UnknownLines = CopyStrings(source.UnknownLines);
        return copy;
    }

    static SipIceDescription CopyIceDescription(
        SipIceDescription source)
    {
        SipIceDescription copy = new SipIceDescription();
        copy.UsernameFragment = source.UsernameFragment;
        copy.Password = source.Password;
        copy.Options = CopyStrings(source.Options);
        copy.IceLite = source.IceLite;
        copy.EndOfCandidates = source.EndOfCandidates;
        copy.ExtensionAttributes = CopyAttributes(
            source.ExtensionAttributes);
        copy.Media = new SipIceMedia[source.Media.Length];
        int index = 0;
        while (index < source.Media.Length)
        {
            copy.Media[index] = CopyIceMedia(source.Media[index]);
            index = index + 1;
        }
        return copy;
    }

    static SipIceMedia CopyIceMedia(SipIceMedia source)
    {
        SipIceMedia copy = new SipIceMedia();
        copy.Mid = source.Mid;
        copy.UsernameFragment = source.UsernameFragment;
        copy.Password = source.Password;
        copy.EndOfCandidates = source.EndOfCandidates;
        copy.ExtensionAttributes = CopyAttributes(
            source.ExtensionAttributes);
        copy.Candidates = new SipIceCandidate[source.Candidates.Length];
        int index = 0;
        while (index < source.Candidates.Length)
        {
            copy.Candidates[index] = CopyCandidate(source.Candidates[index]);
            index = index + 1;
        }
        return copy;
    }

    static SipIceCandidate CopyCandidate(SipIceCandidate source)
    {
        SipIceCandidate copy = new SipIceCandidate();
        copy.Foundation = source.Foundation;
        copy.Component = source.Component;
        copy.Transport = source.Transport;
        copy.Priority = source.Priority;
        copy.Address = source.Address;
        copy.Port = source.Port;
        copy.Type = source.Type;
        copy.Extensions = CopyStrings(source.Extensions);
        return copy;
    }

    static SipDialogId CopyDialogId(SipDialogId source)
    {
        SipDialogId copy = new SipDialogId();
        copy.CallId = source.CallId;
        copy.LocalTag = source.LocalTag;
        copy.RemoteTag = source.RemoteTag;
        return copy;
    }

    static SipSdpAttribute[] CopyAttributes(SipSdpAttribute[]? source)
    {
        if (source == null) return new SipSdpAttribute[0];
        SipSdpAttribute[] copy = new SipSdpAttribute[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyAttribute(source[index]);
            index = index + 1;
        }
        return copy;
    }

    static SipSdpAttribute CopyAttribute(SipSdpAttribute source)
    {
        SipSdpAttribute copy = new SipSdpAttribute();
        copy.Name = source.Name;
        copy.Value = source.Value;
        copy.HasValue = source.HasValue;
        return copy;
    }

    static SipSdpAttribute ValueAttribute(string name, string value)
    {
        SipSdpAttribute attribute = new SipSdpAttribute();
        attribute.Name = name;
        attribute.Value = value;
        attribute.HasValue = true;
        return attribute;
    }

    static SipSdpAttribute PropertyAttribute(string name)
    {
        SipSdpAttribute attribute = new SipSdpAttribute();
        attribute.Name = name;
        return attribute;
    }

    static SipSdpAttribute? ParseAttributeLine(string text)
    {
        int colon = text.IndexOf(':');
        string name = colon < 0 ? text : text.Substring(0, colon);
        string? value = colon < 0 ? null : text.Substring(colon + 1);
        if (!SipAscii.IsToken(name) || colon >= 0 && value != null
            && !SafeLineText(value)) return null;
        SipSdpAttribute attribute = new SipSdpAttribute();
        attribute.Name = name;
        attribute.Value = value;
        attribute.HasValue = colon >= 0;
        return attribute;
    }

    static bool ValidPseudoMediaLine(string value)
    {
        string[] parts = SplitSpaces(value);
        return parts.Length >= 4 && SipAscii.IsToken(parts[0])
            && ValidMediaPort(parts[1])
            && SipSdpSyntax.IsProtocol(parts[2])
            && SipAscii.IsToken(parts[3]);
    }

    static bool ValidMediaPort(string value)
    {
        int slash = value.IndexOf('/');
        string port = slash < 0 ? value : value.Substring(0, slash);
        string? count = slash < 0 ? null : value.Substring(slash + 1);
        long portValue = ParseDecimal(port, 65535);
        if (portValue < 0) return false;
        return count == null || ParseDecimal(count, 65535) > 0;
    }

    static bool ValidFoundation(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 32)
            return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool valid = character >= 'a' && character <= 'z'
                || character >= 'A' && character <= 'Z'
                || character >= '0' && character <= '9'
                || character == '+' || character == '/';
            if (!valid) return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidCredential(SipSdpAttribute attribute, int minimum)
    {
        return attribute.HasValue
            && ValidCredentialText(attribute.Value, minimum);
    }

    static bool ValidCredentialText(string? value, int minimum)
    {
        if (value == null || value.Length < minimum || value.Length > 256)
            return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool valid = character >= 'a' && character <= 'z'
                || character >= 'A' && character <= 'Z'
                || character >= '0' && character <= '9'
                || character == '+' || character == '/';
            if (!valid) return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidAttributeValue(SipSdpAttribute attribute)
    {
        return attribute.HasValue && attribute.Value != null
            && attribute.Value.Length > 0
            && SipAscii.IsToken(attribute.Value);
    }

    static bool SafeAttributeText(string? value)
    {
        if (value == null || value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\r' || character == '\n'
                || character == '\0' || character == ' '
                || character == '\t') return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidExtensionAttributes(SipSdpAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            SipSdpAttribute? attribute = attributes[index];
            if (attribute == null || !SipAscii.IsToken(attribute.Name))
                return false;
            if (attribute.HasValue
                && (attribute.Value == null
                    || !SafeLineText(attribute.Value))) return false;
            index = index + 1;
        }
        return true;
    }

    static bool SafeLineText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\r' || character == '\n'
                || character == '\0') return false;
            index = index + 1;
        }
        return true;
    }

    static bool IsIceExtension(SipSdpAttribute attribute)
    {
        return attribute.Name != null
            && attribute.Name.StartsWith(
                "ice-", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool NameIs(SipSdpAttribute attribute, string name)
    {
        return SipAscii.EqualsIgnoreCase(attribute.Name, name);
    }

    static string[] SplitSpaces(string value)
    {
        return value.Split(new char[] { ' ', '\t' },
            System.StringSplitOptions.RemoveEmptyEntries);
    }

    static long ParseDecimal(string value, long maximum)
    {
        if (value.Length == 0) return -1;
        long result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9') return -1;
            int digit = character - '0';
            if (result > (maximum - digit) / 10) return -1;
            result = result * 10 + digit;
            index = index + 1;
        }
        return result;
    }

    static bool AddSpaceTokens(
        System.Collections.Generic.List<string> values, string text)
    {
        string[] tokens = SplitSpaces(text);
        if (tokens.Length == 0) return false;
        int index = 0;
        while (index < tokens.Length)
        {
            if (!SipAscii.IsToken(tokens[index])) return false;
            AddUniqueToken(values, tokens[index]);
            index = index + 1;
        }
        return true;
    }

    static int FindMediaIndex(SipIceMedia[] media, string? mid)
    {
        if (mid == null) return -1;
        int index = 0;
        while (index < media.Length)
        {
            SipIceMedia? item = media[index];
            if (item != null && item.Mid != null && item.Mid.Equals(
                mid, System.StringComparison.Ordinal)) return index;
            index = index + 1;
        }
        return -1;
    }

    static bool CredentialsMatch(string? currentUser,
        string? currentPassword, string? incomingUser,
        string? incomingPassword)
    {
        return currentUser != null && currentPassword != null
            && incomingUser != null && incomingPassword != null
            && currentUser.Equals(incomingUser,
                System.StringComparison.Ordinal)
            && currentPassword.Equals(incomingPassword,
                System.StringComparison.Ordinal);
    }

    static bool HasAttribute(
        SipSdpAttribute[] attributes, string name)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            if (NameIs(attributes[index], name)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool HasAttributeValue(SipSdpAttribute[] attributes,
        string name, string value)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            SipSdpAttribute attribute = attributes[index];
            if (NameIs(attribute, name) && attribute.Value != null
                && attribute.Value.Equals(value,
                    System.StringComparison.Ordinal)) return true;
            index = index + 1;
        }
        return false;
    }

    static SipSdpAttribute[] AppendAttribute(
        SipSdpAttribute[] source, SipSdpAttribute added)
    {
        SipSdpAttribute[] result = new SipSdpAttribute[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyAttribute(source[index]);
            index = index + 1;
        }
        result[source.Length] = CopyAttribute(added);
        return result;
    }

    static SipSdpAttribute[] AppendAttributes(
        SipSdpAttribute[] source, SipSdpAttribute[] added)
    {
        SipSdpAttribute[] result = new SipSdpAttribute[
            source.Length + added.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyAttribute(source[index]);
            index = index + 1;
        }
        int addedIndex = 0;
        while (addedIndex < added.Length)
        {
            result[index] = CopyAttribute(added[addedIndex]);
            index = index + 1;
            addedIndex = addedIndex + 1;
        }
        return result;
    }

    static void AddAttributes(
        System.Collections.Generic.List<SipSdpAttribute> target,
        SipSdpAttribute[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(CopyAttribute(source[index]));
            index = index + 1;
        }
    }

    static void AddCandidates(
        System.Collections.Generic.List<SipIceCandidate> target,
        SipIceCandidate[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(CopyCandidate(source[index]));
            index = index + 1;
        }
    }

    static void AddStrings(
        System.Collections.Generic.List<string> target, string[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    static bool ContainsText(string[] values, string expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], expected)) return true;
            index = index + 1;
        }
        return false;
    }

    static string JoinStrings(string[] values, string separator)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) text.Append(separator);
            text.Append(values[index]);
            index = index + 1;
        }
        return text.ToString();
    }

    static bool HeaderHasToken(
        SipMessage message, string name, string token)
    {
        System.Collections.Generic.List<string> values =
            ReadHeaderTokens(message, name);
        return ContainsToken(values, token);
    }

    static System.Collections.Generic.List<string> ReadHeaderTokens(
        SipMessage message, string name)
    {
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int headerIndex = 0;
        while (headerIndex < message.Headers.Length)
        {
            SipHeader header = message.Headers[headerIndex];
            if (SipAscii.EqualsIgnoreCase(header.Name, name)
                && header.Value != null)
            {
                string[] values = header.Value.Split(',');
                int valueIndex = 0;
                while (valueIndex < values.Length)
                {
                    string token = values[valueIndex].Trim();
                    if (SipAscii.IsToken(token)) AddUniqueToken(result, token);
                    valueIndex = valueIndex + 1;
                }
            }
            headerIndex = headerIndex + 1;
        }
        return result;
    }

    static void AddUniqueToken(
        System.Collections.Generic.List<string> values, string token)
    {
        if (!ContainsToken(values, token)) values.Add(token);
    }

    static bool ContainsToken(
        System.Collections.Generic.List<string> values, string token)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (SipAscii.EqualsIgnoreCase(values[index], token)) return true;
            index = index + 1;
        }
        return false;
    }

    static void ReplaceTokenHeader(SipMessage message, string name,
        System.Collections.Generic.List<string> values)
    {
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int insertion = -1;
        int index = 0;
        while (index < message.Headers.Length)
        {
            SipHeader header = message.Headers[index];
            if (SipAscii.EqualsIgnoreCase(header.Name, name))
            {
                if (insertion < 0) insertion = headers.Count;
            }
            else
            {
                headers.Add(header);
            }
            index = index + 1;
        }
        if (insertion < 0)
        {
            insertion = headers.Count;
            index = 0;
            while (index < headers.Count)
            {
                if (SipAscii.EqualsIgnoreCase(
                    headers[index].Name, SipHeaderNames.ContentLength))
                {
                    insertion = index;
                    index = headers.Count;
                }
                else index = index + 1;
            }
        }
        SipHeader added = new SipHeader();
        added.Name = name;
        string[] texts = values.ToArray();
        added.Value = JoinStrings(texts, ", ");
        headers.Insert(insertion, added);
        message.Headers = headers.ToArray();
        message.OriginalBytes = new byte[0];
    }

    static string[] CopyStrings(string[]? source)
    {
        if (source == null) return new string[0];
        string[] copy = new string[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static byte[] CopyBytes(byte[]? source)
    {
        if (source == null) return new byte[0];
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, copy, source.Length);
        return copy;
    }

    static SipIceResult Success()
    {
        SipIceResult result = new SipIceResult();
        result.Success = true;
        result.StatusCode = SipStatusCodes.Ok;
        return result;
    }

    static SipIceResult Failure(string message)
    {
        SipIceResult result = new SipIceResult();
        result.StatusCode = SipStatusCodes.BadRequest;
        result.ErrorMessage = message;
        return result;
    }

    static SipIceResult Conflict(string message)
    {
        SipIceResult result = new SipIceResult();
        result.StatusCode = SipStatusCodes.Conflict;
        result.ErrorMessage = message;
        return result;
    }
}

internal class SipIceParseState
{
    internal string? ErrorMessage;
}
