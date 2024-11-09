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
    public class ReturnComponentsController : ControllerBase
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
        private readonly ILogger<ReturnComponentsController> _logger;

        public ReturnComponentsController(IConfiguration configuration, ILogger<ReturnComponentsController> logger)
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
                objGlobal.WriteLog(" Error in ReturnComponentsController :: " + ex.ToString());
                _logger.LogError(" Error in ReturnComponentsController :: {ex}" + ex.ToString());
            }
        }


        [HttpGet("GetProductionOrderHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderHelpforReturn>>> GetProductionOrderHelp(int Series)
        {
            try
            {
                _logger.LogInformation(" Calling ReturnComponentsController : GetProductionOrderHelp() ");

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT A.*, B.ProId FROM 
                (
	                SELECT A.DocEntry, A.DocNum, A.Series, B.SeriesName, 
	                       A.PostDate PostingDate, A.ItemCode ProductNo, A.ProdName ProductName, 
                           CAST(A.PlannedQty as numeric(19,3)) PlannedQty, CAST(A.CmpltQty as numeric(19,3)) CompletedQty,
                           A.Project, A.Warehouse WhsCode, A.OcrCode DistRule, 'Released' Status, A.Uom UomCode, A.Comments Remark
                    FROM  " + Global.SAP_DB + @".dbo.OWOR A  
	                      INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series  
                    WHERE A.Status = 'R' AND A.Series = @series AND
	                      (  (  (A.Type = 'S'  OR  A.Type = 'P' ) AND   
		                         EXISTS 
                                 (
                                    SELECT U0.DocEntry FROM  " + Global.SAP_DB + @".dbo.WOR1 U0  
                                    WHERE A.DocEntry = U0.DocEntry  AND  U0.IssueType = 'M'  AND  U0.IssuedQty > 0.000  
                                 )
		                     ) OR  
		                     ( A.Type = 'D'  AND   
		                       EXISTS 
                               (    
                                    SELECT U0.DocEntry FROM  " + Global.SAP_DB + @".dbo.WOR1 U0  
                                    WHERE A.DocEntry = U0.DocEntry  AND  U0.IssueType = 'M'  AND  U0.PlannedQty > U0.IssuedQty  
                               ) 
		                     )
	                      )  
                ) as A INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header B ON A.DocEntry = B.DocEntry
                ORDER BY A.DocNum, A.DocEntry 
                FOR BROWSE
                ";

                _logger.LogInformation(" ReturnComponentsController : GetProductionOrderHelp() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", Series);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderHelpforReturn> obj = new List<ProductionOrderHelpforReturn>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderHelpforReturn>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReturnComponentsController : GetProductionOrderHelp Error : " + ex.ToString());
                _logger.LogError(" Error in ReturnComponentsController : GetProductionOrderHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductionItemHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderItemHelpforReturn>>> GetProductionItemHelp(int BranchId, int DocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReturnComponentsController : GetProductionItemHelp() ");

                #region Validation

                if (DocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide DocEntry" });

                System.Data.DataTable dtPro = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE DocEntry = @docEntry AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ReturnComponentsController : GetProductionItemHelp : Pro Id Query : {q} ", _Query.ToString());
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
                #endregion

                System.Data.DataTable dtData = new();
                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                WITH data AS
                (
                    SELECT  A.DocEntry, A.DocNum, B.LineNum, B.ItemCode, B.ItemName, 
		                    B.UomCode, D.UomName, null Quantity, 
		                    CAST(B.PlannedQty as numeric(19,3)) PlannedQty, CAST(B.IssuedQty as numeric(19,3)) IssuedQty,
		                    CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                                   WHERE Z.WhsCode = B.wareHouse and Z.ItemCode = B.ItemCode
                            ) as numeric(19,3)) WhsQty, 
		                    CAST(C.OnHand as numeric(19,3))  InStock, B.wareHouse WhsCode,
                            case when C.ManSerNum = 'N' and  C.ManBtchNum = 'N' then 'N' 
                                 when C.ManSerNum = 'N' and  C.ManBtchNum = 'Y' then 'B' 
                                 when C.ManSerNum = 'Y' and  C.ManBtchNum = 'N' then 'S' 
                            end ItemMngBy, E.ProId, B.VisOrder
                    FROM  " + Global.SAP_DB + @".dbo.OWOR A  
	                      INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 B ON A.DocEntry = B.DocEntry   
	                      INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON B.ItemCode = C.ItemCode    
                          INNER JOIN " + Global.SAP_DB + @".dbo.OUOM D ON D.UomCode = B.UomCode
                          INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header E ON E.DocEntry = A.DocEntry
                    WHERE B.IssueType = 'M' AND A.DocEntry = @docEntry AND  
	                      ( ( ( A.Type = 'S'  OR  A.Type = 'P' ) AND B.IssuedQty > 0.000 
		                    ) OR  
		                    ( A.Type = 'D'  AND  B.PlannedQty > B.IssuedQty )
	                      )   
                 ) 
                 SELECT *,
                        case when ItemMngBy = 'B' then 'Batch' when ItemMngBy = 'S' then 'Serial' when ItemMngBy = 'N' then 'None' end ItemMngByName,
                        CASE 
                            WHEN EXISTS (SELECT 1 FROM data WHERE ItemMngBy = 'B') AND EXISTS (SELECT 1 FROM data WHERE ItemMngBy = 'N') THEN 'A'
                            ELSE ItemMngBy
                        END AS ItemsType
                 FROM data
                 ORDER BY DocEntry, VisOrder, LineNum
                 FOR BROWSE
                 ";

                _logger.LogInformation(" ReturnComponentsController : GetProductionItemHelp() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderItemHelpforReturn> obj = new List<ProductionOrderItemHelpforReturn>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderItemHelpforReturn>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReturnComponentsController : GetProductionItemHelp Error : " + ex.ToString());
                _logger.LogError(" Error in ReturnComponentsController : GetProductionItemHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #region Get Batch Data while clicking on Item on Batch Grid

        [HttpGet("GetBatchData")]
        public async Task<ActionResult<IEnumerable<BatchSerialData>>> GetBatchData(int BranchId, string ItemCode, string WhsCode)
        {
            try
            {
                _logger.LogInformation(" Calling ReturnComponentsController : GetBatchData() ");

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
                IF OBJECT_ID('vw_RetBatchData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_RetBatchData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" ReturnComponentsController : DROP Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                _Query = @"
                DECLARE @DynamicQuery NVARCHAR(MAX)
                SET @DynamicQuery = '
                CREATE VIEW vw_RetBatchData" + _id.ToString().Replace("-", "_") + @" AS
                SELECT T0.ItemCode, T0.SysNumber, T0.MdAbsEntry, T0.Quantity, 
                       CAST(T0.CommitQty as numeric(19,3)) CommitQty, CAST(T0.CountQty  as numeric(19,3)) CountQty
                FROM " + Global.SAP_DB + @".dbo.OBTQ T0  
                INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T1 ON T1.ItemCode = T0.ItemCode AND T1.SysNumber = T0.SysNumber
                WHERE T0.ItemCode = ''" + ItemCode + @"'' AND T0.[WhsCode] = ''" + WhsCode + @"'' AND T1.Status <= ''2'' AND T0.Quantity <> 0'
                EXEC sp_executesql @DynamicQuery";

                _logger.LogInformation(" ReturnComponentsController : Create Batch View Query : {q} ", _Query.ToString());

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
                FROM  vw_RetBatchData" + _id.ToString().Replace("-", "_") + @" T0  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T1 ON T1.AbsEntry = T0.MdAbsEntry    
	                  LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.OBTW T2 ON T2.MdAbsEntry = T0.MdAbsEntry AND T2.WhsCode = @whsCode   
                ORDER BY T1.AbsEntry
                ";
                _logger.LogInformation(" ReturnComponentsController : GetBatchData Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                oAdptr.Fill(dtBatchData);
                QITcon.Close();
                #endregion

                #region Drop View
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                IF OBJECT_ID('vw_RetBatchData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_RetBatchData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" ReturnComponentsController : DROP Batch View Query : {q} ", _Query.ToString());

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
                objGlobal.WriteLog("ReturnComponentsController : GetBatchData Error : " + ex.ToString());
                _logger.LogError(" Error in ReturnComponentsController : GetBatchData() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Save Return Components

        [HttpPost("SaveReturnComponents")]
        public async Task<IActionResult> SaveReturnComponents([FromBody] SaveReturnComponents payload)
        {
            string _IsSaved = "N";
            int _RetId = 0;

            try
            {
                _logger.LogInformation(" Calling ReturnComponentsController : SaveReturnComponents() ");

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.itemDetail.Count();

                    #region Get RetId  
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(RetId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Header A  ";
                    _logger.LogInformation(" ReturnComponentsController : Get RetId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _RetId = (Int32)cmd.ExecuteScalar();
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
                    _logger.LogInformation(" ReturnComponentsController : User Query : {q} ", _Query.ToString());
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
                    INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Header
                    (
                        BranchId, RetId, Series, DocEntry, DocNum, PostingDate, Ref2, ProOrdDocEntry,  
                        EntryDate, EntryUser, Remark
                    ) 
                    VALUES 
                    (
                        @bId, @RetId, @series, @docEntry, @docNum, @pDate, @ref2, @proOrdDocEntry,   
                        @eDate, @eUser, @remark 
                    )";
                    _logger.LogInformation(" ReturnComponentsController : SaveReturnComponents() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@RetId", _RetId);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@docEntry", 0);
                    cmd.Parameters.AddWithValue("@docNum", 0);
                    cmd.Parameters.AddWithValue("@pDate", payload.PostingDate);
                    cmd.Parameters.AddWithValue("@ref2", payload.RefNo);
                    cmd.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                    cmd.Parameters.AddWithValue("@eDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@eUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.Remark);

                    int intNum = 0;
                    try
                    {
                        QITcon.Open();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteReturnComp(_RetId);
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
                        foreach (var item in payload.itemDetail)
                        {
                            row = row + 1;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteReturnComp(_RetId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }
                            System.Data.DataTable dtItem = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);

                            _Query = @" 
                            SELECT A.* FROM " + Global.SAP_DB + @".dbo.WOR1 A 
                            WHERE A.DocEntry = @proOrdDocEntry and A.ItemCode = @itemCode and A.LineNum = @baseLineNum
                            ";

                            _logger.LogInformation(" ReturnComponentsController : Item Code Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@proOrdDocEntry", payload.ProOrdDocEntry);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@baseLineNum", item.BaseLineNum);
                            oAdptr.Fill(dtItem);
                            QITcon.Close();

                            if (dtItem.Rows.Count <= 0)
                            {
                                this.DeleteReturnComp(_RetId);
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
                                this.DeleteReturnComp(_RetId);
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
                                this.DeleteReturnComp(_RetId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse for line : " + row });
                            }

                            System.Data.DataTable dtWhs = new();
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" ReturnComponentsController : Detail Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteReturnComp(_RetId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail Warehouse does not exist : " + item.WhsCode
                                });
                            }

                            #endregion


                            #region Save Detail

                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Detail
                            (
                                BranchId, RetId, RetDetId, BaseLine, LineNum, ItemCode, ItemName, Qty,  UoMCode, WhsCode 
                            ) 
                            VALUES 
                            (
                                @bId, @RetId, @RetDetId, @baseLine, @lineNum, @itemCode, @itemName, @Qty, @uomCode, @whsCode 
                            )";
                            _logger.LogInformation(" ReturnComponentsController : SaveReturnComponentsDetail() Query for line " + row + " : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@RetId", _RetId);
                            cmd.Parameters.AddWithValue("@RetDetId", row);
                            cmd.Parameters.AddWithValue("@baseLine", item.BaseLineNum);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", item.ItemName);
                            cmd.Parameters.AddWithValue("@Qty", item.Qty);
                            cmd.Parameters.AddWithValue("@uomCode", item.UoMCode);
                            cmd.Parameters.AddWithValue("@whsCode", item.WhsCode);

                            intNum = 0;
                            try
                            {
                                QITcon.Open();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex)
                            {
                                this.DeleteReturnComp(_RetId);
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
                    {
                        #region Save Return Component
                        var (success, errorMsg) = await objGlobal.ConnectSAP();
                        if (success)
                        {
                            int _Line = 0;

                            Documents productionReceipt = (Documents)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);
                            productionReceipt.Series = payload.Series;
                            productionReceipt.DocDate = DateTime.Parse(payload.PostingDate);
                            productionReceipt.Reference2 = payload.RefNo;
                            productionReceipt.Comments = payload.Remark;
                            productionReceipt.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                            if (Global.gAllowBranch == "Y")
                                productionReceipt.BPL_IDAssignedToInvoice = payload.BranchId;

                            foreach (var item in payload.itemDetail)
                            {
                                productionReceipt.Lines.Quantity = double.Parse(item.Qty); // Set the quantity 
                                productionReceipt.Lines.BaseType = 202;
                                productionReceipt.Lines.BaseEntry = payload.ProOrdDocEntry;

                                productionReceipt.Lines.BaseLine = item.BaseLineNum;
                                //productionIssue.Lines.WarehouseCode = item.WhsCode;

                                if (item.ItemMngBy.ToLower() == "s")
                                {
                                    int i = 0;
                                    foreach (var serial in item.itembatchSerialDet)
                                    {
                                        if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                        {
                                            productionReceipt.Lines.SerialNumbers.SetCurrentLine(i);
                                            productionReceipt.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            productionReceipt.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                            productionReceipt.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                            productionReceipt.Lines.SerialNumbers.Quantity = double.Parse(serial.SelectedQty);
                                            productionReceipt.Lines.SerialNumbers.Add();

                                            i = i + 1;
                                        }
                                    }
                                }
                                else if (item.ItemMngBy.ToLower() == "b")
                                {
                                    int _batchLine = 0;
                                    foreach (var batch in item.itembatchSerialDet)
                                    {
                                        if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                        {
                                            productionReceipt.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            productionReceipt.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                            productionReceipt.Lines.BatchNumbers.Quantity = double.Parse(batch.SelectedQty);
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

                                #region Get Return Component Data from SAP
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);

                                System.Data.DataTable dtRetComp = new();
                                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OIGN where DocEntry = @docEntry  ";
                                _logger.LogInformation(" ReturnComponentsController : SaveReturnComponents : Get Return Component Data from SAP : Query : {q} ", _Query.ToString());
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", _docEntry);
                                oAdptr.Fill(dtRetComp);
                                QITcon.Close();
                                int _docNum = int.Parse(dtRetComp.Rows[0]["DocNum"].ToString());
                                #endregion

                                #region Update Return Component Table
                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" 
                                UPDATE " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Header 
                                SET DocEntry = @docEntry, DocNum = @docNum 
                                WHERE RetId = @retId";
                                _logger.LogInformation(" ReturnComponentsController : SaveReturnComponents : Update Return Component Table Query : {q} ", _Query.ToString());
                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@docEntry", _docEntry);
                                cmd.Parameters.AddWithValue("@docNum", _docNum);
                                cmd.Parameters.AddWithValue("@retId", _RetId);

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
                                        StatusMsg = "Problem in updating Return Component Table"
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
                                    StatusMsg = "Return Component Saved Successfully"
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
                        this.DeleteReturnComp(_RetId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Return Component failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteReturnComp(_RetId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ReturnComponentsController : SaveReturnComponents Error : " + ex.ToString());
                this.DeleteReturnComp(_RetId);
                _logger.LogError("Error in ReturnComponentsController : SaveReturnComponents() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
            finally
            {
                QITcon.Close();
            }
        }

        #endregion


        private bool DeleteReturnComp(int p_RetId)
        {
            try
            {
                _logger.LogInformation(" Calling ReturnComponentsController : DeleteReturnComp() ");

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Detail WHERE RetId = @retId
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ReturnComp_Header WHERE RetId = @retId
                ";

                _logger.LogInformation(" ReturnComponentsController : DeleteReturnComp Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@retId", p_RetId);
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
                objGlobal.WriteLog("ReturnComponentsController : DeleteReturnComp Error : " + ex.ToString());
                _logger.LogError("Error in ReturnComponentsController : DeleteReturnComp() :: {ex}", ex.ToString());
                return false;
            }
        }

    }
}
