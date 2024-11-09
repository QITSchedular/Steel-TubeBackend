namespace ST_Production.Models
{
    public class ProductionIssue
    {
    }

    #region Fill Data on page load
    public class ProductionOrderHelpforIssue
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
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

    }

    public class ProductionOrderItemHelpforIssue
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseQty { get; set; } 
        public string WhsCode { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public string PendingQty { get; set; }
        public string WhsQty { get; set; }
        public string UsedQty { get; set; }
        public double ItemStock { get; set; }
        public string UomCode { get; set; }
        public string DistRule { get; set; }
        public string Project { get; set; }
    }

    #endregion

    #region Save Draft Production Issue

    public class SaveDraftProductionIssue
    {
        public int BranchId { get; set; }
        public int Series { get; set; }
        public int DocNum { get; set; }
        public int ProOrdDocEntry { get; set; }
        public string PostingDate { get; set; }
        public string RefNo { get; set; }
        public string LoginUser { get; set; }
        public string Remark { get; set; }
        public List<SaveDraftProductionIssueDetail> issDetail { get; set; }
    }

    public class SaveDraftProductionIssueDetail
    {
        public int BaseLineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Qty { get; set; }
        public string UoMCode { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public string DistRule { get; set; }
    }

    #endregion


    public class DisplayProductionIssue
    {
        public int IssId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string DocNum { get; set; }
        public string State { get; set; }
        public string PostingDate { get; set; }
        public string Ref2 { get; set; }
        public string Remark { get; set; }
        public string BatchSelected { get; set; }
    }

    public class IssueHeader
    {
        public int IssId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string PeriodIndicator { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string State { get; set; }
        public string PostingDate { get; set; }
        public string Ref2 { get; set; }
        public string Remark { get; set; }
        public string Reason { get; set; }
        public string ItemsType { get; set; }
        public List<IssueDetails> issDetail { get; set; }
    }

    public class IssueDetails
    {
        public int IssDetId { get; set; }
        public int LineNum { get; set; }
        public int BaseLine { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double Qty { get; set; }
        public double BaseQty { get; set; }
        public double PlannedQty { get; set; }
        public double IssuedQty { get; set; }
        public double PendingQty { get; set; }
        public double WhsQty { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public string DistRule { get; set; }
        public string ItemMngBy { get; set; }
        public string ItemMngByName { get; set; }

    }

    public class VerifyDraftProductionIssue
    {
        public int BranchId { get; set; }
        public int IssId { get; set; }
        public string Action { get; set; }
        public string ActionRemark { get; set; }
        public string LoginUser { get; set; }
    }

    public class SaveProductionIssue
    {
        public int BranchId { get; set; }
        public int IssId { get; set; }
        public List<SaveProductionIssueDetail> issDetails { get; set; }
    }

    public class SaveProductionIssueDetail
    {
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public int BaseLine { get; set; }
        public double TotalQty { get; set; }
        public string ItemMngBy { get; set; }
        public List<SaveProductionIssueBatchSerial> batchSerialDet { get; set; }
    }


    public class SaveProductionIssueBatchSerial
    {
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public int FromBinAbsEntry { get; set; }
        public int ToBinAbsEntry { get; set; }
        public string BatchSerialNo { get; set; }
        public double SelectedQty { get; set; }
    }

}
