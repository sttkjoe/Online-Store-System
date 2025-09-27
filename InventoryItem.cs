using System.ComponentModel.DataAnnotations;

public class InventoryItem
{
    [Key]
    public int itemId { get; set; }

    public string Name { get; set; }
    public int quantity { get; set; }
    public string location { get; set; }

    public string DisplayInfo()
    {
        return $"Item: {Name} | Quantity: {quantity} | Location: {location}";
    }
}