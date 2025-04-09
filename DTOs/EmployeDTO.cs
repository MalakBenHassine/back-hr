namespace Back_HR.DTOs
{
    public class EmployeDTO
    {
        public Guid Id { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public string Telephone { get; set; }
        public string Email { get; set; }
        public string Poste { get; set; }
        public string? Department { get; set; }

    }
}
