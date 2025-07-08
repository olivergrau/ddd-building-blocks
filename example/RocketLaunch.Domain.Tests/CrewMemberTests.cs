// RocketLaunch.Domain.Tests/CrewMemberTests.cs

using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.Events.CrewMember;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;
using System.Linq;

namespace RocketLaunch.Domain.Tests;

public class CrewMemberTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var id = new CrewMemberId(Guid.NewGuid());
        var member = new CrewMember(id, "Alice", CrewRole.Commander, ["Flight"]);

        Assert.Equal(id, member.Id);
        Assert.Equal("Alice", member.Name);
        Assert.Equal(CrewRole.Commander, member.Role);
        Assert.Equal(["Flight"], member.Certifications);
        Assert.Equal(CrewMemberStatus.Available, member.Status);
    }

    [Fact]
    public void Assign_WhenAvailable_SetsAssigned()
    {
        var member = new CrewMember(new CrewMemberId(Guid.NewGuid()), "Bob", CrewRole.Pilot, []);

        member.Assign();

        var events = member.GetUncommittedChanges().ToList();
        Assert.Single(events);
        Assert.IsType<CrewMemberAssigned>(events[0]);
        Assert.Equal(CrewMemberStatus.Assigned, member.Status);
    }

    [Fact]
    public void Assign_WhenNotAvailable_Throws()
    {
        var member = new CrewMember(new CrewMemberId(Guid.NewGuid()), "Bob", CrewRole.Pilot, []);
        member.SetStatus(CrewMemberStatus.Unavailable);
        Assert.Throws<Exception>(member.Assign);
    }

    [Fact]
    public void SetCertifications_ReplacesList()
    {
        var member = new CrewMember(new CrewMemberId(Guid.NewGuid()), "Bob", CrewRole.Pilot, ["A"]);

        member.SetCertifications(["B", "C"]);

        var events = member.GetUncommittedChanges().ToList();
        Assert.Single(events);
        var evt = Assert.IsType<CrewMemberCertificationSet>(events[0]);
        Assert.Equal(["B", "C"], evt.Certifications);
        Assert.Equal(["B", "C"], member.Certifications);
    }
}
