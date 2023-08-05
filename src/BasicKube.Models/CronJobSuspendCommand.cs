namespace BasicKube.Models;

public class CronJobSuspendCommand
{
    public string AppName { get; set; } = "";
    public bool Suspend { get; set; }
}