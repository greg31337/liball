namespace asn1;

internal class EcnDecodeMachine
{
    internal EcnExecutablePlan Plan;
    internal EcnClassCategory Category;
    internal EcnEncodingProperty[] Properties;
    internal EcnPlanExecutionBinding[] Bindings = [];
    internal EcnPlanExecutor Executor;
    internal PerBitReader Input;
    internal bool Completed;
    internal EcnPlanExecutionResult Completion = new EcnPlanExecutionResult();

    internal EcnDecodeMachine(
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
        this.Input = new PerBitReader(limits);
    }

    internal void ConfigureBindings(EcnPlanExecutionBinding[] bindings)
    {
        this.Bindings = bindings == null
            ? throw new System.ArgumentNullException("bindings")
            : bindings;
    }

    internal EcnPlanExecutionResult SupplyInputBits(
        byte[] data,
        int offset,
        int bitCount,
        bool finalBlock)
    {
        if (this.Completed)
        {
            EcnPlanExecutionResult completed =
                this.Executor.CreateExecutionError(
                "ASNECN0209",
                "No ECN input can follow a completed value.",
                false,
                this.Plan.Root);
            completed.HasBitOffset = true;
            completed.BitOffset = this.Input.BufferedBitCount;
            return completed;
        }
        PerBitReadResult appended = this.Input.AppendInputBits(
            data,
            offset,
            bitCount,
            finalBlock);
        if (appended.Status == AsnDecodeStatus.Error)
        {
            EcnPlanExecutionResult failure =
                this.Executor.CreateExecutionError(
                appended.ErrorCode,
                appended.ErrorMessage,
                appended.LimitExceeded,
                this.Plan.Root);
            failure.HasBitOffset = true;
            failure.BitOffset = this.Input.BufferedBitCount;
            return failure;
        }
        return this.DecodeAvailable();
    }

    internal EcnPlanExecutionResult DecodeAvailable()
    {
        if (this.Completed)
        {
            return this.Completion;
        }
        EcnPlanExecutionResult result = this.Executor.DecodePlan(
            new EcnPlanExecutionRequest
            {
                Plan = this.Plan,
                Category = this.Category,
                Properties = this.Properties,
                Bindings = this.Bindings,
                Data = this.Input.Buffer,
                BitCount = this.Input.BufferedBitCount,
                FinalBlock = this.Input.FinalBlock
            });
        if (result.Status == AsnDecodeStatus.Done
            || result.Status == AsnDecodeStatus.Error)
        {
            this.Completed = true;
            this.Completion = result;
        }
        return result;
    }

    internal EcnPlanExecutionResult CompleteDecoding()
    {
        this.Input.FinalBlock = true;
        return this.DecodeAvailable();
    }

    internal void ResetDecoding()
    {
        this.Input.ResetReader();
        this.Completed = false;
        this.Completion = new EcnPlanExecutionResult();
        this.Bindings = [];
    }
}
