using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailTraversalNode
{
    MimeEntity? entity;
    EmailPath path;

    internal EmailTraversalNode(MimeEntity? entity, EmailPath path)
    {
        this.entity = entity;
        this.path = path;
    }

    internal MimeEntity? GetEntity()
    {
        return entity;
    }

    internal EmailPath GetPath()
    {
        return path;
    }
}
