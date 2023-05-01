using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SellerApi;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using SellerApi.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace SellerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : ControllerBase
    {

        private HttpClient _httpClient;
        private JsonSerializerOptions options;
        private SellerDBContext _dbContext;
        public SellerController(SellerDBContext context)
        {
            _httpClient = new HttpClient();
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _dbContext = context;
        }

        [HttpGet]
        [Route("GetSeller/{id}")]
        [SwaggerOperation(Summary = "Obtenir un Seller")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le Seller a été trouvé", typeof(Seller))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le Seller n'existe pas", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<Seller>> GetSeller([SwaggerParameter("ID du Seller")] int id)
        {
            try
            {
                var seller = await _dbContext.Seller.FindAsync(id);

                if (seller == null)
                    return NotFound();

                return Ok(seller);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("AddSeller")]
        [SwaggerOperation(Summary = "Ajouter un nouveau Seller")]
        [SwaggerResponse(StatusCodes.Status201Created, "Le Seller a été créé", typeof(Seller))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<Seller>> AddSeller(SellerCreation seller)
        {
            try
            {
                bool nameTaken = _dbContext.Seller.Where(c => c.Username == seller.Username).Count() != 0;

                if (nameTaken)
                    return BadRequest("Ce nom d'utilisateur est déjà pris.");

                if (seller.Password.Length < 5)
                    return BadRequest("Le mot de passe doit contenir au moins 5 caractères.");

                //Enregistrer le Seller
                Seller sellerDb = new Seller()
                {
                    FirstName = seller.FirstName,
                    LastName = seller.LastName,
                    Credit = 0,
                    Username = seller.Username
                };

                await _dbContext.Seller.AddAsync(sellerDb);
                await _dbContext.SaveChangesAsync();

                SellerStats stats = new SellerStats()
                {
                    SoldItems = 0,
                    TotalSell = 0,
                    SellerId = sellerDb.Id //Id modifié automatiquement après la sauvegarde
                };

                //Hasher le mot de passe
                byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
                string hashedPassword = HashPassword(seller.Password, salt);

                _dbContext.Password.Add(new Password()
                {
                    Salt = Convert.ToBase64String(salt),
                    Hash = hashedPassword,
                    SellerId = sellerDb.Id
                });

                await _dbContext.SellerStats.AddAsync(stats);

                await _dbContext.SaveChangesAsync();

                return StatusCode(201, sellerDb);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("ConnectSeller")]
        [SwaggerOperation(Summary = "Connecter un Seller")]
        [SwaggerResponse(StatusCodes.Status200OK, "La connexion a été autorisée")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le Seller n'existe pas")]
        public async Task<ActionResult> ConnectSeller(string username, string password)
        {
            try
            {
                Seller? sellerDb = await _dbContext.Seller.Where(c => c.Username == username).FirstOrDefaultAsync();

                if (sellerDb == null)
                    return BadRequest("Cet usager n'existe pas.");

                if (CheckPassword(sellerDb.Id, password))
                    return Ok(password); //retourner un jsonWebToken

                return BadRequest("Mot de passe eronné.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        [Route("GetSellerStats/{id}")]
        [SwaggerOperation(Summary = "Obtenir les statistiques d'un Vendeur")]
        [SwaggerResponse(StatusCodes.Status200OK, "Les statistiques ont été trouvées", typeof(Seller))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le vendeur n'existe pas")]
        public async Task<ActionResult<SellerStats>> GetSellerStats([SwaggerParameter("ID du seller")] int id) //id du Seller
        {
            try
            {
                SellerStats? stats = await _dbContext.SellerStats.Where(c => c.SellerId == id).FirstOrDefaultAsync();

                if (stats == null)
                    return NotFound();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteClient/{id}")]
        [SwaggerOperation(Summary = "Supprimer un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le client a été supprimé", typeof(SellerStats))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas")]
        public async Task<ActionResult<SellerStats>> DeleteSeller(int id)
        {
            try
            {
                Seller? Seller = await _dbContext.Seller.FindAsync(id);
                SellerStats? stats = await _dbContext.SellerStats.Where(c => c.SellerId == id).FirstOrDefaultAsync();

                if (Seller == null)
                    return NotFound();

                _dbContext.Seller.Remove(Seller);
                _dbContext.SellerStats.Remove(stats);
                await _dbContext.SaveChangesAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpPut]
        [Route("UpdateSeller")]
        [SwaggerOperation(Summary = "Modifier un Seller")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le seller a été modifié", typeof(Seller))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le Seller n'existe pas")]
        public async Task<ActionResult<Seller>> UpdateSeller(Seller seller)
        {
            try
            {
                Seller? SellerDb = await _dbContext.Seller.FindAsync(seller.Id);

                if (SellerDb == null)
                    return NotFound();

                SellerDb.LastName = seller.LastName;
                SellerDb.FirstName = seller.FirstName;
                SellerDb.Credit = seller.Credit;
                await _dbContext.SaveChangesAsync();

                return Ok(seller);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        private bool CheckPassword(int SellerId, string password)
        {
            Password? passwordDb = _dbContext.Password.Where(c => c.SellerId == SellerId).FirstOrDefault();

            byte[] salt = Convert.FromBase64String(passwordDb.Salt);
            //Teste le mot de passe à l'aide du sel enregistré
            return HashPassword(password, salt) == passwordDb.Hash;
        }
        private string HashPassword(string password, byte[] salt)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashed;
        }
    }
}
