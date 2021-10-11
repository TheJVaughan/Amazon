﻿#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Amazon.Ssm;

public sealed class GetCommandInvocationRequest : ISsmRequest
{
    public GetCommandInvocationRequest() { }

    public GetCommandInvocationRequest(string commandId, string instanceId)
    {
        CommandId = commandId;
        InstanceId = instanceId;
    }

    [Required]
    public string CommandId { get; init; }

    [Required]
    public string InstanceId { get; init; }

    public string PluginName { get; init; }
}
