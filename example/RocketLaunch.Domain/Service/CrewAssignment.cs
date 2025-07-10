namespace RocketLaunch.Domain.Service;

public class CrewAssignment
{
    private readonly IResourceAvailabilityService _validator;

    public CrewAssignment(IResourceAvailabilityService validator)
    {
        _validator = validator;
    }

    public async Task AssignAsync(Model.Mission mission, IEnumerable<Model.CrewMember> crewMembers)
    {
        if (mission == null) throw new ArgumentNullException(nameof(mission));
        if (crewMembers == null) throw new ArgumentNullException(nameof(crewMembers));
        var list = crewMembers.ToList();
        await mission.AssignCrewAsync(list.Select(cm => cm.Id), _validator);
        foreach (var member in list)
        {
            member.Assign();
        }
    }
}
