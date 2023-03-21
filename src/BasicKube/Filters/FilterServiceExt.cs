using AoAuth.Common.Filters;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicKube.Api.Filters;

public static class FilterServiceExt
{
    public static void AddAppFilters(this FilterCollection filters)
    {
        filters.Add<LoggerFilter>();
        filters.Add<GlobalExceptionFilter>();
        filters.Add<IamIdAndNsNameFilter>();
    }
}
