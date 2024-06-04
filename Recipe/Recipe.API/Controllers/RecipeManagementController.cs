using Microsoft.AspNetCore.Mvc;
using Recipe.API.Models;

namespace Recipe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeManagementController : ControllerBase
    {
        private static List<RecipeInMemoryStorage> recipes = new List<RecipeInMemoryStorage>();

        [HttpGet]
        public IActionResult GetAllRecipes()
        {
            return Ok(recipes);
        }

        [HttpGet("{id}")]
        public IActionResult GetRecipeById(int id)
        {
            var recipe = recipes.FirstOrDefault(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }
            return Ok(recipe);
        }

        [HttpPost]
        public IActionResult CreateRecipe([FromBody] RecipeInMemoryStorage recipe)
        {
            if (recipe == null)
            {
                return BadRequest();
            }

            recipe.Id = recipes.Count > 0 ? recipes.Max(r => r.Id) + 1 : 1;
            recipes.Add(recipe);
            return CreatedAtAction(nameof(GetRecipeById), new { id = recipe.Id }, recipe);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateRecipe(int id, [FromBody] RecipeInMemoryStorage updatedRecipe)
        {
            if (updatedRecipe == null || updatedRecipe.Id != id)
            {
                return BadRequest();
            }

            var existingRecipe = recipes.FirstOrDefault(r => r.Id == id);
            if (existingRecipe == null)
            {
                return NotFound();
            }

            existingRecipe.Name = updatedRecipe.Name;
            existingRecipe.Image = updatedRecipe.Image;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteRecipe(int id)
        {
            var recipe = recipes.FirstOrDefault(r => r.Id == id);
            if (recipe == null)
            {
                return NotFound();
            }

            recipes.Remove(recipe);
            return NoContent();
        }
    }
}
