namespace ST_Production.Models
{
    public class CloseProduction
    {
    }

    public class ProductionOrderHelpforClose
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string Type { get; set; }
        public string TypeName { get; set; }
        public string Status { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string PostingDate { get; set; }
        public string StartDate { get; set; }
        public string DueDate { get; set; }
        public string PlannedQty { get; set; }
        public string CompletedQty { get; set; }
        public string Project { get; set; }
        public string CardCode { get; set; }
        public string WhsCode { get; set; }
        public string DistRule { get; set; }
        public string UomCode { get; set; }
        public string Remark { get; set; }
        public string Shift { get; set; }
        public string ShiftName { get; set; }
        public string ActWgt { get; set; }
        public int ProId { get; set; }
        public string LastReceiptDate { get; set; }

    }

    public class ProductionOrderItemHelpforClose
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseQty { get; set; }
        public string BaseRatio { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public string WhsCode { get; set; }
        public string WhsQty { get; set; }
        public double ItemStock { get; set; }
        public string UomCode { get; set; }
        public string DistRule { get; set; }
        public string Project { get; set; }
        public string IssueType { get; set; }
        public int ProId { get; set; }
    }

    public class UpdatePlannedQty
    {
        public int BranchId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string Quantity { get; set; }
    }
}
