namespace BackgroundWorker.Utils
{
    public static class DictionaryExtensions
    {
        public static void AddOrUpdate<TValue>(this IDictionary<string, TValue> dict, string key, TValue value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }
    }
}
