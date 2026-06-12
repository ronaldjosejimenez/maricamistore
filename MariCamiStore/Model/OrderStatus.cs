using MariCamiStore.SeedWork;

namespace MariCamiStore.Model;

/// <summary>An order status.</summary>
public class OrderStatus : Enumeration
{
    /// <summary>The pending.</summary>
    public static OrderStatus Pending = new OrderStatus(nameof(Pending), "Pendiente");

    /// <summary>The ordered.</summary>
    public static OrderStatus Active = new OrderStatus(nameof(Active), "Activa");

    /// <summary>The delivering.</summary>
    public static OrderStatus Delivering = new OrderStatus(nameof(Delivering), "Entregando");

    /// <summary>The delivered.</summary>
    public static OrderStatus Delivered = new OrderStatus(nameof(Delivered), "Entregada");

    /// <summary>The completed.</summary>
    public static OrderStatus Completed = new OrderStatus(nameof(Completed), "Completada");

    /// <summary>The cancelled.</summary>
    public static OrderStatus Voided = new OrderStatus(nameof(Voided), "Anulada");

    /// <summary>Enumerates the items in this collection that meet given criteria.</summary>
    /// <returns>An enumerator that allows foreach to be used to process the matched items.</returns>
    public static IEnumerable<OrderStatus> List() => new[] { Pending, Active, Delivering, Delivered, Completed, Voided };

    /// <summary>Constructor.</summary>
    /// <param name="key">The key.</param>
    /// <param name="name">The name.</param>
    public OrderStatus(string key, string name) : base(key, name) { }

    /// <summary>Creates a new OrderStatus from the given key.</summary>
    /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
    /// <param name="key">The key.</param>
    /// <returns>The OrderStatus.</returns>
    public static OrderStatus FromKey(string key)
    {
        var item = List().SingleOrDefault(s => s.Key == key);

        if (item == null)
        {
            throw new ArgumentException($"Valores posibles para {nameof(OrderStatus)}: {string.Join(",", List().Select(s => s.Name))}");
        }

        return item;
    }
}