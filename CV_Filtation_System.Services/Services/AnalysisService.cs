using CV_Filtation_System.Core.Results;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CV_Filtation_System.Services.Services
{
    public class AnalysisService : IAnalysisService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AnalysisService(IHttpClientFactory httpClientFactory)
         => _httpClientFactory = httpClientFactory;
        public async Task<ExternalAnalysisResult> GetResumeAnalysis(byte[] cvBytes, string fileName, string jobDescription)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            // Add resume file
            var fileContent = new ByteArrayContent(cvBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "resume", fileName);

            // Add job description
            content.Add(new StringContent(jobDescription), "job_desc");

            // Call external API
            var response = await httpClient.PostAsync("https://c7a8-154-182-5-213.ngrok-free.app/analyze", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ExternalAnalysisResult>();
        }
        public async Task<PercentageMatchResult> GetPercentageAnalysis(byte[] cvBytes, string fileName, string jobDescription)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            // Add resume file
            var fileContent = new ByteArrayContent(cvBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "resume", fileName);

            // Add job description
            content.Add(new StringContent(jobDescription), "job_desc");

            // Call external API
            var response = await httpClient.PostAsync("https://c7a8-154-182-5-213.ngrok-free.app/percentage_match", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PercentageMatchResult>();
        }
        public async Task<ExternalAnalysisResult> GetResumeSkillImprove(byte[] cvBytes, string fileName, string jobDescription)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            // Add resume file
            var fileContent = new ByteArrayContent(cvBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "resume", fileName);

            // Add job description
            content.Add(new StringContent(jobDescription), "job_desc");

            // Call external API
            var response = await httpClient.PostAsync("https://c7a8-154-182-5-213.ngrok-free.app/skill_improve", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ExternalAnalysisResult>();
        }

        public async Task<JobRecommandResult> GetExpectedPosition(byte[] cvBytes, string fileName)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(cvBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "resume", fileName);

            // Add job description
            content.Add(new StringContent("Software Developer job description"), "job_desc");

            var response = await httpClient.PostAsync("https://c7a8-154-182-5-213.ngrok-free.app/recommend_job", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<JobRecommandResult>();
        }

        public async Task<ExternalSimiratyScoreResult> GetSimilartyScoremprove(byte[] cvBytes, string fileName, string jobDescription)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            // Convert byte[] to StreamContent for better file handling
            var stream = new MemoryStream(cvBytes);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            // Add the resume file with the proper name
            content.Add(fileContent, "file", fileName);

            // Add job description
            content.Add(new StringContent(jobDescription), "jd");

            // Updated URL with trailing slash
            var response = await httpClient.PostAsync("https://442d-156-195-177-8.ngrok-free.app/calculate_similarity/", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Response Status: {response.StatusCode}");
            Console.WriteLine($"API Response Content: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"API Error: {response.StatusCode} - {responseContent}");
                return null;
            }

            try
            {
                var result = await response.Content.ReadFromJsonAsync<ExternalSimiratyScoreResult>();
                Console.WriteLine($"Parsed Result: {result?.similarity_score ?? "null"}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                return null;
            }
        }
    }
}
