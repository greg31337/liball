namespace diameter.Tests;

public class HealthCollectorTests
{
    [Test]
    public void NewCollectorReportsStoppedNode()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        HealthCollector collector = new HealthCollector(clock, 4);

        DiameterNodeHealth health = collector.ReadDiameterNodeHealth();

        Assert.Multiple(() =>
        {
            Assert.That(health.Status,
                Is.EqualTo(DiameterHealthStatus.Stopped));
            Assert.That(health.CheckedAt, Is.EqualTo(clock.UtcTime));
            Assert.That(health.AcceptingRequests, Is.False);
            Assert.That(health.Draining, Is.False);
            Assert.That(health.Subsystems, Is.Empty);
            Assert.That(health.RecentFailures, Is.Empty);
        });
    }

    [Test]
    public void HealthySnapshotCoversEveryOperationalSubsystem()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        HealthCollector collector = new HealthCollector(clock, 4);
        collector.UpdateDiameterNodeActivity(true, false, false);
        collector.ReportDiameterSubsystemHealth(CreateHealth(
            HealthCollector.ListenersSubsystem, true));
        collector.ReportDiameterSubsystemHealth(CreateHealth(
            HealthCollector.PeersSubsystem, true));
        collector.ReportDiameterSubsystemHealth(CreateHealth(
            HealthCollector.RoutesSubsystem, true));
        DiameterSubsystemHealth queues = CreateHealth(
            HealthCollector.QueuesSubsystem, true);
        queues.PendingWork = 12;
        queues.UtilizationPercentage = 40;
        collector.ReportDiameterSubsystemHealth(queues);
        DiameterSubsystemHealth timers = CreateHealth(
            HealthCollector.TimersSubsystem, true);
        timers.TimerLagMilliseconds = 15;
        collector.ReportDiameterSubsystemHealth(timers);
        collector.ReportDiameterSubsystemHealth(CreateHealth(
            HealthCollector.StoresSubsystem, true));
        collector.ReportDiameterSubsystemHealth(CreateHealth(
            HealthCollector.OverloadSubsystem, false));

        DiameterNodeHealth health = collector.ReadDiameterNodeHealth();

        Assert.Multiple(() =>
        {
            Assert.That(health.Status,
                Is.EqualTo(DiameterHealthStatus.Healthy));
            Assert.That(health.AcceptingRequests, Is.True);
            Assert.That(health.Subsystems, Has.Count.EqualTo(7));
            Assert.That(health.Subsystems[0].Name,
                Is.EqualTo(HealthCollector.ListenersSubsystem));
            Assert.That(health.Subsystems[1].Name,
                Is.EqualTo(HealthCollector.PeersSubsystem));
            Assert.That(health.Subsystems[2].Name,
                Is.EqualTo(HealthCollector.RoutesSubsystem));
            Assert.That(health.Subsystems[3].Name,
                Is.EqualTo(HealthCollector.QueuesSubsystem));
            Assert.That(health.Subsystems[3].PendingWork, Is.EqualTo(12));
            Assert.That(health.Subsystems[3].UtilizationPercentage,
                Is.EqualTo(40));
            Assert.That(health.Subsystems[4].Name,
                Is.EqualTo(HealthCollector.TimersSubsystem));
            Assert.That(health.Subsystems[4].TimerLagMilliseconds,
                Is.EqualTo(15));
            Assert.That(health.Subsystems[5].Name,
                Is.EqualTo(HealthCollector.StoresSubsystem));
            Assert.That(health.Subsystems[6].Name,
                Is.EqualTo(HealthCollector.OverloadSubsystem));
        });
    }

    [Test]
    public void AggregateDegradesFailsAndRecoversFromSubsystemChanges()
    {
        HealthCollector collector = new HealthCollector(
            new FakeDiscoveryClock(),
            4);
        collector.UpdateDiameterNodeActivity(true, false, false);
        DiameterSubsystemHealth required = CreateHealth("routes", true);
        DiameterSubsystemHealth optional = CreateHealth("overload", false);
        collector.ReportDiameterSubsystemHealth(required);
        collector.ReportDiameterSubsystemHealth(optional);

        required.Status = DiameterHealthStatus.Degraded;
        required.Detail = "one route remains";
        collector.ReportDiameterSubsystemHealth(required);
        DiameterNodeHealth degraded = collector.ReadDiameterNodeHealth();

        required.Status = DiameterHealthStatus.Healthy;
        collector.ReportDiameterSubsystemHealth(required);
        DiameterNodeHealth recovered = collector.ReadDiameterNodeHealth();

        optional.Status = DiameterHealthStatus.Unhealthy;
        collector.ReportDiameterSubsystemHealth(optional);
        DiameterNodeHealth optionalFailure =
            collector.ReadDiameterNodeHealth();

        required.Status = DiameterHealthStatus.Unhealthy;
        collector.ReportDiameterSubsystemHealth(required);
        DiameterNodeHealth requiredFailure =
            collector.ReadDiameterNodeHealth();

        Assert.Multiple(() =>
        {
            Assert.That(degraded.Status,
                Is.EqualTo(DiameterHealthStatus.Degraded));
            Assert.That(degraded.AcceptingRequests, Is.True);
            Assert.That(recovered.Status,
                Is.EqualTo(DiameterHealthStatus.Healthy));
            Assert.That(optionalFailure.Status,
                Is.EqualTo(DiameterHealthStatus.Degraded));
            Assert.That(optionalFailure.AcceptingRequests, Is.True);
            Assert.That(requiredFailure.Status,
                Is.EqualTo(DiameterHealthStatus.Unhealthy));
            Assert.That(requiredFailure.AcceptingRequests, Is.False);
        });
    }

    [Test]
    public void SubsystemTransitionsMaintainFailureAndRecoveryHistory()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        HealthCollector collector = new HealthCollector(clock, 4);
        collector.UpdateDiameterNodeActivity(true, false, false);
        DiameterSubsystemHealth report = CreateHealth("routes", true);

        collector.ReportDiameterSubsystemHealth(report);
        DateTimeOffset firstHealthyAt = clock.UtcTime;
        clock.AdvanceDiscoveryTime(1);
        report.Status = DiameterHealthStatus.Degraded;
        collector.ReportDiameterSubsystemHealth(report);
        DateTimeOffset firstFailureAt = clock.UtcTime;
        clock.AdvanceDiscoveryTime(1);
        report.Status = DiameterHealthStatus.Unhealthy;
        collector.ReportDiameterSubsystemHealth(report);
        DiameterSubsystemHealth continuousFailure = collector
            .ReadDiameterNodeHealth().Subsystems[0];
        clock.AdvanceDiscoveryTime(1);
        report.Status = DiameterHealthStatus.Healthy;
        collector.ReportDiameterSubsystemHealth(report);
        DateTimeOffset recoveredAt = clock.UtcTime;
        clock.AdvanceDiscoveryTime(1);
        report.Status = DiameterHealthStatus.Unhealthy;
        collector.ReportDiameterSubsystemHealth(report);
        DiameterSubsystemHealth secondFailure = collector
            .ReadDiameterNodeHealth().Subsystems[0];
        secondFailure.FailureCount = 99;
        DiameterSubsystemHealth owned = collector
            .ReadDiameterNodeHealth().Subsystems[0];

        Assert.Multiple(() =>
        {
            Assert.That(continuousFailure.LastHealthyAt,
                Is.EqualTo(firstHealthyAt));
            Assert.That(continuousFailure.LastFailureAt,
                Is.EqualTo(firstFailureAt));
            Assert.That(continuousFailure.FailureCount, Is.EqualTo(1));
            Assert.That(owned.LastHealthyAt,
                Is.EqualTo(recoveredAt));
            Assert.That(owned.LastFailureAt,
                Is.EqualTo(clock.UtcTime));
            Assert.That(owned.FailureCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void DrainingAndStoppedStatesOverrideHealthySubsystems()
    {
        HealthCollector collector = new HealthCollector(
            new FakeDiscoveryClock(),
            4);
        collector.ReportDiameterSubsystemHealth(CreateHealth(
            HealthCollector.ListenersSubsystem, true));
        collector.UpdateDiameterNodeActivity(false, true, false);

        DiameterNodeHealth draining = collector.ReadDiameterNodeHealth();
        collector.UpdateDiameterNodeActivity(false, false, true);
        DiameterNodeHealth stopped = collector.ReadDiameterNodeHealth();

        Assert.Multiple(() =>
        {
            Assert.That(draining.Status,
                Is.EqualTo(DiameterHealthStatus.Degraded));
            Assert.That(draining.Draining, Is.True);
            Assert.That(draining.AcceptingRequests, Is.False);
            Assert.That(stopped.Status,
                Is.EqualTo(DiameterHealthStatus.Stopped));
            Assert.That(stopped.Draining, Is.False);
            Assert.That(stopped.AcceptingRequests, Is.False);
        });
    }

    [Test]
    public void RecentFailuresAreBoundedOrderedAndOwned()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        HealthCollector collector = new HealthCollector(clock, 2);
        collector.UpdateDiameterNodeActivity(true, false, false);
        DiameterOperationalEvent first = CreateFailure("failure.one");
        DiameterOperationalEvent second = CreateFailure("failure.two");
        DiameterOperationalEvent third = CreateFailure("failure.three");

        collector.RecordDiameterHealthFailure(first);
        collector.RecordDiameterHealthFailure(second);
        collector.RecordDiameterHealthFailure(third);
        first.Code = "changed";
        second.Code = "changed";
        DiameterNodeHealth snapshot = collector.ReadDiameterNodeHealth();
        snapshot.RecentFailures[0].Code = "snapshot.changed";
        DiameterNodeHealth next = collector.ReadDiameterNodeHealth();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.RecentFailures, Has.Count.EqualTo(2));
            Assert.That(next.RecentFailures[0].Code,
                Is.EqualTo("failure.two"));
            Assert.That(next.RecentFailures[1].Code,
                Is.EqualTo("failure.three"));
            Assert.That(next.RecentFailures[0].OccurredAt,
                Is.EqualTo(clock.UtcTime));
            Assert.That(next.Status,
                Is.EqualTo(DiameterHealthStatus.Degraded));
            Assert.That(next.AcceptingRequests, Is.True);
        });

        collector.ClearDiameterHealthFailures();
        DiameterNodeHealth recovered = collector.ReadDiameterNodeHealth();
        Assert.Multiple(() =>
        {
            Assert.That(recovered.RecentFailures, Is.Empty);
            Assert.That(recovered.Status,
                Is.EqualTo(DiameterHealthStatus.Healthy));
        });
    }

    [Test]
    public void SnapshotsOwnSubsystemReportsAndRemovalIsDeterministic()
    {
        HealthCollector collector = new HealthCollector(
            new FakeDiscoveryClock(),
            2);
        collector.UpdateDiameterNodeActivity(true, false, false);
        DiameterSubsystemHealth report = CreateHealth("peers", true);
        report.Detail = "available";
        collector.ReportDiameterSubsystemHealth(report);
        report.Detail = "caller changed";

        DiameterNodeHealth first = collector.ReadDiameterNodeHealth();
        first.Subsystems[0].Detail = "snapshot changed";
        DiameterNodeHealth second = collector.ReadDiameterNodeHealth();
        bool removed = collector.RemoveDiameterSubsystemHealth("PEERS");
        bool removedAgain = collector.RemoveDiameterSubsystemHealth("peers");

        Assert.Multiple(() =>
        {
            Assert.That(second.Subsystems[0].Detail,
                Is.EqualTo("available"));
            Assert.That(removed, Is.True);
            Assert.That(removedAgain, Is.False);
            Assert.That(collector.ReadDiameterNodeHealth().Subsystems,
                Is.Empty);
        });
    }

    [Test]
    public void ConcurrentReportsProduceAConsistentOwnedSnapshot()
    {
        HealthCollector collector = new HealthCollector(
            new FakeDiscoveryClock(),
            8);
        collector.UpdateDiameterNodeActivity(true, false, false);

        Parallel.For(
            0,
            1000,
            index =>
            {
                DiameterSubsystemHealth health = CreateHealth(
                    "subsystem-" + (index % 10),
                    index % 2 == 0);
                health.PendingWork = index;
                collector.ReportDiameterSubsystemHealth(health);
            });

        DiameterNodeHealth snapshot = collector.ReadDiameterNodeHealth();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Subsystems, Has.Count.EqualTo(10));
            Assert.That(snapshot.Subsystems,
                Has.All.Matches<DiameterSubsystemHealth>(
                    item => item.PendingWork >= 0));
            Assert.That(snapshot.Status,
                Is.EqualTo(DiameterHealthStatus.Healthy));
        });
    }

    [Test]
    public void InvalidHealthInputsAreRejected()
    {
        HealthCollector collector = new HealthCollector(
            new FakeDiscoveryClock(),
            2);
        DiameterSubsystemHealth invalid = CreateHealth("queues", true);
        invalid.UtilizationPercentage = 101;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new HealthCollector(null!, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HealthCollector(new FakeDiscoveryClock(), 0));
            Assert.Throws<ArgumentException>(() =>
                collector.UpdateDiameterNodeActivity(true, true, false));
            Assert.Throws<ArgumentException>(() =>
                collector.ReportDiameterSubsystemHealth(null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                collector.ReportDiameterSubsystemHealth(invalid));
            Assert.Throws<ArgumentException>(() =>
                collector.RecordDiameterHealthFailure(null));
            Assert.Throws<ArgumentException>(() =>
                collector.RemoveDiameterSubsystemHealth(" "));
        });
    }

    private static DiameterSubsystemHealth CreateHealth(
        string name,
        bool required)
    {
        DiameterSubsystemHealth health = new DiameterSubsystemHealth();
        health.Name = name;
        health.Status = DiameterHealthStatus.Healthy;
        health.Detail = "healthy";
        health.Required = required;
        return health;
    }

    private static DiameterOperationalEvent CreateFailure(string code)
    {
        DiameterOperationalEvent failure = new DiameterOperationalEvent();
        failure.Source = "health";
        failure.Code = code;
        failure.IssueSeverity = DiameterIssueSeverity.Error;
        return failure;
    }
}
