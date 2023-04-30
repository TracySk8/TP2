namespace OrderApi.Classes
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Gender { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Colour { get; set; }
        public string Usage { get; set; }
        public string ProductTitle { get; set; }
        public string? Image { get; set; }
        public string? ImageURL { get; set; }
        public double Price { get; set; }
        public int SellerId { get; set; }
    }
}
