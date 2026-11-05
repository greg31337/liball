namespace asn1;

internal class EcnEncodeMachine
{
    internal EcnExecutablePlan Plan;
    internal EcnClassCategory Category;
    internal EcnEncodingProperty[] Properties;
    internal EcnPlanExecutor Executor;
    internal EcnBitField Output = new EcnBitField();
    internal int Position;
    internal bool Prepared;
    internal bool Failed;
    internal EcnPlanExecutionResult Failure = new EcnPlanExecutionResult();

    internal EcnEncodeMachine(
        EcnExecutablePlan plan,
        EcnClassCategory category,
        EcnEncodingProperty[] properties,
        AsnResourceLimits limits)
    {
        this.Plan = plan == null
            ? throw new System.ArgumentNullException("plan")
            : plan;
        this.Category = category;
        this.Properties = properties == null
            ? throw new System.ArgumentNullException("properties")
            : properties;
        this.Executor = new EcnPlanExecutor(limits);
    }

    internal EcnPlanExecutionResult BeginEncoding(
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }
        if (bindings == null)
        {
            throw new System.ArgumentNullException("bindings");
        }
        this.ResetEncoding();
        EcnPlanExecutionResult result = this.Executor.EncodePlan(
            new EcnPlanExecutionRequest
            {
                Plan = this.Plan,
                Category = this.Category,
                Properties = this.Properties,
                Value = value,
                Bindings = bindings
            });
        if (result.Status != AsnDecodeStatus.Done)
        {
            this.Failed = true;
            this.Failure = result;
            return result;
        }
        this.Output = new EcnBitFieldOperations().CreateFromBytes(
            result.Data,
            result.BitCount);
        this.Prepared = true;
        return new EcnPlanExecutionResult
        {
            Status = AsnDecodeStatus.Done,
            BitCount = result.BitCount
        };
    }

    internal EcnPlanExecutionResult TakeOutputBits(int maximumBitCount)
    {
        if (maximumBitCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumBitCount");
        }
        if (this.Failed)
        {
            return this.Failure;
        }
        if (!this.Prepared)
        {
            return this.Executor.CreateExecutionError(
                "ASNECN0208",
                "ECN encoding has not been started.",
                false,
                this.Plan.Root);
        }
        int remaining = this.Output.BitCount - this.Position;
        int count = remaining < maximumBitCount
            ? remaining
            : maximumBitCount;
        EcnBitField chunk = new EcnBitFieldOperations().SliceField(
            this.Output,
            this.Position,
            count);
        this.Position = this.Position + count;
        return new EcnPlanExecutionResult
        {
            Status = this.Position == this.Output.BitCount
                ? AsnDecodeStatus.Done
                : AsnDecodeStatus.NeedMoreData,
            Data = chunk.Data,
            BitCount = chunk.BitCount,
            ConsumedBitCount = this.Position,
            PendingStepId = this.Position == this.Output.BitCount
                || this.Plan.Root == null
                ? ""
                : this.Plan.Root.StableId,
            PendingPath = this.Position == this.Output.BitCount
                || this.Plan.Root == null
                ? ""
                : this.Plan.Root.Path
        };
    }

    internal void ResetEncoding()
    {
        this.Output = new EcnBitField();
        this.Position = 0;
        this.Prepared = false;
        this.Failed = false;
        this.Failure = new EcnPlanExecutionResult();
    }
}
