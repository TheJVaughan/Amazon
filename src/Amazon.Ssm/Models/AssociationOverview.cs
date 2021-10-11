﻿#nullable disable

using System.Collections.Generic;

namespace Amazon.Ssm;

public sealed class AssociationOverview
{
    public Dictionary<string, int> AssociationStatusAggregatedCount { get; set; }

    public string DetailedStatus { get; set; }

    public string Status { get; set; }
}
