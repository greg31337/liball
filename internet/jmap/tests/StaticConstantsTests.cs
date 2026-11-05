using System.Reflection;

namespace jmap.Tests;

public class StaticConstantsTests
{
    [Test]
    public void CapabilityConstantsMatchRfcUris()
    {
        AssertStringConstants(
            typeof(JmapCapabilities),
            "urn:ietf:params:jmap:core,urn:ietf:params:jmap:mail,urn:ietf:params:jmap:submission,urn:ietf:params:jmap:vacationresponse");
    }

    [Test]
    public void MethodConstantsCoverCoreAndMailMethods()
    {
        AssertStringConstants(
            typeof(JmapMethods),
            "Core/echo,Blob/copy,PushSubscription/get,PushSubscription/set,Mailbox/get,Mailbox/changes,Mailbox/query,Mailbox/queryChanges,Mailbox/set,Thread/get,Thread/changes,Email/get,Email/changes,Email/query,Email/queryChanges,Email/set,Email/copy,Email/import,Email/parse,SearchSnippet/get,Identity/get,Identity/changes,Identity/set,EmailSubmission/get,EmailSubmission/changes,EmailSubmission/query,EmailSubmission/queryChanges,EmailSubmission/set,VacationResponse/get,VacationResponse/set");
    }

    [Test]
    public void ErrorConstantsCoverCoreAndMailErrors()
    {
        AssertStringConstants(
            typeof(JmapErrorCodes),
            "urn:ietf:params:jmap:error:,unknownCapability,notJSON,notRequest,limit,urn:ietf:params:jmap:error:unknownCapability,urn:ietf:params:jmap:error:notJSON,urn:ietf:params:jmap:error:notRequest,urn:ietf:params:jmap:error:limit,serverUnavailable,serverFail,serverPartialFail,unknownMethod,invalidArguments,invalidResultReference,forbidden,accountNotFound,accountNotSupportedByMethod,accountReadOnly,requestTooLarge,cannotCalculateChanges,stateMismatch,anchorNotFound,unsupportedFilter,unsupportedSort,tooManyChanges,fromAccountNotFound,fromAccountNotSupportedByMethod,alreadyExists,overQuota,tooLarge,rateLimit,notFound,invalidPatch,willDestroy,invalidProperties,singleton,mailboxHasChild,mailboxHasEmail,blobNotFound,tooManyKeywords,tooManyMailboxes,forbiddenFrom,invalidEmail,tooManyRecipients,noRecipients,invalidRecipients,forbiddenMailFrom,forbiddenToSend,cannotUnsend");
    }

    [Test]
    public void DataTypeConstantsCoverCoreAndMailDataTypes()
    {
        AssertStringConstants(
            typeof(JmapDataTypes),
            "Core,PushSubscription,Mailbox,Thread,Email,EmailDelivery,SearchSnippet,Identity,EmailSubmission,VacationResponse");
    }

    [Test]
    public void KeywordAndMailboxRoleConstantsMatchWireValues()
    {
        AssertStringConstants(
            typeof(JmapMailKeywords),
            "$draft,$seen,$flagged,$answered,$forwarded,$phishing,$junk,$notjunk");
        AssertStringConstants(
            typeof(JmapMailboxRoles),
            "all,archive,drafts,flagged,important,inbox,junk,sent,trash");
    }

    [Test]
    public void PropertyConstantsCoverProtocolMembersAndHeaderForms()
    {
        AssertStringConstants(
            typeof(JmapProperties),
            "@type,capabilities,accounts,primaryAccounts,username,apiUrl,downloadUrl,uploadUrl,eventSourceUrl,state,maxSizeUpload,maxConcurrentUpload,maxSizeRequest,maxConcurrentRequests,maxCallsInRequest,maxObjectsInGet,maxObjectsInSet,collationAlgorithms,name,isPersonal,isReadOnly,accountCapabilities,maxMailboxesPerEmail,maxMailboxDepth,maxSizeMailboxName,maxSizeAttachmentsPerEmail,emailQuerySortOptions,mayCreateTopLevelMailbox,maxDelayedSend,submissionExtensions,using,methodCalls,createdIds,methodResponses,sessionState,resultOf,path,type,title,status,detail,instance,limit,description,properties,existingId,maxRecipients,invalidRecipients,accountId,id,ids,list,notFound,sinceState,maxChanges,oldState,newState,hasMoreChanges,created,updated,destroyed,updatedProperties,ifInState,create,update,destroy,notCreated,notUpdated,notDestroyed,fromAccountId,ifFromInState,onSuccessDestroyOriginal,destroyFromIfInState,filter,operator,conditions,sort,property,isAscending,collation,position,anchor,anchorOffset,calculateTotal,queryState,canCalculateChanges,total,sinceQueryState,upToId,oldQueryState,newQueryState,removed,added,index,blobId,blobIds,size,copied,notCopied,changed,deviceClientId,url,keys,p256dh,auth,verificationCode,expires,types,pushSubscriptionId,closeafter,ping,interval,parentId,role,sortOrder,totalEmails,unreadEmails,totalThreads,unreadThreads,myRights,mayReadItems,mayAddItems,mayRemoveItems,maySetSeen,maySetKeywords,mayCreateChild,mayRename,mayDelete,maySubmit,isSubscribed,sortAsTree,filterAsTree,hasAnyRole,onDestroyRemoveEmails,emailIds,threadId,mailboxIds,keywords,receivedAt,hasAttachment,email,addresses,headers,value,messageId,inReplyTo,references,sender,from,to,cc,bcc,replyTo,subject,sentAt,partId,charset,disposition,cid,language,location,subParts,bodyStructure,bodyValues,isEncodingProblem,isTruncated,textBody,htmlBody,attachments,preview,bodyProperties,fetchTextBodyValues,fetchHTMLBodyValues,fetchAllBodyValues,maxBodyValueBytes,collapseThreads,inMailbox,inMailboxOtherThan,before,after,minSize,maxSize,allInThreadHaveKeyword,someInThreadHaveKeyword,noneInThreadHaveKeyword,hasKeyword,notKeyword,text,body,header,emails,parsed,notParsable,emailId,textSignature,htmlSignature,identityId,envelope,mailFrom,rcptTo,parameters,sendAt,undoStatus,deliveryStatus,smtpReply,delivered,displayed,dsnBlobIds,mdnBlobIds,identityIds,threadIds,onSuccessUpdateEmail,onSuccessDestroyEmail,isEnabled,fromDate,toDate,header:,:asRaw,:asText,:asAddresses,:asGroupedAddresses,:asMessageIds,:asDate,:asURLs,:all");
    }

    [Test]
    public void MediaTypeConstantsMatchEndpointFormats()
    {
        AssertStringConstants(
            typeof(JmapMediaTypes),
            "application/json,application/problem+json,application/octet-stream,text/event-stream");
    }

    [Test]
    public void NumericLimitsMatchRfcBoundsAndRecommendations()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(JmapProtocolLimits.MinimumIdLength, Is.EqualTo(1));
            Assert.That(JmapProtocolLimits.MaximumIdLength, Is.EqualTo(255));
            Assert.That(JmapProtocolLimits.MinimumInt, Is.EqualTo(-9007199254740991L));
            Assert.That(JmapProtocolLimits.MaximumInt, Is.EqualTo(9007199254740991L));
            Assert.That(JmapProtocolLimits.MinimumUnsignedInt, Is.Zero);
            Assert.That(JmapProtocolLimits.MaximumUnsignedInt, Is.EqualTo(9007199254740991L));
            Assert.That(JmapProtocolLimits.MinimumKeywordLength, Is.EqualTo(1));
            Assert.That(JmapProtocolLimits.MaximumKeywordLength, Is.EqualTo(255));
            Assert.That(JmapProtocolLimits.MinimumMailboxNameLength, Is.EqualTo(1));
            Assert.That(JmapProtocolLimits.MaximumMailboxSortOrder, Is.EqualTo(int.MaxValue));
            Assert.That(JmapProtocolLimits.MaximumEmailPreviewLength, Is.EqualTo(256));
            Assert.That(JmapProtocolLimits.MaximumSearchSnippetPreviewSize, Is.EqualTo(255));
            Assert.That(JmapProtocolLimits.SuggestedMinimumMaxSizeUpload, Is.EqualTo(50000000));
            Assert.That(JmapProtocolLimits.SuggestedMinimumMaxConcurrentUpload, Is.EqualTo(4));
            Assert.That(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, Is.EqualTo(10000000));
            Assert.That(JmapProtocolLimits.SuggestedMinimumMaxConcurrentRequests, Is.EqualTo(4));
            Assert.That(JmapProtocolLimits.SuggestedMinimumMaxCallsInRequest, Is.EqualTo(16));
            Assert.That(JmapProtocolLimits.SuggestedMinimumMaxObjectsInGet, Is.EqualTo(500));
            Assert.That(JmapProtocolLimits.SuggestedMinimumMaxObjectsInSet, Is.EqualTo(500));
            Assert.That(JmapProtocolLimits.MinimumMaxSizeMailboxName, Is.EqualTo(100));
            Assert.That(JmapProtocolLimits.SuggestedMinimumChangesRetentionDays, Is.EqualTo(30));
            Assert.That(JmapProtocolLimits.MinimumUnreferencedBlobLifetimeSeconds, Is.EqualTo(3600));
            Assert.That(JmapProtocolLimits.MinimumPushSubscriptionMaximumLifetimeSeconds, Is.EqualTo(172800));
            Assert.That(JmapProtocolLimits.SuggestedPushSubscriptionMaximumLifetimeSeconds, Is.EqualTo(604800));
            Assert.That(JmapProtocolLimits.MaximumEventSourceMinimumPingIntervalSeconds, Is.EqualTo(30));
            Assert.That(JmapProtocolLimits.MinimumEventSourceMaximumPingIntervalSeconds, Is.EqualTo(300));
        }
    }

    private static void AssertStringConstants(Type type, string commaSeparatedValues)
    {
        string[] expected = commaSeparatedValues.Split(',');
        string[] actual = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }
}
