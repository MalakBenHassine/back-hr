using System;
using System.Collections.Generic;

namespace Back_HR.Models
{
    public class Survey
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public User? Creator { get; set; } // Propriété de navigation, reste nullable
        public List<SurveyQuestion> Questions { get; set; }
        public List<Employe> Employes { get; set; }
        public List<SurveyResponse> Responses { get; set; }

        public Survey()
        {
            Id = Guid.NewGuid();
            Title = string.Empty;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = Guid.Empty;
            Creator = null;
            Questions = new List<SurveyQuestion>();
            Employes = new List<Employe>();
            Responses = new List<SurveyResponse>();
        }
    }
}