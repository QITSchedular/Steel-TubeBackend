using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ST_Production.Common;
using ST_Production.Models;
using System.Data;
using System.Data.SqlClient;
using ValidationRule = ST_Production.Models.ValidationRule;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValidationRuleController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;

        private SqlConnection QITcon;

        private SqlDataAdapter oAdptr;
        private SqlCommand cmd;
        public Global objGlobal;

        private readonly ILogger<ValidationRuleController> _logger;
        public IConfiguration Configuration { get; }


        public ValidationRuleController(IConfiguration configuration, ILogger<ValidationRuleController> logger)
        {
            objGlobal ??= new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
                _QIT_connection = Configuration["connectApp:QITConnString"];

                Global.QIT_DB = "[" + Configuration["QITDB"] + "]";
                Global.SAP_DB = "[" + Configuration["CompanyDB"] + "]";
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in ValidationRuleController :: " + ex.ToString());
                _logger.LogError(" Error in ValidationRuleController :: {ex} ", ex.ToString());
            }
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetValidationMaster>>> Get()
        {
            try
            {
                List<GetValidationMaster> obj = new();
                DataTable dtData = new(); ;
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" select * from " + Global.QIT_DB + @".dbo.QIT_Validation_Master ";
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();
                if (dtData.Rows.Count > 0)
                {
                    dynamic arObj = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GetValidationMaster>>(arObj);
                    return Ok(obj);
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Data not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ValidationRuleController : Get Error : " + ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("SaveValidationMaster")]
        public async Task<ActionResult> Post([FromBody] GetValidationMaster payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ValidationRuleController : SaveValidationMaster() ");

                if (payload != null)
                {
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                    INSERT INTO " + Global.QIT_DB + @".dbo.QIT_Validation_Master(Validation_Name, Modules, Filter_Type, Condition,Comparision_Value,N_Rule_ID,Message) 
                    VALUES (@Validation_Name, @Modules, @Filter_Type, @Condition,@Comparision_Value, @N_Rule_ID,@Message)";
                    _logger.LogInformation(" ValidationRuleController : SaveValidationMaster() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@Validation_Name", payload.Validation_Name);
                    cmd.Parameters.AddWithValue("@Modules", payload.Modules);
                    cmd.Parameters.AddWithValue("@Filter_Type", payload.Filter_Type);
                    cmd.Parameters.AddWithValue("@Condition", payload.Condition);
                    cmd.Parameters.AddWithValue("@Comparision_Value", payload.Comparision_Value);
                    cmd.Parameters.AddWithValue("@N_Rule_ID", payload.N_Rule_ID);
                    cmd.Parameters.AddWithValue("@Message", payload.Message);

                    await QITcon.OpenAsync();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Data can not be empty", isSaved = _IsSaved });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ValidationRuleController : SaveValidationMaster Error : " + ex.ToString());
                _logger.LogError("Error in AuthUserController : SaveValidationMaster() :: {ex}", ex.ToString());
                if (ex.ToString().ToLower().Contains("uq_validation_name"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Validation Rule Name already exist" });
                }
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString(), isSaved = _IsSaved });
            }
        }


        [HttpPost("SaveValidationRuleMaster")]
        public async Task<ActionResult> SaveValidationRuleMaster([FromBody] ValidationRule payload)
        {

            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ValidationRuleController : SaveValidationRuleMaster() ");

                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Data can not be empty", isSaved = _IsSaved });
                }

                QITcon = new SqlConnection(_QIT_connection);
                await QITcon.OpenAsync();

                _Query = @"MERGE INTO " + Global.QIT_DB + @".dbo.QIT_ValidationRule_Master AS Target
                USING (SELECT @Validation_Master_Id AS Validation_Master_Id, @User_details AS User_details) AS Source
                ON Target.Validation_Master_Id = Source.Validation_Master_Id
                WHEN MATCHED THEN
                    UPDATE SET User_details = Source.User_details
                WHEN NOT MATCHED THEN
                    INSERT (Validation_Master_ID, User_details)
                    VALUES (Source.Validation_Master_ID, Source.User_details);";

                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@Validation_Master_ID", payload.Validation_Master_ID);
                    cmd.Parameters.AddWithValue("@User_details", JsonConvert.SerializeObject(payload.User_Details));
                    _logger.LogInformation(" ValidationRuleController : SaveValidationRuleMaster() Query : {q} ", _Query.ToString());
                    int insertCount = cmd.ExecuteNonQuery();
                    if (insertCount > 0)
                        _IsSaved = "Y";
                }

                QITcon.Close();
                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ValidationRuleController : SaveValidationRuleMaster Error : " + ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("GetValidationRule")]
        public async Task<ActionResult<IEnumerable<ValidationRule>>> GetValidationRule([FromBody] GetValidationRule payload)
        {
            try
            {
                if (payload.Validation_Master_ID == null || payload.Validation_Master_ID == 0)
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "Validation rule id is required" });
                }

                List<GetValidationRuleUsers> obj = new();
                DataTable dtV = new(); ;
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @"select Validation_Master_ID,User_Details from " + Global.QIT_DB + @".dbo.QIT_ValidationRule_Master where Validation_Master_ID=@Validation_Master_ID";
                await QITcon.OpenAsync();
                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    cmd.Parameters.AddWithValue("@Validation_Master_ID", payload.Validation_Master_ID);
                    oAdptr = new SqlDataAdapter(cmd);
                    oAdptr.Fill(dtV);
                    QITcon.Close();
                }

                ValidationRule rule = new();
                if (dtV.Rows.Count > 0)
                {
                    rule.User_Details = JsonConvert.DeserializeObject<List<int>>(dtV.Rows[0]["User_Details"].ToString());
                }

                _Query = @"select User_ID,Authentication_Rule_Details from " + Global.QIT_DB + @".dbo.QIT_Authentication_Rule";
                QITcon.Open();
                DataTable dtA = new(); ;
                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    oAdptr = new SqlDataAdapter(cmd);
                    oAdptr.Fill(dtA);
                    QITcon.Close();
                }

                List<int> userIds = dtA.AsEnumerable()
                 .Where(row => row["Authentication_rule_details"] != DBNull.Value)
                 .Select(row =>
                 {
                     getDataClass module = new getDataClass
                     {
                         User_ID = Convert.ToInt32(row["User_ID"]),
                         moduleCLasses = JsonConvert.DeserializeObject<List<getModuleClass>>(row["Authentication_rule_details"].ToString())
                     };
                     return module;
                 })
                 .Where(module => module.moduleCLasses != null &&
                                  module.moduleCLasses.Any(subModule =>
                                     subModule.items != null &&
                                     subModule.items.Any(subItem =>
                                         subItem.text == payload.Modules &&
                                         subItem.rightsAccess != null &&
                                         subItem.rightsAccess.Contains("T"))))
                 .Select(module => module.User_ID)
                 .ToList();

                if (userIds.Count > 0)
                {
                    _Query = @" SELECT User_ID, User_Name FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_ID IN (" + string.Join(',', userIds) + ") ";
                    using (cmd = new SqlCommand(_Query, QITcon))
                    {
                        DataTable dtU = new(); ;
                        using (oAdptr = new SqlDataAdapter(cmd))
                        {
                            oAdptr.Fill(dtU);
                        }

                        if (dtU.Rows.Count > 0)
                        {
                            List<GetValidationRuleUsers> objOfUsers = new();
                            dynamic arData = JsonConvert.SerializeObject(dtU);
                            objOfUsers = JsonConvert.DeserializeObject<List<GetValidationRuleUsers>>(arData.ToString());

                            if (rule.User_Details.Count > 0)
                            {
                                foreach (var user in rule.User_Details)
                                {
                                    objOfUsers.Where(item => item.User_ID == user).ToList().ForEach(o => o.IsBind = true);
                                }
                                return Ok(objOfUsers);
                            }
                            else
                            {
                                return Ok(objOfUsers);
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", StatusMsg = "No user data found " });
                        }
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No user have access of " + payload.Modules + " " });
                }
                //return Ok(userIds);
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ValidationRuleController : GetValidationRule Error : " + ex.ToString());
                _logger.LogError("Error in ValidationRuleController : GetValidationRule() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }
    }
}
