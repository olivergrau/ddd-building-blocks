namespace RocketLaunch.Application.Dto;

public class LaunchWindowDto
{
    public DateTime Start { get; }
    public DateTime End   { get; }

    public LaunchWindowDto(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new ArgumentException("Launch window end must be after start.");
        Start = start;
        End   = end;
    }
}