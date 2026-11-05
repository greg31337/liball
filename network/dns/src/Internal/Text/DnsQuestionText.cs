using System.Collections.Generic;

namespace dns;

internal static class DnsQuestionText
{
    internal static DnsTextResult ParseQuestion(
        string text,
        DnsTextOptions options)
    {
        if (text == null || options == null)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "question",
                "Question text and options are required.",
                0);
        }

        List<string> tokens = DnsTextOperations.ReadWhitespaceTokens(text);
        if (tokens.Count != 3)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.InvalidText,
                "question",
                "A question requires name, class, and type fields.",
                0);
        }

        DnsTextResult name = DnsPresentationNameText.ParseNameToken(
            tokens[0], options);
        if (!name.Success || name.Name == null)
        {
            return name;
        }

        DnsTextNumberResult recordClass = DnsMnemonicText.ParseClass(
            tokens[1], options.UseMnemonicCodes);
        if (!recordClass.Success)
        {
            return DnsTextOperations.FromNumberFailure(recordClass);
        }

        DnsTextNumberResult recordType = DnsMnemonicText.ParseRecordType(
            tokens[2], options.UseMnemonicCodes);
        if (!recordType.Success)
        {
            return DnsTextOperations.FromNumberFailure(recordType);
        }

        DnsQuestion question = new DnsQuestion();
        question.Name = name.Name;
        question.Class = (DnsClass)recordClass.Value;
        question.ClassValue = (int)recordClass.Value;
        question.RecordType = (DnsRecordType)recordType.Value;
        question.RecordTypeValue = (int)recordType.Value;
        DnsTextResult result = DnsTextOperations.SuccessText(text);
        result.Question = question;
        return result;
    }

    internal static DnsTextResult FormatQuestion(
        DnsQuestion question,
        DnsTextOptions options)
    {
        if (question == null || options == null)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "question",
                "A question and text options are required.",
                0);
        }

        int classValue = DnsHeaderCodec.ResolveCode(
            question.ClassValue, (int)question.Class);
        int typeValue = DnsHeaderCodec.ResolveCode(
            question.RecordTypeValue, (int)question.RecordType);
        if (classValue < 0 || classValue > 65535 ||
            typeValue < 0 || typeValue > 65535)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "question",
                "Question class and type values must fit in sixteen bits.",
                0);
        }

        DnsTextResult name = DnsPresentationNameText.FormatName(
            question.Name, options);
        if (!name.Success)
        {
            return name;
        }

        string text = name.Text + " " +
            DnsMnemonicText.FormatClass(
                classValue, options.UseMnemonicCodes) + " " +
            DnsMnemonicText.FormatRecordType(
                typeValue, options.UseMnemonicCodes);
        DnsTextResult result = DnsTextOperations.SuccessText(text);
        result.Question = question;
        return result;
    }
}
