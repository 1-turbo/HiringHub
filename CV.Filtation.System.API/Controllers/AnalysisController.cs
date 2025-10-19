using CV.Filtation.System.API.Controllers;
using CV.Filtation.System.API.DTO;
using CV_Filtation_System.Core.Entities;
using CV_Filtation_System.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly IJobPostingService _jobPostingService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<JobPostingsController> _logger;


    public AnalysisController(IAnalysisService analysisService, IJobPostingService jobPostingService, IHttpClientFactory httpClientFactory, IWebHostEnvironment hostingEnvironment, UserManager<User> userManager, AppDbContext context, ILogger<JobPostingsController> logger)
    {
        _analysisService = analysisService;
        _jobPostingService = jobPostingService;
        _httpClientFactory = httpClientFactory;
        _hostingEnvironment = hostingEnvironment;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AnalysisResultDto>> AnalyzeResume(string userId, int jobId)
    {
        try
        {
            // 1. Validate user and CV
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            if (string.IsNullOrEmpty(user.CV_FilePath))
                return BadRequest("User has no CV uploaded");

            var webRootPath = _hostingEnvironment.WebRootPath;
            var cvPath = Path.Combine(webRootPath, "CVs", Path.GetFileName(user.CV_FilePath));

            if (!System.IO.File.Exists(cvPath))
                return NotFound("CV file not found in storage");

            // 2. Get job description from database
            var job = await _jobPostingService.GetJobPostingByIdAsync(jobId);
            if (job == null) return NotFound("Job not found");

            if (string.IsNullOrWhiteSpace(job.Description))
                return BadRequest("Job has no description");

            // 3. Prepare data for analysis
            var cvBytes = await System.IO.File.ReadAllBytesAsync(cvPath);
            var fileName = Path.GetFileName(user.CV_FilePath);

            // 4. Call external analysis service
            var analysisResult = await _analysisService.GetResumeAnalysis(cvBytes, fileName, job.Description);

            if (analysisResult == null || string.IsNullOrWhiteSpace(analysisResult.Suggestions))
                return NotFound("Could not analyze resume");

            // 5. Return results
            return Ok(new AnalysisResultDto
            {
                Suggestions = analysisResult.Suggestions,
                AnalysisDate = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing resume for user {UserId} and job {JobId}", userId, jobId);
            return StatusCode(500, "Error analyzing resume");
        }
    }
    [HttpPost("skill_improve")]
    public async Task<ActionResult<AnalysisResultDto>> Skill_Improve(string userId, int jobId)
    {
        try
        {
            // 1. Validate user and CV
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            if (string.IsNullOrEmpty(user.CV_FilePath))
                return BadRequest("User has no CV uploaded");

            var webRootPath = _hostingEnvironment.WebRootPath;
            var cvPath = Path.Combine(webRootPath, "CVs", Path.GetFileName(user.CV_FilePath));

            if (!System.IO.File.Exists(cvPath))
                return NotFound("CV file not found in storage");

            // 2. Get job description from database
            var job = await _jobPostingService.GetJobPostingByIdAsync(jobId);
            if (job == null) return NotFound("Job not found");

            if (string.IsNullOrWhiteSpace(job.Description))
                return BadRequest("Job has no description");

            // 3. Prepare data for analysis
            var cvBytes = await System.IO.File.ReadAllBytesAsync(cvPath);
            var fileName = Path.GetFileName(user.CV_FilePath);

            // 4. Call external analysis service
            var analysisResult = await _analysisService.GetResumeSkillImprove(cvBytes, fileName, job.Description);

            if (analysisResult == null || string.IsNullOrWhiteSpace(analysisResult.Suggestions))
                return NotFound("Could not analyze resume");

            // 5. Return results
            return Ok(new AnalysisResultDto
            {
                Suggestions = analysisResult.Suggestions,
                AnalysisDate = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing resume for user {UserId} and job {JobId}", userId, jobId);
            return StatusCode(500, "Error analyzing resume");
        }
    }
    [HttpPost("percentage_match")]
    public async Task<ActionResult<AnalysisResultDto>> Percentage_Match(string userId, int jobId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            if (string.IsNullOrEmpty(user.CV_FilePath))
                return BadRequest("User has no CV uploaded");

            var webRootPath = _hostingEnvironment.WebRootPath;
            var cvPath = Path.Combine(webRootPath, "CVs", Path.GetFileName(user.CV_FilePath));

            if (!System.IO.File.Exists(cvPath))
                return NotFound("CV file not found in storage");

            var job = await _jobPostingService.GetJobPostingByIdAsync(jobId);
            if (job == null) return NotFound("Job not found");

            if (string.IsNullOrWhiteSpace(job.Description))
                return BadRequest("Job has no description");

            var cvBytes = await System.IO.File.ReadAllBytesAsync(cvPath);
            var fileName = Path.GetFileName(user.CV_FilePath);

            // 4. Call external analysis service
            var analysisResult = await _analysisService.GetPercentageAnalysis(cvBytes, fileName, job.Description);

            if (analysisResult == null || string.IsNullOrWhiteSpace(analysisResult.match_percentage))
                return NotFound("Could not analyze resume");

            // 5. Return results
            return Ok(new AnalysisResultDto
            {
                Suggestions = analysisResult.match_percentage,
                AnalysisDate = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing resume for user {UserId} and job {JobId}", userId, jobId);
            return StatusCode(500, "Error analyzing resume");
        }
    }


}

