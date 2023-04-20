using ProductApi.Models;

namespace ProductApi.Classes
{
    public class ProductAndQuantity
    {
        public ProductAndQuantity(Product product, int quantity) {
            Product = product;
            Quantity = quantity;
        }
        public Product Product { get; set; }

        public int Quantity { get; set; }
    }
}
