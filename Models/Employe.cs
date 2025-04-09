using System.ComponentModel.DataAnnotations;

namespace Back_HR.Models
{
    public class Employe : User
    {
        // Position/Role Information
        public string Poste { get; set; }
        public string? Department { get; set; }
        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        // Performance Tracking
        public int TasksCompleted { get; set; }
        public double OnTimeCompletionRate { get; set; }
        public int ProcessImprovementIdeas { get; set; }
        public int Absences { get; set; }
        public int LateArrivals { get; set; }
        public int ClientSatisfactionScore { get; set; } // Average 1-5

        // Navigation Properties
        public List<PerformanceReview> PerformanceReviews { get; set; } = new();
        public List<Attendance> Attendances { get; set; } = new();

        // Survey Relationships (existing)
        public List<Survey> SurveysResponded { get; set; } = new();
        public List<SurveyResponse> SurveyResponses { get; set; } = new();

        // Notification Relationships (inherited from User)
        // public List<Notification> Notifications { get; set; } = new();

        // Helper Methods
        public void UpdateAttendanceMetrics()
        {
            Absences = Attendances.Count(a => a.IsAbsent);
            LateArrivals = Attendances.Count(a => a.IsLate);
        }

        public double CalculateProductivityScore()
        {
            return (TasksCompleted * 0.3)
                 + (OnTimeCompletionRate * 0.2)
                 + (ProcessImprovementIdeas * 0.1)
                 - (Absences * 0.2)
                 - (LateArrivals * 0.1)
                 + (ClientSatisfactionScore * 0.1);
        }
    }
}