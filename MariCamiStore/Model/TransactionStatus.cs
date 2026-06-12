using MariCamiStore.SeedWork;

namespace MariCamiStore.Model;

/// <summary>An order status.</summary>
public class TransactionStatus : Enumeration
{
    /// <summary>The pending.</summary>
    public static TransactionStatus Applied = new TransactionStatus(nameof(Applied), "Aplicada");

    /// <summary>The ordered.</summary>
    public static TransactionStatus Voided = new TransactionStatus(nameof(Voided), "Anulada");

    /// <summary>Enumerates the items in this collection that meet given criteria.</summary>
    /// <returns>An enumerator that allows foreach to be used to process the matched items.</returns>
    public static IEnumerable<TransactionStatus> List() => new[] { Applied, Voided };

    /// <summary>Constructor.</summary>
    /// <param name="key">The key.</param>
    /// <param name="name">The name.</param>
    public TransactionStatus(string key, string name) : base(key, name) { }

    /// <summary>Creates a new OrderStatus from the given key.</summary>
    /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
    /// <param name="key">The key.</param>
    /// <returns>The OrderStatus.</returns>
    public static TransactionStatus FromKey(string key)
    {
        var item = List().SingleOrDefault(s => s.Key == key);

        if (item == null)
        {
            throw new ArgumentException($"Valores posibles para {nameof(TransactionStatus)}: {string.Join(",", List().Select(s => s.Name))}");
        }

        return item;
    }
}