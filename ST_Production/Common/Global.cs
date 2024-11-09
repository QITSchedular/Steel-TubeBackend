using SAPbobsCOM;

namespace ST_Production.Common
{
    public class Global
    {
        public Company oComp = new();
        public string gServer = String.Empty;
        public string gSqlVersion = string.Empty;
        public string gCompanyDB = string.Empty;
        public string gLicenseServer = string.Empty;
        public string gSAPUserName = string.Empty;
        public string gSAPPassword = string.Empty;
        public string gDBUserName = string.Empty;
        public string gDBPassword = string.Empty;
        public static string QIT_DB = string.Empty;
        public static string SAP_DB = string.Empty;

        public static string gLogPath = string.Empty;
        public static string gAllowBranch = "N";
        public static string gItemWhsCode = string.Empty;

        //public bool ConnectSAP(out string p_ErrorMsg)
        //{
        //    int intErrorCode = 0;
        //    string strError = string.Empty;
        //    p_ErrorMsg = string.Empty;

        //    try
        //    { 
        //        oComp.Server = gServer;

        //        if (gSqlVersion == "2008")
        //            oComp.DbServerType = BoDataServerTypes.dst_MSSQL2008;
        //        else if (gSqlVersion == "2012")
        //            oComp.DbServerType = BoDataServerTypes.dst_MSSQL2012;
        //        else if (gSqlVersion == "2014")
        //            oComp.DbServerType = BoDataServerTypes.dst_MSSQL2014;
        //        else if (gSqlVersion == "2016")
        //            oComp.DbServerType = BoDataServerTypes.dst_MSSQL2016;
        //        else if (gSqlVersion == "2017")
        //            oComp.DbServerType = BoDataServerTypes.dst_MSSQL2017;
        //        else if (gSqlVersion == "2019")
        //            oComp.DbServerType = BoDataServerTypes.dst_MSSQL2019;


        //        oComp.CompanyDB = gCompanyDB;
        //        oComp.LicenseServer = gLicenseServer;
        //        oComp.UserName = gSAPUserName;
        //        oComp.Password = gSAPPassword;
        //        oComp.UseTrusted = false;
        //        oComp.DbUserName = gDBUserName;
        //        oComp.DbPassword = gDBPassword;
        //        if (oComp.Connect() == 0)
        //            return true;
        //        oComp.GetLastError(out intErrorCode, out strError);
        //        p_ErrorMsg = "Error Code : " + intErrorCode + Environment.NewLine + "Error : " + strError;
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        p_ErrorMsg = "Error : " + ex;
        //        return false;
        //    }
        //}

        public async Task<(bool success, string errorMsg)> ConnectSAP()
        {
            int intErrorCode = 0;
            string strError = string.Empty;
            string p_ErrorMsg = string.Empty;

            try
            {
                oComp.Server = gServer;

                if (gSqlVersion == "2008")
                    oComp.DbServerType = BoDataServerTypes.dst_MSSQL2008;
                else if (gSqlVersion == "2012")
                    oComp.DbServerType = BoDataServerTypes.dst_MSSQL2012;
                else if (gSqlVersion == "2014")
                    oComp.DbServerType = BoDataServerTypes.dst_MSSQL2014;
                else if (gSqlVersion == "2016")
                    oComp.DbServerType = BoDataServerTypes.dst_MSSQL2016;
                else if (gSqlVersion == "2017")
                    oComp.DbServerType = BoDataServerTypes.dst_MSSQL2017;
                else if (gSqlVersion == "2019")
                    oComp.DbServerType = BoDataServerTypes.dst_MSSQL2019;

                oComp.CompanyDB = gCompanyDB;
                oComp.LicenseServer = gLicenseServer;
                oComp.SLDServer = gLicenseServer;
                oComp.UserName = gSAPUserName;
                oComp.Password = gSAPPassword;
                oComp.UseTrusted = false;
                oComp.DbUserName = gDBUserName;
                oComp.DbPassword = gDBPassword;
                if (await Task.Run(() => oComp.Connect()) == 0)
                    return (true, string.Empty);
                oComp.GetLastError(out intErrorCode, out strError);
                p_ErrorMsg = "Error Code : " + intErrorCode + Environment.NewLine + "Error : " + strError;
                return (false, p_ErrorMsg);
            }
            catch (Exception ex)
            {
                p_ErrorMsg = "Error : " + ex;
                return (false, p_ErrorMsg);
            }
        }

        public void WriteLog(string LogDetails)
        {
            try
            {
                if (!Directory.Exists(Global.gLogPath))
                    return;
                string path = Global.gLogPath + "\\ErrorLog_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                bool flag = File.Exists(path);
                using (StreamWriter streamWriter1 = new StreamWriter((Stream)File.Open(path, FileMode.Append)))
                {
                    StreamWriter streamWriter2 = streamWriter1;
                    string str2;
                    if (!flag)
                        str2 = DateTime.Now.ToString() + " --- Log Start --- " + "\n" + LogDetails + " ";
                    else
                        str2 = DateTime.Now.ToString() + " - " + LogDetails + " ";
                    streamWriter2.WriteLine(str2);
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }
    }
}
