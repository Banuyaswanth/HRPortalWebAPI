using HRPortalWebAPI.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace HRPortalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AttendanceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("TimeIn/{id}")]
        public IActionResult TimeIn([FromRoute] int id)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            try
            {
                SqlCommand cmd = new SqlCommand("getLastAttendanceTimeInID", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@EmpId", id);
                cmd.Parameters.Add("@id", SqlDbType.Int).Direction = ParameterDirection.Output;
                conn.Open();
                var IsEmpIdValid = Validation.DoesEmployeeWithIdExist(id, conn);
                if (!IsEmpIdValid)
                {
                    return BadRequest("The Employee with given ID does not Exist!! Enter a Valid ID");
                }
                int i = cmd.ExecuteNonQuery();
                if (i == 0)
                {
                    return BadRequest("Unable to TimeIN. Please Try Again!!");
                }
                int LastTimeInID = (int)cmd.Parameters["@id"].Value;
                return Ok(new { lastTimeInId = LastTimeInID, Message = "Last TIMEIN record is stored with the Primary key as '" + LastTimeInID + "'" });
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

        [HttpPut("TimeOut/{empId}/{lastTimeInId}")]
        public IActionResult Timeout([FromRoute] int empId, [FromRoute] int lastTimeInId)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlCommand cmd = new SqlCommand("Update attendanceDetails set timeOut = GETDATE() where empId = '" + empId + "' and id = '" + lastTimeInId + "'",conn);
            SqlCommand setDurationCmd = new SqlCommand("Update attendanceDetails set duration = DateDiff(minute,timeIn,timeOut) where id = '"+lastTimeInId+"'", conn);
            try
            {
                conn.Open();
                int i = cmd.ExecuteNonQuery();
                if(i != 1)
                {
                    return BadRequest("Unable to TimeOut. Please Try Again!!");
                }
                setDurationCmd.ExecuteNonQuery();
                return Ok("TimeOut Successful");
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

        [HttpGet("GetAttendance/{id}")]
        public IActionResult GetAttendance([FromRoute] int id)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlCommand cmd = new SqlCommand(@"Select empId,dateOfAttendance, CONCAT(sum(duration)/60,'hr:',sum(duration)%60,'min') as WorkingHours
from attendancedetails where empId = '"+id+"' and duration is not null group by empId,dateOfAttendance order by dateOfAttendance desc", conn);
            try
            {
                var IsEmpIdValid = Validation.DoesEmployeeWithIdExist(id, conn);
                if (!IsEmpIdValid)
                {
                    return BadRequest("Invalid Employee ID. Please provide a valid ID");
                }
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                var attendanceDetails = new List<AttendanceDetailsDTO>();
                while (reader.Read())
                {
                    var attendance = new AttendanceDetailsDTO
                    {
                        DateOfAttendance = reader.GetDateTime(reader.GetOrdinal("dateOfAttendance")).ToString("dd/MM/yyyy"),
                        Duration = reader.GetString(reader.GetOrdinal("WorkingHours"))
                    };
                    attendanceDetails.Add(attendance);
                }
                if(attendanceDetails.Count == 0)
                {
                    return Ok("No attendance records to display for the employee");
                }
                return Ok(attendanceDetails);
            }
            catch (Exception ex) 
            { 
                return BadRequest(ex.Message );
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
