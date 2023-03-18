using AutoMapper.Internal;
using System.Reflection;

namespace BasicKube.Api.Exceptions;

public class ServiceAttribute<T>: ServiceAttribute
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

public static class ServiceInjectExt
{
    static bool IsCare(CustomAttributeData attr)
    {
        if (attr.ToString().ToString().Contains("BasicKube.Api.Exceptions.ServiceAttribute"))
        {
            var t = attr.AttributeType;
            var b = attr.AttributeType == typeof(ServiceAttribute)
                || attr.AttributeType.BaseType == typeof(ServiceAttribute);
          return attr.AttributeType == typeof(ServiceAttribute)
           || attr.AttributeType.BaseType == typeof(ServiceAttribute);
        }
       return false;
    }

    public static void ScanService(this IServiceCollection serviceConllection)
    {
        var assembles = AppDomain.CurrentDomain.GetAssemblies()
            .Where(u => u.FullName!.Contains("BasicKube"));
        foreach (var assembly in assembles)
        {
            var services = assembly.GetTypes()
                .Where(x => !x.IsInterface
                    && !x.IsAbstract
                    && x.CustomAttributes.Any(
                        attr => (
                        attr.AttributeType == typeof(ServiceAttribute)
                        || attr.AttributeType.BaseType == typeof(ServiceAttribute)
                        )
                   )
                )
                .ToList();
            foreach (var service in services)
            {
                if (service == null)
                {
                    continue;
                }
                var attr = service.GetCustomAttribute<ServiceAttribute>();
                if (attr == null)
                {
                    continue;
                }
                var firstBaseType = GetFirstBaseType(service, attr);
                ServiceDescriptor serviceDescriptor =
                    firstBaseType == null
                    ? new ServiceDescriptor(service, service, attr.Lifetime)
                    : new ServiceDescriptor(firstBaseType, service, attr.Lifetime);
                Console.WriteLine(serviceDescriptor);
                serviceConllection.Add(serviceDescriptor);
            }
        }
    }

    private static Type? GetFirstBaseType(Type service, ServiceAttribute serviceAttribute)
    {
        if (serviceAttribute.BaseType != null)
        {
            return serviceAttribute.BaseType;
        }

        var i = service.GetInterfaces();
        return i.FirstOrDefault();
    }
}
