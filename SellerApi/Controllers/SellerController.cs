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

        /// <summary>
        /// Obtenir un vendeur
        /// </summary>
        /// <param name="id">ID du vendeur</param>
        /// <returns>Vendeur</returns>
        /// <response code="200">Le vendeur a été trouvé</response>
        /// <response code="404">Le vendeur n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetSeller/{id}")]
        [HttpGet]
        public async Task<ActionResult<Seller>> GetSeller(int id)
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
                return StatusCode(500, ex.Message);
            }
        }


        /// <summary>
        /// Ajouter un nouveau vendeur
        /// </summary>
        /// <param name="seller">Vendeur</param>
        /// <returns>Le vendeur</returns>
        /// <response code="200">Le vendeur a été créé</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("AddSeller")]
        [HttpPost]
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
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Connecter un vendeur
        /// </summary>
        /// <param name="username">Nom d'usager</param>
        /// <param name="password">Mot de passe</param>
        /// <returns>Un json Web token (JWT)</returns>
        /// <response code="200">La connexion a été autorisée</response>
        /// <response code="400">Requête invalide</response>
        /// <response code="404">Usager inexistant</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("ConnectSeller")]
        [HttpPost]
        public async Task<ActionResult> ConnectSeller(string username, string password)
        {
            try
            {
                Seller? sellerDb = await _dbContext.Seller.Where(c => c.Username == username).FirstOrDefaultAsync();

                if (sellerDb == null)
                    return NotFound("Cet usager n'existe pas.");

                if (CheckPassword(sellerDb.Id, password))
                    return Ok(password); //retourner un jsonWebToken

                return BadRequest("Mot de passe eronné.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Obtenir les statistiques d'un vendeur
        /// </summary>
        /// <param name="id">ID du vendeur</param>
        /// <returns>Les statistiques du endeur</returns>
        /// <response code="200">Les statistiques ont été trouvées</response>
        /// <response code="404">Le vendeur n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetSellerStats/{id}")]
        [HttpGet]
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
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Supprimer un vendeur
        /// </summary>
        /// <param name="id">ID du vendeur</param>
        /// <returns>Les statistiques du endeur</returns>
        /// <response code="200">Le vendeur a été supprimé</response>
        /// <response code="404">Le vendeur n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("DeleteSeller/{id}")]
        [HttpDelete]
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
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Modifier un vendeur
        /// </summary>
        /// <param name="seller">Vendeur</param>
        /// <returns></returns>
        /// <response code="200">Le vendeur a été modifié</response>
        /// <response code="404">Le vendeur n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("UpdateSeller")]
        [HttpPut]
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
                return StatusCode(500, ex.Message);
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
