﻿namespace BasicKube.Models
{
    public class ScaleDeployCommand
    {
        public int IamId { get; set; }

        public string DeployName { get; set; } = "";

        /// <summary>
        /// 副本个数
        /// </summary>
        public int Replicas { get; set; }
    }
}