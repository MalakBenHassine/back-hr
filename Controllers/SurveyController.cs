using Back_HR.Models;
using Back_HR.Models.Dtos;
using Back_HR.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Back_HR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly HRContext _context;

        public SurveyController(HRContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> CreateSurvey([FromBody] SurveyCreateDto surveyDto)
        {
            try
            {
                if (surveyDto == null || !surveyDto.Questions.Any())
                {
                    return BadRequest(new { message = "Le sondage doit contenir au moins une question." });
                }

                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.UserType != UserType.RH)
                {
                    return Unauthorized(new { message = "Seuls les utilisateurs de type RH peuvent créer des sondages." });
                }

                Console.WriteLine($"Utilisateur récupéré: Id={user.Id}, Firstname={user.Firstname}, Lastname={user.Lastname}");

                var survey = new Survey
                {
                    Id = Guid.NewGuid(),
                    Title = surveyDto.Title,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    Questions = surveyDto.Questions.Select(q => new SurveyQuestion
                    {
                        Id = Guid.NewGuid(),
                        Type = q.Type,
                        Text = q.Text,
                        Options = q.Options != null ? q.Options.Split(',').ToList() : null,
                        Required = q.Required
                    }).ToList()
                };

                _context.Surveys.Add(survey);
                await _context.SaveChangesAsync();

                var creatorName = (!string.IsNullOrEmpty(user.Firstname) && !string.IsNullOrEmpty(user.Lastname))
                    ? $"{user.Firstname} {user.Lastname}"
                    : (!string.IsNullOrEmpty(user.Firstname) ? user.Firstname
                        : (!string.IsNullOrEmpty(user.Lastname) ? user.Lastname
                            : "Utilisateur RH"));
                Console.WriteLine($"CreatorName défini dans CreateSurvey: {creatorName}");

                var createdSurveyDto = new SurveyDto
                {
                    Id = survey.Id.ToString(),
                    Title = survey.Title,
                    Questions = survey.Questions.Select(q => new SurveyQuestionDto
                    {
                        Id = q.Id,
                        Type = q.Type,
                        Text = q.Text,
                        Options = q.Options,
                        Required = q.Required
                    }).ToList(),
                    CreatedAt = survey.CreatedAt.ToString("o"),
                    CreatedBy = survey.CreatedBy.ToString(),
                    CreatorName = creatorName
                };

                Console.WriteLine($"Réponse de CreateSurvey: {System.Text.Json.JsonSerializer.Serialize(createdSurveyDto)}");

                return CreatedAtAction(nameof(GetSurveyById), new { id = survey.Id }, createdSurveyDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans CreateSurvey: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la création du sondage.", details = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllSurveys()
        {
            try
            {
                var surveys = await _context.Surveys
                    .Include(s => s.Questions)
                    .ToListAsync();

                var surveyDtos = new List<SurveyDto>();
                foreach (var survey in surveys)
                {
                    var creator = await _context.Users.FindAsync(survey.CreatedBy);

                    var creatorName = creator != null
                        ? (!string.IsNullOrEmpty(creator.Firstname) && !string.IsNullOrEmpty(creator.Lastname))
                            ? $"{creator.Firstname} {creator.Lastname}"
                            : (!string.IsNullOrEmpty(creator.Firstname) ? creator.Firstname
                                : (!string.IsNullOrEmpty(creator.Lastname) ? creator.Lastname
                                    : "Utilisateur inconnu"))
                        : "Créateur inconnu";

                    surveyDtos.Add(new SurveyDto
                    {
                        Id = survey.Id.ToString(),
                        Title = survey.Title,
                        Questions = survey.Questions.Select(q => new SurveyQuestionDto
                        {
                            Id = q.Id,
                            Type = q.Type,
                            Text = q.Text,
                            Options = q.Options,
                            Required = q.Required
                        }).ToList(),
                        CreatedAt = survey.CreatedAt.ToString("o"),
                        CreatedBy = survey.CreatedBy.ToString(),
                        CreatorName = creatorName
                    });
                }

                return Ok(surveyDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetAllSurveys: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la récupération des sondages.", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetSurveyById(Guid id)
        {
            try
            {
                var survey = await _context.Surveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (survey == null)
                {
                    return NotFound(new { message = "Sondage non trouvé." });
                }

                var creator = await _context.Users.FindAsync(survey.CreatedBy);

                var creatorName = creator != null
                    ? (!string.IsNullOrEmpty(creator.Firstname) && !string.IsNullOrEmpty(creator.Lastname))
                        ? $"{creator.Firstname} {creator.Lastname}"
                        : (!string.IsNullOrEmpty(creator.Firstname) ? creator.Firstname
                            : (!string.IsNullOrEmpty(creator.Lastname) ? creator.Lastname
                                : "Utilisateur inconnu"))
                    : "Créateur inconnu";

                var surveyDto = new SurveyDto
                {
                    Id = survey.Id.ToString(),
                    Title = survey.Title,
                    Questions = survey.Questions.Select(q => new SurveyQuestionDto
                    {
                        Id = q.Id,
                        Type = q.Type,
                        Text = q.Text,
                        Options = q.Options,
                        Required = q.Required
                    }).ToList(),
                    CreatedAt = survey.CreatedAt.ToString("o"),
                    CreatedBy = survey.CreatedBy.ToString(),
                    CreatorName = creatorName
                };

                return Ok(surveyDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetSurveyById: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la récupération du sondage.", details = ex.Message });
            }
        }

        [HttpGet("GetSurveysByRH")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> GetSurveysByRH()
        {
            try
            {
                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "Utilisateur non trouvé." });
                }

                Console.WriteLine($"Utilisateur authentifié: Id={user.Id}, Firstname={user.Firstname}, Lastname={user.Lastname}");

                var surveys = await _context.Surveys
                    .Include(s => s.Questions)
                    .Where(s => s.CreatedBy == userId)
                    .ToListAsync();

                var creatorName = (!string.IsNullOrEmpty(user.Firstname) && !string.IsNullOrEmpty(user.Lastname))
                    ? $"{user.Firstname} {user.Lastname}"
                    : (!string.IsNullOrEmpty(user.Firstname) ? user.Firstname
                        : (!string.IsNullOrEmpty(user.Lastname) ? user.Lastname
                            : "Utilisateur RH"));

                Console.WriteLine($"CreatorName défini dans GetSurveysByRH: {creatorName}");

                var surveyDtos = surveys.Select(survey => new SurveyDto
                {
                    Id = survey.Id.ToString(),
                    Title = survey.Title,
                    Questions = survey.Questions.Select(q => new SurveyQuestionDto
                    {
                        Id = q.Id,
                        Type = q.Type,
                        Text = q.Text,
                        Options = q.Options,
                        Required = q.Required
                    }).ToList(),
                    CreatedAt = survey.CreatedAt.ToString("o"),
                    CreatedBy = survey.CreatedBy.ToString(),
                    CreatorName = creatorName
                }).ToList();

                Console.WriteLine($"Réponse de GetSurveysByRH: {System.Text.Json.JsonSerializer.Serialize(surveyDtos)}");

                return Ok(surveyDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetSurveysByRH: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la récupération des sondages.", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> UpdateSurvey(Guid id, [FromBody] SurveyCreateDto surveyDto)
        {
            try
            {
                if (surveyDto == null || !surveyDto.Questions.Any())
                {
                    return BadRequest(new { message = "Le sondage doit contenir au moins une question." });
                }

                Console.WriteLine($"Données reçues dans UpdateSurvey: Title={surveyDto.Title}, Questions={System.Text.Json.JsonSerializer.Serialize(surveyDto.Questions)}");

                foreach (var question in surveyDto.Questions)
                {
                    if (string.IsNullOrEmpty(question.Type) || !new[] { "text", "radio", "checkbox", "rating" }.Contains(question.Type))
                    {
                        return BadRequest(new { message = $"Type de question invalide: {question.Type}. Les types valides sont: text, radio, checkbox, rating." });
                    }
                    if (string.IsNullOrEmpty(question.Text))
                    {
                        return BadRequest(new { message = "Le texte de la question est requis." });
                    }
                }

                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.UserType != UserType.RH)
                {
                    return Unauthorized(new { message = "Seuls les utilisateurs de type RH peuvent modifier des sondages." });
                }

                var survey = await _context.Surveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (survey == null)
                {
                    return NotFound(new { message = "Sondage non trouvé." });
                }

                if (survey.CreatedBy != userId)
                {
                    return Forbid("Vous n'êtes pas autorisé à modifier ce sondage.");
                }

                survey.Title = surveyDto.Title;

                if (survey.Questions.Any())
                {
                    _context.SurveyQuestions.RemoveRange(survey.Questions);
                    await _context.SaveChangesAsync();
                }

                var newQuestions = surveyDto.Questions.Select(q => new SurveyQuestion
                {
                    Id = Guid.NewGuid(),
                    SurveyId = survey.Id,
                    Type = q.Type,
                    Text = q.Text,
                    Options = q.Options != null ? q.Options.Split(',').ToList() : null,
                    Required = q.Required
                }).ToList();

                _context.SurveyQuestions.AddRange(newQuestions);

                await _context.SaveChangesAsync();

                var creatorName = (!string.IsNullOrEmpty(user.Firstname) && !string.IsNullOrEmpty(user.Lastname))
                    ? $"{user.Firstname} {user.Lastname}"
                    : (!string.IsNullOrEmpty(user.Firstname) ? user.Firstname
                        : (!string.IsNullOrEmpty(user.Lastname) ? user.Lastname
                            : "Utilisateur RH"));

                var updatedSurveyDto = new SurveyDto
                {
                    Id = survey.Id.ToString(),
                    Title = survey.Title,
                    Questions = newQuestions.Select(q => new SurveyQuestionDto
                    {
                        Id = q.Id,
                        Type = q.Type,
                        Text = q.Text,
                        Options = q.Options,
                        Required = q.Required
                    }).ToList(),
                    CreatedAt = survey.CreatedAt.ToString("o"),
                    CreatedBy = survey.CreatedBy.ToString(),
                    CreatorName = creatorName
                };

                return Ok(updatedSurveyDto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"Erreur de concurrence dans UpdateSurvey: {ex.Message}");
                return StatusCode(409, new { message = "Le sondage a été modifié ou supprimé par un autre utilisateur. Veuillez recharger la page et réessayer." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans UpdateSurvey: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la mise à jour du sondage.", details = ex.Message });
            }
        }

        [HttpPost("respond")]
        [Authorize(Policy = "EmployeeOnly")]
        public async Task<IActionResult> SubmitSurveyResponse([FromBody] List<SubmitSurveyResponseDto> responsesDto)
        {
            try
            {
                if (responsesDto == null || !responsesDto.Any())
                {
                    return BadRequest(new { message = "Aucune réponse fournie." });
                }

                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var employeeId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }

                var employee = await _context.Users.OfType<Employe>().FirstOrDefaultAsync(e => e.Id == employeeId);
                if (employee == null)
                {
                    return Unauthorized(new { message = "Employé non trouvé." });
                }

                var surveyId = responsesDto.First().SurveyId;
                var survey = await _context.Surveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == surveyId);

                if (survey == null)
                {
                    return NotFound(new { message = "Sondage non trouvé." });
                }

                var existingResponses = await _context.SurveyResponses
                    .AnyAsync(sr => sr.SurveyId == surveyId && sr.EmployeeId == employeeId);
                if (existingResponses)
                {
                    return BadRequest(new { message = "Vous avez déjà répondu à ce sondage." });
                }

                var responses = new List<SurveyResponse>();
                foreach (var responseDto in responsesDto)
                {
                    if (responseDto.SurveyId != surveyId)
                    {
                        return BadRequest(new { message = "Toutes les réponses doivent appartenir au même sondage." });
                    }

                    var question = survey.Questions.FirstOrDefault(q => q.Id == responseDto.QuestionId);
                    if (question == null)
                    {
                        return BadRequest(new { message = $"Question avec ID {responseDto.QuestionId} non trouvée." });
                    }

                    if (question.Required && string.IsNullOrEmpty(responseDto.Answer))
                    {
                        return BadRequest(new { message = $"La question '{question.Text}' est obligatoire." });
                    }

                    responses.Add(new SurveyResponse
                    {
                        SurveyId = surveyId,
                        EmployeeId = employeeId,
                        QuestionId = responseDto.QuestionId,
                        Answer = responseDto.Answer,
                        RespondedAt = DateTime.UtcNow
                    });
                }

                _context.SurveyResponses.AddRange(responses);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Réponses soumises avec succès." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans SubmitSurveyResponse: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la soumission des réponses.", details = ex.Message });
            }
        }

        [HttpGet("GetSurveysForEmployee")]
        [Authorize(Policy = "EmployeeOnly")]
        public async Task<IActionResult> GetSurveysForEmployee()
        {
            try
            {
                Console.WriteLine("Entrée dans l'endpoint GetSurveysForEmployee");

                // Vérifier l'authentification
                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var employeeId))
                {
                    Console.WriteLine("Échec de l'authentification : Aucun claim Identifier valide.");
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }
                Console.WriteLine($"ID utilisateur authentifié : {employeeId}");

                // Vérifier que l'employé existe
                var employee = await _context.Users.OfType<Employe>().FirstOrDefaultAsync(e => e.Id == employeeId);
                if (employee == null)
                {
                    Console.WriteLine($"Employé non trouvé pour l'ID : {employeeId}");
                    return Unauthorized(new { message = "Employé non trouvé." });
                }
                Console.WriteLine($"Employé trouvé : ID={employeeId}, Nom={employee.Firstname} {employee.Lastname}");

                // Charger tous les sondages
                var surveys = await _context.Surveys
                    .Include(s => s.Questions)
                    .ToListAsync();
                Console.WriteLine($"Nombre total de sondages chargés : {surveys.Count}");
                surveys.ForEach(s => Console.WriteLine($"Titre du sondage : {s.Title}, ID : {s.Id}"));

                // Filtrer les sondages commençant par "well-being"
                var wellbeingSurveys = surveys
                    .Where(s => s.Title.StartsWith("well-being", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Console.WriteLine($"Sondages well-being trouvés : {wellbeingSurveys.Count}");
                wellbeingSurveys.ForEach(s => Console.WriteLine($"Titre du sondage well-being : {s.Title}, ID : {s.Id}"));

                // Vérifier les sondages répondus
                var respondedSurveyIds = await _context.SurveyResponses
                    .Where(sr => sr.EmployeeId == employeeId)
                    .Select(sr => sr.SurveyId)
                    .ToListAsync();
                Console.WriteLine($"Sondages répondus par l'employé : {string.Join(", ", respondedSurveyIds)}");

                // Filtrer les sondages non répondus
                wellbeingSurveys = wellbeingSurveys
                    .Where(s => !respondedSurveyIds.Contains(s.Id))
                    .ToList();
                Console.WriteLine($"Sondages well-being après filtre de réponse : {wellbeingSurveys.Count}");
                wellbeingSurveys.ForEach(s => Console.WriteLine($"Titre du sondage well-being filtré : {s.Title}, ID : {s.Id}"));

                // Préparer les DTOs
                var surveyDtos = new List<SurveyDto>();
                foreach (var survey in wellbeingSurveys)
                {
                    var creator = await _context.Users.FindAsync(survey.CreatedBy);
                    var creatorName = creator != null
                        ? (!string.IsNullOrEmpty(creator.Firstname) && !string.IsNullOrEmpty(creator.Lastname))
                            ? $"{creator.Firstname} {creator.Lastname}"
                            : (!string.IsNullOrEmpty(creator.Firstname) ? creator.Firstname
                                : (!string.IsNullOrEmpty(creator.Lastname) ? creator.Lastname
                                    : "Utilisateur inconnu"))
                        : "Créateur inconnu";

                    surveyDtos.Add(new SurveyDto
                    {
                        Id = survey.Id.ToString(),
                        Title = survey.Title,
                        Questions = survey.Questions.Select(q => new SurveyQuestionDto
                        {
                            Id = q.Id,
                            Type = q.Type,
                            Text = q.Text,
                            Options = q.Options,
                            Required = q.Required
                        }).ToList(),
                        CreatedAt = survey.CreatedAt.ToString("o"),
                        CreatedBy = survey.CreatedBy.ToString(),
                        CreatorName = creatorName
                    });
                }
                Console.WriteLine($"DTOs de sondages préparés : {surveyDtos.Count}");
                return Ok(surveyDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetSurveysForEmployee : {ex.Message}, StackTrace : {ex.StackTrace}");
                return StatusCode(500, new { message = "Erreur interne lors de la récupération des sondages.", details = ex.Message });
            }
        }

        [HttpGet("GetResponses/{surveyId}")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> GetResponses(Guid surveyId)
        {
            try
            {
                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.UserType != UserType.RH)
                {
                    return Unauthorized(new { message = "Seuls les utilisateurs de type RH peuvent accéder aux réponses." });
                }

                var survey = await _context.Surveys.FirstOrDefaultAsync(s => s.Id == surveyId);
                if (survey == null)
                {
                    return NotFound(new { message = "Sondage non trouvé." });
                }

                var responses = await _context.SurveyResponses
                    .Where(sr => sr.SurveyId == surveyId)
                    .Include(sr => sr.Employee)
                    .ToListAsync();

                var responseDtos = responses.Select(sr => new SurveyResponseDto
                {
                    Id = sr.Id,
                    SurveyId = sr.SurveyId,
                    EmployeeId = sr.EmployeeId,
                    EmployeeName = sr.Employee != null
                        ? $"{sr.Employee.Firstname} {sr.Employee.Lastname}"
                        : "Employé inconnu",
                    QuestionId = sr.QuestionId,
                    Answer = sr.Answer,
                    RespondedAt = sr.RespondedAt.ToString("o")
                }).ToList();

                return Ok(responseDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetResponses: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la récupération des réponses.", details = ex.Message });
            }
        }

        [HttpGet("GetTotalEmployees")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> GetTotalEmployees()
        {
            try
            {
                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.UserType != UserType.RH)
                {
                    return Unauthorized(new { message = "Seuls les utilisateurs de type RH peuvent accéder à cette information." });
                }

                var totalEmployees = await _context.Users.OfType<Employe>().CountAsync();
                return Ok(new { totalEmployees });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans GetTotalEmployees: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la récupération du nombre d'employés.", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RHOnly")]
        public async Task<IActionResult> DeleteSurvey(Guid id)
        {
            try
            {
                var userIdString = User.FindFirst("Identifier")?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.UserType != UserType.RH)
                {
                    return Unauthorized(new { message = "Seuls les utilisateurs de type RH peuvent supprimer des sondages." });
                }

                var survey = await _context.Surveys
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (survey == null)
                {
                    return NotFound(new { message = "Sondage non trouvé." });
                }

                if (survey.CreatedBy != userId)
                {
                    return Forbid("Vous n'êtes pas autorisé à supprimer ce sondage.");
                }

                _context.Surveys.Remove(survey);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Sondage supprimé avec succès." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans DeleteSurvey: {ex.Message}");
                return StatusCode(500, new { message = "Erreur interne lors de la suppression du sondage.", details = ex.Message });
            }
        }
    }
}