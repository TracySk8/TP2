using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductApi.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public string Gender { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Colour { get; set; }
        public string Usage { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public string ProductTitle { get; set; }
        public string? Image { get; set; }
        public string? ImageURL { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public double Price { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public int SellerId { get; set; }
    }
}
