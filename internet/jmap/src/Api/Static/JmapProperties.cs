namespace jmap;

/// <summary>
/// Defines JSON member names and dynamic Email header-property fragments used by JMAP Core and JMAP Mail.
/// </summary>
public static class JmapProperties
{
    /// <summary>The <c>@type</c> protocol token.</summary>
    public const string AtType = "@type";

    /// <summary>The <c>capabilities</c> protocol token.</summary>
    public const string Capabilities = "capabilities";

    /// <summary>The <c>accounts</c> protocol token.</summary>
    public const string Accounts = "accounts";

    /// <summary>The <c>primaryAccounts</c> protocol token.</summary>
    public const string PrimaryAccounts = "primaryAccounts";

    /// <summary>The <c>username</c> protocol token.</summary>
    public const string Username = "username";

    /// <summary>The <c>apiUrl</c> protocol token.</summary>
    public const string ApiUrl = "apiUrl";

    /// <summary>The <c>downloadUrl</c> protocol token.</summary>
    public const string DownloadUrl = "downloadUrl";

    /// <summary>The <c>uploadUrl</c> protocol token.</summary>
    public const string UploadUrl = "uploadUrl";

    /// <summary>The <c>eventSourceUrl</c> protocol token.</summary>
    public const string EventSourceUrl = "eventSourceUrl";

    /// <summary>The <c>state</c> protocol token.</summary>
    public const string State = "state";

    /// <summary>The <c>maxSizeUpload</c> protocol token.</summary>
    public const string MaxSizeUpload = "maxSizeUpload";

    /// <summary>The <c>maxConcurrentUpload</c> protocol token.</summary>
    public const string MaxConcurrentUpload = "maxConcurrentUpload";

    /// <summary>The <c>maxSizeRequest</c> protocol token.</summary>
    public const string MaxSizeRequest = "maxSizeRequest";

    /// <summary>The <c>maxConcurrentRequests</c> protocol token.</summary>
    public const string MaxConcurrentRequests = "maxConcurrentRequests";

    /// <summary>The <c>maxCallsInRequest</c> protocol token.</summary>
    public const string MaxCallsInRequest = "maxCallsInRequest";

    /// <summary>The <c>maxObjectsInGet</c> protocol token.</summary>
    public const string MaxObjectsInGet = "maxObjectsInGet";

    /// <summary>The <c>maxObjectsInSet</c> protocol token.</summary>
    public const string MaxObjectsInSet = "maxObjectsInSet";

    /// <summary>The <c>collationAlgorithms</c> protocol token.</summary>
    public const string CollationAlgorithms = "collationAlgorithms";

    /// <summary>The <c>name</c> protocol token.</summary>
    public const string Name = "name";

    /// <summary>The <c>isPersonal</c> protocol token.</summary>
    public const string IsPersonal = "isPersonal";

    /// <summary>The <c>isReadOnly</c> protocol token.</summary>
    public const string IsReadOnly = "isReadOnly";

    /// <summary>The <c>accountCapabilities</c> protocol token.</summary>
    public const string AccountCapabilities = "accountCapabilities";

    /// <summary>The <c>maxMailboxesPerEmail</c> protocol token.</summary>
    public const string MaxMailboxesPerEmail = "maxMailboxesPerEmail";

    /// <summary>The <c>maxMailboxDepth</c> protocol token.</summary>
    public const string MaxMailboxDepth = "maxMailboxDepth";

    /// <summary>The <c>maxSizeMailboxName</c> protocol token.</summary>
    public const string MaxSizeMailboxName = "maxSizeMailboxName";

    /// <summary>The <c>maxSizeAttachmentsPerEmail</c> protocol token.</summary>
    public const string MaxSizeAttachmentsPerEmail = "maxSizeAttachmentsPerEmail";

    /// <summary>The <c>emailQuerySortOptions</c> protocol token.</summary>
    public const string EmailQuerySortOptions = "emailQuerySortOptions";

    /// <summary>The <c>mayCreateTopLevelMailbox</c> protocol token.</summary>
    public const string MayCreateTopLevelMailbox = "mayCreateTopLevelMailbox";

    /// <summary>The <c>maxDelayedSend</c> protocol token.</summary>
    public const string MaxDelayedSend = "maxDelayedSend";

    /// <summary>The <c>submissionExtensions</c> protocol token.</summary>
    public const string SubmissionExtensions = "submissionExtensions";

    /// <summary>The <c>using</c> protocol token.</summary>
    public const string Using = "using";

    /// <summary>The <c>methodCalls</c> protocol token.</summary>
    public const string MethodCalls = "methodCalls";

    /// <summary>The <c>createdIds</c> protocol token.</summary>
    public const string CreatedIds = "createdIds";

    /// <summary>The <c>methodResponses</c> protocol token.</summary>
    public const string MethodResponses = "methodResponses";

    /// <summary>The <c>sessionState</c> protocol token.</summary>
    public const string SessionState = "sessionState";

    /// <summary>The <c>resultOf</c> protocol token.</summary>
    public const string ResultOf = "resultOf";

    /// <summary>The <c>path</c> protocol token.</summary>
    public const string Path = "path";

    /// <summary>The <c>type</c> protocol token.</summary>
    public const string Type = "type";

    /// <summary>The <c>title</c> protocol token.</summary>
    public const string Title = "title";

    /// <summary>The <c>status</c> protocol token.</summary>
    public const string Status = "status";

    /// <summary>The <c>detail</c> protocol token.</summary>
    public const string Detail = "detail";

    /// <summary>The <c>instance</c> protocol token.</summary>
    public const string Instance = "instance";

    /// <summary>The <c>limit</c> protocol token.</summary>
    public const string Limit = "limit";

    /// <summary>The <c>description</c> protocol token.</summary>
    public const string Description = "description";

    /// <summary>The <c>properties</c> protocol token.</summary>
    public const string Properties = "properties";

    /// <summary>The <c>existingId</c> protocol token.</summary>
    public const string ExistingId = "existingId";

    /// <summary>The <c>maxRecipients</c> protocol token.</summary>
    public const string MaxRecipients = "maxRecipients";

    /// <summary>The <c>invalidRecipients</c> protocol token.</summary>
    public const string InvalidRecipients = "invalidRecipients";

    /// <summary>The <c>accountId</c> protocol token.</summary>
    public const string AccountId = "accountId";

    /// <summary>The <c>id</c> protocol token.</summary>
    public const string Id = "id";

    /// <summary>The <c>ids</c> protocol token.</summary>
    public const string Ids = "ids";

    /// <summary>The <c>list</c> protocol token.</summary>
    public const string List = "list";

    /// <summary>The <c>notFound</c> protocol token.</summary>
    public const string NotFound = "notFound";

    /// <summary>The <c>sinceState</c> protocol token.</summary>
    public const string SinceState = "sinceState";

    /// <summary>The <c>maxChanges</c> protocol token.</summary>
    public const string MaxChanges = "maxChanges";

    /// <summary>The <c>oldState</c> protocol token.</summary>
    public const string OldState = "oldState";

    /// <summary>The <c>newState</c> protocol token.</summary>
    public const string NewState = "newState";

    /// <summary>The <c>hasMoreChanges</c> protocol token.</summary>
    public const string HasMoreChanges = "hasMoreChanges";

    /// <summary>The <c>created</c> protocol token.</summary>
    public const string Created = "created";

    /// <summary>The <c>updated</c> protocol token.</summary>
    public const string Updated = "updated";

    /// <summary>The <c>destroyed</c> protocol token.</summary>
    public const string Destroyed = "destroyed";

    /// <summary>The <c>updatedProperties</c> protocol token.</summary>
    public const string UpdatedProperties = "updatedProperties";

    /// <summary>The <c>ifInState</c> protocol token.</summary>
    public const string IfInState = "ifInState";

    /// <summary>The <c>create</c> protocol token.</summary>
    public const string Create = "create";

    /// <summary>The <c>update</c> protocol token.</summary>
    public const string Update = "update";

    /// <summary>The <c>destroy</c> protocol token.</summary>
    public const string Destroy = "destroy";

    /// <summary>The <c>notCreated</c> protocol token.</summary>
    public const string NotCreated = "notCreated";

    /// <summary>The <c>notUpdated</c> protocol token.</summary>
    public const string NotUpdated = "notUpdated";

    /// <summary>The <c>notDestroyed</c> protocol token.</summary>
    public const string NotDestroyed = "notDestroyed";

    /// <summary>The <c>fromAccountId</c> protocol token.</summary>
    public const string FromAccountId = "fromAccountId";

    /// <summary>The <c>ifFromInState</c> protocol token.</summary>
    public const string IfFromInState = "ifFromInState";

    /// <summary>The <c>onSuccessDestroyOriginal</c> protocol token.</summary>
    public const string OnSuccessDestroyOriginal = "onSuccessDestroyOriginal";

    /// <summary>The <c>destroyFromIfInState</c> protocol token.</summary>
    public const string DestroyFromIfInState = "destroyFromIfInState";

    /// <summary>The <c>filter</c> protocol token.</summary>
    public const string Filter = "filter";

    /// <summary>The <c>operator</c> protocol token.</summary>
    public const string Operator = "operator";

    /// <summary>The <c>conditions</c> protocol token.</summary>
    public const string Conditions = "conditions";

    /// <summary>The <c>sort</c> protocol token.</summary>
    public const string Sort = "sort";

    /// <summary>The <c>property</c> protocol token.</summary>
    public const string Property = "property";

    /// <summary>The <c>isAscending</c> protocol token.</summary>
    public const string IsAscending = "isAscending";

    /// <summary>The <c>collation</c> protocol token.</summary>
    public const string Collation = "collation";

    /// <summary>The <c>position</c> protocol token.</summary>
    public const string Position = "position";

    /// <summary>The <c>anchor</c> protocol token.</summary>
    public const string Anchor = "anchor";

    /// <summary>The <c>anchorOffset</c> protocol token.</summary>
    public const string AnchorOffset = "anchorOffset";

    /// <summary>The <c>calculateTotal</c> protocol token.</summary>
    public const string CalculateTotal = "calculateTotal";

    /// <summary>The <c>queryState</c> protocol token.</summary>
    public const string QueryState = "queryState";

    /// <summary>The <c>canCalculateChanges</c> protocol token.</summary>
    public const string CanCalculateChanges = "canCalculateChanges";

    /// <summary>The <c>total</c> protocol token.</summary>
    public const string Total = "total";

    /// <summary>The <c>sinceQueryState</c> protocol token.</summary>
    public const string SinceQueryState = "sinceQueryState";

    /// <summary>The <c>upToId</c> protocol token.</summary>
    public const string UpToId = "upToId";

    /// <summary>The <c>oldQueryState</c> protocol token.</summary>
    public const string OldQueryState = "oldQueryState";

    /// <summary>The <c>newQueryState</c> protocol token.</summary>
    public const string NewQueryState = "newQueryState";

    /// <summary>The <c>removed</c> protocol token.</summary>
    public const string Removed = "removed";

    /// <summary>The <c>added</c> protocol token.</summary>
    public const string Added = "added";

    /// <summary>The <c>index</c> protocol token.</summary>
    public const string Index = "index";

    /// <summary>The <c>blobId</c> protocol token.</summary>
    public const string BlobId = "blobId";

    /// <summary>The <c>blobIds</c> protocol token.</summary>
    public const string BlobIds = "blobIds";

    /// <summary>The <c>size</c> protocol token.</summary>
    public const string Size = "size";

    /// <summary>The <c>copied</c> protocol token.</summary>
    public const string Copied = "copied";

    /// <summary>The <c>notCopied</c> protocol token.</summary>
    public const string NotCopied = "notCopied";

    /// <summary>The <c>changed</c> protocol token.</summary>
    public const string Changed = "changed";

    /// <summary>The <c>deviceClientId</c> protocol token.</summary>
    public const string DeviceClientId = "deviceClientId";

    /// <summary>The <c>url</c> protocol token.</summary>
    public const string Url = "url";

    /// <summary>The <c>keys</c> protocol token.</summary>
    public const string Keys = "keys";

    /// <summary>The <c>p256dh</c> protocol token.</summary>
    public const string P256Dh = "p256dh";

    /// <summary>The <c>auth</c> protocol token.</summary>
    public const string Auth = "auth";

    /// <summary>The <c>verificationCode</c> protocol token.</summary>
    public const string VerificationCode = "verificationCode";

    /// <summary>The <c>expires</c> protocol token.</summary>
    public const string Expires = "expires";

    /// <summary>The <c>types</c> protocol token.</summary>
    public const string Types = "types";

    /// <summary>The <c>pushSubscriptionId</c> protocol token.</summary>
    public const string PushSubscriptionId = "pushSubscriptionId";

    /// <summary>The <c>closeafter</c> protocol token.</summary>
    public const string CloseAfter = "closeafter";

    /// <summary>The <c>ping</c> protocol token.</summary>
    public const string Ping = "ping";

    /// <summary>The <c>interval</c> protocol token.</summary>
    public const string Interval = "interval";

    /// <summary>The <c>parentId</c> protocol token.</summary>
    public const string ParentId = "parentId";

    /// <summary>The <c>role</c> protocol token.</summary>
    public const string Role = "role";

    /// <summary>The <c>sortOrder</c> protocol token.</summary>
    public const string SortOrder = "sortOrder";

    /// <summary>The <c>totalEmails</c> protocol token.</summary>
    public const string TotalEmails = "totalEmails";

    /// <summary>The <c>unreadEmails</c> protocol token.</summary>
    public const string UnreadEmails = "unreadEmails";

    /// <summary>The <c>totalThreads</c> protocol token.</summary>
    public const string TotalThreads = "totalThreads";

    /// <summary>The <c>unreadThreads</c> protocol token.</summary>
    public const string UnreadThreads = "unreadThreads";

    /// <summary>The <c>myRights</c> protocol token.</summary>
    public const string MyRights = "myRights";

    /// <summary>The <c>mayReadItems</c> protocol token.</summary>
    public const string MayReadItems = "mayReadItems";

    /// <summary>The <c>mayAddItems</c> protocol token.</summary>
    public const string MayAddItems = "mayAddItems";

    /// <summary>The <c>mayRemoveItems</c> protocol token.</summary>
    public const string MayRemoveItems = "mayRemoveItems";

    /// <summary>The <c>maySetSeen</c> protocol token.</summary>
    public const string MaySetSeen = "maySetSeen";

    /// <summary>The <c>maySetKeywords</c> protocol token.</summary>
    public const string MaySetKeywords = "maySetKeywords";

    /// <summary>The <c>mayCreateChild</c> protocol token.</summary>
    public const string MayCreateChild = "mayCreateChild";

    /// <summary>The <c>mayRename</c> protocol token.</summary>
    public const string MayRename = "mayRename";

    /// <summary>The <c>mayDelete</c> protocol token.</summary>
    public const string MayDelete = "mayDelete";

    /// <summary>The <c>maySubmit</c> protocol token.</summary>
    public const string MaySubmit = "maySubmit";

    /// <summary>The <c>isSubscribed</c> protocol token.</summary>
    public const string IsSubscribed = "isSubscribed";

    /// <summary>The <c>sortAsTree</c> protocol token.</summary>
    public const string SortAsTree = "sortAsTree";

    /// <summary>The <c>filterAsTree</c> protocol token.</summary>
    public const string FilterAsTree = "filterAsTree";

    /// <summary>The <c>hasAnyRole</c> protocol token.</summary>
    public const string HasAnyRole = "hasAnyRole";

    /// <summary>The <c>onDestroyRemoveEmails</c> protocol token.</summary>
    public const string OnDestroyRemoveEmails = "onDestroyRemoveEmails";

    /// <summary>The <c>emailIds</c> protocol token.</summary>
    public const string EmailIds = "emailIds";

    /// <summary>The <c>threadId</c> protocol token.</summary>
    public const string ThreadId = "threadId";

    /// <summary>The <c>mailboxIds</c> protocol token.</summary>
    public const string MailboxIds = "mailboxIds";

    /// <summary>The <c>keywords</c> protocol token.</summary>
    public const string Keywords = "keywords";

    /// <summary>The <c>receivedAt</c> protocol token.</summary>
    public const string ReceivedAt = "receivedAt";

    /// <summary>The <c>hasAttachment</c> protocol token.</summary>
    public const string HasAttachment = "hasAttachment";

    /// <summary>The <c>email</c> protocol token.</summary>
    public const string Email = "email";

    /// <summary>The <c>addresses</c> protocol token.</summary>
    public const string Addresses = "addresses";

    /// <summary>The <c>headers</c> protocol token.</summary>
    public const string Headers = "headers";

    /// <summary>The <c>value</c> protocol token.</summary>
    public const string Value = "value";

    /// <summary>The <c>messageId</c> protocol token.</summary>
    public const string MessageId = "messageId";

    /// <summary>The <c>inReplyTo</c> protocol token.</summary>
    public const string InReplyTo = "inReplyTo";

    /// <summary>The <c>references</c> protocol token.</summary>
    public const string References = "references";

    /// <summary>The <c>sender</c> protocol token.</summary>
    public const string Sender = "sender";

    /// <summary>The <c>from</c> protocol token.</summary>
    public const string From = "from";

    /// <summary>The <c>to</c> protocol token.</summary>
    public const string To = "to";

    /// <summary>The <c>cc</c> protocol token.</summary>
    public const string Cc = "cc";

    /// <summary>The <c>bcc</c> protocol token.</summary>
    public const string Bcc = "bcc";

    /// <summary>The <c>replyTo</c> protocol token.</summary>
    public const string ReplyTo = "replyTo";

    /// <summary>The <c>subject</c> protocol token.</summary>
    public const string Subject = "subject";

    /// <summary>The <c>sentAt</c> protocol token.</summary>
    public const string SentAt = "sentAt";

    /// <summary>The <c>partId</c> protocol token.</summary>
    public const string PartId = "partId";

    /// <summary>The <c>charset</c> protocol token.</summary>
    public const string Charset = "charset";

    /// <summary>The <c>disposition</c> protocol token.</summary>
    public const string Disposition = "disposition";

    /// <summary>The <c>cid</c> protocol token.</summary>
    public const string Cid = "cid";

    /// <summary>The <c>language</c> protocol token.</summary>
    public const string Language = "language";

    /// <summary>The <c>location</c> protocol token.</summary>
    public const string Location = "location";

    /// <summary>The <c>subParts</c> protocol token.</summary>
    public const string SubParts = "subParts";

    /// <summary>The <c>bodyStructure</c> protocol token.</summary>
    public const string BodyStructure = "bodyStructure";

    /// <summary>The <c>bodyValues</c> protocol token.</summary>
    public const string BodyValues = "bodyValues";

    /// <summary>The <c>isEncodingProblem</c> protocol token.</summary>
    public const string IsEncodingProblem = "isEncodingProblem";

    /// <summary>The <c>isTruncated</c> protocol token.</summary>
    public const string IsTruncated = "isTruncated";

    /// <summary>The <c>textBody</c> protocol token.</summary>
    public const string TextBody = "textBody";

    /// <summary>The <c>htmlBody</c> protocol token.</summary>
    public const string HtmlBody = "htmlBody";

    /// <summary>The <c>attachments</c> protocol token.</summary>
    public const string Attachments = "attachments";

    /// <summary>The <c>preview</c> protocol token.</summary>
    public const string Preview = "preview";

    /// <summary>The <c>bodyProperties</c> protocol token.</summary>
    public const string BodyProperties = "bodyProperties";

    /// <summary>The <c>fetchTextBodyValues</c> protocol token.</summary>
    public const string FetchTextBodyValues = "fetchTextBodyValues";

    /// <summary>The <c>fetchHTMLBodyValues</c> protocol token.</summary>
    public const string FetchHtmlBodyValues = "fetchHTMLBodyValues";

    /// <summary>The <c>fetchAllBodyValues</c> protocol token.</summary>
    public const string FetchAllBodyValues = "fetchAllBodyValues";

    /// <summary>The <c>maxBodyValueBytes</c> protocol token.</summary>
    public const string MaxBodyValueBytes = "maxBodyValueBytes";

    /// <summary>The <c>collapseThreads</c> protocol token.</summary>
    public const string CollapseThreads = "collapseThreads";

    /// <summary>The <c>inMailbox</c> protocol token.</summary>
    public const string InMailbox = "inMailbox";

    /// <summary>The <c>inMailboxOtherThan</c> protocol token.</summary>
    public const string InMailboxOtherThan = "inMailboxOtherThan";

    /// <summary>The <c>before</c> protocol token.</summary>
    public const string Before = "before";

    /// <summary>The <c>after</c> protocol token.</summary>
    public const string After = "after";

    /// <summary>The <c>minSize</c> protocol token.</summary>
    public const string MinSize = "minSize";

    /// <summary>The <c>maxSize</c> protocol token.</summary>
    public const string MaxSize = "maxSize";

    /// <summary>The <c>allInThreadHaveKeyword</c> protocol token.</summary>
    public const string AllInThreadHaveKeyword = "allInThreadHaveKeyword";

    /// <summary>The <c>someInThreadHaveKeyword</c> protocol token.</summary>
    public const string SomeInThreadHaveKeyword = "someInThreadHaveKeyword";

    /// <summary>The <c>noneInThreadHaveKeyword</c> protocol token.</summary>
    public const string NoneInThreadHaveKeyword = "noneInThreadHaveKeyword";

    /// <summary>The <c>hasKeyword</c> protocol token.</summary>
    public const string HasKeyword = "hasKeyword";

    /// <summary>The <c>notKeyword</c> protocol token.</summary>
    public const string NotKeyword = "notKeyword";

    /// <summary>The <c>text</c> protocol token.</summary>
    public const string Text = "text";

    /// <summary>The <c>body</c> protocol token.</summary>
    public const string Body = "body";

    /// <summary>The <c>header</c> protocol token.</summary>
    public const string Header = "header";

    /// <summary>The <c>emails</c> protocol token.</summary>
    public const string Emails = "emails";

    /// <summary>The <c>parsed</c> protocol token.</summary>
    public const string Parsed = "parsed";

    /// <summary>The <c>notParsable</c> protocol token.</summary>
    public const string NotParsable = "notParsable";

    /// <summary>The <c>emailId</c> protocol token.</summary>
    public const string EmailId = "emailId";

    /// <summary>The <c>textSignature</c> protocol token.</summary>
    public const string TextSignature = "textSignature";

    /// <summary>The <c>htmlSignature</c> protocol token.</summary>
    public const string HtmlSignature = "htmlSignature";

    /// <summary>The <c>identityId</c> protocol token.</summary>
    public const string IdentityId = "identityId";

    /// <summary>The <c>envelope</c> protocol token.</summary>
    public const string Envelope = "envelope";

    /// <summary>The <c>mailFrom</c> protocol token.</summary>
    public const string MailFrom = "mailFrom";

    /// <summary>The <c>rcptTo</c> protocol token.</summary>
    public const string RcptTo = "rcptTo";

    /// <summary>The <c>parameters</c> protocol token.</summary>
    public const string Parameters = "parameters";

    /// <summary>The <c>sendAt</c> protocol token.</summary>
    public const string SendAt = "sendAt";

    /// <summary>The <c>undoStatus</c> protocol token.</summary>
    public const string UndoStatus = "undoStatus";

    /// <summary>The <c>deliveryStatus</c> protocol token.</summary>
    public const string DeliveryStatus = "deliveryStatus";

    /// <summary>The <c>smtpReply</c> protocol token.</summary>
    public const string SmtpReply = "smtpReply";

    /// <summary>The <c>delivered</c> protocol token.</summary>
    public const string Delivered = "delivered";

    /// <summary>The <c>displayed</c> protocol token.</summary>
    public const string Displayed = "displayed";

    /// <summary>The <c>dsnBlobIds</c> protocol token.</summary>
    public const string DsnBlobIds = "dsnBlobIds";

    /// <summary>The <c>mdnBlobIds</c> protocol token.</summary>
    public const string MdnBlobIds = "mdnBlobIds";

    /// <summary>The <c>identityIds</c> protocol token.</summary>
    public const string IdentityIds = "identityIds";

    /// <summary>The <c>threadIds</c> protocol token.</summary>
    public const string ThreadIds = "threadIds";

    /// <summary>The <c>onSuccessUpdateEmail</c> protocol token.</summary>
    public const string OnSuccessUpdateEmail = "onSuccessUpdateEmail";

    /// <summary>The <c>onSuccessDestroyEmail</c> protocol token.</summary>
    public const string OnSuccessDestroyEmail = "onSuccessDestroyEmail";

    /// <summary>The <c>isEnabled</c> protocol token.</summary>
    public const string IsEnabled = "isEnabled";

    /// <summary>The <c>fromDate</c> protocol token.</summary>
    public const string FromDate = "fromDate";

    /// <summary>The <c>toDate</c> protocol token.</summary>
    public const string ToDate = "toDate";

    /// <summary>The <c>header:</c> protocol token.</summary>
    public const string HeaderPrefix = "header:";

    /// <summary>The <c>:asRaw</c> protocol token.</summary>
    public const string HeaderAsRawSuffix = ":asRaw";

    /// <summary>The <c>:asText</c> protocol token.</summary>
    public const string HeaderAsTextSuffix = ":asText";

    /// <summary>The <c>:asAddresses</c> protocol token.</summary>
    public const string HeaderAsAddressesSuffix = ":asAddresses";

    /// <summary>The <c>:asGroupedAddresses</c> protocol token.</summary>
    public const string HeaderAsGroupedAddressesSuffix = ":asGroupedAddresses";

    /// <summary>The <c>:asMessageIds</c> protocol token.</summary>
    public const string HeaderAsMessageIdsSuffix = ":asMessageIds";

    /// <summary>The <c>:asDate</c> protocol token.</summary>
    public const string HeaderAsDateSuffix = ":asDate";

    /// <summary>The <c>:asURLs</c> protocol token.</summary>
    public const string HeaderAsUrlsSuffix = ":asURLs";

    /// <summary>The <c>:all</c> protocol token.</summary>
    public const string HeaderAllSuffix = ":all";
}

