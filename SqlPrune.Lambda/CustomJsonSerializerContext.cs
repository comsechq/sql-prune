using System.Text.Json.Serialization;

namespace SqlPrune.Lambda;

/// <summary>
/// This speeds up lambda cold start times!
/// https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/
/// </summary>
[JsonSerializable(typeof(Input))]
public partial class CustomJsonSerializerContext : JsonSerializerContext
{
}