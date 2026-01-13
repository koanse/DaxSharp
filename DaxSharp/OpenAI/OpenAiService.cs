using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DaxSharp.Models;

namespace DaxSharp.OpenAI;

/// <summary>
/// Service for interacting with OpenAI API.
/// </summary>
internal static class OpenAiService
{
    /// <summary>
    /// Generates SQL query from prompt using OpenAI API.
    /// </summary>
    public static async Task<string> GenerateSqlFromPrompt(
        string prompt,
        string apiKey,
        string model,
        CancellationToken cancellationToken)
    {
        var config = DaxSharpConfig.Instance;
        var openAiConfig = config.OpenAi;
        
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = openAiConfig.Temperature,
            max_tokens = openAiConfig.MaxTokens
        };

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(
            openAiConfig.ApiUrl,
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<OpenAiResponse>(responseJson, jsonOptions);

        if (responseObj?.Choices == null || responseObj.Choices.Length == 0)
        {
            throw new InvalidOperationException("OpenAI API returned an empty response");
        }

        var sql = responseObj.Choices[0].Message?.Content?.Trim() ?? string.Empty;
        
        // Remove markdown code blocks if present
        if (sql.StartsWith("```sql", StringComparison.OrdinalIgnoreCase))
        {
            sql = sql[6..];
        }
        if (sql.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            sql = sql[3..];
        }
        if (sql.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            sql = sql[..^3];
        }
        
        return sql.Trim();
    }
}
