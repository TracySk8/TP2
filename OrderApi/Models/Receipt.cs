using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public class Receipt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime PurchaseDate { get; set; }
        public double SubTotal { get; set; }
        public double TPS { get; set; }
        public double TVQ { get; set; }
        public double TotalCost { get; set; }
        public int ClientId { get; set; }
    }
}
