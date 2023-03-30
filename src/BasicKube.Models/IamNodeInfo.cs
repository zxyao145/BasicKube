
using System.ComponentModel;

namespace BasicKube.Models;
public class IamNodeInfo
{
    [DisplayName("项目")]
    public string Project { get; set; } = "";

    [DisplayName("IamId")]
    public int IamId { get; set; }
}
