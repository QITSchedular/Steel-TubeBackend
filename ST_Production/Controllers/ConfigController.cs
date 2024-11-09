using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ST_Production.Common;
using ST_Production.Models;
using System.Data.SqlClient;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;

        private string _Query = string.Empty;

        private SqlConnection QITcon;
        private SqlDataAdapter oAdptr;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ConfigController> _logger;

        public ConfigController(IConfiguration configuration, ILogger<ConfigController> logger)
        {
            objGlobal ??= new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
                _connection = Configuration["connectApp:ConnString"];
                _QIT_connection = Configuration["connectApp:QITConnString"];

                Global.QIT_DB = "[" + Configuration["QITDB"] + "]";
                Global.SAP_DB = "[" + Configuration["CompanyDB"] + "]";
                Global.gLogPath = Configuration["LogPath"];

                objGlobal.gServer = Configuration["Server"];
                objGlobal.gSqlVersion = Configuration["SQLVersion"];
                objGlobal.gCompanyDB = Configuration["CompanyDB"];
                objGlobal.gLicenseServer = Configuration["LicenseServer"];
                objGlobal.gSAPUserName = Configuration["SAPUserName"];
                objGlobal.gSAPPassword = Configuration["SAPPassword"];
                objGlobal.gDBUserName = Configuration["DBUserName"];
                objGlobal.gDBPassword = Configuration["DbPassword"];

            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in ConfigController :: " + ex.ToString());
                _logger.LogError(" Error in ConfigController :: {ex}" + ex.ToString());
            }
        }


        [HttpGet("GetConfig")]
        public async Task<ActionResult<IEnumerable<GetConfig>>> GetConfig()
        {
            try
            {
                _logger.LogInformation(" Calling ConfigsController : GetConfig() ");
                List<GetConfig> obj = new List<GetConfig>();
                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT  A.Id, A.PriceListNum, B.ListName PriceListName
                FROM " + Global.QIT_DB + @".dbo.QIT_Config_Master A
                     LEFT JOIN " + Global.SAP_DB + @".dbo.OPLN B ON A.PriceListNum = B.ListNum
                     LEFT JOIN " + Global.QIT_DB + @".dbo.QIT_PriceList C ON A.PriceListNum = C.GroupNum
                ";

                _logger.LogInformation(" ConfigsController : GetConfig() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GetConfig>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Data not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ConfigsController : GetConfig Error : " + ex.ToString());
                _logger.LogError(" Error in ConfigsController : GetConfig() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveConfig")]
        public async Task<ActionResult<IEnumerable<GetConfig>>> SaveConfig(SaveConfig payload)
        {
            try
            {
                _logger.LogInformation(" Calling ConfigsController : SaveConfig() ");

                QITcon = new SqlConnection(_QIT_connection);

                #region Validation

                if (payload.PriceListNum.ToString().Length > 0)
                {
                    System.Data.DataTable dtPriceList = new();
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT T0.[ListNum], T0.[ListName] 
                    FROM " + Global.SAP_DB + @".dbo.OPLN T0 WHERE T0.ListNum = @listNum
                    UNION 
                    SELECT A.GroupNum ListNum, A.GroupName ListName from " + Global.QIT_DB + @".dbo.QIT_PriceList A where A.GroupNum = @listNum
                    ";

                    _logger.LogInformation(" ConfigController : Price List Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@listNum", payload.PriceListNum);
                    oAdptr.Fill(dtPriceList);
                    QITcon.Close();

                    if (dtPriceList.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Price List does not exist : " + payload.PriceListNum
                        });
                }
                else
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Price List" });

                #endregion

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_Config_Master
                INSERT INTO " + Global.QIT_DB + @".dbo.QIT_Config_Master
                (
                    PriceListNum
                ) 
                VALUES 
                (
                    @priceListNum
                )";
                _logger.LogInformation(" ConfigController : SaveConfig() : Query :  {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@priceListNum", payload.PriceListNum);

                int intNum = 0;
                try
                {
                    await QITcon.OpenAsync();
                    intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();
                }
                catch (Exception ex)
                {
                    objGlobal.WriteLog("ConfigsController : SaveConfig Query : " + _Query.ToString());
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        IsSaved = "N",
                        StatusMsg = ex.Message.ToString()
                    });
                }

                return Ok(new { StatusCode = "200", IsSaved = "Y", StatusMsg = "Saved Successfully!!!" });

            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ConfigsController : SaveConfig Error : " + ex.ToString());
                _logger.LogError(" Error in ConfigsController : SaveConfig() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }
    }
}
