using Newtonsoft.Json;
using Recipe.API.Models.JsonModels;

namespace Recipe.API.Services
{
    public class Recipe
    {
        public string Name { get; set; }
        public byte[] Image { get; set; }
    }

    public class RecipeExternalService
    {

        private static readonly string apiKey = "1231809df5af4a4e8a59037d83b59c76";
        private static readonly int imageSize = 500;

        public async Task<IEnumerable<Recipe>> GetRecipesByProductNameAsync(string productName)
        {
            var searchResult = await GetSearchResultAsync(productName);


            var recipies = new List<Recipe>();

            foreach (var recipe in searchResult.results)
            {
                recipies.Add(new Recipe()
                {
                    Name = recipe.name,
                    Image = await DownloadImageAsync(recipe.image)
                });
            }

            return recipies;
        }

        private async Task<SearchResult> GetSearchResultAsync(string productName)
        {
            var url = $"https://api.spoonacular.com/food/ingredients/search?apiKey={apiKey}&query={productName}";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<SearchResult>(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error while searching recipe. Details: {ex.Message}");
            }
        }

        private async Task<byte[]> DownloadImageAsync(string imageName)
        {
            var url = $"https://img.spoonacular.com/ingredients_{imageSize}x{imageSize}/{imageName}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsByteArrayAsync();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error while downloading image. Details: {ex.Message}");
                }
            }
        }
    }
}
