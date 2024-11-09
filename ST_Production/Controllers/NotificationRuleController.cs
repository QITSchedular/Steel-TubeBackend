using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ST_Production.Common;
using ST_Production.Models;
using System.Data;
using System.Data.SqlClient;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationRuleController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        private SqlConnection QITcon;
        private SqlDataAdapter oAdptr;
        public Global objGlobal;
        public IConfiguration Configuration { get; }
        private readonly ILogger<NotificationRuleController> _logger;


        public NotificationRuleController(IConfiguration configuration, ILogger<NotificationRuleController> logger)
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
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in NotificationRuleController :: " + ex.ToString());
                _logger.LogError(" Error in NotificationRuleController :: {ex}" + ex.ToString());
            }
        }


        [HttpGet]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<getNotificationModuleClass>>> Get(int? id)
        {
            try
            {
                if (id == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "User ID is empty..!!" });
                }

                DataTable dtData = new(); ;
                _Query = @" select * from " + Global.QIT_DB + @".dbo.QIT_Notification_Rule where User_ID = @uid";
                QITcon = new SqlConnection(_QIT_connection);
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@uid", id);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<getNotificationModuleClass> obj = JsonConvert.DeserializeObject<List<getNotificationModuleClass>>(dtData.Rows[0]["N_Rule_Details"].ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Data not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("NotificationRuleController : Get Error : " + ex.ToString());
                _logger.LogError("Error in NotificationRuleController : Get() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<NotificationRule>>> Post(NotificationRule nRule)
        {
            string _IsSaved = "N";
            try
            {
                if (nRule == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty..!!" });
                }

                QITcon = new SqlConnection(_QIT_connection);
                await QITcon.OpenAsync();
                dynamic nRuleData = JsonConvert.SerializeObject(nRule.N_Rule_Details);
                _Query = @"MERGE INTO " + Global.QIT_DB + @".dbo.QIT_Notification_Rule AS Target
                USING (SELECT @User_ID AS User_ID, @N_Rule_Details AS N_Rule_Details) AS Source
                ON Target.User_ID = Source.User_ID
                WHEN MATCHED THEN
                    UPDATE SET N_Rule_Details = Source.N_Rule_Details
                WHEN NOT MATCHED THEN
                    INSERT (User_ID, N_Rule_Details)
                    VALUES (Source.User_ID, Source.N_Rule_Details);";

                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@User_ID", nRule.User_ID);
                    cmd.Parameters.AddWithValue("@N_Rule_Details", nRuleData);
                    int insertCount = cmd.ExecuteNonQuery();
                    if (insertCount > 0)
                        _IsSaved = "Y";
                }
                QITcon.Close();
                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });

            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("NotificationRuleController : Post Error : " + ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

    }
}
