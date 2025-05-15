using System.Collections.Generic;

namespace Back_HR.Models.Dtos
{
    public class SurveyDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string CreatorName { get; set; }
        public List<SurveyQuestionDto> Questions { get; set; }

        public SurveyDto()
        {
            Id = string.Empty;
            Title = string.Empty;
            CreatedAt = string.Empty;
            CreatedBy = string.Empty;
            CreatorName = string.Empty; // Correction : suppression du `=`
            Questions = new List<SurveyQuestionDto>();
        }
    }
}