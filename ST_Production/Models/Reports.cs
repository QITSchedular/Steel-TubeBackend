namespace ST_Production.Models
{
    public class Reports
    {
    }

    public class GateInDetails
    {
        public int BranchID { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int PODocEntry { get; set; }
    }

    public class GateInDetailsReport
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int GateInNo { get; set; }
        public DateTime GateInDate { get; set; }
        public double GateInQty { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string VehicleNo { get; set; }
        public string TransporterCode { get; set; }
    }

    public class ItemWiseQRWiseStock
    {
        public int BranchID { get; set; }
        public string ItemCode { get; set; }
        public string Project { get; set; }
    }

    public class ItemWiseQRWiseStockReport
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string QRCodeID { get; set; }
        public string Project { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BinCode { get; set; }
        public string Stock { get; set; }
    }


    public class QRWiseStock
    {
        public int BranchID { get; set; }
        public string ItemCode { get; set; }
        public string Project { get; set; }
        public int? PODocEntry { get; set; }
        public int? PRODocEntry { get; set; }
    }


    public class ProList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }

    }


    public class ProItems
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }

    }

    public class PoIList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }

    }

    public class GRNDetailsReport
    {
        public int PO_DocEntry { get; set; }
        public int PO_DocNum { get; set; }
        public DateTime PO_DocDate { get; set; }
        public int GateInNo { get; set; }
        public DateTime GateIn_Date { get; set; }
        public double GateInQty { get; set; }
        public double PO_Qty { get; set; }
        public double? GRN_Qty { get; set; }
        public int? GRN_DocNum { get; set; }
        public DateTime? GRN_DocDate { get; set; }
        public string PO_WhsCode { get; set; }
        public string? GRN_WhsCode { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string? Vendor_Code { get; set; }
        public string? Vendor_Name { get; set; }

    }

    public class QRScanReportInput
    {
        public int BranchID { get; set; }
        public string QRCodeID { get; set; }
    }

    public class QRScanReportOutput
    {
        public string QRCodeID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BatchSerialNo { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BinCode { get; set; }
        public string Project { get; set; }
        public double BatchQty { get; set; }
        public double Stock { get; set; }
    }

    public class QcDetailsReport
    {
        public int GateInNo { get; set; }
        public DateTime GateIn_Date { get; set; }
        public int GRN_DocNum { get; set; }
        public int GRN_DocEntry { get; set; }
        //public double GateInQty { get; set; }
        public DateTime GRN_DocDate { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string QARequired { get; set; }
        public DateTime? QC_Date { get; set; }
        public double RecQty { get; set; }
        public double GRN_Qty { get; set; }
        public double? QC_Qty { get; set; }
        public string GRN_WhsCode { get; set; }
        public string? QC_FromWhs { get; set; }
        public string? QC_ToWhs { get; set; }
        public int PO_DocNum { get; set; }
        public int PO_DocEntry { get; set; }
        public DateTime PO_DocDate { get; set; }
        public string? Vendor_Code { get; set; }
        public string? Vendor_Name { get; set; }
    }

    public class reportFilter
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ProductNo { get; set; }
    }

    public class reportFilterNos
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int ProOrdDocEntry { get; set; }
    }

    public class itFilter
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
    }

    public class ProductNos
    {
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
    }

    public class ProductionNos
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
    }

    public class rptProductionOrder
    {
        public int ProId { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
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
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string Remark { get; set; }

    }

    public class rptProductionOrderDetail
    {
        public int ProId { get; set; }
        public int ProDetId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseQty { get; set; }
        public string BaseRatio { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public double AvailQty { get; set; }
        public string UoM { get; set; }
        public string IssueType { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public double InStock { get; set; }
    }


    public class rptProductionIssue
    {
        public int IssId { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string State { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string Remark { get; set; }
    }

    public class rptProductionIssueDetail
    {
        public int IssId { get; set; }
        public int IssDetId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int BaseLine { get; set; }
        public int LineNum { get; set; }
        public string Qty { get; set; }
        public string BaseQty { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public string WhsQty { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public string DistRule { get; set; }
    }


    public class rptProductionReceipt
    {
        public int RecId { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string State { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string PlannedQty { get; set; }
        public string CompletedQty { get; set; }
        public string Remark { get; set; }
    }

    public class rptProductionReceiptDetail
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
    }

    public class rptInventoryTransfer
    {
        public int InvId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string State { get; set; }
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string PostingDate { get; set; }
        public string DocDate { get; set; }
        public string Remark { get; set; }
    }

    public class rptInventoryTransferDetail
    {
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public int InvId { get; set; }
        public int InvDetId { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public string Qty { get; set; }
        public string UoM { get; set; }
        public string AvailQty { get; set; }
        public string InStock { get; set; }
    }

    public class rptProductionOrderV2
    {
        public int ProId { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string State { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string HeaderPlannedQty { get; set; }
        public string HeaderCompletedQty { get; set; }
        public string HeaderProject { get; set; }
        public string HeaderWhsCode { get; set; }
        public string HeaderDistRule { get; set; }
        public string Shift { get; set; }
        public string HeaderUoM { get; set; }
        public string Status { get; set; }
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string Remark { get; set; }
        public int ProDetId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseQty { get; set; }
        public string BaseRatio { get; set; }
        public string DetailPlannedQty { get; set; }
        public string DetailIssuedQty { get; set; }
        public double AvailQty { get; set; }
        public string DetailUoM { get; set; }
        public string IssueType { get; set; }
        public string DetailWhsCode { get; set; }
        public string DetailProject { get; set; }
        public double InStock { get; set; }

    }


    public class rptProductionIssueV2
    {
        public int IssId { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string State { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string Remark { get; set; }
        public int IssDetId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int BaseLine { get; set; }
        public int LineNum { get; set; }
        public string Qty { get; set; }
        public string BaseQty { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public string WhsQty { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public string DistRule { get; set; }

    }

    public class rptProductionReceiptV2
    {
        public int RecId { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string State { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string HeaderPlannedQty { get; set; }
        public string HeaderCompletedQty { get; set; }
        public string Remark { get; set; }
        public int RecDetId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseLine { get; set; }
        public string LineNum { get; set; }
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

    }


    public class rptInventoryTransferV2
    {
        public int InvId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string State { get; set; }
        public string HeaderFromWhs { get; set; }
        public string HeaderToWhs { get; set; }
        public string DraftUser { get; set; }
        public string ActionUser { get; set; }
        public string PostingDate { get; set; }
        public string DocDate { get; set; }
        public string Remark { get; set; }
        public int InvDetId { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string DetailFromWhs { get; set; }
        public string DetailToWhs { get; set; }
        public string Qty { get; set; }
        public string UoM { get; set; }
        public string AvailQty { get; set; }
        public string InStock { get; set; }
    }


    public class varProductionOrderHeader
    {
        public string Type { get; set; }
        public string Status { get; set; }
        public string SeriesName { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string OrderDate { get; set; }
        public string StartDate { get; set; }
        public string DueDate { get; set; }
        public string CardCode { get; set; }
        public string OcrCode { get; set; }
        public string OcrName { get; set; }
        public string Project { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string PlannedQty { get; set; }
        public string CompletedQty { get; set; }
        public string Variance { get; set; }
        public string Warehouse { get; set; }
        public string Uom { get; set; }
        public string Comments { get; set; }
    }


    public class varProductionOrderDetail
    {
        public int DocEntry { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaseQty { get; set; }
        public string BaseQtyBOM { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public string UomCode { get; set; }
        public string wareHouse { get; set; }
        public string IssueType { get; set; }

    }


    public class varProductionIssueHeader
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string Ref2 { get; set; }
        public string Comments { get; set; }

    }


    public class varProductionIssueDetail
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Quantity { get; set; }
        public string WhsCode { get; set; }
        public string UomCode { get; set; }

    }


    public class varProductionReceiptHeader
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string SeriesName { get; set; }
        public string PostingDate { get; set; }
        public string Ref2 { get; set; }
        public string Comments { get; set; }

    }

    public class varProductionReceiptDetail
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Quantity { get; set; }
        public string WhsCode { get; set; }
        public string UomCode { get; set; }

    }
}
