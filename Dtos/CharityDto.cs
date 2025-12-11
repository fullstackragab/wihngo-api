namespace Wihngo.Dtos
{
    public class CharityImpactDto
    {
        public decimal TotalContributed { get; set; }
        public int BirdsHelped { get; set; }
        public int SheltersSupported { get; set; }
        public int ConservationProjects { get; set; }
    }

    public class GlobalCharityImpactDto
    {
        public decimal TotalContributed { get; set; }
        public int TotalSubscribers { get; set; }
        public int BirdsHelped { get; set; }
        public int SheltersSupported { get; set; }
        public int ConservationProjects { get; set; }
    }

    public class CharityPartnerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
    }
}
