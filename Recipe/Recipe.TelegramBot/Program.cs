namespace Recipe.TelegramBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var botToken = "6979108421:AAGUJh1N1y23AX_z6L-9xL0cLuvDaRuRD4E";
            var recipeApiBaseUrl = "https://localhost:7058";

            var recipeBot = new RecipeTelegramBot(botToken, recipeApiBaseUrl);
            recipeBot.StartAsync().GetAwaiter().GetResult();
        }
    }
}