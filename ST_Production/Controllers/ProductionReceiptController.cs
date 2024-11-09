using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using ST_Production.Common;
using ST_Production.Models;
using System.Data.SqlClient;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionReceiptController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        private SqlConnection QITcon;
        private SqlDataAdapter oAdptr;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ProductionReceiptController> _logger;


        public ProductionReceiptController(IConfiguration configuration, ILogger<ProductionReceiptController> logger)
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
                objGlobal.WriteLog(" Error in ProductionReceiptController :: " + ex.ToString());
                _logger.LogError(" Error in ProductionReceiptController :: {ex}" + ex.ToString());
            }
        }


        #region Fill data on Page Load

        [HttpGet("GetProductionOrderHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderHelpforReceipt>>> GetProductionOrderHelp()
        {
            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : GetProductionOrderHelp() ");

                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT B.DocEntry, B.DocNum, T1.SeriesName,
	                   B.PostDate PostingDate, B.ItemCode ProductNo, B.ProdName ProductName, 
	                   CAST(B.PlannedQty as numeric(19,3)) PlannedQty, CAST(B.CmpltQty as numeric(19,3)) CompletedQty, 
	                   B.Project, B.Warehouse WhsCode, A.DistRule, 'Released' Status, 
	                   B.Uom UomCode, B.Comments Remark
                FROM  " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A 
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OWOR B ON A.DocEntry = B.DocEntry  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 T1  ON  B.Series = T1.Series  
                WHERE B.Status = 'R'  AND  (B.Type = 'S'  OR  B.Type = 'P' ) AND  B.PlannedQty > B.CmpltQty  
                ORDER BY B.DocNum,B.DocEntry
                ";

                _logger.LogInformation(" ProductionReceiptController : GetProductionOrderHelp() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderHelpforReceipt> obj = new List<ProductionOrderHelpforReceipt>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderHelpforReceipt>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : GetProductionOrderHelp Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionReceiptController : GetProductionOrderHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Display Production Order detail for selected pro

        [HttpGet("DisplayProductionDetail")]
        public async Task<ActionResult<IEnumerable<DisplayProductionDetail>>> DisplayProductionDetail(int BranchId, int DocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : DisplayProductionDetail() ");

                System.Data.DataTable dtRec = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT *, ISNULL(A.PlannedQty, 0) - ISNULL(A.UsedQty, 0) PendingQty FROM (
                SELECT 1 SrNo, A.DocEntry, A.DocNum, 'N'  BaseLine,
	                   A.ItemCode ItemCode, A.ProdName ItemName, 'C' TransType, 'Completed' TransTypeName,        
	                   CAST(ISNULL(A.PlannedQty,0) - ISNULL(A.CmpltQty,0)  as numeric(19,3)) Qty, A.Warehouse WhsCode, 
	                   CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                              WHERE Z.WhsCode = A.Warehouse and Z.ItemCode = A.ItemCode) as numeric(19,3)) WhsQty, 
	                   CAST(( SELECT SUM(ISNULL(Z.OnHand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                              WHERE Z.ItemCode = A.ItemCode) as numeric(19,3)) TotalQty, 
	                   CAST(A.PlannedQty as numeric(19,3)) PlannedQty, CAST(A.CmpltQty as numeric(19,3)) CompletedQty, 0 BaseQty,
                       ISNULL(CAST( ( SELECT SUM(Z.Qty) from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail Z 
                                      INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header Z1 ON Z.RecId = Z1.RecId
                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS = A.ItemCode and Z1.Action IN ('P', 'A') and Z1.ProOrdDocEntry = A.DocEntry
			                 ) as numeric(19,3)
	                   ),0) UsedQty,
                       A.Uom UomCode, A.OcrCode DistRule, A.Project, B.IssueMthd IssueType
                FROM  " + Global.SAP_DB + @".dbo.OWOR A 
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode = B.ItemCode
                WHERE A.DocEntry = @docEntry AND A.Status = 'R'  AND  
                      (A.Type = 'S' OR A.Type = 'P') AND A.PlannedQty > A.CmpltQty

                UNION

                SELECT 2 SrNo, A.DocEntry, A.DocNum, CAST(B.LineNum as nvarchar(5)) BaseLine,
	                   B.ItemCode, B.ItemName, 'C' TransType,  'Completed' TransTypeName,   
	                   CAST(ABS(ISNULL(B.PlannedQty,0)) - ISNULL(B.IssuedQty,0)  as numeric(19,3)) Qty, B.Warehouse WhsCode, 
	                   CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                              WHERE Z.WhsCode = B.Warehouse and Z.ItemCode = B.ItemCode) as numeric(19,3)) WhsQty, 
	                   CAST(( SELECT SUM(ISNULL(Z.OnHand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                              WHERE Z.ItemCode = B.ItemCode) as numeric(19,3)) TotalQty, 
	                   CAST(ABS(B.PlannedQty) as numeric(19,3)) PlannedQty, CAST(B.IssuedQty as numeric(19,3)) CompletedQty, CAST(B.BaseQty as numeric(19,3)) BaseQty,
                       ISNULL(CAST( ( SELECT SUM(Z.Qty) from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail Z 
                                      INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header Z1 ON Z.RecId = Z1.RecId
                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode and Z1.Action IN ('P', 'A') and Z1.ProOrdDocEntry = A.DocEntry
			                 ) as numeric(19,3)
	                   ),0) UsedQty,
                       B.UomCode UomCode, B.OcrCode DistRule, B.Project, B.IssueType
                FROM " + Global.SAP_DB + @".dbo.OWOR A INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 B ON A.DocEntry = B.DocEntry
                WHERE A.DocEntry = @docEntry and B.PlannedQty <= 0 and A.Status = 'R' AND  
                      (A.Type = 'S' OR A.Type = 'P') AND A.PlannedQty > A.CmpltQty  
                ) as A
                ORDER BY A.SrNo, A.BaseLine
                ";

                _logger.LogInformation(" ProductionReceiptController : DisplayProductionDetail() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtRec);
                QITcon.Close();

                if (dtRec.Rows.Count > 0)
                {
                    List<DisplayProductionDetail> obj = new List<DisplayProductionDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtRec);
                    obj = JsonConvert.DeserializeObject<List<DisplayProductionDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : DisplayProductionDetail Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionReceiptController : DisplayProductionDetail() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Save Draft Production Receipt

        [HttpPost("SaveDraftProductionReceipt")]
        public IActionResult SaveDraftProductionReceipt([FromBody] SaveDraftProductionReceipt payload)
        {
            string _IsSaved = "N";
            int _RecId = 0;

            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : SaveDraftProductionReceipt() ");

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.recDetail.Count();

                    #region Get RecId  
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(RecId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header A  ";
                    _logger.LogInformation(" ProductionReceiptController : Get RecId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _RecId = (Int32)cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    #region Header Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.Series <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });

                    if (payload.ProOrdDocEntry <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order" });

                    #region Check for Login User

                    if (payload.LoginUser.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });

                    System.Data.DataTable dtUser = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @uName ";
                    _logger.LogInformation(" ProductionReceiptController : User Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@uName", payload.LoginUser);
                    oAdptr.Fill(dtUser);
                    QITcon.Close();

                    if (dtUser.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "User does not exist : " + payload.LoginUser
                        });

                    #endregion

                    #endregion

                    #region Save Header and Detail

                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                        INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header
                        (
                            BranchId, RecId, Series, DocEntry, DocNum, PostingDate, Ref2, ProOrdDocEntry,  
                            EntryDate, EntryUser, DraftRemark, Action, ActionDate, PrevRecId
                        ) 
                        VALUES 
                        (
                            @bId, @RecId, @series, @docEntry, @docNum, @pDate, @ref2, @proOrdDocEntry,   
                            @eDate, @eUser, @remark, @action, @aDate, 0
                        )";
                    _logger.LogInformation(" ProductionReceiptController : SaveDraftProductionReceipt() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@RecId", _RecId);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@docEntry", 0);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@pDate", payload.PostingDate);
                    cmd.Parameters.AddWithValue("@ref2", payload.RefNo);
                    cmd.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                    cmd.Parameters.AddWithValue("@eDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@eUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.Remark);
                    cmd.Parameters.AddWithValue("@action", "P");
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);

                    int intNum = 0;
                    try
                    {
                        QITcon.Open();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteDraftReceipt(_RecId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = ex.Message.ToString()
                        });
                    }

                    if (intNum > 0)
                    {
                        #region Detail validation
                        int row = 0;
                        foreach (var item in payload.recDetail)
                        {
                            row = row + 1;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftReceipt(_RecId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }

                            if (item.BaseLineNum != "N")
                            {
                                System.Data.DataTable dtItem = new();
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" 
                            SELECT A.* FROM " + Global.SAP_DB + @".dbo.WOR1 A 
                            WHERE A.DocEntry = @proOrdDocEntry and A.ItemCode = @itemCode and A.LineNum = @baseLineNum
                            ";

                                _logger.LogInformation(" ProductionReceiptController : Item Code Query : {q} ", _Query.ToString());
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@baseLineNum", item.BaseLineNum);
                                oAdptr.Fill(dtItem);
                                QITcon.Close();

                                if (dtItem.Rows.Count <= 0)
                                {
                                    this.DeleteDraftReceipt(_RecId);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row + " in Production Order"
                                    });
                                }
                            }

                            #endregion

                            #region Check for Qty

                            if (item.Qty.ToString() == "0")
                            {
                                this.DeleteDraftReceipt(_RecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Quantity for line : " + row
                                });
                            }

                            #endregion

                            #region Check for Warehouse

                            if (item.WhsCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftReceipt(_RecId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse for line : " + row });
                            }

                            System.Data.DataTable dtWhs = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" ProductionReceiptController : Detail Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftReceipt(_RecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail Warehouse does not exist : " + item.WhsCode
                                });
                            }

                            #endregion

                            #region Check for Project

                            if (item.Project.ToString().Length > 0)
                            {


                                System.Data.DataTable dtProject = new();
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OPRJ WHERE PrjCode = @proj AND Locked = 'N' and Active = 'Y' ";
                                _logger.LogInformation(" ProductionReceiptController : Detail Project Query : {q} ", _Query.ToString());
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", item.Project);
                                oAdptr.Fill(dtProject);
                                QITcon.Close();

                                if (dtProject.Rows.Count <= 0)
                                {
                                    this.DeleteDraftReceipt(_RecId);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "Detail Project does not exist : " + item.Project
                                    });
                                }
                            }

                            #endregion

                            #region Check for TransType 

                            if (item.TransType.ToString().ToUpper() != "C" && item.TransType.ToString().ToUpper() != "R")
                            {
                                this.DeleteDraftReceipt(_RecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Trans Type Values : C:Complete / R:Reject for line : " + row
                                });
                            }

                            #endregion

                            #region Save Detail

                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail
                            (
                                BranchId, RecId, RecDetId, BaseLine, LineNum, ItemCode, ItemName, Qty, TransType,  
                                UoMCode, WhsCode, Project, DistRule
                            ) 
                            VALUES 
                            (
                                @bId, @RecId, @RecDetId, @baseLine, @lineNum, @itemCode, @itemName, @Qty, @transType, 
                                @uomCode, @whsCode, @proj, @distRule
                            )";
                            _logger.LogInformation(" ProductionReceiptController : SaveDraftProductionReceiptDetail() Query for line " + row + " : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@RecId", _RecId);
                            cmd.Parameters.AddWithValue("@RecDetId", row);
                            cmd.Parameters.AddWithValue("@baseLine", item.BaseLineNum.ToUpper() == "N" ? DBNull.Value : item.BaseLineNum);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", item.ItemName);
                            cmd.Parameters.AddWithValue("@Qty", item.Qty);
                            cmd.Parameters.AddWithValue("@transType", item.TransType);
                            cmd.Parameters.AddWithValue("@uomCode", item.UoMCode);
                            cmd.Parameters.AddWithValue("@distRule", item.DistRule);
                            cmd.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            cmd.Parameters.AddWithValue("@proj", item.Project);

                            intNum = 0;
                            try
                            {
                                QITcon.Open();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex)
                            {
                                this.DeleteDraftReceipt(_RecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    StatusMsg = "For line : " + row + " Error : " + ex.Message.ToString()
                                });
                            }

                            if (intNum > 0)
                                SucessCount = SucessCount + 1;

                            #endregion
                        }

                        #endregion
                    }

                    #endregion

                    if (SucessCount == itemCount && SucessCount > 0)
                        return Ok(new
                        {
                            StatusCode = "200",
                            IsSaved = "Y",
                            RecId = _RecId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftReceipt(_RecId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Draft Production Receipt failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteDraftReceipt(_RecId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : SaveDraftProductionReceipt Error : " + ex.ToString());
                this.DeleteDraftReceipt(_RecId);
                _logger.LogError("Error in ProductionReceiptController : SaveDraftProductionReceipt() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
            finally
            {
                QITcon.Close();
            }
        }

        #endregion


        #region Display Production Receipt List in Grid

        [HttpGet("DisplayProductionReceipt")]
        public async Task<ActionResult<IEnumerable<DisplayProductionReceipt>>> DisplayProductionReceipt(string UserName, string UserType)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : DisplayProductionReceipt() ");

                System.Data.DataTable dtRec = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                string _strWhere = string.Empty;

                if (UserName.ToLower() != "admin")
                {
                    if (UserType.ToLower() == "c")
                        _strWhere = " and A.EntryUser = @uName ";
                }


                _Query = @" 

                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail WHERE RecId IN
                (
                   SELECT RecId FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header 
                   WHERE CAST(PostingDate AS DATE) < CAST(getdate() AS DATE) AND DocNum <= 0 and Action = 'A' 
                );

                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header
                WHERE CAST(PostingDate AS DATE) < CAST(getdate() AS DATE) AND DocNum <= 0 and Action = 'A'  ;

                SELECT A.RecId, A.ProOrdDocEntry, C.DocNum ProOrdDocNum,
		               C.ItemCode ProductNo, C.ProdName ProductName, 
					   CAST(ABS(C.PlannedQty) as numeric(19,3)) PlannedQty, CAST(C.CmpltQty as numeric(19,3)) CompletedQty, 
		               A.Series, B.SeriesName, 
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
                       CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
                       A.PostingDate, A.Ref2, A.draftRemark Remark,
                       CASE WHEN A.Action <> 'A' then '-' when A.Action = 'A' and A.DocNum = 0 THEN 'No' ELSE 'Yes' END BatchSelected
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header   A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
                WHERE A.RecId NOT IN ( SELECT PrevRecId FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header ) AND C.Status <> 'L'
                and A.RecId NOT IN (
					select Z.RecId   ---,  DATEDIFF(DAY, Z.ActionDate, getdate())
					from " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header Z where Z.Action = 'R' 
					and DATEDIFF(DAY, Z.ActionDate, getdate()) >= (select RejectDocDays from " + Global.QIT_DB + @".dbo.QIT_Config_Master)
				) and A.DocNum <= 0  
                " + _strWhere + @"
                ";

                _logger.LogInformation(" ProductionReceiptController : DisplayProductionReceipt() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@uName", UserName);
                oAdptr.Fill(dtRec);
                QITcon.Close();

                if (dtRec.Rows.Count > 0)
                {
                    List<DisplayProductionReceipt> obj = new List<DisplayProductionReceipt>();
                    dynamic arData = JsonConvert.SerializeObject(dtRec);
                    obj = JsonConvert.DeserializeObject<List<DisplayProductionReceipt>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : DisplayProductionReceipt Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionReceiptController : DisplayProductionReceipt() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Receipt with Detail on Grid Click

        [HttpGet("GetProductionReceiptDetails")]
        public async Task<ActionResult<IEnumerable<ReceiptHeader>>> GetProductionReceiptDetails(int BranchId, int RecId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : GetProductionReceiptDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Rec Id

                if (RecId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Receipt Id" });

                System.Data.DataTable dtRec = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header WHERE RecId = @RecId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionReceiptController : Rec Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@RecId", RecId);
                oAdptr.Fill(dtRec);
                QITcon.Close();

                if (dtRec.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Receipt exists"
                    });
                #endregion

                #region Check for Rec Id - Initiated again or not

                dtRec = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header WHERE PrevRecId = @RecId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionReceiptController : Rec Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@RecId", RecId);
                oAdptr.Fill(dtRec);
                QITcon.Close();

                if (dtRec.Rows.Count > 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "A new Production Receipt has already been initiated for this rejected receipt"
                    });
                }
                #endregion

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.*,
                       case when A.ItemMngBy = 'B' then 'Batch' when A.ItemMngBy = 'S' then 'Serial' when A.ItemMngBy = 'N' then 'None' end ItemMngByName
                FROM (
	            SELECT A.RecId, A.ProOrdDocEntry, C.DocNum ProOrdDocNum, A.Series, B.SeriesName, B.Indicator PeriodIndicator, 
			           CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
			           CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
			           CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
			           A.PostingDate, A.Ref2, A.draftRemark Remark, A.ActionRemark Reason,
			           D.RecDetId, D.ItemCode, D.ItemName,  ISNULL(cast(D.BaseLine as nvarchar(10)), 'N') BaseLine, D.LineNum, D.TransType,
                       case when D.TransType = 'C' then 'Completed' when D.TransType = 'R' then 'Reject' end TransTypeName,
			           CAST(D.Qty as numeric(19,3)) Qty, null BaseQty, CAST(ISNULL(C.PlannedQty,0) as numeric(19,3)) PlannedQty, 
			           CAST(ISNULL(C.CmpltQty, 0) as numeric(19,3)) CompletedQty, 
			           CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
				              WHERE Z.WhsCode = C.wareHouse and Z.ItemCode = C.ItemCode
			           ) as numeric(19,3)) WhsQty,
			           D.UomCode, D.WhsCode, D.Project, D.DistRule,
			           CASE WHEN F.ManSerNum = 'N' and F.ManBtchNum = 'N' then 'N' 
				            WHEN F.ManSerNum = 'N' and F.ManBtchNum = 'Y' then 'B' 
				            WHEN F.ManSerNum = 'Y' and F.ManBtchNum = 'N' then 'S' 
			           END ItemMngBy, 'M' IssueType
	            FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header   A
	            INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	            INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
	            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail D On A.RecId = D.RecId 
	            INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = C.ItemCode
	            WHERE A.RecId = @RecId AND ISNULL(A.BranchId, @bId) = @bId and D.BaseLine is null
 
                UNION
 
	            SELECT A.RecId, A.ProOrdDocEntry, C.DocNum ProOrdDocNum, A.Series, B.SeriesName, B.Indicator PeriodIndicator, 
			           CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
			           CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
			           CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
			           A.PostingDate, A.Ref2, A.draftRemark Remark, A.ActionRemark Reason,
			           D.RecDetId, D.ItemCode, D.ItemName, cast(D.BaseLine as nvarchar(10)) BaseLine, D.LineNum, D.TransType,
                       case when D.TransType = 'C' then 'Completed' when D.TransType = 'R' then 'Reject' end TransTypeName,
			           CAST(D.Qty as numeric(19,3)) Qty, CAST(ISNULL(G.BaseQty,0) as numeric(19,3)) BaseQty, 
                       CAST(ISNULL(G.PlannedQty,0) as numeric(19,3)) PlannedQty, 
			           CAST(ISNULL(G.IssuedQty, 0) as numeric(19,3)) CompletedQty, 
			           CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
				              WHERE Z.WhsCode = C.wareHouse and Z.ItemCode = G.ItemCode
			           ) as numeric(19,3)) WhsQty,
			           D.UomCode, D.WhsCode, D.Project, D.DistRule,
			           CASE WHEN F.ManSerNum = 'N' and  F.ManBtchNum = 'N' then 'N' 
				            WHEN F.ManSerNum = 'N' and  F.ManBtchNum = 'Y' then 'B' 
				            when F.ManSerNum = 'Y' and  F.ManBtchNum = 'N' then 'S' 
			           END ItemMngBy, G.IssueType
	            FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header   A
	            INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
	            INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
	            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail D On A.RecId = D.RecId 
	            INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON C.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = G.ItemCode
	            WHERE A.RecId = @RecId AND ISNULL(A.BranchId, @bId) = @bId and D.BaseLine >= 0 
                ) AS A
                ";
                #endregion

                _logger.LogInformation(" ProductionReceiptController : GetProductionReceiptDetails() Query : {q} ", _Query.ToString());
                dtRec = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@RecId", RecId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtRec);
                QITcon.Close();

                if (dtRec.Rows.Count > 0)
                {
                    List<ReceiptHeader> obj = new List<ReceiptHeader>();
                    List<ReceiptDetails> recDetails = new List<ReceiptDetails>();
                    dynamic arData = JsonConvert.SerializeObject(dtRec);
                    recDetails = JsonConvert.DeserializeObject<List<ReceiptDetails>>(arData.ToString());

                    obj.Add(new ReceiptHeader()
                    {
                        RecId = int.Parse(dtRec.Rows[0]["RecId"].ToString()),
                        ProOrdDocEntry = int.Parse(dtRec.Rows[0]["ProOrdDocEntry"].ToString()),
                        ProOrdDocNum = int.Parse(dtRec.Rows[0]["ProOrdDocNum"].ToString()),
                        PeriodIndicator = dtRec.Rows[0]["PeriodIndicator"].ToString(),
                        Series = int.Parse(dtRec.Rows[0]["Series"].ToString()),
                        SeriesName = dtRec.Rows[0]["SeriesName"].ToString(),
                        DocEntry = dtRec.Rows[0]["DocEntry"].ToString(),
                        DocNum = dtRec.Rows[0]["DocNum"].ToString(),
                        State = dtRec.Rows[0]["State"].ToString(),
                        PostingDate = dtRec.Rows[0]["PostingDate"].ToString(),
                        Ref2 = dtRec.Rows[0]["Ref2"].ToString(),
                        Remark = dtRec.Rows[0]["Remark"].ToString(),
                        Reason = dtRec.Rows[0]["Reason"].ToString(),
                        recDetail = recDetails
                    });

                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : GetProductionReceiptDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionReceiptController : GetProductionReceiptDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Approve/Reject Production Receipt

        [HttpPost("VerifyDraftProductionReceipt")]
        public IActionResult VerifyDraftProductionReceipt([FromBody] VerifyDraftProductionReceipt payload)
        {
            string _IsSaved = "N";

            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : VerifyDraftProductionReceipt() ");

                if (payload != null)
                {
                    #region Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    #region Check for Rec Id

                    if (payload.RecId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Receipt Id" });

                    System.Data.DataTable dtRec = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header WHERE RecId = @RecId AND ISNULL(BranchId, @bId) = @bId ";
                    _logger.LogInformation(" ProductionReceiptController : VerifyDraftProductionReceipt : Rec Id Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@RecId", payload.RecId);
                    oAdptr.Fill(dtRec);
                    QITcon.Close();

                    if (dtRec.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "No such Production Receipt exists"
                        });
                    else
                    {
                        if (dtRec.Rows[0]["Action"].ToString() != "P")
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Production Receipt is already approved or rejected"
                            });
                    }
                    #endregion

                    #region Check for Action

                    if (payload.Action.ToString().ToUpper() != "A" && payload.Action.ToString().ToUpper() != "R")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Action Values : A:Approve / R:Reject" });

                    #endregion

                    #region Check for Action Remark

                    if (payload.Action.ToString().ToUpper() == "R" && (payload.ActionRemark == string.Empty || payload.ActionRemark.ToString().ToLower() == "string"))
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Provide remark for rejection"
                        });

                    #endregion

                    #endregion

                    #region Update Production Receipt
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header
                    SET Action = @action, ActionDate = @aDate, ActionUser = @aUser, ActionRemark = @remark 
                    WHERE RecId = @RecId and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" ProductionReceiptController : VerifyDraftProductionReceipt : Update Production Receipt Action Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@action", payload.Action.ToUpper());
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@aUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.ActionRemark);
                    cmd.Parameters.AddWithValue("@RecId", payload.RecId);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);

                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    if (payload.Action.ToUpper() == "A")
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Production Receipt approved" });
                    else
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Production Receipt rejected" });
                    #endregion
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : VerifyDraftProductionReceipt Error : " + ex.ToString());
                _logger.LogError("Error in ProductionReceiptController : VerifyDraftProductionReceipt() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion


        #region Save Draft Production Receipt for Rejected one

        [HttpPost("SaveDraftProReceiptOfRejectedProReceipt")]
        public IActionResult SaveDraftProReceiptOfRejectedProReceipt(int RecId, [FromBody] SaveDraftProductionReceipt payload)
        {
            string _IsSaved = "N";
            int _NextRecId = 0;

            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : SaveDraftProReceiptOfRejectedProReceipt() ");

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.recDetail.Count();

                    #region Get RecId  
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(RecId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header A  ";
                    _logger.LogInformation(" ProductionReceiptController : Get RecId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _NextRecId = (Int32)cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    #region Header Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.Series <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });

                    if (payload.ProOrdDocEntry <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order" });

                    #region Check for Login User

                    if (payload.LoginUser.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });

                    System.Data.DataTable dtUser = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @uName ";
                    _logger.LogInformation(" ProductionReceiptController : User Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@uName", payload.LoginUser);
                    oAdptr.Fill(dtUser);
                    QITcon.Close();

                    if (dtUser.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "User does not exist : " + payload.LoginUser
                        });

                    #endregion

                    #endregion

                    #region Save Header and Detail

                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                        INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header
                        (
                            BranchId, RecId, Series, DocEntry, DocNum, PostingDate, Ref2, ProOrdDocEntry,  
                            EntryDate, EntryUser, DraftRemark, Action, ActionDate, PrevRecId
                        ) 
                        VALUES 
                        (
                            @bId, @RecId, @series, @docEntry, @docNum, @pDate, @ref2, @proOrdDocEntry,   
                             @eDate, @eUser, @remark, @action, @aDate, @prevRecId
                        )";
                    _logger.LogInformation(" ProductionReceiptController : SaveDraftProReceiptOfRejectedProReceipt() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@RecId", _NextRecId);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@docEntry", 0);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@pDate", payload.PostingDate);
                    cmd.Parameters.AddWithValue("@ref2", payload.RefNo);
                    cmd.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                    cmd.Parameters.AddWithValue("@eDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@eUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.Remark);
                    cmd.Parameters.AddWithValue("@action", "P");
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@prevRecId", RecId);

                    int intNum = 0;
                    try
                    {
                        QITcon.Open();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteDraftReceipt(_NextRecId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = ex.Message.ToString()
                        });
                    }

                    if (intNum > 0)
                    {
                        #region Detail validation
                        int row = 0;
                        foreach (var item in payload.recDetail)
                        {
                            row = row + 1;

                            if (item.BaseLineNum != "N")
                            {
                                #region Check for Item Code

                                if (item.ItemCode.ToString().Length <= 0)
                                {
                                    this.DeleteDraftReceipt(_NextRecId);
                                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                                }
                                System.Data.DataTable dtItem = new();
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" 
                                SELECT A.* FROM " + Global.SAP_DB + @".dbo.WOR1 A 
                                WHERE A.DocEntry = @proOrdDocEntry and A.ItemCode = @itemCode and A.LineNum = @baseLineNum
                                ";

                                _logger.LogInformation(" ProductionReceiptController : Item Code Query : {q} ", _Query.ToString());
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@baseLineNum", item.BaseLineNum);
                                oAdptr.Fill(dtItem);
                                QITcon.Close();

                                if (dtItem.Rows.Count <= 0)
                                {
                                    this.DeleteDraftReceipt(_NextRecId);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row + " in Production Order"
                                    });
                                }
                                #endregion
                            }

                            #region Check for Qty

                            if (item.Qty.ToString() == "0")
                            {
                                this.DeleteDraftReceipt(_NextRecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Quantity for line : " + row
                                });
                            }

                            #endregion

                            #region Check for Warehouse

                            if (item.WhsCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftReceipt(_NextRecId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse for line : " + row });
                            }
                            System.Data.DataTable dtWhs = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" ProductionReceiptController : Detail Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftReceipt(_NextRecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail Warehouse does not exist : " + item.WhsCode
                                });
                            }
                            #endregion

                            #region Check for Project

                            if (item.Project.ToString().Length > 0)
                            {


                                System.Data.DataTable dtProject = new();
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OPRJ WHERE PrjCode = @proj AND Locked = 'N' and Active = 'Y' ";
                                _logger.LogInformation(" ProductionReceiptController : Detail Project Query : {q} ", _Query.ToString());
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", item.Project);
                                oAdptr.Fill(dtProject);
                                QITcon.Close();

                                if (dtProject.Rows.Count <= 0)
                                {
                                    this.DeleteDraftReceipt(_NextRecId);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "Detail Project does not exist : " + item.Project
                                    });
                                }
                            }
                            #endregion

                            #region Check for TransType 

                            if (item.TransType.ToString().ToUpper() != "C" && item.TransType.ToString().ToUpper() != "R")
                            {
                                this.DeleteDraftReceipt(_NextRecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Trans Type Values : C:Complete / R:Reject for line : " + row
                                });
                            }

                            #endregion

                            #region Save Detail

                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail
                            (
                                BranchId, RecId, RecDetId, BaseLine, LineNum, ItemCode, ItemName, Qty, TransType, 
                                UoMCode, WhsCode, Project, DistRule
                            ) 
                            VALUES 
                            (
                                @bId, @RecId, @recDetId, @baseLine, @lineNum, @itemCode, @itemName, @Qty, @transType, 
                                @uomCode, @whsCode, @proj, @distRule
                            )";
                            _logger.LogInformation(" ProductionReceiptController : SaveDraftProReceiptOfRejectedProReceiptDetail() Query for line " + row + " : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@RecId", _NextRecId);
                            cmd.Parameters.AddWithValue("@recDetId", row);
                            cmd.Parameters.AddWithValue("@baseLine", item.BaseLineNum.ToUpper() == "N" ? DBNull.Value : item.BaseLineNum);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", item.ItemName);
                            cmd.Parameters.AddWithValue("@Qty", item.Qty);
                            cmd.Parameters.AddWithValue("@transType", item.TransType);
                            cmd.Parameters.AddWithValue("@uomCode", item.UoMCode);
                            cmd.Parameters.AddWithValue("@distRule", item.DistRule);
                            cmd.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            cmd.Parameters.AddWithValue("@proj", item.Project);

                            intNum = 0;
                            try
                            {
                                QITcon.Open();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex)
                            {
                                this.DeleteDraftReceipt(_NextRecId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    StatusMsg = "For line : " + row + " Error : " + ex.Message.ToString()
                                });
                            }

                            if (intNum > 0)
                                SucessCount = SucessCount + 1;

                            #endregion

                        }

                        #endregion
                    }

                    #endregion

                    if (SucessCount == itemCount && SucessCount > 0)
                        return Ok(new
                        {
                            StatusCode = "200",
                            IsSaved = "Y",
                            RecId = _NextRecId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftReceipt(_NextRecId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Draft Production Receipt failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteDraftReceipt(_NextRecId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : SaveDraftProReceiptOfRejectedProReceipt Error : " + ex.ToString());
                this.DeleteDraftReceipt(_NextRecId);
                _logger.LogError("Error in ProductionReceiptController : SaveDraftProReceiptOfRejectedProReceipt() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion


        #region Save Production Receipt


        [HttpPost("SaveProductionReceipt")]
        public async Task<IActionResult> SaveProductionReceipt([FromBody] SaveProductionReceipt payload)
        {
            string _IsSaved = "N";


            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : SaveProductionReceipt() ");

                if (payload != null)
                {
                    #region Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.RecId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Receipt Id" });

                    #endregion

                    #region Get Production Receipt Header Data

                    System.Data.DataTable dtProRec = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header WHERE RecId = @RecId AND ISNULL(BranchId, @bId) = @bId ";
                    _logger.LogInformation(" ProductionReceiptController : Header data Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@RecId", payload.RecId);
                    oAdptr.Fill(dtProRec);
                    QITcon.Close();

                    if (dtProRec.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "No such Production Receipt exists"
                        });
                    else
                    {
                        if (dtProRec.Rows[0]["Action"].ToString() != "A")
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Production Receipt must be approved first"
                            });
                    }
                    #endregion

                    #region Get Production Receipt Detail Data

                    System.Data.DataTable dtProRecDetail = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail 
                    WHERE RecId = @RecId AND ISNULL(BranchId, @bId) = @bId 
                    ORDER BY LineNum ";
                    _logger.LogInformation(" ProductionReceiptController : Detail data Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@RecId", payload.RecId);
                    oAdptr.Fill(dtProRecDetail);
                    QITcon.Close();

                    #endregion

                    #region Validate Item 

                    //int draftItemCount = dtProRecDetail.Rows.Count;
                    //int payloadItemCount = payload.recDetails.Count();

                    //if (draftItemCount != payloadItemCount)
                    //    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide all draft items for Production Receipt" });
                    #endregion

                    #region Save Production Receipt
                    var (success, errorMsg) = await objGlobal.ConnectSAP();
                    if (success)
                    {
                        int _Line = 0;

                        Documents productionReceipt = (Documents)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);
                        productionReceipt.Series = (int)dtProRec.Rows[0]["Series"];
                        productionReceipt.DocDate = (DateTime)dtProRec.Rows[0]["PostingDate"];
                        productionReceipt.Reference2 = dtProRec.Rows[0]["Ref2"].ToString();
                        productionReceipt.Comments = dtProRec.Rows[0]["DraftRemark"].ToString();
                        productionReceipt.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                        if (Global.gAllowBranch == "Y")
                            productionReceipt.BPL_IDAssignedToInvoice = payload.BranchId;

                        foreach (var item in payload.recDetails)
                        {
                            //if (item.BaseLine.ToString() == "N")
                            //    productionReceipt.Lines.ItemCode = payload.ProductNo;

                            productionReceipt.Lines.Quantity = item.TotalQty; // Set the quantity 
                            productionReceipt.Lines.BaseType = 202;
                            productionReceipt.Lines.BaseEntry = (int)dtProRec.Rows[0]["ProOrdDocEntry"];
                            if (item.BaseLine.ToString() != "N")
                                productionReceipt.Lines.BaseLine = int.Parse(item.BaseLine);
                            //productionIssue.Lines.WarehouseCode = item.WhsCode;

                            if (item.ItemMngBy.ToLower() == "s")
                            {
                                int i = 0;
                                foreach (var serial in item.batchSerialDet)
                                {
                                    if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                    {
                                        productionReceipt.Lines.SerialNumbers.SetCurrentLine(i);
                                        productionReceipt.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                        productionReceipt.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                        productionReceipt.Lines.SerialNumbers.Quantity = serial.Qty;
                                        productionReceipt.Lines.SerialNumbers.Add();

                                        i = i + 1;
                                    }
                                }
                            }
                            else if (item.ItemMngBy.ToLower() == "b")
                            {
                                int _batchLine = 0;
                                foreach (var batch in item.batchSerialDet)
                                {
                                    if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                    {
                                        productionReceipt.Lines.BatchNumbers.BaseLineNumber = _Line;
                                        productionReceipt.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                        productionReceipt.Lines.BatchNumbers.Quantity = batch.Qty;
                                        productionReceipt.Lines.BatchNumbers.Add();

                                        _batchLine = _batchLine + 1;
                                    }
                                }
                            }

                            productionReceipt.Lines.Add();
                            _Line = _Line + 1;
                        }

                        int addResult = productionReceipt.Add();

                        if (addResult != 0)
                        {
                            string msg = "(" + objGlobal.oComp.GetLastErrorCode() + ") " + objGlobal.oComp.GetLastErrorDescription();
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = msg
                            });
                        }
                        else
                        {
                            int _docEntry = int.Parse(objGlobal.oComp.GetNewObjectKey());

                            #region Get Production Receipt Data from SAP
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            System.Data.DataTable dtSAPProRec = new();
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OIGN where DocEntry = @docEntry  ";
                            _logger.LogInformation(" ProductionReceiptController : SaveProductionReceipt : Get Production Receipt Data from SAP : Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", _docEntry);
                            oAdptr.Fill(dtSAPProRec);
                            QITcon.Close();
                            int _docNum = int.Parse(dtSAPProRec.Rows[0]["DocNum"].ToString());
                            #endregion

                            #region Update Production Receipt Table
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" 
                            UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header 
                            SET DocEntry = @docEntry, DocNum = @docNum 
                            WHERE RecId = @recId";
                            _logger.LogInformation(" ProductionReceiptController : SaveProductionReceipt : Update Production Receipt Table Query : {q} ", _Query.ToString());
                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@docEntry", _docEntry);
                            cmd.Parameters.AddWithValue("@docNum", _docNum);
                            cmd.Parameters.AddWithValue("@recId", payload.RecId);

                            QITcon.Open();
                            int rowUpdated = cmd.ExecuteNonQuery();
                            QITcon.Close();

                            if (rowUpdated > 0)
                            {
                                _IsSaved = "Y";
                            }
                            else
                            {
                                _IsSaved = "N";
                                return Ok(new
                                {
                                    StatusCode = "200",
                                    IsSaved = "N",
                                    DocEntry = _docEntry,
                                    DocNum = _docNum,
                                    StatusMsg = "Problem in updating Production Receipt Table"
                                });
                            }
                            #endregion

                            objGlobal.oComp.Disconnect();
                            return Ok(new
                            {
                                StatusCode = "200",
                                IsSaved = "Y",
                                DocEntry = _docEntry,
                                DocNum = _docNum,
                                StatusMsg = "Production Receipt Saved Successfully"
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = errorMsg
                        });
                    }

                    #endregion
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : SaveProductionReceipt Error : " + ex.ToString());
                _logger.LogError("Error in ProductionReceiptController : SaveProductionReceipt() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion

        private bool DeleteDraftReceipt(int p_RecId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionReceiptController : DeleteDraftReceipt() ");

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Detail WHERE RecId = @RecId
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header WHERE RecId = @RecId
                ";

                _logger.LogInformation(" ProductionReceiptController : DeleteDraftReceipt Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@RecId", p_RecId);
                QITcon.Open();
                int intNum = cmd.ExecuteNonQuery();
                QITcon.Close();

                if (intNum > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionReceiptController : DeleteDraftReceipt Error : " + ex.ToString());
                _logger.LogError("Error in ProductionReceiptController : DeleteDraftReceipt() :: {ex}", ex.ToString());
                return false;
            }
        }
    }
}
