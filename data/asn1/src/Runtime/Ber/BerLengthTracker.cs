namespace asn1;

internal class BerLengthTracker
{
    internal ResourceCounter Resources;
    internal System.Collections.Generic.List<BerLengthFrame> Frames =
        new System.Collections.Generic.List<BerLengthFrame>();

    internal BerLengthTracker(AsnResourceLimits limits)
    {
        this.Resources = new ResourceCounter(limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits);
    }

    internal BerLengthAccountingResult EnterLength(
        long length,
        bool indefinite)
    {
        if (!indefinite && length < 0)
        {
            return this.CreateAccountingError(
                "ASND0006",
                "A definite scope cannot have a negative remaining length.");
        }

        if (!indefinite && !this.FitsDefiniteAncestors(length))
        {
            return this.CreateAccountingError(
                "ASND0007",
                "The nested content length exceeds an enclosing definite length.");
        }

        if (!this.Resources.TryEnterNesting())
        {
            return new BerLengthAccountingResult
            {
                LimitExceeded = true,
                ErrorCode = "ASNR0004",
                ErrorMessage = "The BER nesting depth exceeds the configured limit."
            };
        }

        this.Frames.Add(new BerLengthFrame
        {
            Indefinite = indefinite,
            Remaining = indefinite ? 0 : length
        });
        int completed = this.RemoveCompletedDefiniteScopes();
        return new BerLengthAccountingResult
        {
            Success = true,
            CompletedScopeCount = completed
        };
    }

    internal BerLengthAccountingResult ConsumeOctets(long count)
    {
        if (count < 0)
        {
            return this.CreateAccountingError(
                "ASND0008",
                "A consumed octet count cannot be negative.");
        }

        int index = 0;
        while (index < this.Frames.Count)
        {
            BerLengthFrame frame = this.Frames[index];
            if (!frame.Indefinite && count > frame.Remaining)
            {
                return this.CreateAccountingError(
                    "ASND0009",
                    "Consumed data exceeds an enclosing definite length.");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.Frames.Count)
        {
            BerLengthFrame frame = this.Frames[index];
            if (!frame.Indefinite)
            {
                frame.Remaining = frame.Remaining - count;
            }

            index = index + 1;
        }

        int completed = this.RemoveCompletedDefiniteScopes();
        return new BerLengthAccountingResult
        {
            Success = true,
            CompletedScopeCount = completed
        };
    }

    internal BerLengthAccountingResult ConsumeEndOfContents()
    {
        if (this.Frames.Count == 0
            || !this.Frames[this.Frames.Count - 1].Indefinite)
        {
            return this.CreateAccountingError(
                "ASND0010",
                "An end-of-contents marker requires an active indefinite scope.");
        }

        BerLengthAccountingResult consumed = this.ConsumeOctets(2);
        if (!consumed.Success)
        {
            return consumed;
        }

        this.Frames.RemoveAt(this.Frames.Count - 1);
        this.Resources.LeaveNesting();
        int completed = 1 + this.RemoveCompletedDefiniteScopes();
        return new BerLengthAccountingResult
        {
            Success = true,
            CompletedScopeCount = completed
        };
    }

    internal int GetDepth()
    {
        return this.Frames.Count;
    }

    internal long GetInnermostRemaining()
    {
        if (this.Frames.Count == 0)
        {
            return 0;
        }

        BerLengthFrame frame = this.Frames[this.Frames.Count - 1];
        return frame.Indefinite ? -1 : frame.Remaining;
    }

    internal bool FitsDefiniteAncestors(long length)
    {
        int index = 0;
        while (index < this.Frames.Count)
        {
            BerLengthFrame frame = this.Frames[index];
            if (!frame.Indefinite && length > frame.Remaining)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int RemoveCompletedDefiniteScopes()
    {
        int completed = 0;
        while (this.Frames.Count > 0)
        {
            BerLengthFrame frame = this.Frames[this.Frames.Count - 1];
            if (frame.Indefinite || frame.Remaining != 0)
            {
                break;
            }

            this.Frames.RemoveAt(this.Frames.Count - 1);
            this.Resources.LeaveNesting();
            completed = completed + 1;
        }

        return completed;
    }

    internal BerLengthAccountingResult CreateAccountingError(
        string code,
        string message)
    {
        return new BerLengthAccountingResult
        {
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
