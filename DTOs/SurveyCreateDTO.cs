using System.Collections.Generic;

namespace Back_HR.Models.Dtos
{
    public class SurveyCreateDto
    {
        public string Title { get; set; } // Ajouter cette propriété
        public required List<SurveyQuestionDto> Questions { get; set; }

        public SurveyCreateDto()
        {
            Title = string.Empty; // Initialiser pour éviter CS8618
            Questions = new List<SurveyQuestionDto>();
        }

        public class SurveyQuestionDto
        {
            public required string Type { get; set; }
            public required string Text { get; set; }
            public string? Options { get; set; }
            public bool Required { get; set; }
        }
    }
}