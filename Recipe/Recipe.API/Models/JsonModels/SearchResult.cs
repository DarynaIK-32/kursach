namespace Recipe.API.Models.JsonModels
{
    public class SearchResult
    {
        public List<RecipeDTO> results { get; set; }
        public int offset { get; set; }
        public int number { get; set; }
        public int totalResults { get; set; }
    }
}
