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
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;

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

        /* [HttpPost("apply")]
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
         }*/
        [HttpPost("apply")]
        [Authorize(Policy = "CandidatOnly")]
        public async Task<IActionResult> ApplyForJobOffer([FromForm] ApplicationDtoPost dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            Console.WriteLine($"Received DTO: {JsonSerializer.Serialize(dto)}");

            string? cvPath = null;
            List<Competence> extractedSkills = new List<Competence>();

            if (dto.Cv != null && dto.Cv.Length > 0)
            {
                if (dto.Cv.ContentType != "application/pdf")
                    return BadRequest("Only PDF files are allowed for CV upload.");

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cvs");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Cv.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Cv.CopyToAsync(fileStream);
                }
                cvPath = $"/uploads/cvs/{uniqueFileName}";

                // Extract text from CV using iText7
                try
                {
                    using (var pdfDocument = new PdfDocument(new PdfReader(filePath)))
                    {
                        var text = new StringBuilder();
                        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                        {
                            var page = pdfDocument.GetPage(i);
                            text.Append(PdfTextExtractor.GetTextFromPage(page));
                        }

                        // Insérez ici le code pour extraire les compétences
                        var cvText = text.ToString().ToLower(); // Convertir en minuscules pour une recherche insensible à la casse
                        var knownSkills = await _context.Competences.Select(c => c.Titre.ToLower()).ToListAsync(); // Récupérer les compétences connues
                        extractedSkills = knownSkills
                            .Where(skill => cvText.Contains(skill)) // Vérifier si le texte du CV contient la compétence
                            .Select(skill => new Competence { Titre = skill }) // Créer une nouvelle instance Competence
                            .ToList();

                        Console.WriteLine($"Extracted CV Text: {cvText}");
                        Console.WriteLine($"Extracted Skills: {JsonSerializer.Serialize(extractedSkills)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error extracting text from PDF: {ex.Message}");
                    // Optionally return a warning but proceed with application
                }
            }

            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized("Unable to identify user.");

            var candidate = await _userManager.FindByEmailAsync(email) as Candidat;
            if (candidate == null) return Unauthorized("Candidate user not found.");
            if (dto.CandidatId != candidate.Id) return BadRequest("You can only apply for yourself.");

            var jobOffer = await _context.JobOffers.FirstOrDefaultAsync(jo => jo.Id == dto.JobOfferId);
            if (jobOffer == null) return NotFound($"No job offer found with ID {dto.JobOfferId}.");
            if (jobOffer.Status != OffreStatus.OPEN) return BadRequest("This job offer is not open for applications.");

            var existingApplication = await _context.Applications
                .AnyAsync(a => a.CandidateId == dto.CandidatId && a.JobOfferId == dto.JobOfferId);
            if (existingApplication) return Conflict("You have already applied for this job offer.");

            // Mettre à jour les compétences du candidat avec celles extraites
            candidate.Competences = extractedSkills;
            _context.Users.Update(candidate); // Mettre à jour le candidat dans la base

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
            try
            {
                Console.WriteLine($"Starting GetCandidatesAppliedToOffer for jobOfferId: {jobOfferId}");

                var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                Console.WriteLine($"Extracted email from JWT: {email}");
                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized("Unable to identify user.");
                }

                var rhUser = await _userManager.FindByEmailAsync(email) as RH;
                Console.WriteLine($"Found RH user: {(rhUser != null ? rhUser.Id : "null")}");
                if (rhUser == null)
                {
                    return Unauthorized("RH user not found.");
                }

                var jobOffer = await _context.JobOffers
                    .Include(jo => jo.Competences)
                    .FirstOrDefaultAsync(jo => jo.Id == jobOfferId);
                Console.WriteLine($"Found job offer: {(jobOffer != null ? jobOffer.Id : "null")}");
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
                    .ToListAsync();
                Console.WriteLine($"Found {applications?.Count ?? 0} applications for job offer {jobOfferId}");

                if (applications == null || !applications.Any())
                {
                    return Ok(new
                    {
                        Candidates = new List<ApplicationResponseDto>(),
                        PercentageQualified = 0.0,
                        MostQualifiedCandidate = (object)null
                    });
                }

                // Calculate adequacy score for each application
                foreach (var application in applications)
                {
                    if (application.Candidat == null)
                    {
                        Console.WriteLine($"Warning: Application {application.Id} has no associated candidate.");
                        continue;
                    }

                    // Use Distinct() to remove duplicates in skills
                    var candidateSkills = application.Candidat.Competences?.Select(c => c.Titre?.ToLower()).Distinct().ToList() ?? new List<string>();
                    var requiredSkills = jobOffer.Competences?.Select(c => c.Titre?.ToLower()).Distinct().ToList() ?? new List<string>();

                    // Log the skills for debugging
                    Console.WriteLine($"Candidate {application.Candidat.Email} skills: {string.Join(", ", candidateSkills)}");
                    Console.WriteLine($"Required skills: {string.Join(", ", requiredSkills)}");

                    var matchingSkillsCount = candidateSkills.Count(skill => requiredSkills.Contains(skill));
                    var totalRequiredSkills = requiredSkills.Count;

                    Console.WriteLine($"Matching skills: {matchingSkillsCount}, Total required: {totalRequiredSkills}");

                    application.AdequacyScore = totalRequiredSkills > 0
                        ? (double)matchingSkillsCount / totalRequiredSkills * 100
                        : 0;

                    Console.WriteLine($"AdequacyScore for {application.Candidat.Email}: {application.AdequacyScore}");

                    _context.Applications.Update(application);
                }

                Console.WriteLine("Saving changes to database...");
                await _context.SaveChangesAsync();
                Console.WriteLine("Changes saved successfully.");

                const double qualificationThreshold = 50.0;
                var totalCandidates = applications.Count;
                var qualifiedCandidates = applications.Count(a => a.AdequacyScore >= qualificationThreshold);
                var percentageQualified = totalCandidates > 0
                    ? (double)qualifiedCandidates / totalCandidates * 100
                    : 0;

                Console.WriteLine($"Total candidates: {totalCandidates}, Qualified candidates: {qualifiedCandidates}, PercentageQualified: {percentageQualified}");

                var mostQualifiedApplication = applications
                    .Where(a => a.Candidat != null)
                    .OrderByDescending(a => a.AdequacyScore)
                    .FirstOrDefault();

                var result = applications
                    .Where(a => a.Candidat != null)
                    .Select(app =>
                    {
                        var candidateDto = new CandidatDTO
                        {
                            Id = app.Candidat.Id,
                            Lastname = app.Candidat.Lastname ?? string.Empty,
                            Firstname = app.Candidat.Firstname ?? string.Empty,
                            Telephone = app.Candidat.Telephone ?? string.Empty,
                            Email = app.Candidat.Email ?? string.Empty,
                            Competences = app.Candidat.Competences ?? new List<Competence>()
                        };

                        // Map the JobOffer to JobOfferDtoGet
                        var jobOfferDto = new JobOfferDtoGet
                        {
                            Id = jobOffer.Id,
                            Title = jobOffer.Title,
                            Description = jobOffer.Description,
                            Experience = jobOffer.Experience,
                            PublishDate = jobOffer.PublishDate,
                            Salary = jobOffer.Salary,
                            Location = jobOffer.Location,
                            Status = jobOffer.Status,
                            Competences = jobOffer.Competences
                        };

                        if (string.IsNullOrEmpty(app.Cv))
                        {
                            return new ApplicationResponseDto
                            {
                                ApplicationId = app.Id,
                                Candidate = candidateDto,
                                JobOffer = jobOfferDto, // Include JobOffer
                                ApplicationDate = app.ApplicationDate,
                                Status = app.Status.ToString(),
                                CvFile = null,
                                AdequacyScore = app.AdequacyScore ?? 0
                            };
                        }

                        var cvAbsolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", app.Cv.TrimStart('/'));

                        if (!System.IO.File.Exists(cvAbsolutePath))
                        {
                            return new ApplicationResponseDto
                            {
                                ApplicationId = app.Id,
                                Candidate = candidateDto,
                                JobOffer = jobOfferDto, // Include JobOffer
                                ApplicationDate = app.ApplicationDate,
                                CvFile = null,
                                Status = app.Status.ToString(),
                                Message = "CV file not found on server.",
                                AdequacyScore = app.AdequacyScore ?? 0
                            };
                        }

                        var fileBytes = System.IO.File.ReadAllBytes(cvAbsolutePath);
                        var fileName = Path.GetFileName(cvAbsolutePath);

                        return new ApplicationResponseDto
                        {
                            ApplicationId = app.Id,
                            Candidate = candidateDto,
                            JobOffer = jobOfferDto, // Include JobOffer
                            ApplicationDate = app.ApplicationDate,
                            Status = app.Status.ToString(),
                            CvFile = new CvFileDto
                            {
                                FileName = fileName,
                                Content = Convert.ToBase64String(fileBytes),
                                ContentType = "application/pdf"
                            },
                            AdequacyScore = app.AdequacyScore ?? 0
                        };
                    }).ToList();

                var response = new
                {
                    Candidates = result,
                    PercentageQualified = percentageQualified,
                    MostQualifiedCandidate = mostQualifiedApplication != null
                        ? new
                        {
                            Name = $"{mostQualifiedApplication.Candidat.Firstname ?? "Unknown"} {mostQualifiedApplication.Candidat.Lastname ?? "Candidate"}",
                            Email = mostQualifiedApplication.Candidat.Email ?? "N/A",
                            AdequacyScore = mostQualifiedApplication.AdequacyScore ?? 0
                        }
                        : null
                };

                Console.WriteLine("Returning response...");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCandidatesAppliedToOffer: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "An error occurred while fetching candidates.", Details = ex.Message });
            }
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