// Domain/Aggregates/Mission.cs

using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using JetBrains.Annotations;
using RocketLaunch.Domain.Model.Entities;
using RocketLaunch.Domain.Service;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.Events;
using RocketLaunch.SharedKernel.Events.Mission;
using RocketLaunch.SharedKernel.Snapshots;
using RocketLaunch.SharedKernel.ValueObjects;
using AggregateException = DDD.BuildingBlocks.Core.Exception.AggregateException;
// ReSharper disable PossibleMultipleEnumeration
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace RocketLaunch.Domain.Model;

public class Mission : AggregateRoot<MissionId>, ISnapshotEnabled
{
    // Public API
    public Mission(MissionId id,
        MissionName name,
        TargetOrbit target,
        PayloadDescription payload,
        LaunchWindow window)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(window);

        ApplyEvent(
            new MissionCreated(id, name, target, payload, window));
    }

    // For rehydration
    public Mission() : base(default!) { }

    public MissionName        Name            { get; private set; }
    public TargetOrbit        TargetOrbit     { get; private set; }
    public PayloadDescription Payload         { get; private set; }
    public LaunchWindow       Window          { get; private set; }
    public MissionStatus      Status          { get; private set; }
    public RocketId?          AssignedRocket  { get; private set; }
    public LaunchPadId?       AssignedPad     { get; private set; }
    public List<DomainRelation> Crew { get; } = new();

    // Commands
    public async Task AssignRocketAsync(
        Rocket rocket, 
        IResourceAvailabilityService validator)
    {
        if (Status != MissionStatus.Planned)
            throw new AggregateValidationException(
                Id, nameof(Status), Status, "Can only assign rocket in Planned state");

        if (!await validator.IsRocketAvailableAsync(rocket.Id, Window))
            throw new RuleValidationException(
                Id, "Rocket not available", $"RocketId: {rocket.Id}");

        ApplyEvent(new RocketAssigned(
            Id, rocket.Id, 
            rocket.Name,
            rocket.ThrustCapacity,
            rocket.PayloadCapacityKg,
            rocket.CrewCapacity,
            CurrentVersion));
    }


    public async Task AssignLaunchPadAsync(LaunchPad pad, 
        IResourceAvailabilityService validator)
    {
        if (AssignedRocket is null)
            throw new AggregateValidationException(Id, nameof(AssignedPad), null, "Rocket must be assigned first");
            
        if (!await validator.IsLaunchPadAvailableAsync(pad.Id, Window))
            throw new RuleValidationException(
                Id, "LaunchPad not available", $"LaunchPadId: {pad.Id}");
            
        ApplyEvent(new LaunchPadAssigned(Id, pad.Id, 
            pad.Name, pad.Location, pad.SupportedRocketTypes.ToArray(),
            Window, CurrentVersion));
    }

    public async Task AssignCrewAsync(IEnumerable<CrewMemberId> crew, 
        IResourceAvailabilityService validator)
    {
        if (AssignedRocket is null || AssignedPad is null)
            throw new AggregateValidationException(Id, nameof(AssignedRocket), null, "Rocket & pad must be assigned first");
            
        if (!await validator.AreCrewMembersAvailableAsync(crew, Window))
            throw new RuleValidationException(
                Id, "Crew not available");
            
        ApplyEvent(new CrewAssigned(Id, crew, CurrentVersion));
    }

    public void Schedule()
    {
        if (AssignedRocket is null || AssignedPad is null)
            throw new AggregateException(Id, "Resources incomplete");
        if (Status != MissionStatus.Planned)
            throw new AggregateException(Id, "Mission already scheduled or started");
        ApplyEvent(new MissionScheduled(Id, CurrentVersion));
    }

    public void Abort()
    {
        if (Status == MissionStatus.Launched)
            throw new AggregateException(Id, "Cannot abort after launch");
        ApplyEvent(new MissionAborted(Id, CurrentVersion));
    }

    public void MarkLaunched()
    {
        if (Status != MissionStatus.Scheduled)
            throw new AggregateException(Id, "Only scheduled missions can launch");
        ApplyEvent(new MissionLaunched(Id, CurrentVersion));
    }

    public void MarkArrived(DateTime arrivalTime,
        string vehicleType,
        IEnumerable<(string Name,string Role)> crewManifest,
        IEnumerable<(string Item,double Mass)> payloadManifest)
    {
        if (Status != MissionStatus.Launched)
            throw new AggregateException(Id, "Only launched missions can arrive");
        ApplyEvent(new MissionArrivedAtLunarOrbit(
            Id, arrivalTime, vehicleType, crewManifest, payloadManifest, CurrentVersion));
    }

    // Event handlers
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(MissionCreated e)
    {
        Name        = e.Name;
        TargetOrbit = e.TargetOrbit;
        Payload     = e.Payload;
        Window      = e.LaunchWindow;
        Status      = MissionStatus.Planned;
    }
        
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(RocketAssigned e)
    {
        AssignedRocket = e.RocketId;
    }
        
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(LaunchPadAssigned e)
    {
        AssignedPad = e.PadId;
    }
        
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(CrewAssigned e)
    {
        Crew.AddRange(e.Crew.Select(id => new DomainRelation(id.Value.ToString())));
    }
        
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(MissionScheduled e)
    {
        Status = MissionStatus.Scheduled;
    }
        
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(MissionAborted e)
    {
        Status = MissionStatus.Aborted;
    }
        
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(MissionLaunched e)
    {
        Status = MissionStatus.Launched;
    }
        
    [UsedImplicitly]
    [InternalEventHandler]
    private void On(MissionArrivedAtLunarOrbit e)
    {
        Status = MissionStatus.Arrived;
        // Integration-event will be published externally
    }
        
    protected override MissionId GetIdFromStringRepresentation(string value) => new(Guid.Parse(value));
        
    // Snapshot support
    public Snapshot? TakeSnapshot()
    {
        return new MissionSnapshot(Id.ToString(), CurrentVersion)
        {
            Id = Id.Value,
            CurrentVersion = CurrentVersion,
            LastCommittedVersion = LastCommittedVersion,

            Name = Name.Value,
            TargetOrbit = TargetOrbit.Value,
            Payload = Payload.Value,
            LaunchWindowStart = Window.Start,
            LaunchWindowEnd = Window.End,

            Status = Status,
            AssignedRocketId = AssignedRocket?.Value,
            AssignedPadId = AssignedPad?.Value,

            CrewMemberIds = Crew.Select(c => Guid.Parse(c.AggregateId)).ToList()
        };
    }

    public void ApplySnapshot(Snapshot snapshot)
    {
        if (snapshot is not MissionSnapshot s)
            throw new InvalidCastException($"Invalid snapshot type: {snapshot.GetType().Name}");

        // set ID manually (AggregateRoot already has the backing field for Id)
        Id = new MissionId(s.Id);

        CurrentVersion = s.CurrentVersion;
        LastCommittedVersion = s.LastCommittedVersion;

        Name = new MissionName(s.Name);
        TargetOrbit = new TargetOrbit(s.TargetOrbit);
        Payload = new PayloadDescription(s.Payload);
        Window = new LaunchWindow(s.LaunchWindowStart, s.LaunchWindowEnd);

        Status = s.Status;

        AssignedRocket = s.AssignedRocketId != null ? new RocketId(s.AssignedRocketId.Value) : null;
        AssignedPad = s.AssignedPadId != null ? new LaunchPadId(s.AssignedPadId.Value) : null;

        Crew.Clear();
        Crew.AddRange(s.CrewMemberIds.Select(id => new DomainRelation(id.ToString())));
    }
}