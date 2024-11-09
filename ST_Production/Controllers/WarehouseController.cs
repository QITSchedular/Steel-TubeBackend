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
    public class WarehouseController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;

        private string _Query = string.Empty;
        private SqlConnection SAPcon;
        private SqlDataAdapter oAdptr;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<WarehouseController> _logger;

        public WarehouseController(IConfiguration configuration, ILogger<WarehouseController> logger)
        {
            objGlobal ??= new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
                _connection = Configuration["connectApp:ConnString"];
                _QIT_connection = Configuration["connectApp:QITConnString"];

                Global.QIT_DB = "[" +  Configuration["QITDB"] + "]";
                Global.SAP_DB = "[" + Configuration["CompanyDB"] + "]";
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog(" Error in WarehouseController :: " + ex.ToString());
                _logger.LogError(" Error in WarehouseController :: {ex}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouse(string Locked)
        {
            try
            {
                _logger.LogInformation(" Calling WarehouseController : GetWarehouse() ");

                DataTable dtData = new();;
                SAPcon = new SqlConnection(_connection);

                string _where = string.Empty;
                if (Locked.ToUpper() == "Y" || Locked.ToUpper() == "N")
                    _where = " AND A.Locked = @locked ";
                else
                {
                    if (Locked.ToUpper() != "A")
                        return BadRequest(new { StatusCode = "200", StatusMsg = "Locked values should be Y/N/A" });
                }

                _Query = @" SELECT A.WhsCode, A.WhsName, A.Locked, A.BinActivat, B.Code LocationCode, B.Location 
                            FROM OWHS A INNER JOIN OLCT B ON B.Code = A.Location    
                            WHERE 1=1 AND Inactive = 'N' " + _where +
                         @"  ORDER BY WhsCode ";
                _logger.LogInformation(" WarehouseController : GetWarehouse() Query : {q} ", _Query.ToString());
                await SAPcon.OpenAsync();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                if (Locked.ToUpper() == "Y" || Locked.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@locked", Locked);
                oAdptr.Fill(dtData);
                SAPcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<Warehouse> obj = new List<Warehouse>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<Warehouse>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return Ok(new { StatusCode = "200", StatusMsg = "Data not found" });
                }
            }
            catch (Exception ex)
            {
                objGlobal.WriteLog("WarehouseController : GetWarehouse Error : " + ex.ToString());
                _logger.LogError(" Error in WarehouseController : GetWarehouse() :: {ex}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

    }
}
