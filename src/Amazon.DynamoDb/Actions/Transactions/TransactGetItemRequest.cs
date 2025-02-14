﻿namespace Amazon.DynamoDb.Transactions;

public sealed class TransactGetItemRequest
{
    public TransactGetItem[] TransactItems { get; init; }

    public ReturnConsumedCapacity? ReturnConsumedCapacity { get; init; }

    public TransactGetItemRequest(TransactGetItem[] transactItems)
    {
        ArgumentNullException.ThrowIfNull(transactItems);

        TransactItems = transactItems;
    }
}

public sealed class TransactGetItem
{
    public TransactGetItem(Get get)
    {
        ArgumentNullException.ThrowIfNull(get);

        Get = get;
    }

    public Get Get { get; init; }
}

public sealed class Get
{
    public Get(string tableName, IReadOnlyDictionary<string, DbValue> key)
    {
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(key);

        TableName = tableName;
        Key = key;
    }

    public string TableName { get; init; }

    public IReadOnlyDictionary<string, DbValue> Key { get; init; }

    public Dictionary<string, string>? ExpressionAttributeNames { get; init; }

    public string? ProjectionExpression { get; init; }
}
