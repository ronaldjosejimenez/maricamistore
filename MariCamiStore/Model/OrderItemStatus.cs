using MariCamiStore.SeedWork;

namespace MariCamiStore.Model;

/// <summary>An order item status.</summary>
public class OrderItemStatus : Enumeration
{
    /// <summary>The item is valid, counts towards the order and is pending reception.</summary>
    public static OrderItemStatus Active = new OrderItemStatus(nameof(Active), "Activo");

    /// <summary>The item was received by the customer; it still counts towards the order.</summary>
    public static OrderItemStatus Received = new OrderItemStatus(nameof(Received), "Recibido");

    /// <summary>The item was voided; it is kept but no longer counts towards the order.</summary>
    public static OrderItemStatus Voided = new OrderItemStatus(nameof(Voided), "Anulado");

    /// <summary>Enumerates the items in this collection that meet given criteria.</summary>
    /// <returns>An enumerator that allows foreach to be used to process the matched items.</returns>
    public static IEnumerable<OrderItemStatus> List() => new[] { Active, Received, Voided };

    /// <summary>Constructor.</summary>
    /// <param name="key">The key.</param>
    /// <param name="name">The name.</param>
    public OrderItemStatus(string key, string name) : base(key, name) { }

    /// <summary>Creates a new OrderItemStatus from the given key.</summary>
    /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
    /// <param name="key">The key.</param>
    /// <returns>The OrderItemStatus.</returns>
    public static OrderItemStatus FromKey(string key)
    {
        var item = List().SingleOrDefault(s => s.Key == key);

        if (item == null)
        {
            throw new ArgumentException($"Valores posibles para {nameof(OrderItemStatus)}: {string.Join(",", List().Select(s => s.Name))}");
        }

        return item;
    }
}
