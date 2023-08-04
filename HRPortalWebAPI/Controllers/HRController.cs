using HRPortalWebAPI.Models.Domain;
using HRPortalWebAPI.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Numerics;
using System.Xml.Linq;

namespace HRPortalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HRController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public HRController(IConfiguration configuration) 
        {
            _configuration = configuration;
        }

        //Assuming the HR has the privilege to Fetch details of all employees, Insert, Update and Delete employees from the database
        //New HR record can be added to the admindetails table in the database manually by an existing admin


        //If a user selects admin option while logging in this api will be used and email and password will be sent to this api
        //Will check if the user is present in the admindetails table and if present login will be successful
        //otherwise proper message "Invalid Username/ Password" will be sent 
        [HttpPost("Login")]
        public IActionResult Login(LoginDetailsDTO adminLoginDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            try
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter("Select * from admindetails where email = '" + adminLoginDetails.Email + "'", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if(dt.Rows.Count > 0)
                {
                    if(dt.Rows[0]["password"].ToString() == adminLoginDetails.Password)
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

        //Will create a new employee record in the employeedetails table in the database
        //Implements the email and password validation. Prompts respective messages if not valid
        //If the email id already exists then it will prompt a message saying email already exists
        [HttpPost("CreateNewEmployee/{hrId}")]
        public IActionResult CreateNewEmployee([FromRoute] int hrId,EmployeeDetailsDTO employeeDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            var IsHRIdValid = Validation.DoesHrWithIdExist(hrId, conn);
            if(!IsHRIdValid)
            {
                return NotFound("HR with given ID does not exist. Please access the API with valid HR ID.");
            }
            SqlCommand cmd = new SqlCommand("CreateNewEmployee", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Name", employeeDetails.Name);
            cmd.Parameters.AddWithValue("@Email", employeeDetails.Email);
            cmd.Parameters.AddWithValue("@Password", employeeDetails.Password);
            cmd.Parameters.AddWithValue("@Phone", employeeDetails.Phone);
            cmd.Parameters.AddWithValue("@Salary", employeeDetails.Salary);
            cmd.Parameters.AddWithValue("@Department", employeeDetails.Department);
            cmd.Parameters.AddWithValue("@JoiningDate", employeeDetails.DateOfJoining);
            cmd.Parameters.AddWithValue("@ManagerId", employeeDetails.ManagerId);
            cmd.Parameters.Add("@id", SqlDbType.Int).Direction = ParameterDirection.Output;
            if (!employeeDetails.Email.IsEmailValid(out string EmailMessage))
            {
                return BadRequest(EmailMessage);
            }
            if (!employeeDetails.Password.IsPasswordValid(out string PasswordMessage))
            {
                return BadRequest(PasswordMessage);
            }
            try
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter("Select * from employeedetails where email = '"+employeeDetails.Email+"'", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if(dt.Rows.Count > 0)
                {
                    return BadRequest("Email ID is already taken. Please try using another email.");
                }
                var IsManagerIdValid = Validation.DoesManagerWithIdExist(employeeDetails.ManagerId, conn);
                if(!IsManagerIdValid)
                {
                    return BadRequest("Manager with the given ID does not Exist!! Please enter a valid Manager Id");
                }
                int rowsAffected = cmd.ExecuteNonQuery();
                int newEmployeeId = (int)cmd.Parameters["@id"].Value;
                if (rowsAffected != 0)
                {
                    SqlCommand timelineUpdateCmd = new SqlCommand("Insert into timelinedetails (empId,action,dateOfAction) values (@Id, @Action, @Date)", conn);
                    timelineUpdateCmd.Parameters.AddWithValue("@Id", hrId);
                    timelineUpdateCmd.Parameters.AddWithValue("@Action", "Created a New Employee. ID of newly created Employee is '" + newEmployeeId + "'");
                    timelineUpdateCmd.Parameters.AddWithValue("@Date", DateTime.Now);
                    timelineUpdateCmd.ExecuteNonQuery();
                    employeeDetails.Id = newEmployeeId;
                    return CreatedAtAction(nameof(CreateNewEmployee), employeeDetails);
                }
                return BadRequest("Unable to add the user. Please try again!!");
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

        //This api will fetch the details of the employee except the password and sends them as a json object
        [HttpGet("GetAllEmployees")]
        public IActionResult GetAllEmployees()
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlCommand cmd = new SqlCommand("Select * from employeedetails",conn);
            var employees = new List<PublicEmployeeDetailsDTO>();
            try
            {
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var employee = new PublicEmployeeDetailsDTO
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Email = reader.GetString(reader.GetOrdinal("email")),
                        Phone = reader.GetString(reader.GetOrdinal("phone")),
                        Salary = reader.GetInt32(reader.GetOrdinal("salary")),
                        Department = reader.GetString(reader.GetOrdinal("department")),
                        DateOfJoining = reader.GetDateTime(reader.GetOrdinal("DateOfJoining")),
                        ManagerId = reader.GetInt32(reader.GetOrdinal("managerId"))
                    };
                    employees.Add(employee);
                }
                return Ok(employees);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        //will store the id of the HR in the React context after successful login
        //and use the saved context to pass the id of the HR to the url 
        //Will pass the email and new password using the front end
        //Checks if the email id send from the front end matches with the record in the database
        //if email matches then validates the password if it is according to the standards and changes the password
        [HttpPut("ChangeHRPassword/{id}")]
        public IActionResult ChangeHRPassword([FromRoute] int id, ChangePasswordDetailsDTO newDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("Select * from admindetails where empId='" + id + "'", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 0)
                {
                    return NotFound("There is no employee with the given ID in the admindetails table. Provide correct ID!!");
                }
                if (dt.Rows[0]["email"].ToString() != newDetails.Email)
                {
                    return NotFound("Incorrect Email ID..!! Try again.");
                }
                if (dt.Rows[0]["password"].ToString() != newDetails.OldPassword)
                {
                    return BadRequest("Incorrect Password!! Enter the correct current password to change to the new Password");
                }
                if (!newDetails.NewPassword.IsPasswordValid(out string Message))
                {
                    return BadRequest(Message);
                }
                if(newDetails.NewPassword == newDetails.OldPassword)
                {
                    return BadRequest("New Password cannot be the same as Old Password!! Provide a new password");
                }

                SqlCommand cmd = new SqlCommand("Update admindetails set password = '" + newDetails.NewPassword + "' where empId='" + id + "'", conn);
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                SqlCommand timelineUpdateCmd = new SqlCommand("Insert into timelinedetails (empId,action,dateOfAction) values (@Id, @Action, @Date)", conn);
                timelineUpdateCmd.Parameters.AddWithValue("@Id", id);
                timelineUpdateCmd.Parameters.AddWithValue("@Action", "Changed HR Login Password");
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

        //Works in conjunction with the GetEmployeeDetails API 
        //Employee Details obtained from the GetEmployeeDetails API will be displayed to the HR
        //only the Name, Phone, Salary, Department will be editable fields in the front end
        //If any changes are made to these fields those fields will be updated in the respective record of employeedetails table in the database
        [HttpPost("UpdateEmployeeDetails/{hrId}/{empId}")]
        public IActionResult UpdateEmployeeDetails([FromRoute] int hrId,[FromRoute] int empId,PublicEmployeeDetailsDTO publicEmployeeDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlCommand cmd = new SqlCommand(@"Update employeedetails set name = '"+publicEmployeeDetails.Name+"'," +
                "phone = '"+publicEmployeeDetails.Phone+"', salary = '"+publicEmployeeDetails.Salary+"'," +
                "department = '"+publicEmployeeDetails.Department+"', managerId = '"+publicEmployeeDetails.ManagerId+"' where id = '"+ empId + "'", conn);
            try
            {
                var IsHrIdValid = Validation.DoesHrWithIdExist(hrId, conn);
                var IsEmployeeIdValid = Validation.DoesEmployeeWithIdExist(empId, conn);
                if(!IsHrIdValid)
                {
                    return BadRequest("HR with given ID does not exist. Please access the API with valid HR ID.");
                }
                if (!IsEmployeeIdValid)
                {
                    return BadRequest("Employee with given ID does not exist!! Please enter valid ID");
                }
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                SqlCommand timelineUpdateCmd = new SqlCommand("Insert into timelinedetails (empId,action,dateOfAction) values (@Id, @Action, @Date)", conn);
                timelineUpdateCmd.Parameters.AddWithValue("@Id", hrId);
                timelineUpdateCmd.Parameters.AddWithValue("@Action", "Updated the Details of employee with id '" + empId + "'");
                timelineUpdateCmd.Parameters.AddWithValue("@Date", DateTime.Now);
                timelineUpdateCmd.ExecuteNonQuery();
                return Ok("Employee Details Updated");
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

        //Will delete the employee from the employeedetails table
        [HttpDelete("DeleteEmployee/{hrId}/{empId}")]
        public IActionResult DeleteEmployee([FromRoute] int hrId,[FromRoute] int empId)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlCommand cmd = new SqlCommand("Delete from employeedetails where id='" + empId + "'",conn);
            try
            {
                var IsHrIdValid = Validation.DoesHrWithIdExist(hrId, conn);
                if (!IsHrIdValid)
                {
                    return NotFound("HR with given ID does not exist. Please access the API with valid HR ID.");
                }
                var IsEmployeeIdValid = Validation.DoesEmployeeWithIdExist(empId, conn);
                if (!IsEmployeeIdValid)
                {
                    return NotFound("Employee with given ID does not exist!! Please enter valid ID");
                }
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                SqlCommand timelineUpdateCmd = new SqlCommand("Insert into timelinedetails (empId,action,dateOfAction) values (@Id, @Action, @Date)", conn);
                timelineUpdateCmd.Parameters.AddWithValue("@Id", hrId);
                timelineUpdateCmd.Parameters.AddWithValue("@Action", "Deleted an Employee with Id '" + empId + "'");
                timelineUpdateCmd.Parameters.AddWithValue("@Date", DateTime.Now);
                timelineUpdateCmd.ExecuteNonQuery();
                return Ok("Employee Deleted");
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
        
        //This API returns list of the employees reporting to a particular manager whose id is provided
        //This API can be accessed by the HR, Manager
        //This API checks if a Manager exists with the given ID and if exists returns the list of employees reporting to the manager
        [HttpGet("/api/GetEmployeesReportingToManager/{id}")]
        public IActionResult GetEmployeesReportingToManager([FromRoute] int id)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("Select * from managerdetails where managerId='" + id + "';", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 1)
                {
                    SqlCommand cmd = new SqlCommand("select * from employeedetails where managerId = '" + id + "' and id <> '" + id + "'",conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    var employeeList = new List<PublicEmployeeDetailsDTO>();
                    while(reader.Read())
                    {
                        var employee = new PublicEmployeeDetailsDTO
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Email = reader.GetString(reader.GetOrdinal("email")),
                            Phone = reader.GetString(reader.GetOrdinal("phone")),
                            Salary = reader.GetInt32(reader.GetOrdinal("salary")),
                            Department = reader.GetString(reader.GetOrdinal("department")),
                            DateOfJoining = reader.GetDateTime(reader.GetOrdinal("DateOfJoining")),
                            ManagerId = reader.GetInt32(reader.GetOrdinal("managerId"))
                        };
                        employeeList.Add(employee);
                    }
                    return Ok(employeeList);
                }
                return NotFound("Manager with the given ID does not exist!! Please enter the correct ID of Manager");
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

        //User must be created first before adding to the managerdetails table as the managerId in managerdetails
        //is a foreign key which references the id (primary key) in employeedetails table
        //This API checks if employee with given id is present in the employeedetails table if present will add the manager
        [HttpPost("AddManager/{hrId}")]
        public IActionResult AddManager([FromRoute] int hrId,ManagerDetails managerDetails)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            try
            {
                var IsHrIdValid = Validation.DoesHrWithIdExist(hrId, conn);
                if(!IsHrIdValid)
                {
                    return NotFound("HR with given ID does not exist. Please access the API with valid HR ID.");
                }
                SqlDataAdapter da = new SqlDataAdapter("Select * from employeedetails where id = '" + managerDetails.Id + "'", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if(dt.Rows.Count > 0)
                {
                    SqlCommand cmd = new SqlCommand("Insert into managerdetails(managerId, department) values (@Id,@Department)",conn);
                    cmd.Parameters.AddWithValue("@Id", managerDetails.Id);
                    cmd.Parameters.AddWithValue("@Department", managerDetails.Department);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        SqlCommand timelineUpdateCmd = new SqlCommand("Insert into timelinedetails (empId,action,dateOfAction) values (@Id, @Action, @Date)", conn);
                        timelineUpdateCmd.Parameters.AddWithValue("@Id", hrId);
                        timelineUpdateCmd.Parameters.AddWithValue("@Action", "Added employee with Id '" + managerDetails.Id + "' as a Manager");
                        timelineUpdateCmd.Parameters.AddWithValue("@Date", DateTime.Now);
                        timelineUpdateCmd.ExecuteNonQuery();
                        return CreatedAtAction(nameof(AddManager),managerDetails);
                    }
                }
                return NotFound("Employee with given ID does not exist. Please provide a valid ID to add Employee as Manager.");
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
