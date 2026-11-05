using System.Collections.Generic;
using System.Threading;

namespace iso20022;

/// <summary>Controls pull-based reading of message sequences and file envelopes.</summary>
public class StreamReadOptions
{
    /// <summary>The options applied to each message.</summary>
    public MessageReadOptions MessageOptions = new MessageReadOptions();
    /// <summary>Whether reading continues after an invalid item.</summary>
    public bool ContinueAfterInvalidItem;
    /// <summary>The cancellation request observed between bounded streaming operations.</summary>
    public CancellationToken CancellationToken;
    /// <summary>The maximum number of items to return, or zero for the resource-limit default.</summary>
    public int MaxItems;
    /// <summary>The qualified names accepted as sequence wrappers.</summary>
    public List<QualifiedNameData> WrapperNames = new List<QualifiedNameData>();
}
