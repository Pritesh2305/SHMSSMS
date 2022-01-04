using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Data.SqlClient;

namespace EVENTSCHEDULER
{
    public partial class frmeventserver : Form
    {

        clsGeneral objclsgen = new clsGeneral();
        clsMsSqlDbFunction mssql = new clsMsSqlDbFunction();

        public frmeventserver()
        {
            InitializeComponent();
            objclsgen.GetConnectionDetails();
            objclsgen.FillSMSDetails();
        }

        private bool START_Timer()
        {
            try
            {
                this.tmrevent.Interval = 60000;
                this.tmrsms.Interval = 25000;// 1 minutes

                this.tmrevent.Enabled = true;
                this.tmrsms.Enabled = true;

                this.Write_In_Error_Log("EVENT SCHEDULER START [" + DateTime.Now.ToString() + " ]");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool STOP_Timer()
        {
            try
            {
                this.tmrevent.Enabled = false;
                this.tmrsms.Enabled = false;
                this.Write_In_Error_Log("EVENT SCHEDULER  STOP [" + DateTime.Now.ToString() + " ]");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void btnrun_Click(object sender, EventArgs e)
        {
            this.START_Timer();
        }

        private void btnstop_Click(object sender, EventArgs e)
        {
            this.STOP_Timer();
        }

        private void tmrevent_Tick(object sender, EventArgs e)
        {
            this.RunEvent();
        }

        private bool RunEvent()
        {
            DataTable dtevent = new DataTable();
            Int64 rid = 0;
            String eventname, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail = "";
            String sendsms, sendemail, eventstop = "";
            string templateid1, otherno1, para1, para2, para3 = "";

            try
            {
                //Write_In_Error_Log("GENERATING NEW SMS [ " + DateTime.Now.ToString() + " ]");

                //Write_In_Error_Log("ServerName1 = " + clsPublicVariables.ServerName1 + " [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log("DatabaseName1 = " +clsPublicVariables.DatabaseName1 + " [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log(" UserName1 = "+ clsPublicVariables.UserName1 + " [ " + DateTime.Now.ToString() + " ]");
                //Write_In_Error_Log(" Password1 = " + clsPublicVariables.Password1 + " [ " + DateTime.Now.ToString() + " ]");

                string str1 = " SELECT *,DATEDIFF(SECOND, CONVERT(varchar(20),EVENTLASTRUN,120), CONVERT(varchar(20),getdate(),120)) as TIMEDIFF  " +
                                " FROM EVENTSCHEDULER  " +
                                " WHERE (ISNULL(EVENTSTOP,0)=0 AND (ISNULL(SENDSMS,0)=1) OR ISNULL(SENDEMAIL,0)=1)  " +
                                " AND CONVERT(varchar(20),EVENTLASTRUN,120) <= CONVERT(varchar(20),getdate(),120)   " +
                                " ORDER BY TIMEDIFF,RID DESC ";

                dtevent = mssql.FillDataTable(str1, "EVENTSCHEDULER");

                //Write_In_Error_Log( dtevent.Rows.Count.ToString() +  " [ " + DateTime.Now.ToString() + " ]");

                if (dtevent.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtevent.Rows)
                    {
                        Int64.TryParse(row1["rid"] + "".ToString(), out rid);
                        eventname = row1["EVENTNAME"] + "".ToString();
                        eventruntype = row1["EVENTRUNTYPE"] + "".ToString();
                        eventinterval = row1["EVENTINTERVAL"] + "".ToString();
                        eventstartdate = row1["EVENTSTARTDATE"] + "".ToString();
                        eventlastrun = row1["EVENTLASTRUN"] + "".ToString();
                        eventtext = row1["EVENTTEXT"] + "".ToString();
                        eventmobno = row1["EVENTMOBNO"] + "".ToString();
                        eventemail = row1["EVENTEMAIL"] + "".ToString();
                        sendsms = row1["SENDSMS"] + "".ToString();
                        sendemail = row1["SENDEMAIL"] + "".ToString();
                        eventstop = row1["EVENTSTOP"] + "".ToString();
                        templateid1 = row1["TEMPLATEID"] + "".Trim();
                        otherno1 = row1["OTHERID"] + "".Trim();
                        para1 = row1["PARA1"] + "".Trim();
                        para2 = row1["PARA2"] + "".Trim();
                        para3 = row1["PARA3"] + "".Trim();

                        //this.Write_In_Error_Log("sendsms = " + sendsms + "Event = " + eventname + "Rid = " + rid.ToString() + "[ " + DateTime.Now.ToString() + " ]");

                        if (eventruntype == "EVENT_ONCEADAY")
                        {
                            if (Convert.ToInt64(eventinterval) > DateTime.Now.Hour)
                            {
                                return true;
                            }
                        }
                        else if (eventruntype == "EVENT_ONCEAMONTH")
                        {
                            if (Convert.ToInt16(eventinterval) != DateTime.Now.Day)
                            {
                                return true;
                            }
                        }
                        else if (eventruntype == "EVENT_ONCEAWEEK")
                        {
                            if (Convert.ToInt16(eventinterval) != (int)DateTime.Now.DayOfWeek)
                            {
                                return true;
                            }
                        }

                        if ((sendsms.ToLower().Trim() == "true"))
                        {
                            bool retvalsms = this.SEND_SMS_EVENT(rid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail,
                                                                templateid1, otherno1, para1, para2, para3);
                        }

                        if ((sendemail.ToLower().Trim() == "true"))
                        {
                            bool retvalemail = this.SEND_EMAIL_EVENT(rid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        }

                        // Update time
                        DateTime nextrun1;
                        nextrun1 = GetEventLastRunTime(rid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                        str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + rid;
                        mssql.ExecuteMsSqlCommand(str1);

                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in RunEvent()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SEND_SMS_EVENT(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun, string eventtext, string eventmobno, string eventemail,
                                    string templateid1 = "", string otherid1 = "", string para1 = "", string para2 = "", string para3 = "")
        {
            try
            {
                switch (eventid)
                {
                    case 1:
                        this.SMS_Bithday_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 2:
                        this.SMS_Anniversary_Wishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 3:
                        this.SMS_CheckIN_SMS(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 4: // Hotel Anjani Inn
                        this.SMS_CheckIN_SMS_Type1(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 5: // Hotel Anjani Inn
                        this.SMS_CheckOUT_SMS(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 6: // General
                        this.SMS_CheckOUT_SMS_Type1(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 7: // General
                        this.SMS_CheckIN_SMS_Type2(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 8: // Summary SMS
                        this.SMS_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 9: // Entry Summary SMS
                        this.SMS_EntryTicket_Summary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 10: // Costume Summary SMS
                        this.SMS_CostumeTicket_Summary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 11: // Check IN SMS TO Reporting Person
                        this.SMS_CheckIN_SMS_To_RepotingPerson(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 12: // Check OUT SMS TO Reporting Person
                        this.SMS_CheckOUT_SMS_To_RepotingPerson(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    case 13: // Check OUT SMS TO Reporting Person With Amount
                        this.SMS_CheckOUT_SMS_To_RepotingPerson_WithAmount(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;
                    case 14: //CHECK IN SMS OF SHANTIVAN RESORT
                        this.SMS_CheckIN_SHANTIVAN(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail,
                                                    templateid1, otherid1, para1, para2, para3);
                        break;
                    case 15: //BOOKING SMS  SHANTIVAN
                        this.SMS_ROOMBOOKING_SMS(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail,
                                                templateid1, otherid1, para1, para2, para3);
                        break;
                    case 16: //CHECK IN SMS TO REPORTING PERSON SHANTIVAN RESORT
                        this.SMS_CheckIN_SHANTIVAN_ReportingPerson(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail,
                                                    templateid1, otherid1, para1, para2, para3);
                        break;
                    case 17://BOOKING SMS TO REPORTING PERSON SHANTIVAN
                        this.SMS_ROOMBOOKING_SMS_ReportingPerson(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail,
                                                templateid1, otherid1, para1, para2, para3);
                        break;
                    default:
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SEND_SMS_EVENT()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SEND_EMAIL_EVENT(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                 string eventtext, string eventmobno, string eventemail)
        {
            try
            {
                //Write_In_Error_Log("GENERATING NEW E-MAIL [ " + DateTime.Now.ToString() + " ]");

                switch (eventid)
                {
                    //case 1:
                    //    this.EMAIL_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                    //    break;

                    //case 2:
                    //    this.EMAIL_Feedback(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                    //    break;

                    case 3:
                        this.EMAIL_CheckIN(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                        break;

                    //case 4:
                    //    this.EMAIL_AnniversaryWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                    //    break;

                    //case 5:
                    //    this.EMAIL_BillWishes(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                    //    break;

                    //case 6:
                    //    this.EMAIL_DailySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                    //    break;

                    //case 7:
                    //    this.SMS_YesterdaySalesSummary(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun, eventtext, eventmobno, eventemail);
                    //    break;

                    default:
                        break;
                }


                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Write_In_Error_Log(string errstr)
        {
            try
            {
                this.txtinfo.Text = errstr + System.Environment.NewLine + this.txtinfo.Text;
                Thread.Sleep(10);
                this.Refresh();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DateTime GetEventLastRunTime(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun)
        {
            DateTime lastrun1, newlastrun1;
            Int64 eventint;

            lastrun1 = Convert.ToDateTime(eventlastrun);
            newlastrun1 = lastrun1;

            Int64.TryParse(eventinterval, out eventint);

            try
            {
                switch (eventruntype)
                {
                    case "EVENT_FREQUENTLY":
                        newlastrun1 = DateTime.Now.AddMinutes(eventint);
                        break;

                    case "EVENT_ONCEADAY":
                        int eventint1 = (int)eventint;
                        newlastrun1 = lastrun1.AddDays(1);

                        if (newlastrun1.Date < DateTime.Now.Date)
                        {
                            newlastrun1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, eventint1, 0, 0);
                        }
                        else
                        {
                            newlastrun1 = new DateTime(newlastrun1.Year, newlastrun1.Month, newlastrun1.Day, eventint1, 0, 0);
                        }
                        break;

                    case "EVENT_ONCEAWEEK":
                        int eventint2 = (int)eventint;
                        newlastrun1 = lastrun1.AddDays(7);
                        newlastrun1 = new DateTime(newlastrun1.Year, newlastrun1.Month, newlastrun1.Day, eventint2, 0, 0);
                        break;

                    case "EVENT_ONCEAMONTH":
                        int eventint3 = (int)eventint;
                        newlastrun1 = lastrun1.AddMonths(1);
                        newlastrun1 = new DateTime(newlastrun1.Year, newlastrun1.Month, newlastrun1.Day, 10, 0, 0);
                        break;

                    default:
                        break;
                }

                return newlastrun1;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in GetEventLastRunTime())");
                return lastrun1;
            }
        }

        private bool Check_SMS_Exit(string rmsid, Int64 eventid)
        {
            string str1;
            DataTable dtsms1 = new DataTable();
            try
            {
                str1 = "Select Rmsid from SMSHISTORY Where Rmsid='" + rmsid + "' and isnull(SENDFLG,0)=1 and SMSEVENTID = " + eventid;
                dtsms1 = mssql.FillDataTable(str1, "SMSHISTORY");

                if (dtsms1.Rows.Count > 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region SMS EVENT CODE


        //private bool SMS_YesterdaySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
        //                                  string eventtext, string eventmobno, string eventemail)
        //{
        //    string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;
        //    DateTime nextrun1;
        //    DataTable dtsalesinfo = new DataTable();
        //    DataTable dtsettinfo = new DataTable();

        //    string smstext1 = "";
        //    string billamt, settamt = "0";

        //    try
        //    {

        //        Write_In_Error_Log("GENERATING YESTERDAY SALES SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

        //        // Billing
        //        billamt = "";
        //        wstr_delflg = "isnull(delflg,0)=0";
        //        wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
        //        wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),(getdate()-1),112)";
        //        str1 = "select sum(Netamount) as NetAmt from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
        //        mssql.OpenMsSqlConnection();
        //        dtsalesinfo = mssql.FillDataTable(str1, "bill");

        //        billamt = "0";
        //        if (dtsalesinfo.Rows.Count > 0)
        //        {
        //            billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
        //        }

        //        if (billamt.Trim() == "")
        //        {
        //            billamt = "0";
        //        }

        //        /// Settlements
        //        str2 = "";
        //        wstr_date1 = "";
        //        wstr_delflg = "isnull(delflg,0)=0";
        //        wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate()-1,112)";

        //        str2 = " select " +
        //              " sum(setleamount) as NetAmt from settlement " +
        //              " where " + wstr_delflg + " And " + wstr_date1;
        //        mssql.OpenMsSqlConnection();
        //        dtsettinfo = mssql.FillDataTable(str2, "settlement");

        //        settamt = "0";
        //        if (dtsettinfo.Rows.Count > 0)
        //        {
        //            settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
        //        }

        //        if (settamt.Trim() == "")
        //        {
        //            settamt = "0";
        //        }


        //        smstext1 = "Date : " + DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy") + ", Billing : " + billamt + ", Settlement : " + settamt + "@" + clsPublicVariables.SMSSIGN;
        //        //Date : 123, Billing : 123, Settlement : 123

        //        // INSERT INTO SMSHISTORY

        //        string[] strArr = eventmobno.Split(',');
        //        Int64 cnt = 0;

        //        for (cnt = 1; cnt <= strArr.Length; cnt++)
        //        {
        //            clssmshistoryBal smsbal = new clssmshistoryBal();
        //            smsbal.Id = 0;
        //            smsbal.Formmode = 0;
        //            smsbal.Smsevent = "YesterdaySalesSummary";
        //            smsbal.Smseventid = eventid;
        //            smsbal.Mobno = strArr[cnt - 1].ToString();
        //            smsbal.Smstext = smstext1;
        //            smsbal.Sendflg = 0;
        //            smsbal.Smspername = "";
        //            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
        //            smsbal.Smstype = "TRANSACTION";
        //            smsbal.Resid = "";
        //            smsbal.Rmsid = "YSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
        //            smsbal.Tobesenddatetime = DateTime.Now;
        //            smsbal.Loginuserid = 0;

        //            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
        //            {
        //                smsbal.Db_Operation_SMSHISTORY(smsbal);
        //            }
        //        }

        //        //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
        //        //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
        //        //mssql.ExecuteMsSqlCommand(str1);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_YesterdaySalesSummary()) " + DateTime.Now.ToString());
        //        return false;
        //    }
        //}

        //private bool SMS_DailySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
        //                                    string eventtext, string eventmobno, string eventemail)
        //{
        //    string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;
        //    DateTime nextrun1;
        //    DataTable dtsalesinfo = new DataTable();
        //    DataTable dtsettinfo = new DataTable();

        //    string smstext1 = "";
        //    string billamt, settamt = "0";

        //    try
        //    {

        //        Write_In_Error_Log("GENERATING DAILY SALES SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

        //        // Billing
        //        billamt = "";
        //        wstr_delflg = "isnull(delflg,0)=0";
        //        wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
        //        wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
        //        str1 = "select sum(Netamount) as NetAmt from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
        //        mssql.OpenMsSqlConnection();
        //        dtsalesinfo = mssql.FillDataTable(str1, "bill");

        //        billamt = "0";
        //        if (dtsalesinfo.Rows.Count > 0)
        //        {
        //            billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
        //        }

        //        if (billamt.Trim() == "")
        //        {
        //            billamt = "0";
        //        }

        //        /// Settlements
        //        str2 = "";
        //        wstr_date1 = "";
        //        wstr_delflg = "isnull(delflg,0)=0";
        //        wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate(),112)";

        //        str2 = " select " +
        //              " sum(setleamount) as NetAmt from settlement " +
        //              " where " + wstr_delflg + " And " + wstr_date1;
        //        mssql.OpenMsSqlConnection();
        //        dtsettinfo = mssql.FillDataTable(str2, "settlement");

        //        settamt = "0";
        //        if (dtsettinfo.Rows.Count > 0)
        //        {
        //            settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
        //        }

        //        if (settamt.Trim() == "")
        //        {
        //            settamt = "0";
        //        }

        //        smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + ", Billing : " + billamt + ", Settlement : " + settamt + "@" + clsPublicVariables.SMSSIGN;
        //        //Date : 123, Billing : 123, Settlement : 123

        //        // INSERT INTO SMSHISTORY

        //        string[] strArr = eventmobno.Split(',');
        //        Int64 cnt = 0;

        //        for (cnt = 1; cnt <= strArr.Length; cnt++)
        //        {
        //            clssmshistoryBal smsbal = new clssmshistoryBal();
        //            smsbal.Id = 0;
        //            smsbal.Formmode = 0;
        //            smsbal.Smsevent = "DailySalesSummary";
        //            smsbal.Smseventid = eventid;
        //            smsbal.Mobno = strArr[cnt - 1].ToString();
        //            smsbal.Smstext = smstext1;
        //            smsbal.Sendflg = 0;
        //            smsbal.Smspername = "";
        //            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
        //            smsbal.Smstype = "TRANSACTION";
        //            smsbal.Resid = "";
        //            smsbal.Rmsid = "DSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
        //            smsbal.Tobesenddatetime = DateTime.Now;
        //            smsbal.Loginuserid = 0;

        //            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
        //            {
        //                smsbal.Db_Operation_SMSHISTORY(smsbal);
        //            }
        //        }

        //        //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
        //        //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
        //        //mssql.ExecuteMsSqlCommand(str1);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_DailySalesSummary()) " + DateTime.Now.ToString());
        //        return false;
        //    }
        //}

        //private bool SMS_Feedback(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
        //                                      string eventtext, string eventmobno, string eventemail)
        //{
        //    string str1;
        //    string wstr_date1 = "";
        //    string wstr_mobno = "";
        //    string wstr_delflg = "";
        //    wstr_delflg = "isnull(MSTFEEDBACK.delflg,0)=0";
        //    wstr_date1 = "CONVERT(varchar(10),MSTFEEDBACK.FEEDDATE,112) = CONVERT(varchar(10),getdate(),112)";
        //    wstr_mobno = "(ISNULL(MSTFEEDBACK.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";

        //    DataTable dtfeedback = new DataTable();

        //    string custname;
        //    string custmobno;
        //    string feedrid;
        //    string smstext1 = "";
        //    DateTime nextrun1;
        //    try
        //    {

        //        Write_In_Error_Log("GENERATING FEEDBACK SMS [ " + DateTime.Now.ToString() + " ]");

        //        str1 = "Select MSTFEEDBACK.RID,MSTFEEDBACK.CUSTCONTNO,MSTFEEDBACK.FEEDDATE,MSTCUST.CUSTNAME,MSTCUST.CUSTMOBNO From MSTFEEDBACK " +
        //                " LEFT JOIN MSTCUST ON (MSTCUST.RID = MSTFEEDBACK.CUSTRID)" +
        //                " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

        //        dtfeedback = mssql.FillDataTable(str1, "MSTFEEDBACK");

        //        if (dtfeedback.Rows.Count > 0)
        //        {
        //            foreach (DataRow row1 in dtfeedback.Rows)
        //            {
        //                feedrid = row1["RID"] + "".ToString();

        //                custname = row1["CUSTNAME"] + "".ToString();

        //                if (custname.Trim() == "")
        //                {
        //                    custname = "Customer";
        //                }

        //                custmobno = row1["CUSTCONTNO"] + "".ToString();

        //                if (custmobno.Trim() == "")
        //                {
        //                    custmobno = row1["CUSTMOBNO"] + "".ToString();
        //                }

        //                //custname = row1["CUSTNAME"] + "".ToString();
        //                //custmobno = row1["CUSTCONTNO"] + "".ToString();

        //                smstext1 = "Dear " + custname + " Thank you for feedback " + clsPublicVariables.SMSSIGN;

        //                clssmshistoryBal smsbal = new clssmshistoryBal();
        //                smsbal.Id = 0;
        //                smsbal.Formmode = 0;
        //                smsbal.Smsevent = "FeedbackSMS";
        //                smsbal.Smseventid = eventid;
        //                smsbal.Mobno = custmobno;
        //                smsbal.Smstext = smstext1;
        //                smsbal.Sendflg = 0;
        //                smsbal.Smspername = custname;
        //                smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
        //                smsbal.Smstype = "TRANSACTION";
        //                smsbal.Resid = "";
        //                smsbal.Rmsid = "FED" + feedrid;
        //                smsbal.Tobesenddatetime = DateTime.Now;
        //                smsbal.Loginuserid = 0;

        //                if (Check_SMS_Exit(smsbal.Rmsid, eventid))
        //                {
        //                    smsbal.Db_Operation_SMSHISTORY(smsbal);
        //                }
        //            }
        //        }

        //        //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
        //        //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
        //        //mssql.ExecuteMsSqlCommand(str1);

        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //private bool SMS_BirthdayWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
        //                                      string eventtext, string eventmobno, string eventemail)
        //{
        //    string str1;

        //    DataTable dtbirth = new DataTable();

        //    string custname;
        //    string custmobno;
        //    string rid;
        //    string smstext1 = "";
        //    DateTime nextrun1;
        //    try
        //    {

        //        Write_In_Error_Log("GENERATING BIRTHDAY SMS [ " + DateTime.Now.ToString() + " ]");

        //        str1 = " select * from BIRTHDATESMSALERTLIST ";

        //        dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

        //        if (dtbirth.Rows.Count > 0)
        //        {
        //            foreach (DataRow row1 in dtbirth.Rows)
        //            {
        //                rid = row1["RID"] + "".ToString();
        //                custname = row1["ENAME"] + "".ToString();
        //                custmobno = row1["MOBNO"] + "".ToString();

        //                //smstext1 = "Dear Member," + custname + " Happy Birthday From:" + clsPublicVariables.SMSSIGN;
        //                smstext1 = "Many many happy return of the day if you visit " + clsPublicVariables.SMSSIGN + " will get pestry complementary";

        //                clssmshistoryBal smsbal = new clssmshistoryBal();
        //                smsbal.Id = 0;
        //                smsbal.Formmode = 0;
        //                smsbal.Smsevent = "BirthdaySMS";
        //                smsbal.Smseventid = eventid;
        //                smsbal.Mobno = custmobno;
        //                smsbal.Smstext = smstext1;
        //                smsbal.Sendflg = 0;
        //                smsbal.Smspername = custname;
        //                smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
        //                smsbal.Smstype = "TRANSACTION";
        //                smsbal.Resid = "";
        //                smsbal.Rmsid = "BIR" + rid;
        //                smsbal.Tobesenddatetime = DateTime.Now;
        //                smsbal.Loginuserid = 0;

        //                if (Check_SMS_Exit(smsbal.Rmsid, eventid))
        //                {
        //                    smsbal.Db_Operation_SMSHISTORY(smsbal);
        //                }
        //            }
        //        }

        //        //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
        //        //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
        //        //mssql.ExecuteMsSqlCommand(str1);



        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //private bool SMS_AnniversaryWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
        //                                      string eventtext, string eventmobno, string eventemail)
        //{
        //    string str1;

        //    DataTable dtbirth = new DataTable();

        //    string custname;
        //    string custmobno;
        //    string rid;
        //    string smstext1 = "";
        //    DateTime nextrun1;
        //    try
        //    {

        //        Write_In_Error_Log("GENERATING ANNIVERSARY SMS [ " + DateTime.Now.ToString() + " ]");

        //        str1 = " select * from ANNIDATESMSALERTLIST ";

        //        dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

        //        if (dtbirth.Rows.Count > 0)
        //        {
        //            foreach (DataRow row1 in dtbirth.Rows)
        //            {
        //                rid = row1["RID"] + "".ToString();
        //                custname = row1["ENAME"] + "".ToString();
        //                custmobno = row1["MOBNO"] + "".ToString();

        //                //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
        //                smstext1 = "Many many happy anniversary if you visit " + clsPublicVariables.SMSSIGN + " will get pestry complementary";

        //                clssmshistoryBal smsbal = new clssmshistoryBal();
        //                smsbal.Id = 0;
        //                smsbal.Formmode = 0;
        //                smsbal.Smsevent = "AnniversarySMS";
        //                smsbal.Smseventid = eventid;
        //                smsbal.Mobno = custmobno;
        //                smsbal.Smstext = smstext1;
        //                smsbal.Sendflg = 0;
        //                smsbal.Smspername = custname;
        //                smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
        //                smsbal.Smstype = "TRANSACTION";
        //                smsbal.Resid = "";
        //                smsbal.Rmsid = "ANN" + rid;
        //                smsbal.Tobesenddatetime = DateTime.Now;
        //                smsbal.Loginuserid = 0;

        //                if (Check_SMS_Exit(smsbal.Rmsid, eventid))
        //                {
        //                    smsbal.Db_Operation_SMSHISTORY(smsbal);
        //                }
        //            }
        //        }

        //        //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
        //        //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
        //        //mssql.ExecuteMsSqlCommand(str1);

        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //private bool SMS_BillWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
        //                                   string eventtext, string eventmobno, string eventemail)
        //{
        //    string str1;
        //    string wstr_date1 = "";
        //    string wstr_delflg = "";
        //    string wstr_mobno = "";
        //    wstr_delflg = "isnull(BILL.delflg,0)=0";
        //    wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
        //    wstr_mobno = "(ISNULL(BILL.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";
        //    DataTable dtbill = new DataTable();

        //    string custname;
        //    string custmobno;
        //    string billrid;
        //    string smstext1 = "";
        //    DateTime nextrun1;
        //    String Netamt1 = "0";

        //    try
        //    {

        //        Write_In_Error_Log("GENERATING Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

        //        str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
        //                    " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO From Bill" +
        //                     " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
        //                     " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

        //        dtbill = mssql.FillDataTable(str1, "BILL");

        //        Netamt1 = "0";

        //        if (dtbill.Rows.Count > 0)
        //        {
        //            foreach (DataRow row1 in dtbill.Rows)
        //            {
        //                billrid = row1["RID"] + "".ToString();

        //                custname = row1["CUTOMERNAME"] + "".ToString();

        //                if (custname.Trim() == "")
        //                {
        //                    custname = "Customer";
        //                }

        //                custmobno = row1["CUSTCONTNO"] + "".ToString();

        //                if (custmobno.Trim() == "")
        //                {
        //                    custmobno = row1["CUSTMOBNO"] + "".ToString();
        //                }

        //                Netamt1 = row1["NETAMOUNT"] + "".ToString();

        //                //smstext1 = custname + " - Thanks for visiting " + clsPublicVariables.SMSSIGN + " see you again";
        //                smstext1 = "Thanks for visiting " + clsPublicVariables.SMSSIGN + " your bill amount is " + Netamt1 + " looking for next visit";

        //                clssmshistoryBal smsbal = new clssmshistoryBal();
        //                smsbal.Id = 0;
        //                smsbal.Formmode = 0;
        //                smsbal.Smsevent = "BillWishes";
        //                smsbal.Smseventid = eventid;
        //                smsbal.Mobno = custmobno;
        //                smsbal.Smstext = smstext1;
        //                smsbal.Sendflg = 0;
        //                smsbal.Smspername = custname;
        //                smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
        //                smsbal.Smstype = "TRANSACTION";
        //                smsbal.Resid = "";
        //                smsbal.Rmsid = "BIL" + billrid;
        //                smsbal.Tobesenddatetime = DateTime.Now;
        //                smsbal.Loginuserid = 0;

        //                if (Check_SMS_Exit(smsbal.Rmsid, eventid))
        //                {
        //                    smsbal.Db_Operation_SMSHISTORY(smsbal);
        //                }
        //            }
        //        }

        //        //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
        //        //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
        //        //mssql.ExecuteMsSqlCommand(str1);

        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //private bool SMS_Feedback_2(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
        //                                     string eventtext, string eventmobno, string eventemail)
        //{
        //    string str1;
        //    string wstr_date1 = "";
        //    string wstr_mobno = "";
        //    string wstr_delflg = "";
        //    wstr_delflg = "isnull(MSTFEEDBACK.delflg,0)=0";
        //    wstr_date1 = "CONVERT(varchar(10),MSTFEEDBACK.FEEDDATE,112) = CONVERT(varchar(10),getdate(),112)";
        //    wstr_mobno = "(ISNULL(MSTFEEDBACK.CUSTCONTNO,'') <>'' OR ISNULL(MSTCUST.CUSTMOBNO,'') <>'')";

        //    DataTable dtfeedback = new DataTable();

        //    string custname;
        //    string custmobno;
        //    string feedrid;
        //    string smstext1 = "";
        //    //DateTime nextrun1;
        //    try
        //    {

        //        Write_In_Error_Log("GENERATING FEEDBACK SMS [ " + DateTime.Now.ToString() + " ]");

        //        str1 = "Select MSTFEEDBACK.RID,MSTFEEDBACK.CUSTCONTNO,MSTFEEDBACK.FEEDDATE,MSTCUST.CUSTNAME,MSTCUST.CUSTMOBNO From MSTFEEDBACK " +
        //                " LEFT JOIN MSTCUST ON (MSTCUST.RID = MSTFEEDBACK.CUSTRID)" +
        //                " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

        //        dtfeedback = mssql.FillDataTable(str1, "MSTFEEDBACK");

        //        if (dtfeedback.Rows.Count > 0)
        //        {
        //            foreach (DataRow row1 in dtfeedback.Rows)
        //            {
        //                feedrid = row1["RID"] + "".ToString();

        //                custname = row1["CUSTNAME"] + "".ToString();

        //                if (custname.Trim() == "")
        //                {
        //                    custname = "Customer";
        //                }

        //                custmobno = row1["CUSTCONTNO"] + "".ToString();

        //                if (custmobno.Trim() == "")
        //                {
        //                    custmobno = row1["CUSTMOBNO"] + "".ToString();
        //                }

        //                //custname = row1["CUSTNAME"] + "".ToString();
        //                //custmobno = row1["CUSTCONTNO"] + "".ToString();

        //                //smstext1 = "Dear " + custname + " Thank you for feedback " + clsPublicVariables.SMSSIGN;
        //                smstext1 = "Thank you for dinning at " + clsPublicVariables.SMSSIGN + " and sharing your valuable feedback. We hope to serve you soon.";

        //                //smstext1 = "Thankyou for dinning at Souq Bistro & Grills and sharing your valuable feedback.we hope to serve you soon.";

        //                clssmshistoryBal smsbal = new clssmshistoryBal();
        //                smsbal.Id = 0;
        //                smsbal.Formmode = 0;
        //                smsbal.Smsevent = "FeedbackSMS2";
        //                smsbal.Smseventid = eventid;
        //                smsbal.Mobno = custmobno;
        //                smsbal.Smstext = smstext1;
        //                smsbal.Sendflg = 0;
        //                smsbal.Smspername = custname;
        //                smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
        //                smsbal.Smstype = "TRANSACTION";
        //                smsbal.Resid = "";
        //                smsbal.Rmsid = "FED2" + feedrid;
        //                smsbal.Tobesenddatetime = DateTime.Now;
        //                smsbal.Loginuserid = 0;

        //                if (Check_SMS_Exit(smsbal.Rmsid, eventid))
        //                {
        //                    smsbal.Db_Operation_SMSHISTORY(smsbal);
        //                }
        //            }
        //        }

        //        //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
        //        //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
        //        //mssql.ExecuteMsSqlCommand(str1);

        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}


        private bool SMS_Bithday_Wishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING BIRTHDAY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from BIRTHDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        smstext1 = "Many many happy return of the day from " + clsPublicVariables.GENHOTELNAME + ".";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BirthdaySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BIR" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_Anniversary_Wishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                            string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;

            try
            {

                Write_In_Error_Log("GENERATING ANNIVERSARY SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ANNIDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["MOBNO"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Many many happy anniversary from " + clsPublicVariables.GENHOTELNAME + ".";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "AnniversarySMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "ANN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckIN_SMS(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check IN SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        roomno = row1["ROOMNAME"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Dear Guest, " + custname + " welcomes you, please check in room no. " + roomno;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "CheckINSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "CHECKIN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckIN_SMS_Type1(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check IN SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        roomno = row1["ROOMNAME"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Welcome to " + clsPublicVariables.GENHOTELNAME + " We Provide all type of facility to our guest.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "CheckINSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "CHECKIN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckOUT_SMS(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check OUT SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKOUTLIST where DATEPART(d, CKOUTDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, CKOUTDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, CKOUTDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKOUTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        //roomno = row1["ROOMNAME"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Thanks to Visit " + clsPublicVariables.GENHOTELNAME + " Next time most Welcome to here.";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "CheckOUTSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "CHECKOUT" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CHECKIN_OUT_SMS(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check IN SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        roomno = row1["ROOMNAME"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Dear Guest, " + custname + " welcomes you, please check in room no. " + roomno;

                        //"Date : 123 Check In: 123 Check Out : 123 Billing: 123123"

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "CheckINSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "CHECKIN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckOUT_SMS_Type1(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check OUT SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKOUTLIST where DATEPART(d, CKOUTDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, CKOUTDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, CKOUTDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKOUTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        //roomno = row1["ROOMNAME"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Thanks to Visit " + clsPublicVariables.GENHOTELNAME + " Next time most Welcome to here." +
                                       System.Environment.NewLine + clsPublicVariables.GENSMSREMARK1;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "CheckOUTSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "CHECKOUT" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckIN_SMS_Type2(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check IN SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".ToString();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        roomno = row1["ROOMNAME"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Dear Guest, It is a great pleasure for us to welcome you at " + clsPublicVariables.GENHOTELNAME +
                                        System.Environment.NewLine + clsPublicVariables.GENSMSREMARK2;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "CheckINSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "CHECKIN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_DailySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string totckin1 = "";
            string totckout1 = "";
            string totfinalamt1 = "";

            try
            {

                Write_In_Error_Log("GENERATING DAILY SALES SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

                // CHECK IN 
                wstr_date1 = "  CONVERT(varchar(10),ARRIDATE,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = " SELECT COUNT(*) AS TOTCHKIN FROM ROOMCHECKIN WHERE " + wstr_date1 + " AND ISNULL(ROOMCHECKIN.DELFLG,0)=0 ";
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                if (dtsalesinfo.Rows.Count > 0)
                {
                    totckin1 = dtsalesinfo.Rows[0]["TOTCHKIN"] + "";
                }

                if (totckin1.Trim() == "")
                {
                    totckin1 = "0";
                }

                // CHECK OUT 
                str1 = "";
                wstr_date1 = "  CONVERT(varchar(10),CKOUTDATE,112) = CONVERT(varchar(10),getdate(),112)";

                str1 = " SELECT COUNT(*) AS TOTCHKOUT FROM ROOMCHECKOUT WHERE " + wstr_date1 + " AND ISNULL(ROOMCHECKOUT.DELFLG,0)=0 ";
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "ROOMCHECKOUT");

                if (dtsalesinfo.Rows.Count > 0)
                {
                    totckout1 = dtsalesinfo.Rows[0]["TOTCHKOUT"] + "";
                }
                if (totckin1.Trim() == "")
                {
                    totckout1 = "0";
                }

                str1 = "";
                wstr_date1 = "";
                wstr_delflg = " isnull(delflg,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),CKOUTDATE,112) = CONVERT(varchar(10),getdate(),112)";

                str1 = " Select " +
                            " sum(TOTBASICAMT) As TOTBASICAMT, " +
                            " sum(TOTADVANCEAMT) As TOTADVANCEAMT," +
                            " sum(TOTDISCAMT) As TOTDISCAMT, " +
                            " sum(TOTLUXTAXAMT) As TOTLUXTAXAMT, " +
                            " sum(TOTSERTAXAMT) As TOTSERTAXAMT," +
                            " sum(TOTVATAMT) As TOTVATAMT, " +
                            " sum(TOTCESSAMT) AS TOTCESSAMT," +
                            " sum(TOTECESSAMT) As TOTECESSAMT ," +
                            " sum(TOTROFF) As TOTROFF ," +
                            " sum(TOTNETAMT) As TOTNETAMT, " +
                            " (sum(TOTADVANCEAMT)  + sum(TOTNETAMT)) As TOTFINALAMT " +
                            " From ROOMCHECKOUT " +
                            " Where " + wstr_delflg + " And " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "ROOMCHECKOUT");

                if (dtsalesinfo.Rows.Count > 0)
                {
                    //this.txtbillingdet_basicamt.Text = dtsalesinfo.Rows[0]["TOTBASICAMT"] + "";
                    //this.txtbillingdet_advance.Text = dtbill.Rows[0]["TOTADVANCEAMT"] + "";
                    //this.txtbillingdet_discount.Text = dtbill.Rows[0]["TOTDISCAMT"] + "";
                    //this.txtbillingdet_luxtaxamt.Text = dtbill.Rows[0]["TOTLUXTAXAMT"] + "";
                    //this.txtbillingdet_servicetax.Text = dtbill.Rows[0]["TOTSERTAXAMT"] + "";
                    //this.txtbillingdet_vat.Text = dtbill.Rows[0]["TOTVATAMT"] + "";
                    //this.txtbillingdet_cess.Text = dtbill.Rows[0]["TOTCESSAMT"] + "";
                    //this.txtbillingdet_educess.Text = dtbill.Rows[0]["TOTECESSAMT"] + "";
                    //this.txtbillingdet_roff.Text = dtbill.Rows[0]["TOTROFF"] + "";
                    //this.txtbilling_total.Text = dtbill.Rows[0]["TOTNETAMT"] + "";
                    totfinalamt1 = dtsalesinfo.Rows[0]["TOTFINALAMT"] + "";
                }

                if (totfinalamt1.Trim() == "")
                {
                    totfinalamt1 = "0";
                }

                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + " CHECKIN : " + totckin1 + " , CHECKOUT : " + totckout1 + " , TOTAL AMOUNT : " + totfinalamt1 + " @ " + clsPublicVariables.SMSSIGN;

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "DailySalesSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "DSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_DailySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_EntryTicket_Summary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";

            string totfinalamt1 = "";
            string totperson1 = "";

            try
            {

                Write_In_Error_Log("GENERATING ENTRY TICKET SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

                str1 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(entryticket.delflg,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),entryticket.ETDATE,112) = CONVERT(varchar(10),getdate(),112)";

                str1 = " select " +
                            " sum(etnoof) as etnoof,sum(ETTOTAMT) as ettotamt," +
                            " sum(etdiscamt) as etdiscamt,sum(ettax1amt) as ettax1amt,sum(ettax2amt) as ettax2amt," +
                            " sum(ettax3amt) as ettax3amt,sum(ETNETAMT) as ETNETAMT from entryticket " +
                            " Where " + wstr_delflg + " And " + wstr_date1;

                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "entryticket");

                if (dtsalesinfo.Rows.Count > 0)
                {
                    totperson1 = dtsalesinfo.Rows[0]["etnoof"] + "";
                    //this.txtamount.Text = dtsalesinfo.Rows[0]["ettotamt"] + "";
                    //this.txtdiscount.Text = dtsalesinfo.Rows[0]["etdiscamt"] + "";
                    //this.txttax1.Text = dtsalesinfo.Rows[0]["ettax1amt"] + "";
                    //this.txttax2.Text = dtsalesinfo.Rows[0]["ettax2amt"] + "";
                    //this.txttax3.Text = dtsalesinfo.Rows[0]["ettax3amt"] + "";
                    totfinalamt1 = dtsalesinfo.Rows[0]["ETNETAMT"] + "";
                }

                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + " PERSON : " + totperson1 + " , TOTAL AMOUNT : " + totfinalamt1 + " @ " + clsPublicVariables.SMSSIGN;

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "EntryTicketSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "ETS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_EntryTicket_Summary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_CostumeTicket_Summary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                          string eventtext, string eventmobno, string eventemail)
        {
            string str1, wstr_delflg, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";

            string totfinalamt1 = "";
            string totperson1 = "";

            try
            {

                Write_In_Error_Log("GENERATING COSTUME TICKET SUMMARY SMS  [ " + DateTime.Now.ToString() + " ]");

                str1 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(COUPONBILL.delflg,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),COUPONBILL.CBDATE,112) = CONVERT(varchar(10),getdate(),112)";

                str1 = " SELECT COUPONBILL.CBDATE, " +
                           " SUM(ISNULL(COUPONBILL.IQTY,0)) AS TOTQTY, " +
                           " SUM(ISNULL(TOTRENT,0)) AS TOTRENT, " +
                           " SUM(ISNULL(TOTNETAMT,0)) AS TOTNETAMT " +
                           " FROM COUPONBILL " +
                           " LEFT JOIN MSTCOUPONITEM ON (MSTCOUPONITEM.RID=COUPONBILL.IRID) " +
                           " WHERE " + wstr_delflg + " AND " + wstr_date1 +
                           " GROUP BY COUPONBILL.CBDATE";

                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "entryticket");

                if (dtsalesinfo.Rows.Count > 0)
                {
                    totperson1 = dtsalesinfo.Rows[0]["TOTQTY"] + "";
                    //this.txtamount.Text = dtsalesinfo.Rows[0]["ettotamt"] + "";
                    //this.txtdiscount.Text = dtsalesinfo.Rows[0]["etdiscamt"] + "";
                    //this.txttax1.Text = dtsalesinfo.Rows[0]["ettax1amt"] + "";
                    //this.txttax2.Text = dtsalesinfo.Rows[0]["ettax2amt"] + "";
                    //this.txttax3.Text = dtsalesinfo.Rows[0]["ettax3amt"] + "";
                    totfinalamt1 = dtsalesinfo.Rows[0]["TOTRENT"] + "";
                }

                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + " TOTAL COSTUME : " + totperson1 + " , TOTAL RENT : " + totfinalamt1 + " @ " + clsPublicVariables.SMSSIGN;

                // INSERT INTO SMSHISTORY

                string[] strArr = eventmobno.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "CostumeTicketSummary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = strArr[cnt - 1].ToString();
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 0;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                    smsbal.Smstype = "TRANSACTION";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "CTS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_CostumeTicket_Summary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool SMS_CheckIN_SMS_To_RepotingPerson(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                         string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";
            string Checkindate;
            try
            {

                Write_In_Error_Log("GENERATING Check IN Reporting SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    string[] strArr = eventmobno.Split(',');
                    Int64 cnt = 0;

                    for (cnt = 1; cnt <= strArr.Length; cnt++)
                    {
                        foreach (DataRow row1 in dtbirth.Rows)
                        {
                            rid = row1["RID"] + "".ToString();
                            custname = row1["GNAME"] + "".ToString();
                            custmobno = row1["GMOBNO"] + "".ToString();
                            roomno = row1["ROOMNAME"] + "".ToString();
                            Checkindate = Convert.ToDateTime(row1["ARRIDATE"] + "").ToString("dd/MM/yyyy");

                            //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                            //smstext1 = "Dear Guest, It is a great pleasure for us to welcome you at " + clsPublicVariables.GENHOTELNAME;
                            smstext1 = "Guest : " + custname + " is CHECKIN in Room No : " + roomno + " On " + Checkindate;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "CHECKIN2REPO";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1].ToString();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = custname;
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "CHECKIN2REPO" + strArr[cnt - 1].ToString() + rid;
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckOUT_SMS_To_RepotingPerson(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                       string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";
            string Checkoutdate;
            try
            {

                Write_In_Error_Log("GENERATING Check OUT Reporting SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKOUTLIST where DATEPART(d, CKOUTDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, CKOUTDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, CKOUTDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    string[] strArr = eventmobno.Split(',');
                    Int64 cnt = 0;

                    for (cnt = 1; cnt <= strArr.Length; cnt++)
                    {
                        foreach (DataRow row1 in dtbirth.Rows)
                        {
                            rid = row1["RID"] + "".ToString();
                            custname = row1["GNAME"] + "".ToString();
                            custmobno = row1["GMOBNO"] + "".ToString();
                            roomno = row1["ROOMNAME"] + "".ToString();
                            Checkoutdate = Convert.ToDateTime(row1["CKOUTDATE"] + "").ToString("dd/MM/yyyy");

                            //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                            //smstext1 = "Dear Guest, It is a great pleasure for us to welcome you at " + clsPublicVariables.GENHOTELNAME;
                            smstext1 = "Guest : " + custname + " is CHECKOUT in Room No : " + roomno + " On " + Checkoutdate;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "CHECKOUT2REPO";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1].ToString();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = custname;
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "CHECKOUT2REPO" + strArr[cnt - 1].ToString() + rid;
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckOUT_SMS_To_RepotingPerson_WithAmount(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                      string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";
            string Checkoutdate;
            try
            {

                Write_In_Error_Log("GENERATING Check OUT Reporting SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKOUTLIST where DATEPART(d, CKOUTDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, CKOUTDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, CKOUTDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    string[] strArr = eventmobno.Split(',');
                    Int64 cnt = 0;

                    for (cnt = 1; cnt <= strArr.Length; cnt++)
                    {
                        foreach (DataRow row1 in dtbirth.Rows)
                        {

                            rid = row1["RID"] + "".ToString();
                            custname = row1["GNAME"] + "".ToString();
                            custmobno = row1["GMOBNO"] + "".ToString();
                            roomno = row1["ROOMNAME"] + "".ToString();
                            Checkoutdate = Convert.ToDateTime(row1["CKOUTDATE"] + "").ToString("dd/MM/yyyy");
                            Decimal totnetamt1 = 0;
                            Decimal.TryParse(row1["TOTNETAMT"] + "", out totnetamt1);


                            //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                            //smstext1 = "Dear Guest, It is a great pleasure for us to welcome you at " + clsPublicVariables.GENHOTELNAME;
                            smstext1 = "CheckOut Room No : " + roomno + " Amount : " + totnetamt1 + " On " + Checkoutdate;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "CHECKOUT2REPOAMT";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1].ToString();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = custname;
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "CHECKOUT2REPOAMT" + strArr[cnt - 1].ToString() + rid;
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckIN_SHANTIVAN(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun, string eventtext, string eventmobno, string eventemail,
                                            string templateid1, string otherid1, string para1, string para2, string para3)

        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check IN SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".Trim();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        roomno = row1["ROOMNAME"] + "".ToString();

                        smstext1 = "Welcome " + custname + " Thank you for staying at SHANTIVAN RESORT Your Room No. is " + roomno + System.Environment.NewLine +
                                    "We believe you like your stay with us. If you have any needs please contact to Reception helpline no " + para1 + " or call " + para2;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "CheckINSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "CHECKIN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_CheckIN_SHANTIVAN_ReportingPerson(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun, string eventtext, string eventmobno, string eventemail,
                                            string templateid1, string otherid1, string para1, string para2, string para3)

        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING Check IN SMS 2 REPO [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                         " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                if (dtbirth.Rows.Count > 0)
                {
                    string[] strArr = eventmobno.Split(',');
                    Int64 cnt = 0;

                    for (cnt = 1; cnt <= strArr.Length; cnt++)
                    {
                        foreach (DataRow row1 in dtbirth.Rows)
                        {
                            rid = row1["RID"] + "".ToString();
                            custname = row1["GNAME"] + "".Trim();
                            custmobno = row1["GMOBNO"] + "".ToString();
                            roomno = row1["ROOMNAME"] + "".ToString();

                            smstext1 = "Welcome " + custname + " Thank you for staying at SHANTIVAN RESORT Your Room No. is " + roomno + System.Environment.NewLine +
                                        "We believe you like your stay with us. If you have any needs please contact to Reception helpline no " + para1 + " or call " + para2;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "CheckINSMS";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1] + "".Trim();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = custname;
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "CHECKIN" + strArr[cnt - 1] + "".Trim() + rid;
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_ROOMBOOKING_SMS(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun, string eventtext, string eventmobno, string eventemail,
                                         string templateid1, string otherid1, string para1, string para2, string para3)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";



            try
            {

                Write_In_Error_Log("GENERATING BOOKING IN SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMBOOKINGLIST where DATEPART(d, BODATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, BODATE) = DATEPART(m, GETDATE()) " +
                         "  AND DATEPART(yy, BODATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMBOOKINGLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["GNAME"] + "".Trim();
                        custmobno = row1["GMOBNO"] + "".ToString();
                        roomno = row1["ROOMNAME"] + "".ToString();

                        DateTime fromdt1;
                        DateTime.TryParse(row1["ARRIDATE"] + "", out fromdt1);
                        DateTime todt1;
                        DateTime.TryParse(row1["DEPTDATE"] + "", out todt1);

                        smstext1 = "Mr " + custname + " We confirm your reservation at " + para1 + " from " + fromdt1.ToString("dd/MM/yyyy") + " to " + todt1.ToString("dd/MM/yyyy") + " in Room No." + roomno + " For more information about your booking,  Please contact to reception helpline " + para2;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "BOOKINGSMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 0;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "TRANSACTION";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "BOOKING" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SMS_ROOMBOOKING_SMS_ReportingPerson(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun, string eventtext, string eventmobno, string eventemail,
                                        string templateid1, string otherid1, string para1, string para2, string para3)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            string roomno = "";

            try
            {

                Write_In_Error_Log("GENERATING BOOKING IN SMS 2 REPO [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ROOMBOOKINGLIST where DATEPART(d, BODATE) = DATEPART(d, GETDATE())  " +
                          " AND DATEPART(m, BODATE) = DATEPART(m, GETDATE()) " +
                         "  AND DATEPART(yy, BODATE) = DATEPART(yy, GETDATE()) ";

                dtbirth = mssql.FillDataTable(str1, "ROOMBOOKINGLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    string[] strArr = eventmobno.Split(',');
                    Int64 cnt = 0;

                    for (cnt = 1; cnt <= strArr.Length; cnt++)
                    {
                        foreach (DataRow row1 in dtbirth.Rows)
                        {
                            rid = row1["RID"] + "".ToString();
                            custname = row1["GNAME"] + "".Trim();
                            custmobno = row1["GMOBNO"] + "".ToString();
                            roomno = row1["ROOMNAME"] + "".ToString();

                            DateTime fromdt1;
                            DateTime.TryParse(row1["ARRIDATE"] + "", out fromdt1);
                            DateTime todt1;
                            DateTime.TryParse(row1["DEPTDATE"] + "", out todt1);

                            smstext1 = "Mr " + custname + " We confirm your reservation at " + para1 + " from " + fromdt1.ToString("dd/MM/yyyy") + " to " + todt1.ToString("dd/MM/yyyy") + " in Room No." + roomno + " For more information about your booking,  Please contact to reception helpline " + para2;

                            clssmshistoryBal smsbal = new clssmshistoryBal();
                            smsbal.Id = 0;
                            smsbal.Formmode = 0;
                            smsbal.Smsevent = "BOOKINGSMS";
                            smsbal.Smseventid = eventid;
                            smsbal.Mobno = strArr[cnt - 1] + "".Trim();
                            smsbal.Smstext = smstext1;
                            smsbal.Sendflg = 0;
                            smsbal.Smspername = custname;
                            smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                            smsbal.Smstype = "TRANSACTION";
                            smsbal.Resid = "";
                            smsbal.Rmsid = "BOOKING" + strArr[cnt - 1] + "".Trim() + rid;
                            smsbal.Tobesenddatetime = DateTime.Now;
                            smsbal.Loginuserid = 0;

                            if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                            {
                                smsbal.Db_Operation_SMSHISTORY(smsbal);
                            }
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region EMAIL EVENT CODE


        private bool EMAIL_YesterdaySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING YESTERDAY SALES SUMMARY E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                // Billing
                billamt = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate()-1,112)";
                str1 = "select sum(Netamount) as NetAmt from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                billamt = "0";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
                }

                if (billamt.Trim() == "")
                {
                    billamt = "0";
                }

                /// Settlements
                str2 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate()-1,112)";

                str2 = " select " +
                      " sum(setleamount) as NetAmt from settlement " +
                      " where " + wstr_delflg + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsettinfo = mssql.FillDataTable(str2, "settlement");

                settamt = "0";
                if (dtsettinfo.Rows.Count > 0)
                {
                    settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
                }

                if (settamt.Trim() == "")
                {
                    settamt = "0";
                }

                smstext1 = "Date : " + DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy") + ", Billing : " + billamt + ", Settlement : " + settamt + "@" + clsPublicVariables.SMSSIGN;
                //Date : 123, Billing : 123, Settlement : 123

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {

                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Yesterday Sales Summary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILYSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL(strArr[cnt - 1].ToString(), smsbal.Smsevent, smsbal.Smstext, false);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_YesterdaySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_DailySalesSummary(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                            string eventtext, string eventmobno, string eventemail)
        {
            string str1, str2, wstr_delflg, wstr_rev_bill, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            string billamt, settamt = "0";

            try
            {

                Write_In_Error_Log("GENERATING DAILY SALES SUMMARY E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                // Billing
                billamt = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_rev_bill = "isnull(ISREVISEDBILL,0)=0";
                wstr_date1 = "  CONVERT(varchar(10),billdate,112) = CONVERT(varchar(10),getdate(),112)";
                str1 = "select sum(Netamount) as NetAmt from bill  where " + wstr_delflg + " And " + wstr_rev_bill + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsalesinfo = mssql.FillDataTable(str1, "bill");

                billamt = "0";
                if (dtsalesinfo.Rows.Count > 0)
                {
                    billamt = dtsalesinfo.Rows[0]["NetAmt"] + "";
                }

                if (billamt.Trim() == "")
                {
                    billamt = "0";
                }

                /// Settlements
                str2 = "";
                wstr_date1 = "";
                wstr_delflg = "isnull(delflg,0)=0";
                wstr_date1 = "CONVERT(varchar(10),setledate,112) = CONVERT(varchar(10),getdate(),112)";

                str2 = " select " +
                      " sum(setleamount) as NetAmt from settlement " +
                      " where " + wstr_delflg + " And " + wstr_date1;
                mssql.OpenMsSqlConnection();
                dtsettinfo = mssql.FillDataTable(str2, "settlement");

                settamt = "0";
                if (dtsettinfo.Rows.Count > 0)
                {
                    settamt = dtsettinfo.Rows[0]["NetAmt"] + "";
                }

                if (settamt.Trim() == "")
                {
                    settamt = "0";
                }

                smstext1 = "Date : " + DateTime.Today.ToString("dd/MM/yyyy") + ", Billing : " + billamt + ", Settlement : " + settamt + "@" + clsPublicVariables.SMSSIGN;
                //Date : 123, Billing : 123, Settlement : 123

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {

                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "Email - Daily Sales Summary";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = smstext1;
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILDSS" + DateTime.Now.ToString("yyyyMMdd") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL(strArr[cnt - 1].ToString(), smsbal.Smsevent, smsbal.Smstext, false);
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SMS_DailySalesSummary()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_Feedback(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_mobno = "";
            string wstr_delflg = "";
            wstr_delflg = "isnull(MSTFEEDBACK.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),MSTFEEDBACK.FEEDDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "(ISNULL(MSTCUST.CUSTEMAIL,'') <> '')";

            DataTable dtfeedback = new DataTable();

            string custname;
            string custmobno;
            string feedrid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING FEEDBACK E-MAIL [ " + DateTime.Now.ToString() + " ]");

                str1 = "Select MSTFEEDBACK.RID,MSTFEEDBACK.CUSTCONTNO,MSTFEEDBACK.FEEDDATE,MSTCUST.CUSTNAME,MSTCUST.CUSTMOBNO,MSTCUST.CUSTEMAIL From MSTFEEDBACK " +
                        " LEFT JOIN MSTCUST ON (MSTCUST.RID = MSTFEEDBACK.CUSTRID)" +
                        " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtfeedback = mssql.FillDataTable(str1, "MSTFEEDBACK");

                if (dtfeedback.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtfeedback.Rows)
                    {
                        feedrid = row1["RID"] + "".ToString();

                        custname = row1["CUSTNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTCONTNO"] + "".ToString();

                        if (custmobno.Trim() == "")
                        {
                            custmobno = row1["CUSTEMAIL"] + "".ToString();
                        }

                        //custname = row1["CUSTNAME"] + "".ToString();
                        //custmobno = row1["CUSTCONTNO"] + "".ToString();

                        smstext1 = "Dear " + custname + " Thank you for feedback " + clsPublicVariables.SMSSIGN;

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "Email Feedback SMS";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = "";
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = "";
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILFED" + feedrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_BirthdayWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING BIRTHDAY E-MAIL [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from BIRTHDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "BIRTHDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["EMAIL"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Birthday From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Many many happy return of the day if you visit " + clsPublicVariables.SMSSIGN + " will get pestry complementary";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "EMAIL Birthday";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = "";
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILBIR" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_AnniversaryWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                              string eventtext, string eventmobno, string eventemail)
        {
            string str1;

            DataTable dtbirth = new DataTable();

            string custname;
            string custmobno;
            string rid;
            string smstext1 = "";
            //DateTime nextrun1;
            try
            {

                Write_In_Error_Log("GENERATING ANNIVERSARY E-MAIL [ " + DateTime.Now.ToString() + " ]");

                str1 = " select * from ANNIDATESMSALERTLIST ";

                dtbirth = mssql.FillDataTable(str1, "ANNIDATESMSALERTLIST");

                if (dtbirth.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbirth.Rows)
                    {
                        rid = row1["RID"] + "".ToString();
                        custname = row1["ENAME"] + "".ToString();
                        custmobno = row1["EMAIL"] + "".ToString();

                        //smstext1 = "Dear Member," + custname + " Happy Anniversary From:" + clsPublicVariables.SMSSIGN;
                        smstext1 = "Many many happy anniversary if you visit " + clsPublicVariables.SMSSIGN + " will get pestry complementary";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "EMAIL Anniversary";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILANN" + rid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_BillWishes(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                           string eventtext, string eventmobno, string eventemail)
        {
            string str1;
            string wstr_date1 = "";
            string wstr_delflg = "";
            string wstr_mobno = "";
            wstr_delflg = "isnull(BILL.delflg,0)=0";
            wstr_date1 = "CONVERT(varchar(10),BILL.BILLDATE,112) = CONVERT(varchar(10),getdate(),112)";
            wstr_mobno = "( ISNULL(MSTCUST.CUSTEMAIL,'') <>'')";
            DataTable dtbill = new DataTable();

            string custname;
            string custmobno;
            string billrid;
            string smstext1 = "";
            //DateTime nextrun1;
            String Netamt1 = "0";

            try
            {

                Write_In_Error_Log("GENERATING Bill Wishes SMS [ " + DateTime.Now.ToString() + " ]");

                str1 = "SELECT BILL.RID,BILL.BILLNO,BILL.BILLDATE,ISNULL(BILL.NETAMOUNT,0) AS NETAMOUNT,ISNULL(BILL.CUSTCONTNO,'') AS CUSTCONTNO, " +
                            " ISNULL(MSTCUST.CUSTNAME,'') AS CUTOMERNAME,ISNULL(MSTCUST.CUSTMOBNO,'') AS  CUSTMOBNO, ISNULL(MSTCUST.CUSTEMAIL,'') AS  CUSTEMAIL From Bill" +
                             " LEFT JOIN MSTCUST ON (MSTCUST.RID = BILL.CUSTRID) " +
                             " Where " + wstr_delflg + " And " + wstr_date1 + " And " + wstr_mobno;

                dtbill = mssql.FillDataTable(str1, "BILL");

                Netamt1 = "0";

                if (dtbill.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtbill.Rows)
                    {
                        billrid = row1["RID"] + "".ToString();

                        custname = row1["CUTOMERNAME"] + "".ToString();

                        if (custname.Trim() == "")
                        {
                            custname = "Customer";
                        }

                        custmobno = row1["CUSTEMAIL"] + "".ToString();

                        //if (custmobno.Trim() == "")
                        //{
                        //    custmobno = row1["CUSTMOBNO"] + "".ToString();
                        //}

                        Netamt1 = row1["NETAMOUNT"] + "".ToString();

                        //smstext1 = custname + " - Thanks for visiting " + clsPublicVariables.SMSSIGN + " see you again";
                        smstext1 = "Thanks for visiting " + clsPublicVariables.SMSSIGN + " your bill amount is " + Netamt1 + " looking for next visit";

                        clssmshistoryBal smsbal = new clssmshistoryBal();
                        smsbal.Id = 0;
                        smsbal.Formmode = 0;
                        smsbal.Smsevent = "EMAIL Bill Wishes";
                        smsbal.Smseventid = eventid;
                        smsbal.Mobno = custmobno;
                        smsbal.Smstext = smstext1;
                        smsbal.Sendflg = 1;
                        smsbal.Smspername = custname;
                        smsbal.Smsaccuserid = clsPublicVariables.TRAUSERID;
                        smsbal.Smstype = "EMAIL";
                        smsbal.Resid = "";
                        smsbal.Rmsid = "EMAILBIL" + billrid;
                        smsbal.Tobesenddatetime = DateTime.Now;
                        smsbal.Loginuserid = 0;

                        if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                        {
                            smsbal.Db_Operation_SMSHISTORY(smsbal);
                            this.SEND_EMAIL(custmobno, smsbal.Smsevent, smsbal.Smstext, false);
                        }
                    }
                }

                //nextrun1 = GetEventLastRunTime(eventid, eventruntype, eventinterval, eventstartdate, eventlastrun);
                //str1 = " UPDATE EVENTSCHEDULER SET EVENTLASTRUN = '" + nextrun1.ToString("yyyy/MM/dd HH:mm") + "' WHERE RID = " + eventid;
                //mssql.ExecuteMsSqlCommand(str1);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool EMAIL_CheckIN(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                        string eventtext, string eventmobno, string eventemail)
        {
            //string str1, str2, wstr_delflg, EMAIL_CheckIN, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";
            //string billamt, settamt = "0";

            try
            {

                // Write_In_Error_Log("GENERATING PURCHASE STOCK REGISTER E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_CheckInRegister(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + ("CHECK IN : " + clsPublicVariables.GENHOTELNAME);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "CHECK IN";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "CHECK IN : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILCHECKIN" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL(strArr[cnt - 1].ToString(), smsbal.Smsevent, smstext1, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_CheckIN()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private bool EMAIL_CheckOUT(Int64 eventid, string eventruntype, string eventinterval, string eventstartdate, string eventlastrun,
                                       string eventtext, string eventmobno, string eventemail)
        {
            //string str1, str2, wstr_delflg, EMAIL_CheckIN, wstr_date1;

            DataTable dtsalesinfo = new DataTable();
            DataTable dtsettinfo = new DataTable();

            string smstext1 = "";


            try
            {

                // Write_In_Error_Log("GENERATING PURCHASE STOCK REGISTER E-MAIL  [ " + DateTime.Now.ToString() + " ]");

                smstext1 = this.Generate_CheckOutRegister(DateTime.Now.Date, DateTime.Now.Date);

                if (smstext1.Trim() == "")
                {
                    smstext1 = (smstext1 == "" ? "" : smstext1 + Environment.NewLine) + ("CHECK OUT : " + clsPublicVariables.GENHOTELNAME);
                }

                // INSERT INTO SMSHISTORY

                string[] strArr = eventemail.Split(',');
                Int64 cnt = 0;

                for (cnt = 1; cnt <= strArr.Length; cnt++)
                {
                    clssmshistoryBal smsbal = new clssmshistoryBal();
                    smsbal.Id = 0;
                    smsbal.Formmode = 0;
                    smsbal.Smsevent = "CHECK OUT";
                    smsbal.Smseventid = eventid;
                    smsbal.Mobno = "";
                    smsbal.Smstext = "CHECK OUT : " + DateTime.Now.ToString();
                    smsbal.Sendflg = 1;
                    smsbal.Smspername = "";
                    smsbal.Smsaccuserid = "";
                    smsbal.Smstype = "E-MAIL";
                    smsbal.Resid = "";
                    smsbal.Rmsid = "EMAILCHECKOUT" + DateTime.Now.ToString("yyyyMMdd hh:mm:ss") + strArr[cnt - 1].ToString();
                    smsbal.Tobesenddatetime = DateTime.Now;
                    smsbal.Loginuserid = 0;

                    if (Check_SMS_Exit(smsbal.Rmsid, eventid))
                    {
                        smsbal.Db_Operation_SMSHISTORY(smsbal);
                        this.SEND_EMAIL(strArr[cnt - 1].ToString(), smsbal.Smsevent, smstext1, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in EMAIL_CheckOUT()) " + DateTime.Now.ToString());
                return false;
            }
        }


        #endregion

        private void tmrsms_Tick(object sender, EventArgs e)
        {
            this.Send_SMS_Scheduler();
        }

        private bool Send_SMS_Scheduler()
        {
            String str1, str2, wstr_date1 = "";
            DataTable dtsms = new DataTable();
            Int64 rid1 = 0;

            String sms1 = "";
            String mob1 = "";
            String response = "";

            try
            {

                // Write_In_Error_Log("CHECKING FOR NEW GENERATED SMS [ " + DateTime.Now.ToString() + " ]");

                wstr_date1 = "CONVERT(varchar(10),TOBESENDDATETIME,112) <= CONVERT(varchar(10),getdate(),112)";

                str1 = "SELECT * FROM SMSHISTORY WHERE ISNULL(SENDFLG,0)=0 AND " + wstr_date1 + " Order by TOBESENDDATETIME,SMSEVENTID,RID";
                dtsms = mssql.FillDataTable(str1, "SMSHISTORY");

                if (dtsms.Rows.Count > 0)
                {
                    foreach (DataRow row1 in dtsms.Rows)
                    {
                        Int64.TryParse(row1["RID"] + "".ToString(), out rid1);
                        sms1 = row1["SMSTEXT"] + "".ToString();
                        mob1 = row1["MOBNO"] + "".ToString();
                        response = this.SENDSMS_TRANSACTIONAL(sms1, mob1);
                        str2 = "UPDATE SMSHISTORY SET SENDFLG = 1,RESID = '" + response + "' , SENDDATETIME = '" + DateTime.Now.ToString("yyyy/MM/dd HH:MM") + "' WHERE RID = " + rid1;
                        Write_In_Error_Log("SMS ( " + sms1 + " ) SEND. @ [" + DateTime.Now.ToString() + " ]");
                        mssql.ExecuteMsSqlCommand(str2);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in Send_SMS()) " + DateTime.Now.ToString());
                return false;
            }
        }

        private string SENDSMS_TRANSACTIONAL(string smstext, string mobno1)
        {
            string smsurl = "";
            string smsresponse = "ERROR";

            try
            {
                if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "ASK4SMS")
                {
                    mobno1 = "91" + objclsgen.RightString(mobno1, 10);
                    smstext = smstext.Replace("&", "%26");
                    smstext = smstext.Replace("$", "%24");
                    smstext = smstext.Replace("@", "%40");
                    smstext = smstext.Replace("!", "%21");
                    smstext = smstext.Replace("?", "%3F");
                    smstext = smstext.Replace("/", "%2F");
                    smstext = smstext.Replace("-", "%2D");
                    smstext = smstext.Replace("*", "%2A");
                    smsurl = "http://api.ask4sms.com/sms/1/text/query?username=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&to=" + mobno1 + "&text=" + smstext + "&from=" + clsPublicVariables.SMSSIGN;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "FECUND")
                {
                    smsurl = "http://sms.fecundtechno.com/sendsms.aspx?mobile=" + clsPublicVariables.TRAUSERID + "&pass=" + clsPublicVariables.TRAPASSWORD + "&senderid=" + clsPublicVariables.SMSSIGN + "&to=" + mobno1 + "&msg=" + smstext;
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSINDIAHUB")
                {
                    smsurl = "http://cloud.smsindiahub.in/vendorsms/pushsms.aspx?user=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&msisdn=" + mobno1 + "&sid=" + clsPublicVariables.SMSSIGN + "&msg=" + smstext + "&fl=0";
                }
                else if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "OMNIINFOSYSTEM")
                {
                    smstext = smstext.Replace("&", "%26");
                    smsurl = "http://esms.ominfosys.net/api/push?apikey=616564a93f331&route=Transactional&sender=" + clsPublicVariables.SMSSIGN + "&mobileno=" + mobno1 + "&text=" + smstext;
                }
                else
                {
                    smstext = smstext.Replace("&", "%26");
                    smsurl = "http://enterprise.smsgupshup.com/GatewayAPI/rest?msg=" + smstext + "&Message&v=1.1&userid=" + clsPublicVariables.TRAUSERID + "&password=" + clsPublicVariables.TRAPASSWORD + "&send_to=" + mobno1 + "&msg_type=text&method=sendMessage";
                }

                if (clsPublicVariables.TRAPROVIDER.ToUpper().Trim() == "SMSINDIAHUB")
                {
                    smsresponse = objclsgen.SEND_WEB_REQUEST(smsurl);
                    smsresponse = smsresponse.Substring(0, 50);
                }
                else
                {
                    smsresponse = objclsgen.SEND_WEB_REQUEST(smsurl);
                }
                return smsresponse;
            }
            catch (Exception ex)
            {
                Write_In_Error_Log(ex.Message.ToString() + " Error occures in SENDSMS_TRANSACTIONAL()) " + DateTime.Now.ToString());
                return smsresponse;
            }
        }

        private bool SEND_EMAIL(string emailto1, string subject1, string smstext1, bool isbodyhtml)
        {
            MailMessage message;
            SmtpClient smtp;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                message = new MailMessage();

                message.To.Add(emailto1);
                //message.CC.Add(this.txtcc.Text.Trim());

                message.Subject = subject1;

                message.From = new MailAddress(clsPublicVariables.GENSMTPEMAILADDRESS);
                message.Body = smstext1;
                if (isbodyhtml)
                {
                    message.IsBodyHtml = true;
                }

                //if (lblattachments.Tag.ToString().Length > 0)
                //{
                //    if (System.IO.File.Exists(lblattachments.Tag.ToString()))
                //    {
                //        message.Attachments.Add(new Attachment(lblattachments.Tag.ToString()));
                //    }
                //}

                // set smtp details
                smtp = new SmtpClient(clsPublicVariables.GENSMTPADDRESS);

                Int32 gensmtpport;
                Int32.TryParse(clsPublicVariables.GENSMTPPORT, out gensmtpport);
                smtp.Port = gensmtpport;

                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(clsPublicVariables.GENSMTPEMAILADDRESS, clsPublicVariables.GENSMTPEMAILPASSWORD);
                smtp.SendAsync(message, message.Subject);
                smtp.SendCompleted += new SendCompletedEventHandler(smtp_SendCompleted);

                return true;
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                //MessageBox.Show("Error occured in SEND_EMAIL() : " + ex.Message.ToString(), clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Write_In_Error_Log("Error occured in SEND_EMAIL() : " + ex.Message.ToString() + DateTime.Now.ToString());
                return false;
            }
        }

        void smtp_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                //Cursor.Current = Cursors.WaitCursor;

                if (e.Cancelled == true)
                {
                    //Cursor.Current = Cursors.Default;
                    //MessageBox.Show("Email sending cancelled.", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Write_In_Error_Log("Email sending cancelled.");
                }
                else if (e.Error != null)
                {
                    //Cursor.Current = Cursors.Default;
                    //MessageBox.Show("Error occured in Sending E-Mail : " + e.Error.Message, clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Write_In_Error_Log("Error occured in Sending E-Mail : " + e.Error.Message);
                }
                else
                {
                    //Cursor.Current = Cursors.Default;
                    //MessageBox.Show("E-Mail Sent Sucessfully.", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Write_In_Error_Log("E-Mail Sent Sucessfully.");
                }
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                //MessageBox.Show("Error occured in smtp_SendCompleted()." + ex.Message.ToString(), clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Write_In_Error_Log("Error occured in smtp_SendCompleted()." + ex.Message.ToString());

            }
        }

        private void frmeventserver_Load(object sender, EventArgs e)
        {
            try
            {
                clsPublicVariables.EventVer = System.Diagnostics.FileVersionInfo.GetVersionInfo(clsPublicVariables.ActualFilePath).FileVersion.ToString();
                this.Text = "EVENT SCHEDULER SERVER [ Version : " + clsPublicVariables.EventVer + " ] Date : " + DateTime.Today.ToShortDateString();

                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg.ToUpper() == "AUTO")
                    {
                        clsPublicVariables.LoginUserId = 1;
                        clsPublicVariables.LoginUserName = "AdminAuto";
                        clsPublicVariables.LoginDatetime = System.DateTime.Now;
                        clsPublicVariables.ISAUTOSTART = true;
                    }
                }

                if (clsPublicVariables.ISAUTOSTART)
                {
                    this.btnrun_Click(sender, e);
                }

            }
            catch (Exception)
            { }
        }

        //private bool Send_Web_Request()
        //{
        //    try
        //    {
        //        string strUrl = "http://api.mVaayoo.com/mvaayooapi/MessageCompose?user=Username:Password&senderID=mVaayoo&receipientno=919849558211&msgtxt=This is a test frommVaayoo API&state=4";
        //        WebRequest request = HttpWebRequest.Create(strUrl);
        //        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse();
        //        Stream s = (Stream)response.GetResponseStream();
        //        StreamReader readStream = new StreamReader(s);
        //        string dataString = readStream.ReadToEnd();
        //        response.Close();
        //        s.Close();
        //        readStream.Close();
        //    }
        //    catch (Exception)
        //    {

        //    }

        //}

        private string Generate_CheckInRegister(DateTime Fromdate, DateTime Todate)
        {
            DataTable dtchekin = new DataTable();
            string str1 = "";
            string textBody = "";

            try
            {

                str1 = " select * from ROOMCHECKINlist where DATEPART(d, ARRIDATE) = DATEPART(d, GETDATE())  " +
                         " AND DATEPART(m, ARRIDATE) = DATEPART(m, GETDATE()) " +
                        " AND DATEPART(yy, ARRIDATE) = DATEPART(yy, GETDATE()) ";

                dtchekin = mssql.FillDataTable(str1, "ROOMCHECKINlist");

                textBody = "";
                if (dtchekin.Rows.Count > 0)
                {
                    textBody = textBody + " <table border=" + 1 + " cellpadding=" + 0 + " cellspacing=" + 0 + " width = " + 400 + "><tr bgcolor='#4da6ff'> " +
                                   " <td><b>REG NO</b></td>" +
                                   " <td><b>ARRIVAL DATE</b></td>" +
                                   " <td><b>ROOM NO</b></td>" +
                                   " <td><b>GUEST NAME</b></td>" +
                                   " <td><b>MOBILE NO</b></td>" +
                                   "</tr>";

                    for (int loopCount = 0; loopCount < dtchekin.Rows.Count; loopCount++)
                    {
                        textBody += "<tr><td>" + (dtchekin.Rows[loopCount]["REGNO"] + "") + "</td> " +
                                        "<td> " + (dtchekin.Rows[loopCount]["ARRIDATE"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["ROOMNAME"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["GNAME"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["GMOBNO"] + "") + "</td>" +
                                        "</tr>";
                    }
                    textBody += "</table>";

                    return textBody;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in Generate_CheckInRegister())", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "";
            }
        }

        private string Generate_CheckOutRegister(DateTime Fromdate, DateTime Todate)
        {
            DataTable dtchekin = new DataTable();
            string str1 = "";
            string textBody = "";

            try
            {

                str1 = " select * from ROOMCHECKOUTLIST where DATEPART(d, CKOUTDATE) = DATEPART(d, GETDATE())  " +
                         " AND DATEPART(m, CKOUTDATE) = DATEPART(m, GETDATE()) " +
                        " AND DATEPART(yy, CKOUTDATE) = DATEPART(yy, GETDATE()) ";

                dtchekin = mssql.FillDataTable(str1, "ROOMCHECKOUTLIST");

                textBody = "";

                if (dtchekin.Rows.Count > 0)
                {
                    textBody = textBody + " <table border=" + 1 + " cellpadding=" + 0 + " cellspacing=" + 0 + " width = " + 400 + "><tr bgcolor='#4da6ff'> " +
                                   " <td><b>BILL NO</b></td>" +
                                   " <td><b>CHECKOUT DATE</b></td>" +
                                   " <td><b>CHECKOUT TIME</b></td>" +
                                   " <td><b>ROOM NO</b></td>" +
                                   " <td><b>GUEST NAME</b></td>" +
                                   " <td><b>MOBILE NO</b></td>" +
                                   " <td><b>NET AMOUNT</b></td>" +
                                   "</tr>";

                    for (int loopCount = 0; loopCount < dtchekin.Rows.Count; loopCount++)
                    {
                        textBody += "<tr><td>" + (dtchekin.Rows[loopCount]["BILLNO"] + "") + "</td> " +
                                        "<td> " + (dtchekin.Rows[loopCount]["CKOUTDATE"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["CKOUTTIME"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["ROOMNAME"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["GNAME"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["GMOBNO"] + "") + "</td>" +
                                        "<td> " + (dtchekin.Rows[loopCount]["TOTNETAMT"] + "") + "</td>" +
                                        "</tr>";
                    }

                    textBody += "</table>";

                    return textBody;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString() + " Error occures in Generate_CheckOutRegister())", clsPublicVariables.Project_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "";
            }
        }

        private void btnclear_Click(object sender, EventArgs e)
        {
            this.txtinfo.Text = "";
        }
    }
}
