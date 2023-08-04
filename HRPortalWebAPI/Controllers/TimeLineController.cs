using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using HRPortalWebAPI.Models.DTO;

namespace HRPortalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeLineController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TimeLineController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        //Gives the list of timeline details object as json
        [HttpGet("GetTimeLine/{id}")]
        public IActionResult GetTimeLine([FromRoute] int id)
        {
            SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("HRPortalConnection"));
            SqlCommand cmd = new SqlCommand("Select * from timelinedetails where empId = '" + id + "' order by dateOfAction desc", conn);
            var timelineDetails = new List<TimeLineDetailsDTO>();
            try
            {
                var IsEmpIdValid = Validation.DoesEmployeeWithIdExist(id,conn);
                if (!IsEmpIdValid)
                {
                    return BadRequest("Employee with given ID does not exist!! Please enter valid ID");
                }
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var timelineAction = new TimeLineDetailsDTO
                    {
                        Action = reader.GetString(reader.GetOrdinal("action")),
                        DateOfAction = reader.GetDateTime(reader.GetOrdinal("dateOfAction"))
                    };
                    timelineDetails.Add(timelineAction);
                }
                if (timelineDetails.Count == 0)
                {
                    return Ok("The user with given ID does not have any actions to display");
                }
                return Ok(timelineDetails);
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
