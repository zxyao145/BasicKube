using KubeClient.Models;

namespace BasicKube.Api.Controllers;

[ApiController]
[Route("/api/[controller]/[action]/{iamId}")]
public abstract class KubeControllerBase : ControllerBase
{
    public string NsName => (string)HttpContext.Items["NsName"]!;
    public int IamId => (int)HttpContext.Items["IamId"]!;


    public static async Task<string> K8SModelToYaml(object model)
    {
        await using StringWriter yamlWriter = new StringWriter();
        Yaml.Serialize(model, yamlWriter);
        string resourceYaml = yamlWriter.ToString();

        return resourceYaml;
    }

    public static async Task<string> K8SModelToJson(object model)
    {
        await using StringWriter yamlWriter = new StringWriter();
        Yaml.Serialize(model, yamlWriter);
        string resourceYaml = yamlWriter.ToString();
        return Yaml.ToJson(resourceYaml);
    }
}