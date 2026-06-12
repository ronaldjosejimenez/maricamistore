using MariCamiStore.SeedWork;

namespace MariCamiStore.Model;

public class TransactionSource : Enumeration
{
    /// <summary>The pending.</summary>
    public static TransactionSource OrderItem = new TransactionSource(nameof(OrderItem), "OrderItem");

    public static TransactionSource Manual = new TransactionSource(nameof(Manual), "Manual");

    /// <summary>The ordered.</summary>
    public static TransactionSource NotApply = new TransactionSource(nameof(NotApply), "No Aplica");

    /// <summary>Enumerates the items in this collection that meet given criteria.</summary>
    /// <returns>An enumerator that allows foreach to be used to process the matched items.</returns>
    public static IEnumerable<TransactionSource> List() => [ OrderItem, NotApply ];

    /// <summary>Constructor.</summary>
    /// <param name="key">The key.</param>
    /// <param name="name">The name.</param>
    public TransactionSource(string key, string name) : base(key, name) { }

    /// <summary>Creates a new OrderStatus from the given key.</summary>
    /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
    /// <param name="key">The key.</param>
    /// <returns>The OrderStatus.</returns>
    public static TransactionSource FromKey(string key)
    {
        var item = List().SingleOrDefault(s => s.Key == key);

        if (item == null)
        {
            throw new ArgumentException($"Valores posibles para {nameof(TransactionSource)}: {string.Join(",", List().Select(s => s.Name))}");
        }

        return item;
    }
}