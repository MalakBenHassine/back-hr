using System.Collections.Generic;
using System;

namespace Back_HR.Models
{
    public class SurveyQuestion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string>? Options { get; set; }
        public bool Required { get; set; }
        public Guid SurveyId { get; set; }
        public Survey? Survey { get; set; } // Ligne 12 : Erreur CS0246 ici
    }
}