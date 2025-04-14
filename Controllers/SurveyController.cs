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

                // Log pour vérifier les données de l'utilisateur
                Console.WriteLine($"Utilisateur récupéré: Id={user.Id}, Firstname={user.Firstname}, Lastname={user.Lastname}");

                var survey = new Survey
                {
                    Id = Guid.NewGuid(),
                    Title = surveyDto.Title,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    CreatorId = userId, // Définir CreatorId pour lier le sondage à l'utilisateur
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
                // Log pour vérifier CreatorName
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

                // Log pour vérifier la réponse complète
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
                    .Include(s => s.Creator)
                    .ToListAsync();

                var surveyDtos = new List<SurveyDto>();
                foreach (var survey in surveys)
                {
                    User creator = survey.Creator;
                    if (creator == null)
                    {
                        // Si CreatorId est défini, charger l'utilisateur via CreatorId
                        if (survey.CreatorId.HasValue)
                        {
                            creator = await _context.Users.FindAsync(survey.CreatorId.Value);
                        }
                        // Sinon, utiliser CreatedBy comme solution de secours
                        if (creator == null && survey.CreatedBy != Guid.Empty)
                        {
                            creator = await _context.Users.FindAsync(survey.CreatedBy);
                        }
                    }

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
                    .Include(s => s.Creator)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (survey == null)
                {
                    return NotFound(new { message = "Sondage non trouvé." });
                }

                User creator = survey.Creator;
                if (creator == null)
                {
                    // Si CreatorId est défini, charger l'utilisateur via CreatorId
                    if (survey.CreatorId.HasValue)
                    {
                        creator = await _context.Users.FindAsync(survey.CreatorId.Value);
                    }
                    // Sinon, utiliser CreatedBy comme solution de secours
                    if (creator == null && survey.CreatedBy != Guid.Empty)
                    {
                        creator = await _context.Users.FindAsync(survey.CreatedBy);
                    }
                }

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

                // Log pour vérifier les données de l'utilisateur
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

                // Log pour vérifier CreatorName
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

                // Log pour vérifier la réponse complète
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

                // Log des données reçues
                Console.WriteLine($"Données reçues dans UpdateSurvey: Title={surveyDto.Title}, Questions={System.Text.Json.JsonSerializer.Serialize(surveyDto.Questions)}");

                // Validation des données entrantes
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

                // Recharger le sondage
                var survey = await _context.Surveys
                    .Include(s => s.Questions)
                    .Include(s => s.Creator)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (survey == null)
                {
                    return NotFound(new { message = "Sondage non trouvé." });
                }

                if (survey.CreatedBy != userId)
                {
                    return Forbid("Vous n'êtes pas autorisé à modifier ce sondage.");
                }

                // Mise à jour du titre
                survey.Title = surveyDto.Title;

                // Supprimer les anciennes questions de la base de données
                if (survey.Questions.Any())
                {
                    _context.SurveyQuestions.RemoveRange(survey.Questions);
                    await _context.SaveChangesAsync(); // Sauvegarder immédiatement pour éviter les conflits
                }

                // Ajouter les nouvelles questions
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

                // Sauvegarder les changements
                await _context.SaveChangesAsync();

                // Utiliser l'utilisateur authentifié pour CreatorName
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