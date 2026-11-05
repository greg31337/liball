namespace fix;

internal class GapTracker
{
    internal ResendProcessor ResendProcessor;
    internal bool GapActive;
    internal int GapBeginSeqNo;
    internal int GapEndSeqNo;
    internal int HighestReceivedSeqNo;
    internal bool RequestOutstanding;
    internal int OutstandingBeginSeqNo;
    internal int OutstandingEndSeqNo;

    internal GapTracker(SessionSettings settings)
    {
        ResendProcessor = new ResendProcessor(settings);
    }

    internal ResendRequestActionResult ObserveGap(int expectedSeqNo, int receivedSeqNo)
    {
        lock (this)
        {
            if (expectedSeqNo < FixDefaults.InitialSequenceNumber || receivedSeqNo <= expectedSeqNo)
            {
                return CompleteResult(ResendProcessor.CreateFailure("invalid-sequence-gap", "A sequence gap requires a positive expected number and a higher received number."));
            }
            if (!GapActive)
            {
                GapActive = true;
                GapBeginSeqNo = expectedSeqNo;
                HighestReceivedSeqNo = receivedSeqNo;
                GapEndSeqNo = receivedSeqNo - 1;
                return GenerateRequest(expectedSeqNo, false);
            }
            if (expectedSeqNo < GapBeginSeqNo)
            {
                return CompleteResult(ResendProcessor.CreateFailure("stale-gap-expectation", "The expected sequence number moved behind the active gap."));
            }
            if (expectedSeqNo > GapBeginSeqNo)
            {
                GapBeginSeqNo = expectedSeqNo;
            }
            if (receivedSeqNo > HighestReceivedSeqNo)
            {
                HighestReceivedSeqNo = receivedSeqNo;
                GapEndSeqNo = receivedSeqNo - 1;
            }
            if (GapBeginSeqNo > GapEndSeqNo)
            {
                ClearGap();
                return CompleteResult(CreateSuccess("sequence-gap-complete", "The expected sequence number passed the tracked gap."));
            }
            if (RequestOutstanding && OutstandingEndSeqNo != 0 && GapBeginSeqNo > OutstandingEndSeqNo)
            {
                RequestOutstanding = false;
            }
            if (RequestOutstanding && !ResendProcessor.SendRedundantResendRequests)
            {
                return CompleteResult(CreateSuccess("resend-request-outstanding", "A ResendRequest is already outstanding for the active gap."));
            }
            return GenerateRequest(GapBeginSeqNo, RequestOutstanding);
        }
    }

    internal ResendRequestActionResult AdvanceGap(int nextExpectedSeqNo)
    {
        lock (this)
        {
            if (!GapActive)
            {
                return CompleteResult(ResendProcessor.CreateFailure("no-sequence-gap", "No sequence gap is active."));
            }
            if (nextExpectedSeqNo < GapBeginSeqNo)
            {
                return CompleteResult(ResendProcessor.CreateFailure("stale-gap-progress", "Gap progress cannot move the expected sequence number backward."));
            }
            if (nextExpectedSeqNo == GapBeginSeqNo)
            {
                return CompleteResult(CreateSuccess("sequence-gap-pending", "The active sequence gap has not advanced."));
            }
            GapBeginSeqNo = nextExpectedSeqNo;
            if (GapBeginSeqNo > GapEndSeqNo)
            {
                ClearGap();
                return CompleteResult(CreateSuccess("sequence-gap-complete", "The tracked sequence gap is complete."));
            }
            if (OutstandingEndSeqNo == 0)
            {
                return CompleteResult(CreateSuccess("open-resend-request-outstanding", "The open-ended ResendRequest remains outstanding."));
            }
            if (GapBeginSeqNo <= OutstandingEndSeqNo)
            {
                return CompleteResult(CreateSuccess("sequence-gap-chunk-pending", "The current ResendRequest range has not been completed."));
            }
            RequestOutstanding = false;
            return GenerateRequest(GapBeginSeqNo, false);
        }
    }

    internal ResendRequestActionResult ResetGapTracking()
    {
        lock (this)
        {
            ClearGap();
            return CompleteResult(CreateSuccess("sequence-gap-reset", "Sequence gap tracking was reset."));
        }
    }

    internal ResendRequestActionResult GenerateRequest(int beginSeqNo, bool redundant)
    {
        ResendRequestActionResult result = ResendProcessor.CreateResendRequest(beginSeqNo, GapEndSeqNo, redundant);
        if (result.GenerateResendRequest)
        {
            RequestOutstanding = true;
            OutstandingBeginSeqNo = result.BeginSeqNo;
            OutstandingEndSeqNo = result.EndSeqNo;
        }
        return CompleteResult(result);
    }

    internal void ClearGap()
    {
        GapActive = false;
        GapBeginSeqNo = 0;
        GapEndSeqNo = 0;
        HighestReceivedSeqNo = 0;
        RequestOutstanding = false;
        OutstandingBeginSeqNo = 0;
        OutstandingEndSeqNo = 0;
    }

    internal ResendRequestActionResult CompleteResult(ResendRequestActionResult result)
    {
        result.GapActive = GapActive;
        result.GapBeginSeqNo = GapBeginSeqNo;
        result.GapEndSeqNo = GapEndSeqNo;
        result.HighestReceivedSeqNo = HighestReceivedSeqNo;
        return result;
    }

    internal ResendRequestActionResult CreateSuccess(string code, string text)
    {
        ResendRequestActionResult result = new ResendRequestActionResult();
        result.Accepted = true;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
