﻿#nullable disable

namespace Amazon.DynamoDb
{
    public sealed class AttributeDefinition
    {
        public string AttributeName { get; set; }

        public AttributeType AttributeType { get; set; }
    }
}