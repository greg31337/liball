using System;
using System.Collections.Generic;

namespace http;

internal class PreconditionFieldValue
{
    internal bool IsPresent;
    internal bool IsValid;
    internal bool IsWildcard;
    internal bool IsEntityTag;
    internal bool IsDate;
    internal HttpEntityTag EntityTag = new HttpEntityTag();
    internal List<HttpEntityTag> EntityTags = new List<HttpEntityTag>();
    internal DateTimeOffset Date;
}
