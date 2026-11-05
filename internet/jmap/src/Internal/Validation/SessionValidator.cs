using System.Collections.Generic;

namespace jmap;

internal class SessionValidator
{
    internal ValueValidator Values = new ValueValidator();
    internal SubmissionValidator Submissions = new SubmissionValidator();

    internal List<string> ValidateSession(JmapSession? session)
    {
        List<string> invalid = new List<string>();
        if (session == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (session.Capabilities == null)
        {
            invalid.Add("capabilities");
        }
        else if (!session.Capabilities.ContainsKey(JmapCapabilities.Core))
        {
            invalid.Add("capabilities/" + JmapCapabilities.Core);
        }

        if (session.Capabilities != null)
        {
            foreach (KeyValuePair<string, JmapCapability> capability in session.Capabilities)
            {
                string path = "capabilities/" + capability.Key;
                if (!Values.IsCapability(capability.Key) || capability.Value == null)
                {
                    invalid.Add(path);
                }
                else
                {
                    ValidateCapability(capability.Value, path, invalid);
                }
            }
        }

        if (session.Accounts == null)
        {
            invalid.Add("accounts");
        }
        else
        {
            foreach (KeyValuePair<string, JmapAccount> account in session.Accounts)
            {
                string path = "accounts/" + account.Key;
                if (!Values.IsId(account.Key) || account.Value == null)
                {
                    invalid.Add(path);
                }
                else
                {
                    ValidateAccount(account.Value, path, invalid);
                }
            }
        }

        if (session.PrimaryAccounts == null)
        {
            invalid.Add("primaryAccounts");
        }
        else
        {
            foreach (KeyValuePair<string, string> primary in session.PrimaryAccounts)
            {
                string path = "primaryAccounts/" + primary.Key;
                if (!Values.IsCapability(primary.Key))
                {
                    invalid.Add(path);
                }
                else if (!Values.IsId(primary.Value) || session.Accounts == null || !session.Accounts.ContainsKey(primary.Value))
                {
                    invalid.Add(path);
                }
            }
        }

        if (session.Username == null || !Values.IsValidUnicode(session.Username))
        {
            invalid.Add("username");
        }

        if (!IsBoundedEndpointUrl(session.ApiUrl))
        {
            invalid.Add("apiUrl");
        }

        if (!IsBoundedEndpointUrl(session.DownloadUrl))
        {
            invalid.Add("downloadUrl");
        }

        if (!IsBoundedEndpointUrl(session.UploadUrl))
        {
            invalid.Add("uploadUrl");
        }

        if (!IsBoundedEndpointUrl(session.EventSourceUrl))
        {
            invalid.Add("eventSourceUrl");
        }

        if (session.State == null || !Values.IsValidUnicode(session.State))
        {
            invalid.Add("state");
        }

        return invalid;
    }

    internal bool IsBoundedEndpointUrl(string? value)
    {
        return value != null && value.Length > 0 &&
            value.Length <= SecurityLimits.MaximumUriTemplateCharacters &&
            Values.IsValidUnicode(value);
    }

    internal void ValidateCapability(JmapCapability capability, string path, List<string> invalid)
    {
        if (capability.Core != null)
        {
            ValidateCoreCapability(capability.Core, path, invalid);
        }

        if (capability.Mail != null)
        {
            ValidateMailCapability(capability.Mail, path, invalid);
        }

        if (capability.Submission != null)
        {
            ValidateSubmissionCapability(capability.Submission, path, invalid);
        }
    }

    internal void ValidateAccount(JmapAccount account, string path, List<string> invalid)
    {
        if (account.Name == null || !Values.IsValidUnicode(account.Name))
        {
            invalid.Add(path + "/name");
        }

        if (account.AccountCapabilities == null)
        {
            invalid.Add(path + "/accountCapabilities");
            return;
        }

        foreach (KeyValuePair<string, JmapCapability> capability in account.AccountCapabilities)
        {
            string capabilityPath = path + "/accountCapabilities/" + capability.Key;
            if (!Values.IsCapability(capability.Key) || capability.Value == null)
            {
                invalid.Add(capabilityPath);
            }
            else
            {
                ValidateCapability(capability.Value, capabilityPath, invalid);
            }
        }
    }

    internal void ValidateCoreCapability(JmapCoreCapabilities core, string path, List<string> invalid)
    {
        if (!Values.IsUnsignedInt(core.MaxSizeUpload))
        {
            invalid.Add(path + "/maxSizeUpload");
        }

        if (!Values.IsUnsignedInt(core.MaxConcurrentUpload))
        {
            invalid.Add(path + "/maxConcurrentUpload");
        }

        if (!Values.IsUnsignedInt(core.MaxSizeRequest))
        {
            invalid.Add(path + "/maxSizeRequest");
        }

        if (!Values.IsUnsignedInt(core.MaxConcurrentRequests))
        {
            invalid.Add(path + "/maxConcurrentRequests");
        }

        if (!Values.IsUnsignedInt(core.MaxCallsInRequest))
        {
            invalid.Add(path + "/maxCallsInRequest");
        }

        if (!Values.IsUnsignedInt(core.MaxObjectsInGet))
        {
            invalid.Add(path + "/maxObjectsInGet");
        }

        if (!Values.IsUnsignedInt(core.MaxObjectsInSet))
        {
            invalid.Add(path + "/maxObjectsInSet");
        }

        if (core.CollationAlgorithms == null)
        {
            invalid.Add(path + "/collationAlgorithms");
            return;
        }

        int index = 0;
        while (index < core.CollationAlgorithms.Count)
        {
            string algorithm = core.CollationAlgorithms[index];
            if (algorithm == null || algorithm.Length == 0 || !Values.IsValidUnicode(algorithm))
            {
                invalid.Add(path + "/collationAlgorithms/" + index);
            }

            index = index + 1;
        }
    }

    internal void ValidateMailCapability(JmapMailCapabilities mail, string path, List<string> invalid)
    {
        if (mail.HasMaxMailboxesPerEmail && !mail.IsMaxMailboxesPerEmailNull && !Values.IsUnsignedInt(mail.MaxMailboxesPerEmail))
        {
            invalid.Add(path + "/maxMailboxesPerEmail");
        }

        if (mail.HasMaxMailboxDepth && !mail.IsMaxMailboxDepthNull && !Values.IsUnsignedInt(mail.MaxMailboxDepth))
        {
            invalid.Add(path + "/maxMailboxDepth");
        }

        if (!Values.IsUnsignedInt(mail.MaxSizeMailboxName))
        {
            invalid.Add(path + "/maxSizeMailboxName");
        }

        if (!Values.IsUnsignedInt(mail.MaxSizeAttachmentsPerEmail))
        {
            invalid.Add(path + "/maxSizeAttachmentsPerEmail");
        }
    }

    internal void ValidateSubmissionCapability(JmapSubmissionCapabilities submission, string path, List<string> invalid)
    {
        if (!Values.IsUnsignedInt(submission.MaxDelayedSend))
        {
            invalid.Add(path + "/maxDelayedSend");
        }

        if (submission.SubmissionExtensions == null)
        {
            invalid.Add(path + "/submissionExtensions");
            return;
        }

        foreach (KeyValuePair<string, List<string>> extension in submission.SubmissionExtensions)
        {
            string extensionPath = path + "/submissionExtensions/" + extension.Key;
            if (!Submissions.IsSmtpParameterName(extension.Key) || extension.Value == null)
            {
                invalid.Add(extensionPath);
            }
            else
            {
                int index = 0;
                while (index < extension.Value.Count)
                {
                    if (!IsSmtpEhloArgument(extension.Value[index]))
                    {
                        invalid.Add(extensionPath + "/" + index);
                    }

                    index = index + 1;
                }
            }
        }
    }

    internal bool IsSmtpEhloArgument(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '!' || value[index] > '~')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
