namespace BasicKube.Api.Common.Components.ServiceScaner;

public class ServiceAttribute<T> : ServiceAttribute
{
    public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped
       )
    {
        BaseType = typeof(T);
        Lifetime = lifetime;
    }
}

public class ServiceAttribute : Attribute
{
    public ServiceAttribute(
        Type? baseType = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
        )
    {
        BaseType = baseType;
        Lifetime = lifetime;
    }

    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    public Type? BaseType { get; set; }
}
