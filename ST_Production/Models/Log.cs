namespace ST_Production.Models
{
    public class SaveLog
    {
        public int BranchID { get; set; }
        public string Module { get; set; }
        public string ControllerName { get; set; }
        public string MethodName { get; set; }
        public string LogLevel { get; set; }
        public string LogMessage { get; set; }
        public string APIUrl { get; set; }
        public string jsonPayload { get; set; }
        public string LoginUser { get; set; }
        public string FormType { get; set; }
        public string ObjectType { get; set;}
        public string DocNum { get; set; }
        public string ModuleTransId { get; set; }
        public string ProOrdDocNum { get; set; }

    }

    public class LogModules
    {
        public string Module { get; set; }
    }


    public class LogDetails
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Module { get; set; }
        public string UserName { get; set; }
        public string AdminOnly { get; set; }
        public string LogLevel { get; set; }

    }

    public class LogReport
    {
        public int Id { get; set; }
        public string ModuleTransId { get; set; }
        public string Module { get; set; }
        public string SubModule { get; set; }
        public string Status { get; set; }
        public string LogMessage { get; set; }
        public string UserName { get; set; }
        public string LogDate { get; set; }
        public string ProductNo { get; set; }
        public string ProductName { get; set; }
        public string ProOrdDocNum { get; set; }
        public string ProIssueDocNum { get; set; }
        public string ProReceiptDocNum { get; set; }
        public string ITDocNum { get; set; }
        public string ReturnDocNum { get; set; }
        public string ProOrdSeries { get; set; }
        public string ProIssueSeries { get; set; }
        public string ProReceiptSeries { get; set; }
        public string ITSeries { get; set; }
        public string ProOrdApprover { get; set; }
        public string ProIssueApprover { get; set; }
        public string ProReceiptApprover { get; set; }
        public string ITApprover { get; set; }
        public string APIUrl { get; set; }
        public string jsonPayload { get; set; }
        public string RefProOrdDocNum { get; set; }
    }

}
