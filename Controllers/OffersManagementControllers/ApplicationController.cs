using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Back_HR.Models;
using Back_HR.DTOs;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Back_HR.Models.enums;
using System.IO;
using System.Text.Json;

namespace Back_HR.Controllers.OffersManagementControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationController : ControllerBase
    {
        private readonly HRContext _context;
        private readonly UserManager<User> _userManager;

        public ApplicationController(HRContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("apply")]
        [Authorize(Policy = "CandidatOnly")]
        public async Task<IActionResult> ApplyForJobOffer([FromForm] ApplicationDtoPost dto)
        {
            string json = JsonSerializer.Serialize(dto);
            Console.WriteLine($"Received DTO: {json}");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            string? cvPath = null;
            if (dto.Cv != null && dto.Cv.Length > 0)
            {
                // Ensure the file is a PDF
                if (dto.Cv.ContentType != "application/pdf")
                {
                    return BadRequest("Only PDF files are allowed for CV upload.");
                }

                // Define the storage path (e.g., wwwroot/uploads/cvs/)
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cvs");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate a unique file name to avoid overwriting
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Cv.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file to the server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Cv.CopyToAsync(fileStream);
                }

                // Store the relative path or URL in the database
                cvPath = $"/uploads/cvs/{uniqueFileName}";
            }

            // Get the authenticated user's email from the JWT
            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Unable to identify user.");
            }
            // Find the candidate user
            var candidate = await _userManager.FindByEmailAsync(email) as Candidat;
            if (candidate == null)
            {
                return Unauthorized("Candidate user not found.");
            }

            // Ensure the CandidatId matches the authenticated user
            if (dto.CandidatId != candidate.Id)
            {
                return Forbid("You can only apply for yourself.");
            }

            // Verify the job offer exists and is open
            var jobOffer = await _context.JobOffers
                .FirstOrDefaultAsync(jo => jo.Id == dto.JobOfferId);
            if (jobOffer == null)
            {
                return NotFound($"No job offer found with ID {dto.JobOfferId}.");
            }
            if (jobOffer.Status != OffreStatus.OPEN)
            {
                return BadRequest("This job offer is not open for applications.");
            }

            // Check if the candidate has already applied
            var existingApplication = await _context.Applications
                .AnyAsync(a => a.CandidateId == dto.CandidatId && a.JobOfferId == dto.JobOfferId);
            if (existingApplication)
            {
                return Conflict("You have already applied for this job offer.");
            }

            // Create the application
            var application = new Application
            {
                Id = Guid.NewGuid(),
                CandidateId = dto.CandidatId,
                JobOfferId = dto.JobOfferId,
                Cv = cvPath,
                Status = ApplicationStatus.PENDING,
                ApplicationDate = DateTime.Now
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Application submitted successfully", ApplicationId = application.Id });
        }

        [HttpGet("offer/{jobOfferId}/candidates")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> GetCandidatesAppliedToOffer(Guid jobOfferId)
        {
            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Unable to identify user.");
            }

            var rhUser = await _userManager.FindByEmailAsync(email) as RH;
            if (rhUser == null)
            {
                return Unauthorized("RH user not found.");
            }

            var jobOffer = await _context.JobOffers
                .FirstOrDefaultAsync(jo => jo.Id == jobOfferId);
            if (jobOffer == null)
            {
                return NotFound($"No job offer found with ID {jobOfferId}.");
            }
            if (jobOffer.RHId != rhUser.Id)
            {
                return Forbid("You can only view candidates for your own job offers.");
            }

            var applications = await _context.Applications
                .Where(a => a.JobOfferId == jobOfferId)
                .Include(a => a.Candidat)
                    .ThenInclude(c => c.Competences)
                .Select(a => new
                {
                    ApplicationId = a.Id,
                    Candidate = a.Candidat,
                    CvPath = a.Cv,
                    Status = a.Status.ToString(),
                    ApplicationDate = a.ApplicationDate
                })
                .ToListAsync();

            var result = applications.Select(app =>
            {
                var candidateDto = new CandidatDTO
                {
                    Id = app.Candidate.Id,
                    Lastname = app.Candidate.Lastname,
                    Firstname = app.Candidate.Firstname,
                    Telephone = app.Candidate.Telephone,
                    Email = app.Candidate.Email,
                    Competences = app.Candidate.Competences
                };

                if (string.IsNullOrEmpty(app.CvPath))
                {
                    return new ApplicationResponseDto
                    {
                        ApplicationId = app.ApplicationId,
                        Candidate = candidateDto,
                        ApplicationDate = app.ApplicationDate,
                        Status = app.Status.ToString(),
                        CvFile = null
                    };
                }

                var cvAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", app.CvPath.TrimStart('/'));

                if (!System.IO.File.Exists(cvAbsolutePath))
                {
                    return new ApplicationResponseDto
                    {
                        ApplicationId = app.ApplicationId,
                        Candidate = candidateDto,
                        ApplicationDate = app.ApplicationDate,
                        CvFile = null,
                        Status = app.Status.ToString(),
                        Message = "CV file not found on server."
                    };
                }

                var fileBytes = System.IO.File.ReadAllBytes(cvAbsolutePath);
                var fileName = Path.GetFileName(cvAbsolutePath);

                return new ApplicationResponseDto
                {
                    ApplicationId = app.ApplicationId,
                    Candidate = candidateDto,
                    ApplicationDate = app.ApplicationDate,
                    Status = app.Status.ToString(),
                    CvFile = new CvFileDto
                    {
                        FileName = fileName,
                        Content = Convert.ToBase64String(fileBytes),
                        ContentType = "application/pdf"
                    }
                };
            }).ToList();

            return Ok(result);
        }
        [HttpDelete("{applicationId}/cancel")]
        [Authorize(Policy = "CandidatOnly")]
        public async Task<IActionResult> CancelApplication(Guid applicationId)
        {
            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Unable to identify user.");
            }

            var candidate = await _userManager.FindByEmailAsync(email) as Candidat;
            if (candidate == null)
            {
                return Unauthorized("Candidate user not found.");
            }

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
            {
                return NotFound($"No application found with ID {applicationId}.");
            }

            if (application.CandidateId != candidate.Id)
            {
                return Forbid("You can only cancel your own applications.");
            }

            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Application canceled successfully", ApplicationId = applicationId });
        }

        [HttpPut("{applicationId}/accept")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> AcceptApplication(Guid applicationId)
        {
            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Unable to identify user.");
            }

            var rhUser = await _userManager.FindByEmailAsync(email) as RH;
            if (rhUser == null)
            {
                return Unauthorized("RH user not found.");
            }

            var application = await _context.Applications
                .Include(a => a.JobOffer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
            {
                return NotFound($"No application found with ID {applicationId}.");
            }

            if (application.JobOffer.RHId != rhUser.Id)
            {
                return Forbid("You can only accept applications for your own job offers.");
            }

            application.Status = ApplicationStatus.ACCEPTED;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Application accepted successfully", ApplicationId = applicationId });
        }

        [HttpPut("{applicationId}/reject")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> RejectApplication(Guid applicationId)
        {
            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Unable to identify user.");
            }

            var rhUser = await _userManager.FindByEmailAsync(email) as RH;
            if (rhUser == null)
            {
                return Unauthorized("RH user not found.");
            }

            var application = await _context.Applications
                .Include(a => a.JobOffer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
            {
                return NotFound($"No application found with ID {applicationId}.");
            }

            if (application.JobOffer.RHId != rhUser.Id)
            {
                return Forbid("You can only reject applications for your own job offers.");
            }

            application.Status = ApplicationStatus.REJECTED;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Application rejected successfully", ApplicationId = applicationId });
        }


        [HttpGet("my-applications")]
        [Authorize(Policy = "CandidatOnly")] // Assuming you have a policy for candidates
        public async Task<IActionResult> GetMyApplications()
        {
            // Get the authenticated user's email from JWT token
            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Unable to identify user.");
            }

            // Find the candidate user
            var candidateUser = await _userManager.FindByEmailAsync(email) as Candidat;
            if (candidateUser == null)
            {
                return Unauthorized("Candidate user not found.");
            }

            // Retrieve all applications for this candidate
            var applications = await _context.Applications
                .Where(a => a.Candidat.Id == candidateUser.Id)
                .Include(a => a.JobOffer)
                .Include(a => a.Candidat)
                    .ThenInclude(c => c.Competences)
                .Select(a => new
                {
                    ApplicationId = a.Id,
                    JobOffer = a.JobOffer,
                    CvPath = a.Cv,
                    Status = a.Status.ToString(),
                    ApplicationDate = a.ApplicationDate
                })
                .ToListAsync();

            // Transform the data into ApplicationResponseDto
            var result = applications.Select(app =>
            {
                // Create JobOffer DTO
                var jobOfferDto = new JobOfferDtoGet
                {
                    Id = app.JobOffer.Id,
                    Title = app.JobOffer.Title,
                    Description = app.JobOffer.Description,
                    Experience = app.JobOffer.Experience,
                    PublishDate = app.JobOffer.PublishDate,
                    Salary = app.JobOffer.Salary,
                    Location = app.JobOffer.Location,
                    Status = app.JobOffer.Status,
                    Competences = app.JobOffer.Competences
                };

                // Handle CV file
                if (string.IsNullOrEmpty(app.CvPath))
                {
                    return new ApplicationResponseDto
                    {
                        ApplicationId = app.ApplicationId,
                        JobOffer = jobOfferDto,
                        ApplicationDate = app.ApplicationDate,
                        Status = app.Status,
                        CvFile = null
                    };
                }

                var cvAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", app.CvPath.TrimStart('/'));

                if (!System.IO.File.Exists(cvAbsolutePath))
                {
                    return new ApplicationResponseDto
                    {
                        ApplicationId = app.ApplicationId,
                        JobOffer = jobOfferDto,
                        ApplicationDate = app.ApplicationDate,
                        Status = app.Status,
                        CvFile = null,
                        Message = "CV file not found on server."
                    };
                }

                var fileBytes = System.IO.File.ReadAllBytes(cvAbsolutePath);
                var fileName = Path.GetFileName(cvAbsolutePath);

                return new ApplicationResponseDto
                {
                    ApplicationId = app.ApplicationId,
                    JobOffer = jobOfferDto,
                    ApplicationDate = app.ApplicationDate,
                    Status = app.Status,
                    CvFile = new CvFileDto
                    {
                        FileName = fileName,
                        Content = Convert.ToBase64String(fileBytes),
                        ContentType = "application/pdf"
                    }
                };
            }).ToList();

            return Ok(result);
        }
    }
}