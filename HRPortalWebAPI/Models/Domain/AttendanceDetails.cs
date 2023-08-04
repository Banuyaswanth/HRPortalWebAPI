namespace HRPortalWebAPI.Models.Domain
{
    public class AttendanceDetails
    {
        public int Id { get; set; }
        public int EmpId { get; set; }
        public string DateOfAttendance { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public string? Duration { get; set; }
    }
}
