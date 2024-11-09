using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using ST_Production.Common;
using ST_Production.Models;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTransferController : ControllerBase
    {
        public Global objGlobal;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;

        private SqlCommand cmd;
        private SqlDataAdapter oAdptr;
        private SqlConnection QITcon;


        public IConfiguration Configuration { get; }
        private readonly ILogger<InventoryTransferController> _logger;

        public InventoryTransferController(IConfiguration configuration, ILogger<InventoryTransferController> logger)
        {
            objGlobal ??= new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
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
                objGlobal.WriteLog(" Error in InventoryTransferController :: " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController :: {ex}", ex.ToString());
            }
        }


        #region Fill data on Page Load

        [HttpGet("GetProductionOrderHelp")]
        public async Task<ActionResult<IEnumerable<ProductionOrderHelp>>> GetProductionOrderHelp()
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : GetProductionOrderHelp() ");

                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT A.ProId, A.DocEntry, A.DocNum, C.SeriesName, A.OrderDate PostingDate, A.ProductNo, A.ProductName, 
                       CAST(A.PlannedQty as numeric(19,3)) PlannedQty, CAST(B.CmpltQty as numeric(19,3)) CmpltQty, A.Project, A.WhsCode, A.DistRule, 'Released' Status, A.UoM, A.DraftRemark
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OWOR B ON A.DocEntry = B.DocEntry
	                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 C ON A.Series = C.Series
                WHERE A.Status = 'R'
                ";

                _logger.LogInformation(" InventoryTransferController : GetProductionOrderHelp() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderHelp> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderHelp>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("InventoryTransferController : GetProductionOrderHelp Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : GetProductionOrderHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetItemHelp")]
        public async Task<ActionResult<IEnumerable<SpecialItemDetailHelp>>> GetItemHelp(int DocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : GetItemHelp() ");

                System.Data.DataTable dtData = new();
                string _where = string.Empty;
                QITcon = new SqlConnection(_QIT_connection);

                #region Inventory Transfer Validation and Filter

                if (DocEntry > 0)
                {
                    _Query = @" 
                    SELECT A.ProId, A.DocEntry, A.DocNum, C.SeriesName, A.OrderDate PostingDate, A.ProductNo, A.ProductName, 
                           CAST(A.PlannedQty as numeric(19,3)) PlannedQty, CAST(B.CmpltQty as numeric(19,3)) CmpltQty, A.Project, A.WhsCode, A.DistRule, 'Released' Status, A.UoM, A.DraftRemark
                    FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A 
	                     INNER JOIN " + Global.SAP_DB + @".dbo.OWOR B ON A.DocEntry = B.DocEntry
	                     INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 C ON A.Series = C.Series
                    WHERE A.Status = 'R' and A.DocEntry = @docEntry
                    ";

                    _logger.LogInformation(" InventoryTransferController : GetProductionOrderHelp() Query : {q} ", _Query.ToString());
                    await QITcon.OpenAsync();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        _where = " WHERE A.ItemCode IN (SELECT ItemCode FROM " + Global.SAP_DB + @".dbo.WOR1 where DocEntry = @docEntry) ";
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "No such Inventory Transfer exist" });
                    }
                    dtData = new();
                }

                #endregion

                #region Get Items

                _Query = @" 
                SELECT ItemCode, ItemName, InStock, UoM
                FROM 
                (
                    SELECT T0.[ItemCode], T0.[ItemName], T0.[OnHand] InStock, T0.[InvntryUom] UoM
                    FROM " + Global.SAP_DB + @".dbo.OITM T0 
                    WHERE T0.[InvntItem] = 'Y' AND  
	                      ( ( T0.[validFor] = 'N' OR 
                              (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND 
                              (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
                            ) AND  
		                    ( T0.[frozenFor] = 'N' OR 
                              T0.[frozenFrom] IS NOT NULL AND T0.[frozenFrom] > @date OR 
                              T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date 
                            )
	                      )  
                ) as A " + _where + @"
                ";

                _logger.LogInformation(" InventoryTransferController : GetItemHelp() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", DocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<SpecialItemDetailHelp> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SpecialItemDetailHelp>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }

                #endregion

            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("InventoryTransferController : GetItemHelp Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : GetItemHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Save Draft Inventory Transfer

        [HttpPost("SaveDraftInventoryTransfer")]
        public IActionResult SaveDraftInventoryTransfer([FromBody] SaveDraftInventoryTransfer payload)
        {
            string _IsSaved = "N";
            int _InvId = 0;

            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : SaveDraftInventoryTransfer() ");

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.itDetail.Count;

                    #region Get InvId  
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(InvId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header A  ";
                    _logger.LogInformation(" InventoryTransferController : Get InvId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _InvId = (Int32)cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    #region Header Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.Series <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });


                    #region Check From Warehouse

                    if (payload.FromWhs.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Header From Warehouse" });

                    System.Data.DataTable dtWhs = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                    _logger.LogInformation(" InventoryTransferController : Header From Warehouse Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", payload.FromWhs);
                    oAdptr.Fill(dtWhs);
                    QITcon.Close();

                    if (dtWhs.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Header From Warehouse does not exist : " + payload.FromWhs
                        });
                    #endregion

                    #region Check To Warehouse

                    if (payload.ToWhs.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Header To Warehouse" });

                    dtWhs = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                    _logger.LogInformation(" InventoryTransferController : Header To Warehouse Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", payload.ToWhs);
                    oAdptr.Fill(dtWhs);
                    QITcon.Close();

                    if (dtWhs.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Header To Warehouse does not exist : " + payload.ToWhs
                        });
                    #endregion

                    #region Check for Price List

                    if (payload.PriceListId.ToString().Length > 0)
                    {
                        System.Data.DataTable dtPriceList = new();
                        QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" 
                        SELECT A.ListNum, A.ListName
                        FROM " + Global.SAP_DB + @".dbo.OPLN A WHERE A.ListNum = @listNum
                    
                        UNION
 
                        SELECT A.GroupNum ListNum, A.GroupName ListName from " + Global.QIT_DB + @".dbo.QIT_PriceList A where A.GroupNum = @listNum
                        ";

                        _logger.LogInformation(" InventoryTransferController : Price List Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@listNum", payload.PriceListId);
                        oAdptr.Fill(dtPriceList);
                        QITcon.Close();

                        if (dtPriceList.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Price List does not exist : " + payload.PriceListId
                            });
                    }
                    else
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Price List" });

                    #endregion

                    #region Check for Sales Employee

                    if (payload.SlpCode.ToString().Length > 0)
                    {
                        System.Data.DataTable dtSlpCodes = new();
                        QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" 
                        SELECT T0.SlpCode, T0.SlpName FROM " + Global.SAP_DB + @".dbo.OSLP T0 
                        WHERE T0.Active = 'Y' AND T0.SlpCode = @slpCode
                        ";

                        _logger.LogInformation(" InventoryTransferController : Sales Employee Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@slpCode", payload.SlpCode);
                        oAdptr.Fill(dtSlpCodes);
                        QITcon.Close();

                        if (dtSlpCodes.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Sales Employee does not exist : " + payload.SlpCode
                            });
                    }

                    #endregion

                    #region Check for Login User

                    if (payload.LoginUser.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });

                    System.Data.DataTable dtUser = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @uName ";
                    _logger.LogInformation(" InventoryTransferController : User Query : {q} ", _Query.ToString());
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

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                        INSERT INTO " + Global.QIT_DB + @".dbo.QIT_IT_Header
                        (
                            BranchId, InvId, ProOrdDocEntry, DocEntry, DocNum, Series, FromWhs, ToWhs,
                            PostingDate, DocDate, PriceListId, SlpCode, ShipTo,
                            EntryDate, EntryUser, DraftRemark, Action, ActionDate, PrevInvId
                        ) 
                        VALUES 
                        (
                            @bId, @InvId, @proDocEntry, @docEntry, @docNum, @series, @frWhs, @toWhs,
                            @pDate, @docDate, @priceList, @slpCode, @shipTo, 
                            @eDate, @eUser, @remark, @action, @aDate, @prevInvId
                        )";
                    _logger.LogInformation(" InventoryTransferController : SaveDraftInventoryTransfer() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@InvId", _InvId);
                    cmd.Parameters.AddWithValue("@proDocEntry", payload.ProOrdDocEntry);
                    cmd.Parameters.AddWithValue("@docEntry", 0);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@frWhs", payload.FromWhs);
                    cmd.Parameters.AddWithValue("@toWhs", payload.ToWhs);
                    cmd.Parameters.AddWithValue("@pDate", payload.PostingDate);
                    cmd.Parameters.AddWithValue("@docDate", payload.DocDate);
                    cmd.Parameters.AddWithValue("@priceList", payload.PriceListId);
                    cmd.Parameters.AddWithValue("@slpCode", payload.SlpCode);
                    cmd.Parameters.AddWithValue("@shipTo", payload.ShipTo);
                    cmd.Parameters.AddWithValue("@eDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@eUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.Remark);
                    cmd.Parameters.AddWithValue("@action", "P");
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@prevInvId", 0);

                    int intNum = 0;
                    try
                    {
                        QITcon.Open();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteDraftInventoryTransfer(_InvId);
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
                        foreach (var item in payload.itDetail)
                        {
                            row++;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }

                            System.Data.DataTable dtItem = new();
                            QITcon = new SqlConnection(_QIT_connection);


                            _Query = @" 
                            SELECT T0.[ItemCode], T0.[ItemName], T0.[OnHand] InStock, T0.[InvntryUom] UoM
                            FROM " + Global.SAP_DB + @".dbo.OITM T0 
                            WHERE ( ( T0.[validFor] = 'N' OR 
                                      (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND 
		                              (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
	                                ) AND  
		                            (   T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                                T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date 
		                            )
	                              ) AND T0.ItemCode = @itemCode
                            
                            ";

                            _logger.LogInformation(" InventoryTransferController : Item Code Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                            oAdptr.Fill(dtItem);
                            QITcon.Close();

                            if (dtItem.Rows.Count <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row
                                });
                            }

                            #endregion

                            #region Check for Planned Qty

                            if (item.Qty.ToString() == "0" || double.Parse(item.Qty.ToString()) <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Quantity for line : " + row
                                });
                            }

                            #endregion

                            #region Check for UoM

                            if (item.UoM != dtItem.Rows[0]["UoM"].ToString())
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide valid UoM for line : " + row
                                });
                            }

                            #endregion

                            #region Check From Warehouse : Detail

                            if (item.FromWhs.ToString().Length <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Detail From Warehouse" });
                            }

                            dtWhs = new();
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" InventoryTransferController : Detail From Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.FromWhs);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail From Warehouse does not exist : " + item.FromWhs
                                });
                            }

                            #endregion

                            #region Check To Warehouse : Detail

                            if (item.ToWhs.ToString().Length <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Detail To Warehouse" });
                            }

                            dtWhs = new();
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" InventoryTransferController : Detail To Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.ToWhs);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail To Warehouse does not exist : " + item.ToWhs
                                });
                            }

                            #endregion

                            #region Save Detail

                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_IT_Detail
                            (
                                BranchId, InvId, InvDetId, LineNum, ItemCode, ItemName, FromWhs, ToWhs, Qty, UoM
                            ) 
                            VALUES 
                            (
                                @bId, @InvId, @invDetId, @lineNum, @itemCode, @itemName, @frWhs, @toWhs, @Qty, @uom
                            )";
                            _logger.LogInformation(" InventoryTransferController : SaveDraftInventoryTransferDetail() Query for line {p} : {q} ", row, _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@InvId", _InvId);
                            cmd.Parameters.AddWithValue("@invDetId", row);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", dtItem.Rows[0]["ItemName"]);
                            cmd.Parameters.AddWithValue("@frWhs", item.FromWhs);
                            cmd.Parameters.AddWithValue("@toWhs", item.ToWhs);
                            cmd.Parameters.AddWithValue("@Qty", item.Qty);
                            cmd.Parameters.AddWithValue("@uom", item.UoM);

                            intNum = 0;
                            try
                            {
                                QITcon.Open();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex)
                            {
                                this.DeleteDraftInventoryTransfer(_InvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    StatusMsg = "For line : " + row + " Error : " + ex.Message.ToString()
                                });
                            }

                            if (intNum > 0)
                                SucessCount++;

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
                            InvId = _InvId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftInventoryTransfer(_InvId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Draft Production failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteDraftInventoryTransfer(_InvId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("InventoryTransferController : SaveDraftInventoryTransfer Error : " + ex.ToString());
                this.DeleteDraftInventoryTransfer(_InvId);
                _logger.LogError("Error in InventoryTransferController : SaveDraftInventoryTransfer() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        private bool DeleteDraftInventoryTransfer(int p_InvId)
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : DeleteDraftInventoryTransfer() ");

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_IT_Detail WHERE InvId = @InvId
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE InvId = @InvId
                ";
                _logger.LogInformation(" InventoryTransferController : DeleteDraftInventoryTransfer Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@InvId", p_InvId);
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
                objGlobal.WriteLog("InventoryTransferController : DeleteDraftInventoryTransfer Error : " + ex.ToString());
                _logger.LogError("Error in InventoryTransferController : DeleteDraftInventoryTransfer() :: {ex}", ex.ToString());
                return false;
            }
        }

        #endregion


        #region Display Inventory Transfer List in Grid

        [HttpGet("DisplayInventoryTransfer")]
        public async Task<ActionResult<IEnumerable<DisplayInventoryTransfer>>> DisplayInventoryTransfer(string UserName, string UserType)
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : DisplayInventoryTransfer() ");

                System.Data.DataTable dtInv = new();
                QITcon = new SqlConnection(_QIT_connection);

                string _strWhere = string.Empty;

                if (UserName.ToLower() != "admin")
                {
                    if (UserType.ToLower() == "c")
                        _strWhere = " and A.EntryUser = @uName ";
                }


                _Query = @" 
                SELECT A.InvId, A.ProOrdDocEntry,
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
                       CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
                       B.SeriesName, A.FromWhs, A.ToWhs, A.PostingDate, A.DocDate, 
	                   case when C.ListName is null then E.GroupName else C.ListName end ListName, D.SlpName, A.ShipTo, 
                       A.draftRemark Remark,
                       CASE WHEN A.Action <> 'A' then '-' when A.Action = 'A' and A.DocNum = 0 THEN 'No' ELSE 'Yes' END BatchSelected
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header  A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                LEFT JOIN " + Global.SAP_DB + @".dbo.OPLN C On A.PriceListId = C.ListNum
                LEFT JOIN " + Global.SAP_DB + @".dbo.OSLP D On A.SlpCode = D.SlpCode
                LEFT JOIN " + Global.QIT_DB + @".dbo.QIT_PriceList E On A.PriceListId = E.GroupNum
                WHERE A.InvId NOT IN ( SELECT PrevInvId from " + Global.QIT_DB + @".dbo.QIT_IT_Header ) 
                and A.InvId NOT IN (
					select Z.InvId   ---,  DATEDIFF(DAY, Z.ActionDate, getdate())
					from " + Global.QIT_DB + @".dbo.QIT_IT_Header Z where Z.Action = 'R' 
					and DATEDIFF(DAY, Z.ActionDate, getdate()) >= (select RejectDocDays from " + Global.QIT_DB + @".dbo.QIT_Config_Master)
				)
                " + _strWhere + @"
                ";

                _logger.LogInformation(" InventoryTransferController : DisplayInventoryTransfer() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@uName", UserName);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count > 0)
                {
                    List<DisplayInventoryTransfer> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtInv);
                    obj = JsonConvert.DeserializeObject<List<DisplayInventoryTransfer>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("InventoryTransferController : DisplayInventoryTransfer Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : DisplayInventoryTransfer() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Get Inventory Transfer with Detail on Grid Click

        [HttpGet("GetInventoryTransferDetails")]
        public async Task<ActionResult<IEnumerable<ITHeader>>> GetInventoryTransferDetails(int BranchId, int InvId)
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : GetInventoryTransferDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Inv Id

                if (InvId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Inventory Transfer Id" });

                System.Data.DataTable dtInv = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE InvId = @InvId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" InventoryTransferController : Inv Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", InvId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Inventory Transfer exists"
                    });
                #endregion

                #region Check for Inv Id - Initiated again or not

                dtInv = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE PrevInvId = @InvId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" InventoryTransferController : Inv Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", InvId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count > 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "A new inventory transfer has already been initiated for this rejected transfer"
                    });
                }
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                WITH data AS
                (
                SELECT A.InvId, A.ProOrdDocEntry, 
                       ISNULL(( SELECT DocNum FROM " + Global.SAP_DB + @".dbo.OWOR WHERE DocEntry = A.ProOrdDocEntry),0) ProOrdDocNum,
                       CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approve' WHEN A.Action = 'R' THEN 'Reject' END State,
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry,
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
	                   B.Indicator PeriodIndicator, A.Series, B.SeriesName,  
	                   A.FromWhs, A.ToWhs, A.PostingDate, A.DocDate, 
	                   A.PriceListId, case when C.ListName is null then G.GroupName else C.ListName end PriceListName, 
                       A.SlpCode, E.SlpName,
	                   A.ShipTo, A.DraftRemark Remark, A.ActionRemark Reason, 
                       D.InvDetId, D.LineNum, D.ItemCode DetailItemCode, D.ItemName DetailItemName, 
	                   D.FromWhs DetailFromWhs, D.ToWhs DetailToWhs,
	                   CAST(D.Qty as numeric(19,3)) Qty, D.Uom DetailUoM, 
                       CAST(ISNULL((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
                          FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode AND 
                                Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = D.FromWhs 
                       ),0) as numeric(19,3)) AvailQty,  
                       CAST(ISNULL((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                       ),0) as numeric(19,3)) InStock, 
                       case when F.ManSerNum = 'N' and  F.ManBtchNum = 'N' then 'N' 
                            when F.ManSerNum = 'N' and  F.ManBtchNum = 'Y' then 'B' 
                            when F.ManSerNum = 'Y' and  F.ManBtchNum = 'N' then 'S' 
                       end ItemMngBy
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header  A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_IT_Detail D ON A.InvId = D.InvId
                LEFT JOIN " + Global.SAP_DB + @".dbo.OPLN C On A.PriceListId = C.ListNum
                LEFT JOIN " + Global.SAP_DB + @".dbo.OSLP E On A.SlpCode = E.SlpCode
                INNER JOIN " + Global.SAP_DB + @".dbo.OITM F ON F.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                LEFT JOIN " + Global.QIT_DB + @".dbo.QIT_PriceList G On A.PriceListId = G.GroupNum
                WHERE A.InvId = @InvId AND ISNULL(A.BranchId, @bId) = @bId
                )
                SELECT *,
                       case when ItemMngBy = 'B' then 'Batch' when ItemMngBy = 'S' then 'Serial' when ItemMngBy = 'N' then 'None' end ItemMngByName,
	                   CASE 
                           WHEN EXISTS (SELECT 1 FROM data WHERE ItemMngBy = 'B') AND EXISTS (SELECT 1 FROM data WHERE ItemMngBy = 'N') THEN 'A'
                           ELSE ItemMngBy
                       END AS ItemsType
                FROM data
                ";
                #endregion

                _logger.LogInformation(" InventoryTransferController : GetInventoryTransferDetails() Query : {q} ", _Query.ToString());
                dtInv = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", InvId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count > 0)
                {
                    List<ITHeader> obj = new();
                    List<ITDetail> itDetail = new();
                    dynamic arData = JsonConvert.SerializeObject(dtInv);
                    itDetail = JsonConvert.DeserializeObject<List<ITDetail>>(arData.ToString());

                    obj.Add(new ITHeader()
                    {
                        InvId = int.Parse(dtInv.Rows[0]["InvId"].ToString()),
                        ProOrdDocEntry = int.Parse(dtInv.Rows[0]["ProOrdDocEntry"].ToString()),
                        ProOrdDocNum = int.Parse(dtInv.Rows[0]["ProOrdDocNum"].ToString()),
                        State = dtInv.Rows[0]["State"].ToString(),
                        DocEntry = dtInv.Rows[0]["DocEntry"].ToString(),
                        DocNum = dtInv.Rows[0]["DocNum"].ToString(),
                        PeriodIndicator = dtInv.Rows[0]["PeriodIndicator"].ToString(),
                        Series = int.Parse(dtInv.Rows[0]["Series"].ToString()),
                        SeriesName = dtInv.Rows[0]["SeriesName"].ToString(),
                        FromWhs = dtInv.Rows[0]["FromWhs"].ToString(),
                        ToWhs = dtInv.Rows[0]["ToWhs"].ToString(),
                        PostingDate = dtInv.Rows[0]["PostingDate"].ToString(),
                        DocDate = dtInv.Rows[0]["DocDate"].ToString(),
                        PriceListId = int.Parse(dtInv.Rows[0]["PriceListId"].ToString()),
                        PriceListName = dtInv.Rows[0]["PriceListName"].ToString(),
                        SlpCode = int.Parse(dtInv.Rows[0]["SlpCode"].ToString()),
                        SlpName = dtInv.Rows[0]["SlpName"].ToString(),
                        ShipTo = dtInv.Rows[0]["ShipTo"].ToString(),
                        Remark = dtInv.Rows[0]["Remark"].ToString(),
                        Reason = dtInv.Rows[0]["Reason"].ToString(),
                        ItemsType = dtInv.Rows[0]["ItemsType"].ToString(),
                        itDetails = itDetail
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
                objGlobal.WriteLog("InventoryTransferController : GetInventoryTransferDetails Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : GetInventoryTransferDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Approve/Reject Inventory Transfer

        [HttpPost("VerifyDraftIT")]
        public IActionResult VerifyDraftIT([FromBody] VerifyDraftIT payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;

            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : VerifyDraftIT() ");

                if (payload != null)
                {
                    #region Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    #region Check for Inv Id

                    if (payload.InvId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Inventory Transfer Id" });

                    System.Data.DataTable dtIT = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE InvId = @InvId AND ISNULL(BranchId, @bId) = @bId ";
                    _logger.LogInformation(" InventoryTransferController : VerifyDraftIT : Inv Id Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", payload.InvId);
                    oAdptr.Fill(dtIT);
                    QITcon.Close();

                    if (dtIT.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "No such Inventory Transfer exists"
                        });
                    else
                    {
                        if (dtIT.Rows[0]["Action"].ToString() != "P")
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Inventory Transfer is already approved or rejected"
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

                    #region Update Inventory Transfer
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE " + Global.QIT_DB + @".dbo.QIT_IT_Header
                    SET Action = @action, ActionDate = @aDate, ActionUser = @aUser, ActionRemark = @remark 
                    WHERE InvId = @InvId and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" InventoryTransferController : VerifyDraftIT : Update Inventory Transfer Action Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@action", payload.Action.ToUpper());
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@aUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.ActionRemark);
                    cmd.Parameters.AddWithValue("@InvId", payload.InvId);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);

                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    if (payload.Action.ToUpper() == "A")
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Inventory Transfer approved" });
                    else
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Inventory Transfer rejected" });
                    #endregion
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("InventoryTransferController : VerifyDraftIT Error : " + ex.ToString());
                _logger.LogError("Error in InventoryTransferController : VerifyDraftIT() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion


        #region Save Inventory Transfer flow

        #region Display Batch Item information before saving Inventory Transfer

        [HttpGet("GetBatchItemDetails")]
        public async Task<ActionResult<IEnumerable<BatchSerialItemDetails>>> GetBatchItemDetails(int BranchId, int InvId)
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : GetBatchItemDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Inv Id

                if (InvId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Inventory Transfer Id" });

                System.Data.DataTable dtInv = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE InvId = @InvId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" InventoryTransferController : Inv Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", InvId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Inventory Transfer exists"
                    });
                else
                {
                    if (dtInv.Rows[0]["Action"].ToString() != "A")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Inventory Transfer must be approved first"
                        });
                }
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.ItemCode, A.ItemName, A.FromWhs WhsCode, B.WhsName, CAST(A.Qty as numeric(19,3)) Qty
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Detail A 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OWHS B ON A.FromWhs collate SQL_Latin1_General_CP850_CI_AS = B.WhsCode
                     INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE A.InvId = @InvId AND ISNULL(A.BranchId, @bId) = @bId AND C.ManBtchNum = 'Y'
                ";
                #endregion

                _logger.LogInformation(" InventoryTransferController : GetBatchItemDetails() Query : {q} ", _Query.ToString());
                dtInv = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", InvId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count > 0)
                {
                    List<BatchSerialItemDetails> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtInv);
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
                objGlobal.WriteLog("InventoryTransferController : GetBatchItemDetails Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : GetBatchItemDetails() :: {ex}", ex.ToString());
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
                _logger.LogInformation(" Calling InventoryTransferController : GetBatchData() ");

                #region Validation

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                if (ItemCode.ToString() == string.Empty || ItemCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });

                if (WhsCode.ToString() == string.Empty || WhsCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Warehouse" });

                #endregion

                #region Create View
                Guid _id = Guid.NewGuid();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                IF OBJECT_ID('vw_ITSerialData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" InventoryTransferController : DROP Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @"
                DECLARE @DynamicQuery NVARCHAR(MAX)
                SET @DynamicQuery = '
                CREATE VIEW vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" AS 
                SELECT T0.ItemCode, T0.SysNumber, T0.MdAbsEntry, T0.Quantity, 
                       CAST(T0.CommitQty as numeric(19,3)) CommitQty, CAST(T0.CountQty as numeric(19,3)) CountQty 
                FROM " + Global.SAP_DB + @".dbo.OBTQ T0  
                INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T1 ON T1.ItemCode = T0.ItemCode AND T1.SysNumber = T0.SysNumber
                WHERE T0.ItemCode = ''" + ItemCode + @"'' AND T0.[WhsCode] = ''" + WhsCode + @"'' AND T1.Status <= ''2'' AND T0.Quantity <> 0'
                EXEC sp_executesql @DynamicQuery";


                _logger.LogInformation(" InventoryTransferController : Create Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                #endregion

                #region Get Batch data Query

                System.Data.DataTable dtBatchData = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @"
                SELECT  T1.ItemCode, T1.ItemName, T1.SysNumber, T1.DistNumber, T1.LotNumber, CAST(T0.Quantity as numeric(19,3)) AvailQty 
                FROM  vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" T0  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T1 ON T1.AbsEntry = T0.MdAbsEntry    
	                  LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.OBTW T2 ON T2.MdAbsEntry = T0.MdAbsEntry AND T2.WhsCode = @whsCode   
                ORDER BY T1.AbsEntry
                ";
                _logger.LogInformation(" InventoryTransferController : GetBatchData Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                oAdptr.Fill(dtBatchData);
                QITcon.Close();
                #endregion

                #region Drop View
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                IF OBJECT_ID('vw_ITSerialData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" InventoryTransferController : DROP Batch View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();
                #endregion

                if (dtBatchData.Rows.Count > 0)
                {
                    List<BatchSerialData> obj = new();
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
                objGlobal.WriteLog("InventoryTransferController : GetBatchData Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : GetBatchData() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Display Serial item information before saving Inventory Transfer

        [HttpGet("GetSerialItemDetails")]
        public async Task<ActionResult<IEnumerable<BatchSerialItemDetails>>> GetSerialItemDetails(int BranchId, int InvId)
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : GetSerialItemDetails() ");

                #region Check for Branch Id

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #endregion

                #region Check for Inv Id

                if (InvId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Inventory Transfer Id" });

                System.Data.DataTable dtInv = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE InvId = @InvId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" InventoryTransferController : Inv Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", InvId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Inventory Transfer exists"
                    });
                else
                {
                    if (dtInv.Rows[0]["Action"].ToString() != "A")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Inventory Transfer must be approved first"
                        });
                }
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT A.ItemCode, A.ItemName, A.FromWhs WhsCode, B.WhsName, CAST(A.Qty as numeric(19,3)) Qty
                FROM " + Global.QIT_DB + @".dbo.QIT_IT_Detail A 
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OWHS B ON A.FromWhs collate SQL_Latin1_General_CP850_CI_AS = B.WhsCode
                     INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE A.InvId = @InvId AND ISNULL(A.BranchId, @bId) = @bId AND C.ManSerNum = 'Y'
                ";
                #endregion

                _logger.LogInformation(" InventoryTransferController : GetSerialItemDetails() Query : {q} ", _Query.ToString());
                dtInv = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", InvId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtInv);
                QITcon.Close();

                if (dtInv.Rows.Count > 0)
                {
                    List<BatchSerialItemDetails> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtInv);
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
                objGlobal.WriteLog("InventoryTransferController : GetSerialItemDetails Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : GetSerialItemDetails() :: {ex}", ex.ToString());
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
                _logger.LogInformation(" Calling InventoryTransferController : GetSerialData() ");

                #region Validation

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                if (ItemCode.ToString() == string.Empty || ItemCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });

                if (WhsCode.ToString() == string.Empty || WhsCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Warehouse" });

                #endregion

                #region Create View

                Guid _id = Guid.NewGuid();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                IF OBJECT_ID('vw_ITSerialData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" InventoryTransferController : DROP Serial View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                _Query = @"
                DECLARE @DynamicQuery NVARCHAR(MAX)
                SET @DynamicQuery = '
                CREATE VIEW vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" AS 
                SELECT T0.ItemCode, T0.SysNumber, T0.MdAbsEntry, T0.Quantity, 
                       CAST(T0.CommitQty as numeric(19,3)) CommitQty, CAST(T0.CountQty as numeric(19,3)) CountQty
                FROM " + Global.SAP_DB + @".dbo.OSRQ T0  
                WHERE T0.ItemCode = ''" + ItemCode + @"'' AND T0.WhsCode = ''" + WhsCode + @"'' AND T0.[Quantity] <> 0
                '
                EXEC sp_executesql @DynamicQuery
                ";

                _logger.LogInformation(" InventoryTransferController : Create Serial View Query : {q} ", _Query.ToString());
                QITcon.Open();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@itemCode", ItemCode);
                cmd.Parameters.AddWithValue("@whsCode", WhsCode);
                cmd.ExecuteNonQuery();
                QITcon.Close();

                #endregion

                #region Get Serial data Query

                System.Data.DataTable dtSerialData = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @"
                SELECT  T1.ItemCode, T1.ItemName, T1.SysNumber, T1.DistNumber, T1.LotNumber, CAST(T0.Quantity as numeric(19,3)) AvailQty
                FROM  vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" T0  
	                  INNER JOIN " + Global.SAP_DB + @".dbo.OSRN T1 ON T1.AbsEntry = T0.MdAbsEntry    
                ORDER BY T1.AbsEntry
                ";
                _logger.LogInformation(" InventoryTransferController : GetSerialData Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtSerialData);
                QITcon.Close();
                #endregion

                #region Drop View
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                IF OBJECT_ID('vw_ITSerialData" + _id.ToString().Replace("-", "_") + @"', 'V') IS NOT NULL
                    DROP VIEW vw_ITSerialData" + _id.ToString().Replace("-", "_") + @" 
                ";
                _logger.LogInformation(" InventoryTransferController : DROP Serial View Query : {q} ", _Query.ToString());

                await QITcon.OpenAsync();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.ExecuteNonQuery();
                QITcon.Close();
                #endregion

                if (dtSerialData.Rows.Count > 0)
                {
                    List<BatchSerialData> obj = new();
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
                objGlobal.WriteLog("InventoryTransferController : GetSerialData Error : " + ex.ToString());
                _logger.LogError(" Error in InventoryTransferController : GetSerialData() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Save Inventory Transfer

        [HttpPost("SaveInventoryTransfer")]
        public async Task<IActionResult> SaveInventoryTransfer([FromBody] SaveInventoryTransfer payload)
        {
            string _IsSaved = "N";

            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : SaveInventoryTransfer() ");

                if (payload != null)
                {
                    #region Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.InvId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Inventory Transfer Id" });

                    #endregion

                    #region Get Inventory Transfer Header Data

                    System.Data.DataTable dtIT = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header WHERE InvId = @InvId AND ISNULL(BranchId, @bId) = @bId ";
                    _logger.LogInformation(" InventoryTransferController : Header data Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", payload.InvId);
                    oAdptr.Fill(dtIT);
                    QITcon.Close();

                    if (dtIT.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "No such Inventory Transfer exists"
                        });
                    else
                    {
                        if (dtIT.Rows[0]["Action"].ToString() != "A")
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Inventory Transfer must be approved first"
                            });
                    }
                    #endregion

                    #region Get Inventory Transfer Detail Data

                    System.Data.DataTable dtITDetail = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Detail 
                    WHERE InvId = @InvId AND ISNULL(BranchId, @bId) = @bId 
                    ORDER BY LineNum ";
                    _logger.LogInformation(" InventoryTransferController : Detail data Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@InvId", payload.InvId);
                    oAdptr.Fill(dtITDetail);
                    QITcon.Close();

                    #endregion

                    #region Validate Item 

                    int draftItemCount = dtITDetail.Rows.Count;
                    int payloadItemCount = payload.itDetails.Count;

                    if (draftItemCount != payloadItemCount)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide all draft items for Inventory Transfer" });
                    #endregion

                    #region Save Inventory Transfer
                    //string p_ErrorMsg = string.Empty;
                    //if (objGlobal.ConnectSAP(out p_ErrorMsg))
                    var (success, errorMsg) = await objGlobal.ConnectSAP();
                    if (success)
                    {
                        int _Line = 0;

                        StockTransfer oStockTransfer = (StockTransfer)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oStockTransfer);
                        oStockTransfer.DocObjectCode = BoObjectTypes.oStockTransfer;
                        oStockTransfer.Series = (int)dtIT.Rows[0]["Series"];
                        oStockTransfer.DocDate = (DateTime)dtIT.Rows[0]["PostingDate"];
                        oStockTransfer.TaxDate = (DateTime)dtIT.Rows[0]["DocDate"];

                        oStockTransfer.FromWarehouse = dtIT.Rows[0]["FromWhs"].ToString();
                        oStockTransfer.ToWarehouse = dtIT.Rows[0]["ToWhs"].ToString();
                        oStockTransfer.PriceList = (int)dtIT.Rows[0]["PriceListId"];
                        oStockTransfer.Address = dtIT.Rows[0]["ShipTo"].ToString();

                        oStockTransfer.Comments = dtIT.Rows[0]["DraftRemark"].ToString();
                        oStockTransfer.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                        foreach (var item in payload.itDetails)
                        {
                            oStockTransfer.Lines.ItemCode = item.ItemCode;
                            oStockTransfer.Lines.Quantity = item.TotalQty;

                            if (item.ItemMngBy.ToLower() == "s")
                            {
                                int i = 0;
                                foreach (var serial in item.batchSerialDet)
                                {
                                    oStockTransfer.Lines.WarehouseCode = serial.ToWhs;
                                    if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                    {
                                        oStockTransfer.Lines.FromWarehouseCode = serial.FromWhs;
                                        oStockTransfer.Lines.SerialNumbers.SetCurrentLine(i);
                                        oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                        oStockTransfer.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                        oStockTransfer.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                        oStockTransfer.Lines.SerialNumbers.Quantity = serial.SelectedQty;
                                        oStockTransfer.Lines.SerialNumbers.Add();

                                        if (serial.FromBinAbsEntry > 0) // Enter in this code block only when From Whs has Bin Allocation
                                        {
                                            oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                            oStockTransfer.Lines.BinAllocations.BinAbsEntry = serial.FromBinAbsEntry;
                                            oStockTransfer.Lines.BinAllocations.Quantity = serial.SelectedQty;
                                            oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                            oStockTransfer.Lines.BinAllocations.Add();
                                        }
                                        if (serial.ToBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                        {
                                            oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                            oStockTransfer.Lines.BinAllocations.BinAbsEntry = serial.ToBinAbsEntry;
                                            oStockTransfer.Lines.BinAllocations.Quantity = serial.SelectedQty;
                                            oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                            oStockTransfer.Lines.BinAllocations.Add();
                                        }

                                        i++;
                                    }
                                }
                            }
                            else if (item.ItemMngBy.ToLower() == "b")
                            {
                                int _batchLine = 0;
                                foreach (var batch in item.batchSerialDet)
                                {
                                    oStockTransfer.Lines.WarehouseCode = batch.ToWhs;
                                    if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                    {
                                        oStockTransfer.Lines.FromWarehouseCode = batch.FromWhs;
                                        oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                        oStockTransfer.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                        oStockTransfer.Lines.BatchNumbers.Quantity = batch.SelectedQty;
                                        oStockTransfer.Lines.BatchNumbers.Add();

                                        if (batch.FromBinAbsEntry > 0) // Enter in this code block only when From Whs has Bin Allocation
                                        {
                                            oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                            oStockTransfer.Lines.BinAllocations.BinAbsEntry = batch.FromBinAbsEntry;
                                            oStockTransfer.Lines.BinAllocations.Quantity = batch.SelectedQty;
                                            oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                            oStockTransfer.Lines.BinAllocations.Add();
                                        }
                                        if (batch.ToBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                        {
                                            oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                            oStockTransfer.Lines.BinAllocations.BinAbsEntry = batch.ToBinAbsEntry;
                                            oStockTransfer.Lines.BinAllocations.Quantity = batch.SelectedQty;
                                            oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                            oStockTransfer.Lines.BinAllocations.Add();
                                        }

                                        _batchLine++;
                                    }
                                }
                            }
                            else if (item.ItemMngBy.ToLower() == "n")
                            {
                                DataTable dtItemDetail = new DataTable();
                                QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_IT_Detail where InvId = @id and ItemCode = @iCode and LineNum = @line ";

                                _logger.LogInformation(" InventoryTransferController : SaveInventoryTransfer : Item Detail Query : {q} ", _Query.ToString());
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@id", payload.InvId);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", item.ItemCode);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@line", item.LineNum);
                                oAdptr.Fill(dtItemDetail);
                                QITcon.Close();

                                if (dtItemDetail.Rows.Count > 0)
                                { 
                                    oStockTransfer.Lines.FromWarehouseCode = dtITDetail.Rows[0]["FromWhs"].ToString();
                                    oStockTransfer.Lines.WarehouseCode = dtItemDetail.Rows[0]["ToWhs"].ToString();
                                }
                                else
                                {
                                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item detail not found" });
                                }
                            }
                            oStockTransfer.Lines.Add();
                            _Line++;

                        }

                        int addResult = oStockTransfer.Add();

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

                            #region Get Inventory Transfer Data from SAP
                            QITcon = new SqlConnection(_QIT_connection);
                            System.Data.DataTable dtSAPIT = new();
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWTR where DocEntry = @docEntry  ";
                            _logger.LogInformation(" InventoryTransferController : SaveInventoryTransfer : Get Inventory Transfer Data from SAP : Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", _docEntry);
                            oAdptr.Fill(dtSAPIT);
                            QITcon.Close();
                            int _docNum = int.Parse(dtSAPIT.Rows[0]["DocNum"].ToString());
                            #endregion

                            #region Update Production Table
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" 
                            UPDATE " + Global.QIT_DB + @".dbo.QIT_IT_Header 
                            SET DocEntry = @docEntry, DocNum = @docNum 
                            WHERE InvId = @invId";
                            _logger.LogInformation(" InventoryTransferController : SaveInventoryTransfer : Update Inventory Table Query : {q} ", _Query.ToString());
                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@docEntry", _docEntry);
                            cmd.Parameters.AddWithValue("@docNum", _docNum);
                            cmd.Parameters.AddWithValue("@invId", payload.InvId);

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
                                    StatusMsg = "Problem in updating Inventory Table"
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
                                StatusMsg = "Inventory Transfer Saved Successfully"
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
                objGlobal.WriteLog("InventoryTransferController : SaveInventoryTransfer Error : " + ex.ToString());
                _logger.LogError("Error in InventoryTransferController : SaveInventoryTransfer() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion

        #endregion


        #region Save Draft Inventory Transfer for Rejected one

        [HttpPost("SaveDraftITOfRejectedIT")]
        public IActionResult SaveDraftITOfRejectedIT(int InvId, [FromBody] SaveDraftInventoryTransfer payload)
        {
            string _IsSaved = "N";
            int _NextInvId = 0;

            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : SaveDraftITOfRejectedIT() ");

                if (InvId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Inventory Id" });

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.itDetail.Count;

                    #region Get InvId  
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(InvId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_IT_Header A  ";
                    _logger.LogInformation(" InventoryTransferController : Get InvId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _NextInvId = (Int32)cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    #region Header Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.Series <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });


                    #region Check for Sales Employee

                    if (payload.SlpCode.ToString().Length > 0)
                    {
                        System.Data.DataTable dtSlpCodes = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        SELECT T0.SlpCode, T0.SlpName FROM " + Global.SAP_DB + @".dbo.OSLP T0 
                        WHERE T0.Active = 'Y' AND T0.SlpCode = @slpCode
                        ";
                        _logger.LogInformation(" InventoryTransferController : Sales Employee Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@slpCode", payload.SlpCode);
                        oAdptr.Fill(dtSlpCodes);
                        QITcon.Close();

                        if (dtSlpCodes.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Sales Employee does not exist : " + payload.SlpCode
                            });
                    }

                    #endregion

                    #region Check for Login User

                    if (payload.LoginUser.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });

                    System.Data.DataTable dtUser = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @uName ";
                    _logger.LogInformation(" InventoryTransferController : User Query : {q} ", _Query.ToString());
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

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                        INSERT INTO " + Global.QIT_DB + @".dbo.QIT_IT_Header
                        (
                            BranchId, InvId, ProOrdDocEntry, DocEntry, DocNum, Series, FromWhs, ToWhs,
                            PostingDate, DocDate, PriceListId, SlpCode, ShipTo,
                            EntryDate, EntryUser, DraftRemark, Action, ActionDate, PrevInvId
                        ) 
                        VALUES 
                        (
                            @bId, @InvId, @proDocEntry, @docEntry, @docNum, @series, @frWhs, @toWhs,
                            @pDate, @docDate, @priceList, @slpCode, @shipTo, 
                            @eDate, @eUser, @remark, @action, @aDate, @prevInvId
                        )";
                    _logger.LogInformation(" InventoryTransferController : SaveDraftInventoryTransfer() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@InvId", _NextInvId);
                    cmd.Parameters.AddWithValue("@proDocEntry", payload.ProOrdDocEntry);
                    cmd.Parameters.AddWithValue("@docEntry", 0);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@frWhs", payload.FromWhs);
                    cmd.Parameters.AddWithValue("@toWhs", payload.ToWhs);
                    cmd.Parameters.AddWithValue("@pDate", payload.PostingDate);
                    cmd.Parameters.AddWithValue("@docDate", payload.DocDate);
                    cmd.Parameters.AddWithValue("@priceList", payload.PriceListId);
                    cmd.Parameters.AddWithValue("@slpCode", payload.SlpCode);
                    cmd.Parameters.AddWithValue("@shipTo", payload.ShipTo);
                    cmd.Parameters.AddWithValue("@eDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@eUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.Remark);
                    cmd.Parameters.AddWithValue("@action", "P");
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@prevInvId", InvId);

                    int intNum = 0;
                    try
                    {
                        QITcon.Open();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteDraftInventoryTransfer(_NextInvId);
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
                        foreach (var item in payload.itDetail)
                        {
                            row++;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }
                            System.Data.DataTable dtItem = new();
                            QITcon = new SqlConnection(_QIT_connection);


                            _Query = @" 
                            SELECT T0.[ItemCode], T0.[ItemName], T0.[OnHand] InStock, T0.[InvntryUom] UoM
                            FROM " + Global.SAP_DB + @".dbo.OITM T0 
                            WHERE ( ( T0.[validFor] = 'N' OR 
                                      (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND 
		                              (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
	                                ) AND  
		                            (   T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                                T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date 
		                            )
	                              ) AND T0.ItemCode = @itemCode
                            
                            ";

                            _logger.LogInformation(" InventoryTransferController : Item Code Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                            oAdptr.Fill(dtItem);
                            QITcon.Close();

                            if (dtItem.Rows.Count <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row
                                });
                            }

                            #endregion

                            #region Check for Planned Qty

                            if (item.Qty.ToString() == "0" || double.Parse(item.Qty.ToString()) <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Quantity for line : " + row
                                });
                            }

                            #endregion

                            #region Check for UoM

                            if (item.UoM != dtItem.Rows[0]["UoM"].ToString())
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide valid UoM for line : " + row
                                });
                            }

                            #endregion

                            #region Check From Warehouse : Detail

                            if (item.FromWhs.ToString().Length <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Detail From Warehouse" });
                            }

                            System.Data.DataTable dtWhs = new();
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" InventoryTransferController : Detail From Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.FromWhs);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail From Warehouse does not exist : " + item.FromWhs
                                });
                            }

                            #endregion

                            #region Check To Warehouse : Detail

                            if (item.ToWhs.ToString().Length <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Detail To Warehouse" });
                            }

                            dtWhs = new();
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" InventoryTransferController : Detail To Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.ToWhs);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail To Warehouse does not exist : " + item.ToWhs
                                });
                            }

                            #endregion

                            #region Save Detail

                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_IT_Detail
                            (
                                BranchId, InvId, InvDetId, LineNum, ItemCode, ItemName, FromWhs, ToWhs, Qty, UoM
                            ) 
                            VALUES 
                            (
                                @bId, @InvId, @invDetId, @lineNum, @itemCode, @itemName, @frWhs, @toWhs, @Qty, @uom
                            )";
                            _logger.LogInformation(" InventoryTransferController : SaveDraftInventoryTransferDetail() Query for line {l} : {q} ", row, _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@InvId", _NextInvId);
                            cmd.Parameters.AddWithValue("@invDetId", row);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", dtItem.Rows[0]["ItemName"]);
                            cmd.Parameters.AddWithValue("@frWhs", item.FromWhs);
                            cmd.Parameters.AddWithValue("@toWhs", item.ToWhs);
                            cmd.Parameters.AddWithValue("@Qty", item.Qty);
                            cmd.Parameters.AddWithValue("@uom", item.UoM);

                            intNum = 0;
                            try
                            {
                                QITcon.Open();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex)
                            {
                                this.DeleteDraftInventoryTransfer(_NextInvId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    StatusMsg = "For line : " + row + " Error : " + ex.Message.ToString()
                                });
                            }

                            if (intNum > 0)
                                SucessCount++;

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
                            InvId = _NextInvId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftInventoryTransfer(_NextInvId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Draft Inventory Transfer failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteDraftInventoryTransfer(_NextInvId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("InventoryTransferController : SaveDraftITOfRejectedIT Error : " + ex.ToString());
                this.DeleteDraftInventoryTransfer(_NextInvId);
                _logger.LogError("Error in InventoryTransferController : SaveDraftITOfRejectedIT() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion

    }
}
