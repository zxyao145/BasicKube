using Microsoft.OpenApi.Extensions;
using System.ComponentModel.DataAnnotations;

namespace BasicKube.Api.Common;

public static class EnumExtension
{
    public static string GetDisplayName(this Enum enumValue)
    {
        var attribute = enumValue.GetAttributeOfType<DisplayAttribute>();
        return attribute == null 
            ? enumValue.ToString() 
            : attribute.Name ?? "";
    }
}
