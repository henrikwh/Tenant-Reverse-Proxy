public record RequestDump(Dictionary<string, string> headers, Dictionary<string, string> cookies, Dictionary<string, string> claims, Dictionary<string, KeyValuePair<string, object?>> routes);
