namespace HRPortalWebAPI.Models.DTO
{
    public class ChangePasswordDetailsDTO
    {
        public string Email { get; set; }

        public string OldPassword { get; set; }

        public string NewPassword { get; set; }
    }
}
