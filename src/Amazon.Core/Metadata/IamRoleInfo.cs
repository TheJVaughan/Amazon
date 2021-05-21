﻿#pragma warning disable IDE0057 // Use range operator

#nullable disable

using System;

namespace Amazon.Metadata
{
    internal sealed class IamRoleInfo
    {
        public string Code { get; set; }

        public DateTime LastUpdated { get; set; }

        public string InstanceProfileArn { get; set; }

        public string InstanceProfileId { get; set; }

        public string ProfileName => InstanceProfileArn.Substring(InstanceProfileArn.IndexOf('/') + 1);
    }
}
