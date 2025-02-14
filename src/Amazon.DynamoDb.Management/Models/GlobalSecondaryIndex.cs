﻿using System;

namespace Amazon.DynamoDb.Models;

public sealed class GlobalSecondaryIndex
{
    public GlobalSecondaryIndex(string indexName, KeySchemaElement[] keySchema, Projection projection)
    {
        ArgumentNullException.ThrowIfNull(indexName);
        ArgumentNullException.ThrowIfNull(keySchema);
        ArgumentNullException.ThrowIfNull(projection);

        IndexName = indexName;
        KeySchema = keySchema;
        Projection = projection;
    }

    public string IndexName { get; }

    public KeySchemaElement[] KeySchema { get; }

    public Projection Projection { get; }

    public ProvisionedThroughput? ProvisionedThroughput { get; set; }
}