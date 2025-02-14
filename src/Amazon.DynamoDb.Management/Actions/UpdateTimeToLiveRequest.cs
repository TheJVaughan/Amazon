﻿using System;

using Amazon.DynamoDb.Models;

namespace Amazon.DynamoDb;

public sealed class UpdateTimeToLiveRequest
{
    public UpdateTimeToLiveRequest(string tableName, string attributeName, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(tableName);

        TableName = tableName;
        TimeToLiveSpecification = new TimeToLiveSpecification(attributeName, enabled);
    }

    public string TableName { get; }

    public TimeToLiveSpecification TimeToLiveSpecification { get; }
}
