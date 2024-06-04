using Newtonsoft.Json;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Recipe.TelegramBot
{
    public class RecipeTelegramBot
    {
        private readonly TelegramBotClient botClient;
        private readonly string recipeApiBaseUrl;
        private readonly Dictionary<long, (int RecipeId, string RecipeName, byte[] Image)> updateRecipeData;
        private readonly Dictionary<long, (string RecipeName, byte[] Image)> createRecipeData;

        public RecipeTelegramBot(string botToken, string recipeApiBaseUrl)
        {
            this.botClient = new TelegramBotClient(botToken);
            this.recipeApiBaseUrl = recipeApiBaseUrl;
            this.updateRecipeData = new Dictionary<long, (int RecipeId, string RecipeName, byte[] Image)>();
            this.createRecipeData = new Dictionary<long, (string RecipeName, byte[] Image)>();
        }

        public async Task StartAsync()
        {
            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Hello, I am user {me.Id} and my name is {me.FirstName}.");
            botClient.OnMessage += async (sender, e) =>
            {
                if (e.Message.Type == MessageType.Text)
                {
                    var commandParts = e.Message.Text.Split(' ');
                    var command = commandParts[0].ToLower();
                    var parameter = commandParts.Length > 1 ? string.Join(" ", commandParts.Skip(1)) : null;

                    switch (command)
                    {
                        case "/getall":
                            await HandleGetAllRecipesAsync(e.Message.Chat.Id);
                            break;
                        case "/get":
                            if (int.TryParse(parameter, out int id))
                            {
                                await HandleGetRecipeByIdAsync(e.Message.Chat.Id, id);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: e.Message.Chat.Id,
                                    text: "Please provide a valid recipe ID."
                                );
                            }
                            break;
                        case "/create":
                            await HandleCreateCommandAsync(e.Message.Chat.Id, parameter);
                            break;
                        case "/update":
                            await HandleUpdateCommandAsync(e.Message.Chat.Id, parameter);
                            break;
                        case "/delete":
                            if (int.TryParse(parameter, out id))
                            {
                                await HandleDeleteRecipeAsync(e.Message.Chat.Id, id);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: e.Message.Chat.Id,
                                    text: "Please provide a valid recipe ID."
                                );
                            }
                            break;
                        case "/search":
                            if (!string.IsNullOrEmpty(parameter))
                            {
                                await HandleSearchByIngredientAsync(e.Message.Chat.Id, parameter);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(
                                    chatId: e.Message.Chat.Id,
                                    text: "Please provide an ingredient to search for."
                                );
                            }
                            break;
                        default:
                            await botClient.SendTextMessageAsync(
                                chatId: e.Message.Chat.Id,
                                text: "Unknown command."
                            );
                            break;
                    }
                }
                else if (e.Message.Type == MessageType.Photo)
                {
                    if (updateRecipeData.ContainsKey(e.Message.Chat.Id))
                    {
                        await HandleUpdatePhotoMessageAsync(e.Message);
                    }
                    else if (createRecipeData.ContainsKey(e.Message.Chat.Id))
                    {
                        await HandleCreatePhotoMessageAsync(e.Message);
                    }
                }
            };

            botClient.StartReceiving();

            Console.WriteLine("Bot is running. Press any key to exit...");
            Console.ReadKey();
            botClient.StopReceiving();
        }

        private async Task HandleSearchByIngredientAsync(long chatId, string ingredient)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var url = $"{recipeApiBaseUrl}/api/Recipe?productName={ingredient}";
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var recipes = JsonConvert.DeserializeObject<List<Recipe>>(jsonResponse);

                    if (recipes?.Count > 0)
                    {
                        var limitedRecipes = recipes.Take(10).ToList();
                        foreach (var recipe in limitedRecipes)
                        {
                            await botClient.SendPhotoAsync(
                                chatId: chatId,
                                photo: new InputOnlineFile(new MemoryStream(recipe.Image)),
                                caption: recipe.Name
                            );
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"No recipes found for ingredient '{ingredient}'."
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "An error occurred while retrieving recipes."
                    );
                }
            }
        }

        private async Task HandleGetAllRecipesAsync(long chatId)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync($"{recipeApiBaseUrl}/api/RecipeManagement");
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var recipes = JsonConvert.DeserializeObject<List<Recipe>>(jsonResponse);

                    if (recipes?.Count > 0)
                    {
                        foreach (var recipe in recipes)
                        {
                            await botClient.SendPhotoAsync(
                                chatId: chatId,
                                photo: new InputOnlineFile(new MemoryStream(recipe.Image)),
                                caption: recipe.Name
                            );
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "No recipes found."
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "An error occurred while retrieving recipes."
                    );
                }
            }
        }

        private async Task HandleGetRecipeByIdAsync(long chatId, int id)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync($"{recipeApiBaseUrl}/api/RecipeManagement/{id}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Recipe not found."
                        );
                        return;
                    }

                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var recipe = JsonConvert.DeserializeObject<Recipe>(jsonResponse);

                    await botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: new InputOnlineFile(new MemoryStream(recipe.Image)),
                        caption: recipe.Name
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "An error occurred while retrieving the recipe."
                    );
                }
            }
        }

        private async Task HandleCreateCommandAsync(long chatId, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Please provide a name for the recipe."
                );
                return;
            }

            createRecipeData[chatId] = (name, null);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Please send a photo for the recipe."
            );
        }

        private async Task HandleUpdateCommandAsync(long chatId, string parameter)
        {
            var updateParts = parameter?.Split(',');
            if (updateParts?.Length != 2 || !int.TryParse(updateParts[0], out int id))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Please provide a valid recipe ID and name in the format /update ID, NewName."
                );
                return;
            }

            var newName = updateParts[1].Trim();
            updateRecipeData[chatId] = (id, newName, null);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Please send a new photo for the recipe."
            );
        }

        private async Task HandleCreatePhotoMessageAsync(Telegram.Bot.Types.Message message)
        {
            var chatId = message.Chat.Id;

            if (createRecipeData.ContainsKey(chatId))
            {
                var fileId = message.Photo.Last().FileId;
                var file = await botClient.GetFileAsync(fileId);
                using (var stream = new MemoryStream())
                {
                    await botClient.DownloadFileAsync(file.FilePath, stream);
                    createRecipeData[chatId] = (createRecipeData[chatId].RecipeName, stream.ToArray());
                }

                var newRecipe = new Recipe
                {
                    Name = createRecipeData[chatId].RecipeName,
                    Image = createRecipeData[chatId].Image
                };

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        var jsonContent = JsonConvert.SerializeObject(newRecipe);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync($"{recipeApiBaseUrl}/api/RecipeManagement", content);
                        response.EnsureSuccessStatusCode();

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Recipe created successfully."
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "An error occurred while creating the recipe."
                        );
                    }
                }

                createRecipeData.Remove(chatId);
            }
        }

        private async Task HandleUpdatePhotoMessageAsync(Telegram.Bot.Types.Message message)
        {
            var chatId = message.Chat.Id;

            if (updateRecipeData.ContainsKey(chatId))
            {
                var fileId = message.Photo.Last().FileId;
                var file = await botClient.GetFileAsync(fileId);
                using (var stream = new MemoryStream())
                {
                    await botClient.DownloadFileAsync(file.FilePath, stream);
                    updateRecipeData[chatId] = (updateRecipeData[chatId].RecipeId, updateRecipeData[chatId].RecipeName, stream.ToArray());
                }

                var updatedRecipe = new Recipe
                {
                    Id = updateRecipeData[chatId].RecipeId,
                    Name = updateRecipeData[chatId].RecipeName,
                    Image = updateRecipeData[chatId].Image
                };

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        var jsonContent = JsonConvert.SerializeObject(updatedRecipe);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await client.PutAsync($"{recipeApiBaseUrl}/api/RecipeManagement/{updatedRecipe.Id}", content);
                        response.EnsureSuccessStatusCode();

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Recipe updated successfully."
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "An error occurred while updating the recipe."
                        );
                    }
                }

                updateRecipeData.Remove(chatId);
            }
        }

        private async Task HandleDeleteRecipeAsync(long chatId, int id)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.DeleteAsync($"{recipeApiBaseUrl}/api/RecipeManagement/{id}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Recipe not found."
                        );
                        return;
                    }

                    response.EnsureSuccessStatusCode();

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Recipe deleted successfully."
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "An error occurred while deleting the recipe."
                    );
                }
            }
        }

        private class Recipe
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public byte[] Image { get; set; }
        }
    }
}
