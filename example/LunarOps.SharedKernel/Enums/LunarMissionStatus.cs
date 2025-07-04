// Domain/Enums/LunarMissionStatus.cs
namespace LunarOps.SharedKernel.Enums
{
    public enum LunarMissionStatus
    {
        Registered,
        DockingScheduled,
        Docked,
        CrewTransferred,
        PayloadUnloaded,
        ReadyForService,
        InService,
        Departed
    }
}