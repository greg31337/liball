
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDomInvariantTests
{
    [Test]
    public void DeterministicRandomMutationsPreserveEveryTreeInvariant()
    {
        const int MutationCount = 2000;
        HtmlDocument document = Html.Parse(
            "<!doctype html><html><head></head><body><main></main></body></html>");
        HtmlNode body = document.GetBody();
        List<HtmlNode> created = new List<HtmlNode>();
        Random random = new Random(0x5EED5EED);
        int name = 0;

        for (int step = 0; step < MutationCount; step++)
        {
            int operation = random.Next(10);
            if (operation == 0)
            {
                HtmlNode target = PickContainer(document, body, created,
                    random);
                HtmlNode element = document.CreateElement("node" + name++);
                created.Add(element);
                target.Append(element);
            }
            else if (operation == 1)
            {
                HtmlNode text = step % 2 == 0
                    ? document.CreateText("text-" + step)
                    : document.CreateComment("comment-" + step);
                created.Add(text);
                PickContainer(document, body, created, random).Prepend(text);
            }
            else if (operation == 2)
            {
                InsertBesideRandomChild(document, body, created, random,
                    name++);
            }
            else if (operation == 3)
            {
                MoveRandomNode(document, body, created, random);
            }
            else if (operation == 4)
            {
                RemoveRandomNode(created, random);
            }
            else if (operation == 5)
            {
                ReplaceRandomNode(document, created, random, name++);
            }
            else if (operation == 6)
            {
                UnwrapRandomElement(created, random);
            }
            else if (operation == 7)
            {
                PickContainer(document, body, created, random).Empty();
            }
            else if (operation == 8)
            {
                SetRandomText(document, body, created, random, step);
            }
            else
            {
                SpliceRandomFragment(document, body, created, random,
                    name++);
            }

            if (step % 17 == 0)
            {
                body.Normalize();
            }

            string violation = HtmlDomInvariant.FindViolation(document,
                created.ToArray());
            Assert.That(violation, Is.Empty,
                "Mutation " + step + " (operation " + operation + ")");
        }

        Assert.That(document.GetMutationVersionStorage(),
            Is.GreaterThan(1000));
    }

    [Test]
    public void RejectedCyclesOwnershipAndDocumentChildrenRemainAtomic()
    {
        HtmlDocument document = Html.Parse("<!doctype html><p>x");
        HtmlDocument foreign = Html.Parse("<!doctype html><p>foreign");
        HtmlNode root = document.GetRoot();
        HtmlNode body = document.GetBody();
        HtmlNode paragraph = body.GetFirstChild();
        HtmlNode[] originalRootChildren = root.GetChildren();
        HtmlNode[] originalBodyChildren = body.GetChildren();

        Assert.Throws<InvalidOperationException>(() => paragraph.Append(body));
        Assert.Throws<InvalidOperationException>(() => body.Append(body));
        Assert.Throws<ArgumentException>(() => body.Append(
            foreign.CreateElement("foreign")));
        Assert.Throws<InvalidOperationException>(() => root.Append(
            document.CreateElement("second-root")));
        Assert.Throws<InvalidOperationException>(() => root.Append(
            document.CreateText("document text")));

        Assert.Multiple(() =>
        {
            Assert.That(root.GetChildren(), Is.EqualTo(originalRootChildren));
            Assert.That(body.GetChildren(), Is.EqualTo(originalBodyChildren));
            Assert.That(HtmlDomInvariant.FindViolation(document), Is.Empty);
            Assert.That(HtmlDomInvariant.FindViolation(foreign), Is.Empty);
        });
    }

    static HtmlNode PickContainer(HtmlDocument document, HtmlNode body,
        List<HtmlNode> created, Random random)
    {
        HtmlNode[] candidates = created.Where(value =>
            (value.GetKind() == HtmlNodeKind.Element
                && value.GetTemplateContent() == null
                || value.GetKind() == HtmlNodeKind.Fragment)
            && value.GetOwnerDocument() == document).Prepend(body).ToArray();
        return candidates[random.Next(candidates.Length)];
    }

    static void InsertBesideRandomChild(HtmlDocument document, HtmlNode body,
        List<HtmlNode> created, Random random, int name)
    {
        HtmlNode parent = PickContainer(document, body, created, random);
        HtmlNode[] children = parent.GetChildren();
        HtmlNode added = document.CreateElement("inserted" + name);
        created.Add(added);
        if (children.Length == 0)
        {
            parent.Append(added);
            return;
        }

        HtmlNode reference = children[random.Next(children.Length)];
        if (random.Next(2) == 0)
        {
            parent.InsertBefore(added, reference);
        }
        else
        {
            parent.InsertAfter(added, reference);
        }
    }

    static void MoveRandomNode(HtmlDocument document, HtmlNode body,
        List<HtmlNode> created, Random random)
    {
        HtmlNode[] movable = created.Where(value =>
            value.GetKind() != HtmlNodeKind.Fragment).ToArray();
        if (movable.Length == 0)
        {
            return;
        }

        HtmlNode child = movable[random.Next(movable.Length)];
        HtmlNode[] containers = created.Where(value =>
            value.GetKind() == HtmlNodeKind.Element
            && value.GetTemplateContent() == null
            && value != child && !child.Contains(value)).Prepend(body)
            .Where(value => value != child && !child.Contains(value))
            .ToArray();
        if (containers.Length > 0)
        {
            containers[random.Next(containers.Length)].Append(child);
        }
    }

    static void RemoveRandomNode(List<HtmlNode> created, Random random)
    {
        HtmlNode[] attached = created.Where(value =>
            value.GetParent() != null).ToArray();
        if (attached.Length > 0)
        {
            attached[random.Next(attached.Length)].Remove();
        }
    }

    static void ReplaceRandomNode(HtmlDocument document,
        List<HtmlNode> created, Random random, int name)
    {
        HtmlNode[] attached = created.Where(value =>
            value.GetParent() != null).ToArray();
        if (attached.Length == 0)
        {
            return;
        }

        HtmlNode replacement = document.CreateElement("replacement" + name);
        created.Add(replacement);
        attached[random.Next(attached.Length)].ReplaceWith(replacement);
    }

    static void UnwrapRandomElement(List<HtmlNode> created, Random random)
    {
        HtmlNode[] attached = created.Where(value =>
            value.GetKind() == HtmlNodeKind.Element
            && value.GetParent() != null).ToArray();
        if (attached.Length > 0)
        {
            attached[random.Next(attached.Length)].Unwrap();
        }
    }

    static void SetRandomText(HtmlDocument document, HtmlNode body,
        List<HtmlNode> created, Random random, int step)
    {
        HtmlNode[] candidates = created.Where(value =>
            value.GetKind() != HtmlNodeKind.Fragment).Prepend(body).ToArray();
        HtmlNode target = candidates[random.Next(candidates.Length)];
        target.SetTextContent("value-" + step);
        if (target.GetKind() == HtmlNodeKind.Element)
        {
            foreach (HtmlNode child in target.GetChildren())
            {
                if (!created.Contains(child))
                {
                    created.Add(child);
                }
            }
        }
    }

    static void SpliceRandomFragment(HtmlDocument document, HtmlNode body,
        List<HtmlNode> created, Random random, int name)
    {
        HtmlNode target = PickContainer(document, body, created, random);
        HtmlNode fragment = document.CreateFragment();
        HtmlNode element = document.CreateElement("fragment" + name);
        HtmlNode text = document.CreateText("fragment-text-" + name);
        fragment.Append(element);
        fragment.Append(text);
        created.Add(fragment);
        created.Add(element);
        created.Add(text);
        target.Append(fragment);
    }
}
