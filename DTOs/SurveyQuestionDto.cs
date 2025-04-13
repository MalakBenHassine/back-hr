using System.Collections.Generic;
using System;

namespace Back_HR.Models.Dtos
{
    public class SurveyQuestionDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public List<string>? Options { get; set; }
        public bool Required { get; set; }
    }
}