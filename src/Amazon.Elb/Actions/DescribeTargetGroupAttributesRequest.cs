﻿using System.ComponentModel.DataAnnotations;

namespace Amazon.Elb;

public sealed class DescribeTargetGroupAttributesRequest : IElbRequest
{
    public string Action => "DescribeTargetGroupAttributes";

    public DescribeTargetGroupAttributesRequest(string targetGroupArn)
    {
        TargetGroupArn = targetGroupArn;
    }

    [Required]
    public string TargetGroupArn { get; }
}
