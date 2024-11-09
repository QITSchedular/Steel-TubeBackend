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
    public class ProductionIssueController : ControllerBase
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
        private readonly ILogger<ProductionIssueController> _logger;

        public ProductionIssueController(IConfiguration configuration, ILogger<ProductionIssueController> logger)
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
                objGlobal.WriteLog(" Error in ProductionIssueController :: " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController :: {ex}" + ex.ToString());
            }
        }


        #region Fill data on Page Load

        [HttpGet("GetProductionOrderHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderHelpforIssue>>> GetProductionOrderHelp()
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : GetProductionOrderHelp() ");

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
            
                _Query = @" 
                SELECT A.* FROM 
                (
	                SELECT T0.[DocEntry], T0.[DocNum], T1.[SeriesName], 
		                   T0.PostDate PostingDate, T0.[ItemCode] ProductNo, T0.[ProdName] ProductName, 
                           CAST(T0.PlannedQty as numeric(19,3)) PlannedQty, CAST(T0.CmpltQty as numeric(19,3)) CompletedQty,
                           T0.Project, T0.Warehouse WhsCode, T0.OcrCode DistRule, 'Released' Status, T0.Uom UomCode, T0.Comments Remark
	                FROM  " + Global.SAP_DB + @".dbo.OWOR T0  
		                  INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 T1  ON  T0.[Series] = T1.[Series]   
	                WHERE T0.[Status] = 'R'  AND  
		                  ( ( ( T0.[Type] = 'S'  OR  T0.[Type] = 'P' ) AND   
				                EXISTS 
				                (
					                SELECT U0.[DocEntry] FROM " + Global.SAP_DB + @".dbo.WOR1 U0  
					                WHERE T0.[DocEntry] = U0.[DocEntry] AND U0.[IssueType] = 'M' AND U0.[PlannedQty] > U0.[IssuedQty]  
				                ) 
			                  ) OR  
                              ( T0.[Type] = 'D'  AND   
					            EXISTS 
					            (
							        SELECT U0.[DocEntry] FROM " + Global.SAP_DB + @".dbo.WOR1 U0  
							        WHERE T0.[DocEntry] = U0.[DocEntry] AND U0.[IssueType] = 'M' AND U0.[IssuedQty] > 0.00 
					            ) 
					          )
		                  )   
                ) as A INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header B ON A.DocEntry = B.DocEntry
                ";

                _logger.LogInformation(" ProductionIssueController : GetProductionOrderHelp() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderHelpforIssue> obj = new List<ProductionOrderHelpforIssue>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderHelpforIssue>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : GetProductionOrderHelp Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : GetProductionOrderHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductionItemHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderItemHelpforIssue>>> GetProductionItemHelp(int BranchId, int DocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : GetProductionItemHelp() ");

                #region Validation

                if (DocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide DocEntry" });

                System.Data.DataTable dtPro = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE DocEntry = @docEntry AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionIssueController : GetProductionItemHelp : Pro Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Order exists"
                    });
                dtPro = null;
                #endregion

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT  *,
                        CAST(ISNULL(A.PlannedQty,0) as numeric(19,3)) - CAST(ISNULL(A.UsedQty, 0) as numeric(19,3)) PendingQty
                FROM 
                (
                SELECT T0.DocEntry, T0.DocNum, T1.LineNum, T1.ItemCode, T1.ItemName, T1.VisOrder,
	                   CAST(ISNULL(T1.BaseQty,0) as numeric(19,3)) BaseQty, T1.wareHouse WhsCode, 
                       CAST(ISNULL(T1.PlannedQty,0) as numeric(19,3)) PlannedQty, 
                       CAST(ISNULL(T1.IssuedQty, 0) as numeric(19,3)) IssuedQty,  
                       -- CAST(ISNULL(T1.PlannedQty,0) as numeric(19,3)) - CAST(ISNULL(T1.IssuedQty, 0) as numeric(19,3)) PendingQty,
	                   CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                              WHERE Z.WhsCode = T1.wareHouse and Z.ItemCode = T1.ItemCode
                       ) as numeric(19,3)) WhsQty, 
                       ISNULL(CAST( ( SELECT SUM(Z.Qty) from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail Z 
                                      INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header Z1 ON Z.IssId = Z1.IssId
		                       WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS = T1.ItemCode and Z1.Action IN ('P', 'A') and Z1.ProOrdDocEntry = T0.DocEntry
		                     ) as numeric(19,3)
                       ), 0) -
		                ISNULL(CAST( (SELECT SUM(Z.Qty) FROM " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Detail Z 
                                      INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Header Z1 ON Z.RetId = Z1.RetId 
				                WHERE Z1.ProOrdDocEntry = T0.DocEntry AND Z.ItemCode COLLATE SQL_Latin1_General_CP850_CI_AS = T1.ItemCode 
				                ) AS numeric(19,3)
	                    ) ,0)
		                UsedQty,
                       CAST(ISNULL(T3.OnHand,0) as numeric(19,3)) ItemStock, T1.UomCode, T1.OcrCode DistRule, T1.Project
                FROM  " + Global.SAP_DB + @".dbo.OWOR T0  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 T1 ON T0.DocEntry = T1.DocEntry   
	                  INNER JOIN " + Global.SAP_DB + @".dbo.B1_DocItemView T2 ON T1.ItemType = T2.DocItemType AND T1.ItemCode = T2.DocItemCode   
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OITM T3 ON T3.ItemCode = T1.ItemCode
                WHERE T1.IssueType = 'M'  AND  T0.DocEntry = @docEntry AND 
                      (((T0.Type = 'S'  OR  T0.Type = 'P' ) AND  T1.PlannedQty > T1.IssuedQty ) OR  (T0.Type = 'D'  AND  T1.IssuedQty > 0 ))  
                ) AS A
                ORDER BY A.DocEntry, A.VisOrder, A.LineNum 
                ";

                _logger.LogInformation(" ProductionIssueController : GetProductionItemHelp() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderItemHelpforIssue> obj = new List<ProductionOrderItemHelpforIssue>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderItemHelpforIssue>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : GetProductionItemHelp Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : GetProductionItemHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #endregion


        #region Save Draft Production Issue

        [HttpPost("SaveDraftProductionIssue")]
        public async Task<IActionResult> SaveDraftProductionIssue([FromBody] SaveDraftProductionIssue payload)
        {
            string _IsSaved = "N";
            int _IssId = 0;

            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : SaveDraftProductionIssue() ");

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.issDetail.Count();

                    #region Get IssId  
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(IssId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header A  ";
                    _logger.LogInformation(" ProductionIssueController : Get IssId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    await QITcon.OpenAsync();
                    _IssId = (Int32)cmd.ExecuteScalar();
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
                    _logger.LogInformation(" ProductionIssueController : User Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
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
                    INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header
                    (
                        BranchId, IssId, Series, DocEntry, DocNum, PostingDate, Ref2, ProOrdDocEntry,  
                        EntryDate, EntryUser, DraftRemark, Action, ActionDate, PrevIssId
                    ) 
                    VALUES 
                    (
                        @bId, @IssId, @series, @docEntry, @docNum, @pDate, @ref2, @proOrdDocEntry,   
                        @eDate, @eUser, @remark, @action, @aDate, 0
                    )";

                    _logger.LogInformation(" ProductionIssueController : SaveDraftProductionIssue() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@IssId", _IssId);
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
                        await QITcon.OpenAsync();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteDraftIssue(_IssId);
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
                        foreach (var item in payload.issDetail)
                        {
                            row = row + 1;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftIssue(_IssId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }
                            System.Data.DataTable dtItem = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);

                            _Query = @" 
                            SELECT A.* FROM " + Global.SAP_DB + @".dbo.WOR1 A 
                            WHERE A.DocEntry = @proOrdDocEntry and A.ItemCode = @itemCode and A.LineNum = @baseLineNum
                            ";

                            _logger.LogInformation(" ProductionIssueController : Item Code Query : {q} ", _Query.ToString());
                            await QITcon.OpenAsync();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@baseLineNum", item.BaseLineNum);
                            oAdptr.Fill(dtItem);
                            QITcon.Close();

                            if (dtItem.Rows.Count <= 0)
                            {
                                this.DeleteDraftIssue(_IssId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row + " in Production Order"
                                });
                            }

                            #endregion

                            #region Check for Qty

                            if (item.Qty.ToString() == "0")
                            {
                                this.DeleteDraftIssue(_IssId);
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
                                this.DeleteDraftIssue(_IssId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse for line : " + row });
                            }

                            System.Data.DataTable dtWhs = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" ProductionIssueController : Detail Warehouse Query : {q} ", _Query.ToString());
                            await QITcon.OpenAsync();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftIssue(_IssId);
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
                                _logger.LogInformation(" ProductionIssueController : Detail Project Query : {q} ", _Query.ToString());
                                await QITcon.OpenAsync();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", item.Project);
                                oAdptr.Fill(dtProject);
                                QITcon.Close();

                                if (dtProject.Rows.Count <= 0)
                                {
                                    this.DeleteDraftIssue(_IssId);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "Detail Project does not exist : " + item.Project
                                    });
                                }
                            }
                            #endregion

                            #region Save Detail

                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail
                            (
                                BranchId, IssId, IssDetId, BaseLine, LineNum, ItemCode, ItemName, Qty,  
                                UoMCode, WhsCode, Project, DistRule
                            ) 
                            VALUES 
                            (
                                @bId, @IssId, @IssDetId, @baseLine, @lineNum, @itemCode, @itemName, @Qty, 
                                @uomCode, @whsCode, @proj, @distRule
                            )";
                            _logger.LogInformation(" ProductionIssueController : SaveDraftProductionIssueDetail() Query for line " + row + " : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@IssId", _IssId);
                            cmd.Parameters.AddWithValue("@IssDetId", row);
                            cmd.Parameters.AddWithValue("@baseLine", item.BaseLineNum);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", item.ItemName);
                            cmd.Parameters.AddWithValue("@Qty", item.Qty);
                            cmd.Parameters.AddWithValue("@uomCode", item.UoMCode);
                            cmd.Parameters.AddWithValue("@distRule", item.DistRule);
                            cmd.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            cmd.Parameters.AddWithValue("@proj", item.Project);

                            intNum = 0;
                            try
                            {
                                await QITcon.OpenAsync();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex)
                            {
                                this.DeleteDraftIssue(_IssId);
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
                            IssId = _IssId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftIssue(_IssId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            IssId = _IssId,
                            StatusMsg = "Draft Production Issue failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteDraftIssue(_IssId);
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        IsSaved = _IsSaved,
                        IssId = _IssId,
                        StatusMsg = "Details not found"
                    });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : SaveDraftProductionIssue Error : " + ex.ToString());
                this.DeleteDraftIssue(_IssId);
                _logger.LogError("Error in ProductionIssueController : SaveDraftProductionIssue() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
            finally
            {
                QITcon.Close();
            }
        }

        #endregion


        #region Display Production Issue List in Grid

        [HttpGet("DisplayProductionIssue")]
        public async Task<ActionResult<IEnumerable<DisplayProductionIssue>>> DisplayProductionIssue(string UserName, string UserType)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : DisplayProductionIssue() ");

                System.Data.DataTable dtIss = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                string _strWhere = string.Empty;

                if (UserName.ToLower() != "admin")
                {
                    if (UserType.ToLower() == "c")
                        _strWhere = " and A.EntryUser = @uName ";
                }

                _Query = @" 
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail WHERE IssId IN
                (
                   SELECT IssId FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header 
                   WHERE CAST(PostingDate AS DATE) < CAST(getdate() AS DATE) AND DocNum <= 0 and Action = 'A' 
                );

                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header
                WHERE CAST(PostingDate AS DATE) < CAST(getdate() AS DATE) AND DocNum <= 0 and Action = 'A'  ;

                SELECT A.IssId, A.ProOrdDocEntry, C.DocNum ProOrdDocNum,
		               C.ItemCode ProductNo, C.ProdName ProductName, 
		               A.Series, B.SeriesName, 
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
                       CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
                       A.PostingDate, A.Ref2, A.draftRemark Remark,
                       CASE WHEN A.Action <> 'A' then '-' when A.Action = 'A' and A.DocNum = 0 THEN 'No' ELSE 'Yes' END BatchSelected
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header   A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
                WHERE A.IssId NOT IN ( SELECT PrevIssId FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header ) AND C.Status <> 'L'
                and A.IssId NOT IN (
					select Z.IssId   ---,  DATEDIFF(DAY, Z.ActionDate, getdate())
					from " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header Z where Z.Action = 'R' 
					and DATEDIFF(DAY, Z.ActionDate, getdate()) >= (select RejectDocDays from " + Global.QIT_DB + @".dbo.QIT_Config_Master)
				) and A.DocNum <= 0  
                " + _strWhere + @"
                ";

                _logger.LogInformation(" ProductionIssueController : DisplayProductionIssue() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@uName", UserName);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count > 0)
                {
                    List<DisplayProductionIssue> obj = new List<DisplayProductionIssue>();
                    dynamic arData = JsonConvert.SerializeObject(dtIss);
                    obj = JsonConvert.DeserializeObject<List<DisplayProductionIssue>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : DisplayProductionIssue Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : DisplayProductionIssue() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Production Issue with Detail on Grid Click

        [HttpGet("GetProductionIssueDetails")]
        public async Task<ActionResult<IEnumerable<IssueHeader>>> GetProductionIssueDetails(int BranchId, int IssId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : GetProductionIssueDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Iss Id

                if (IssId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Issue Id" });

                System.Data.DataTable dtIss = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE IssId = @IssId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionIssueController : Iss Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", IssId);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Issue exists"
                    });
                #endregion

                #region Check for Iss Id - Initiated again or not

                dtIss = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE PrevIssId = @IssId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionIssueController : Iss Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", IssId);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count > 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "A new Production Issue has already been initiated for this rejected issue"
                    });
                }
                #endregion

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                WITH data AS
                (
                    SELECT A.IssId, A.ProOrdDocEntry, C.DocNum ProOrdDocNum,
		                   C.ItemCode ProductNo, C.ProdName ProductName, A.Series, B.SeriesName, B.Indicator PeriodIndicator, 
                           CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
                           CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
                           CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
                           A.PostingDate, A.Ref2, A.draftRemark Remark, A.ActionRemark Reason,
					       D.IssDetId, D.ItemCode, D.ItemName, D.BaseLine, D.LineNum, CAST(D.Qty as numeric(19,3)) Qty,
                           CAST(ISNULL(G.BaseQty,0) as numeric(19,3)) BaseQty, CAST(ISNULL(G.PlannedQty,0) as numeric(19,3)) PlannedQty, 
                           CAST(ISNULL(G.IssuedQty, 0) as numeric(19,3)) IssuedQty,  
	                       CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                             WHERE Z.WhsCode = G.wareHouse and Z.ItemCode = G.ItemCode
                           ) as numeric(19,3)) WhsQty,
                           D.UomCode, D.WhsCode, D.Project, D.DistRule,
                           CASE WHEN F.ManSerNum = 'N' and  F.ManBtchNum = 'N' THEN 'N' 
                                WHEN F.ManSerNum = 'N' and  F.ManBtchNum = 'Y' THEN 'B' 
                                WHEN F.ManSerNum = 'Y' and  F.ManBtchNum = 'N' THEN 'S' 
                           END ItemMngBy
                    FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header   A
                    INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                    INNER JOIN " + Global.SAP_DB + @".dbo.OWOR C On C.DocEntry = A.ProOrdDocEntry
				    INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail D On A.IssId = D.IssId 
                    INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 G ON C.DocEntry = G.DocEntry and G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                    WHERE A.IssId = @IssId AND ISNULL(A.BranchId, @bId) = @bId
                )
                SELECT *, ISNULL(PlannedQty,0) - ISNULL(IssuedQty,0) PendingQty,
                       case when ItemMngBy = 'B' then 'Batch' when ItemMngBy = 'S' then 'Serial' when ItemMngBy = 'N' then 'None' end ItemMngByName,
	                   CASE 
                           WHEN EXISTS (SELECT 1 FROM data WHERE ItemMngBy = 'B') AND EXISTS (SELECT 1 FROM data WHERE ItemMngBy = 'N') THEN 'A'
                           ELSE ItemMngBy
                       END AS ItemsType
                FROM data
                ";
                #endregion

                _logger.LogInformation(" ProductionIssueController : GetProductionIssueDetails() Query : {q} ", _Query.ToString());
                dtIss = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", IssId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count > 0)
                {
                    List<IssueHeader> obj = new List<IssueHeader>();
                    List<IssueDetails> issDetail = new List<IssueDetails>();
                    dynamic arData = JsonConvert.SerializeObject(dtIss);
                    issDetail = JsonConvert.DeserializeObject<List<IssueDetails>>(arData.ToString());

                    obj.Add(new IssueHeader()
                    {
                        IssId = int.Parse(dtIss.Rows[0]["IssId"].ToString()),
                        ProOrdDocEntry = int.Parse(dtIss.Rows[0]["ProOrdDocEntry"].ToString()),
                        ProOrdDocNum = int.Parse(dtIss.Rows[0]["ProOrdDocNum"].ToString()),
                        ProductNo = dtIss.Rows[0]["ProductNo"].ToString(),
                        ProductName = dtIss.Rows[0]["ProductName"].ToString(),
                        DocEntry = dtIss.Rows[0]["DocEntry"].ToString(),
                        DocNum = dtIss.Rows[0]["DocNum"].ToString(),
                        PeriodIndicator = dtIss.Rows[0]["PeriodIndicator"].ToString(),
                        Series = int.Parse(dtIss.Rows[0]["Series"].ToString()),
                        SeriesName = dtIss.Rows[0]["SeriesName"].ToString(),
                        State = dtIss.Rows[0]["State"].ToString(),
                        PostingDate = dtIss.Rows[0]["PostingDate"].ToString(),
                        Ref2 = dtIss.Rows[0]["Ref2"].ToString(),
                        Remark = dtIss.Rows[0]["Remark"].ToString(),
                        Reason = dtIss.Rows[0]["Reason"].ToString(),
                        ItemsType = dtIss.Rows[0]["ItemsType"].ToString(),
                        issDetail = issDetail
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
                objGlobal.WriteLog("ProductionIssueController : GetProductionIssueDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : GetProductionIssueDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Approve/Reject Production Issue

        [HttpPost("VerifyDraftProductionIssue")]
        public async Task<IActionResult> VerifyDraftProductionIssue([FromBody] VerifyDraftProductionIssue payload)
        {
            string _IsSaved = "N";

            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : VerifyDraftProductionIssue() ");

                if (payload != null)
                {
                    #region Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    #region Check for Iss Id

                    if (payload.IssId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Issue Id" });

                    System.Data.DataTable dtIT = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE IssId = @IssId AND ISNULL(BranchId, @bId) = @bId ";
                    _logger.LogInformation(" ProductionIssueController : VerifyDraftProductionIssue : Iss Id Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", payload.IssId);
                    oAdptr.Fill(dtIT);
                    QITcon.Close();

                    if (dtIT.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "No such Production Issue exists"
                        });
                    else
                    {
                        if (dtIT.Rows[0]["Action"].ToString() != "P")
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Production Issue is already approved or rejected"
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

                    #region Update Production Issue
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header
                    SET Action = @action, ActionDate = @aDate, ActionUser = @aUser, ActionRemark = @remark 
                    WHERE IssId = @IssId and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" ProductionIssueController : VerifyDraftProductionIssue : Update Production Issue Action Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@action", payload.Action.ToUpper());
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@aUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.ActionRemark);
                    cmd.Parameters.AddWithValue("@IssId", payload.IssId);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);

                    await QITcon.OpenAsync();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    if (payload.Action.ToUpper() == "A")
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Production Issue approved" });
                    else
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Production Issue rejected" });
                    #endregion
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : VerifyDraftProductionIssue Error : " + ex.ToString());
                _logger.LogError("Error in ProductionIssueController : VerifyDraftProductionIssue() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion


        #region Save Draft Production Issue for Rejected one

        [HttpPost("SaveDraftProIssueOfRejectedProIssue")]
        public async Task<IActionResult> SaveDraftProIssueOfRejectedProIssue(int IssId, [FromBody] SaveDraftProductionIssue payload)
        {
            string _IsSaved = "N";
            int _NextIssId = 0;

            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : SaveDraftProIssueOfRejectedProIssue() ");

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.issDetail.Count();

                    #region Get IssId  
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(IssId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header A  ";
                    _logger.LogInformation(" ProductionIssueController : Get IssId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    await QITcon.OpenAsync();
                    _NextIssId = (Int32)cmd.ExecuteScalar();
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
                    _logger.LogInformation(" ProductionIssueController : User Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
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
                    INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header
                    (
                        BranchId, IssId, Series, DocEntry, DocNum, PostingDate, Ref2, ProOrdDocEntry,  
                        EntryDate, EntryUser, DraftRemark, Action, ActionDate, PrevIssId
                    ) 
                    VALUES 
                    (
                        @bId, @IssId, @series, @docEntry, @docNum, @pDate, @ref2, @proOrdDocEntry,   
                            @eDate, @eUser, @remark, @action, @aDate, @prevIssId
                    )";
                    _logger.LogInformation(" ProductionIssueController : SaveDraftProIssueOfRejectedProIssue() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@IssId", _NextIssId);
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
                    cmd.Parameters.AddWithValue("@prevIssId", IssId);

                    int intNum = 0;
                    try
                    {
                        await QITcon.OpenAsync();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteDraftIssue(_NextIssId);
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
                        foreach (var item in payload.issDetail)
                        {
                            row = row + 1;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftIssue(_NextIssId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }
                            System.Data.DataTable dtItem = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);

                            _Query = @" 
                            SELECT A.* FROM " + Global.SAP_DB + @".dbo.WOR1 A 
                            WHERE A.DocEntry = @proOrdDocEntry and A.ItemCode = @itemCode and A.LineNum = @baseLineNum
                            ";

                            _logger.LogInformation(" ProductionIssueController : Item Code Query : {q} ", _Query.ToString());
                            await QITcon.OpenAsync();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@baseLineNum", item.BaseLineNum);
                            oAdptr.Fill(dtItem);
                            QITcon.Close();

                            if (dtItem.Rows.Count <= 0)
                            {
                                this.DeleteDraftIssue(_NextIssId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row + " in Production Order"
                                });
                            }
                            #endregion

                            #region Check for Qty

                            if (item.Qty.ToString() == "0")
                            {
                                this.DeleteDraftIssue(_NextIssId);
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
                                this.DeleteDraftIssue(_NextIssId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse for line : " + row });
                            }
                            System.Data.DataTable dtWhs = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" ProductionIssueController : Detail Warehouse Query : {q} ", _Query.ToString());
                            await QITcon.OpenAsync();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftIssue(_NextIssId);
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
                                _logger.LogInformation(" ProductionIssueController : Detail Project Query : {q} ", _Query.ToString());
                                await QITcon.OpenAsync();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", item.Project);
                                oAdptr.Fill(dtProject);
                                QITcon.Close();

                                if (dtProject.Rows.Count <= 0)
                                {
                                    this.DeleteDraftIssue(_NextIssId);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "Detail Project does not exist : " + item.Project
                                    });
                                }
                            }
                            #endregion

                            #region Save Detail

                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail
                            (
                                BranchId, IssId, IssDetId, BaseLine, LineNum, ItemCode, ItemName, Qty,  
                                UoMCode, WhsCode, Project, DistRule
                            ) 
                            VALUES 
                            (
                                @bId, @IssId, @IssDetId, @baseLine, @lineNum, @itemCode, @itemName, @Qty, 
                                @uomCode, @whsCode, @proj, @distRule
                            )";
                            _logger.LogInformation(" ProductionIssueController : SaveDraftProIssueOfRejectedProIssueDetail() Query for line " + row + " : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@IssId", _NextIssId);
                            cmd.Parameters.AddWithValue("@IssDetId", row);
                            cmd.Parameters.AddWithValue("@baseLine", item.BaseLineNum);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", item.ItemName);
                            cmd.Parameters.AddWithValue("@Qty", item.Qty);
                            cmd.Parameters.AddWithValue("@uomCode", item.UoMCode);
                            cmd.Parameters.AddWithValue("@distRule", item.DistRule);
                            cmd.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            cmd.Parameters.AddWithValue("@proj", item.Project);

                            intNum = 0;
                            try
                            {
                                await QITcon.OpenAsync();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex)
                            {
                                this.DeleteDraftIssue(_NextIssId);
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
                            IssId = _NextIssId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftIssue(_NextIssId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Draft Production Issue failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteDraftIssue(_NextIssId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : SaveDraftProIssueOfRejectedProIssue Error : " + ex.ToString());
                this.DeleteDraftIssue(_NextIssId);
                _logger.LogError("Error in ProductionIssueController : SaveDraftProIssueOfRejectedProIssue() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion


        #region Save Production Issue flow

        #region Display Batch Item information before saving Production Issue

        [HttpGet("GetBatchItemDetails")]
        public async Task<ActionResult<IEnumerable<BatchSerialItemDetails>>> GetBatchItemDetails(int BranchId, int IssId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : GetBatchItemDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Iss Id

                if (IssId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Issue Id" });

                System.Data.DataTable dtIss = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE IssId = @IssId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionIssueController : Iss Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", IssId);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Issue exists"
                    });
                else
                {
                    if (dtIss.Rows[0]["Action"].ToString() != "A")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Issue must be approved first"
                        });
                }
                #endregion

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.ItemCode, A.ItemName, A.WhsCode, B.WhsName, CAST(A.Qty as numeric(19,3)) Qty
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail A 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OWHS B ON A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = B.WhsCode
                     INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE A.IssId = @IssId AND ISNULL(A.BranchId, @bId) = @bId AND C.ManBtchNum = 'Y'
                ";
                #endregion

                _logger.LogInformation(" ProductionIssueController : GetBatchItemDetails() Query : {q} ", _Query.ToString());
                dtIss = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", IssId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count > 0)
                {
                    List<BatchSerialItemDetails> obj = new List<BatchSerialItemDetails>();
                    dynamic arData = JsonConvert.SerializeObject(dtIss);
                    obj = JsonConvert.DeserializeObject<List<BatchSerialItemDetails>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : GetBatchItemDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : GetBatchItemDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Batch Data while clicking on Item on Batch Grid

        [HttpGet("GetBatchData")]
        public async Task<ActionResult<IEnumerable<BatchSerialData>>> GetBatchData(int BranchId, string ItemCode, string WhsCode)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : GetBatchData() ");

                #region Validation

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                if (ItemCode.ToString() == string.Empty || ItemCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });

                if (WhsCode.ToString() == string.Empty || WhsCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Warehouse" });

                #endregion

                #region Create View

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                Guid _id = Guid.NewGuid();
                _Query = @" 
                IF OBJECT_ID('vw_IssBatchData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_IssBatchData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" ProductionIssueController : DROP Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @"
                DECLARE @DynamicQuery NVARCHAR(MAX)
                SET @DynamicQuery = '
                CREATE VIEW vw_IssBatchData" + _id.ToString().Replace("-", "_") + @" AS
                SELECT T0.ItemCode, T0.SysNumber, T0.MdAbsEntry, T0.Quantity, 
                       CAST(T0.CommitQty as numeric(19,3)) CommitQty, CAST(T0.CountQty  as numeric(19,3)) CountQty
                FROM " + Global.SAP_DB + @".dbo.OBTQ T0  
                INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T1 ON T1.ItemCode = T0.ItemCode AND T1.SysNumber = T0.SysNumber
                WHERE T0.ItemCode = ''" + ItemCode + @"'' AND T0.[WhsCode] = ''" + WhsCode + @"'' AND T1.Status <= ''2'' AND T0.Quantity <> 0'
                EXEC sp_executesql @DynamicQuery";


                _logger.LogInformation(" ProductionIssueController : Create Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                #endregion

                #region Get Batch data Query

                System.Data.DataTable dtBatchData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @"
                SELECT  T1.ItemCode, T1.ItemName, T1.SysNumber, T1.DistNumber, T1.LotNumber, CAST(T0.Quantity as numeric(19,3)) AvailQty 
                FROM  vw_IssBatchData" + _id.ToString().Replace("-", "_") + @" T0  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T1 ON T1.AbsEntry = T0.MdAbsEntry    
	                  LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.OBTW T2 ON T2.MdAbsEntry = T0.MdAbsEntry AND T2.WhsCode = @whsCode   
                ORDER BY T1.AbsEntry
                ";
                _logger.LogInformation(" ProductionIssueController : GetBatchData Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                oAdptr.Fill(dtBatchData);
                QITcon.Close();
                #endregion

                #region Drop View
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                IF OBJECT_ID('vw_IssBatchData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_IssBatchData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" ProductionIssueController : DROP Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();
                #endregion

                if (dtBatchData.Rows.Count > 0)
                {
                    List<BatchSerialData> obj = new List<BatchSerialData>();
                    dynamic arData = JsonConvert.SerializeObject(dtBatchData);
                    obj = JsonConvert.DeserializeObject<List<BatchSerialData>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : GetBatchData Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : GetBatchData() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Display Serial item information before saving Production Issue

        [HttpGet("GetSerialItemDetails")]
        public async Task<ActionResult<IEnumerable<BatchSerialItemDetails>>> GetSerialItemDetails(int BranchId, int IssId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : GetSerialItemDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Iss Id

                if (IssId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Issue Id" });

                System.Data.DataTable dtIss = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE IssId = @IssId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionIssueController : Iss Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", IssId);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Issue exists"
                    });
                else
                {
                    if (dtIss.Rows[0]["Action"].ToString() != "A")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Issue must be approved first"
                        });
                }
                #endregion

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.ItemCode, A.ItemName, A.WhsCode, B.WhsName, CAST(A.Qty as numeric(19,3)) Qty
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail A 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OWHS B ON A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = B.WhsCode
                     INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE A.IssId = @IssId AND ISNULL(A.BranchId, @bId) = @bId AND C.ManSerNum = 'Y'
                ";
                #endregion

                _logger.LogInformation(" ProductionIssueController : GetSerialItemDetails() Query : {q} ", _Query.ToString());
                dtIss = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", IssId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtIss);
                QITcon.Close();

                if (dtIss.Rows.Count > 0)
                {
                    List<BatchSerialItemDetails> obj = new List<BatchSerialItemDetails>();
                    dynamic arData = JsonConvert.SerializeObject(dtIss);
                    obj = JsonConvert.DeserializeObject<List<BatchSerialItemDetails>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : GetSerialItemDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : GetSerialItemDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Serial Data while clicking on Item on Batch Grid

        [HttpGet("GetSerialData")]
        public async Task<ActionResult<IEnumerable<BatchSerialData>>> GetSerialData(int BranchId, string ItemCode, string WhsCode)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : GetSerialData() ");

                #region Validation

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                if (ItemCode.ToString() == string.Empty || ItemCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });

                if (WhsCode.ToString() == string.Empty || WhsCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Warehouse" });

                #endregion

                #region Create View

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                Guid _id = Guid.NewGuid();
                _Query = @" 
                IF OBJECT_ID('vw_IssSerialData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_IssSerialData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" ProductionIssueController : DROP Serial View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                _Query = @"
                DECLARE @DynamicQuery NVARCHAR(MAX)
                SET @DynamicQuery = '
                CREATE VIEW vw_IssSerialData" + _id.ToString().Replace("-", "_") + @" AS 
                SELECT T0.ItemCode, T0.SysNumber, T0.MdAbsEntry, T0.Quantity, 
                       CAST(T0.CommitQty as numeric(19,3)) CommitQty, CAST(T0.CountQty as numeric(19,3)) CountQty 
                FROM " + Global.SAP_DB + @".dbo.OSRQ T0  
                WHERE T0.ItemCode = ''" + ItemCode + @"'' AND T0.WhsCode = ''" + WhsCode + @"'' AND T0.[Quantity] <> 0
                '
                EXEC sp_executesql @DynamicQuery
                ";

                _logger.LogInformation(" ProductionIssueController : Create Serial View Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@itemCode", ItemCode);
                cmd.Parameters.AddWithValue("@whsCode", WhsCode);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                #endregion

                #region Get Serial data Query

                System.Data.DataTable dtSerialData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @"
                SELECT  T1.ItemCode, T1.ItemName, T1.SysNumber, T1.DistNumber, T1.LotNumber, CAST(T0.Quantity as numeric(19,3)) AvailQty
                FROM  vw_IssSerialData"" + _id.ToString().Replace(""-"", ""_"") + @"" T0  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OSRN T1 ON T1.AbsEntry = T0.MdAbsEntry    
                ORDER BY T1.AbsEntry
                ";
                _logger.LogInformation(" ProductionIssueController : GetSerialData Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtSerialData);
                QITcon.Close();
                #endregion

                #region Drop View
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                IF OBJECT_ID('vw_IssSerialData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_IssSerialData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" InventoryTransferController : DROP Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();
                #endregion

                if (dtSerialData.Rows.Count > 0)
                {
                    List<BatchSerialData> obj = new List<BatchSerialData>();
                    dynamic arData = JsonConvert.SerializeObject(dtSerialData);
                    obj = JsonConvert.DeserializeObject<List<BatchSerialData>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : GetSerialData Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionIssueController : GetSerialData() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Save Production Issue

        [HttpPost("SaveProductionIssue")]
        public async Task<IActionResult> SaveProductionIssue([FromBody] SaveProductionIssue payload)
        {
            string _IsSaved = "N";
            int _docEntry = 0;
            int _docNum = 0;

            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : SaveProductionIssue() ");

                if (payload != null)
                {
                    #region Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.IssId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Issue Id" });

                    #endregion

                    #region Get Production Issue Header Data

                    System.Data.DataTable dtProIss = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE IssId = @IssId AND ISNULL(BranchId, @bId) = @bId ";
                    _logger.LogInformation(" ProductionIssueController : Header data Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", payload.IssId);
                    oAdptr.Fill(dtProIss);
                    QITcon.Close();

                    if (dtProIss.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "No such Production Issue exists"
                        });
                    else
                    {
                        if (dtProIss.Rows[0]["Action"].ToString() != "A")
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Production Issue must be approved first"
                            });
                    }
                    #endregion

                    #region Get Production Issue Detail Data

                    System.Data.DataTable dtProIssDetail = new();
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail 
                    WHERE IssId = @IssId AND ISNULL(BranchId, @bId) = @bId 
                    ORDER BY LineNum ";
                    _logger.LogInformation(" ProductionIssueController : Detail data Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@IssId", payload.IssId);
                    oAdptr.Fill(dtProIssDetail);
                    QITcon.Close();

                    #endregion

                    #region Validate Item 

                    int draftItemCount = dtProIssDetail.Rows.Count;
                    int payloadItemCount = payload.issDetails.Count();

                    if (draftItemCount != payloadItemCount)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide all draft items for Production Issue" });
                    #endregion

                    #region Save Production Issue
                    var (success, errorMsg) = await objGlobal.ConnectSAP();
                    if (success)
                    {
                        int _Line = 0;

                        ProductionOrders proOrder = (ProductionOrders)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oProductionOrders);
                        if (proOrder.GetByKey((int)dtProIss.Rows[0]["ProOrdDocEntry"]))
                        {
                            Documents productionIssue = (Documents)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oInventoryGenExit);
                            //productionIssue.DocObjectCode = BoObjectTypes.oInventoryGenExit;
                            productionIssue.Series = (int)dtProIss.Rows[0]["Series"];
                            productionIssue.DocDate = (DateTime)dtProIss.Rows[0]["PostingDate"];
                            productionIssue.Reference2 = dtProIss.Rows[0]["Ref2"].ToString();
                            productionIssue.Comments = dtProIss.Rows[0]["DraftRemark"].ToString();
                            productionIssue.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                            if (Global.gAllowBranch == "Y")
                                productionIssue.BPL_IDAssignedToInvoice = payload.BranchId;

                            foreach (var item in payload.issDetails)
                            {
                                //productionIssue.Lines.ItemCode = item.ItemCode;

                                productionIssue.Lines.Quantity = item.TotalQty; // Set the quantity 
                                productionIssue.Lines.BaseType = 202;
                                productionIssue.Lines.BaseEntry = (int)dtProIss.Rows[0]["ProOrdDocEntry"];
                                productionIssue.Lines.BaseLine = item.BaseLine;
                                
                                //productionIssue.Lines.WarehouseCode = item.WhsCode;

                                if (item.ItemMngBy.ToLower() == "s")
                                {
                                    int i = 0;
                                    foreach (var serial in item.batchSerialDet)
                                    {
                                        if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                        {
                                            productionIssue.Lines.SerialNumbers.SetCurrentLine(i);
                                            productionIssue.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            productionIssue.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                            productionIssue.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                            productionIssue.Lines.SerialNumbers.Quantity = serial.SelectedQty;
                                            productionIssue.Lines.SerialNumbers.Add();

                                            if (serial.FromBinAbsEntry > 0)
                                            {
                                                productionIssue.Lines.BinAllocations.BinAbsEntry = serial.FromBinAbsEntry;
                                                productionIssue.Lines.BinAllocations.Quantity = serial.SelectedQty;
                                                productionIssue.Lines.BinAllocations.BaseLineNumber = _Line;
                                                productionIssue.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                productionIssue.Lines.BinAllocations.Add();

                                            }
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
                                            productionIssue.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            productionIssue.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                            productionIssue.Lines.BatchNumbers.Quantity = batch.SelectedQty;
                                            productionIssue.Lines.BatchNumbers.Add();

                                            if (batch.FromBinAbsEntry > 0)
                                            {
                                                productionIssue.Lines.BinAllocations.BinAbsEntry = batch.FromBinAbsEntry;
                                                productionIssue.Lines.BinAllocations.Quantity = batch.SelectedQty;
                                                productionIssue.Lines.BinAllocations.BaseLineNumber = _Line;
                                                productionIssue.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                productionIssue.Lines.BinAllocations.Add();
                                            }
                                            _batchLine = _batchLine + 1;
                                        }
                                    }
                                }
                                productionIssue.Lines.Add();
                                _Line = _Line + 1;
                            }

                            int addResult = productionIssue.Add();

                            objGlobal.WriteLog("ProductionIssueController : addResult : " + addResult.ToString());
                            objGlobal.WriteLog("ProductionIssueController : objGlobal.oComp.GetLastErrorDescription() : " + objGlobal.oComp.GetLastErrorDescription());

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
                                _docEntry = int.Parse(objGlobal.oComp.GetNewObjectKey());

                                #region Get Production Issue Data from SAP
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);
                                System.Data.DataTable dtSAPProIss = new();
                                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OIGE where DocEntry = @docEntry  ";
                                _logger.LogInformation(" ProductionIssueController : SaveProductionIssue : Get Production Issue Data from SAP : Query : {q} ", _Query.ToString());

                                objGlobal.WriteLog("ProductionIssueController : SaveProductionIssue Query : " + _Query.ToString() + "   DocEntry = " + _docEntry);

                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", _docEntry);
                                oAdptr.Fill(dtSAPProIss);
                                QITcon.Close();
                                _docNum = int.Parse(dtSAPProIss.Rows[0]["DocNum"].ToString());
                                #endregion

                                #region Update Production Table
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" 
                                UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header 
                                SET DocEntry = @docEntry, DocNum = @docNum 
                                WHERE IssId = @issId";

                                _logger.LogInformation(" ProductionIssueController : SaveProductionIssue : Update Production Issue Table Query : {q} ", _Query.ToString());
                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@docEntry", _docEntry);
                                cmd.Parameters.AddWithValue("@docNum", _docNum);
                                cmd.Parameters.AddWithValue("@issId", payload.IssId);

                                await QITcon.OpenAsync();
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
                                        StatusMsg = "Problem in updating Production Issue Table"
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
                                    StatusMsg = "Production Issue Saved Successfully"
                                });
                            }
                        }
                        else
                        {
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = "N",
                                StatusMsg = "No such Production Order exist !!!"
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
                objGlobal.WriteLog("ProductionIssueController : SaveProductionIssue Error : " + ex.ToString());
                _logger.LogError("Error in ProductionIssueController : SaveProductionIssue() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion

        #endregion

        private bool DeleteDraftIssue(int p_IssId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionIssueController : DeleteDraftIssue() ");

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Detail WHERE IssId = @issId
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionIssue_Header WHERE IssId = @issId
                ";
                _logger.LogInformation(" ProductionIssueController : DeleteDraftIssue Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@issId", p_IssId);
                QITcon.OpenAsync();
                int intNum = cmd.ExecuteNonQuery();
                QITcon.Close();

                if (intNum > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionIssueController : DeleteDraftIssue Error : " + ex.ToString());
                _logger.LogError("Error in ProductionIssueController : DeleteDraftIssue() :: {ex}", ex.ToString());
                return false;
            }
        }

    }
}
