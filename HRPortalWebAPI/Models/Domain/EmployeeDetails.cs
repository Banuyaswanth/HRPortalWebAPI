﻿namespace HRPortalWebAPI.Models.Domain
{
    public class EmployeeDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Phone { get; set; }
        public int Salary { get; set; }
        public string Department { get; set; }
        public DateTime DateOfJoining { get; set; }
        public int ManagerId { get; set; }
    }
}
