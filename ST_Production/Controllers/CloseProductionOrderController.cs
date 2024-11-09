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
    public class CloseProductionOrderController : ControllerBase
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
        private readonly ILogger<CloseProductionOrderController> _logger;

        public CloseProductionOrderController(IConfiguration configuration, ILogger<CloseProductionOrderController> logger)
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

                objGlobal.gServer = Configuration["Server"];
                objGlobal.gSqlVersion = Configuration["SQLVersion"];
                objGlobal.gCompanyDB = Configuration["CompanyDB"];
                objGlobal.gLicenseServer = Configuration["LicenseServer"];
                objGlobal.gSAPUserName = Configuration["SAPUserName"];
                objGlobal.gSAPPassword = Configuration["SAPPassword"];
                objGlobal.gDBUserName = Configuration["DBUserName"];
                objGlobal.gDBPassword = Configuration["DbPassword"];

                Global.gLogPath = Configuration["LogPath"];
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in CloseProductionOrderController :: " + ex.ToString());
                _logger.LogError(" Error in CloseProductionOrderController :: {ex}", ex.ToString());
            }
        }


        #region Production Order Help 

        [HttpGet("GetProductionOrderHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderHelpforClose>>> GetProductionOrderHelp(int Series)
        {
            try
            {
                _logger.LogInformation(" Calling CloseProductionOrderController : GetProductionOrderHelp() ");

                #region Validation

                if (Series <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });

                System.Data.DataTable dtSeries = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.NNM1 WHERE Series = @series and ObjectCode = @objCode ";
                _logger.LogInformation(" CloseProductionOrderController : GetProductionOrderHelp : Series Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objCode", "202");
                oAdptr.Fill(dtSeries);
                QITcon.Close();

                if (dtSeries.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide valid series for Production Order"
                    });
                #endregion

                System.Data.DataTable dtData = new();
                QITcon ??= new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.*, B.ProId FROM 
                (
	                SELECT T0.[DocEntry], T0.[DocNum], T0.Series, T1.[SeriesName], T0.Type,  
                           case when T0.Type = 'S' then 'Standard' when T0.Type = 'P' then 'Special' when T0.Type = 'D' then 'Disassembly' end TypeName,
                           'Released' Status,
						   T0.[ItemCode] ProductNo, T0.[ProdName] ProductName, 
		                   T0.PostDate PostingDate, T0.StartDate, T0.DueDate,  
                           CAST(T0.PlannedQty as numeric(19,3)) PlannedQty, CAST(T0.CmpltQty as numeric(19,3)) CompletedQty,
                           T0.Project, T0.CardCode, T0.Warehouse WhsCode, T0.OcrCode DistRule, T0.Uom UomCode, T0.Comments Remark,
						   T0.U_Shift Shift, T2.ShiftName, T0.U_AW ActWgt,
                           ( SELECT MAX(PostingDate) FROM " + Global.QIT_DB + @".dbo.QIT_ProductionReceipt_Header Z WHERE Z.ProOrdDocEntry = T0.DocEntry) LastReceiptDate
	                FROM  " + Global.SAP_DB + @".dbo.OWOR T0  
		                  INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 T1 ON T0.[Series] = T1.[Series] 
                          INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master T2 ON T2.ShiftId collate SQL_Latin1_General_CP850_CI_AS = T0.U_Shift
	                WHERE T0.[Status] = 'R' AND T0.Series = @series AND T0.PlannedQty = T0.CmpltQty
                ) as A INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header B ON A.DocEntry = B.DocEntry
                ";
                #endregion

                _logger.LogInformation(" CloseProductionOrderController : GetProductionOrderHelp() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", Series);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderHelpforClose> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderHelpforClose>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CloseProductionOrderController : GetProductionOrderHelp Error : " + ex.ToString());
                _logger.LogError(" Error in CloseProductionOrderController : GetProductionOrderHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductionItemHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderItemHelpforClose>>> GetProductionItemHelp(int BranchId, int DocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling CloseProductionOrderController : GetProductionItemHelp() ");

                #region Validation

                if (DocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide DocEntry" });

                System.Data.DataTable dtPro = new();
                QITcon ??= new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE DocEntry = @docEntry AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" CloseProductionOrderController : GetProductionItemHelp : Pro Id Query : {q} ", _Query.ToString());
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
                QITcon ??= new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT T0.DocEntry, T0.DocNum, T1.LineNum, T1.ItemCode, T1.ItemName, 
	                   CAST(ISNULL(T1.BaseQty,0) as numeric(19,3)) BaseQty, T5.BaseRatio, 
                       CAST(ISNULL(T1.PlannedQty,0) as numeric(19,3)) PlannedQty, 
                       CAST(ISNULL(T1.IssuedQty, 0) as numeric(19,3)) IssuedQty,      
                       T1.wareHouse WhsCode, 
	                   CAST(( SELECT ISNULL(Z.OnHand,0) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                              WHERE Z.WhsCode = T1.wareHouse and Z.ItemCode = T1.ItemCode
                       ) as numeric(19,3)) WhsQty, 
                       CAST(ISNULL(T3.OnHand,0) as numeric(19,3)) ItemStock, T1.UomCode, T1.OcrCode DistRule, T1.Project, 
					   case when T5.IssueType = 'M' then 'Manual' else 'Backflush' end IssueType, T4.ProId
                FROM  " + Global.SAP_DB + @".dbo.OWOR T0  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 T1 ON T0.DocEntry = T1.DocEntry   
	                  INNER JOIN " + Global.SAP_DB + @".dbo.B1_DocItemView T2 ON T1.ItemType = T2.DocItemType AND T1.ItemCode = T2.DocItemCode   
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OITM T3 ON T3.ItemCode = T1.ItemCode
                      INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header T4 ON T0.DocEntry = T4.DocEntry
					  INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail T5 ON T5.ProId = T4.ProId and T5.ItemCode collate SQL_Latin1_General_CP850_CI_AS = T1.ItemCode --and T5.IssueType = 'M' 
                WHERE /*T1.IssueType = 'M' AND*/ T0.DocEntry = @docEntry AND 
                      (((T0.Type = 'S' OR T0.Type = 'P' ) /*AND T1.PlannedQty >= T1.IssuedQty*/ ) OR (T0.Type = 'D' AND T1.IssuedQty > 0 ))  
                ORDER BY T1.DocEntry, T1.VisOrder, T1.LineNum 
                ";
                #endregion

                _logger.LogInformation(" CloseProductionOrderController : GetProductionItemHelp() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderItemHelpforClose> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderItemHelpforClose>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CloseProductionOrderController : GetProductionItemHelp Error : " + ex.ToString());
                _logger.LogError(" Error in CloseProductionOrderController : GetProductionItemHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #endregion


        #region Close Production Order

        [HttpPost("CloseProductionOrder")]
        public async Task<IActionResult> CloseProductionOrder(int BranchId, int DocEntry)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling CloseProductionOrderController : CloseProductionOrder() ");

                #region Validation

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #region Check for Pro Id

                if (DocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order Entry" });

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE DocEntry = @docEntry AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" CloseProductionOrderController : CloseProductionOrder : Pro Id Query : {q} ", _Query.ToString());
                QITcon.Open();
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
                else
                {
                    if (dtPro.Rows[0]["Action"].ToString() != "A")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Order must be approved first"
                        });

                    if (dtPro.Rows[0]["Status"].ToString() != "R")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Order status must be Released"
                        });

                    if (dtPro.Rows[0]["Status"].ToString() == "L")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Order is already Closed"
                        });
                }
                #endregion

                #region Check for Quantity

                System.Data.DataTable dtSAPPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWOR WHERE DocEntry = @docEntry and PlannedQty <> CmpltQty ";
                _logger.LogInformation(" CloseProductionOrderController : Check SAP PRO Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtSAPPro);
                QITcon.Close();

                if (dtSAPPro.Rows.Count > 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Not all Products/Components were issued for this production order"
                    });


                System.Data.DataTable dtSAPProDet = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.WOR1 WHERE DocEntry = @docEntry and PlannedQty <> IssuedQty and IssueType = 'M' ";
                _logger.LogInformation(" CloseProductionOrderController : Check SAP PRO Detail Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtSAPProDet);
                QITcon.Close();

                if (dtSAPProDet.Rows.Count > 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Not all Products/Components were issued for this production order"
                    });
                #endregion

                #endregion

                #region Update Production Order in SAP

                //string p_ErrorMsg = string.Empty;
                //if (objGlobal.ConnectSAP(out p_ErrorMsg))

                var (success, errorMsg) = await objGlobal.ConnectSAP();
                if (success)
                {
                    ProductionOrders proOrder = (ProductionOrders)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oProductionOrders);
                    if (proOrder.GetByKey(DocEntry))
                    {
                        proOrder.ProductionOrderStatus = BoProductionOrderStatusEnum.boposClosed;
                        proOrder.ClosingDate = DateTime.Now;
                        int updateResult = proOrder.Update();

                        if (updateResult != 0)
                        {
                            string msg = "(" + objGlobal.oComp.GetLastErrorCode() + ") " + objGlobal.oComp.GetLastErrorDescription();
                            _logger.LogInformation(" CloseProductionOrderController : CloseProductionOrder : Error :: {ex}", msg);
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = msg
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "No such Production Order exist"
                        });
                    }
                }
                else
                {
                    string msg = "(" + objGlobal.oComp.GetLastErrorCode() + ") " + objGlobal.oComp.GetLastErrorDescription();
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        IsSaved = _IsSaved,
                        StatusMsg = msg
                    });
                }
                #endregion

                #region Update production
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                SET Status = @status 
                WHERE DocEntry = @docEntry and ISNULL(BranchID, @bId) = @bId";
                _logger.LogInformation(" CloseProductionOrderController : CloseProductionOrder : Update Production Order Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@status", "L");
                cmd.Parameters.AddWithValue("@docEntry", DocEntry);
                cmd.Parameters.AddWithValue("@bId", BranchId);

                QITcon.Open();
                int intNum = cmd.ExecuteNonQuery();
                QITcon.Close();

                if (intNum > 0)
                    _IsSaved = "Y";
                else
                    _IsSaved = "N";

                return Ok(new
                {
                    StatusCode = "200",
                    IsSaved = "Y",
                    StatusMsg = "Production Order closed successfully in SAP"
                });

                #endregion

            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CloseProductionOrderController : CloseProductionOrder Error : " + ex.ToString());
                _logger.LogError("Error in CloseProductionOrderController : CloseProductionOrder() :: {ex}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    IsSaved = _IsSaved,
                    StatusMsg = ex.Message.ToString()
                });
            }
        }

        #endregion


        #region Update raw item's planned qty

        [HttpPut("UpdatePlannedQty")]
        public async Task<IActionResult> UpdatePlannedQty(UpdatePlannedQty payload)
        {
            string _IsSaved = "N";

            try
            {
                _logger.LogInformation(" Calling CloseProductionOrderController : UpdatePlannedQty() ");


                #region Validation

                if (payload.BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #region Check for Pro Id

                if (payload.ProOrdDocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order Entry" });

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header 
                WHERE DocEntry = @docEntry AND ISNULL(BranchId, @bId) = @bId ";

                _logger.LogInformation(" CloseProductionOrderController : UpdatePlannedQty : Pro Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Production Order exists"
                    });
                else
                {
                    if (dtPro.Rows[0]["Action"].ToString() != "A")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Order must be approved first"
                        });

                }
                #endregion

                if (payload.LineNum < 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide only raw item(s)" });

                #endregion


                #region Update Production Order in SAP

                var (success, errorMsg) = await objGlobal.ConnectSAP();
                if (success)
                {
                    ProductionOrders proOrder = (ProductionOrders)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oProductionOrders);
                    if (proOrder.GetByKey(payload.ProOrdDocEntry))
                    {
                        proOrder.Lines.SetCurrentLine(payload.LineNum);

                        if (proOrder.Lines.ItemNo == payload.ItemCode)
                        {
                            if (proOrder.Lines.IssuedQuantity > double.Parse(payload.Quantity))
                            {
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide valid Planned Quantity"
                                });
                            }
                            proOrder.Lines.PlannedQuantity = double.Parse(payload.Quantity);
                        }
                        else
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Provide valid Item Code for the Line : " + payload.LineNum
                            });

                        int updateResult = proOrder.Update();

                        if (updateResult != 0)
                        {
                            string msg = "(" + objGlobal.oComp.GetLastErrorCode() + ") " + objGlobal.oComp.GetLastErrorDescription();
                            _logger.LogInformation(" CloseProductionOrderController : UpdatePlannedQty : Error :: {ex} ", msg);
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = msg
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "No such Production Order exist"
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


                #region Update production

                System.Data.DataTable dtProDet = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT * FROM " + Global.SAP_DB + @".dbo.WOR1 
                WHERE DocEntry = @docEntry AND LineNum = @line AND ItemCode = @itemCode";

                _logger.LogInformation(" CloseProductionOrderController : UpdatePlannedQty : Pro Detail Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@line", payload.LineNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                oAdptr.Fill(dtProDet);
                QITcon.Close();


                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                UPDATE B
                SET B.PlannedQty = @qty, B.BaseQty = @baseQty, B.BaseRatio = @baseQty
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail B ON A.ProId = B.ProId
                WHERE A.DocEntry = @docEntry and B.LineNum = @line and B.ItemCode = @itemCode and ISNULL(A.BranchID, @bId) = @bId";
                _logger.LogInformation(" CloseProductionOrderController : UpdatePlannedQty : Update Production Order Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@qty", payload.Quantity);
                cmd.Parameters.AddWithValue("@baseQty", dtProDet.Rows[0]["BaseQty"]);
                cmd.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                cmd.Parameters.AddWithValue("@line", payload.LineNum);
                cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                cmd.Parameters.AddWithValue("@bId", payload.BranchId);

                await QITcon.OpenAsync();
                int intNum = cmd.ExecuteNonQuery();
                QITcon.Close();

                if (intNum > 0)
                    _IsSaved = "Y";
                else
                    _IsSaved = "N";

                return Ok(new
                {
                    StatusCode = "200",
                    IsSaved = "Y",
                    StatusMsg = "Production Order updated successfully in SAP"
                });

                #endregion

            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CloseProductionOrderController : UpdatePlannedQty Error : " + ex.ToString());
                _logger.LogError("Error in CloseProductionOrderController : UpdatePlannedQty() :: {ex}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    IsSaved = _IsSaved,
                    StatusMsg = ex.Message.ToString()
                });
            }
        }

        #endregion

    }
}
