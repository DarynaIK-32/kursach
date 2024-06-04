using Microsoft.AspNetCore.Mvc;
using Recipe.API.Services;

namespace Recipe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        private readonly RecipeExternalService externalService;

        public RecipeController(RecipeExternalService externalService)
        {
            this.externalService = externalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecipe([FromQuery] string productName)
        {
            if (string.IsNullOrEmpty(productName))
            {
                return BadRequest("Product name is required.");
            }

            try
            {
                var result = await this.externalService.GetRecipesByProductNameAsync(productName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
