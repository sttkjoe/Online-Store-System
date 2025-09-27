using System.ComponentModel.DataAnnotations;

public class Order
{
    [Key]
    public int orderId { get; set; }
    
    public string customerName { get; set; }
    public DateTime orderDate { get; set; }
    public List<InventoryItem> items { get; set; } = new List<InventoryItem>();

    public void AddItem(InventoryItem item)
    {
        items.Add(item);
    }

    public void RemoveItem(int itemID)
    {
        var item = items.Find(i => i.itemId == itemID);
        if (item != null)
        {
            items.Remove(item);
        }
    }

    public string GetOrderSummary()
    {

        return $"Order {orderId} for {customerName} | Items: {items.Count} | Placed: {orderDate}";
    }
}