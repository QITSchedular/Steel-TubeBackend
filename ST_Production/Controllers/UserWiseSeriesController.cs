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
    public class UserWiseSeriesController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;

        private SqlConnection QITcon;
     
        private SqlDataAdapter oAdptr;
        private SqlCommand cmd;
        public Global objGlobal;

        private readonly ILogger<UserWiseSeriesController> _logger;
        public IConfiguration Configuration { get; }

        public UserWiseSeriesController(IConfiguration configuration, ILogger<UserWiseSeriesController> logger)
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
                objGlobal.WriteLog(" Error in UserWiseSeriesController :: " + ex.ToString());
                _logger.LogError(" Error in UserWiseSeriesController :: {ex}" + ex.ToString());
            }
        }


        [HttpPost("SaveUserWiseSeries")]
        public async Task<ActionResult<UserWiseSeries>> SaveUserWiseSeries([FromBody] UserWiseSeries payload)
        {
            try
            {
                dynamic arData = JsonConvert.SerializeObject(payload.Series_Details);

                _Query = @"
                MERGE INTO " + Global.QIT_DB + @".dbo.QIT_UserWiseSeries_Config AS Target
                USING 
                (
                    SELECT @User_ID AS User_ID, @Series_Details AS Series_Details
                ) AS Source
                ON Target.User_ID = Source.User_ID
                WHEN MATCHED THEN
                     UPDATE SET Series_Details = Source.Series_Details
                WHEN NOT MATCHED THEN
                     INSERT ( User_ID, Series_Details)
                     VALUES ( Source.User_ID, Source.Series_Details);";

                _logger.LogInformation(" UserWiseSeriesController : SaveUserWiseSeries() Query : {q} ", _Query.ToString());

                QITcon = new SqlConnection(_QIT_connection);
                using (cmd = new SqlCommand(_Query, QITcon))
                {
                    await QITcon.OpenAsync();
                    cmd.Parameters.AddWithValue("@User_ID", payload.User_ID);
                    cmd.Parameters.AddWithValue("@Series_Details", arData);

                    int insertCount = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (insertCount > 0)
                        return Ok(new { StatusCode = "200", IsSaved = "Y", StatusMsg = "Saved Successfully!!!" });
                    else
                        return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = "Unable to save" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("UserWiseSeriesController : SaveUserWiseSeries Error : " + ex.ToString());
                _logger.LogError(" Error in UserWiseSeriesController : SaveUserWiseSeries() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("GetUserWiseSeries")]
        public async Task<ActionResult<IEnumerable<ShiftSeries>>> GetUserWiseSeries([FromBody] getUserSeries payload)
        {
            try
            {
                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Details not found" });
                }
                else
                {
                    QITcon = new SqlConnection(_QIT_connection);
                    DataTable dtData = new();;

                    _Query = @" select User_ID, Series_Details from " + Global.QIT_DB + @".dbo.QIT_UserWiseSeries_Config where User_ID= @userId ";
                    _logger.LogInformation(" UserWiseSeriesController : GetUserWiseSeries() Query : {q} ", _Query.ToString());

                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@userId", payload.User_ID);
                    oAdptr.Fill(dtData);
                    QITcon.Close();
                    if (dtData.Rows.Count > 0)
                    {
                        List<ShiftSeries> shiftSeriesList = new List<ShiftSeries>();

                        foreach (DataRow row in dtData.Rows)
                        {
                            ShiftSeries shiftSeries = new ShiftSeries
                            {
                                SeriesList = JsonConvert.DeserializeObject<List<subSeriesClass>>(row["Series_Details"].ToString())
                            };
                            shiftSeriesList.Add(shiftSeries);
                        }
                        return shiftSeriesList;
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Data not found" });
                    }
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("UserWiseSeriesController : GetUserWiseSeries Error : " + ex.ToString());
                _logger.LogError(" Error in UserWiseSeriesController : GetUserWiseSeries() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

    }
}
