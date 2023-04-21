using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClientApi.Classes
{
    public class ClientCreation
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
