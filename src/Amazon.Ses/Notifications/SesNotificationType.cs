﻿using System.Text.Json.Serialization;

namespace Amazon.Ses;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SesNotificationType
{
    Bounce    = 1,
    Complaint = 2
}