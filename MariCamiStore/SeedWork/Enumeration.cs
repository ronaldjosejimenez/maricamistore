using System.Reflection;

namespace MariCamiStore.SeedWork;

/// <summary>An enumeration.</summary>
public abstract class Enumeration : IComparable
{
    /// <summary>Gets or sets the name.</summary>
    /// <value>The name.</value>
    public string Name { get; private set; }

    /// <summary>Gets or sets the key.</summary>
    /// <value>The key.</value>
    public string Key { get; private set; }

    /// <summary>Specialized constructor for use only by derived class.</summary>
    /// <param name="key">The key.</param>
    /// <param name="name">The name.</param>
    protected Enumeration(string key, string name)
    {
        Key = key;
        Name = name;
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return Name;
    }

    /// <summary>Gets all items in this collection.</summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <returns>An enumerator that allows foreach to be used to process all items in this collection.</returns>
    public static IEnumerable<T> GetAll<T>() where T : Enumeration, new()
    {
        var type = typeof(T);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var info in fields)
        {
            var instance = new T();
            var locatedValue = info.GetValue(instance) as T;

            if (locatedValue != null)
            {
                yield return locatedValue;
            }
        }
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object obj)
    {
        var otherValue = obj as Enumeration;

        if (otherValue == null)
        {
            return false;
        }

        var typeMatches = GetType().Equals(obj.GetType());
        var valueMatches = Key.Equals(otherValue.Key);

        return typeMatches && valueMatches;
    }

    /// <summary>Serves as the default hash function.</summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }

    /// <summary>Creates a new Enumeration from the given value.</summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A T.</returns>
    public static T FromValue<T>(string value) where T : Enumeration, new()
    {
        var matchingItem = Parse<T, string>(value, "value", item => item.Key == value);
        return matchingItem;
    }

    /// <summary>Creates a new Enumeration from the given display name.</summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <param name="displayName">Name of the display.</param>
    /// <returns>A T.</returns>
    public static T FromDisplayName<T>(string displayName) where T : Enumeration, new()
    {
        var matchingItem = Parse<T, string>(displayName, "display name", item => item.Name == displayName);
        return matchingItem;
    }

    /// <summary>Parses.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the requested operation is invalid.</exception>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <typeparam name="K">Generic type parameter.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="description">The description.</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>A T.</returns>
    private static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration, new()
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem == null)
        {
            var message = string.Format("'{0}' is not a valid {1} in {2}", value, description, typeof(T));

            throw new InvalidOperationException(message);
        }

        return matchingItem;
    }

    /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
    /// <exception cref="T:System.ArgumentException">.</exception>
    /// <param name="other">An object to compare with this instance.</param>
    /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings:  
    /// 
    ///  <list type="table"><listheader><term> Value</term><description> Meaning</description></listheader><item><term> Less than zero</term><description> This instance precedes <paramref name="obj" /> in the sort order.</description></item><item><term> Zero</term><description> This instance occurs in the same position in the sort order as <paramref name="obj" />.</description></item><item><term> Greater than zero</term><description> This instance follows <paramref name="obj" /> in the sort order.</description></item></list></returns>
    public int CompareTo(object other)
    {
        return Key.CompareTo(((Enumeration)other).Key);
    }
}
