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
    public class ReportController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;

        private string _Query = string.Empty;

        public Global objGlobal;
        SqlConnection QITcon;
        SqlDataAdapter oAdptr;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ReportController> _logger;

        public ReportController(IConfiguration configuration, ILogger<ReportController> logger)
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
                Global.gAllowBranch = Configuration["AllowBranch"];

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
                objGlobal.WriteLog(" Error in ReportController :: " + ex.ToString());
                _logger.LogError(" Error in ReportController :: {ex}" + ex.ToString());
            }
        }


        #region Get Weekly Dashboard data

        [HttpGet("GetWeeklyDashboard")]
        public async Task<IActionResult> GetWeeklyDashboard(int BranchId, string ObjType)
        {
            //ObjType = 202 = Production Order
            //ObjType = 60 = Production Issue
            //ObjType = 59 = Production Receipt
            //ObjType = 67 = Inventory Transfer
            try
            {
                _logger.LogInformation("Calling ReportController: GetWeeklyDashboard()");

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                if (ObjType.ToString() == string.Empty)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Object Type" });

                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtData = new(); ;

                #region Production Order
                if (ObjType == "202")
                {
                    _Query = @" 
                    SELECT ISNULL(Action, 'T') AS Action, COUNT(*) AS ActionCount
                    FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                    WHERE DATEPART(WEEK, OrderDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                    GROUP BY GROUPING SETS (Action, ())
                    ";
                }
                #endregion

                #region Production Issue
                else if (ObjType == "60")
                {
                    _Query = @" 
                    SELECT ISNULL(Action, 'T') AS Action, COUNT(*) AS ActionCount
                    FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header
                    WHERE DATEPART(WEEK, PostingDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                    GROUP BY GROUPING SETS (Action, ())
                    ";
                }
                #endregion

                #region Production Receipt
                else if (ObjType == "59")
                {
                    _Query = @" 
                    SELECT ISNULL(Action, 'T') AS Action, COUNT(*) AS ActionCount
                    FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header
                    WHERE DATEPART(WEEK, PostingDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                    GROUP BY GROUPING SETS (Action, ())
                    ";
                }
                #endregion

                #region Inventory Transfer
                else if (ObjType == "67")
                {
                    _Query = @" 
                    SELECT ISNULL(Action, 'T') AS Action, COUNT(*) AS ActionCount
                    FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header
                    WHERE DATEPART(WEEK, DocDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                    GROUP BY GROUPING SETS (Action, ())
                    ";
                }
                #endregion

                else
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide valid Object Type" });

                _logger.LogInformation("ReportController: GetWeeklyDashboard Query : {Query}", _Query);
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtData);

                var result = dtData.AsEnumerable()
                    .Select(row => new { Action = row["Action"].ToString(), ActionCount = row["ActionCount"] })
                    .ToList();

                var pendingCount = (int)(result.Where(item => item.Action == "P").FirstOrDefault()?.ActionCount ?? 0);
                var approvedCount = (int)(result.Where(item => item.Action == "A").FirstOrDefault()?.ActionCount ?? 0);
                var rejectedCount = (int)(result.Where(item => item.Action == "R").FirstOrDefault()?.ActionCount ?? 0);
                var sapCount = (int)(result.Where(item => item.Action == "S").FirstOrDefault()?.ActionCount ?? 0);
                var totalCount = (int)(result.Where(item => item.Action == "T").FirstOrDefault()?.ActionCount ?? 0);
                var response = new
                {
                    Pending = pendingCount,
                    Approved = approvedCount,
                    Rejected = rejectedCount,
                    SAP = sapCount,
                    Total = totalCount
                };

                _logger.LogInformation("ReportController: Successfully retrieved details.");

                return Ok(response);
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetWeeklyDashboard Error : " + ex.ToString());
                _logger.LogError("Error in ReportController: GetWeeklyDashboard() :: {ex}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    StatusMsg = ex.Message
                });
            }
        }

        #endregion


        #region Get Total Weekly Dashboard data

        [HttpGet("GetTotalWeeklyDashboard")]
        public async Task<IActionResult> GetTotalWeeklyDashboard(int BranchId)
        {
            try
            {
                _logger.LogInformation("Calling ReportController: GetTotalWeeklyDashboard()");

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtData = new(); ;

                _Query = @" 
                SELECT '202' ObjType, COUNT(*) AS ActionCount
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                WHERE DATEPART(WEEK, OrderDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                     
                UNION 

                SELECT '59' ObjType, COUNT(*) AS ActionCount
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header
                WHERE DATEPART(WEEK, PostingDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                
                UNION

                SELECT '60' ObjType, COUNT(*) AS ActionCount
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header
                WHERE DATEPART(WEEK, PostingDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                    
                UNION

                SELECT '67' ObjType, COUNT(*) AS ActionCount
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header
                WHERE DATEPART(WEEK, DocDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId
                   
                UNION
        
                SELECT '2' ObjType, COUNT(*) AS ActionCount
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                WHERE Status = 'L' AND DATEPART(WEEK, OrderDate) = DATEPART(WEEK, GETDATE()) AND ISNULL(BranchId, @bId) = @bId

                ";

                _logger.LogInformation("ReportController: GetTotalWeeklyDashboard Query : {Query}", _Query);
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtData);

                var result = dtData.AsEnumerable()
                    .Select(row => new { ObjType = row["ObjType"].ToString(), ActionCount = row["ActionCount"] })
                    .ToList();

                var ProductionOrderCount = (int)(result.Where(item => item.ObjType == "202").FirstOrDefault()?.ActionCount ?? 0);
                var ProductionIssueCount = (int)(result.Where(item => item.ObjType == "60").FirstOrDefault()?.ActionCount ?? 0);
                var ProductionReceiptCount = (int)(result.Where(item => item.ObjType == "59").FirstOrDefault()?.ActionCount ?? 0);
                var ClosedProductionCount = (int)(result.Where(item => item.ObjType == "2").FirstOrDefault()?.ActionCount ?? 0);
                var InventoryTransferCount = (int)(result.Where(item => item.ObjType == "67").FirstOrDefault()?.ActionCount ?? 0);
                var response = new
                {
                    ProductionOrder = ProductionOrderCount,
                    ProductionIssue = ProductionIssueCount,
                    ProductionReceipt = ProductionReceiptCount,
                    ClosedProduction = ClosedProductionCount,
                    InventoryTransfer = InventoryTransferCount
                };

                _logger.LogInformation("ReportController: GetTotalWeeklyDashboard : Successfully retrieved details.");

                return Ok(response);
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetTotalWeeklyDashboard Error : " + ex.ToString());
                _logger.LogError("Error in ReportController: GetTotalWeeklyDashboard() :: {ex}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    StatusMsg = ex.Message
                });
            }
        }

        #endregion


        #region Header Detail Reports


        #region Get Production Order Data


        [HttpGet("GetProProductNos")]
        public async Task<ActionResult<IEnumerable<ProductNos>>> GetProProductNos(string FromDate, string ToDate)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProProductNos() ");

                #region Validation

                if (FromDate.ToString() == string.Empty || FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (ToDate.ToString() == string.Empty || ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                #region Get Data

                System.Data.DataTable dtPro = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT DISTINCT ProductNo, ProductName FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A
                WHERE A.OrderDate >= @frDate and A.OrderDate <= @toDate
                      AND A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header ) 
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProProductNos Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", ToDate);
                oAdptr.Fill(dtPro);
                QITcon.Close();
                #endregion

                if (dtPro.Rows.Count > 0)
                {
                    List<ProductNos> obj = new List<ProductNos>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<ProductNos>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProProductNos Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProProductNos() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionOrders")]
        public async Task<ActionResult<IEnumerable<rptProductionOrder>>> GetProProductionOrders(reportFilter payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProProductionOrders() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProductNo.ToString().Length > 0)
                    _filter = " and A.ProductNo = @productNo ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT A.ProId, 
		               CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                   CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
	                   CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                   B.SeriesName, A.OrderDate PostingDate, A.ProductNo, A.ProductName, 
                       CAST(A.PlannedQty as numeric(19,3)) PlannedQty, 
                       ISNULL(CAST(E.CmpltQty as numeric(19,3)),0) CompletedQty, A.Project, A.WhsCode, 
	                   C.OcrName DistRule, 
                       D.ShiftName Shift, A.UoM,
	                   CASE WHEN A.Status = 'P' THEN 'Planned' WHEN A.Status = 'R' THEN 'Released' 
	                        WHEN  A.Status = 'C' THEN 'Canceled' WHEN A.Status = 'L' THEN 'Closed' END Status,
                       A.EntryUser DraftUser, A.ActionUser,
	                   A.draftRemark Remark
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
	                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master D on D.ShiftId = A.Shift
	                LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR E ON E.DocEntry = A.DocEntry
                WHERE A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header )
                and A.OrderDate >= @frDate and A.OrderDate <= @toDate " +
                _filter + @"
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProProductionOrders Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@productNo", payload.ProductNo);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionOrder> obj = new List<rptProductionOrder>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionOrder>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProProductionOrders Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProProductionOrders() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductionDetails")]
        public async Task<ActionResult<IEnumerable<rptProductionOrderDetail>>> GetProductionDetails(int BranchId, int ProId)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Pro Id

                if (ProId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order Id" });

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE ProId = @proId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ReportController : Pro Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proId", ProId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Order exists"
                    });
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.ProId, D.ProDetId, 
                       D.ItemCode, D.ItemName, CAST(D.BaseQty as numeric(19,3)) BaseQty, D.BaseRatio, 
                       CAST(D.PlannedQty as numeric(19,3)) PlannedQty, CAST(BB.IssuedQty as numeric(19,3)) IssuedQty,
		               CAST((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
		                  FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode AND 
                                Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = D.WhsCode 
                       ) as numeric(19,3)) AvailQty, D.UomCode UoM, 
		               CASE WHEN D.IssueType = 'M' THEN 'Manual' WHEN D.IssueType = 'B' THEN 'Backflush' END IssueType,
		               D.WhsCode WhsCode,
		               CAST((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                       ) as numeric(19,3)) InStock, D.Project Project
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail D ON A.ProId = D.ProId
                LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR AA ON AA.DocEntry = A.DocEntry
				LEFT JOIN " + Global.SAP_DB + @".dbo.WOR1 BB ON AA.DocEntry = BB.DocEntry and BB.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master E ON E.ShiftId = A.Shift
                LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                WHERE A.ProId = @proId AND ISNULL(A.BranchId, @bId) = @bId
                ORDER BY D.ProDetId 
                FOR BROWSE
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetProductionDetails() Query : {q} ", _Query.ToString());
                dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proId", ProId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {

                    List<rptProductionOrderDetail> obj = new List<rptProductionOrderDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<rptProductionOrderDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Issue Data

        [HttpGet("GetIssueProductionNos")]
        public async Task<ActionResult<IEnumerable<ProductionNos>>> GetIssueProductionNos(string FromDate, string ToDate)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetIssueProductionNos() ");

                #region Validation

                if (FromDate.ToString() == string.Empty || FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (ToDate.ToString() == string.Empty || ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                #region Get Data

                System.Data.DataTable dtPro = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT DISTINCT B.DocEntry, B.DocNum, B.ProductNo, B.ProductName 
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header A
                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header B ON A.ProOrdDocEntry = B.DocEntry
                WHERE A.PostingDate >= @frDate and A.PostingDate <= @toDate
                      AND A.IssId NOT IN ( SELECT PrevIssId from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header ) 
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetIssueProductionNos Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", ToDate);
                oAdptr.Fill(dtPro);
                QITcon.Close();
                #endregion

                if (dtPro.Rows.Count > 0)
                {
                    List<ProductionNos> obj = new List<ProductionNos>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<ProductionNos>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetIssueProductionNos Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetIssueProductionNos() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionIssue")]
        public async Task<ActionResult<IEnumerable<rptProductionIssue>>> GetProductionIssue(reportFilterNos payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionIssue() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProOrdDocEntry > 0)
                    _filter = " and E.DocEntry = @docEntry ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT A.IssId, 
		               CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                   CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
					   E.DocEntry ProOrdDocEntry, E.DocNum ProOrdDocNum, 
	                   CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                   B.SeriesName, A.PostingDate,  A.EntryUser DraftUser, A.ActionUser, E.ItemCode ProductNo, E.ProdName ProductName, 
                       A.draftRemark Remark
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header  A
	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series 
                     INNER JOIN " + Global.SAP_DB + @".dbo.OWOR E ON E.DocEntry = A.ProOrdDocEntry
                WHERE A.IssId NOT IN ( SELECT PrevIssId from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header ) and
                      A.PostingDate >= @frDate and A.PostingDate <= @toDate " +
                _filter + @"
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProductionIssue Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionIssue> obj = new List<rptProductionIssue>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionIssue>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionIssue Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionIssue() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductionIssueDetails")]
        public async Task<ActionResult<IEnumerable<rptProductionIssueDetail>>> GetProductionIssueDetails(int BranchId, int IssId)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionIssueDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Iss Id

                if (IssId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Issue Id" });

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE IssId = @issId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ReportController : Iss Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@issId", IssId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Issue exists"
                    });
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.IssId, D.IssDetId, D.ItemCode, D.ItemName, D.BaseLine, D.LineNum, CAST(D.Qty as numeric(19,3)) Qty,
                        CAST(ISNULL(G.BaseQty,0) as numeric(19,3)) BaseQty, CAST(ISNULL(G.PlannedQty,0) as numeric(19,3)) PlannedQty, 
                        CAST(ISNULL(G.IssuedQty, 0) as numeric(19,3)) IssuedQty,  
	                    CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                            WHERE Z.WhsCode = G.wareHouse and Z.ItemCode = G.ItemCode
                        ) as numeric(19,3)) WhsQty,
                        D.UomCode, D.WhsCode, D.Project, D.DistRule 
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header   A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
				INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail D On A.IssId = D.IssId 
                INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON C.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                WHERE A.IssId = @issId AND ISNULL(A.BranchId, @bId) = @bId
                ORDER BY D.IssDetId
				FOR BROWSE
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetProductionIssueDetails() Query : {q} ", _Query.ToString());
                dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@issId", IssId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<rptProductionIssueDetail> obj = new List<rptProductionIssueDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<rptProductionIssueDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionIssueDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionIssueDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #endregion


        #region Get Production Receipt Data

        [HttpGet("GetReceiptProductionNos")]
        public async Task<ActionResult<IEnumerable<ProductionNos>>> GetReceiptProductionNos(string FromDate, string ToDate)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetReceiptProductionNos() ");

                #region Validation

                if (FromDate.ToString() == string.Empty || FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (ToDate.ToString() == string.Empty || ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                #region Get Data

                System.Data.DataTable dtPro = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT DISTINCT B.DocEntry, B.DocNum, B.ProductNo, B.ProductName 
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header A
                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header B ON A.ProOrdDocEntry = B.DocEntry
                WHERE A.PostingDate >= @frDate and A.PostingDate <= @toDate
                      AND A.RecId NOT IN ( SELECT PrevRecId from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header ) 
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetReceiptProductionNos Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", ToDate);
                oAdptr.Fill(dtPro);
                QITcon.Close();
                #endregion

                if (dtPro.Rows.Count > 0)
                {
                    List<ProductionNos> obj = new List<ProductionNos>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<ProductionNos>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetReceiptProductionNos Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetReceiptProductionNos() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionReceipt")]
        public async Task<ActionResult<IEnumerable<rptProductionReceipt>>> GetProductionReceipt(reportFilterNos payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionReceipt() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProOrdDocEntry > 0)
                    _filter = " and E.DocEntry = @docEntry ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT A.RecId, 
		               CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                   CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
					   E.DocEntry ProOrdDocEntry, E.DocNum ProOrdDocNum, 
	                   CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                   B.SeriesName, A.PostingDate, E.ItemCode ProductNo, E.ProdName ProductName,  A.EntryUser DraftUser, A.ActionUser, 
                       CAST(ISNULL(E.PlannedQty, 0) as numeric(19,3)) PlannedQty, CAST(ISNULL(E.CmpltQty, 0) as numeric(19,3)) CompletedQty, 
                       A.draftRemark Remark
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header  A
	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series 
                     LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR E ON E.DocEntry = A.ProOrdDocEntry
                WHERE A.RecId NOT IN ( SELECT PrevRecId from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header ) and
                    A.PostingDate >= @frDate and A.PostingDate <= @toDate " +
                    _filter + @"
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProductionReceipt Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionReceipt> obj = new List<rptProductionReceipt>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionReceipt>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionReceipt Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionReceipt() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductionReceiptDetails")]
        public async Task<ActionResult<IEnumerable<rptProductionReceiptDetail>>> GetProductionReceiptDetails(int BranchId, int RecId)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionReceiptDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Rec Id

                if (RecId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Receipt Id" });

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header WHERE RecId = @recId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ReportController : Rec Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@recId", RecId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Receipt exists"
                    });
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                 
	            SELECT A.RecId, D.RecDetId,  
			           D.ItemCode, D.ItemName,  ISNULL(cast(D.BaseLine as nvarchar(10)), 'N') BaseLine, D.LineNum, D.TransType,
                       case when D.TransType = 'C' then 'Completed' when D.TransType = 'R' then 'Reject' end TransTypeName,
			           CAST(D.Qty as numeric(19,3)) Qty, null BaseQty, CAST(ISNULL(C.PlannedQty,0) as numeric(19,3)) PlannedQty, 
			           CAST(ISNULL(C.CmpltQty, 0) as numeric(19,3)) CompletedQty, 
			           CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
				              WHERE Z.WhsCode = C.wareHouse and Z.ItemCode = C.ItemCode
			           ) as numeric(19,3)) WhsQty,
			           D.UomCode, D.WhsCode, D.Project, D.DistRule 
	            FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header   A
	            INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	            INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
	            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail D On A.RecId = D.RecId 
	            INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = C.ItemCode
	            WHERE A.RecId = @recId AND ISNULL(A.BranchId, @bId) = @bId and D.BaseLine is null
 
                UNION
 
	            SELECT A.RecId, D.RecDetId,  
			           D.ItemCode, D.ItemName, cast(D.BaseLine as nvarchar(10)) BaseLine, D.LineNum, D.TransType,
                       case when D.TransType = 'C' then 'Completed' when D.TransType = 'R' then 'Reject' end TransTypeName,
			           CAST(D.Qty as numeric(19,3)) Qty, CAST(ISNULL(G.BaseQty,0) as numeric(19,3)) BaseQty, 
                       CAST(ISNULL(G.PlannedQty,0) as numeric(19,3)) PlannedQty, 
			           CAST(ISNULL(G.IssuedQty, 0) as numeric(19,3)) CompletedQty, 
			           CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
				              WHERE Z.WhsCode = C.wareHouse and Z.ItemCode = G.ItemCode
			           ) as numeric(19,3)) WhsQty,
			           D.UomCode, D.WhsCode, D.Project, D.DistRule 
	            FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header   A
	            INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	            INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
	            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail D On A.RecId = D.RecId 
	            INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = C.ItemCode
	            INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON C.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
	            WHERE A.RecId = @recId AND ISNULL(A.BranchId, @bId) = @bId and D.BaseLine > 0 
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetProductionReceiptDetails() Query : {q} ", _Query.ToString());
                dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@recId", RecId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<rptProductionReceiptDetail> obj = new List<rptProductionReceiptDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<rptProductionReceiptDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionReceiptDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionReceiptDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #endregion


        #region Get Closed Production Order Data


        [HttpGet("GetClosedProductionNos")]
        public async Task<ActionResult<IEnumerable<ProductionNos>>> GetClosedProductionNos(string FromDate, string ToDate)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetClosedProductNos() ");

                #region Validation

                if (FromDate.ToString() == string.Empty || FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (ToDate.ToString() == string.Empty || ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                #region Get Data

                System.Data.DataTable dtPro = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT DISTINCT DocEntry, DocNum, ProductNo, ProductName FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A
                WHERE A.OrderDate >= @frDate and A.OrderDate <= @toDate and A.Status = 'L'
                      AND A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header ) 
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetClosedProductionNos Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", ToDate);
                oAdptr.Fill(dtPro);
                QITcon.Close();
                #endregion

                if (dtPro.Rows.Count > 0)
                {
                    List<ProductionNos> obj = new List<ProductionNos>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<ProductionNos>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetClosedProductionNos Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetClosedProductionNos() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetClosedProductionOrders")]
        public async Task<ActionResult<IEnumerable<rptProductionOrder>>> GetClosedProductionOrders(reportFilterNos payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetClosedProductionOrders() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProOrdDocEntry > 0)
                    _filter = " and E.DocEntry = @docEntry ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT A.ProId, 
		               CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                   CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
	                   CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                   B.SeriesName, A.OrderDate PostingDate, A.ProductNo, A.ProductName, 
                       CAST(A.PlannedQty as numeric(19,3)) PlannedQty, 
                       ISNULL(CAST(E.CmpltQty as numeric(19,3)),0) CompletedQty, A.Project, A.WhsCode, 
	                   C.OcrName DistRule, 
                       D.ShiftName Shift, A.UoM,
	                   CASE WHEN A.Status = 'P' THEN 'Planned' WHEN A.Status = 'R' THEN 'Released' 
	                        WHEN  A.Status = 'C' THEN 'Canceled' WHEN A.Status = 'L' THEN 'Closed' END Status,
                       A.EntryUser DraftUser, A.ActionUser,
	                   A.draftRemark Remark
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
	                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master D on D.ShiftId = A.Shift
	                LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR E ON E.DocEntry = A.DocEntry
                WHERE A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header )
                and A.OrderDate >= @frDate and A.OrderDate <= @toDate and A.Status = 'L' " +
                _filter + @"
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetClosedProductionOrders Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionOrder> obj = new List<rptProductionOrder>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionOrder>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetClosedProductionOrders Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetClosedProductionOrders() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetClosedProductionDetails")]
        public async Task<ActionResult<IEnumerable<rptProductionOrderDetail>>> GetClosedProductionDetails(int BranchId, int ProId)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetClosedProductionDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Pro Id

                if (ProId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order Id" });

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE ProId = @proId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ReportController : Pro Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proId", ProId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Order exists"
                    });

                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.ProId, D.ProDetId, 
                       D.ItemCode, D.ItemName, CAST(D.BaseQty as numeric(19,3)) BaseQty, D.BaseRatio, 
                       CAST(D.PlannedQty as numeric(19,3)) PlannedQty, CAST(BB.IssuedQty as numeric(19,3)) IssuedQty,
		               CAST((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
		                  FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode AND 
                                Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = D.WhsCode 
                       ) as numeric(19,3)) AvailQty, D.UomCode UoM, 
		               CASE WHEN D.IssueType = 'M' THEN 'Manual' WHEN D.IssueType = 'B' THEN 'Backflush' END IssueType,
		               D.WhsCode WhsCode,
		               CAST((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                       ) as numeric(19,3)) InStock, D.Project Project
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail D ON A.ProId = D.ProId
                LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR AA ON AA.DocEntry = A.DocEntry
				LEFT JOIN " + Global.SAP_DB + @".dbo.WOR1 BB ON AA.DocEntry = BB.DocEntry and BB.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master E ON E.ShiftId = A.Shift
                LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                WHERE A.ProId = @proId AND ISNULL(A.BranchId, @bId) = @bId and A.Status = 'L'
                ORDER BY D.ProDetId 
                FOR BROWSE
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetClosedProductionDetails() Query : {q} ", _Query.ToString());
                dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proId", ProId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<rptProductionOrderDetail> obj = new List<rptProductionOrderDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<rptProductionOrderDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetClosedProductionDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetClosedProductionDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Inventory Transfer Data

        [HttpPost("GetInventoryTransfer")]
        public async Task<ActionResult<IEnumerable<rptInventoryTransfer>>> GetInventoryTransfer(itFilter payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetInventoryTransfer() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.FromWhs.ToString().Length > 0)
                    _filter = " and A.FromWhs = @fromWhs ";
                if (payload.ToWhs.ToString().Length > 0)
                    _filter = " and A.ToWhs = @toWhs ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                 SELECT A.InvId, A.ProOrdDocEntry,
		                CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
                        CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
                        CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
		                A.FromWhs, A.ToWhs, A.EntryUser DraftUser, A.ActionUser, A.PostingDate, A.DocDate, A.draftRemark Remark
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header  A 
                WHERE A.InvId NOT IN ( SELECT PrevInvId from " + Global.QIT_DB + @".dbo.QIT_IT_Header ) AND				 
                      A.PostingDate >= @frDate and A.PostingDate <= @toDate " +
                      _filter + @"
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetInventoryTransfer Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toWhs", payload.ToWhs);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptInventoryTransfer> obj = new List<rptInventoryTransfer>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptInventoryTransfer>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetInventoryTransfer Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetInventoryTransfer() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetInventoryTransferDetails")]
        public async Task<ActionResult<IEnumerable<rptInventoryTransferDetail>>> GetInventoryTransferDetails(int BranchId, int InvId)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetInventoryTransferDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Inv Id

                if (InvId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Inventory Transfer Id" });

                System.Data.DataTable dtInv = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE InvId = @invId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ReportController : Inv Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@invId", InvId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Inventory Transfer exists"
                    });
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT  A.ProOrdDocEntry, ISNULL(( SELECT DocNum FROM " + Global.SAP_DB + @".dbo.OWOR WHERE DocEntry = A.ProOrdDocEntry),0) ProOrdDocNum,
                        A.InvId, D.InvDetId, D.LineNum, D.ItemCode ItemCode, D.ItemName ItemName, 
	                    D.FromWhs FromWhs, D.ToWhs ToWhs,
	                    CAST(D.Qty as numeric(19,3)) Qty, D.Uom UoM, 
                        CAST(ISNULL((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
                            FROM " + Global.SAP_DB + @".dbo.OITW Z 
                            WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode AND 
                            Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = D.FromWhs 
                        ),0) as numeric(19,3)) AvailQty,  
                        CAST(ISNULL((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                            WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                        ),0) as numeric(19,3)) InStock 
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header  A
 	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
 	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_IT_Detail D ON A.InvId = D.InvId
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.OPLN C On A.PriceListId = C.ListNum
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.OSLP E On A.SlpCode = E.SlpCode
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                WHERE A.InvId = @invId AND ISNULL(A.BranchId, @bId) = @bId 
                ORDER BY D.InvDetId
				FOR BROWSE
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetInventoryTransferDetails() Query : {q} ", _Query.ToString());
                dtInv = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@invId", InvId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count > 0)
                {

                    List<rptInventoryTransferDetail> obj = new List<rptInventoryTransferDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtInv);
                    obj = JsonConvert.DeserializeObject<List<rptInventoryTransferDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetInventoryTransferDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetInventoryTransferDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #endregion

        #endregion

        #region All data in one line - Reports

        [HttpPost("GetProductionOrdersV2")]
        public async Task<ActionResult<IEnumerable<rptProductionOrderV2>>> GetProProductionOrdersV2(reportFilter payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProProductionOrdersV2() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProductNo.ToString().Length > 0)
                    _filter = " and A.ProductNo = @productNo ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT  A.ProId, 
		                CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                    CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
	                    CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                    B.SeriesName, A.OrderDate PostingDate, A.ProductNo, A.ProductName, 
                        CAST(A.PlannedQty as numeric(19,3)) HeaderPlannedQty, 
                        ISNULL(CAST(E.CmpltQty as numeric(19,3)),0) HeaderCompletedQty, A.Project HeaderProject, A.WhsCode HeaderWhsCode, 
	                    C.OcrName HeaderDistRule, 
                        D.ShiftName Shift, A.UoM HeaderUoM,
	                    CASE WHEN A.Status = 'P' THEN 'Planned' WHEN A.Status = 'R' THEN 'Released' 
	                        WHEN  A.Status = 'C' THEN 'Canceled' WHEN A.Status = 'L' THEN 'Closed' END Status,
                        A.EntryUser DraftUser, A.ActionUser, A.draftRemark Remark,
		                F.ProDetId, F.ItemCode, F.ItemName, CAST(F.BaseQty as numeric(19,3)) BaseQty, F.BaseRatio, 
                        CAST(F.PlannedQty as numeric(19,3)) DetailPlannedQty, CAST(G.IssuedQty as numeric(19,3)) DetailIssuedQty,
		                CAST((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
		                        FROM " + Global.SAP_DB + @".dbo.OITW Z 
                                WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = F.ItemCode AND 
                                      Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = F.WhsCode 
                        ) as numeric(19,3)) AvailQty, 
		                CAST((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                                WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = F.ItemCode
                        ) as numeric(19,3)) InStock, F.UomCode DetailUoM, F.Project DetailProject,  F.WhsCode DetailWhsCode,
                        CASE WHEN F.IssueType = 'M' THEN 'Manual' WHEN F.IssueType = 'B' THEN 'Backflush' END IssueType
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master D on D.ShiftId = A.Shift
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                     LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR E ON E.DocEntry = A.DocEntry
	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail F ON A.ProId = F.ProId
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON E.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = F.ItemCode
                WHERE A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header )
                and A.OrderDate >= @frDate and A.OrderDate <= @toDate " +
                _filter + @"
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProProductionOrdersV2 Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@productNo", payload.ProductNo);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionOrderV2> obj = new List<rptProductionOrderV2>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionOrderV2>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProProductionOrdersV2 Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProProductionOrdersV2() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionIssueV2")]
        public async Task<ActionResult<IEnumerable<rptProductionIssueV2>>> GetProProductionIssueV2(reportFilterNos payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProProductionIssueV2() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProOrdDocEntry > 0)
                    _filter = " and C.DocEntry = @docEntry ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT  A.IssId, 
		                CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                    CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
		                C.DocEntry ProOrdDocEntry, C.DocNum ProOrdDocNum, 
	                    CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                    B.SeriesName, A.PostingDate,  A.EntryUser DraftUser, A.ActionUser, C.ItemCode ProductNo, C.ProdName ProductName, 
                        A.draftRemark Remark,
		                D.IssDetId, D.ItemCode, D.ItemName, D.BaseLine, D.LineNum, CAST(D.Qty as numeric(19,3)) Qty,
                        CAST(ISNULL(G.BaseQty,0) as numeric(19,3)) BaseQty, CAST(ISNULL(G.PlannedQty,0) as numeric(19,3)) PlannedQty, 
                        CAST(ISNULL(G.IssuedQty, 0) as numeric(19,3)) IssuedQty,  
	                    CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                            WHERE Z.WhsCode = G.wareHouse and Z.ItemCode = G.ItemCode
                        ) as numeric(19,3)) WhsQty,
                        D.UomCode, D.WhsCode, D.Project, D.DistRule 
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header  A
	                    INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series 
		                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail D On A.IssId = D.IssId 
                        LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR C ON C.DocEntry = A.ProOrdDocEntry
                        INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON C.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                WHERE A.IssId NOT IN ( SELECT PrevIssId from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header ) 
                and A.PostingDate >= @frDate and A.PostingDate <= @toDate " +
                _filter + @"
                ORDER BY A.IssId, D.IssDetId
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProProductionIssueV2 Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionIssueV2> obj = new List<rptProductionIssueV2>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionIssueV2>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProProductionIssueV2 Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProProductionIssueV2() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionReceiptV2")]
        public async Task<ActionResult<IEnumerable<rptProductionReceiptV2>>> GetProProductionReceiptV2(reportFilterNos payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProProductionReceiptV2() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProOrdDocEntry > 0)
                    _filter = " and C.DocEntry = @docEntry ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT * FROM (
                SELECT  A.RecId, 
		                CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                    CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
		                C.DocEntry ProOrdDocEntry, C.DocNum ProOrdDocNum, 
		                CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                    B.SeriesName, A.PostingDate, C.ItemCode ProductNo, C.ProdName ProductName,
		                A.EntryUser DraftUser, A.ActionUser, 
		                CAST(ISNULL(C.PlannedQty, 0) as numeric(19,3)) HeaderPlannedQty, CAST(ISNULL(C.CmpltQty, 0) as numeric(19,3)) HeaderCompletedQty, 
                        A.draftRemark Remark,
		                D.RecDetId,  
		                D.ItemCode, D.ItemName,  ISNULL(cast(D.BaseLine as nvarchar(10)), 'N') BaseLine, D.LineNum, D.TransType,
		                case when D.TransType = 'C' then 'Completed' when D.TransType = 'R' then 'Reject' end TransTypeName,
		                CAST(D.Qty as numeric(19,3)) Qty, null BaseQty, CAST(ISNULL(C.PlannedQty,0) as numeric(19,3)) PlannedQty, 
		                CAST(ISNULL(C.CmpltQty, 0) as numeric(19,3)) CompletedQty, 
		                CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
			                   WHERE Z.WhsCode = C.wareHouse and Z.ItemCode = C.ItemCode
		                ) as numeric(19,3)) WhsQty,
		                D.UomCode, D.WhsCode, D.Project, D.DistRule 
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header   A
 	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
 	                 INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
 	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail D On A.RecId = D.RecId 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = C.ItemCode
                WHERE A.RecId NOT IN ( SELECT PrevRecId from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header ) AND D.BaseLine is null AND 
                      A.PostingDate >= @frDate and A.PostingDate <= @toDate " +
                _filter + @"
 
                UNION
 
                SELECT A.RecId,
	                   CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                   CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
	                   C.DocEntry ProOrdDocEntry, C.DocNum ProOrdDocNum, 
	                   CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                   B.SeriesName, A.PostingDate, C.ItemCode ProductNo, C.ProdName ProductName,
	                   A.EntryUser DraftUser, A.ActionUser, 
	                   CAST(ISNULL(C.PlannedQty, 0) as numeric(19,3)) HeaderPlannedQty, CAST(ISNULL(C.CmpltQty, 0) as numeric(19,3)) HeaderCompletedQty, 
                       A.draftRemark Remark,
	                   D.RecDetId,  
	                   D.ItemCode, D.ItemName, cast(D.BaseLine as nvarchar(10)) BaseLine, D.LineNum, D.TransType,
                       case when D.TransType = 'C' then 'Completed' when D.TransType = 'R' then 'Reject' end TransTypeName,
	                   CAST(D.Qty as numeric(19,3)) Qty, CAST(ISNULL(G.BaseQty,0) as numeric(19,3)) BaseQty, 
                       CAST(ISNULL(G.PlannedQty,0) as numeric(19,3)) PlannedQty, 
	                   CAST(ISNULL(G.IssuedQty, 0) as numeric(19,3)) CompletedQty, 
	                   CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
		                      WHERE Z.WhsCode = C.wareHouse and Z.ItemCode = G.ItemCode
	                   ) as numeric(19,3)) WhsQty,
	                   D.UomCode, D.WhsCode, D.Project, D.DistRule 
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header   A
	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail D On A.RecId = D.RecId 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = C.ItemCode
	                 INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON C.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                WHERE A.RecId NOT IN ( SELECT PrevRecId from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header ) AND D.BaseLine > 0 AND 
                      A.PostingDate >= @frDate and A.PostingDate <= @toDate " +
                _filter + @"
                ) AS A
                ORDER BY A.RecId, A.RecDetId
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProProductionReceiptV2 Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionReceiptV2> obj = new List<rptProductionReceiptV2>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionReceiptV2>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProProductionReceiptV2 Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProProductionReceiptV2() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetClosedProductionOrdersV2")]
        public async Task<ActionResult<IEnumerable<rptProductionOrderV2>>> GetClosedProductionOrdersV2(reportFilterNos payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetClosedProductionOrdersV2() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.ProOrdDocEntry > 0)
                    _filter = " and E.DocEntry = @docEntry ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT  A.ProId, 
		                CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                    CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
	                    CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                    B.SeriesName, A.OrderDate PostingDate, A.ProductNo, A.ProductName, 
                        CAST(A.PlannedQty as numeric(19,3)) HeaderPlannedQty, 
                        ISNULL(CAST(E.CmpltQty as numeric(19,3)),0) HeaderCompletedQty, A.Project HeaderProject, A.WhsCode HeaderWhsCode, 
	                    C.OcrName HeaderDistRule, 
                        D.ShiftName Shift, A.UoM HeaderUoM,
	                    CASE WHEN A.Status = 'P' THEN 'Planned' WHEN A.Status = 'R' THEN 'Released' 
	                        WHEN  A.Status = 'C' THEN 'Canceled' WHEN A.Status = 'L' THEN 'Closed' END Status,
                        A.EntryUser DraftUser, A.ActionUser, A.draftRemark Remark,
		                F.ProDetId, F.ItemCode, F.ItemName, CAST(F.BaseQty as numeric(19,3)) BaseQty, F.BaseRatio, 
                        CAST(F.PlannedQty as numeric(19,3)) DetailPlannedQty, CAST(G.IssuedQty as numeric(19,3)) DetailIssuedQty,
		                CAST((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
		                        FROM " + Global.SAP_DB + @".dbo.OITW Z 
                                WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = F.ItemCode AND 
                                      Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = F.WhsCode 
                        ) as numeric(19,3)) AvailQty, 
		                CAST((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                                WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = F.ItemCode
                        ) as numeric(19,3)) InStock, F.UomCode DetailUoM, F.Project DetailProject,  F.WhsCode DetailWhsCode,
                        CASE WHEN F.IssueType = 'M' THEN 'Manual' WHEN F.IssueType = 'B' THEN 'Backflush' END IssueType
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master D on D.ShiftId = A.Shift
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                     LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR E ON E.DocEntry = A.DocEntry
	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail F ON A.ProId = F.ProId
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON E.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = F.ItemCode
                WHERE A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header )
                and A.OrderDate >= @frDate and A.OrderDate <= @toDate and A.Status = 'L' " +
                 _filter + @"
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetClosedProductionOrdersV2 Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptProductionOrderV2> obj = new List<rptProductionOrderV2>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptProductionOrderV2>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetClosedProductionOrdersV2 Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetClosedProductionOrdersV2() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetInventoryTransferV2")]
        public async Task<ActionResult<IEnumerable<rptInventoryTransferV2>>> GetInventoryTransferV2(itFilter payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetInventoryTransferV2() ");

                #region Validation

                if (payload.FromDate.ToString() == string.Empty || payload.FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (payload.ToDate.ToString() == string.Empty || payload.ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                string _filter = string.Empty;
                if (payload.FromWhs.ToString().Length > 0)
                    _filter = " and A.FromWhs = @fromWhs ";
                if (payload.ToWhs.ToString().Length > 0)
                    _filter = " and A.ToWhs = @toWhs ";

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);


                _Query = @" 
                SELECT  A.InvId, 
                        A.ProOrdDocEntry, ISNULL(( SELECT DocNum FROM " + Global.SAP_DB + @".dbo.OWOR WHERE DocEntry = A.ProOrdDocEntry),0) ProOrdDocNum,
                        CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
                        CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
                        CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
                        A.FromWhs HeaderFromWhs, A.ToWhs HeaderToWhs, 
                        A.EntryUser DraftUser, A.ActionUser, A.PostingDate, A.DocDate, A.draftRemark Remark,
                        D.InvDetId, D.LineNum, D.ItemCode ItemCode, D.ItemName ItemName, 
	                    D.FromWhs DetailFromWhs, D.ToWhs DetailToWhs,
	                    CAST(D.Qty as numeric(19,3)) Qty, D.Uom UoM, 
                        CAST(ISNULL((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
                            FROM " + Global.SAP_DB + @".dbo.OITW Z 
                            WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode AND 
                            Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = D.FromWhs 
                        ),0) as numeric(19,3)) AvailQty,  
                        CAST(ISNULL((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                            WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                        ),0) as numeric(19,3)) InStock 
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header  A
 	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
 	                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_IT_Detail D ON A.InvId = D.InvId
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.OPLN C On A.PriceListId = C.ListNum
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.OSLP E On A.SlpCode = E.SlpCode
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                WHERE A.InvId NOT IN ( SELECT PrevInvId from " + Global.QIT_DB + @".dbo.QIT_IT_Header ) AND				 
                      A.PostingDate >= @frDate and A.PostingDate <= @toDate " +
                      _filter + @"
                ORDER BY A.InvId, D.InvDetId
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetInventoryTransferV2 Query : {q} ", _Query.ToString());


                await QITcon.OpenAsync();

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toWhs", payload.ToWhs);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<rptInventoryTransferV2> obj = new List<rptInventoryTransferV2>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<rptInventoryTransferV2>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetInventoryTransferV2 Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetInventoryTransferV2() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion



        #region Variance Report


        #region Get Production Order Nos

        [HttpGet("varGetProductionOrderNos")]
        public async Task<ActionResult<IEnumerable<ProductionNos>>> GetProductionOrderNos(string FromDate, string ToDate)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionOrderNos() ");

                #region Validation

                if (FromDate.ToString() == string.Empty || FromDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide From Date" });

                if (ToDate.ToString() == string.Empty || ToDate.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide To Date" });

                #endregion

                #region Get Data

                System.Data.DataTable dtPro = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT DISTINCT DocEntry, DocNum, ProductNo, ProductName FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A
                WHERE A.OrderDate >= @frDate and A.OrderDate <= @toDate AND A.DocEntry > 0
                      AND A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header ) 
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProductNos Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", ToDate);
                oAdptr.Fill(dtPro);
                QITcon.Close();
                #endregion

                if (dtPro.Rows.Count > 0)
                {
                    List<ProductionNos> obj = new List<ProductionNos>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<ProductionNos>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionOrderNos Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionOrderNos() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Order Header

        [HttpGet("varGetProductionOrderHeader")]
        public async Task<ActionResult<IEnumerable<varProductionOrderHeader>>> GetProductionOrderHeader(int ProOrdDocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionOrderHeader() ");

                #region Validation

                if (ProOrdDocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order DocEntry" });

                #endregion

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT CASE WHEN A.Type = 'S' THEN 'Standard' WHEN A.Type = 'P' THEN 'Special' END Type,
	                   CASE WHEN A.Status = 'C' THEN 'Canceled' WHEN A.Status = 'L' THEN 'Closed' 
                            WHEN A.Status = 'P' THEN 'Planned' WHEN A.Status = 'R' THEN 'Released' END Status,
	                   B.SeriesName, A.DocEntry, A.DocNum, 
	                   A.PostDate OrderDate, A.StartDate, A.DueDate,
	                   A.CardCode, C.OcrCode ,C.OcrName, A.Project,
	                   A.ItemCode ProductNo, A.ProdName ProductName, A.PlannedQty, ISNULL(A.CmpltQty,0) CompletedQty, ISNULL(A.CmpltQty,0) - A.PlannedQty Variance,  
                       A.Warehouse, A.Uom, A.Comments
                FROM " + Global.SAP_DB + @".dbo.OWOR A 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	                 LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C on A.OcrCode = C.OcrCode
                WHERE DocEntry = @docEntry 
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : GetProductionOrderHeader Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<varProductionOrderHeader> obj = new List<varProductionOrderHeader>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<varProductionOrderHeader>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionOrderHeader Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionOrderHeader() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Order Detail

        [HttpGet("varGetProductionOrderDetail")]
        public async Task<ActionResult<IEnumerable<varProductionOrderDetail>>> GetProductionOrderDetail(int ProOrdDocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionOrderDetail() ");

                #region Check for Branch Id

                if (ProOrdDocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order DocEntry" });

                #endregion


                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.DocEntry, B.ItemCode, B.ItemName, B.BaseQty, D.BaseQtyBOM, B.PlannedQty, B.IssuedQty, B.UomCode, B.wareHouse, 
                       case when B.IssueType = 'M' then 'Manual' else 'Backflush' end IssueType
                FROM  " + Global.SAP_DB + @".dbo.OWOR A 
	                 inner join " + Global.SAP_DB + @".dbo.WOR1 B ON A.DocEntry = B.DocEntry
	                 inner join " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header C on A.DocEntry = C.DocEntry
	                 inner join " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail D on C.ProId = D.ProId and B.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                WHERE A.DocEntry = @docEntry 
                FOR BROWSE
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetProductionOrderDetail() Query : {q} ", _Query.ToString());
                DataTable dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProOrdDocEntry);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<varProductionOrderDetail> obj = new List<varProductionOrderDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<varProductionOrderDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionOrderDetail Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionOrderDetail() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Issue Header

        [HttpGet("varGetProductionIssueHeader")]
        public async Task<ActionResult<IEnumerable<varProductionIssueHeader>>> varGetProductionIssueHeader(int ProOrdDocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : varGetProductionIssueHeader() ");

                #region Validation

                if (ProOrdDocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order DocEntry" });

                #endregion

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"    
                select A.DocEntry, A.DocNum, B.SeriesName, A.DocDate PostingDate, A.Ref2, A.Comments
                from " + Global.SAP_DB + @".dbo.OIGE A inner join " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                where DocEntry in (select DocEntry from " + Global.SAP_DB + @".dbo.IGE1 where BaseEntry = @docEntry )
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : varGetProductionIssueHeader Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<varProductionIssueHeader> obj = new List<varProductionIssueHeader>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<varProductionIssueHeader>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : varGetProductionIssueHeader Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : varGetProductionIssueHeader() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Issue Detail

        [HttpGet("varGetProductionIssueDetail")]
        public async Task<ActionResult<IEnumerable<varProductionIssueDetail>>> GetProductionIssueDetail(int ProIssDocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionIssueDetail() ");

                #region Check for Branch Id

                if (ProIssDocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Issue DocEntry" });

                #endregion


                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.DocEntry, A.DocNum, B.ItemCode, C.ItemName, B.Quantity, B.WhsCode, B.UomCode
                FROM " + Global.SAP_DB + @".dbo.OIGE A INNER JOIN " + Global.SAP_DB + @".dbo.IGE1 B ON A.DocEntry = B.DocEntry
                INNER JOIN " + Global.SAP_DB + @".dbo.OITM C On B.ItemCode = C.ItemCode
                WHERE A.DocEntry = @docEntry
                FOR BROWSE
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetProductionIssueDetail() Query : {q} ", _Query.ToString());
                DataTable dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProIssDocEntry);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<varProductionIssueDetail> obj = new List<varProductionIssueDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<varProductionIssueDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionIssueDetail Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionIssueDetail() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Receipt Header       

        [HttpGet("varGetProductionReceiptHeader")]
        public async Task<ActionResult<IEnumerable<varProductionReceiptHeader>>> varGetProductionReceiptHeader(int ProOrdDocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : varGetProductionReceiptHeader() ");

                #region Validation

                if (ProOrdDocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order DocEntry" });

                #endregion

                #region Get Data

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @"    
                select A.DocEntry, A.DocNum, B.SeriesName, A.DocDate PostingDate, A.Ref2, A.Comments
                from " + Global.SAP_DB + @".dbo.OIGN A inner join " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                where DocEntry in (select DocEntry from " + Global.SAP_DB + @".dbo.IGN1 where BaseEntry = @docEntry )
                FOR BROWSE
                ";

                _logger.LogInformation(" ReportController : varGetProductionReceiptHeader Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProOrdDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();
                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<varProductionReceiptHeader> obj = new List<varProductionReceiptHeader>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<varProductionReceiptHeader>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : varGetProductionReceiptHeader Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : varGetProductionReceiptHeader() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Receipt Detail

        [HttpGet("varGetProductionReceiptDetail")]
        public async Task<ActionResult<IEnumerable<varProductionReceiptDetail>>> GetProductionReceiptDetail(int ProRecDocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionReceiptDetail() ");

                #region Check for Branch Id

                if (ProRecDocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Receipt DocEntry" });

                #endregion


                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.DocEntry, A.DocNum, B.ItemCode, C.ItemName, B.Quantity, B.WhsCode, B.UomCode
                FROM " + Global.SAP_DB + @".dbo.OIGN A INNER JOIN " + Global.SAP_DB + @".dbo.IGN1 B ON A.DocEntry = B.DocEntry
                INNER JOIN " + Global.SAP_DB + @".dbo.OITM C On B.ItemCode = C.ItemCode
                WHERE A.DocEntry = @docEntry
                FOR BROWSE
                ";
                #endregion

                _logger.LogInformation(" ReportController : GetProductionReceiptDetail() Query : {q} ", _Query.ToString());
                DataTable dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProRecDocEntry);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<varProductionReceiptDetail> obj = new List<varProductionReceiptDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<varProductionReceiptDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReportController : GetProductionReceiptDetail Error : " + ex.ToString());
                _logger.LogError(" Error in ReportController : GetProductionReceiptDetail() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion

        #endregion
    }
}
