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
    public class ProductionOrderController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        private SqlConnection QITcon;
        private SqlConnection SAPcon;
        private SqlDataAdapter oAdptr;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ProductionOrderController> _logger;

        public ProductionOrderController(IConfiguration configuration, ILogger<ProductionOrderController> logger)
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
                Global.gItemWhsCode = Configuration["ItemWhsCode"];

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
                objGlobal.WriteLog(" Error in ProductionOrderController :: " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController :: {ex}" + ex.ToString());
            }
        }


        #region Display API/Fill APIs for Initiate Production Order

        [HttpGet("ProductionOrderType")]
        public async Task<ActionResult<IEnumerable<ProductionOrderType>>> ProductionOrderType()
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : ProductionOrderType() ");

                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT TypeId ID, TypeName Name FROM " + Global.QIT_DB + @".dbo.QIT_ProOrd_Type ORDER BY Id ";

                _logger.LogInformation(" ProductionOrderController : ProductionOrderType() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderType> obj = new List<ProductionOrderType>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderType>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : ProductionOrderType Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : ProductionOrderType() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("ProductionOrderStatus")]
        public async Task<ActionResult<IEnumerable<ProductionOrderStatus>>> ProductionOrderStatus()
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : ProductionOrderStatus() ");

                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT StatusId ID, StatusName Name FROM " + Global.QIT_DB + @".dbo.QIT_ProOrd_Status ORDER BY Id ";

                _logger.LogInformation(" ProductionOrderController : ProductionOrderStatus() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderStatus> obj = new List<ProductionOrderStatus>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderStatus>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : ProductionOrderStatus Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : ProductionOrderStatus() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetDocumentNo")]
        public async Task<ActionResult<IEnumerable<ProductionOrderDocNo>>> GetDocumentNo(int Series)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : GetDocumentNo() ");

                if (Series <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });

                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT Series, SeriesName, NextNumber 
                            FROM " + Global.SAP_DB + @".dbo.NNM1 
                            WHERE series = @series ";

                _logger.LogInformation(" ProductionOrderController : GetDocumentNo() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", Series);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderDocNo> obj = new List<ProductionOrderDocNo>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderDocNo>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : GetDocumentNo Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : GetDocumentNo() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductList")]
        public async Task<ActionResult<IEnumerable<ProductList>>> GetProductList()
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : GetProductList() ");

                System.Data.DataTable dtData = new();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT T1.[ItemCode], T1.[ItemName], T1.[OnHand] 
                FROM  [dbo].[OITT] T0  
                      INNER  JOIN [dbo].[OITM] T1  ON  T0.[Code] = T1.[ItemCode]   
                WHERE T0.[TreeType] = 'P' AND  
	                  ( (  T1.[validFor] = 'N' OR  
	                       (T1.[validFrom] IS NULL OR T1.[validFrom] <= @date ) AND  
		                   (T1.[validTo] IS NULL OR T1.[validTo] >= @date )
	                    ) AND
	                    (  T1.[frozenFor] = ('N') OR  
		                   T1.[frozenFrom] IS NOT NULL AND T1.[frozenFrom] > @date  OR  T1.[frozenTo] IS NOT NULL   AND  T1.[frozenTo] < @date
		                )
	                  )  
                ORDER BY T1.[ItemCode]
                ";

                _logger.LogInformation(" ProductionOrderController : GetProductList() Query : {q} ", _Query.ToString());
                await SAPcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                oAdptr.Fill(dtData);
                SAPcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductList> obj = new List<ProductList>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : GetProductList Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : GetProductList() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetBOMData")]
        public async Task<ActionResult<IEnumerable<BOMHeader>>> GetBOMData(string ItemCode)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : GetBOMData() ");

                #region ItemCode Validation
                if (ItemCode == null)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Product Number" });
                if (ItemCode == string.Empty || ItemCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Product Number" });
                #endregion

                System.Data.DataTable dtBOM = new();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT A.Code ProductNo, A.Name ProductName, CAST(A.Qauntity as numeric(19,3)) HeaderPlannedQty, D.InvntryUom HeaderUoM, 
                       A.ToWH HeaderWhsCode, A.Project HeaderProject,
	                   B.Code ItemCode, C.ItemName ItemName, 
	                   CAST(B.Quantity as numeric(19,3)) BaseQty, CAST(B.Quantity as numeric(19,3)) BaseQtyBOM,
                       B.Quantity BaseRatio, CAST(B.Quantity as numeric(19,3)) PlannedQty, 0 IssuedQty,
	                   CAST(( SELECT (ISNULL(Z.OnHand,0) + ISNULL(Z.OnOrder,0)) - ISNULL(Z.IsCommited,0) 
                         FROM OITW Z WHERE Z.ItemCode = C.ItemCode and Z.WhsCode = B.Warehouse
                       ) as numeric(19,3)) AvailableQty, C.InvntryUom UoM, C.IssueMthd IssueMethod,
                       CASE WHEN  C.IssueMthd = 'M' THEN 'Manual' WHEN  C.IssueMthd = 'B' THEN 'Backflush' END IssueMethodName,
	                   B.Warehouse WhsCode, B.Project Project,
	                   CAST(( SELECT sum(OnHand) FROM OITW WHERE ItemCode = C.ItemCode) as numeric(19,3)) InStock,
                       CAST(( SELECT OnHand FROM OITW WHERE ItemCode = C.ItemCode and WhsCode = B.Warehouse) as numeric(19,3)) QtyInWhs
                FROM OITT A INNER JOIN ITT1 B ON A.Code = B.Father
                INNER JOIN OITM C ON B.Code = C.ItemCode
                INNER JOIN OITM D ON D.ItemCode = A.Code
                WHERE A.Code = @code
                ";

                _logger.LogInformation(" ProductionOrderController : GetBOMData() Query : {q} ", _Query.ToString());
                await SAPcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@code", ItemCode);
                oAdptr.Fill(dtBOM);
                SAPcon.Close();

                if (dtBOM.Rows.Count > 0)
                {
                    List<BOMHeader> obj = new List<BOMHeader>();
                    List<BOMDetail> oBOMDetail = new List<BOMDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtBOM);
                    oBOMDetail = JsonConvert.DeserializeObject<List<BOMDetail>>(arData.ToString());

                    obj.Add(new BOMHeader()
                    {
                        ProductNo = dtBOM.Rows[0]["ProductNo"].ToString(),
                        ProductName = dtBOM.Rows[0]["ProductName"].ToString(),
                        HeaderPlannedQty = dtBOM.Rows[0]["HeaderPlannedQty"].ToString(),
                        HeaderUoM = dtBOM.Rows[0]["HeaderUoM"].ToString(),
                        HeaderWhsCode = dtBOM.Rows[0]["HeaderWhsCode"].ToString(),
                        HeaderProject = dtBOM.Rows[0]["HeaderProject"].ToString(),
                        BOMDet = oBOMDetail
                    });

                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "BOM does not exist for Item : " + ItemCode });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : GetBOMData Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : GetBOMData() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetSpecialProductList")]
        public async Task<ActionResult<IEnumerable<ProductList>>> GetSpecialProductList()
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : GetSpecialProductList() ");

                System.Data.DataTable dtData = new();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT T0.[ItemCode], T0.[ItemName], T0.[OnHand] 
                FROM [dbo].[OITM] T0 
                WHERE (
		                ( T0.[validFor] = 'N' OR  
		                  (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
		                ) AND  
		                ( T0.[frozenFor] = 'N' OR  
		                  T0.[frozenFrom] IS NOT NULL AND T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date )
		                )  
                ORDER BY T0.[ItemCode] 
                ";

                _logger.LogInformation(" ProductionOrderController : GetSpecialProductList() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                oAdptr.Fill(dtData);
                SAPcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductList> obj = new List<ProductList>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : GetSpecialProductList Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : GetSpecialProductList() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetSpecialHeaderItemData")]
        public async Task<ActionResult<IEnumerable<SpecialItemHeaderData>>> GetSpecialHeaderItemData(int BranchId, string ItemCode)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : GetSpecialHeaderItemData() ");

                #region BranchId Validation 
                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                #endregion

                #region ItemCode Validation
                if (ItemCode == null)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Product Number" });
                if (ItemCode == string.Empty || ItemCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Product Number" });
                #endregion

                System.Data.DataTable dtItemData = new();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT T0.[ItemCode] ProductNo , T0.[ItemName] ProductName, 1 PlannedQty, T0.[InvntryUom] UoM,
	                   (select A.DflWhs from OBPL A where ISNULL(A.BPLId, @bId) = @bId ) WhsCode
                FROM [dbo].[OITM] T0 
                WHERE T0.[ItemCode] = @itemCode  
                ";

                _logger.LogInformation(" ProductionOrderController : GetSpecialHeaderItemData() Query : {q} ", _Query.ToString());
                await SAPcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtItemData);
                SAPcon.Close();

                if (dtItemData.Rows.Count > 0)
                {
                    List<SpecialItemHeaderData> obj = new List<SpecialItemHeaderData>();
                    dynamic arData = JsonConvert.SerializeObject(dtItemData);
                    obj = JsonConvert.DeserializeObject<List<SpecialItemHeaderData>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item does not exist : " + ItemCode });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : GetSpecialHeaderItemData Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : GetSpecialHeaderItemData() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetSpecialDetailItemHelp")]
        public async Task<ActionResult<IEnumerable<SpecialItemDetailHelp>>> GetSpecialDetailItemHelp()
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : GetSpecialDetailItemHelp() ");

                System.Data.DataTable dtData = new();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                select A.ItemCode, A.ItemName, 
                       1 BaseQty, 1 BaseRatio, 1 PlannedQty,
                       A.InStock, A.UoM, A.WhsCode,
	                   CAST(( SELECT (ISNULL(Z.OnHand,0) + ISNULL(Z.OnOrder,0)) - ISNULL(Z.IsCommited,0) 
		                  FROM OITW Z WHERE Z.ItemCode = A.ItemCode and Z.WhsCode = A.WhsCode
		                ) as numeric(19,3)) AvailableQty,
		                CAST(( SELECT OnHand FROM OITW WHERE ItemCode = A.ItemCode and WhsCode = A.WhsCode) as numeric(19,3)) QtyInWhs,
                        IssueMethod, IssueMethodName
                from
                (
                    SELECT T0.[ItemCode], T0.[ItemName], CAST(T0.[OnHand] as numeric(19,3))  InStock, T0.[InvntryUom] UoM,
                           CASE WHEN LEN(T0.DfltWH) > 0 THEN T0.DfltWH ELSE '" + Global.gItemWhsCode + @"' END WhsCode,
                           T0.IssueMthd IssueMethod, CASE WHEN T0.IssueMthd = 'M' THEN 'Manual' WHEN T0.IssueMthd = 'B' THEN 'Backflush' END IssueMethodName
                    FROM [dbo].[OITM] T0 
                    WHERE ( ( T0.[validFor] = 'N' OR 
                              (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND 
		                      (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
	                        ) AND  
		                    (   T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                        T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date 
		                    )
	                      ) AND  T0.[ItemType] <> 'F'
                ) as A
                ORDER BY A.[ItemCode] 
                ";

                _logger.LogInformation(" ProductionOrderController : GetSpecialDetailItemHelp() Query : {q} ", _Query.ToString());
                await SAPcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                oAdptr.Fill(dtData);
                SAPcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<SpecialItemDetailHelp> obj = new List<SpecialItemDetailHelp>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SpecialItemDetailHelp>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : GetSpecialDetailItemHelp Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : GetSpecialDetailItemHelp() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        //[HttpGet("GetItemStock")]
        //public async Task<ActionResult<IEnumerable<ItemStock>>> GetItemStock(string ItemCode, string WhsCode)
        //{
        //    SqlConnection SAPcon;
        //    SqlDataAdapter oAdptr;
        //    try
        //    {
        //        _logger.LogInformation(" Calling ProductionOrderController : GetItemStock() ");

        //        #region ItemCode Validation
        //        if (ItemCode == null)
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });
        //        if (ItemCode == string.Empty || ItemCode.ToString().ToLower() == "string")
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });
        //        #endregion

        //        #region Warehouse Validation
        //        if (WhsCode == null)
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Warehouse" });
        //        if (WhsCode == string.Empty || WhsCode.ToString().ToLower() == "string")
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Warehouse" });
        //        #endregion

        //        System.Data.DataTable dtStock = new();
        //        SAPcon = new SqlConnection(_connection);

        //        _Query = @" 
        //        SELECT A.ItemCode, A.ItemName, 
        //               ( 
        //                    SELECT (ISNULL(Z.OnHand,0) + ISNULL(Z.OnOrder,0)) - ISNULL(Z.IsCommited,0) 
        //                    FROM OITW Z WHERE Z.ItemCode = A.ItemCode and Z.WhsCode = @whsCode
        //               ) AvailableQty,
        //               ( SELECT sum(OnHand) FROM OITW WHERE ItemCode = A.ItemCode) InStock
        //        FROM OITM A 
        //        WHERE A.ItemCode = @itemCode
        //        ";

        //        _logger.LogInformation(" ProductionOrderController : GetItemStock() Query : {q} ", _Query.ToString());
        //        SAPcon.Open();
        //        oAdptr = new SqlDataAdapter(_Query, SAPcon);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", ItemCode);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
        //        oAdptr.Fill(dtStock);
        //        SAPcon.Close();

        //        if (dtStock.Rows.Count > 0)
        //        {
        //            List<ItemStock> obj = new List<ItemStock>();
        //            dynamic arData = JsonConvert.SerializeObject(dtStock);
        //            obj = JsonConvert.DeserializeObject<List<ItemStock>>(arData.ToString());
        //            return obj;
        //        }
        //        else
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Item does not exist : " + ItemCode });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(" Error in ProductionOrderController : GetItemStock() :: {ex}", ex.ToString());
        //        return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
        //    }
        //}

        #endregion


        #region Validate Product No

        [HttpGet("ValidateProductNo")]
        public async Task<ActionResult<IEnumerable<ProductionOrderDocNo>>> ValidateProductNo(string ProductNo)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : ValidateProductNo() ");

                if (ProductNo.Trim().Length <=0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Product No" });

                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT ItemCode, ItemName, SellItem, InvntItem FROM " + Global.SAP_DB + @".dbo.OITM WHERE ItemCode = @itemCode ";

                _logger.LogInformation(" ProductionOrderController : ValidateProductNo() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", ProductNo);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    if (dtData.Rows[0]["InvntItem"].ToString() == "Y")
                        return Ok(new
                        {
                            StatusCode = "200",  
                            StatusMsg = "Ok"
                        });
                    else
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "(-10) Must be a warehouse item"
                        });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No such Product No exists " + ProductNo });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : ValidateProductNo Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : ValidateProductNo() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Display Production Order List in Grid

        [HttpGet("DisplayProOrd")]
        public async Task<ActionResult<IEnumerable<DisplayProOrd>>> DisplayProOrd(string UserName, string UserType)
        {
            // UserType : C:Create V:Verify
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : DisplayProOrd() ");

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);

                string _strWhere = string.Empty;

                if (UserName.ToLower() != "admin")
                {
                    if (UserType.ToLower() == "c")
                        _strWhere = " and A.EntryUser = @uName ";
                }


                _Query = @" 
                SELECT A.ProId,
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry, 
	                   CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, 
                       A.Type, CASE WHEN A.Type = 'S' THEN 'Standard' WHEN A.Type = 'P' THEN 'Special' END TypeName,  
	                   CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approved' WHEN A.Action = 'R' THEN 'Rejected' END State,
	                   B.SeriesName, A.OrderDate PostingDate, A.ProductNo, A.ProductName, 
                       CAST(A.PlannedQty as numeric(19,3)) PlannedQty, ISNULL(CAST(E.CmpltQty as numeric(19,3)),0) CompletedQty, 
                       A.Project, A.WhsCode, C.OcrName DistRule, D.ShiftName Shift, A.UoM,
	                   CASE WHEN A.Status = 'P' THEN 'Planned' WHEN A.Status = 'R' THEN 'Released' 
	                        WHEN A.Status = 'C' THEN 'Canceled' WHEN A.Status = 'L' THEN 'Closed' END Status,
	                   A.draftRemark Remark
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master D on D.ShiftId = A.Shift
                LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR E ON E.DocEntry = A.DocEntry
                WHERE A.ProId NOT IN ( SELECT PrevProId from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header ) AND A.Status <> 'L' 
                AND A.ProId NOT IN (
					select Z.ProId   ---,  DATEDIFF(DAY, Z.ActionDate, getdate())
					from " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header Z where Z.Action = 'R' 
					and DATEDIFF(DAY, Z.ActionDate, getdate()) >= (select RejectDocDays from " + Global.QIT_DB + @".dbo.QIT_Config_Master)
				) 
                " + _strWhere + @"
                ORDER BY A.ProId
                ";

                _logger.LogInformation(" ProductionOrderController : DisplayProOrd() Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@uName", UserName);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<DisplayProOrd> obj = new List<DisplayProOrd>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    obj = JsonConvert.DeserializeObject<List<DisplayProOrd>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : DisplayProOrd Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : DisplayProOrd() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Save Draft Production Order

        [HttpPost("SaveDraftProductionOrder")]
        public IActionResult SaveDraftProductionOrder([FromBody] SaveDraftProductionOrder payload)
        {
            string _IsSaved = "N";
            int _ProId = 0;

            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : SaveDraftProductionOrder() ");

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.proDetail.Count();

                    #region Get ProId  
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(ProId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A  ";
                    _logger.LogInformation(" ProductionOrderController : Get ProId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _ProId = (Int32)cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    #region Header Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.Series <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });

                    #region Check for Status

                    if (payload.Status.ToString().ToUpper() != "P")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Status value should be P : Planned" });

                    #endregion

                    #region Check for Type

                    if (payload.Type.ToString().ToUpper() != "S" && payload.Type.ToString().ToUpper() != "P")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Type Values : S:Standard / P:Special" });

                    #endregion

                    #region Check for Product No

                    if (payload.ProductNo.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Product No" });

                    System.Data.DataTable dtProduct = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OITM WHERE ItemCode = @itemCode ";
                    _logger.LogInformation(" ProductionOrderController : Product No Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", payload.ProductNo);
                    oAdptr.Fill(dtProduct);
                    QITcon.Close();

                    if (dtProduct.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Product No does not exist : " + payload.ProductNo
                        });
                    #endregion

                    #region Check for Planned Qty

                    if (payload.PlannedQty.ToString() == "0" || double.Parse(payload.PlannedQty.ToString()) <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Header Planned Qty" });

                    #endregion

                    #region Check for UoM

                    if (payload.UoM != dtProduct.Rows[0]["InvntryUom"].ToString())
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Provide valid UoM for Product No : " + payload.ProductNo
                        });

                    #endregion

                    #region Check for Warehouse

                    if (payload.WhsCode.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse" });

                    System.Data.DataTable dtWhs = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                    _logger.LogInformation(" ProductionOrderController : Header Warehouse Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", payload.WhsCode);
                    oAdptr.Fill(dtWhs);
                    QITcon.Close();

                    if (dtWhs.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Header Warehouse does not exist : " + payload.WhsCode
                        });
                    #endregion

                    #region Check for DistRule

                    if (payload.DistRule.ToString().Length > 0)
                    {
                        //return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Dist Rule" });

                        System.Data.DataTable dtOCR = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OOCR WHERE OcrCode = @ocrCode and Active = 'Y' ";
                        _logger.LogInformation(" ProductionOrderController : Dist Rule Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@ocrCode", payload.DistRule);
                        oAdptr.Fill(dtOCR);
                        QITcon.Close();

                        if (dtOCR.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Dist Rule does not exist : " + payload.DistRule
                            });
                    }
                    #endregion

                    #region Check for Project

                    if (payload.Project.ToString().Length > 0)
                    {
                        System.Data.DataTable dtProject = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OPRJ WHERE PrjCode = @proj AND Locked = 'N' and Active = 'Y' ";
                        _logger.LogInformation(" ProductionOrderController : Header Project Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@proj", payload.Project);
                        oAdptr.Fill(dtProject);
                        QITcon.Close();

                        if (dtProject.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Header Project does not exist : " + payload.Project
                            });
                    }

                    #endregion

                    #region Check for Customer

                    if (payload.Customer.ToString().Length > 0)
                    {
                        System.Data.DataTable dtCustomer = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        SELECT T0.[CardCode], T0.[CardName], T0.[Balance], 'Customer' CardType, T0.[CntctPrsn] ContactPerson
                        FROM " + Global.SAP_DB + @".dbo.OCRD T0 
                        WHERE T0.[CardType] = 'C'  AND  
	                          (
		                        (  T0.[validFor] = 'N' OR (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND  
		                           (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
		                        ) AND  
		                        (  T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                           T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date
		                        )
	                          ) AND  T0.[CardType] <> 'L' AND T0.CardCode = @cardCode
                        ";
                        _logger.LogInformation(" ProductionOrderController : Customer Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@cardCode", payload.Customer);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                        oAdptr.Fill(dtCustomer);
                        QITcon.Close();

                        if (dtCustomer.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Customer does not exist : " + payload.Customer
                            });
                    }

                    #endregion

                    #region Check for Shift

                    if (payload.Shift.ToString().Length > 0)
                    {
                        System.Data.DataTable dtShift = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        SELECT *
                        FROM " + Global.QIT_DB + @".dbo.QIT_Shift_Master T0 
                        WHERE T0.ShiftId = @shift
                        ";
                        _logger.LogInformation(" ProductionOrderController : Shift Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@shift", payload.Shift);
                        oAdptr.Fill(dtShift);
                        QITcon.Close();

                        if (dtShift.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Shift does not exist : " + payload.Shift
                            });
                    }
                    else
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Provide Shift"
                        });

                    #endregion

                    #region Check for Actual Weight

                    //if (payload.ActWgt.ToString().Length <= 0)
                    //    return BadRequest(new
                    //    {
                    //        StatusCode = "400",
                    //        IsSaved = _IsSaved,
                    //        StatusMsg = "Provide Actual Weight"
                    //    });

                    #endregion

                    #region Check for Priority

                    if (payload.Priority <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Provide valid Priority"
                        });

                    #endregion

                    #region Check for Login User

                    if (payload.LoginUser.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });

                    System.Data.DataTable dtUser = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @uName ";
                    _logger.LogInformation(" ProductionOrderController : User Query : {q} ", _Query.ToString());
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
                        INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                        (
                            BranchId, ProId, Series, Type, DocEntry, DocNum, ProductNo, ProductName, PlannedQty, UoM, WhsCode, 
                            OrderDate, StartDate, DueDate, DistRule, Project, Customer, Shift, ActWgt, Priority, 
                            EntryDate, EntryUser, DraftRemark, Action, ActionDate,
                            Status, PrevProId
                        ) 
                        VALUES 
                        (
                            @bId, @proId, @series, @type, @docEntry, @docNum, @pNo, @pName, @plannedQty, @uom, @whsCode, @orderDate, @startDate, 
                            @dueDate, @distRule, @proj, @cust, @shift, @actWgt, @p, @eDate, @eUser, @remark, @action, @aDate, @status, 0
                        )";
                    _logger.LogInformation(" ProductionOrderController : SaveDraftProductionOrder() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@proId", _ProId);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@type", payload.Type);
                    cmd.Parameters.AddWithValue("@docEntry", 0);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@pNo", payload.ProductNo);
                    cmd.Parameters.AddWithValue("@pName", dtProduct.Rows[0]["ItemName"]);
                    cmd.Parameters.AddWithValue("@plannedQty", payload.PlannedQty);
                    cmd.Parameters.AddWithValue("@uom", payload.UoM);
                    cmd.Parameters.AddWithValue("@whsCode", payload.WhsCode);
                    cmd.Parameters.AddWithValue("@orderDate", payload.OrderDate);
                    cmd.Parameters.AddWithValue("@startDate", payload.StartDate);
                    cmd.Parameters.AddWithValue("@dueDate", payload.DueDate);
                    cmd.Parameters.AddWithValue("@distRule", payload.DistRule);
                    cmd.Parameters.AddWithValue("@proj", payload.Project);
                    cmd.Parameters.AddWithValue("@cust", payload.Customer);
                    cmd.Parameters.AddWithValue("@shift", payload.Shift);
                    cmd.Parameters.AddWithValue("@actWgt", payload.ActWgt);
                    cmd.Parameters.AddWithValue("@p", payload.Priority);
                    cmd.Parameters.AddWithValue("@status", "P");
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
                        this.DeleteDraftProduction(_ProId);
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
                        foreach (var item in payload.proDetail)
                        {
                            row = row + 1;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }

                            System.Data.DataTable dtItem = new();
                            QITcon = new SqlConnection(_QIT_connection);

                            _Query = @" 
                            SELECT  B.Code ItemCode, C.ItemName ItemName, 
                                    CAST(( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW WHERE ItemCode = C.ItemCode) as numeric(19,3))  InStock,
	                                C.InvntryUom UoM
                            FROM " + Global.SAP_DB + @".dbo.OITT A INNER JOIN " + Global.SAP_DB + @".dbo.ITT1 B ON A.Code = B.Father
                            INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON B.Code = C.ItemCode
                            INNER JOIN " + Global.SAP_DB + @".dbo.OITM D ON D.ItemCode = A.Code
                            WHERE A.Code = @code and C.ItemCode = @itemCode

                            UNION
                               
                            SELECT T0.[ItemCode], T0.[ItemName], CAST(T0.[OnHand] as numeric(19,3)) InStock, T0.[InvntryUom] UoM
                            FROM " + Global.SAP_DB + @".dbo.OITM T0 
                            WHERE ( ( T0.[validFor] = 'N' OR 
                                      (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND 
		                              (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
	                                ) AND  
		                            (   T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                                T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date 
		                            )
	                              ) AND  T0.[ItemType] <> 'F' AND T0.ItemCode = @itemCode
                            
                            ";

                            _logger.LogInformation(" ProductionOrderController : Item Code Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@code", payload.ProductNo);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                            oAdptr.Fill(dtItem);
                            QITcon.Close();

                            if (dtItem.Rows.Count <= 0)
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row
                                });
                            }
                            #endregion

                            #region Check for Base Qty

                            if (item.BaseQty.ToString() == "0")
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Base Qty for line : " + row
                                });
                            }
                            #endregion

                            #region Check for Planned Qty

                            if (item.PlannedQty.ToString() == "0")
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Planned Qty for line : " + row
                                });
                            }
                            #endregion

                            #region Check for UoM

                            if (item.UoMCode != dtItem.Rows[0]["UoM"].ToString())
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide valid UoMCode for line : " + row
                                });
                            }
                            #endregion

                            #region Check for Issue Type

                            if (item.IssueType.ToString().ToUpper() != "M" && item.IssueType.ToString().ToUpper() != "B")
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Issue Type Values : M:Manual / B:Backflush for line : " + row
                                });
                            }
                            #endregion

                            #region Check for Warehouse

                            if (item.WhsCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse for line : " + row });
                            }

                            dtWhs = new();
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" ProductionOrderController : Detail Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftProduction(_ProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail Warehouse does not exist : " + item.WhsCode
                                });
                            }
                            #endregion

                            #region Check for Project

                            //if (item.Project.ToString().Length <= 0)
                            //{
                            //    this.DeleteDraftProduction(_ProId);
                            //    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Project for line : " + row });
                            //}

                            //System.Data.DataTable dtProject = new();
                            //QITcon = new SqlConnection(_QIT_connection);
                            //_Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OPRJ WHERE PrjCode = @proj AND Locked = 'N' and Active = 'Y' ";
                            //_logger.LogInformation(" ProductionOrderController : Detail Project Query : {q} ", _Query.ToString());
                            //QITcon.Open();
                            //oAdptr = new SqlDataAdapter(_Query, QITcon);
                            //oAdptr.SelectCommand.Parameters.AddWithValue("@proj", item.Project);
                            //oAdptr.Fill(dtProject);
                            //QITcon.Close();

                            //if (dtProject.Rows.Count <= 0)
                            //{
                            //    this.DeleteDraftProduction(_ProId);
                            //    return BadRequest(new
                            //    {
                            //        StatusCode = "400",
                            //        IsSaved = _IsSaved,
                            //        StatusMsg = "Detail Project does not exist : " + item.Project
                            //    });
                            //}
                            #endregion

                            #region Save Detail

                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail
                            (
                                BranchId, ProId, ProDetId, LineNum, ItemCode, ItemName, BaseQtyBOM, BaseQty, BaseRatio, PlannedQty, IssuedQty, 
                                UoMCode, IssueType, WhsCode, Project
                            ) 
                            VALUES 
                            (
                                @bId, @proId, @proDetId, @lineNum, @itemCode, @itemName, @baseQtyBOM, @baseQty, @baseRatio, @plannedQty, @issuedQty, 
                                @uomCode, @issueType, @whsCode, @proj
                            )";
                            _logger.LogInformation(" ProductionOrderController : SaveDraftProductionOrderDetail() Query for line " + row + " : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@proId", _ProId);
                            cmd.Parameters.AddWithValue("@proDetId", row);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", dtItem.Rows[0]["ItemName"]);
                            cmd.Parameters.AddWithValue("@baseQtyBOM", item.BaseQtyBOM);
                            cmd.Parameters.AddWithValue("@baseQty", item.BaseQty);
                            cmd.Parameters.AddWithValue("@baseRatio", item.BaseRatio);
                            cmd.Parameters.AddWithValue("@plannedQty", item.PlannedQty);
                            cmd.Parameters.AddWithValue("@issuedQty", 0);
                            cmd.Parameters.AddWithValue("@uomCode", item.UoMCode);
                            cmd.Parameters.AddWithValue("@issueType", item.IssueType);
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
                                this.DeleteDraftProduction(_ProId);
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
                            ProId = _ProId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftProduction(_ProId);
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Draft Production Order failed while saving"
                        });
                    }
                }
                else
                {
                    this.DeleteDraftProduction(_ProId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : SaveDraftProductionOrder Error : " + ex.ToString());
                this.DeleteDraftProduction(_ProId);
                _logger.LogError("Error in ProductionOrderController : SaveDraftProductionOrder() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        private bool DeleteDraftProduction(int p_ProId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : DeleteDraftProduction() ");

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail WHERE ProId = @proId
                DELETE FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE ProId = @proId
                ";
                _logger.LogInformation(" ProductionOrderController : DeleteDraftProduction Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@proId", p_ProId);
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
                objGlobal.WriteLog("ProductionOrderController : DeleteDraftProduction Error : " + ex.ToString());
                _logger.LogError("Error in ProductionOrderController : DeleteDraftProduction() :: {ex}", ex.ToString());
                return false;
            }
        }

        #endregion


        #region Get Production with Detail on Grid Click

        [HttpGet("GetProductionDetails")]
        public async Task<ActionResult<IEnumerable<ProHeader>>> GetProductionDetails(int BranchId, int ProId)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : GetProductionDetails() ");

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
                _logger.LogInformation(" ProductionOrderController : Pro Id Query : {q} ", _Query.ToString());
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

                #region Check for Pro Id - Initiated again or not

                dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE PrevProId = @proId AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionOrderController : Pro Id Query : {q} ", _Query.ToString());
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proId", ProId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "A new production order has already been initiated for this rejected order"
                    });
                }
                #endregion

                QITcon = new SqlConnection(_QIT_connection);

                #region Query
                _Query = @" 
                SELECT * FROM 
                (
                SELECT A.ProId, 
                       CASE WHEN A.Status = 'C' THEN 'Canceled' WHEN A.Status = 'L' THEN 'Closed' 
                            WHEN A.Status = 'P' THEN 'Planned' WHEN A.Status = 'R' THEN 'Released' END Status,
		               CASE WHEN A.Action = 'P' THEN 'Pending' WHEN A.Action = 'A' THEN 'Approve' WHEN A.Action = 'R' THEN 'Reject' END State,
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocEntry end DocEntry,
                       CASE WHEN A.Action = 'P' THEN '-' else A.DocNum end DocNum, A.Series, B.SeriesName, 
                       CAST(A.PlannedQty as numeric(19,3)) HeaderPlannedQty,
		               A.ProductNo, A.DraftRemark Remark, A.ActionRemark Reason, 
		               A.ProductName, CASE WHEN A.Type = 'S' THEN 'Standard' WHEN A.Type = 'P' THEN 'Special' END Type,  
                       A.UoM HeaderUoM, A.OrderDate PostingDate, A.StartDate, A.DueDate, A.Customer, A.WhsCode HeaderWhsCode, C.OcrName DistRule,
		               A.Project HeaderProject, A.ActWgt, A.Priority, 
                       A.Shift ShiftId, E.ShiftName,
                       D.LineNum,
                       D.ItemCode ItemCode, D.ItemName ItemName, 
                       CAST(D.BaseQty as numeric(19,3)) BaseQty, 
                       CAST(D.BaseQtyBOM as numeric(19,3)) BaseQtyBOM, 
                       CAST(D.BaseQtyBOM as numeric(19,3)) - CAST(D.BaseQty as numeric(19,3)) DiffBaseQty,
                       D.BaseRatio, 
                       CAST(D.PlannedQty as numeric(19,3)) PlannedQty, ISNULL(CAST(BB.IssuedQty as numeric(19,3)),0) IssuedQty,
		               CAST((  SELECT (SUM(ISNULL(Z.Onhand,0)) + SUM(ISNULL(Z.OnOrder,0))) - SUM(ISNULL(Z.IsCommited,0))
		                  FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode AND 
                                Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = D.WhsCode 
                       ) as numeric(19,3)) AvailQty, D.UomCode DetailUoM, 
		               CASE WHEN D.IssueType = 'M' THEN 'Manual' WHEN D.IssueType = 'B' THEN 'Backflush' END IssueType,
		               D.WhsCode DetailWhsCode,
		               CAST((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                          WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                       ) as numeric(19,3)) InStock, 
                       CAST((  SELECT SUM(ISNULL(Z.Onhand,0)) FROM " + Global.SAP_DB + @".dbo.OITW Z 
                           WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode and Z.WhsCode collate SQL_Latin1_General_CP1_CI_AS = D.WhsCode
                       ) as numeric(19,3)) WhsQty,
                       D.Project DetailProject
                FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header  A
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail D ON A.ProId = D.ProId
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Shift_Master E ON E.ShiftId = A.Shift
                LEFT JOIN " + Global.SAP_DB + @".dbo.OOCR C On A.DistRule collate SQL_Latin1_General_CP850_CI_AS = C.OcrCode
                LEFT JOIN " + Global.SAP_DB + @".dbo.OWOR AA ON AA.DocEntry = A.DocEntry
				LEFT JOIN " + Global.SAP_DB + @".dbo.WOR1 BB ON AA.DocEntry = BB.DocEntry and BB.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
                WHERE A.ProId = @proId AND ISNULL(A.BranchId, @bId) = @bId
                ) AS A
                ORDER BY A.LineNum 
                ";
                #endregion

                _logger.LogInformation(" ProductionOrderController : GetProductionDetails() Query : {q} ", _Query.ToString());
                dtPro = new();
                await QITcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proId", ProId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
                oAdptr.Fill(dtPro);
                QITcon.Close();

                if (dtPro.Rows.Count > 0)
                {
                    List<ProHeader> obj = new List<ProHeader>();
                    List<ProDetail> proDetail = new List<ProDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtPro);
                    proDetail = JsonConvert.DeserializeObject<List<ProDetail>>(arData.ToString());

                    obj.Add(new ProHeader()
                    {
                        ProId = int.Parse(dtPro.Rows[0]["ProId"].ToString()),
                        Status = dtPro.Rows[0]["Status"].ToString(),
                        State = dtPro.Rows[0]["State"].ToString(),
                        DocEntry = dtPro.Rows[0]["DocEntry"].ToString(),
                        DocNum = dtPro.Rows[0]["DocNum"].ToString(),
                        Series = int.Parse(dtPro.Rows[0]["Series"].ToString()),
                        SeriesName = dtPro.Rows[0]["SeriesName"].ToString(),
                        HeaderPlannedQty = dtPro.Rows[0]["HeaderPlannedQty"].ToString(),
                        ProductNo = dtPro.Rows[0]["ProductNo"].ToString(),
                        Remark = dtPro.Rows[0]["Remark"].ToString(),
                        Reason = dtPro.Rows[0]["Reason"].ToString(),
                        ProductName = dtPro.Rows[0]["ProductName"].ToString(),
                        Type = dtPro.Rows[0]["Type"].ToString(),
                        HeaderUoM = dtPro.Rows[0]["HeaderUoM"].ToString(),
                        PostingDate = dtPro.Rows[0]["PostingDate"].ToString(),
                        StartDate = dtPro.Rows[0]["StartDate"].ToString(),
                        DueDate = dtPro.Rows[0]["DueDate"].ToString(),
                        Customer = dtPro.Rows[0]["Customer"].ToString(),
                        HeaderWhsCode = dtPro.Rows[0]["HeaderWhsCode"].ToString(),
                        DistRule = dtPro.Rows[0]["DistRule"].ToString(),
                        HeaderProject = dtPro.Rows[0]["HeaderProject"].ToString(),
                        ActWgt = dtPro.Rows[0]["ActWgt"].ToString(),
                        Priority = dtPro.Rows[0]["Priority"].ToString(),
                        ShiftId = dtPro.Rows[0]["ShiftId"].ToString(),
                        ShiftName = dtPro.Rows[0]["ShiftName"].ToString(),
                        proDetail = proDetail
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
                objGlobal.WriteLog("ProductionOrderController : GetProductionDetails Error : " + ex.ToString());
                _logger.LogError(" Error in ProductionOrderController : GetProductionDetails() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Approve/Reject Production Order

        [HttpPost("VerifyDraftProductionOrder")]
        public async Task<IActionResult> VerifyDraftProductionOrder([FromBody] VerifyDraftProductionOrder payload)
        {
            string _IsSaved = "N";
            int _docEntry = 0;
            int _docNum = 0;
            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : VerifyDraftProductionOrder() ");

                if (payload != null)
                {
                    #region Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    #region Check for Pro Id

                    if (payload.ProId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order Id" });

                    System.Data.DataTable dtPro = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE ProId = @proId AND ISNULL(BranchId, @bId) = @bId ";
                    _logger.LogInformation(" ProductionOrderController : VerifyDraftProductionOrder : Pro Id Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@proId", payload.ProId);
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
                        if (dtPro.Rows[0]["Action"].ToString() != "P")
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Production Order is already approved or rejected"
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

                    #region Get Production Order Data

                    System.Data.DataTable dtProDetails = new();

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail 
                    WHERE ProId = @proId AND ISNULL(BranchId, @bId) = @bId 
                    ORDER BY LineNum ";
                    _logger.LogInformation(" ProductionOrderController : VerifyDraftProductionOrder : Detail Data Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@proId", payload.ProId);
                    oAdptr.Fill(dtProDetails);
                    QITcon.Close();

                    #endregion

                    #region Save Production Order in SAP
                    if (payload.Action.ToUpper() == "A")
                    {
                        var (success, errorMsg) = await objGlobal.ConnectSAP();
                        if (success)
                        {
                            ProductionOrders proOrder = (ProductionOrders)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oProductionOrders);
                            proOrder.Series = int.Parse(dtPro.Rows[0]["Series"].ToString());

                            if (dtPro.Rows[0]["type"].ToString().ToUpper() == "S")
                                proOrder.ProductionOrderType = BoProductionOrderTypeEnum.bopotStandard;
                            else if (dtPro.Rows[0]["type"].ToString().ToUpper() == "P")
                                proOrder.ProductionOrderType = BoProductionOrderTypeEnum.bopotSpecial;

                            proOrder.ProductionOrderStatus = BoProductionOrderStatusEnum.boposPlanned;
                            proOrder.ItemNo = dtPro.Rows[0]["ProductNo"].ToString();
                            //proOrder.ProductDescription = dtPro.Rows[0]["ProductNo"].ToString();

                            proOrder.PlannedQuantity = double.Parse(dtPro.Rows[0]["PlannedQty"].ToString());
                            proOrder.Warehouse = dtPro.Rows[0]["WhsCode"].ToString();

                            proOrder.PostingDate = (DateTime)dtPro.Rows[0]["OrderDate"];
                            proOrder.StartDate = (DateTime)dtPro.Rows[0]["StartDate"];
                            proOrder.DueDate = (DateTime)dtPro.Rows[0]["DueDate"];
                            proOrder.CustomerCode = dtPro.Rows[0]["Customer"].ToString();
                            proOrder.DistributionRule = dtPro.Rows[0]["DistRule"].ToString();
                            proOrder.Project = dtPro.Rows[0]["Project"].ToString();
                            proOrder.Remarks = dtPro.Rows[0]["DraftRemark"].ToString();

                            proOrder.UserFields.Fields.Item("U_Shift").Value = dtPro.Rows[0]["Shift"].ToString();
                            proOrder.UserFields.Fields.Item("U_AW").Value = dtPro.Rows[0]["ActWgt"].ToString();
                            proOrder.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                            for (int i = 0; i < dtProDetails.Rows.Count; i++)
                            {
                                proOrder.Lines.ItemNo = dtProDetails.Rows[i]["ItemCode"].ToString();
                                proOrder.Lines.BaseQuantity = double.Parse(dtProDetails.Rows[i]["BaseQty"].ToString());
                                proOrder.Lines.PlannedQuantity = double.Parse(dtProDetails.Rows[i]["PlannedQty"].ToString());
                                proOrder.Lines.Warehouse = dtProDetails.Rows[i]["WhsCode"].ToString();

                                if (dtProDetails.Rows[i]["IssueType"].ToString().ToUpper() == "M")
                                    proOrder.Lines.ProductionOrderIssueType = BoIssueMethod.im_Manual;
                                else
                                    proOrder.Lines.ProductionOrderIssueType = BoIssueMethod.im_Backflush;

                                proOrder.Lines.Project = dtProDetails.Rows[i]["Project"].ToString();

                                proOrder.Lines.Add();
                            }

                            int addResult = proOrder.Add();

                            if (addResult != 0)
                            {
                                string msg = "(" + objGlobal.oComp.GetLastErrorCode() + ") " + objGlobal.oComp.GetLastErrorDescription();
                                _logger.LogInformation(" ProductionOrderController : VerifyDraftProductionOrder : Error " + msg);
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

                                #region Get Production Order Data from SAP
                                QITcon = new SqlConnection(_QIT_connection);
                                System.Data.DataTable dtSAPPro = new();
                                _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWOR where DocEntry = @docEntry  ";
                                _logger.LogInformation(" ProductionOrderController : VerifyDraftProductionOrder : Get Production Order Data from SAP : Query : {q} ", _Query.ToString());
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", _docEntry);
                                oAdptr.Fill(dtSAPPro);
                                QITcon.Close();
                                _docNum = int.Parse(dtSAPPro.Rows[0]["DocNum"].ToString());
                                #endregion

                                #region Update Production Table
                                QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" 
                                UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header 
                                SET DocEntry = @docEntry, DocNum = @docNum 
                                WHERE ProId = @proId";
                                _logger.LogInformation(" ProductionOrderController : VerifyDraftProductionOrder : Update Production Table Query : {q} ", _Query.ToString());
                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@docEntry", _docEntry);
                                cmd.Parameters.AddWithValue("@docNum", _docNum);
                                cmd.Parameters.AddWithValue("@proId", payload.ProId);

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
                                        StatusMsg = "Problem in updating Production Table"
                                    });
                                }
                                #endregion
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
                    }
                    #endregion

                    #region Update production
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                    SET Action = @action, ActionDate = @aDate, ActionUser = @aUser, ActionRemark = @remark 
                    WHERE ProId = @proId and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" ProductionOrderController : VerifyDraftProductionOrder : Update Production Order Action Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@action", payload.Action.ToUpper());
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@aUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.ActionRemark);
                    cmd.Parameters.AddWithValue("@proId", payload.ProId);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);

                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    if (payload.Action.ToUpper() == "A")
                        return Ok(new
                        {
                            StatusCode = "200",
                            IsSaved = "Y",
                            DocEntry = _docEntry,
                            DocNum = _docNum,
                            StatusMsg = "Production Order created successfully in SAP"
                        });
                    else
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Production Order rejected" });
                    #endregion
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : VerifyDraftProductionOrder Error : " + ex.ToString());
                _logger.LogError("Error in ProductionOrderController : VerifyDraftProductionOrder() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion


        #region Release Production Order

        [HttpPost("ReleaseProductionOrder")]
        public async Task<IActionResult> ReleaseProductionOrder(int BranchId, int DocEntry)
        {
            string _IsSaved = "N";

            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : ReleaseProductionOrder() ");


                #region Validation

                if (BranchId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                #region Check for Pro Id

                if (DocEntry <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Order Entry" });

                System.Data.DataTable dtPro = new();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header WHERE DocEntry = @docEntry AND ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" ProductionOrderController : ReleaseProductionOrder : Pro Id Query : {q} ", _Query.ToString());
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

                    if (dtPro.Rows[0]["Status"].ToString() == "R")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Order is already Released"
                        });

                    if (dtPro.Rows[0]["Status"].ToString() != "P")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Production Order status must be Planned"
                        });
                }
                #endregion

                #endregion

                #region Update Production
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                    UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                    SET CanReleaseInSAP = 'Y'
                    WHERE DocEntry = @docEntry and ISNULL(BranchID, @bId) = @bId";
                _logger.LogInformation(" ProductionOrderController : ReleaseProductionOrder : Update Production Order Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@docEntry", DocEntry);
                cmd.Parameters.AddWithValue("@bId", BranchId);

                QITcon.Open();
                int intNum = cmd.ExecuteNonQuery();
                QITcon.Close();

                if (intNum > 0)
                {
                    _IsSaved = "Y";

                    #region Update Production Order in SAP

                    var (success, errorMsg) = await objGlobal.ConnectSAP();
                    if (success)
                    {
                        ProductionOrders proOrder = (ProductionOrders)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oProductionOrders);
                        if (proOrder.GetByKey(DocEntry))
                        {
                            proOrder.ProductionOrderStatus = BoProductionOrderStatusEnum.boposReleased;
                            int updateResult = proOrder.Update();

                            if (updateResult != 0)
                            {
                                string msg = "(" + objGlobal.oComp.GetLastErrorCode() + ") " + objGlobal.oComp.GetLastErrorDescription();
                                _logger.LogInformation(" ProductionOrderController : ReleaseProductionOrder : Error " + msg);
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
                    intNum = 0;
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                    SET Status = @status 
                    WHERE DocEntry = @docEntry and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" ProductionOrderController : ReleaseProductionOrder : Update Production Order Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@status", "R");
                    cmd.Parameters.AddWithValue("@docEntry", DocEntry);
                    cmd.Parameters.AddWithValue("@bId", BranchId);

                    QITcon.Open();
                    intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";


                    return Ok(new
                    {
                        StatusCode = "200",
                        IsSaved = "Y",
                        StatusMsg = "Production Order released successfully in SAP"
                    });

                    #endregion
                }

                else
                    _IsSaved = "N";

                #endregion

                return BadRequest(new
                {
                    StatusCode = "400",
                    IsSaved = "N",
                    StatusMsg = "Unable to Release Production Order"
                });


            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : ReleaseProductionOrder Error : " + ex.ToString());
                _logger.LogError("Error in ProductionOrderController : ReleaseProductionOrder() :: {ex}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    IsSaved = _IsSaved,
                    StatusMsg = ex.Message.ToString()
                });
            }
        }

        #endregion


        #region Save Draft Production Order for Rejected one

        [HttpPost("SaveDraftProductionOrderOfRejectedPro")]
        public IActionResult SaveDraftProductionOrderOfRejectedPro(int ProId, [FromBody] SaveDraftProductionOrder payload)
        {
            string _IsSaved = "N";
            int _NextProId = 0;

            try
            {
                _logger.LogInformation(" Calling ProductionOrderController : SaveDraftProductionOrderOfRejectedPro() ");

                if (ProId <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production Id" });

                if (payload != null)
                {
                    int SucessCount = 0;
                    int itemCount = payload.proDetail.Count();

                    #region Get ProId  
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(ProId),0) + 1 FROM " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header A  ";
                    _logger.LogInformation(" ProductionOrderController : Get ProId Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _NextProId = (Int32)cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    #region Header Validation

                    if (payload.BranchId <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    if (payload.Series <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });

                    #region Check for Status

                    if (payload.Status.ToString().ToUpper() != "P")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Status value should be P : Planned" });

                    #endregion

                    #region Check for Type

                    if (payload.Type.ToString().ToUpper() != "S" && payload.Type.ToString().ToUpper() != "P")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Type Values : S:Standard / P:Special" });

                    #endregion

                    #region Check for Product No

                    if (payload.ProductNo.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Product No" });

                    System.Data.DataTable dtProduct = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OITM WHERE ItemCode = @itemCode ";
                    _logger.LogInformation(" ProductionOrderController : Product No Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", payload.ProductNo);
                    oAdptr.Fill(dtProduct);
                    QITcon.Close();

                    if (dtProduct.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Product No does not exist : " + payload.ProductNo
                        });
                    #endregion

                    #region Check for Planned Qty

                    if (payload.PlannedQty.ToString() == "0" || double.Parse(payload.PlannedQty.ToString()) <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Header Planned Qty" });

                    #endregion

                    #region Check for UoM

                    if (payload.UoM != dtProduct.Rows[0]["InvntryUom"].ToString())
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Provide valid UoM for Product No : " + payload.ProductNo
                        });

                    #endregion

                    #region Check for Warehouse

                    if (payload.WhsCode.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse" });

                    System.Data.DataTable dtWhs = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                    _logger.LogInformation(" ProductionOrderController : Header Warehouse Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", payload.WhsCode);
                    oAdptr.Fill(dtWhs);
                    QITcon.Close();

                    if (dtWhs.Rows.Count <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Header Warehouse does not exist : " + payload.WhsCode
                        });
                    #endregion

                    #region Check for DistRule

                    if (payload.DistRule.ToString().Length > 0)
                    {
                        System.Data.DataTable dtOCR = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OOCR WHERE OcrCode = @ocrCode and Active = 'Y' ";
                        _logger.LogInformation(" ProductionOrderController : Dist Rule Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@ocrCode", payload.DistRule);
                        oAdptr.Fill(dtOCR);
                        QITcon.Close();

                        if (dtOCR.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Dist Rule does not exist : " + payload.DistRule
                            });
                    }

                    #endregion

                    #region Check for Project

                    if (payload.Project.ToString().Length > 0)
                    {
                        System.Data.DataTable dtProject = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OPRJ WHERE PrjCode = @proj AND Locked = 'N' and Active = 'Y' ";
                        _logger.LogInformation(" ProductionOrderController : Header Project Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@proj", payload.Project);
                        oAdptr.Fill(dtProject);
                        QITcon.Close();

                        if (dtProject.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Header Project does not exist : " + payload.Project
                            });
                    }

                    #endregion

                    #region Check for Customer

                    if (payload.Customer.ToString().Length > 0)
                    {
                        System.Data.DataTable dtCustomer = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        SELECT T0.[CardCode], T0.[CardName], T0.[Balance], 'Customer' CardType, T0.[CntctPrsn] ContactPerson
                        FROM " + Global.SAP_DB + @".dbo.OCRD T0 
                        WHERE T0.[CardType] = 'C'  AND  
	                          (
		                        (  T0.[validFor] = 'N' OR (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND  
		                           (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
		                        ) AND  
		                        (  T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                           T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date
		                        )
	                          ) AND  T0.[CardType] <> 'L' AND T0.CardCode = @cardCode
                        ";
                        _logger.LogInformation(" ProductionOrderController : Customer Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@cardCode", payload.Customer);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                        oAdptr.Fill(dtCustomer);
                        QITcon.Close();

                        if (dtCustomer.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Customer does not exist : " + payload.Customer
                            });
                    }

                    #endregion

                    #region Check for Shift

                    if (payload.Shift.ToString().Length > 0)
                    {
                        System.Data.DataTable dtShift = new();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        SELECT *
                        FROM " + Global.QIT_DB + @".dbo.QIT_Shift_Master T0 
                        WHERE T0.ShiftId = @shift
                        ";
                        _logger.LogInformation(" ProductionOrderController : Shift Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@shift", payload.Shift);
                        oAdptr.Fill(dtShift);
                        QITcon.Close();

                        if (dtShift.Rows.Count <= 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Shift does not exist : " + payload.Shift
                            });
                    }
                    else
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Provide Shift"
                        });


                    #endregion

                    #region Check for Actual Weight

                    //if (payload.ActWgt.ToString().Length <= 0)
                    //    return BadRequest(new
                    //    {
                    //        StatusCode = "400",
                    //        IsSaved = _IsSaved,
                    //        StatusMsg = "Provide Actual Weight"
                    //    });

                    #endregion

                    #region Check for Priority

                    if (payload.Priority <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Provide valid Priority"
                        });

                    #endregion

                    #region Check for Login User

                    if (payload.LoginUser.ToString().Length <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Actual Weight" });

                    System.Data.DataTable dtUser = new();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM " + Global.QIT_DB + @".dbo.QIT_User_Master WHERE User_Name = @uName ";
                    _logger.LogInformation(" ProductionOrderController : User Query : {q} ", _Query.ToString());
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
                        INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Header
                        (
                            BranchId, ProId, Series, Type, DocEntry, DocNum, ProductNo, ProductName, PlannedQty, UoM, WhsCode, 
                            OrderDate, StartDate, DueDate, DistRule, Project, Customer, Shift, ActWgt, Priority, 
                            EntryDate, EntryUser, DraftRemark, Action, ActionDate,
                            Status, PrevProId
                        ) 
                        VALUES 
                        (
                            @bId, @proId, @series, @type, @docEntry, @docNum, @pNo, @pName, @plannedQty, @uom, @whsCode, @orderDate, @startDate, 
                            @dueDate, @distRule, @proj, @cust, @shift, @actWgt, @p, @eDate, @eUser, @remark, @action, @aDate, @status, @prevProId
                        )";
                    _logger.LogInformation(" ProductionOrderController : SaveDraftProductionOrderOfRejectedPro() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                    cmd.Parameters.AddWithValue("@proId", _NextProId);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@type", payload.Type);
                    cmd.Parameters.AddWithValue("@docEntry", 0);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@pNo", payload.ProductNo);
                    cmd.Parameters.AddWithValue("@pName", dtProduct.Rows[0]["ItemName"]);
                    cmd.Parameters.AddWithValue("@plannedQty", payload.PlannedQty);
                    cmd.Parameters.AddWithValue("@uom", payload.UoM);
                    cmd.Parameters.AddWithValue("@whsCode", payload.WhsCode);
                    cmd.Parameters.AddWithValue("@orderDate", payload.OrderDate);
                    cmd.Parameters.AddWithValue("@startDate", payload.StartDate);
                    cmd.Parameters.AddWithValue("@dueDate", payload.DueDate);
                    cmd.Parameters.AddWithValue("@distRule", payload.DistRule);
                    cmd.Parameters.AddWithValue("@proj", payload.Project);
                    cmd.Parameters.AddWithValue("@cust", payload.Customer);
                    cmd.Parameters.AddWithValue("@shift", payload.Shift);
                    cmd.Parameters.AddWithValue("@actWgt", payload.ActWgt);
                    cmd.Parameters.AddWithValue("@p", payload.Priority);
                    cmd.Parameters.AddWithValue("@status", "P");
                    cmd.Parameters.AddWithValue("@eDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@eUser", payload.LoginUser);
                    cmd.Parameters.AddWithValue("@remark", payload.Remark);
                    cmd.Parameters.AddWithValue("@action", "P");
                    cmd.Parameters.AddWithValue("@aDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@prevProId", ProId);

                    int intNum = 0;
                    try
                    {
                        QITcon.Open();
                        intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();
                    }
                    catch (Exception ex)
                    {
                        this.DeleteDraftProduction(_NextProId);
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
                        foreach (var item in payload.proDetail)
                        {
                            row = row + 1;

                            #region Check for Item Code

                            if (item.ItemCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code for line : " + row });
                            }

                            System.Data.DataTable dtItem = new();
                            QITcon = new SqlConnection(_QIT_connection);


                            _Query = @" 
                            SELECT  B.Code ItemCode, C.ItemName ItemName, 
                                    CAST(( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW WHERE ItemCode = C.ItemCode) as numeric(19,3)) InStock,
	                                C.InvntryUom UoM
                            FROM " + Global.SAP_DB + @".dbo.OITT A INNER JOIN " + Global.SAP_DB + @".dbo.ITT1 B ON A.Code = B.Father
                            INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON B.Code = C.ItemCode
                            INNER JOIN " + Global.SAP_DB + @".dbo.OITM D ON D.ItemCode = A.Code
                            WHERE A.Code = @code and C.ItemCode = @itemCode

                            UNION
                               
                            SELECT T0.[ItemCode], T0.[ItemName], CAST(T0.[OnHand] as numeric(19,3)) InStock, T0.[InvntryUom] UoM
                            FROM " + Global.SAP_DB + @".dbo.OITM T0 
                            WHERE ( ( T0.[validFor] = 'N' OR 
                                      (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND 
		                              (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
	                                ) AND  
		                            (   T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                                T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date 
		                            )
	                              ) AND  T0.[ItemType] <> 'F' AND T0.ItemCode = @itemCode
                            
                            ";

                            _logger.LogInformation(" ProductionOrderController : Item Code Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@code", payload.ProductNo);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                            oAdptr.Fill(dtItem);
                            QITcon.Close();

                            if (dtItem.Rows.Count <= 0)
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Item Code : " + item.ItemCode + " does not exist for line : " + row
                                });
                            }
                            #endregion

                            #region Check for Base Qty

                            if (item.BaseQty.ToString() == "0")
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Base Qty for line : " + row
                                });
                            }

                            #endregion

                            #region Check for Planned Qty

                            if (item.PlannedQty.ToString() == "0" )
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide Planned Qty for line : " + row
                                });
                            }

                            #endregion

                            #region Check for UoM

                            if (item.UoMCode != dtItem.Rows[0]["UoM"].ToString())
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide valid UoM for line : " + row
                                });
                            }

                            #endregion

                            #region Check for Issue Type

                            if (item.IssueType.ToString().ToUpper() != "M" && item.IssueType.ToString().ToUpper() != "B")
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Issue Type Values : M:Manual / B:Backflush for line : " + row
                                });
                            }

                            #endregion

                            #region Check for Warehouse

                            if (item.WhsCode.ToString().Length <= 0)
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse for line : " + row });
                            }

                            dtWhs = new();
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = @whsCode and Locked = 'N' ";
                            _logger.LogInformation(" ProductionOrderController : Detail Warehouse Query : {q} ", _Query.ToString());
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", item.WhsCode);
                            oAdptr.Fill(dtWhs);
                            QITcon.Close();

                            if (dtWhs.Rows.Count <= 0)
                            {
                                this.DeleteDraftProduction(_NextProId);
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Detail Warehouse does not exist : " + item.WhsCode
                                });
                            }

                            #endregion

                            #region Check for Project

                            //if (item.Project.ToString().Length <= 0)
                            //{
                            //    this.DeleteDraftProduction(_NextProId);
                            //    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Project for line : " + row });
                            //}

                            //dtProject = new();
                            //QITcon = new SqlConnection(_QIT_connection);
                            //_Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.OPRJ WHERE PrjCode = @proj AND Locked = 'N' and Active = 'Y' ";
                            //_logger.LogInformation(" ProductionOrderController : Detail Project Query : {q} ", _Query.ToString());
                            //QITcon.Open();
                            //oAdptr = new SqlDataAdapter(_Query, QITcon);
                            //oAdptr.SelectCommand.Parameters.AddWithValue("@proj", item.Project);
                            //oAdptr.Fill(dtProject);
                            //QITcon.Close();

                            //if (dtProject.Rows.Count <= 0)
                            //{
                            //    this.DeleteDraftProduction(_NextProId);
                            //    return BadRequest(new
                            //    {
                            //        StatusCode = "400",
                            //        IsSaved = _IsSaved,
                            //        StatusMsg = "Detail Project does not exist : " + item.Project
                            //    });
                            //}
                            #endregion

                            #region Save Detail

                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                            INSERT INTO " + Global.QIT_DB + @".dbo.QIT_ProductionOrder_Detail
                            (
                                BranchId, ProId, ProDetId, LineNum, ItemCode, ItemName, BaseQtyBOM, BaseQty, BaseRatio, PlannedQty, IssuedQty, 
                                UoMCode, IssueType, WhsCode, Project
                            ) 
                            VALUES 
                            (
                                @bId, @proId, @proDetId, @lineNum, @itemCode, @itemName, @baseQtyBOM, @baseQty, @baseRatio, @plannedQty, @issuedQty, 
                                @uomCode, @issueType, @whsCode, @proj
                            )";
                            _logger.LogInformation(" ProductionOrderController : SaveDraftProductionOrderOfRejectedProDetail() Query for line " + row + " : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bId", payload.BranchId);
                            cmd.Parameters.AddWithValue("@proId", _NextProId);
                            cmd.Parameters.AddWithValue("@proDetId", row);
                            cmd.Parameters.AddWithValue("@lineNum", row - 1);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@itemName", dtItem.Rows[0]["ItemName"]);
                            cmd.Parameters.AddWithValue("@baseQtyBOM", item.BaseQtyBOM);
                            cmd.Parameters.AddWithValue("@baseQty", item.BaseQty);
                            cmd.Parameters.AddWithValue("@baseRatio", item.BaseRatio);
                            cmd.Parameters.AddWithValue("@plannedQty", item.PlannedQty);
                            cmd.Parameters.AddWithValue("@issuedQty", 0);
                            cmd.Parameters.AddWithValue("@uomCode", item.UoMCode);
                            cmd.Parameters.AddWithValue("@issueType", item.IssueType);
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
                                this.DeleteDraftProduction(_NextProId);
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
                            ProId = _NextProId,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    else
                    {
                        this.DeleteDraftProduction(_NextProId);
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
                    this.DeleteDraftProduction(_NextProId);
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("ProductionOrderController : SaveDraftProductionOrderOfRejectedPro Error : " + ex.ToString());
                this.DeleteDraftProduction(_NextProId);
                _logger.LogError("Error in ProductionOrderController : SaveDraftProductionOrderOfRejectedPro() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        #endregion




    }
}
