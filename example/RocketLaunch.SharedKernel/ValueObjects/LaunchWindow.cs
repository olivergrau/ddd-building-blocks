// Domain/ValueObjects/LaunchWindow.cs

using DDD.BuildingBlocks.Core.Domain;

namespace RocketLaunch.SharedKernel.ValueObjects
{
    public class LaunchWindow : ValueObject<LaunchWindow>
    {
        public DateTime Start { get; }
        public DateTime End   { get; }

        public LaunchWindow(DateTime start, DateTime end)
        {
            if (end <= start)
                throw new Exception("Launch window end must be after start");
            Start = start;
            End   = end;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            yield return Start;
            yield return End;
        }
    }
}