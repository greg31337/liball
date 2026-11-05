using System;
using System.Collections.Generic;

namespace template;

internal enum TemplateScopeFrameKind
{
    Root = 0,
    Local = 1,
    Loop = 2,
    Block = 3,
    Macro = 4,
    Include = 5
}

internal class TemplateScopeFrame
{
    internal TemplateScopeFrameKind Kind;
    internal string Name;
    internal TemplateValue CurrentValue;
    internal Dictionary<string, TemplateValue> Locals;

    internal TemplateScopeFrame(TemplateScopeFrameKind kind, string name, TemplateValue currentValue)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (currentValue == null)
        {
            throw new ArgumentNullException("currentValue");
        }

        Kind = kind;
        Name = name;
        CurrentValue = currentValue;
        Locals = new Dictionary<string, TemplateValue>();
    }

    internal void SetLocal(string name, TemplateValue value)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        Locals[name] = value;
    }

    internal bool HasLocal(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        return Locals.ContainsKey(name);
    }

    internal TemplateValue GetLocal(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (!Locals.ContainsKey(name))
        {
            return TemplateValue.Missing();
        }

        return Locals[name];
    }
}

internal class TemplateScopeStack
{
    internal TemplateValue RootValue;
    internal List<TemplateScopeFrame> Frames;

    internal TemplateScopeStack(TemplateValue rootValue)
    {
        if (rootValue == null)
        {
            throw new ArgumentNullException("rootValue");
        }

        RootValue = rootValue;
        Frames = new List<TemplateScopeFrame>();
        Frames.Add(new TemplateScopeFrame(TemplateScopeFrameKind.Root, "", rootValue));
    }

    internal static TemplateScopeStack Create(TemplateValue rootValue)
    {
        return new TemplateScopeStack(rootValue);
    }

    internal int GetFrameCount()
    {
        return Frames.Count;
    }

    internal TemplateScopeFrame GetCurrentFrame()
    {
        return Frames[Frames.Count - 1];
    }

    internal TemplateValue GetCurrentValue()
    {
        return GetCurrentFrame().CurrentValue;
    }

    internal TemplateValue GetParentCurrentValue()
    {
        if (Frames.Count <= 1)
        {
            return RootValue;
        }

        return Frames[Frames.Count - 2].CurrentValue;
    }

    internal TemplateScopeFrame PushFrame(TemplateScopeFrameKind kind, string name, TemplateValue currentValue)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (currentValue == null)
        {
            throw new ArgumentNullException("currentValue");
        }

        TemplateScopeFrame frame = new TemplateScopeFrame(kind, name, currentValue);
        Frames.Add(frame);
        return frame;
    }

    internal TemplateScopeFrame PushLocalScope()
    {
        return PushFrame(TemplateScopeFrameKind.Local, "", GetCurrentValue());
    }

    internal TemplateScopeFrame PushLoopScope(TemplateValue currentValue)
    {
        return PushFrame(TemplateScopeFrameKind.Loop, "", currentValue);
    }

    internal TemplateScopeFrame PushBlockScope(string name)
    {
        return PushFrame(TemplateScopeFrameKind.Block, name, GetCurrentValue());
    }

    internal TemplateScopeFrame PushMacroScope(string name)
    {
        return PushFrame(TemplateScopeFrameKind.Macro, name, RootValue);
    }

    internal TemplateScopeFrame PushIncludeScope(string name, TemplateValue currentValue)
    {
        return PushFrame(TemplateScopeFrameKind.Include, name, currentValue);
    }

    internal TemplateScopeFrame PopFrame()
    {
        if (Frames.Count <= 1)
        {
            throw new InvalidOperationException("Cannot pop the root scope frame.");
        }

        TemplateScopeFrame frame = GetCurrentFrame();
        Frames.RemoveAt(Frames.Count - 1);
        return frame;
    }

    internal void SetLocal(string name, TemplateValue value)
    {
        GetCurrentFrame().SetLocal(name, value);
    }

    internal void SetLiquidLocal(string name, TemplateValue value)
    {
        int index = Frames.Count - 1;
        while (index >= 0)
        {
            if (Frames[index].Kind != TemplateScopeFrameKind.Loop && Frames[index].HasLocal(name))
            {
                Frames[index].SetLocal(name, value);
                return;
            }

            index = index - 1;
        }

        index = Frames.Count - 1;
        while (index > 0 && (Frames[index].Kind == TemplateScopeFrameKind.Loop || Frames[index].Kind == TemplateScopeFrameKind.Include))
        {
            index = index - 1;
        }

        Frames[index].SetLocal(name, value);
    }

    internal bool HasLocal(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = Frames.Count - 1;
        while (index >= 0)
        {
            if (Frames[index].HasLocal(name))
            {
                return true;
            }

            if (Frames[index].Kind == TemplateScopeFrameKind.Macro)
            {
                return false;
            }

            index = index - 1;
        }

        return false;
    }

    internal TemplateValue GetLocal(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = Frames.Count - 1;
        while (index >= 0)
        {
            TemplateValue value = Frames[index].GetLocal(name);
            if (!value.IsMissing())
            {
                return value;
            }

            if (Frames[index].Kind == TemplateScopeFrameKind.Macro)
            {
                return TemplateValue.Missing();
            }

            index = index - 1;
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue GetLocalMatching(string name, bool caseSensitive)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int frameIndex = Frames.Count - 1;
        while (frameIndex >= 0)
        {
            TemplateScopeFrame frame = Frames[frameIndex];
            if (frame.Locals.ContainsKey(name))
            {
                return frame.Locals[name];
            }

            if (!caseSensitive)
            {
                foreach (KeyValuePair<string, TemplateValue> pair in frame.Locals)
                {
                    if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return pair.Value;
                    }
                }
            }

            if (frame.Kind == TemplateScopeFrameKind.Macro)
            {
                return TemplateValue.Missing();
            }

            frameIndex = frameIndex - 1;
        }

        return TemplateValue.Missing();
    }

    internal TemplateValue Resolve(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name == "this")
        {
            return GetCurrentValue();
        }

        TemplateValue local = GetLocal(name);
        if (!local.IsMissing())
        {
            return local;
        }

        TemplateValue currentMember = ResolveMapValue(GetCurrentValue(), name);
        if (!currentMember.IsMissing())
        {
            return currentMember;
        }

        TemplateValue rootMember = ResolveMapValue(RootValue, name);
        if (!rootMember.IsMissing())
        {
            return rootMember;
        }

        return TemplateValue.Missing();
    }

    internal TemplateScopeFrame? FindNearestFrame(TemplateScopeFrameKind kind)
    {
        int index = Frames.Count - 1;
        while (index >= 0)
        {
            if (Frames[index].Kind == kind)
            {
                return Frames[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal TemplateValue ResolveMapValue(TemplateValue value, string name)
    {
        if (value.Kind != TemplateValueKind.Map)
        {
            return TemplateValue.Missing();
        }

        return value.GetMapItem(name);
    }
}
