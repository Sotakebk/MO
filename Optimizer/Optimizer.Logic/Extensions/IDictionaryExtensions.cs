namespace Optimizer.Logic.Extensions;

public static class DictionaryExtensions
{
    
    public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue addValue, Func<TValue, TValue> updateValueFactory) where TKey : notnull
    {
        if (key is null)
            throw new ArgumentException(nameof(key));

        if (updateValueFactory is null)
            throw new ArgumentException(nameof(updateValueFactory));


        if (dict.TryGetValue(key, out var val))
        {
            var updateValue = updateValueFactory(val);
            dict[key] = updateValue;
            return updateValue;
        }
        else
        {
            dict[key] = addValue;
            return addValue;
        }
    }
    
    public static TValue AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory) where TKey : notnull
    {
        if (key is null)
            throw new ArgumentException(nameof(key));

        if (addValueFactory is null)
            throw new ArgumentException(nameof(addValueFactory));

        if (updateValueFactory is null)
            throw new ArgumentException(nameof(updateValueFactory));


        if (dict.TryGetValue(key, out var val))
        {
            var newValue = updateValueFactory(key, val);
            dict[key] = newValue;
            return newValue;
        }
        else
        {
            var newValue = addValueFactory(key);
            dict[key] = newValue;
            return newValue;
        }
    }
}