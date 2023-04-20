using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductApi.Models
{
    public class CartProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public int ClientId { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Champs requis")]
        public int Quantity { get; set; }

        public Product Product { get; set;}
    }
}
