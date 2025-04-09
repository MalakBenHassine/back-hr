using System.ComponentModel.DataAnnotations;

namespace Back_HR.Models
{
    public class Attendance
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Employe Employee { get; set; }
        public DateTime Date { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsLate { get; set; }
        public string Notes { get; set; }
    }
}
