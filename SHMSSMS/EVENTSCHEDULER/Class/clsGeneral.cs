using System;
using System.IO;
using System.Net;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace EVENTSCHEDULER
{
    class clsGeneral
    {
        public void GetConnectionDetails()
        {
            try
            {
                clsPublicVariables.ServerName1 = System.Configuration.ConfigurationManager.AppSettings["SERVER"];
                clsPublicVariables.DatabaseName1 = System.Configuration.ConfigurationManager.AppSettings["DATABASE"];
                clsPublicVariables.UserName1 = System.Configuration.ConfigurationManager.AppSettings["DBUSERID"];
                clsPublicVariables.Password1 = System.Configuration.ConfigurationManager.AppSettings["DBPASSWORD"];
                clsPublicVariables.ActualFilePath = clsPublicVariables.AppPath + "\\SHMSSMS.exe";
                clsPublicVariables.EventVer = System.Diagnostics.FileVersionInfo.GetVersionInfo(clsPublicVariables.ActualFilePath).FileVersion.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in GetConnectionDetails())");
            }
        }

        public double Null2Dbl(object Numbr1)
        {
            Double Amt1;
            try
            {

                if (DBNull.Value == Numbr1)
                {
                    Amt1 = 0;
                }

                else if ((string.IsNullOrEmpty(Numbr1.ToString()) || (Numbr1.ToString().Trim() == "")))
                {
                    Amt1 = 0;
                }
                else
                {
                    Amt1 = (double)Numbr1;
                }

                return (double)Amt1;
            }
            catch (Exception)
            {

                return 0;
            }
        }

        public long Null2lng(object Numbr1)
        {
            long Amt1;
            try
            {

                if (DBNull.Value == Numbr1)
                {
                    Amt1 = 0;
                }

                else if ((string.IsNullOrEmpty(Numbr1.ToString()) || (Numbr1.ToString().Trim() == "")))
                {
                    Amt1 = 0;
                }
                else
                {
                    Amt1 = Convert.ToInt64(Numbr1);
                }

                return Convert.ToInt64(Amt1);
            }
            catch (Exception)
            {

                return 0;
            }
        }

        public DateTime Null2Date(object Dt1)
        {
            DateTime tDt1;
            //DateTime eDt1;
            bool IsEmpDateYn;
            try
            {
                if (DBNull.Value == Dt1)
                {
                    IsEmpDateYn = true;
                }
                else if (Dt1.ToString() == "1/1/1753")
                {
                    IsEmpDateYn = true;
                }
                else if ((Dt1.ToString().Trim() == ""))
                {
                    IsEmpDateYn = true;
                }
                else
                {
                    IsEmpDateYn = false;
                }
                if ((IsEmpDateYn == true))
                {
                    tDt1 = DateTime.MinValue;
                }
                else
                {
                    tDt1 = ((DateTime)(Dt1));
                }

                return tDt1;
            }
            catch (Exception)
            {

                return (DateTime)Dt1;
            }
        }

        public string Null2Str(object Str1)
        {
            string tStr1;
            try
            {
                if (DBNull.Value == (Str1))
                {
                    tStr1 = " ";
                }
                else if (string.IsNullOrEmpty(Str1.ToString()))
                {
                    tStr1 = " ";
                }
                else
                {
                    tStr1 = Str1.ToString();
                }
                return tStr1;
            }
            catch (Exception)
            {
                return Str1.ToString();
            }
        }

        //public string SEND_WEB_REQUEST(string url)
        //{
        //    string result = "";
        //    WebRequest request = null;
        //    HttpWebResponse response = null;
        //    try
        //    {                
        //        request = WebRequest.Create(url);

        //        response = (HttpWebResponse)request.GetResponse();
        //        Stream stream = response.GetResponseStream();
        //        Encoding ec = System.Text.Encoding.GetEncoding("utf-8");
        //        StreamReader reader = new
        //        System.IO.StreamReader(stream, ec);
                
        //        if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "FECUND")
        //        {
        //            string str1;
        //            //str1 = reader.ReadToEnd();
        //            str1 = "1 sms send";
        //            //if (str1.Length > 11)
        //            //{
        //            //    result = str1.Substring(0, 10);
        //            //}
        //            result = str1;

        //        }
        //        else
        //        {
        //            result = reader.ReadToEnd();
        //        }    //Console.WriteLine(result);

        //        reader.Close();
        //        stream.Close();

        //        return result;
        //    }
        //    catch (Exception)
        //    {
        //        //Console.WriteLine(exp.ToString());
        //        return "ERROR,ERROR";
        //    }
        //    finally
        //    {
        //        if (response != null)
        //            response.Close();
        //    }
        //}

        public string SEND_WEB_REQUEST(string url)
        {
            string result = "";
            string xml = "";
            try
            {

                //MessageBox.Show(url);
                
                var request1 = (HttpWebRequest)WebRequest.Create(url);

                var bytes = Encoding.ASCII.GetBytes(xml);

                request1.Method = "POST";

                request1.ContentType = "application/x-www-form-urlencoded";
                request1.ContentLength = bytes.Length;

                using (var stream = request1.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                var response1 = (HttpWebResponse)request1.GetResponse();

                var responseString = new StreamReader(response1.GetResponseStream()).ReadToEnd();

                //MessageBox.Show(responseString);

                if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "FECUND")
                {
                    string str1;
                    //str1 = reader.ReadToEnd();
                    str1 = "1 sms send";
                    //if (str1.Length > 11)
                    //{
                    //    result = str1.Substring(0, 10);
                    //}
                    result = str1;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSGUPSHUP")
                {
                    //result = reader.ReadToEnd();
                    result = responseString;
                }
                else
                {
                    string strresult1;
                    //strresult1 = reader.ReadToEnd();
                    strresult1 = responseString;

                    int ulen1=0;
                    ulen1  =    (strresult1 + "").Length;
                    if (ulen1 >= 150)
                    {
                        result = strresult1.Substring(0, 150);
                    }
                    else
                    {
                        result = strresult1.Substring(0, ulen1);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(exp.ToString());
                return "ERROR,ERROR" + ex.Message.ToString().Substring(0, 150);
                //return "ERROR,ERROR";
            }
            finally
            {
                //if (response != null)
                //    response.Close();
            }
        }

        public bool FillSMSDetails()
        {
            DataTable dtsetting = new DataTable();
            clsMsSqlDbFunction mssql = new clsMsSqlDbFunction();
            try
            {
                string str1 = "SELECT * FROM SHMSSETTING";

                dtsetting = mssql.FillDataTable(str1, "SHMSSETTING");

                if (dtsetting.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtsetting.Rows)
                    {
                        clsPublicVariables.PROPROVIDER = row1["PROPROVIDER"] + "".Trim();
                        clsPublicVariables.PROUSERID = row1["PROUSERID"] + "".Trim();
                        clsPublicVariables.PROPASSWORD = row1["PROPASSWORD"] + "".Trim();
                        clsPublicVariables.TRAPROVIDER = row1["TRAPROVIDER"] + "".Trim();
                        clsPublicVariables.TRAUSERID = row1["TRAUSERID"] + "".Trim();
                        clsPublicVariables.TRAPASSWORD = row1["TRAPASSWORD"] + "".Trim();
                        clsPublicVariables.SMSSIGN = row1["SMSSIGN"] + "".Trim();
                        clsPublicVariables.GENSMTPEMAILADDRESS = (row1["SMTPEMAILADDRESS"]) + "".Trim();
                        clsPublicVariables.GENSMTPEMAILPASSWORD = (row1["SMTPEMAILPASSWORD"]) + "".Trim();
                        clsPublicVariables.GENSMTPADDRESS = (row1["SMTPADDRESS"]) + "".Trim();
                        clsPublicVariables.GENSMTPPORT = (row1["SMTPPORT"]) + "".Trim();
                        clsPublicVariables.GENHOTELNAME = (row1["SMSHOTELNAME"]) + "".Trim();
                        clsPublicVariables.GENPROSENDERID = (row1["PROSENDERID"]) + "".Trim();

                        clsPublicVariables.GENSMSREMARK1 = (row1["SMSREMARK1"]) + "".Trim();
                        clsPublicVariables.GENSMSREMARK2 = (row1["SMSREMARK2"]) + "".Trim();
                        clsPublicVariables.GENSMSREMARK3 = (row1["SMSREMARK3"]) + "".Trim();
                        clsPublicVariables.GENSMSREMARK4 = (row1["SMSREMARK4"]) + "".Trim();

                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string RightString(string s, int length)
        {
            try
            {
                length = Math.Max(length, 0);

                if (s.Length > length)
                {
                    return s.Substring(s.Length - length, length);
                }
                else
                {
                    return s;
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

    }
}
