// RocketLaunch.Domain.Tests/CrewMemberTests.cs

using RocketLaunch.Domain.Model;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;
using Xunit;

namespace RocketLaunch.Domain.Tests;

public class CrewMemberTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var id = new CrewMemberId(Guid.NewGuid());
        var member = new CrewMember(id, "Alice", CrewRole.Commander, new[] { "Flight" });

        Assert.Equal(id, member.Id);
        Assert.Equal("Alice", member.Name);
        Assert.Equal(CrewRole.Commander, member.Role);
        Assert.Equal(new[] { "Flight" }, member.Certifications);
        Assert.Equal(CrewMemberStatus.Available, member.Status);
    }

    [Fact]
    public void Assign_WhenAvailable_SetsAssigned()
    {
        var member = new CrewMember(new CrewMemberId(Guid.NewGuid()), "Bob", CrewRole.Pilot, Array.Empty<string>());
        member.Assign();
        Assert.Equal(CrewMemberStatus.Assigned, member.Status);
    }

    [Fact]
    public void Assign_WhenNotAvailable_Throws()
    {
        var member = new CrewMember(new CrewMemberId(Guid.NewGuid()), "Bob", CrewRole.Pilot, Array.Empty<string>());
        member.SetStatus(CrewMemberStatus.Unavailable);
        Assert.Throws<Exception>(member.Assign);
    }

    [Fact]
    public void SetCertifications_ReplacesList()
    {
        var member = new CrewMember(new CrewMemberId(Guid.NewGuid()), "Bob", CrewRole.Pilot, new[] { "A" });
        member.SetCertifications(new[] { "B", "C" });
        Assert.Equal(new[] { "B", "C" }, member.Certifications);
    }
}
