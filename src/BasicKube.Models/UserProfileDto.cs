using System.Text.Json.Serialization;

namespace BasicKube.Models;


public class UserProfileDto
{
    public string Name { get; set; } = "";
    public List<int>? IamIds { get; set; } = new List<int>();


    public string GetIamIdStr()
    {
        return string.Join(",", IamIds ?? new List<int>());
    }

    public static List<int> GetIamIdList(string iamIdStr)
    {
        return iamIdStr
            .Split(",")
            .Select(x => int.Parse(x))
            .ToList();
    }
}
