namespace RocketLaunch.Application.Dto;

public class PayloadManifestItemDto
{
    public string Item { get; }
    public double Mass { get; }

    public PayloadManifestItemDto(string item, double mass)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Mass = mass;
    }
}