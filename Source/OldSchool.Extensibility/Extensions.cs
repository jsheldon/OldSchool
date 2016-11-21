using System.Collections.Generic;

namespace OldSchool.Extensibility
{
    public static class Extensions
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> collection, TKey key)
        {
            if (collection.ContainsKey(key))
                return collection[key];
            return default(TValue);
        }
    }
}