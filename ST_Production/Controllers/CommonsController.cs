using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ST_Production.Common;
using ST_Production.Models;
using System.Data;
using System.Data.SqlClient;
using Project = ST_Production.Models.Project;

namespace ST_Production.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;

        private string _Query = string.Empty;

        private SqlConnection QITcon;
        private SqlConnection SAPcon;
        private SqlDataAdapter oAdptr;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<CommonsController> _logger;


        public CommonsController(IConfiguration configuration, ILogger<CommonsController> logger)
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
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in CommonsController :: " + ex.ToString());
                _logger.LogError(" Error in CommonsController :: {ex}" , ex.ToString());
            }
        }


        [HttpGet("PeriodIndicator")]
        public async Task<ActionResult<IEnumerable<PeriodIndicator>>> GetPeriodIndicator()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetPeriodIndicator() ");

                DataTable dtPeriod = new();;
                SAPcon = new SqlConnection(_connection);

                _Query = @" SELECT Indicator from OPID ";
                _logger.LogInformation(" CommonsController : GetPeriodIndicator() Query : {q} ", _Query.ToString());
                await SAPcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.Fill(dtPeriod);
                SAPcon.Close();

                if (dtPeriod.Rows.Count > 0)
                {
                    List<PeriodIndicator> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtPeriod);
                    obj = JsonConvert.DeserializeObject<List<PeriodIndicator>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define period indicator in SAP" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetPeriodIndicator Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetPeriodIndicator() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Series")]
        public ActionResult<IEnumerable<SeriesCls>> GetSeries(string? Indicator, string ObjType)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetSeries() ");

                DataTable dtSeries = new();;
                SAPcon = new SqlConnection(_connection);

                if (Indicator == null)
                    _Query = @" select Series, SeriesName from NNM1 WHERE Locked = 'N' and ObjectCode = @objType  ";
                else
                    _Query = @" select Series, SeriesName from NNM1 WHERE Indicator = ISNULL(@indi,Indicator) and Locked = 'N' and ObjectCode = @objType  ";

                _logger.LogInformation(" CommonsController : GetSeries() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                if (Indicator != null)
                    oAdptr.SelectCommand.Parameters.AddWithValue("@indi", Indicator);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", ObjType);
                oAdptr.Fill(dtSeries);
                SAPcon.Close();

                if (dtSeries.Rows.Count > 0)
                {
                    List<SeriesCls> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtSeries);
                    obj = JsonConvert.DeserializeObject<List<SeriesCls>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Series in SAP" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetSeries Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetSeries() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Project")]
        public ActionResult<IEnumerable<Project>> GetProject()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetProject() ");

                DataTable dtProject = new();;
                SAPcon = new SqlConnection(_connection);

                _Query = @" select PrjCode, PrjName from OPRJ where Locked = 'N' and Active = 'Y' ";
                _logger.LogInformation(" CommonsController : GetProject() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.Fill(dtProject);
                SAPcon.Close();

                if (dtProject.Rows.Count > 0)
                {
                    List<Project> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtProject);
                    obj = JsonConvert.DeserializeObject<List<Project>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Project in SAP" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetProject Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetProject() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("DistRule")]
        public ActionResult<IEnumerable<DistRule>> GetDistRule()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetDistRule() ");

                DataTable dtDistRule = new();;
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT A.DimCode, A.DimName, A.DimDesc, B. OcrCode, B.OcrName
                FROM ODIM A 
	                INNER JOIN OOCR B on A.DimCode = B.DimCode
	                INNER JOIN OCR1 C ON (C.[ValidFrom] IS NULL OR C.[ValidFrom] <= @date ) AND (C.[ValidTo] IS NULL OR C.[ValidTo] >= @date )
	                and B.OcrCode = C.OcrCode
                WHERE A.DimActive = 'Y' and B.Active = 'Y'
                ";
                _logger.LogInformation(" CommonsController : GetDistRule() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                oAdptr.Fill(dtDistRule);
                SAPcon.Close();

                if (dtDistRule.Rows.Count > 0)
                {
                    List<DistRule> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtDistRule);
                    obj = JsonConvert.DeserializeObject<List<DistRule>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Distribution Rule in SAP" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetDistRule Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetDistRule() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Customer")]
        public ActionResult<IEnumerable<Customer>> GetCustomer()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetCustomer() ");

                DataTable dtCustomer = new();;
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT T0.[CardCode], T0.[CardName], T0.[Balance], 'Customer' CardType, T0.[CntctPrsn] ContactPerson
                FROM [dbo].[OCRD] T0 
                WHERE T0.[CardType] = 'C'  AND  
	                  (
		                (  T0.[validFor] = 'N' OR (T0.[validFrom] IS NULL OR T0.[validFrom] <= @date ) AND  
		                   (T0.[validTo] IS NULL OR T0.[validTo] >= @date )
		                ) AND  
		                (  T0.[frozenFor] = 'N' OR T0.[frozenFrom] IS NOT NULL AND 
		                   T0.[frozenFrom] > @date OR T0.[frozenTo] IS NOT NULL AND T0.[frozenTo] < @date
		                )
	                  ) AND  T0.[CardType] <> 'L'  
                 ORDER BY T0.[CardName]
                ";
                _logger.LogInformation(" CommonsController : GetCustomer() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@date", DateTime.Now);
                oAdptr.Fill(dtCustomer);
                SAPcon.Close();

                if (dtCustomer.Rows.Count > 0)
                {
                    List<Customer> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtCustomer);
                    obj = JsonConvert.DeserializeObject<List<Customer>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Customer in SAP" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetCustomer Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetCustomer() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("PriceList")]
        public ActionResult<IEnumerable<PriceList>> GetPriceList()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetPriceList() ");

                DataTable dtPriceList = new();;
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT * FROM
                (
                    SELECT A.ListNum, A.ListName FROM " + Global.SAP_DB + @".dbo.OPLN A
                    UNION ALL
                    SELECT A.GroupNum ListNum, A.GroupName ListName from " + Global.QIT_DB + @".dbo.QIT_PriceList A
                ) as A
                ";
                _logger.LogInformation(" CommonsController : GetPriceList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtPriceList);
                QITcon.Close();

                if (dtPriceList.Rows.Count > 0)
                {
                    List<PriceList> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtPriceList);
                    obj = JsonConvert.DeserializeObject<List<PriceList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Price List in SAP" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetPriceList Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetPriceList() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("SalesEmployee")]
        public ActionResult<IEnumerable<SalesEmployee>> GetSalesEmployee()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetSalesEmployee() ");

                DataTable dtSalesEmp = new();;
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT T0.SlpCode, T0.SlpName FROM " + Global.SAP_DB + @".dbo.OSLP T0 
                WHERE T0.Active = 'Y' AND T0.SlpCode <> -1
                ORDER BY T0.SlpCode ";
                _logger.LogInformation(" CommonsController : GetSalesEmployee() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtSalesEmp);
                QITcon.Close();

                if (dtSalesEmp.Rows.Count > 0)
                {
                    List<SalesEmployee> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtSalesEmp);
                    obj = JsonConvert.DeserializeObject<List<SalesEmployee>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Sales Employee in SAP" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetSalesEmployee Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetSalesEmployee() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetItemStock")]
        public async Task<ActionResult<IEnumerable<ItemStock>>> GetItemStock(string ItemCode)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetItemStock() ");

                #region ItemCode Validation
                if (ItemCode == null)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });
                if (ItemCode == string.Empty || ItemCode.ToString().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });
                #endregion

                System.Data.DataTable dtStock = new();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT A.WhsCode, B.WhsName, B.Locked, 
	                   CAST(( ISNULL(A.OnHand,0) + ISNULL(A.OnOrder,0) ) - ISNULL(A.IsCommited,0) as numeric(19,3)) AvailQty,
	                   CAST(ISNULL(A.OnHand, 0) as numeric(19,3)) InStock, C.Location
                FROM OITW A INNER JOIN OWHS B ON A.WhsCode = B.WhsCode
                INNER JOIN OLCT C ON C.Code = B.Location
                WHERE ItemCode = @itemCode
                ";

                _logger.LogInformation(" CommonsController : GetItemStock() Query : {q} ", _Query.ToString());
                await SAPcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", ItemCode);
                oAdptr.Fill(dtStock);
                SAPcon.Close();

                if (dtStock.Rows.Count > 0)
                {
                    List<ItemStock> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtStock);
                    obj = JsonConvert.DeserializeObject<List<ItemStock>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item does not exist : " + ItemCode });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetItemStock Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetItemStock() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Shift")]
        public ActionResult<IEnumerable<Shift>> GetShift()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetShift() ");

                System.Data.DataTable dtData = new();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT ShiftId ID, ShiftName Name FROM " + Global.QIT_DB + @".dbo.QIT_Shift_Master ORDER BY Id ";

                _logger.LogInformation(" CommonsController : GetShift() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<Shift> obj = new();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<Shift>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("CommonsController : GetShift Error : " + ex.ToString());
                _logger.LogError(" Error in CommonsController : GetShift() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

    }
}
