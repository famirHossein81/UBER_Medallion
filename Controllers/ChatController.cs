using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using crud.Data;
using crud.DTOs;
using System.Text;
using System.Text.Json;
using Npgsql;
using System.Data;


namespace crud.Controllers
{
    [ApiController]
    [Route("api/{Controller}")]

    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponseDto>> AskQuestion([FromBody] ChatRequestDto request)
        {
            string apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            string model = Environment.GetEnvironmentVariable("AI_MODEL") ?? "llama3-8b-8192";

            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(500, new { error = "Server Configuration Error: API Key missing." });
            }

            string sqlQuery = await GetSqlFromAi(request.Question, apiKey, model);
            if (sqlQuery.StartsWith("ERROR:"))
            {
                return BadRequest(new ChatResponseDto { Error = sqlQuery });
            }

            sqlQuery = SanitizeSql(sqlQuery);
            if (string.IsNullOrEmpty(sqlQuery))
            {
                return BadRequest(new ChatResponseDto { Error = "Unsafe or invalid query generated." });
            }

            try
            {
                var result = ExecuteDynamicQuery(sqlQuery);
                return Ok(new ChatResponseDto
                {
                    GeneratedSql = sqlQuery,
                    Answer = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ChatResponseDto
                {
                    GeneratedSql = sqlQuery,
                    Error = $"Execution Failed: {ex.Message}"
                });
            }
        }

        private async Task<string> GetSqlFromAi(string userQuestion, string apiKey, string model)
        {
            var schemaDef = @"
    Table: gold.cleaned_dataset
    Columns:
    - booking_status (VARCHAR) values: 'Completed', 'Cancelled by Customer', 'No Driver Found', 'Cancelled by Driver', 'Incomplete'
    - vehicle_type (VARCHAR) values: 'Auto', 'Bike', 'eBike', 'Go Mini', 'Go Sedan', 'Premier Sedan', 'Uber XL'
    - booking_value (DECIMAL) -- The revenue or cost of the trip
    - ride_distance (DECIMAL) -- Distance in km
    - booking_date (DATE)
    - booking_time (TIME)
    - customer_cancel_reason (VARCHAR) -- Reason if customer cancelled
    - driver_cancel_reason (VARCHAR)   -- Reason if driver cancelled
    - unified_cancellation_reason (VARCHAR) -- The text reason why a trip failed (Main column for reasons)
    - revenue_per_km (DECIMAL) -- Pre-calculated revenue per km. Use this instead of dividing.
    ";

            var payload = new
            {
                model = model,
                messages = new[]
                {
            new {
                role = "system",
                content = $"You are a PostgreSQL expert. Convert the user's question into a SQL query for the table 'gold.cleaned_dataset'. \n" +
                          $"Schema: {schemaDef}\n" +
                          $"RULES:\n" +
                          $"1. Return ONLY the SQL string. No markdown, no explanations.\n" +
                          $"2. Do NOT use markdown ```sql tags.\n" +
                          $"3. If the question is about REVENUE, DURATION, or RATINGS, add condition: WHERE booking_status = 'Completed'.\n" +
                          $"4. If the question is about CANCELLATIONS, add condition: WHERE booking_status != 'Completed'.\n" +
                          $"5. If the question is unrelated to data, return 'ERROR: Irrelevant'.\n" +
                          $"6. If the user asks for a list of text values (like reasons or types), always use 'SELECT DISTINCT'."+
                          $"7. When dividing by ride_distance, ALWAYS use NULLIF(ride_distance, 0) to avoid division by zero errors.\n"
                          
            },
            new { role = "user", content = userQuestion }
        },
                temperature = 0
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
                var resString = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(resString))
                {
                    if (doc.RootElement.TryGetProperty("error", out var error))
                        return "ERROR: AI Service Error - " + error.GetProperty("message").GetString();

                    var sql = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    return sql.Trim().Replace("```sql", "").Replace("```", "").Replace("\n", " ");
                }
            }
        }
        private string SanitizeSql(string sql)
        {
            sql = sql.Trim().Replace("```sql", "").Replace("```", "");

            if (sql.EndsWith(";"))
            {
                sql = sql.Substring(0, sql.Length - 1);
            }

            var upperSql = sql.ToUpper();

            if (upperSql.Contains("DROP ") ||
                upperSql.Contains("DELETE ") ||
                upperSql.Contains("UPDATE ") ||
                upperSql.Contains("INSERT ") ||
                upperSql.Contains("ALTER ") ||
                upperSql.Contains("TRUNCATE "))
            {
                return null;
            }

            if (!upperSql.Contains("LIMIT") && upperSql.Contains("SELECT"))
            {
                sql += " LIMIT 10";
            }

            return sql;
        }

        private List<Dictionary<string, object>> ExecuteDynamicQuery(string sql)
        {
            var results = new List<Dictionary<string, object>>();

            var connectionString = _context.Database.GetDbConnection().ConnectionString;

            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.GetName(i), reader.GetValue(i));
                            }
                            results.Add(row);
                        }
                    }
                }
            }
            return results;
        }


    }
}