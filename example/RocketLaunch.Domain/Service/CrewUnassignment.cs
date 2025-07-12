namespace RocketLaunch.Domain.Service;

public class CrewUnassignment
{
    public void Unassign(Model.Mission mission, IEnumerable<Model.CrewMember> crewMembers)
    {
        if (mission == null) throw new ArgumentNullException(nameof(mission));
        if (crewMembers == null) throw new ArgumentNullException(nameof(crewMembers));

        mission.Abort();
        foreach (var member in crewMembers)
        {
            member.Release();
        }
    }
}
