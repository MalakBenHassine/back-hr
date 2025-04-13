using System;
using System.Collections.Generic;

namespace Back_HR.DTOs
{
    public class SurveyResponseResultDTO
    {
        public Guid Id { get; set; }
        public Guid SurveyId { get; set; }
        public Guid EmployeeId { get; set; }
        public List<string> Answers { get; set; } = new List<string>();
    }
}