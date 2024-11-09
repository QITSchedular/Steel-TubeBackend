using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ST_Production.Common;
using ST_Production.Hubs;
using ST_Production.Models;
using System.Data;
using System.Data.SqlClient;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationMasterController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;

        private SqlCommand cmd;
        private SqlConnection QITcon;
        private SqlDataAdapter oAdptr;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<NotificationMasterController> _logger;

        public NotificationMasterController(IConfiguration configuration, ILogger<NotificationMasterController> logger, IHubContext<NotificationHub> hubContext)
        {
            objGlobal ??= new Global();
            _logger = logger;
            try
            {
                _hubContext = hubContext;
                Configuration = configuration;
                _QIT_connection = Configuration["connectApp:QITConnString"];

                Global.QIT_DB = "[" + Configuration["QITDB"] + "]";
                Global.SAP_DB = "[" + Configuration["CompanyDB"] + "]";
                Global.gLogPath = Configuration["LogPath"];
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in NotificationMasterController :: " + ex.ToString());
                _logger.LogError(" Error in NotificationMasterController :: {ex}" + ex.ToString());
            }
        }


        [HttpPost]
        public async Task<ActionResult<IEnumerable<NotificationMasterClass>>> Post(NotificationMasterClass payload)
        {
            string _IsSaved = "N";
            try
            {
                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty..!!" });
                }

                QITcon = new SqlConnection(_QIT_connection);
                QITcon.Open();

                if (payload.Module == string.Empty)
                {
                    _logger.LogError("Error : Notification Module is required..!!");
                    return BadRequest(new { StatusCode = "403", IsSaved = _IsSaved, StatusMsg = "Notification Module is required..!!" });
                }
                List<int> userIds = new List<int>();

                using (cmd = new SqlCommand("SP_SearchUserByModule", QITcon))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@searchedText", SqlDbType.NVarChar, 100) { Value = payload.Module });
                    cmd.Parameters.Add(new SqlParameter("@applicationValue", SqlDbType.NVarChar, 100) { Value = true });

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            userIds.Add(userId);
                        }
                    }
                }

                _Query = @" SELECT User_Id FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @UserName ";

                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@UserName", payload.Sender_User_Name);
                    int Sender_User_ID = (int)cmd.ExecuteScalar();

                    List<Notification_Get_Class> newEntity_notifications = new List<Notification_Get_Class>();
                    foreach (int user_id in userIds)
                    {
                        _Query = @"
                        INSERT INTO " + Global.QIT_DB + @".dbo.QIT_Notification_Master 
                        (Sender_User_Id, Receiver_User_Id, Notification_Text, N_Date_Time, Chk_Status) 
                        OUTPUT INSERTED.N_Id VALUES (@Sender_User_Id, @Receiver_User_Id, @Notification_Text, @N_Date_Time, @Chk_Status)";

                        using (cmd = new SqlCommand(_Query, QITcon))
                        {
                            cmd.Parameters.AddWithValue("@Sender_User_Id", Sender_User_ID);
                            cmd.Parameters.AddWithValue("@Notification_Text", payload.Notification_Text);
                            cmd.Parameters.AddWithValue("@N_Date_Time", payload.N_Date_Time);
                            cmd.Parameters.AddWithValue("@Chk_Status", payload.Chk_Status);
                            cmd.Parameters.AddWithValue("@Receiver_User_Id", user_id);

                            int nId = (int)cmd.ExecuteScalar();
                            _Query = @" SELECT User_Name FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Id = @UserId ";

                            using (cmd = new SqlCommand(_Query, QITcon))
                            {
                                cmd.Parameters.AddWithValue("@UserId", user_id);

                                string userName = cmd.ExecuteScalar() as string;
                                var newEntity = new Notification_Get_Class
                                {
                                    N_Id = nId,
                                    Notification_Text = payload.Notification_Text,
                                    Chk_Status = "0",
                                    timeLimit = GetHumanReadableTimeDifference(payload.N_Date_Time)
                                };
                                newEntity_notifications.Add(newEntity);
                                _logger.LogInformation("Notification Added successfully for userName..", userName);
                                await _hubContext.Clients.Group(userName).SendAsync("newEntryAdded", newEntity_notifications);
                            }
                        }
                    }
                }
                QITcon.Close();
                _logger.LogInformation("Notification Added successfully.. ");

                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Notification Added successfully.." });
            }
            catch (SqlException ex)
            {
                objGlobal.WriteLog("NotificationMasterController : Post Error : " + ex.ToString());
                if (ex.Number == 547)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Invalid user.Please provide a valid user ID." });
                }
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("NotificationMasterController : Post Error : " + ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        static string GetHumanReadableTimeDifference(DateTime timestamp)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan timeDifference = currentTime - timestamp;

                if (timeDifference.TotalSeconds < 60)
                {
                    return $"{(int)timeDifference.TotalSeconds} seconds ago";
                }
                else if (timeDifference.TotalMinutes < 60)
                {
                    return $"{(int)timeDifference.TotalMinutes} minutes ago";
                }
                else if (timeDifference.TotalHours < 24)
                {
                    return $"{(int)timeDifference.TotalHours} hours ago";
                }
                else if (timeDifference.TotalDays < 30) // Approximation for a month
                {
                    return $"{(int)timeDifference.TotalDays} days ago";
                }
                else if (timeDifference.TotalDays < 365) // Approximation for a year
                {
                    int months = (int)(timeDifference.TotalDays / 30);
                    return $"{months} {(months == 1 ? "month" : "months")} ago";
                }
                else
                {
                    int years = (int)(timeDifference.TotalDays / 365);
                    return $"{years} {(years == 1 ? "year" : "years")} ago";
                }
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }


        [HttpGet("GetALlNotification")]
        public async Task<IEnumerable<testclass>> GetAllNotification(string userName)
        {
            try
            {
                _Query = @"
                SELECT N_Id, Notification_Text, N_Date_Time, Chk_Status 
                FROM " + Global.QIT_DB + @".dbo.QIT_Notification_Master 
                WHERE Receiver_User_Id = (select User_ID from " + Global.QIT_DB + @".dbo.QIT_User_Master where User_Name='{userName}')ORDER BY N_Date_Time DESC";
                int unread_Cnt = 0;
                List<Notification_Get_Class> notifications = new List<Notification_Get_Class>();
                using (QITcon = new SqlConnection(_QIT_connection))
                {
                    await QITcon.OpenAsync();

                    using (oAdptr = new SqlDataAdapter(_Query, QITcon))
                    {
                        DataTable dt = new(); ;
                        oAdptr.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            notifications = dt.AsEnumerable().Select(item => new Notification_Get_Class
                            {
                                N_Id = item.Field<int>("N_Id"),
                                Notification_Text = item.Field<string>("Notification_Text"),
                                timeLimit = GetHumanReadableTimeDifference(item.Field<DateTime>("N_Date_Time")),
                                Chk_Status = item.Field<string>("Chk_Status")
                            }).ToList();
                            unread_Cnt = notifications.Where(item => item.Chk_Status == "0").Count();
                            List<testclass> data = new List<testclass>();
                            data.Add(new testclass { data = notifications, dataCount = unread_Cnt });
                            return data;
                        }
                    }
                }
                return Enumerable.Empty<testclass>();
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("NotificationMasterController : GetAllNotification Error : " + ex.ToString());
                _logger.LogError(" Error in NotificationMasterController : GetAllNotification() :: {ex}", ex.ToString());
                return Enumerable.Empty<testclass>();
            }
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification_Get_Class>>> Get(int? id)
        {
            try
            {
                if (id == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "User ID is empty..!!" });
                }

                _Query = @"
                SELECT N_Id, Notification_Text, N_Date_Time, Chk_Status 
                FROM " + Global.QIT_DB + @".dbo.QIT_Notification_Master 
                WHERE Receiver_User_Id = @id 
                ORDER BY N_Date_Time desc";

                List<Notification_Get_Class> notifications = new List<Notification_Get_Class>();
                DataTable dtData = new(); ;
                int unread_Cnt = 0;
                using (QITcon = new SqlConnection(_QIT_connection))
                {
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@id", id);
                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        notifications = dtData.AsEnumerable().Select(item => new Notification_Get_Class
                        {
                            N_Id = item.Field<int>("N_Id"),
                            Notification_Text = item.Field<string>("Notification_Text"),
                            timeLimit = GetHumanReadableTimeDifference(item.Field<DateTime>("N_Date_Time")),
                            Chk_Status = item.Field<string>("Chk_Status")
                        }).ToList();
                        unread_Cnt = notifications.Where(item => item.Chk_Status == "0").Count();
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Data not found" });
                    }
                }
                var data = new { n_Data = notifications, UnRead = unread_Cnt };

                return Ok(new { StatusCode = "200", data });
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("NotificationMasterController : Get Error : " + ex.ToString());
                _logger.LogError("Error in NotificationMasterController : Get() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("updateNotificationStatus")]
        public async Task<ActionResult> updateNotificationStatus(Notification_Update_Status data)
        {
            string _IsSaved = "N";
            try
            {
                if (data == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "payload is empty..!!" });
                }
                QITcon = new SqlConnection(_QIT_connection);
                await QITcon.OpenAsync();
                _Query = @"update " + Global.QIT_DB + @".dbo.QIT_Notification_Master set Chk_Status = 1 where N_Id = @NotificationID;";

                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@NotificationID", data.N_Id);
                    cmd.ExecuteNonQuery();
                    int updateCount = cmd.ExecuteNonQuery();
                    if (updateCount > 0)
                        _IsSaved = "Y";
                }
                if (_IsSaved == "Y")
                {
                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Status Updated Successfully..!!" });
                }
                else
                {
                    return Ok(new { StatusCode = "404", IsSaved = _IsSaved, StatusMsg = "Updated unsuccessfully..!!" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("NotificationMasterController : updateNotificationStatus Error : " + ex.ToString());
                _logger.LogError("Error in NotificationMasterController : updateNotificationStatus() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("readAllNotificationStatus")]
        public async Task<ActionResult> readAllNotificationStatus(Notification_readAll_Status data)
        {
            string _IsSaved = "N";
            try
            {
                if (data == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "payload is empty..!!" });
                }
                QITcon = new SqlConnection(_QIT_connection);
                QITcon.Open();
                _Query = @"
                UPDATE " + Global.QIT_DB + @".dbo.QIT_Notification_Master 
                SET Chk_Status = 1 WHERE Receiver_User_Id = (select User_ID from " + Global.QIT_DB + @".dbo.QIT_User_Master where User_Name=@UserName)";

                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@UserName", data.Username);
                    int updateCount = cmd.ExecuteNonQuery();
                    if (updateCount > 0)
                        _IsSaved = "Y";
                }
                if (_IsSaved == "Y")
                {
                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Status Updated Successfully..!!" });
                }
                else
                {
                    return Ok(new { StatusCode = "404", IsSaved = _IsSaved, StatusMsg = "Updated unsuccessfully..!!" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("NotificationMasterController : readAllNotificationStatus Error : " + ex.ToString());
                _logger.LogError("Error in NotificationMasterController : Get() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

    }
}
