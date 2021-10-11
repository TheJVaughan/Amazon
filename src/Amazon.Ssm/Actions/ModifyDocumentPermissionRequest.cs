﻿#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Amazon.Ssm;

public sealed class ModifyDocumentPermissionRequest : ISsmRequest
{
    public string[] AccountIdsToAdd { get; set; }

    public string[] AccountIdsToRemove { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string PermissionType { get; set; }
}
