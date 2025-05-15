using System;

namespace Back_HR.Models
{
    public class SurveyResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SurveyId { get; set; }
        public Survey Survey { get; set; }
        public Guid EmployeeId { get; set; }
        public Employe Employee { get; set; }
        public Guid QuestionId { get; set; }
        public SurveyQuestion Question { get; set; }
        public string Answer { get; set; } = string.Empty; // Store the employee's answer
        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    }
}