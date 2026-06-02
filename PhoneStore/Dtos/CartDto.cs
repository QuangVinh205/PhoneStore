namespace PhoneStore.Dtos
{
    public class CartDto
    {
        public PhoneStore.Models.Product? Item { get; set; }
        public int Quantity { get; set; }
    }
}
