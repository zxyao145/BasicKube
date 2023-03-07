namespace BasicKube.Api.Domain.Pod;

public interface IPodService
{
    public Task DelAsync(string name, string nsName);
}
