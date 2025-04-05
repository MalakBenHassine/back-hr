using Back_HR.Services;
using Microsoft.AspNetCore.Mvc;

namespace Back_HR.Controllers.chatbot
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly OllamaService _ollama;

        public ChatController(OllamaService ollama)
        {
            _ollama = ollama;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            try
            {
                // Add your context/data to the prompt
                string fullPrompt = $"""
                Use the following context to answer the question.
                Context: {GetRelevantContext(request.Message)}
                Question: {request.Message}
                Answer: 
                """;

                var response = await _ollama.GetResponseAsync(fullPrompt);
                return Ok(new { reply = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private string GetRelevantContext(string query)
        {
            // Implement your context retrieval logic here
            // This could be from a local file, database, etc.
            return "Your custom data goes here...";
        }
    }

    public record ChatRequest(string Message);
}
