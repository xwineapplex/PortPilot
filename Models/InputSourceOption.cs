namespace PortPilot_Project.Models;

public sealed record InputSourceOption(string Name, ushort Code)
{
    public override string ToString() => $"{Name} (0x{Code:X2})";
}
