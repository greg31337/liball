using System;

namespace http;

internal static class HttpSnapshotCopy
{
    internal static HttpRequest? CopyRequest(HttpRequest? source)
    {
        if (source == null)
        {
            return null;
        }

        HttpRequest copy = new HttpRequest();
        copy.ProtocolVersion = source.ProtocolVersion;
        copy.Method = source.Method;
        copy.MethodMetadata = CopyMethodMetadata(source.MethodMetadata);
        copy.Target = source.Target;
        copy.TargetForm = source.TargetForm;
        copy.Scheme = source.Scheme;
        copy.Authority = source.Authority;
        copy.Path = source.Path;
        copy.HasQuery = source.HasQuery;
        copy.Query = source.Query;
        copy.ConnectProtocol = source.ConnectProtocol;
        copy.Headers = CopyHeaders(source.Headers);
        copy.Priority = CopyPriority(source.Priority);
        copy.HasContinueExpectation = source.HasContinueExpectation;
        copy.ShouldSendContinue = source.ShouldSendContinue;
        copy.HasUnsupportedExpectations = source.HasUnsupportedExpectations;
        copy.IsUpgradeRequested = source.IsUpgradeRequested;
        copy.Content = CopyBytes(source.Content);
        copy.Trailers = CopyHeaders(source.Trailers);
        copy.IsComplete = source.IsComplete;

        int index = 0;
        while (index < source.WeightedPreferences.Count)
        {
            copy.WeightedPreferences.Add(CopyWeightedValue(
                source.WeightedPreferences[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.LanguagePreferences.Count)
        {
            copy.LanguagePreferences.Add(CopyLanguageRange(
                source.LanguagePreferences[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.AuthenticationValues.Count)
        {
            copy.AuthenticationValues.Add(CopyAuthenticationValue(
                source.AuthenticationValues[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.ViaEntries.Count)
        {
            copy.ViaEntries.Add(CopyViaEntry(source.ViaEntries[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.UpgradeProtocols.Count)
        {
            copy.UpgradeProtocols.Add(source.UpgradeProtocols[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static HttpPriority? CopyPriority(HttpPriority? source)
    {
        if (source == null)
        {
            return null;
        }

        HttpPriority copy = new HttpPriority();
        copy.HasUrgency = source.HasUrgency;
        copy.Urgency = source.Urgency;
        copy.HasIncremental = source.HasIncremental;
        copy.Incremental = source.Incremental;
        copy.StructuredValue = CopyStructuredValue(source.StructuredValue);
        int index = 0;
        while (index < source.UnknownParameters.Count)
        {
            copy.UnknownParameters.Add(CopyStructuredMember(
                source.UnknownParameters[index]));
            index = index + 1;
        }

        return copy;
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    static HttpMethodMetadata? CopyMethodMetadata(HttpMethodMetadata? source)
    {
        if (source == null)
        {
            return null;
        }

        HttpMethodMetadata copy = new HttpMethodMetadata();
        copy.Method = source.Method;
        copy.IsRegistered = source.IsRegistered;
        copy.IsCore = source.IsCore;
        copy.IsSafe = source.IsSafe;
        copy.IsIdempotent = source.IsIdempotent;
        copy.IsCacheable = source.IsCacheable;
        copy.HasDefinedRequestContentSemantics =
            source.HasDefinedRequestContentSemantics;
        return copy;
    }

    static HttpHeaders CopyHeaders(HttpHeaders source)
    {
        HttpHeaders copy = new HttpHeaders();
        int index = 0;
        while (index < source.Fields.Count)
        {
            HttpHeaderField sourceField = source.Fields[index];
            HttpHeaderField field = new HttpHeaderField();
            field.Name = sourceField.Name;
            field.Value = CopyBytes(sourceField.Value);
            field.IsSensitive = sourceField.IsSensitive;
            field.NeverIndex = sourceField.NeverIndex;
            copy.Fields.Add(field);
            index = index + 1;
        }

        return copy;
    }

    static HttpWeightedValue CopyWeightedValue(HttpWeightedValue source)
    {
        HttpWeightedValue copy = new HttpWeightedValue();
        copy.Value = source.Value;
        copy.Quality = source.Quality;
        copy.SourceOrder = source.SourceOrder;
        copy.Specificity = source.Specificity;
        int index = 0;
        while (index < source.Parameters.Count)
        {
            copy.Parameters.Add(CopyParameter(source.Parameters[index]));
            index = index + 1;
        }

        return copy;
    }

    static HttpParameter CopyParameter(HttpParameter source)
    {
        HttpParameter copy = new HttpParameter();
        copy.Name = source.Name;
        copy.HasValue = source.HasValue;
        copy.Value = CopyBytes(source.Value);
        copy.IsQuoted = source.IsQuoted;
        return copy;
    }

    static HttpLanguageRange CopyLanguageRange(HttpLanguageRange source)
    {
        HttpLanguageRange copy = new HttpLanguageRange();
        copy.Range = source.Range;
        copy.IsWildcard = source.IsWildcard;
        copy.Quality = source.Quality;
        copy.SourceOrder = source.SourceOrder;
        copy.Specificity = source.Specificity;
        return copy;
    }

    static HttpAuthenticationValue CopyAuthenticationValue(
        HttpAuthenticationValue source)
    {
        HttpAuthenticationValue copy = new HttpAuthenticationValue();
        copy.Scheme = source.Scheme;
        if (source.Token68 != null)
        {
            copy.Token68 = CopyBytes(source.Token68);
        }

        int index = 0;
        while (index < source.Parameters.Count)
        {
            copy.Parameters.Add(CopyParameter(source.Parameters[index]));
            index = index + 1;
        }

        return copy;
    }

    static HttpViaEntry CopyViaEntry(HttpViaEntry source)
    {
        HttpViaEntry copy = new HttpViaEntry();
        copy.ReceivedProtocol = source.ReceivedProtocol;
        copy.ReceivedBy = source.ReceivedBy;
        copy.HasPort = source.HasPort;
        copy.Port = source.Port;
        if (source.Comment != null)
        {
            copy.Comment = CopyBytes(source.Comment);
        }

        return copy;
    }

    static StructuredFieldValue? CopyStructuredValue(
        StructuredFieldValue? source)
    {
        if (source == null)
        {
            return null;
        }

        StructuredFieldValue copy = new StructuredFieldValue();
        copy.IsItem = source.IsItem;
        copy.IsList = source.IsList;
        copy.IsDictionary = source.IsDictionary;
        if (source.Item != null)
        {
            copy.Item = CopyStructuredMember(source.Item);
        }

        int index = 0;
        while (index < source.Members.Count)
        {
            copy.Members.Add(CopyStructuredMember(source.Members[index]));
            index = index + 1;
        }

        return copy;
    }

    static StructuredFieldMember CopyStructuredMember(
        StructuredFieldMember source)
    {
        StructuredFieldMember copy = new StructuredFieldMember();
        copy.ValueKind = source.ValueKind;
        copy.TextValue = source.TextValue;
        copy.ByteSequenceValue = CopyBytes(source.ByteSequenceValue);
        copy.IntegerValue = source.IntegerValue;
        copy.DecimalThousandths = source.DecimalThousandths;
        copy.BooleanValue = source.BooleanValue;
        copy.DateSeconds = source.DateSeconds;
        copy.IsInnerList = source.IsInnerList;
        copy.DictionaryKey = source.DictionaryKey;
        copy.HasExplicitDictionaryValue =
            source.HasExplicitDictionaryValue;
        int index = 0;
        while (index < source.InnerList.Count)
        {
            copy.InnerList.Add(CopyStructuredMember(source.InnerList[index]));
            index = index + 1;
        }

        index = 0;
        while (index < source.Parameters.Count)
        {
            copy.Parameters.Add(CopyStructuredParameter(
                source.Parameters[index]));
            index = index + 1;
        }

        return copy;
    }

    static StructuredFieldParameter CopyStructuredParameter(
        StructuredFieldParameter source)
    {
        StructuredFieldParameter copy = new StructuredFieldParameter();
        copy.Name = source.Name;
        if (source.BareItem != null)
        {
            copy.BareItem = CopyStructuredMember(source.BareItem);
        }

        return copy;
    }
}
