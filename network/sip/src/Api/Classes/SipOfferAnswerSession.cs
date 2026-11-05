namespace sip;

/// <summary>Coordinates one opaque or SDP offer/answer exchange at a time.</summary>
public class SipOfferAnswerSession : ISipOfferAnswerSession
{
    object gate = new object();
    SipOfferAnswer current;
    SipOfferAnswer rollback;
    bool rollbackAvailable;

    /// <summary>Initializes a stable offer/answer session.</summary>
    public SipOfferAnswerSession()
    {
        current = new SipOfferAnswer();
        current.State = SipOfferAnswerState.Stable;
        rollback = new SipOfferAnswer();
        rollback.State = SipOfferAnswerState.Stable;
    }

    /// <summary>Gets the current detached offer/answer state.</summary>
    /// <returns>The current exchange.</returns>
    public SipOfferAnswer GetOfferAnswer()
    {
        lock (gate)
        {
            return CopyRequiredExchange(current);
        }
    }

    /// <summary>Applies a local offer.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The updated exchange.</returns>
    public SipOfferAnswer ApplyLocalOffer(SipOfferAnswer exchange)
    {
        lock (gate)
        {
            if (exchange == null)
            {
                throw new System.ArgumentNullException("exchange");
            }
            ValidateOfferCommand(exchange, exchange.LocalOffer, true, "exchange");
            if (current.State == SipOfferAnswerState.LocalOfferPending)
            {
                if (OfferMatchesCurrent(exchange.LocalOffer, exchange.ContentType,
                    current.LocalOffer))
                {
                    return CopyRequiredExchange(current);
                }
                throw new System.InvalidOperationException(
                    "A different local offer is already outstanding.");
            }
            if (current.State == SipOfferAnswerState.LocalOfferRequired)
            {
                if (exchange.LocalOffer.Length == 0)
                {
                    throw new System.ArgumentException(
                        "The offerless request now requires a local offer.", "exchange");
                }
                BeginLocalOffer(exchange);
                return CopyRequiredExchange(current);
            }
            if (HasOutstandingExchange())
            {
                throw new System.InvalidOperationException(
                    "Another offer/answer exchange is already outstanding.");
            }
            SaveRollbackState();
            if (exchange.LocalOffer.Length == 0)
            {
                BeginOfferlessExchange(SipOfferAnswerState.RemoteOfferRequired);
            }
            else
            {
                BeginLocalOffer(exchange);
            }
            return CopyRequiredExchange(current);
        }
    }

    /// <summary>Applies a remote offer.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The updated exchange.</returns>
    public SipOfferAnswer ApplyRemoteOffer(SipOfferAnswer exchange)
    {
        lock (gate)
        {
            if (exchange == null)
            {
                throw new System.ArgumentNullException("exchange");
            }
            ValidateOfferCommand(exchange, exchange.RemoteOffer, false, "exchange");
            if (current.State == SipOfferAnswerState.RemoteOfferPending)
            {
                if (OfferMatchesCurrent(exchange.RemoteOffer, exchange.ContentType,
                    current.RemoteOffer))
                {
                    return CopyRequiredExchange(current);
                }
                throw new System.InvalidOperationException(
                    "A different remote offer is already outstanding.");
            }
            if (current.State == SipOfferAnswerState.RemoteOfferRequired)
            {
                if (exchange.RemoteOffer.Length == 0)
                {
                    throw new System.ArgumentException(
                        "The offerless request now requires a remote offer.", "exchange");
                }
                BeginRemoteOffer(exchange);
                return CopyRequiredExchange(current);
            }
            if (HasOutstandingExchange())
            {
                throw new System.InvalidOperationException(
                    "Another offer/answer exchange is already outstanding.");
            }
            SaveRollbackState();
            if (exchange.RemoteOffer.Length == 0)
            {
                BeginOfferlessExchange(SipOfferAnswerState.LocalOfferRequired);
            }
            else
            {
                BeginRemoteOffer(exchange);
            }
            return CopyRequiredExchange(current);
        }
    }

    /// <summary>Applies an answer.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The updated exchange.</returns>
    public SipOfferAnswer ApplyOfferAnswer(SipOfferAnswer exchange)
    {
        lock (gate)
        {
            if (exchange == null)
            {
                throw new System.ArgumentNullException("exchange");
            }
            if (current.State == SipOfferAnswerState.Completed)
            {
                if (current.RemoteAnswer.Length > 0)
                {
                    ValidateAnswerCommand(exchange, exchange.RemoteAnswer,
                        true, "exchange");
                }
                else
                {
                    ValidateAnswerCommand(exchange, exchange.LocalAnswer,
                        false, "exchange");
                }
                if (CompletedAnswerMatches(exchange))
                {
                    return CopyRequiredExchange(current);
                }
                throw new System.InvalidOperationException(
                    "No offer is awaiting an answer.");
            }
            if (current.State == SipOfferAnswerState.LocalOfferPending)
            {
                CompleteRemoteAnswer(exchange);
                return CopyRequiredExchange(current);
            }
            if (current.State == SipOfferAnswerState.RemoteOfferPending)
            {
                CompleteLocalAnswer(exchange);
                return CopyRequiredExchange(current);
            }
            throw new System.InvalidOperationException(
                "No offer is awaiting an answer.");
        }
    }

    /// <summary>Rolls back an outstanding exchange.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The rolled-back exchange.</returns>
    public SipOfferAnswer RollbackOfferAnswer(SipOfferAnswer exchange)
    {
        lock (gate)
        {
            if (exchange == null)
            {
                throw new System.ArgumentNullException("exchange");
            }
            if (!HasOutstandingExchange())
            {
                throw new System.InvalidOperationException(
                    "No offer/answer exchange can be rolled back.");
            }
            ValidateOutstandingExchange(exchange);
            FailOutstandingExchange();
            return CopyRequiredExchange(current);
        }
    }

    /// <summary>Rejects an outstanding remote offer and restores negotiated state.</summary>
    /// <param name="exchange">The offer/answer exchange.</param>
    /// <returns>The rejected exchange.</returns>
    public SipOfferAnswer RejectOfferAnswer(SipOfferAnswer exchange)
    {
        lock (gate)
        {
            if (exchange == null)
            {
                throw new System.ArgumentNullException("exchange");
            }
            if (current.State != SipOfferAnswerState.RemoteOfferPending
                && current.State != SipOfferAnswerState.LocalOfferRequired)
            {
                throw new System.InvalidOperationException(
                    "No remote offer can be rejected.");
            }
            ValidateOutstandingExchange(exchange);
            FailOutstandingExchange();
            return CopyRequiredExchange(current);
        }
    }

    void BeginLocalOffer(SipOfferAnswer exchange)
    {
        SipOfferAnswer input = CopyRequiredExchange(exchange);
        SipOfferAnswer previous = CopyRequiredExchange(current);
        SipOfferAnswer next = new SipOfferAnswer();
        next.State = SipOfferAnswerState.LocalOfferPending;
        next.LocalOffer = input.LocalOffer;
        next.ContentType = input.ContentType;
        next.NegotiatedBody = previous.NegotiatedBody;
        next.NegotiatedContentType = previous.NegotiatedContentType;
        current = next;
    }

    void BeginRemoteOffer(SipOfferAnswer exchange)
    {
        SipOfferAnswer input = CopyRequiredExchange(exchange);
        SipOfferAnswer previous = CopyRequiredExchange(current);
        SipOfferAnswer next = new SipOfferAnswer();
        next.State = SipOfferAnswerState.RemoteOfferPending;
        next.RemoteOffer = input.RemoteOffer;
        next.ContentType = input.ContentType;
        next.NegotiatedBody = previous.NegotiatedBody;
        next.NegotiatedContentType = previous.NegotiatedContentType;
        current = next;
    }

    void BeginOfferlessExchange(SipOfferAnswerState state)
    {
        SipOfferAnswer previous = CopyRequiredExchange(current);
        SipOfferAnswer next = new SipOfferAnswer();
        next.State = state;
        next.NegotiatedBody = previous.NegotiatedBody;
        next.NegotiatedContentType = previous.NegotiatedContentType;
        current = next;
    }

    void CompleteRemoteAnswer(SipOfferAnswer exchange)
    {
        ValidateAnswerCommand(exchange, exchange.RemoteAnswer, true, "exchange");
        SipOfferAnswer input = CopyRequiredExchange(exchange);
        SipMediaType mediaType = ResolveAnswerMediaType(input.ContentType);
        ValidateAnswerBody(input.RemoteAnswer, "exchange");
        current.RemoteAnswer = input.RemoteAnswer;
        current.ContentType = mediaType;
        current.NegotiatedBody = CopyRequiredExchange(input).RemoteAnswer;
        current.NegotiatedContentType = CopyRequiredExchange(current).ContentType;
        current.State = SipOfferAnswerState.Completed;
        rollbackAvailable = false;
    }

    void CompleteLocalAnswer(SipOfferAnswer exchange)
    {
        ValidateAnswerCommand(exchange, exchange.LocalAnswer, false, "exchange");
        SipOfferAnswer input = CopyRequiredExchange(exchange);
        SipMediaType mediaType = ResolveAnswerMediaType(input.ContentType);
        ValidateAnswerBody(input.LocalAnswer, "exchange");
        current.LocalAnswer = input.LocalAnswer;
        current.ContentType = mediaType;
        current.NegotiatedBody = CopyRequiredExchange(input).LocalAnswer;
        current.NegotiatedContentType = CopyRequiredExchange(current).ContentType;
        current.State = SipOfferAnswerState.Completed;
        rollbackAvailable = false;
    }

    void SaveRollbackState()
    {
        rollback = CopyRequiredExchange(current);
        rollbackAvailable = true;
    }

    void FailOutstandingExchange()
    {
        if (!rollbackAvailable)
        {
            throw new System.InvalidOperationException(
                "The offer/answer rollback state is unavailable.");
        }
        SipOfferAnswer failed = CopyRequiredExchange(current);
        SipOfferAnswer restored = CopyRequiredExchange(rollback);
        failed.State = SipOfferAnswerState.Failed;
        failed.NegotiatedBody = restored.NegotiatedBody;
        failed.NegotiatedContentType = restored.NegotiatedContentType;
        current = failed;
        rollbackAvailable = false;
    }

    bool HasOutstandingExchange()
    {
        return current.State == SipOfferAnswerState.LocalOfferPending
            || current.State == SipOfferAnswerState.RemoteOfferPending
            || current.State == SipOfferAnswerState.LocalOfferRequired
            || current.State == SipOfferAnswerState.RemoteOfferRequired;
    }

    bool OfferMatchesCurrent(
        byte[] candidate, SipMediaType? contentType, byte[] existing)
    {
        return ByteArraysEqual(candidate, existing)
            && MediaTypesCompatible(contentType, current.ContentType);
    }

    bool CompletedAnswerMatches(SipOfferAnswer exchange)
    {
        bool bodyMatches = current.RemoteAnswer.Length > 0
            && exchange.RemoteAnswer != null
            && ByteArraysEqual(current.RemoteAnswer, exchange.RemoteAnswer)
            || current.LocalAnswer.Length > 0
                && exchange.LocalAnswer != null
                && ByteArraysEqual(current.LocalAnswer, exchange.LocalAnswer);
        return bodyMatches
            && (exchange.ContentType == null
                || MediaTypesCompatible(exchange.ContentType, current.ContentType));
    }

    bool ByteArraysEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    SipMediaType ResolveAnswerMediaType(SipMediaType? supplied)
    {
        SipMediaType? effective = supplied == null ? current.ContentType : supplied;
        ValidateMediaType(effective, "exchange");
        if (!MediaTypesCompatible(effective, current.ContentType))
        {
            throw new System.ArgumentException(
                "The answer media type must match the offer media type.", "exchange");
        }
        SipOfferAnswer holder = new SipOfferAnswer();
        holder.ContentType = effective;
        SipMediaType? copied = CopyRequiredExchange(holder).ContentType;
        if (copied == null)
        {
            throw new System.InvalidOperationException(
                "The answer media type cannot be copied.");
        }
        return copied;
    }

    bool MediaTypesCompatible(SipMediaType? first, SipMediaType? second)
    {
        return first != null && second != null
            && SipAscii.EqualsIgnoreCase(first.Type, second.Type)
            && SipAscii.EqualsIgnoreCase(first.Subtype, second.Subtype);
    }

    void ValidateOfferCommand(
        SipOfferAnswer? exchange, byte[]? body, bool local, string parameterName)
    {
        if (exchange == null || body == null || body.Length > SipDefaults.MaxBodyBytes
            || exchange.LocalOffer == null || exchange.RemoteOffer == null
            || exchange.LocalAnswer == null || exchange.RemoteAnswer == null
            || local && (exchange.RemoteOffer.Length > 0
                || exchange.LocalAnswer.Length > 0
                || exchange.RemoteAnswer.Length > 0)
            || !local && (exchange.LocalOffer.Length > 0
                || exchange.LocalAnswer.Length > 0
                || exchange.RemoteAnswer.Length > 0))
        {
            throw new System.ArgumentException(
                "A bounded offer/answer exchange is required.", parameterName);
        }
        if (body.Length == 0)
        {
            if (exchange.ContentType != null)
            {
                throw new System.ArgumentException(
                    "An offerless exchange cannot declare a media type.", parameterName);
            }
            return;
        }
        ValidateMediaType(exchange.ContentType, parameterName);
    }

    void ValidateAnswerCommand(
        SipOfferAnswer? exchange, byte[]? body, bool remote, string parameterName)
    {
        if (exchange == null || body == null
            || exchange.LocalOffer == null || exchange.RemoteOffer == null
            || exchange.LocalAnswer == null || exchange.RemoteAnswer == null
            || exchange.LocalOffer.Length > 0 || exchange.RemoteOffer.Length > 0
            || remote && exchange.LocalAnswer.Length > 0
            || !remote && exchange.RemoteAnswer.Length > 0)
        {
            throw new System.ArgumentException(
                "The answer is placed in an invalid exchange field.", parameterName);
        }
    }

    void ValidateOutstandingExchange(SipOfferAnswer exchange)
    {
        if (exchange.State != current.State
            || exchange.LocalOffer == null || exchange.RemoteOffer == null
            || exchange.LocalAnswer == null || exchange.RemoteAnswer == null
            || current.State == SipOfferAnswerState.LocalOfferPending
                && !OfferMatchesCurrent(exchange.LocalOffer,
                    exchange.ContentType, current.LocalOffer)
            || current.State == SipOfferAnswerState.RemoteOfferPending
                && !OfferMatchesCurrent(exchange.RemoteOffer,
                    exchange.ContentType, current.RemoteOffer))
        {
            throw new System.ArgumentException(
                "The supplied exchange does not match the outstanding exchange.",
                "exchange");
        }
    }

    void ValidateAnswerBody(byte[]? body, string parameterName)
    {
        if (body == null || body.Length == 0
            || body.Length > SipDefaults.MaxBodyBytes)
        {
            throw new System.ArgumentException(
                "A bounded non-empty answer is required.", parameterName);
        }
    }

    void ValidateMediaType(SipMediaType? mediaType, string parameterName)
    {
        if (mediaType == null || !SipAscii.IsToken(mediaType.Type)
            || !SipAscii.IsToken(mediaType.Subtype)
            || mediaType.Parameters == null)
        {
            throw new System.ArgumentException(
                "A valid body media type is required.", parameterName);
        }
        int index = 0;
        while (index < mediaType.Parameters.Length)
        {
            SipUriParameter? parameter = mediaType.Parameters[index];
            if (parameter == null || !SipAscii.IsToken(parameter.Name)
                || !parameter.HasValue || parameter.Value == null)
            {
                throw new System.ArgumentException(
                    "A valid body media type is required.", parameterName);
            }
            index = index + 1;
        }
    }

    SipOfferAnswer CopyRequiredExchange(SipOfferAnswer source)
    {
        SipOfferAnswer? copy = SipSnapshotCopy.CopyOfferAnswer(source);
        if (copy == null)
        {
            throw new System.InvalidOperationException(
                "The offer/answer state cannot be copied.");
        }
        return copy;
    }
}
