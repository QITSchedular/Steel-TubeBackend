using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ST_Production.Common;
using ST_Production.Models;
using System.Data.SqlClient;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _QIT_connection = string.Empty;

        private string _Query = string.Empty;
        private SqlConnection QITcon;
        private SqlCommand cmd;
        private SqlDataAdapter oAdptr;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<LogController> _logger;

        public LogController(IConfiguration configuration, ILogger<LogController> logger)
        {
            objGlobal ??= new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];

                _QIT_connection = Configuration["connectApp:QITConnString"];

                objGlobal.gServer = Configuration["Server"];
                objGlobal.gSqlVersion = Configuration["SQLVersion"];
                objGlobal.gCompanyDB = Configuration["CompanyDB"];
                objGlobal.gLicenseServer = Configuration["LicenseServer"];
                objGlobal.gSAPUserName = Configuration["SAPUserName"];
                objGlobal.gSAPPassword = Configuration["SAPPassword"];
                objGlobal.gDBUserName = Configuration["DBUserName"];
                objGlobal.gDBPassword = Configuration["DbPassword"];

                Global.QIT_DB = "[" + Configuration["QITDB"] + "]";
                Global.SAP_DB = "[" + Configuration["CompanyDB"] + "]";
                Global.gLogPath = Configuration["LogPath"];
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in LogController :: " + ex.ToString());
                _logger.LogError(" Error in LogController :: {ex}" + ex.ToString());
            }
        }



        [HttpPost("Save")]
        public IActionResult SaveLog([FromBody] SaveLog payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling LogController : SaveLog() ");

                if (payload != null)
                {
                    #region Check Branch
                    if (payload.BranchID == 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Branch"
                        });
                    }
                    #endregion

                    #region Check for Module
                    if (payload.Module.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Module" });
                    }
                    if (payload.Module.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Module" });
                    }
                    #endregion

                    #region Check for Controller Name
                    if (payload.ControllerName.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Controller" });
                    }
                    if (payload.ControllerName.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Controller" });
                    }
                    #endregion

                    #region Check for FormType 
                    int _formTypel = payload.FormType.ToString().Length;
                    if (_formTypel > 1)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "FormType Values : C:Create/V:Verify/-" });
                    }
                    else
                    {
                        if (payload.FormType.ToString().ToUpper() != "C" && payload.FormType.ToString().ToUpper() != "V" && payload.FormType.ToString().ToUpper() != "-")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "FormType Values : C:Create/V:Verify/-" });
                    }
                    #endregion

                    #region Check for ObjectType  
                    if (payload.ObjectType.ToString().ToUpper() != "202" && payload.ObjectType.ToString().ToUpper() != "59" &&
                        payload.ObjectType.ToString().ToUpper() != "60" && payload.ObjectType.ToString().ToUpper() != "67" &&
                        payload.ObjectType.ToString().ToUpper() != "-")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "ObjectType Values : 202:ProductionOrder/59:ProductionReceipt/60:ProductionIssue/67:InventoryTransfer/-" });

                    #endregion

                    #region Check for Method
                    if (payload.MethodName.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Method" });
                    }
                    if (payload.MethodName.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Method" });
                    }
                    #endregion

                    #region Check for LogLevel 
                    int _logLevel = payload.LogLevel.ToString().Length;
                    if (_logLevel > 1)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "LogLevel Values : I:Information/S:Success/E:Error" });
                    }
                    else
                    {
                        if (payload.LogLevel.ToString().ToUpper() != "I" && payload.LogLevel.ToString().ToUpper() != "E" && payload.LogLevel.ToString().ToUpper() != "S")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "LogLevel Values : I:Information/S:Success/E:Error" });
                    }
                    #endregion

                    #region Check for Log Message
                    if (payload.LogMessage.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Log Message" });
                    }
                    if (payload.LogMessage.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Log Message" });
                    }
                    #endregion

                    #region Check for loginUser
                    if (payload.LoginUser.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });
                    }
                    if (payload.LoginUser.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });
                    }
                    #endregion

                    #region Check for API Url
                    if (payload.APIUrl.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide API URL" });
                    }
                    if (payload.APIUrl.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide API URL" });
                    }
                    #endregion


                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"insert into " + Global.QIT_DB + @".dbo.QIT_API_Log(BranchID, DocNum, Module, ControllerName, FormType, ObjectType, MethodName, LogLevel, LogMessage, APIUrl, jsonPayload, ModuleTransId, ProOrdDocNum, LoginUser) 
                           VALUES ( @bID, @docNum, @module, @cName, @formType, @objType, @mName, @logLevel, @logMsg, @apiURL, @json, @moduleTransId, @proOrdDocNum, @user)";
                    _logger.LogInformation(" LogController : SaveLog() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@module", payload.Module);
                    cmd.Parameters.AddWithValue("@cName", payload.ControllerName);
                    cmd.Parameters.AddWithValue("@formType", payload.FormType);
                    cmd.Parameters.AddWithValue("@objType", payload.ObjectType);
                    cmd.Parameters.AddWithValue("@mName", payload.MethodName);
                    cmd.Parameters.AddWithValue("@logLevel", payload.LogLevel);
                    cmd.Parameters.AddWithValue("@logMsg", payload.LogMessage);
                    cmd.Parameters.AddWithValue("@apiURL", payload.APIUrl);
                    cmd.Parameters.AddWithValue("@json", payload.jsonPayload);
                    cmd.Parameters.AddWithValue("@moduleTransId", payload.ModuleTransId);
                    cmd.Parameters.AddWithValue("@proOrdDocNum", payload.ProOrdDocNum);
                    cmd.Parameters.AddWithValue("@user", payload.LoginUser);

                    QITcon.Open();
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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("LogController : SaveLog Error : " + ex.ToString());
                _logger.LogError("Error in LogController : SaveLog() :: {ex}", ex.ToString());

                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });

            }
        }


        [HttpGet("Modules")]
        public async Task<ActionResult<IEnumerable<LogModules>>> GetModules()
        {
            try
            {
                _logger.LogInformation(" Calling LogController : GetModules() ");

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT Distinct Module FROM " + Global.QIT_DB + @".dbo.QIT_API_Log where Module <> 'Report'
                FOR BROWSE
                ";

                _logger.LogInformation(" LogController : GetModules Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<LogModules> obj = new List<LogModules>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<LogModules>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("LogController : GetModules Error : " + ex.ToString());
                _logger.LogError(" Error in LogController : GetModules() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("LogReport")]
        public async Task<ActionResult<IEnumerable<LogReport>>> GetLogReport(LogDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling LogController : GetLogReport() ");

                string _strWhere = string.Empty;

                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }

                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }

                if (payload.AdminOnly.ToString().ToUpper() != "Y" && payload.AdminOnly.ToString().ToUpper() != "N")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "AdminOnly Values : Y:Yes / N:No" });


                if (payload.Module == string.Empty)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Module Name" });
                }

                if (payload.UserName != string.Empty && payload.UserName != "")
                {
                    if (payload.UserName.ToLower() != "admin")
                        _strWhere += " AND LoginUser = @userName";
                    else if (payload.UserName.ToLower() == "admin" && payload.AdminOnly.ToLower() == "y")
                        _strWhere += " AND LoginUser = @userName";
                }

                if (payload.LogLevel != string.Empty && payload.LogLevel != "")
                {
                    _strWhere += " AND LogLevel = @loglevel";
                }



                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT  Id, ModuleTransId, Module, A.ControllerName SubModule,
                        CASE WHEN LogLevel = 'S' THEN 'Success' WHEN LogLevel = 'I' THEN 'Information' ELSE 'Error' END AS Status,
                        LogMessage, LoginUser AS UserName, 
                        EntryDate  AS LogDate,  
	                    case when A.LogLevel = 'S' and A.FormType IN ('C', 'V') and A.ObjectType = '202' then 
			                (select Z.ProductNo from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header Z where Z.ProId = A.ModuleTransId)
	                    else 
			                '-' 
	                    end ProductNo,
	                    case when A.LogLevel = 'S' and A.FormType IN ('C', 'V') and A.ObjectType = '202' then 
			                (select Z.ProductName from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header Z where Z.ProId = A.ModuleTransId)
	                    else 
			                '-' 
	                    end ProductName,
	                    case when A.LogLevel = 'S' and A.FormType = 'V' and A.ObjectType = '202' then 
                            CAST((select Z.DocNum from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header Z where Z.ProId = A.ModuleTransId) AS nvarchar(50))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ProOrdDocNum,
		                  case when A.LogLevel = 'S' and A.FormType = 'V' and A.ObjectType = '60' then 
                            CAST((select Z.DocNum from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header Z where Z.IssId = A.ModuleTransId) AS nvarchar(50))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ProIssueDocNum,
		                case when A.LogLevel = 'S' and A.FormType = 'V' and A.ObjectType = '59' then 
                            CAST((select Z.DocNum from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header Z where Z.RecId = A.ModuleTransId) AS nvarchar(50))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ProReceiptDocNum,
                        case when A.LogLevel = 'S' and A.FormType = 'V' and A.ObjectType = '67' then 
                            CAST((select Z.DocNum from " + Global.QIT_DB + @".dbo.QIT_IT_Header Z where Z.InvId = A.ModuleTransId) AS nvarchar(50))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ITDocNum,
                        case when A.LogLevel ='S' and A.ObjectType = '-' and A.Module = 'ReturnComponents' and A.ControllerName = 'ReturnComponents'  then 
			                CAST((A.DocNum) AS nvarchar(50))
		                else 
                            CAST ('-' AS NVARCHAR(5)) 
                        end ReturnDocNum,
                        case when A.LogLevel = 'S' and A.FormType IN ('C', 'V') and A.ObjectType = '202' then 
                            cast((  SELECT Z1.SeriesName from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header Z inner join " + Global.SAP_DB + @".dbo.NNM1 z1 ON Z.Series = Z1.Series 
                                    WHERE Z.ProId = A.ModuleTransId) as nvarchar(100))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ProOrdSeries,
		                case when A.LogLevel = 'S' and A.FormType IN ('C', 'V') and A.ObjectType = '60' then 
                            cast((  SELECT Z1.SeriesName from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header Z inner join " + Global.SAP_DB + @".dbo.NNM1 z1 ON Z.Series = Z1.Series 
                                    WHERE Z.IssId = A.ModuleTransId) as nvarchar(100))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ProIssueSeries,
		                case when A.LogLevel = 'S' and A.FormType IN ('C', 'V') and A.ObjectType = '59' then 
                            cast((  SELECT Z1.SeriesName from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header Z inner join " + Global.SAP_DB + @".dbo.NNM1 z1 ON Z.Series = Z1.Series 
                                    WHERE Z.RecId = A.ModuleTransId) as nvarchar(100))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ProReceiptSeries,
                        case when A.LogLevel = 'S' and A.FormType IN ('C', 'V') and A.ObjectType = '67' then 
                            cast((  SELECT Z1.SeriesName from " + Global.QIT_DB + @".dbo.QIT_IT_Header Z inner join " + Global.SAP_DB + @".dbo.NNM1 z1 ON Z.Series = Z1.Series 
                                    WHERE Z.InvId = A.ModuleTransId) as nvarchar(100))
                        else 
                            CAST ('-' AS NVARCHAR(5))
                        end ITSeries,
                        case when A.LogLevel = 'S' and A.FormType IN ('V') and A.ObjectType = '202' then 
                            (select Z.ActionUser from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header Z where Z.ProId = A.ModuleTransId)
                        else 
                            '-' 
                        end ProOrdApprover,
		                case when A.LogLevel = 'S' and A.FormType IN ('V') and A.ObjectType = '60' then 
                            (select Z.ActionUser from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header Z where Z.IssId = A.ModuleTransId)
                        else 
                            '-' 
                        end ProIssueApprover,
		                case when A.LogLevel = 'S' and A.FormType IN ('V') and A.ObjectType = '59' then 
                            (select Z.ActionUser from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header Z where Z.RecId = A.ModuleTransId)
                        else 
                            '-' 
                        end ProReceiptApprover,
		                case when A.LogLevel = 'S' and A.FormType IN ('V') and A.ObjectType = '67' then 
                            (select Z.ActionUser from " + Global.QIT_DB + @".dbo.QIT_IT_Header Z where Z.InvId = A.ModuleTransId)
                        else 
                            '-' 
                        end ITApprover, APIUrl, jsonPayload, ProOrdDocNum RefProOrdDocNum
                FROM " + Global.QIT_DB + @".dbo.QIT_API_Log A
                WHERE CONCAT(YEAR(EntryDate), '-', FORMAT(EntryDate, 'MM'), '-' , FORMAT(EntryDate, 'dd')) >= @frDate AND 
                      CONCAT(YEAR(EntryDate), '-', FORMAT(EntryDate, 'MM'), '-' , FORMAT(EntryDate, 'dd')) <= @toDate AND
	                  A.Module = @module and A.ControllerName <> 'commons' " + _strWhere;


                _logger.LogInformation(" LogController : GetLogReport Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@module", payload.Module);
                oAdptr.SelectCommand.Parameters.AddWithValue("@userName", payload.UserName);
                oAdptr.SelectCommand.Parameters.AddWithValue("@loglevel", payload.LogLevel);
                oAdptr.Fill(dtData);
                QITcon.Close();

                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<LogReport> obj = new List<LogReport>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<LogReport>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("LogController : GetLogReport Error : " + ex.ToString());
                _logger.LogError(" Error in LogController : GetLogReport() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }
    }
}
