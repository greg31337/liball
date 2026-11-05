using System;
using System.Collections.Generic;
using System.Text;

namespace email;

class EmailRelatedIndexWorker
    {
        EmailRelatedIndexOptions options;
        Dictionary<string, List<MimeEntity>> contentIds;
        Dictionary<string, List<MimeEntity>> locations;
        List<string> contentIdOrder;
        List<string> locationOrder;

        internal EmailRelatedIndexWorker(EmailRelatedIndexOptions options)
        {
            this.options = options;
            contentIds = new Dictionary<string, List<MimeEntity>>(StringComparer.Ordinal);
            locations = new Dictionary<string, List<MimeEntity>>(StringComparer.Ordinal);
            contentIdOrder = new List<string>();
            locationOrder = new List<string>();
        }

        internal EmailRelatedIndex Build(MimeEntity? root)
        {
            List<MimeEntity> entities = new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, options.GetMaximumDepth(), options.GetMaximumEntityCount());
            IndexEntities(entities);
            List<EmailRelatedDuplicate> duplicates = CreateDuplicates();
            List<EmailRelatedReference> references = new List<EmailRelatedReference>();
            List<EmailRelatedReference> missing = new List<EmailRelatedReference>();
            List<string> missingKeys = new List<string>();
            List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(root, options.GetMaximumDepth(), options.GetMaximumEntityCount());
            int viewIndex = 0;
            while (viewIndex < views.Count)
            {
                EmailAttachmentView view = views[viewIndex];
                if ((view.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html") && view.GetEntity().HasContent())
                {
                    string html = ReadHtml(view.GetEntity());
                    EmailHtmlReferenceScanner scanner = new EmailHtmlReferenceScanner(options.GetMaximumReferences(), options.GetMaximumUrlCharacters());
                    List<string> found = scanner.Find(html);
                    AddReferences(found, view, references, missing, missingKeys);
                }

                viewIndex++;
            }

            return new EmailRelatedIndex(contentIds, locations, duplicates, references, missing);
        }

        void IndexEntities(List<MimeEntity> entities)
        {
            int index = 0;
            while (index < entities.Count)
            {
                MimeEntity entity = entities[index];
                if (entity.HasContentId())
                {
                    Add(contentIds, contentIdOrder, new EmailRelatedUri().NormalizeContentIdWithIdentifier(entity.GetContentId()), entity);
                }

                if (entity.HasContentLocation())
                {
                    Add(locations, locationOrder, new EmailRelatedUri().NormalizeLocation(entity.GetContentLocation()), entity);
                }

                index++;
            }
        }

        void AddReferences(List<string> found, EmailAttachmentView source, List<EmailRelatedReference> references, List<EmailRelatedReference> missing, List<string> missingKeys)
        {
            int index = 0;
            while (index < found.Count)
            {
                if (references.Count >= options.GetMaximumReferences())
                {
                    throw new EmailLimitException("MaxRelatedReferenceCount", options.GetMaximumReferences(), (long)references.Count + 1);
                }

                string original = found[index];
                EmailRelatedReference reference = CreateReference(original, source);
                references.Add(reference);
                if (reference.IsMissing())
                {
                    string missingKey = ((int)reference.GetKind()).ToString() + ":" + reference.GetNormalizedValue();
                    if (!missingKeys.Contains(missingKey))
                    {
                        missingKeys.Add(missingKey);
                        missing.Add(reference);
                    }
                }

                index++;
            }
        }

        EmailRelatedReference CreateReference(string original, EmailAttachmentView source)
        {
            if (original.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
            {
                string contentId = new EmailRelatedUri().NormalizeContentIdWithValue(original);
                MimeEntity? resolvedId = GetFirst(contentIds, contentId);
                return new EmailRelatedReference(original, contentId, EmailRelatedReferenceKind.ContentId, source.GetEntity(), source.GetPath(), resolvedId, resolvedId == null);
            }

            string location = new EmailRelatedUri().NormalizeLocation(original);
            MimeEntity? resolvedLocation = GetFirst(locations, location);
            if (resolvedLocation == null && !new EmailRelatedUri().IsAbsolute(location) && source.GetEntity().HasContentLocation())
            {
                location = new EmailRelatedUri().ResolveLocation(original, source.GetEntity().GetContentLocation());
                resolvedLocation = GetFirst(locations, location);
            }

            if (resolvedLocation != null)
            {
                return new EmailRelatedReference(original, location, EmailRelatedReferenceKind.ContentLocation, source.GetEntity(), source.GetPath(), resolvedLocation, false);
            }

            bool external = new EmailRelatedUri().IsAbsolute(location);
            return new EmailRelatedReference(original, location, external ? EmailRelatedReferenceKind.External : EmailRelatedReferenceKind.ContentLocation, source.GetEntity(), source.GetPath(), null, !external);
        }

        string ReadHtml(MimeEntity entity)
        {
            EmailContent? content = entity.HasDecodedContent() ? entity.GetDecodedContent() : entity.GetRawContent();
            if ((content ?? throw new InvalidOperationException()).HasKnownLength() && content.GetLength() > options.GetMaximumHtmlBytes())
            {
                throw new EmailLimitException("MaxRelatedHtmlBytes", options.GetMaximumHtmlBytes(), content.GetLength());
            }

            byte[] bytes = content.Materialize(options.GetMaximumHtmlBytes());
            if (!entity.HasDecodedContent() && entity.HasKnownTransferEncoding())
            {
                if (entity.GetTransferEncodingKind() == EmailTransferEncodingKind.Base64)
                {
                    EmailBase64DecodeResult decodedBase64 = new EmailBase64().Decode(bytes, EmailBase64DecodeMode.Tolerant, options.GetMaximumHtmlBytes());
                    if (decodedBase64.HasFatalError())
                    {
                        throw new EmailLimitException("MaxRelatedHtmlBytes", options.GetMaximumHtmlBytes(), (long)options.GetMaximumHtmlBytes() + 1);
                    }

                    bytes = decodedBase64.GetDecodedBytes();
                }
                else if (entity.GetTransferEncodingKind() == EmailTransferEncodingKind.QuotedPrintable)
                {
                    EmailQuotedPrintableDecodeResult decodedQuoted = new EmailQuotedPrintable().Decode(bytes, EmailQuotedPrintableDecodeMode.Tolerant, options.GetMaximumHtmlBytes());
                    if (decodedQuoted.HasFatalError())
                    {
                        throw new EmailLimitException("MaxRelatedHtmlBytes", options.GetMaximumHtmlBytes(), (long)options.GetMaximumHtmlBytes() + 1);
                    }

                    bytes = decodedQuoted.GetDecodedBytes();
                }
            }

            string charset = new EmailMimeDefaults().GetEffectiveTextCharset(entity.GetMediaType());
            EmailCharsetDecodeOptions charsetOptions = new EmailCharsetDecodeOptions();
            charsetOptions.SetMaximumCharacters(options.GetMaximumHtmlBytes());
            EmailCharsetDecodeResult decoded = new EmailCharsetRegistry().Decode(bytes, charset, charsetOptions);
            return decoded.HasResolvedCharset() ? decoded.GetText() : Encoding.UTF8.GetString(bytes);
        }

        List<EmailRelatedDuplicate> CreateDuplicates()
        {
            List<EmailRelatedDuplicate> result = new List<EmailRelatedDuplicate>();
            AddDuplicates(result, contentIds, contentIdOrder, EmailRelatedReferenceKind.ContentId);
            AddDuplicates(result, locations, locationOrder, EmailRelatedReferenceKind.ContentLocation);
            return result;
        }

        static void AddDuplicates(List<EmailRelatedDuplicate> result, Dictionary<string, List<MimeEntity>> values, List<string> order, EmailRelatedReferenceKind kind)
        {
            int index = 0;
            while (index < order.Count)
            {
                string key = order[index];
                if (values[key].Count > 1)
                {
                    result.Add(new EmailRelatedDuplicate(kind, key, values[key]));
                }

                index++;
            }
        }

        static void Add(Dictionary<string, List<MimeEntity>> values, List<string> order, string key, MimeEntity entity)
        {
            if (!values.ContainsKey(key))
            {
                values.Add(key, new List<MimeEntity>());
                order.Add(key);
            }

            values[key].Add(entity);
        }

        static MimeEntity? GetFirst(Dictionary<string, List<MimeEntity>> values, string key)
        {
            return values.ContainsKey(key) ? values[key][0] : null;
        }
}
