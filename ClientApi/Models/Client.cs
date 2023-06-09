﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientApi.Models
{
    [Index(nameof(Username), IsUnique = true)]
    public class Client
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
