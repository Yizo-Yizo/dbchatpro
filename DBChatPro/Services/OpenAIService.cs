using Azure.AI.OpenAI;
using Azure;
using System.Text;
using OpenAI.Chat;

namespace DBChatPro.Services
{
    public class OpenAIService
    {
        public static async Task<AIQuery> GetAISQLQuery(string userPrompt, AIConnection aiConnection)
        {
            // Configure OpenAI client
            string openAIEndpoint = "your-openai-endpoint";
            string openAIKey = "your-openai-key";
            string openAIDeploymentName = "your-model-deployment-name";

            AzureOpenAIClient aiClient = new(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));
            ChatClient chatClient = aiClient.GetChatClient("wolfo");
            
            List<ChatMessage> chatHistory = new List<ChatMessage>();
            var builder = new StringBuilder();

            builder.AppendLine("Your are a helpful, cheerful database assistant. Do not respond with any information unrelated to databases or queries. Use the following database schema when creating your answers:");

            foreach(var table in aiConnection.SchemaRaw)
            {
                builder.AppendLine(table);
            }

            builder.AppendLine("Include column name headers in the query results.");
            builder.AppendLine("Always provide your answer in the JSON format below:");
            builder.AppendLine(@"{ ""summary"": ""your-summary"", ""query"":  ""your-query"" }");
            builder.AppendLine("Output ONLY JSON formatted on a single line. Do not use new line characters.");
            builder.AppendLine(@"In the preceding JSON response, substitute ""your-query"" with Microsoft SQL Server Query to retrieve the requested data.");
            builder.AppendLine(@"In the preceding JSON response, substitute ""your-summary"" with an explanation of each step you took to create this query in a detailed paragraph.");
            builder.AppendLine("Do not use MySQL syntax.");
            builder.AppendLine("Always limit the SQL Query to 100 rows.");
            builder.AppendLine("Always include all of the table columns and details.");

            // Build the AI chat/prompts
            chatHistory.Add(new SystemChatMessage(builder.ToString()));
            chatHistory.Add(new UserChatMessage(userPrompt));

            // Send request to Azure OpenAI model
            var response = await chatClient.CompleteChatAsync(chatHistory);
            var responseContent = response.Value.Content[0].Text.Replace("```json", "").Replace("```", "").Replace("\\n", "");

            try
            {
                return JsonSerializer.Deserialize<AIQuery>(responseContent);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse AI response as a SQL Query. The AI response was: " + response.Value.Content[0].Text);
            }
        }
    }
}
