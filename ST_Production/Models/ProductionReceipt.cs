namespace ST_Production.Models
{
    public class ProductionReceipt
    {
    }

    public class ProductionOrderHelpforReceipt
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

    public class DisplayProductionDetail
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string BaseLine { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string TransType { get; set; }
        public string TransTypeName { get; set; }
        public string Qty { get; set; }
        public string WhsCode { get; set; }
        public string WhsQty { get; set; }
        public string TotalQty { get; set; }
        public string PlannedQty { get; set; }
        public string CompletedQty { get; set; }
        public string BaseQty { get; set; }
        public string UsedQty { get; set; }
        public string UomCode { get; set; }
        public string DistRule { get; set; }
        public string Project { get; set; }
        public string IssueType { get; set; }
        public string PendingQty { get; set; }

    }


    public class SaveDraftProductionReceipt
    {
        public int BranchId { get; set; }
        public int Series { get; set; }
        public int DocNum { get; set; }
        public int ProOrdDocEntry { get; set; }
        public string PostingDate { get; set; }
        public string RefNo { get; set; }
        public string LoginUser { get; set; }
        public string Remark { get; set; }
        public List<SaveDraftProductionReceiptDetail> recDetail { get; set; }

    }


    public class SaveDraftProductionReceiptDetail
    {
        public string BaseLineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Qty { get; set; }
        public string TransType { get; set; }
        public string UoMCode { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public string DistRule { get; set; }
    }


    public class DisplayProductionReceipt
    {
        public int RecId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public double PlannedQty { get; set; }
        public double CompletedQty { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string DocNum { get; set; }
        public string State { get; set; }
        public string PostingDate { get; set; }
        public string Ref2 { get; set; }
        public string Remark { get; set; }
        public string BatchSelected { get; set; }
    }


    public class ReceiptHeader
    {
        public int RecId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string PeriodIndicator { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string State { get; set; }
        public string PostingDate { get; set; }
        public string Ref2 { get; set; }

        public string Remark { get; set; }
        public string Reason { get; set; }
        public List<ReceiptDetails> recDetail { get; set; }

    }


    public class ReceiptDetails
    {
        public int RecId { get; set; }
        public int RecDetId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseLine { get; set; }
        public int LineNum { get; set; }
        public string TransType { get; set; }
        public string TransTypeName { get; set; }
        public string Qty { get; set; }
        public string BaseQty { get; set; }
        public string PlannedQty { get; set; }
        public string CompletedQty { get; set; }
        public string WhsQty { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public string DistRule { get; set; }
        public string ItemMngBy { get; set; }
        public string ItemMngByName { get; set; }
        public string IssueType { get; set; }

    }


    public class VerifyDraftProductionReceipt
    {
        public int BranchId { get; set; }
        public int RecId { get; set; }
        public string Action { get; set; }
        public string ActionRemark { get; set; }
        public string LoginUser { get; set; }
    }


    public class SaveProductionReceipt
    {
        public int BranchId { get; set; }
        public int RecId { get; set; }
        public string ProductNo { get; set; }
        public List<SaveProductionReceiptDetail> recDetails { get; set; }

    }

    public class SaveProductionReceiptDetail
    {
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public string BaseLine { get; set; }
        public double TotalQty { get; set; }
        public string ItemMngBy { get; set; }
        public List<SaveProductionReceiptBatchSerial> batchSerialDet { get; set; }
    }

    public class SaveProductionReceiptBatchSerial
    {
        public string BatchSerialNo { get; set; }
        public double Qty { get; set; }
    }

}
