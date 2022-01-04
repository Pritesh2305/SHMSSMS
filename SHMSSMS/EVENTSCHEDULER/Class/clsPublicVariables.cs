﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace EVENTSCHEDULER
{
    class clsPublicVariables
    {
        public static string AppPath = Path.GetDirectoryName(Application.ExecutablePath).ToString();
        public static string Project_Title = "KRUPA INFOTECH";

        //public static string DatabaseType1;
        public static string ServerName1;
        public static string DatabaseName1;
        public static string UserName1;
        public static string Password1;
       // public static string DataPort1;

        public static string PROPROVIDER;
        public static string PROUSERID;
        public static string PROPASSWORD;
        public static string TRAPROVIDER;
        public static string TRAUSERID;
        public static string TRAPASSWORD;
        public static string SMSSIGN;
        //public static string SENDSMS;
        public static string GENSMTPEMAILADDRESS;
        public static string GENSMTPEMAILPASSWORD;
        public static string GENSMTPADDRESS;
        public static string GENSMTPPORT;
        public static string ActualFilePath;
        public static string EventVer;
        public static string GENHOTELNAME;
        public static string GENSMSREMARK1;
        public static string GENSMSREMARK2;
        public static string GENSMSREMARK3;
        public static string GENSMSREMARK4;

        public static string GENPROSENDERID;


        public static long LoginUserId;
        public static string LoginUserName;
        public static DateTime LoginDatetime;
        public static bool ISAUTOSTART = false;

    }
}
