using System.Reflection;
using NBitcoin;
using Newtonsoft.Json;

public class WordlistJsonConverter : JsonConverter
{
    static WordlistJsonConverter()
    {

        _Wordlists = new Dictionary<string, Wordlist>(StringComparer.OrdinalIgnoreCase)
        {
            {"English", Wordlist.English},
            {"Japanese", Wordlist.Japanese},
            {"Spanish", Wordlist.Spanish},
            {"ChineseSimplified", Wordlist.ChineseSimplified},
            {"ChineseTraditional", Wordlist.ChineseTraditional},
            {"French", Wordlist.French},
            {"PortugueseBrazil", Wordlist.PortugueseBrazil},
            {"Czech", Wordlist.Czech}
        };

        _WordlistsReverse = _Wordlists.ToDictionary(kv => kv.Value, kv => kv.Key);
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Wordlist).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonToken.String)
        {
            throw new NBitcoin.JsonConverters.JsonObjectException(
                $"Unexpected json token type, expected String, actual {reader.TokenType}", reader);
        }

        if (!_Wordlists.TryGetValue((string)reader.Value, out Wordlist result))
        {
            throw new NBitcoin.JsonConverters.JsonObjectException(
                $"Invalid wordlist, possible values {string.Join(", ", _Wordlists.Keys.ToArray())} (default: English)",
                reader);
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is Wordlist wl)
        {
            writer.WriteValue(_WordlistsReverse[wl]);
        }
    }

    private static readonly Dictionary<string, Wordlist> _Wordlists;
    private static readonly Dictionary<Wordlist, string> _WordlistsReverse;
}
