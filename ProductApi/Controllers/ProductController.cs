using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductApi.Classes;
using ProductApi.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private ProductDBContext _dbContext;
        public ProductController(ProductDBContext context) 
        {
            _dbContext = context;
        }

        [HttpGet]
        [Route("GetProduct/{id}")]
        [SwaggerOperation(Summary = "Obtenir un produit")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le produit a été trouvé", typeof(Product))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le produit n'existe pas", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<Product>> GetProduct([SwaggerParameter("ID du produit")] int id)
        {
            try
            {
                var product = await _dbContext.Product.FindAsync(id);

                if (product == null)
                    return NotFound();

                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetCartProducts/{id}")]
        [SwaggerOperation(Summary = "Obtenir les produits dans le panier d'un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Les produits ont été trouvés", typeof(List<ProductAndQuantity>))]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Le client n'a aucun produit dans son panier")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<List<ProductAndQuantity>>> GetCartProducts([SwaggerParameter("ID du client")] int id)
        {
            try
            {
                if (ClientExists(id).Result == false)
                    return NotFound("Le client n'existe pas");


                var products = await _dbContext.CartProduct
                    .Include(x => x.Product)
                    .Where(c => c.ClientId == id)
                    .ToListAsync();

                if(products.Count == 0)
                    return NoContent();

                List<ProductAndQuantity> lstCartProducts = new List<ProductAndQuantity>();

                foreach (var product in products)
                { 
                    ProductAndQuantity productAndQuantity = new ProductAndQuantity(product.Product, product.Quantity);
                    lstCartProducts.Add(productAndQuantity);
                }    

                return Ok(lstCartProducts);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("ManageCartProduct")]
        [SwaggerOperation(Summary = "Gèrer un produit dans le panier d'un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le panier a été modifié")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client ou le produit n'existe pas", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult> ManageCartProduct(int clientId, int productId, int quantity)
        {
            try
            {
                if (ClientExists(clientId).Result == false)
                    return NotFound("Le client n'existe pas");

                CartProduct? cartProduct = await _dbContext.CartProduct
                .Where(c => c.ClientId == clientId)
                .Where(c => c.ProductId == productId)
                .FirstOrDefaultAsync();

                
                if (cartProduct == null) //Création du CartProduct
                {
                    cartProduct = new CartProduct()
                    {
                        ClientId = clientId,
                        ProductId = productId,
                        Quantity = quantity
                    };
                    await _dbContext.CartProduct.AddAsync(cartProduct);
                }
                else
                { 
                    if(quantity == 0) //Suppression
                        _dbContext.CartProduct.Remove(cartProduct);
                    else //Modifier la quantité
                        cartProduct.Quantity = quantity;
                }

                await _dbContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetSellerProducts/{id}")]
        [SwaggerOperation(Summary = "Obtenir les produits d'un vendeur")]
        [SwaggerResponse(StatusCodes.Status200OK, "Les produits ont été trouvés", typeof(List<Product>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le vendeur n'existe pas", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<List<Product>>> GetSellerProducts([SwaggerParameter("ID du vendeur")] int id)
        {
            try
            {
                //Appeler l'API vendeur pour déterminer si le vendeur existe vraiment?
                //if (seller == null)
                //    return NotFound();

                List<Product> products = await _dbContext.Product.Where(c => c.SellerId == id).ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("AddProduct")]
        [SwaggerOperation(Summary = "Ajouter un nouveau produit")]
        [SwaggerResponse(StatusCodes.Status201Created, "Le produit a été créé", typeof(Product))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult> AddProduct(Product product)
        {
            try 
            { 
                await _dbContext.Product.AddAsync(product);
                await _dbContext.SaveChangesAsync();
                return StatusCode(201, product);
            }
            catch(Exception ex) 
            { 
                return BadRequest(ex.Message);
            }

        }

        [HttpPut]
        [Route("UpdateProduct")]
        [SwaggerOperation(Summary = "Modifier un produit")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le produit a été modifié", typeof(Product))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le produit n'existe pas", typeof(Product))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult> UpdateProduct(Product product)
        {
            try 
            {
                Product? productDb = await _dbContext.Product.FindAsync(product.ProductId);

                if (productDb == null)
                {
                    return NotFound();
                }
                //Modifier les attributs du produit
                productDb.Colour = product.Colour;
                productDb.Gender = product.Gender;
                productDb.Category = product.Category;
                productDb.SubCategory = product.SubCategory;
                productDb.Image = product.Image;
                productDb.ImageURL = product.ImageURL;
                productDb.Usage = product.Usage;
                productDb.Price = product.Price;
                productDb.ProductTitle = product.ProductTitle;

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch(Exception ex) 
            { 
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteProduct/{id}")]
        [SwaggerOperation(Summary = "Supprimer un produit")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le produit a été supprimé", typeof(Product))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le produit n'existe pas", typeof(Product))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult> DeleteProduct([SwaggerParameter("ID du produit")] int id)
        {
            try
            {
                Product? product = await _dbContext.Product.FindAsync(id);

                if (product == null)
                    return NotFound();

                _dbContext.Product.Remove(product);

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("ClearCartProducts/{id}")]
        [SwaggerOperation(Summary = "Supprimer les produits au painer d'un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Les produits ont été supprimés du panier")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult> ClearCartProducts([SwaggerParameter("ID du client")] int id)
        {
            try
            {
                List<CartProduct> cartProducts = await _dbContext.CartProduct.Where(c => c.ClientId == id).ToListAsync();

                if (ClientExists(id).Result == false)
                    return NotFound();

                if (cartProducts.Count != 0)
                {
                    _dbContext.CartProduct.RemoveRange(cartProducts);
                    await _dbContext.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<bool> ClientExists(int clientId)
        {
            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5000/api/Client/GetClient/{clientId}");

            //Client? client = await httpClient.GetFromJsonAsync<ClientApi.Models.Client>($"http://localhost:5000/api/Client/GetClient/{clientId}");


            return response.IsSuccessStatusCode;
        }
    }
}
