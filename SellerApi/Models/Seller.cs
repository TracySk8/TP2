﻿using System.ComponentModel.DataAnnotations;

namespace SellerApi.Models
{
    public class Seller
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Champs requis")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Champs requis")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Champs requis")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Champs requis")]
        public float Credit { get; set; }

    }
}