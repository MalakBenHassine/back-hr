namespace Back_HR.DTOs
{
    public class ApplicationResponseDto
    {
        public Guid ApplicationId { get; set; }
        public CandidatDTO Candidate { get; set; }
        public JobOfferDtoGet JobOffer { get; set; }
        public DateTime ApplicationDate { get; set; }
        public CvFileDto? CvFile { get; set; }
        public string? Message { get; set; }
        public string? Status { get; set; }
        public double AdequacyScore { get; set; }
    

    }
}
