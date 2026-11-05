namespace svg.Tests;

public class SvgDocumentProcessorTests
{
    [Test]
    public void PrerequisitesReorderJobsStablyAndMultipassExecutionConverges()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgProcessJob first = CommentJob("first");
        SvgProcessJob second = CommentJob("second");
        SvgProcessJob third = CommentJob("third");
        second.Prerequisites.Add("first");
        third.Prerequisites.Add("second");
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(third);
        options.Jobs.Add(second);
        options.Jobs.Add(first);
        List<string> completed = new List<string>();
        options.JobCompletedCallback = run => completed.Add(run.Pass + ":" + run.Job.Id);

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Converged, Is.True);
            Assert.That(result.PassCount, Is.EqualTo(2));
            Assert.That(result.Changed, Is.True);
            Assert.That(result.Changes.Select(change => change.JobId), Is.EqualTo(new[] { "first", "second", "third" }));
            Assert.That(result.Changes.Select(change => change.Sequence), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(result.Runs.Select(run => run.Job.Id), Is.EqualTo(new[] { "first", "second", "third", "first", "second", "third" }));
            Assert.That(result.Runs.Select(run => run.ChangeCount), Is.EqualTo(new[] { 1, 1, 1, 0, 0, 0 }));
            Assert.That(completed, Is.EqualTo(new[] { "1:first", "1:second", "1:third", "2:first", "2:second", "2:third" }));
            Assert.That(document.DocumentElement.Children.Select(node => node.Value), Is.EqualTo(new[] { "first", "second", "third" }));
        });
    }

    [Test]
    public void MissingCyclicDuplicateAndDisabledPrerequisitesFailBeforeMutation()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgProcessJob missing = CommentJob("missing");
        missing.Prerequisites.Add("absent");
        SvgProcessOptions missingOptions = new SvgProcessOptions();
        missingOptions.Jobs.Add(missing);

        SvgProcessJob cycleA = CommentJob("a");
        SvgProcessJob cycleB = CommentJob("b");
        cycleA.Prerequisites.Add("b");
        cycleB.Prerequisites.Add("a");
        SvgProcessOptions cycleOptions = new SvgProcessOptions();
        cycleOptions.Jobs.Add(cycleA);
        cycleOptions.Jobs.Add(cycleB);

        SvgProcessJob duplicateA = CommentJob("same");
        SvgProcessJob duplicateB = CommentJob("other");
        duplicateB.Id = "same";
        SvgProcessOptions duplicateOptions = new SvgProcessOptions();
        duplicateOptions.Jobs.Add(duplicateA);
        duplicateOptions.Jobs.Add(duplicateB);

        SvgProcessJob disabled = CommentJob("disabled");
        disabled.Enabled = false;
        SvgProcessJob dependent = CommentJob("dependent");
        dependent.Prerequisites.Add("disabled");
        SvgProcessOptions disabledOptions = new SvgProcessOptions();
        disabledOptions.Jobs.Add(disabled);
        disabledOptions.Jobs.Add(dependent);

        SvgProcessResult missingResult = document.ProcessWithOptions(missingOptions);
        SvgProcessResult cycleResult = document.ProcessWithOptions(cycleOptions);
        SvgProcessResult duplicateResult = document.ProcessWithOptions(duplicateOptions);
        SvgProcessResult disabledResult = document.ProcessWithOptions(disabledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { missingResult.Error, cycleResult.Error, duplicateResult.Error, disabledResult.Error }, Has.All.EqualTo(SvgProcessError.InvalidJobGraph));
            Assert.That(new[] { missingResult, cycleResult, duplicateResult, disabledResult }, Has.All.Matches<SvgProcessResult>(item => !item.Success && item.Runs.Count == 0 && item.Changes.Count == 0));
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.DocumentElement.Children, Is.Empty);
        });
    }

    [Test]
    public void DryRunReportsProspectiveChangesAndRestoresDocumentAndIndexes()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='target'/></svg>");
        SvgDocument original = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='target'/></svg>");
        Assert.That(document.Index.GetById("target"), Is.Not.Null);
        int invalidations = 0;
        SvgProcessJob remove = new SvgProcessJob("remove", context =>
        {
            SvgNode target = context.Document.Index.GetById("target");
            if (target != null) Assert.That(context.Mutations.RemoveChild(target.Parent, target).Success, Is.True);
        });
        remove.Reason = "Remove the selected node.";
        SvgProcessOptions options = new SvgProcessOptions();
        options.DryRun = true;
        options.Jobs.Add(remove);
        options.CacheInvalidationCallback = _ => invalidations++;

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.DryRun, Is.True);
            Assert.That(result.RolledBack, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.Changes, Has.Count.EqualTo(1));
            Assert.That(result.Changes[0].Kind, Is.EqualTo(SvgChangeKind.NodeRemove));
            Assert.That(result.Changes[0].Reason, Is.EqualTo("Remove the selected node."));
            Assert.That(result.StartVersion, Is.Zero);
            Assert.That(result.EndVersion, Is.Zero);
            Assert.That(result.IsCurrent(), Is.True);
            Assert.That(document.Index.GetById("target"), Is.Not.Null);
            Assert.That(new SvgNodeApi().DeepEquals(document.DocumentNode, original.DocumentNode), Is.True);
            Assert.That(invalidations, Is.Zero);
        });
    }

    [Test]
    public void NonConvergentJobsStopAtThePassBoundAndRollbackAtomically()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='shape' data-state='a'/></svg>");
        SvgProcessJob toggle = new SvgProcessJob("toggle", context =>
        {
            SvgNode shape = context.Document.Index.GetById("shape");
            SvgAttribute current = shape.Attributes.Single(attribute => attribute.LocalName == "data-state");
            string next = current.Value == "a" ? "b" : "a";
            SvgAttribute replacement = context.Documents.CreateAttribute("data-state", "", "data-state", "", next);
            Assert.That(context.Mutations.ReplaceAttribute(shape, current, replacement).Success, Is.True);
        });
        toggle.Reason = "Toggle state.";
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(toggle);
        options.MaximumPasses = 3;

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgProcessError.ConvergenceLimitExceeded));
            Assert.That(result.PassCount, Is.EqualTo(3));
            Assert.That(result.Converged, Is.False);
            Assert.That(result.RolledBack, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.Changes.Select(change => change.NewValue), Is.EqualTo(new[] { "b", "a", "b" }));
            Assert.That(result.Changes.Select(change => change.Path), Has.All.EqualTo("/svg[1]/rect[1]/@data-state"));
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.Index.GetById("shape").Attributes.Single(attribute => attribute.LocalName == "data-state").Value, Is.EqualTo("a"));
        });
    }

    [Test]
    public void ChangeBoundsAndExplicitJobFailuresRollbackRecordedMutations()
    {
        SvgDocument boundedDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgProcessJob addTwo = new SvgProcessJob("add-two", context =>
        {
            context.Mutations.AppendChild(context.Document.DocumentElement, context.Documents.CreateComment("one"));
            context.Mutations.AppendChild(context.Document.DocumentElement, context.Documents.CreateComment("two"));
        });
        SvgProcessOptions boundedOptions = new SvgProcessOptions();
        boundedOptions.Jobs.Add(addTwo);
        boundedOptions.MaximumChanges = 1;

        SvgDocument failedDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='target'/></svg>");
        SvgProcessJob fail = new SvgProcessJob("fail", context =>
        {
            SvgNode target = context.Document.Index.GetById("target");
            context.Mutations.RemoveChild(target.Parent, target);
            context.Fail("The requested rewrite is unavailable.");
        });
        SvgProcessOptions failedOptions = new SvgProcessOptions();
        failedOptions.Jobs.Add(fail);

        SvgProcessResult bounded = boundedDocument.ProcessWithOptions(boundedOptions);
        SvgProcessResult failed = failedDocument.ProcessWithOptions(failedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Error, Is.EqualTo(SvgProcessError.ChangeLimitExceeded));
            Assert.That(bounded.Changes, Has.Count.EqualTo(2));
            Assert.That(bounded.RolledBack, Is.True);
            Assert.That(boundedDocument.DocumentElement.Children, Is.Empty);
            Assert.That(failed.Error, Is.EqualTo(SvgProcessError.JobFailed));
            Assert.That(failed.Diagnostics.Single().Message, Is.EqualTo("The requested rewrite is unavailable."));
            Assert.That(failed.Changes, Has.Count.EqualTo(1));
            Assert.That(failed.RolledBack, Is.True);
            Assert.That(failedDocument.Index.GetById("target"), Is.Not.Null);
            Assert.That(failedDocument.Version, Is.Zero);
        });
    }

    [Test]
    public void CallbacksCannotRelaxBoundsOrMutateOutsideTheJobChangeLog()
    {
        SvgDocument boundedDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgProcessOptions boundedOptions = new SvgProcessOptions();
        boundedOptions.MaximumChanges = 1;
        boundedOptions.Jobs.Add(new SvgProcessJob("relax", context =>
        {
            context.Mutations.AppendChild(context.Document.DocumentElement, context.Documents.CreateComment("one"));
            context.Mutations.AppendChild(context.Document.DocumentElement, context.Documents.CreateComment("two"));
            boundedOptions.MaximumChanges = 2;
        }));
        SvgDocument completionDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgProcessOptions completionOptions = new SvgProcessOptions();
        completionOptions.Jobs.Add(new SvgProcessJob("observe", context => { }));
        completionOptions.JobCompletedCallback = run =>
        {
            SvgNode comment = new SvgDocumentApi().CreateComment("outside");
            new SvgNodeApi().AppendChild(completionDocument.DocumentElement, comment);
        };

        SvgProcessResult bounded = boundedDocument.ProcessWithOptions(boundedOptions);
        SvgProcessResult completion = completionDocument.ProcessWithOptions(completionOptions);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Error, Is.EqualTo(SvgProcessError.ChangeLimitExceeded));
            Assert.That(bounded.RolledBack, Is.True);
            Assert.That(boundedDocument.DocumentElement.Children, Is.Empty);
            Assert.That(completion.Error, Is.EqualTo(SvgProcessError.UntrackedMutation));
            Assert.That(completion.RolledBack, Is.True);
            Assert.That(completionDocument.DocumentElement.Children, Is.Empty);
            Assert.That(completionDocument.Version, Is.Zero);
        });
    }

    [Test]
    public void CommittedChangesRefreshIndexesInvalidateVersionedStateAndNotifyOnce()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='old'/></svg>");
        SvgNode original = document.Index.GetById("old");
        long originalVersion = original.Version;
        int invalidations = 0;
        SvgProcessJob rename = new SvgProcessJob("rename", context =>
        {
            SvgNode target = context.Document.Index.GetById("old");
            if (target == null) return;
            SvgAttribute current = target.Attributes.Single(attribute => attribute.LocalName == SvgAttributeNames.Id);
            SvgAttribute replacement = context.Documents.CreateAttribute("id", "", "id", "", "new");
            Assert.That(context.Mutations.ReplaceAttribute(target, current, replacement).Success, Is.True);
        });
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(rename);
        options.CacheInvalidationCallback = _ => invalidations++;

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Changed, Is.True);
            Assert.That(result.CacheInvalidated, Is.True);
            Assert.That(result.PassCount, Is.EqualTo(2));
            Assert.That(document.Version, Is.EqualTo(1));
            Assert.That(document.Index.IndexedVersion, Is.EqualTo(document.Version));
            Assert.That(document.Index.GetById("old"), Is.Null);
            Assert.That(document.Index.GetById("new"), Is.SameAs(original));
            Assert.That(original.Version, Is.GreaterThan(originalVersion));
            Assert.That(invalidations, Is.EqualTo(1));
            Assert.That(result.IsCurrent(), Is.True);
        });
    }

    [Test]
    public void EquivalentRunsProduceIdenticalSourceIndependentChangeLogs()
    {
        SvgDocument firstDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='shape' title='old'/></svg>");
        SvgDocument secondDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='shape' title='old'/></svg>");

        SvgProcessResult first = firstDocument.ProcessWithOptions(LogOptions());
        SvgProcessResult second = secondDocument.ProcessWithOptions(LogOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(Log(first), Is.EqualTo(Log(second)));
            Assert.That(first.Changes.Select(change => change.Path), Is.EqualTo(new[] { "/svg[1]/rect[1]/@class", "/svg[1]/rect[1]/@title" }));
            Assert.That(first.Changes.Select(change => change.Reason), Is.EqualTo(new[] { "Add a class.", "Remove obsolete title." }));
            Assert.That(first.Changes.Select(change => change.SemanticLoss), Is.EqualTo(new[] { false, true }));
        });
    }

    [Test]
    public void UntrackedDocumentMutationIsRejectedAndRestored()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(new SvgProcessJob("untracked", context => context.Document.MarkDirty()));

        SvgProcessResult result = document.ProcessWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error, Is.EqualTo(SvgProcessError.UntrackedMutation));
            Assert.That(result.RolledBack, Is.True);
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
        });
    }

    [Test]
    public void DefaultProcessingSucceedsAndCallbackCancellationRollsBack()
    {
        SvgDocument empty = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgProcessResult defaultResult = new SvgDocumentProcessor().ProcessWithDocument(empty);
        SvgDocument cancelledDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        CancellationTokenSource source = new CancellationTokenSource();
        SvgProcessOptions options = new SvgProcessOptions();
        options.CancellationToken = source.Token;
        options.Jobs.Add(new SvgProcessJob("cancel", context =>
        {
            context.Mutations.AppendChild(context.Document.DocumentElement, context.Documents.CreateComment("pending"));
            source.Cancel();
            Assert.That(context.CancellationToken.IsCancellationRequested, Is.True);
        }));

        SvgProcessResult cancelled = new SvgDocumentProcessor().ProcessWithDocumentAndOptions(cancelledDocument, options);

        Assert.Multiple(() =>
        {
            Assert.That(defaultResult.Success, Is.True);
            Assert.That(defaultResult.Converged, Is.True);
            Assert.That(defaultResult.Changed, Is.False);
            Assert.That(cancelled.Error, Is.EqualTo(SvgProcessError.Cancelled));
            Assert.That(cancelled.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
            Assert.That(cancelled.RolledBack, Is.True);
            Assert.That(cancelledDocument.DocumentElement.Children, Is.Empty);
            Assert.That(cancelledDocument.Version, Is.Zero);
        });
    }

    static SvgProcessJob CommentJob(string id)
    {
        SvgProcessJob job = new SvgProcessJob(id, context =>
        {
            if (context.Document.DocumentElement.Children.Any(node => node.Kind == SvgNodeKind.Comment && node.Value == context.Job.Id)) return;
            SvgNode comment = context.Documents.CreateComment(context.Job.Id);
            Assert.That(context.Mutations.AppendChild(context.Document.DocumentElement, comment).Success, Is.True);
        });
        job.Reason = "Add " + id + ".";
        return job;
    }

    static SvgProcessOptions LogOptions()
    {
        SvgProcessJob add = new SvgProcessJob("add-class", context =>
        {
            SvgNode shape = context.Document.Index.GetById("shape");
            if (shape.Attributes.Any(attribute => attribute.LocalName == "class")) return;
            SvgAttribute attribute = context.Documents.CreateAttribute("class", "", "class", "", "processed");
            context.Mutations.AppendAttribute(shape, attribute);
        });
        add.Reason = "Add a class.";
        SvgProcessJob remove = new SvgProcessJob("remove-title", context =>
        {
            SvgNode shape = context.Document.Index.GetById("shape");
            SvgAttribute attribute = shape.Attributes.FirstOrDefault(item => item.LocalName == "title");
            if (attribute != null) context.Mutations.RemoveAttribute(shape, attribute);
        });
        remove.Prerequisites.Add("add-class");
        remove.Reason = "Remove obsolete title.";
        remove.SemanticLoss = true;
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(remove);
        options.Jobs.Add(add);
        return options;
    }

    static string[] Log(SvgProcessResult result)
    {
        return result.Changes.Select(change => string.Join("|", change.Sequence, change.Pass, change.JobId, change.Kind, change.Path, change.AttributeName, change.OldValue, change.NewValue, change.SemanticLoss, change.Reason, change.BeforeVersion, change.AfterVersion)).ToArray();
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
