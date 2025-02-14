﻿using System;

using Carbon.Data.Streams;

namespace Amazon.Kinesis;

public readonly struct KinesisIterator : IIterator
{
    public KinesisIterator(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    public string Value { get; }
}
