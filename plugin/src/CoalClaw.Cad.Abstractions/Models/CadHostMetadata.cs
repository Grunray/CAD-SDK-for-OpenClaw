namespace CoalClaw.Cad.Abstractions.Models;

public sealed record CadHostMetadata(
    string HostId,
    string Platform,
    string HostVersion,
    string ApiThreadingVersion,
    int Port,
    string ApiVersion = "1");
