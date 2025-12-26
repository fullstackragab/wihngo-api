namespace Wihngo.Dtos
{
    using System;

    public class UserSummaryDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
    }
}
