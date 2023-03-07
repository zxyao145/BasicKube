namespace BasicKube.Models;

public class AppPublishCommand
{
    public string DeployUnitName { get; set; } = "";
    public string Tag { get; set; } = "";
    public string? Description { get; set; }
}