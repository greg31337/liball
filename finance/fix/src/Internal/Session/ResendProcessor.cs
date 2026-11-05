namespace fix;

internal class ResendProcessor
{
    internal bool ClosedResendInterval;
    internal bool SendRedundantResendRequests;
    internal int ResendRequestChunkSize;

    internal ResendProcessor(SessionSettings settings)
    {
        if (settings == null)
        {
            throw new FixSessionException("The resend settings are missing.");
        }
        if (settings.ResendRequestChunkSize < 0)
        {
            throw new FixSessionException("The ResendRequest chunk size cannot be negative.");
        }
        ClosedResendInterval = settings.ClosedResendInterval;
        SendRedundantResendRequests = settings.SendRedundantResendRequests;
        ResendRequestChunkSize = settings.ResendRequestChunkSize;
    }

    internal ResendRequestActionResult CreateResendRequest(int beginSeqNo, int gapEndSeqNo, bool redundant)
    {
        if (beginSeqNo < FixDefaults.InitialSequenceNumber || gapEndSeqNo < beginSeqNo)
        {
            return CreateFailure("invalid-resend-request-range", "The ResendRequest range is invalid.");
        }
        int endSeqNo = 0;
        if (ResendRequestChunkSize > 0)
        {
            long chunkEnd = (long)beginSeqNo + ResendRequestChunkSize - 1L;
            endSeqNo = chunkEnd < gapEndSeqNo ? (int)chunkEnd : gapEndSeqNo;
        }
        else if (ClosedResendInterval)
        {
            endSeqNo = gapEndSeqNo;
        }
        ResendRequestActionResult result = new ResendRequestActionResult();
        result.Accepted = true;
        result.GenerateResendRequest = true;
        result.Redundant = redundant;
        result.BeginSeqNo = beginSeqNo;
        result.EndSeqNo = endSeqNo;
        result.Code = redundant ? "redundant-resend-request-required" : "resend-request-required";
        result.Text = redundant ? "The active gap requires a redundant ResendRequest." : "The sequence gap requires a ResendRequest.";
        return result;
    }

    internal ResendRequestActionResult CreateFailure(string code, string text)
    {
        ResendRequestActionResult result = new ResendRequestActionResult();
        result.Code = code;
        result.Text = text;
        return result;
    }
}
