using MariCamiStore.SeedWork;

namespace MariCamiStore.Model;

public class TransactionType : Enumeration
{
    /// <summary>The pending.</summary>
    public static TransactionType Payment = new TransactionType(nameof(Payment), "Pago");

    public static TransactionType Charge = new TransactionType(nameof(Charge), "Cargo");

    public static TransactionType Void = new TransactionType(nameof(Void), "Anulación");

    /// <summary>The ordered.</summary>
    public static TransactionType NotApply = new TransactionType(nameof(NotApply), "No Aplica");

    /// <summary>Enumerates the items in this collection that meet given criteria.</summary>
    /// <returns>An enumerator that allows foreach to be used to process the matched items.</returns>
    public static IEnumerable<TransactionType> List() => [ Payment, NotApply, Charge, Void];

    /// <summary>Constructor.</summary>
    /// <param name="key">The key.</param>
    /// <param name="name">The name.</param>
    public TransactionType(string key, string name) : base(key, name) { }

    /// <summary>Creates a new OrderStatus from the given key.</summary>
    /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
    /// <param name="key">The key.</param>
    /// <returns>The OrderStatus.</returns>
    public static TransactionType FromKey(string key)
    {
        var item = List().SingleOrDefault(s => s.Key == key);

        if (item == null)
        {
            throw new ArgumentException($"Valores posibles para {nameof(TransactionType)}: {string.Join(",", List().Select(s => s.Name))}");
        }

        return item;
    }
}