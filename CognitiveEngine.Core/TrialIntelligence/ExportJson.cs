using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CognitiveEngine.Core.TrialIntelligence;

public static class ExportJson
{
    public static JsonSerializerSettings Settings { get; } = CreateSettings();

    public static string Serialize(object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return JsonConvert.SerializeObject(value, Settings);
    }

    public static T Deserialize<T>(string json)
    {
        if (json == null)
            throw new ArgumentNullException(nameof(json));
        return JsonConvert.DeserializeObject<T>(json, Settings)
               ?? throw new JsonSerializationException($"Deserialization returned null for {typeof(T).Name}.");
    }

    public static JsonSerializerSettings CreateSettings()
    {
        var s = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            ContractResolver = new DefaultContractResolver(),
            DateParseHandling = DateParseHandling.None
        };
        s.Converters.Add(new StringEnumConverter
        {
            NamingStrategy = new CamelCaseNamingStrategy(),
            AllowIntegerValues = false
        });
        return s;
    }
}
