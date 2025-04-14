using System;
using System.Collections.Generic;

namespace Back_HR.Models
{
    public class SurveyResponse
    {
        public Guid Id { get; set; }
        public Guid SurveyId { get; set; }
        public Survey? Survey { get; set; }
        public Guid EmployeeId { get; set; } // Renommé de EmployeId à EmployeeId
        public Employe? Employee { get; set; } // Renommé de Employe à Employee
        public DateTime SubmittedAt { get; set; }
        public List<string> Answers { get; set; } // Changé de string à List<string>

        public SurveyResponse()
        {
            Id = Guid.NewGuid();
            SurveyId = Guid.Empty;
            Survey = null;
            EmployeeId = Guid.Empty;
            Employee = null;
            SubmittedAt = DateTime.UtcNow;
            Answers = new List<string>();
        }
    }
}