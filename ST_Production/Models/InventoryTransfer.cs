namespace ST_Production.Models
{
    public class InventoryTransfer
    {
    }

    public class ProductionOrderHelp
    {
        public int ProId { get; set; }
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
        public string UoM { get; set; }
        public string DraftRemark { get; set; }
    }

    public class SaveDraftInventoryTransfer
    {
        public int BranchId { get; set; }
        public int ProOrdDocEntry { get; set; } = 0;
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public string PostingDate { get; set; }
        public string DocDate { get; set; }
        public int PriceListId { get; set; }
        public string SlpCode { get; set; }
        public string ShipTo { get; set; }
        public string LoginUser { get; set; }
        public string Remark { get; set; }
        public List<SaveDraftITDetail> itDetail { get; set; }
    }

    public class SaveDraftITDetail
    {
        public string ItemCode { get; set; }
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public string Qty { get; set; }
        public string UoM { get; set; }

    }


    public class DisplayInventoryTransfer
    {
        public int InvId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int DocNum { get; set; }
        public string State { get; set; }
        public string SeriesName { get; set; }
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public string PostingDate { get; set; }
        public string DocDate { get; set; }
        public string ListName { get; set; }
        public string SlpName { get; set; }
        public string ShipTo { get; set; }
        public string Remark { get; set; }
        public string BatchSelected { get; set; }
    }

    public class ITHeader
    {
        public int InvId { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string State { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string PeriodIndicator { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public string PostingDate { get; set; }
        public string DocDate { get; set; }
        public int PriceListId { get; set; }
        public string PriceListName { get; set; }
        public int SlpCode { get; set; }
        public string SlpName { get; set; }
        public string ShipTo { get; set; }
        public string Remark { get; set; }
        public string Reason { get; set; }
        public string ItemsType { get; set; }
        public List<ITDetail> itDetails { get; set; }

    }

    public class ITDetail
    {
        public int InvDetId { get; set; }
        public int LineNum { get; set; }
        public string DetailItemCode { get; set; }
        public string DetailItemName { get; set; }
        public string DetailFromWhs { get; set; }
        public string DetailToWhs { get; set; }
        public string Qty { get; set; }
        public string DetailUoM { get; set; }
        public double AvailQty { get; set; }
        public double InStock { get; set; }
        public string ItemMngBy { get; set; }
        public string ItemMngByName { get; set; }
    }

    public class VerifyDraftIT
    {
        public int BranchId { get; set; }
        public int InvId { get; set; }
        public string Action { get; set; }
        public string ActionRemark { get; set; }
        public string LoginUser { get; set; }
    }

    public class BatchSerialItemDetails
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string Qty { get; set; }
    }


    public class BatchSerialData
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int SysNumber { get; set; }
        public string DistNumber { get; set; }
        public string LotNumber { get; set; }
        public double AvailQty { get; set; }

    }


    public class SaveInventoryTransfer
    {
        public int BranchId { get; set; }
        public int InvId { get; set; }
        public List<SaveInventoryTransferDetail> itDetails { get; set; }
    }


    public class SaveInventoryTransferDetail
    {
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public double TotalQty { get; set; }
        public string ItemMngBy { get; set; }
        public List<SaveInventoryTransferBatchSerial> batchSerialDet { get; set; }
    }

    public class SaveInventoryTransferBatchSerial
    {
        public string FromWhs { get; set; }
        public string ToWhs { get; set; }
        public int FromBinAbsEntry { get; set; }
        public int ToBinAbsEntry { get; set; }
        public string BatchSerialNo { get; set; }
        public double SelectedQty { get; set; }
    }
}
