namespace ST_Production.Models
{
    public class ProductionOrder
    {
    }

    public class ProductionOrderType
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class ProductionOrderStatus
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class ProductionOrderDocNo
    {
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public int NextNumber { get; set; }
    }

    public class ProductList
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double OnHand { get; set; }
    }

    public class BOMHeader
    {
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string HeaderPlannedQty { get; set; }
        public string HeaderUoM { get; set; }
        public string HeaderWhsCode { get; set; }
        public string HeaderProject { get; set; }
        public List<BOMDetail> BOMDet { get; set; }

    }

    public class BOMDetail
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseQty { get; set; }
        public string BaseQtyBOM { get; set; }
        public string BaseRatio { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public string AvailableQty { get; set; }
        public string UoM { get; set; }
        public string IssueMethod { get; set; }
        public string IssueMethodName { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public double InStock { get; set; }
        public double QtyInWhs { get; set; }
    }

    public class SpecialItemHeaderData
    {
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string PlannedQty { get; set; }
        public string UoM { get; set; }
        public string WhsCode { get; set; }

    }

    public class SpecialItemDetailHelp
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int BaseQty { get; set; }
        public int BaseRatio { get; set; }
        public int PlannedQty { get; set; }
        public string InStock { get; set; }
        public string UoM { get; set; }
        public string WhsCode { get; set; }
        public string AvailableQty { get; set; }
        public string QtyInWhs { get; set; }
        public string IssueMethod { get; set; }
        public string IssueMethodName { get; set; }
    }

    public class ItemStock
    {
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public double AvailQty { get; set; }
        public double InStock { get; set; }
        public string Location { get; set; }
    }


    public class SaveDraftProductionOrder
    {
        public int BranchId { get; set; }
        public string Status { get; set; }
        public int Series { get; set; }
        public string Type { get; set; }
        public int DocNum { get; set; }
        public string ProductNo { get; set; }
        public string PlannedQty { get; set; }
        public string UoM { get; set; }
        public string WhsCode { get; set; }
        public string OrderDate { get; set; } // validation pending
        public string StartDate { get; set; } // validation pending
        public string DueDate { get; set; } // validation pending
        public string DistRule { get; set; }
        public string Project { get; set; }
        public string Customer { get; set; }
        public string Shift { get; set; }
        public string ActWgt { get; set; }
        public int Priority { get; set; }
        public string LoginUser { get; set; }
        public string Remark { get; set; }
        public List<SaveDraftProDetail> proDetail { get; set; }
    }

    public class SaveDraftProDetail
    {
        public string ItemCode { get; set; }
        public string BaseQtyBOM { get; set; }
        public string BaseQty { get; set; }
        public string BaseRatio { get; set; }
        public string PlannedQty { get; set; }
        public string UoMCode { get; set; }
        public string IssueType { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
    }

    public class DisplayProOrd
    {
        public int ProId { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string Type { get; set; }
        public string TypeName { get; set; }
        public string State { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string PlannedQty { get; set; }
        public string CompletedQty { get; set; }
        public string Project { get; set; }
        public string WhsCode { get; set; }
        public string DistRule { get; set; }
        public string Shift { get; set; }
        public string UoM { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }

    }

    public class ProHeader
    {
        public int ProId { get; set; }
        public string Status { get; set; }
        public string State { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string HeaderPlannedQty { get; set; }
        public string ProductNo { get; set; }
        public string Remark { get; set; }
        public string Reason { get; set; }
        public string ProductName { get; set; }
        public string Type { get; set; }
        public string HeaderUoM { get; set; }
        public string PostingDate { get; set; }
        public string StartDate { get; set; }
        public string DueDate { get; set; }
        public string Customer { get; set; }
        public string HeaderWhsCode { get; set; }
        public string DistRule { get; set; }
        public string HeaderProject { get; set; }
        public string ActWgt { get; set; }
        public string Priority { get; set; }
        public string ShiftId { get; set; }
        public string ShiftName { get; set; }
        public List<ProDetail> proDetail { get; set; }
    }

    public class ProDetail
    {
        public string LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseQtyBOM { get; set; }
        public string BaseQty { get; set; }
        public string DiffBaseQty { get; set; }
        public string BaseRatio { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public double AvailQty { get; set; }
        public string DetailUoM { get; set; }
        public string IssueType { get; set; }
        public string DetailWhsCode { get; set; }
        public string DetailProject { get; set; }
        public double InStock { get; set; }
        public double WhsQty { get; set; }
    }


    public class VerifyDraftProductionOrder
    {
        public int BranchId { get; set; }
        public int ProId { get; set; }
        public string Action { get; set; }
        public string ActionRemark { get; set; }
        public string LoginUser { get; set; }
    }

}
