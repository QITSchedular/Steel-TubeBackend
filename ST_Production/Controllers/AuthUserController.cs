using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ST_Production.Common;
using ST_Production.Models;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthUserController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;

        private SqlConnection QITcon;
        private SqlDataAdapter oAdptr;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<AuthUserController> _logger;

        public AuthUserController(IConfiguration configuration, ILogger<AuthUserController> logger)
        {
            objGlobal ??= new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;

                _QIT_connection = Configuration["connectApp:QITConnString"];
                Global.QIT_DB = "[" + Configuration["QITDB"] + "]";
                Global.SAP_DB = "[" + Configuration["CompanyDB"] + "]";
                Global.gLogPath = Configuration["LogPath"];

                cmd = new SqlCommand();
                QITcon = new SqlConnection(_QIT_connection);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error : {ex}", ex.ToString());
            }
        }


        [HttpPost("LoginPost")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<User>>> LoginPost([FromBody] User payload)
        {
            try
            {
                objGlobal.WriteLog("AuthUserController : LoginPost Info : " + payload.User_Name);
                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty." });
                }
                else
                {
                    objGlobal.WriteLog(" Info : LoginPost Initiated ");
                    DataTable dtUser = new();
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @userName";
                    _logger.LogInformation(" AuthUserController : LoginPost() Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@userName", payload.User_Name);
                    oAdptr.Fill(dtUser);
                    QITcon.Close();

                    if (dtUser.Rows.Count > 0)
                    {
                        bool isValidPwd = BCrypt.Net.BCrypt.Verify(payload.User_Password, dtUser.Rows[0]["User_Password"].ToString());

                        if (!isValidPwd)
                        {
                            return BadRequest(new { StatusCode = "400", StatusMsg = "Incorrect password." });
                        }

                        int userId = (int)dtUser.Rows[0]["User_ID"];

                        // Fetch Authentication Rule Details
                        DataTable dtAuthRule = new();
                        _Query = @" SELECT Authentication_Rule_Details FROM " + Global.QIT_DB + @".dbo.QIT_Authentication_Rule WHERE User_ID = @userId";
                        _logger.LogInformation(" AuthUserController : LoginPost() Query : {q} ", _Query.ToString());
                        using (oAdptr = new SqlDataAdapter(_Query, QITcon))
                        {
                            await QITcon.OpenAsync();
                            oAdptr.SelectCommand.Parameters.AddWithValue("@userId", userId);
                            oAdptr.Fill(dtAuthRule);
                            QITcon.Close();
                        }

                        List<getModuleClass> lstAuthRules = new();
                        if (dtAuthRule.Rows.Count > 0)
                        {
                            lstAuthRules = JsonConvert.DeserializeObject<List<getModuleClass>>(dtAuthRule.Rows[0]["Authentication_Rule_Details"].ToString());
                        }

                        // Fetch Warehouse Details
                        DataTable dtWhs = new();
                        _Query = @"
                        SELECT Warehouse_Code, Warehouse_Name 
                        FROM " + Global.QIT_DB + @".dbo.QIT_WarehouseRule_Master 
                        WHERE (',' +(SELECT REPLACE(REPLACE(User_details, '[', ''), ']', '') + ',') + ',') LIKE @userId ";
                        using (oAdptr = new SqlDataAdapter(_Query, QITcon))
                        {
                            await QITcon.OpenAsync();
                            oAdptr.SelectCommand.Parameters.AddWithValue("@userId", "%," + userId + ",%");
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();
                        }

                        List<GetWarehouseForUser> lstWhsRules = dtWhs.AsEnumerable()
                             .Select(row => new GetWarehouseForUser
                             {
                                 Warehouse_Code = row.Field<string>("Warehouse_Code"),
                                 Warehouse_Name = row.Field<string>("Warehouse_Name")
                             })
                             .ToList();


                        // Fetch Series Details
                        DataTable dtSeries = new();
                        _Query = @" SELECT User_ID, Series_Details FROM " + Global.QIT_DB + @".dbo.QIT_UserWiseSeries_Config WHERE User_ID = @userId ";
                        using (oAdptr = new SqlDataAdapter(_Query, QITcon))
                        {
                            await QITcon.OpenAsync();
                            oAdptr.SelectCommand.Parameters.AddWithValue("@userId", userId);
                            oAdptr.Fill(dtSeries);
                            QITcon.Close();
                        }

                        List<subSeriesClass> lstSeries = new();

                        if (dtSeries.Rows.Count > 0)
                        {
                            lstSeries = JsonConvert.DeserializeObject<List<subSeriesClass>>(dtSeries.Rows[0]["Series_Details"].ToString());
                        }

                        // Generate JWT token
                        string jwtToken = GenerateJWTToken(dtUser.Rows[0]["User_Name"].ToString());
                        SetJWTTokenAsCookie(jwtToken);

                        // Log successful login
                        _logger.LogInformation("User successfully logged in: {u}", dtUser.Rows[0]["User_Name"]);

                        return Ok(new
                        {
                            StatusCode = "200",
                            Token = jwtToken,
                            UserName = dtUser.Rows[0]["User_Name"].ToString(),
                            Authentication_Rule = lstAuthRules,
                            WareHouse_Rule = lstWhsRules,
                            SeriesList = lstSeries
                        });
                    }
                    else
                    {
                        _logger.LogError("User details not found.");
                        return BadRequest(new { StatusCode = "400", StatusMsg = "No such user exist : " + payload.User_Name });
                    }
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : LoginPost Error : " + ex.ToString());
                _logger.LogError("Exception during user login: {e}", ex.Message);
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message });
            }
        }


        [HttpGet("get-jwt-cookie")]
        public IActionResult GetJwtCookie()
        {
            try
            {
                bool tokenbool = Request.Cookies.TryGetValue("jwt", out var token);
                if (token != null)
                {
                    return Ok(new { JwtToken = token });
                }
                return NotFound("JWT Token cookie not found");
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : GetJwtCookie Error : " + ex.ToString());
                _logger.LogError(" GetJwtCookie Error : {e}", ex.Message);
                return NotFound(ex.Message.ToString());
            }
        }


        [HttpGet("validate_jwt")]
        [Authorize]
        public async Task<ActionResult> Validate_jwt()
        {
            try
            {
                return Ok(new { StatusCode = "200", Message = "valid token" });
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : Validate_jwt Error : " + ex.ToString());
                _logger.LogError(" validate_jwt Error : {e}", ex.Message.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("SaveAuthRule")]
        [Authorize]
        public async Task<ActionResult<getDataClass>> SaveAuthRule([FromBody] getDataClass payload)
        {
            try
            {
                QITcon = new SqlConnection(_QIT_connection);
                await QITcon.OpenAsync();

                dynamic arData = JsonConvert.SerializeObject(payload.moduleCLasses);

                _Query = @"MERGE INTO " + Global.QIT_DB + @".dbo.QIT_Authentication_Rule AS Target
                USING (SELECT @User_ID AS User_ID, @Authentication_Rule_Details AS Authentication_Rule_Details) AS Source
                ON Target.User_ID = Source.User_ID
                WHEN MATCHED THEN
                    UPDATE SET Authentication_Rule_Details = Source.Authentication_Rule_Details
                WHEN NOT MATCHED THEN
                    INSERT (User_ID, Authentication_Rule_Details)
                    VALUES (Source.User_ID, Source.Authentication_Rule_Details);";

                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@User_ID", payload.User_ID);
                    cmd.Parameters.AddWithValue("@Authentication_Rule_Details", arData);

                    int insertCount = cmd.ExecuteNonQuery();
                    QITcon.Close();
                    if (insertCount > 0)
                        return Ok(new { StatusCode = "200", IsSaved = "Y", StatusMsg = "Saved Successfully!!!" });
                }
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = "Unable to save auth rule" });
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : SaveAuthRule Error : " + ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpGet("GetUser")]
        public async Task<ActionResult<IEnumerable<User>>> GetUser(string? username)
        {
            try
            {
                List<User> obj = new();
                DataTable dtUser = new();

                string search = null;
                if (username != null)
                {
                    search = " And User_Name='" + username + "'";
                }

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE 1 = 1 " + search;
                _logger.LogInformation(" AuthUserController : GetUser() Query : {q} ", _Query.ToString());
                using (oAdptr = new SqlDataAdapter(_Query, QITcon))
                {
                    await QITcon.OpenAsync();
                    oAdptr.Fill(dtUser);
                    QITcon.Close();
                }

                if (dtUser.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtUser);
                    obj = JsonConvert.DeserializeObject<List<User>>(arData.ToString());
                    return obj;
                }
                else
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : GetUser Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : Get() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveUser([FromBody] User payload)
        {
            try
            {
                _logger.LogInformation(" Calling AuthUserController : SaveUser() ");

                if (payload != null)
                {
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(payload.User_Password);
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"insert into " + Global.QIT_DB + @".dbo.QIT_User_Master(User_Name, User_Email, User_Password, Mobile_No,Gender,Department) 
                           VALUES (@User_Name, @User_Email, @User_Password, @Mobile_No,@Gender, @Department)";
                    _logger.LogInformation(" AuthUserController : SaveUser() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@User_Name", payload.User_Name);
                    cmd.Parameters.AddWithValue("@User_Email", payload.User_Email);
                    cmd.Parameters.AddWithValue("@User_Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Mobile_No", payload.Mobile_No.ToString());
                    cmd.Parameters.AddWithValue("@Gender", payload.Gender);
                    cmd.Parameters.AddWithValue("@Department", payload.Department);

                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        return Ok(new { StatusCode = "200", IsSaved = "Y", StatusMsg = "Saved Successfully!!!" });
                    else
                        return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = "Unable to save" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : SaveUser Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : SaveUser() :: {ex}", ex.ToString());
                if (ex.Message.ToString().Contains("UQ_User_Name"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = "User Name already exist" });
                }
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPut("Edit")]
        public IActionResult EditUser([FromBody] EditUser payload)
        {
            try
            {
                _logger.LogInformation(" Calling AuthUserController : EditUser() ");

                if (payload != null)
                {
                    DataTable dtUser = new();
                    QITcon = new SqlConnection(_QIT_connection);

                    // Check if the user exists
                    _Query = @"SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @User_Name";
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@User_Name", payload.User_Name);
                    oAdptr.Fill(dtUser);
                    QITcon.Close();

                    if (dtUser.Rows.Count > 0)
                    {
                        string _where = string.Empty;
                        int flag = 0;
                        if (payload.Old_Password != null && payload.Old_Password != string.Empty)
                        {
                            bool isPwdMatched = BCrypt.Net.BCrypt.Verify(payload.Old_Password, dtUser.Rows[0]["User_Password"].ToString());
                            if (!isPwdMatched)
                            {
                                return BadRequest(new { StatusCode = "400", StatusMsg = "Incorrect password." });
                            }
                            else
                            {
                                flag = 1;
                                _where = ", User_Password = @UserPassword ";
                            }
                        }

                        if (payload.ProfilePicture != null)
                        {
                            _where = ", ProfilePicture = @ProfilePicture ";
                        }

                        _Query = @"
                        UPDATE " + Global.QIT_DB + @".dbo.QIT_User_Master
                        SET User_Email = @User_Email,
                            Mobile_No = @Mobile_No,
                            Gender = @Gender
                            " + _where + @"
                        WHERE User_Name = @User_Name";

                        _logger.LogInformation(" AuthUserController : EditUser() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@User_Name", payload.User_Name);
                        cmd.Parameters.AddWithValue("@User_Email", payload.User_Email);
                        cmd.Parameters.AddWithValue("@Mobile_No", payload.Mobile_No.ToString());
                        cmd.Parameters.AddWithValue("@Gender", payload.Gender);
                        if (payload.ProfilePicture != null)
                        {
                            cmd.Parameters.AddWithValue("@ProfilePicture", payload.ProfilePicture);
                        }
                        if (flag == 1)
                        {
                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(payload.New_Password);
                            cmd.Parameters.AddWithValue("@UserPassword", hashedPassword);
                        }


                        QITcon.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();

                        if (intNum > 0)
                            return Ok(new { StatusCode = "200", IsUpdated = "Y", StatusMsg = "Updated Successfully!!!" });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsUpdated = "N", StatusMsg = "User does not exist" });
                    }
                    return BadRequest(new { StatusCode = "400", IsUpdated = "N", StatusMsg = "Unable to update" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsUpdated = "N", StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : EditUser Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : EditUser() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsUpdated = "N", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPut("ChangePassword")]
        public IActionResult ChangePassword([FromBody] ChangePassword payload)
        {
            string _IsUpdated = "N";
            try
            {
                _logger.LogInformation(" Calling AuthUserController : ChangePassword() ");


                if (payload != null)
                {
                    QITcon = new SqlConnection(_QIT_connection);

                    // Check if the user exists
                    _Query = @" SELECT COUNT(*) FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @User_Name";
                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@User_Name", payload.User_Name);

                    QITcon.Open();
                    int userCount = (int)cmd.ExecuteScalar();
                    QITcon.Close();

                    if (userCount > 0)
                    {
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(payload.User_Password);

                        _Query = @"UPDATE " + Global.QIT_DB + @".dbo.QIT_User_Master
                      SET User_Password = @User_Password
                      WHERE User_Name = @User_Name";

                        _logger.LogInformation(" AuthUserController : ChangePassword() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@User_Password", hashedPassword);
                        cmd.Parameters.AddWithValue("@User_Name", payload.User_Name);

                        QITcon.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();


                        if (intNum > 0)
                            _IsUpdated = "Y";
                    }
                    else
                    {
                        // User does not exist
                        return BadRequest(new { StatusCode = "400", IsUpdated = _IsUpdated, StatusMsg = "User does not exist" });
                    }

                    return Ok(new { StatusCode = "200", IsUpdated = _IsUpdated, StatusMsg = "Password Updated Successfully!!!" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsUpdated = _IsUpdated, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : ChangePassword Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : ChangePassword() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsUpdated = _IsUpdated, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("SaveWarehouseRule")]
        [Authorize]
        public IActionResult SaveWarehouseRule([FromBody] WarehouseRule payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation("Calling AuthUserController: SaveUser()");

                if (payload != null)
                {
                    using (QITcon = new SqlConnection(_QIT_connection))
                    {
                        int intNum = 0;
                        QITcon.Open();

                        // Check if a record with the given Warehouse_Code exists
                        _Query = @"SELECT COUNT(*) FROM " + Global.QIT_DB + @".dbo.QIT_WarehouseRule_Master WHERE Warehouse_Code = @Warehouse_Code";
                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@Warehouse_Code", payload.Warehouse_Code);
                        int count = (int)cmd.ExecuteScalar();

                        if (count > 0)
                        {
                            // If the record exists, update it
                            _Query = @"
                            UPDATE " + Global.QIT_DB + @".dbo.QIT_WarehouseRule_Master 
                            SET Warehouse_Name = @Warehouse_Name, Warehouse_Location = @Warehouse_Location,  
                                Warehouse_binActivat = @Warehouse_binActivat, User_details = @User_details  
                            WHERE Warehouse_Code = @Warehouse_Code";

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@Warehouse_Name", payload.Warehouse_Name);
                            cmd.Parameters.AddWithValue("@Warehouse_Location", payload.Warehouse_Location);
                            cmd.Parameters.AddWithValue("@Warehouse_binActivat", payload.Warehouse_binActivat);
                            cmd.Parameters.AddWithValue("@User_details", JsonConvert.SerializeObject(payload.User_Details));
                            cmd.Parameters.AddWithValue("@Warehouse_Code", payload.Warehouse_Code);
                            intNum = cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            // If the record doesn't exist, insert it
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_WarehouseRule_Master (Warehouse_Code, Warehouse_Name, Warehouse_Location, Warehouse_binActivat, User_details) 
                            VALUES (@Warehouse_Code, @Warehouse_Name, @Warehouse_Location, @Warehouse_binActivat, @User_details) ";

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@Warehouse_Code", payload.Warehouse_Code);
                            cmd.Parameters.AddWithValue("@Warehouse_Name", payload.Warehouse_Name);
                            cmd.Parameters.AddWithValue("@Warehouse_Location", payload.Warehouse_Location);
                            cmd.Parameters.AddWithValue("@Warehouse_binActivat", payload.Warehouse_binActivat);
                            cmd.Parameters.AddWithValue("@User_details", JsonConvert.SerializeObject(payload.User_Details));
                            intNum = cmd.ExecuteNonQuery();
                        }

                        QITcon.Close();
                        if (intNum > 0)
                            return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                        else
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Unable to save" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : SaveWarehouseRule Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : SaveWarehouseRule() :: {ex}", ex.ToString());
                if (ex.Message.ToString().ToLower().Contains("uq_user_name"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "User Name already exists" });
                }
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("GetWarehouseRule")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserBindWithWarehouse>>> GetWarehouseRule([FromBody] GetWarehouseRule payload)
        {
            try
            {
                if (payload.Warehouse_Code == null || payload.Warehouse_Code == "")
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "Warehouse code is required" });
                }

                List<UserBindWithWarehouse> obj = new();
                WarehouseRule rule = new();
                DataTable dtU = new();
                DataTable dtW = new();

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @"select Warehouse_Code,User_Details from " + Global.QIT_DB + @".dbo.QIT_WarehouseRule_Master where Warehouse_Code=@whsCode";
                QITcon.Open();
                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@whsCode", payload.Warehouse_Code); // Add the whsCode parameter
                    oAdptr = new SqlDataAdapter(cmd);
                    oAdptr.Fill(dtW);
                    QITcon.Close();

                }

                if (dtW.Rows.Count > 0)
                {
                    rule.User_Details = JsonConvert.DeserializeObject<List<int>>(dtW.Rows[0]["User_Details"].ToString());
                }
                _Query = @"select * from " + Global.QIT_DB + @".dbo.QIT_User_Master";
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtU);
                QITcon.Close();

                dynamic arData = JsonConvert.SerializeObject(dtU);
                obj = JsonConvert.DeserializeObject<List<UserBindWithWarehouse>>(arData.ToString());


                if (dtU.Rows.Count > 0)
                {
                    if (dtW.Rows.Count > 0)
                    {
                        foreach (var user in obj)
                        {
                            user.IsBind = rule.User_Details.Contains(user.User_ID);
                        }
                    }
                    return Ok(obj);
                }
                else
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : GetWarehouseRule Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : GetWarehouseRule() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpGet("GetWarehousebyUser")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<GetWarehouseForUser>>> GetWarehousebyUser()
        {
            try
            {
                string UserId = String.Empty;

                if (HttpContext.Request.Headers.TryGetValue("UserId", out var headerValue))
                {
                    UserId = headerValue.ToString();
                }

                if (UserId == null || string.IsNullOrEmpty(UserId) || UserId.Length <= 0)
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "UserId is required" });
                }

                List<GetWarehouseForUser> obj = new();
                DataTable dtW = new();

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @"SELECT Warehouse_Code, Warehouse_Name FROM " + Global.QIT_DB + @".dbo.QIT_WarehouseRule_Master WHERE (',' +(SELECT REPLACE(REPLACE(User_details, '[', ''), ']', '') + ',') + ',') LIKE @userId";
                await QITcon.OpenAsync();
                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@userId", "%," + UserId + ",%");
                    oAdptr = new SqlDataAdapter(cmd);
                    oAdptr.Fill(dtW);
                    QITcon.Close();
                }

                dynamic arData = JsonConvert.SerializeObject(dtW);
                obj = JsonConvert.DeserializeObject<List<GetWarehouseForUser>>(arData.ToString());

                if (dtW.Rows.Count > 0)
                {
                    return Ok(obj);
                }
                else
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : GetWarehousebyUser Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : GetWarehousebyUser() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("GetAuthRule")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetAuthRule([FromBody] getUserAuthRule user)
        {
            try
            {
                if (user == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty..!!" });
                }
                else
                {
                    DataTable dtData = new();

                    QITcon = new SqlConnection(_QIT_connection);

                    List<getModuleClass> obj = new();

                    int UID = user.User_ID;
                    _Query = @" select Authentication_Rule_Details from " + Global.QIT_DB + @".dbo.QIT_Authentication_Rule where User_ID = @uid"; // + UID;
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@uid", UID);
                    oAdptr.Fill(dtData);
                    QITcon.Close();
                    if (dtData.Rows.Count > 0)
                    {
                        obj = JsonConvert.DeserializeObject<List<getModuleClass>>(dtData.Rows[0]["Authentication_Rule_Details"].ToString());
                        return Ok(new { StatusCode = "200", Authentication_Rule = obj });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Data not found" });
                    }
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : GetAuthRule Error : " + ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        #region Methods

        private string GenerateJWTToken(string username)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1), // Set expiration to 1 day from the current time
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : GenerateJWTToken Error : " + ex.ToString());
                _logger.LogError(" GenerateJWTToken Error : {ex}", ex.Message);
                return ex.Message.ToString();
            }
        }

        private void SetJWTTokenAsCookie(string token)
        {
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Set to true if your site uses HTTPS
                    SameSite = SameSiteMode.Lax,
                };

                Response.Cookies.Append("jwt", token, cookieOptions);
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("AuthUserController : SetJWTTokenAsCookie Error : " + ex.ToString());
                _logger.LogError(" SetJWTTokenAsCookie Error : {ex}", ex.Message);
                Response.Cookies.Append("jwt", token, null);
            }
        }

        #endregion  

    }
}
