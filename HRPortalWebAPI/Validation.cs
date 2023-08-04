using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace HRPortalWebAPI
{
    public static class Validation
    {
        public static bool IsEmailValid(this string email, out string Message)
        {
            if(email == string.Empty)
            {
                Message = "Email cannot be empty. Please enter valid Email,";
                return false;
            }
            if (!email.EndsWith("@gmail.com")) 
            {
                Message = "Email should end with \'@gmail.com\' domain address.";
                return false;
            }
            Message = "";
            return true;
        }

        public static bool IsPasswordValid (this string password, out string Message)
        {
            var ValidGuidRegux = new Regex("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$");
            if (ValidGuidRegux.IsMatch(password))
            {
                Message = "";
                return true;
            }
            else
            {
                Message = @"- Password should contain at least 8 Characters
- Password should contain at least 1 Uppercase letter
- Password should contain at least 1 Lowercase Letter
- Password should contain at least 1 Special Character";
                return false;
            }
        }

        public static bool DoesHrWithIdExist(int empId, SqlConnection conn)
        {
            SqlDataAdapter da = new SqlDataAdapter("Select * from admindetails where empId = '" + empId + "'", conn);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if(dt.Rows.Count == 0)
            {
                return false;
            }
            return true;
        }

        public static bool DoesEmployeeWithIdExist (int empId, SqlConnection conn)
        {
            SqlDataAdapter da = new SqlDataAdapter("Select * from employeedetails where id = '" + empId + "'", conn);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count == 0)
            {
                return false;
            }
            return true;
        }

        public static bool DoesManagerWithIdExist(int id, SqlConnection conn)
        {
            SqlDataAdapter da = new SqlDataAdapter("select * from managerdetails where managerId = '" + id + "'", conn);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if(dt.Rows.Count == 0)
            {
                return false;
            }
            return true;
        }
    }
}
