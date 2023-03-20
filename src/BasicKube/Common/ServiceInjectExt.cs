﻿using System.Reflection;

namespace BasicKube.Api.Common;

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

public static class ServiceAttributeExt
{
    public static IServiceCollection ScanService(this IServiceCollection serviceConllection)
    {
        var assembles = AppDomain.CurrentDomain.GetAssemblies()
            .Where(u => u.FullName!.Contains("BasicKube"));
        foreach (var assembly in assembles)
        {
            var services = assembly.GetTypes()
                .Where(x => !x.IsInterface
                    && !x.IsAbstract
                    && x.CustomAttributes.Any(
                        attr =>
                        attr.AttributeType == typeof(ServiceAttribute)
                        || attr.AttributeType.BaseType == typeof(ServiceAttribute)

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

        return serviceConllection;
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
