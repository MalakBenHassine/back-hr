namespace Back_HR.Models.Dtos
{
    public class SurveyResponseDto
    {
        public Guid Id { get; set; }
        public Guid SurveyId { get; set; }
        public Guid EmployeeId { get; set; } 
        public string? EmployeeName { get; set; } 
        public Guid QuestionId { get; set; }
        public string? Answer { get; set; } 
        public string? RespondedAt { get; set; } 
    }

    public class SubmitSurveyResponseDto
    {
        public Guid SurveyId { get; set; }
        public Guid QuestionId { get; set; }
        public string? Answer { get; set; } 
    }
}