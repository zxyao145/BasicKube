using System.ComponentModel.DataAnnotations;

namespace BasicKube.Models
{
    public class DeployDetails : AppDetailsQuery
    {
        [MinLength(0)]
        public int ReadyReplicas { get; set; }

        [MinLength(0)]
        public int Replicas { get; set; }
    }
}