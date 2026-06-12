using MariCamiStore.SeedWork;

namespace MariCamiStore.Model;

/// <summary>An order status.</summary>
public class OrderItemStatus : Enumeration
{
    /// <summary>The pending.</summary>
    public static OrderItemStatus Pending = new OrderItemStatus(nameof(Pending), "Pendiente");

    /// <summary>The active.</summary>
    public static OrderItemStatus Active = new OrderItemStatus(nameof(Active), "Activo");

    /// <summary>The checked.</summary>
    public static OrderItemStatus Checked = new OrderItemStatus(nameof(Checked), "Revisado");

    /// <summary>The delivered.</summary>
    public static OrderItemStatus Delivered = new OrderItemStatus(nameof(Delivered), "Entregada");

    /// <summary>The ordered.</summary>
    public static OrderItemStatus Voided = new OrderItemStatus(nameof(Voided), "Anulada");
    
    /// <summary>Enumerates the items in this collection that meet given criteria.</summary>
    /// <returns>An enumerator that allows foreach to be used to process the matched items.</returns>
    public static IEnumerable<OrderItemStatus> List() => new[] { Pending, Active, Checked, Delivered, Voided };

    /// <summary>Constructor.</summary>
    /// <param name="key">The key.</param>
    /// <param name="name">The name.</param>
    public OrderItemStatus(string key, string name) : base(key, name) { }

    /// <summary>Creates a new OrderStatus from the given key.</summary>
    /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.</exception>
    /// <param name="key">The key.</param>
    /// <returns>The OrderStatus.</returns>
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