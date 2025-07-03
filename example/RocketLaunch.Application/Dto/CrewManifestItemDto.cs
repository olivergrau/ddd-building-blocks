namespace RocketLaunch.Application.Dto;

// RocketLaunchScheduling.Domain/Commands/Dtos.cs
using System;

public class CrewManifestItemDto
{
    public string Name { get; }
    public string Role { get; }

    public CrewManifestItemDto(string name, string role)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }
}