
using System.ComponentModel;

namespace BasicKube.Models;

#nullable disable
public record EventInfo
{
    [DisplayName("DateTime")]
    public DateTime? DateTime { get; set; }

    public string Type { get; set; }

    public string Reason { get; set; }

    public string Source { get; set; }

    public string Message { get; set; }

    public string GetDateTimeStr()
    {
        if (DateTime == null)
        {
            return "";
        }

        return DateTime.Value.ToString("yyyy-MM-dd HH:mm:ss");

        //return DateTime.Value.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }
}
