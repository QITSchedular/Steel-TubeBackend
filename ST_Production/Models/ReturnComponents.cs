namespace ST_Production.Models
{
    public class ReturnComponents
    {
    }

    public class ProductionOrderHelpforReturn
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string PlannedQty { get; set; }
        public string CompletedQty { get; set; }
        public string Project { get; set; }
        public string WhsCode { get; set; }
        public string DistRule { get; set; }
        public string Status { get; set; }
        public string UomCode { get; set; }
        public string Remark { get; set; }
        public int ProId { get; set; }

    }

    public class ProductionOrderItemHelpforReturn
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UomCode { get; set; }
        public string UomName { get; set; }
        public string Quantity { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public string WhsQty { get; set; }
        public string InStock { get; set; }
        public string WhsCode { get; set; }
        public string ItemMngBy { get; set; }
        public int ProId { get; set; }
        public string ItemMngByName { get; set; }
        public string ItemsType { get; set; }
    }


    public class SaveReturnComponents
    {
        public int BranchId { get; set; }
        public int Series { get; set; }
        public int ProOrdDocEntry { get; set; }
        public string PostingDate { get; set; }
        public string RefNo { get; set; }
        public string LoginUser { get; set; }
        public string Remark { get; set; }
        public List<SaveReturnComponentsDetail> itemDetail { get; set; }
    }

    public class SaveReturnComponentsDetail
    {
        public int BaseLineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Qty { get; set; }
        public string UoMCode { get; set; }
        public string WhsCode { get; set; }
        public string ItemMngBy { get; set; }
        public List<SaveReturnComponentsBatchSerial> itembatchSerialDet { get; set; }
    }

    public class SaveReturnComponentsBatchSerial
    {
        public string BatchSerialNo { get; set; }
        public string SelectedQty { get; set; }
    }

}
