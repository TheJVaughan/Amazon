﻿#nullable disable

using Carbon.Data.Streams;

namespace Amazon.Kinesis;

public sealed class Record : KinesisRequest, IRecord
{
    public Record() { }

    public Record(string streamName, byte[] data)
    {
        StreamName = streamName;
        Data = data;
    }

    public byte[] Data { get; set; }

    public string ExplicitHashKey { get; set; }

    public string PartitionKey { get; set; }

    public string SequenceNumberForOrdering { get; set; }

    public string StreamName { get; init; }
}

/*
{
    "Data": "blob",
    "ExplicitHashKey": "string",
    "PartitionKey": "string",
    "SequenceNumberForOrdering": "string",
    "StreamName": "string"
}
*/
