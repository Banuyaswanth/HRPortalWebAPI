using HRPortalWebAPI.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace HRPortalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public EmployeeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("Login")]
        public IActionResult Login(LoginDetailsDTO loginDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            try
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter("Select * from employeedetails where email = '" + loginDetails.Email + "'", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["password"].ToString() == loginDetails.Password)
                    {
                        return Ok("Login Successful!!");
                    }
                    return BadRequest("Invalid Password");
                }
                return NotFound("Invalid Username!");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        //This api will return a json of PublicEmployeeDetails json
        //This will take an id as a parameter in the route and use that to get details of employee from employeedetails table
        //This api can be used by the HR to get details of any employee 
        //This api can be used by the Employee to get their details
        [HttpGet("/api/GetEmployeeDetails/{id}")]
        public IActionResult GetEmployeeDetails([FromRoute] int id)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlDataAdapter da = new SqlDataAdapter("Select * from employeedetails where id = '" + id + "'", conn);
            DataTable dt = new DataTable();
            try
            {
                da.Fill(dt);
                if (dt.Rows.Count == 0)
                {
                    return BadRequest("The employee with the given ID does not Exist!!\nEnter valid employee ID");
                }
                PublicEmployeeDetailsDTO empDetails = new PublicEmployeeDetailsDTO
                {
                    Id = int.Parse(dt.Rows[0]["id"].ToString()),
                    Name = dt.Rows[0]["name"].ToString(),
                    Email = dt.Rows[0]["email"].ToString(),
                    Phone = dt.Rows[0]["phone"].ToString(),
                    Salary = int.Parse(dt.Rows[0]["salary"].ToString()),
                    Department = dt.Rows[0]["phone"].ToString(),
                    DateOfJoining = Convert.ToDateTime(dt.Rows[0]["DateOfJoining"].ToString()),
                    ManagerId = int.Parse(dt.Rows[0]["managerId"].ToString())
                };
                return Ok(empDetails);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //Id will be stored in the context in the frontend and will be used to send in the url for updating details
        [HttpPut("UpdatePersonalDetails/{id}")]
        public IActionResult UpdatePersonalDetails([FromRoute] int id, UpdatablePersonalDetailsDTO updatablePersonalDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlCommand cmd = new SqlCommand(@"Update employeedetails set name = '" + updatablePersonalDetails.Name + "'," +
                "phone = '" + updatablePersonalDetails.Phone + "' where id = '" + id + "'", conn);
            try
            {
                var IsEmpIdValid = Validation.DoesEmployeeWithIdExist(id, conn);
                if(!IsEmpIdValid)
                {
                    return BadRequest("Employee with given ID does not exist!! Please enter valid ID");
                }
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                SqlCommand timelineUpdateCmd = new SqlCommand("Insert into timelinedetails (empId,action,dateOfAction) values (@Id, @Action, @Date)", conn);
                timelineUpdateCmd.Parameters.AddWithValue("@Id", id);
                timelineUpdateCmd.Parameters.AddWithValue("@Action", "Updated personal details");
                timelineUpdateCmd.Parameters.AddWithValue("@Date", DateTime.Now);
                timelineUpdateCmd.ExecuteNonQuery();
                return Ok("Changes Saved");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        [HttpPut("ChangePassword/{id}")]
        public IActionResult ChangePassword([FromRoute] int id,ChangePasswordDetailsDTO changePasswordDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlDataAdapter da = new SqlDataAdapter("Select * from employeedetails where id='" + id + "'", conn);
            DataTable dt = new DataTable();
            try
            {
                da.Fill(dt);
                if (dt.Rows.Count == 0)
                {
                    return BadRequest("There is no employee with the given ID in the employeedetails table. Provide correct ID!!");
                }
                if (dt.Rows[0]["email"].ToString() != changePasswordDetails.Email)
                {
                    return NotFound("Incorrect Email ID..!! Try again.");
                }
                if (dt.Rows[0]["password"].ToString() != changePasswordDetails.OldPassword)
                {
                    return BadRequest("Incorrect Password!! Enter the correct current password to change to the new Password");
                }
                if (!changePasswordDetails.NewPassword.IsPasswordValid(out string Message))
                {
                    return BadRequest(Message);
                }
                if (changePasswordDetails.NewPassword == changePasswordDetails.OldPassword)
                {
                    return BadRequest("New Password cannot be same as the Old Password!! Provide a new Password");
                }

                SqlCommand cmd = new SqlCommand("Update employeedetails set password = '" + changePasswordDetails.NewPassword + "' where id='" + id + "'", conn);
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                SqlCommand timelineUpdateCmd = new SqlCommand("Insert into timelinedetails (empId,action,dateOfAction) values (@Id, @Action, @Date)", conn);
                timelineUpdateCmd.Parameters.AddWithValue("@Id", id);
                timelineUpdateCmd.Parameters.AddWithValue("@Action", "Changed the Password");
                timelineUpdateCmd.Parameters.AddWithValue("@Date", DateTime.Now);
                timelineUpdateCmd.ExecuteNonQuery();
                return Ok("Password has been changed");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
