using System.ComponentModel.DataAnnotations;

namespace Back_HR.Models
{
    public class PerformanceReview
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Employe Employee { get; set; }
        public DateTime ReviewDate { get; set; }
        public int TasksCompleted { get; set; }
        public double OnTimeCompletionRate { get; set; }
        public int ProcessImprovementIdeas { get; set; }
        public int Absences { get; set; }
        public int LateArrivals { get; set; }
        public int OutputQualityScore { get; set; }
        public int InitiativeScore { get; set; }
        public int CommunicationScore { get; set; }
        public double OverallScore { get; set; }
        public string ManagerComment { get; set; }
        public int ClientSatisfactionScore { get; set; }


        public void CalculateOverallScore()
        {
            // Weights can be adjusted based on company priorities
            double quantitativeWeight = 0.6;
            double qualitativeWeight = 0.4;

            // Quantitative (60% weight)
            double quantitativeScore =
                (OnTimeCompletionRate * 0.3) +
                (ProcessImprovementIdeas * 0.1) +
                (TasksCompleted * 0.2) -
                (Absences * 0.2) -
                (LateArrivals * 0.2);

            // Qualitative (40% weight)
            double qualitativeScore =
                (OutputQualityScore * 0.4) +
                (InitiativeScore * 0.3) +
                (CommunicationScore * 0.3);

            OverallScore = (quantitativeScore * quantitativeWeight) + (qualitativeScore * qualitativeWeight);
        }
    }
}
