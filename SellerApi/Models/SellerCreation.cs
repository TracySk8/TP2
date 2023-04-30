using System.ComponentModel.DataAnnotations;

namespace SellerApi.Models
{
    public class SellerCreation
    {
        [Required(ErrorMessage = "Champs requis")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Champs requis")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Champs requis")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Champs requis")]
        public string Password { get; set; }
    }
}
