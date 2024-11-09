namespace ST_Production.Models
{

    public class Common
    {
    }

    public class ApiResponse
    {
        public string ResCode { get; set; }
        public string ResMsg { get; set; }
    }

    public class ApiResponses_Inv
    {
        public string StatusCode { get; set; }
        public string IsSaved { get; set; }
        public string StatusMsg { get; set; }
    }

    public class QRMngBy
    {
        public string QRMngById { get; set; }
        public string QRMngByName { get; set; }
    }

    public class Branch
    {
        public int BPLId { get; set; }
        public string BPLName { get; set; }
    }

    public class PeriodIndicator
    {
        public string Indicator { get; set; }
    }

    public class SeriesCls
    {
        public int Series { get; set; }
        public string SeriesName { get; set; }
    }

    public class Project
    {
        public string PrjCode { get; set; }
        public string PrjName { get; set; }
    }

    public class DistRule
    {
        public int DimCode { get; set; }
        public string DimName { get; set; }
        public string DimDesc { get; set; }
        public string OcrCode { get; set; }
        public string OcrName { get; set; }
    }

    public class Customer
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public double Balance { get; set; }
        public string CardType { get; set; }
        public string ContactPerson { get; set; }
    }

    public class BinLocation
    {
        public int AbsEntry { get; set; }
        public string BinCode { get; set; }
    }


    public class PriceList
    {
        public int ListNum { get; set; }
        public string ListName { get; set; }
    }

    public class SalesEmployee
    {
        public int SlpCode { get; set; }
        public string SlpName { get; set; }
    }

    public class Shift
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

}
