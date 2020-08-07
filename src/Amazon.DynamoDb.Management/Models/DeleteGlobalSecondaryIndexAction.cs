﻿using System;

namespace Amazon.DynamoDb.Models
{
    public sealed class DeleteGlobalSecondaryIndexAction
    {
        public DeleteGlobalSecondaryIndexAction(string indexName)
        {
            IndexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        }

        public string IndexName { get; }
    }
}
