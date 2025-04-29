﻿namespace MaxSystemWebSite.Models.EMAIL
{
    public class SP_EmployeeInformation
    {
        public SP_EmployeeInformation()
        {
                
        }
        public SP_EmployeeInformation(string eMP_ID,string eMP_NAME, string eMP_EMAIL, string eMP_NO, int dAY_JOINED, string dESIGNATION, string dEPARTMENT, DateTime? jOIN_DATE, string tMS_MANAGER, string tEAM, string lEADER, DateTime? rESIGNATION_DATE, DateTime? lAST_WORKING_DATE, DateTime? bIRTH_DATE, string eMP_PHOTO, string eMP_STATUS, string gENDER, string rACE, string mARITAL_STATUS, string hIGH_EDUCATION, double lEAVE_ANUAL, double fORECAST_TOTAL_DAYS, bool sURVEY_SUBMISSION)
        {
            EMP_ID = eMP_ID;
            EMP_NAME = eMP_NAME;
            EMP_EMAIL = eMP_EMAIL;
            EMP_NO = eMP_NO;
            DAY_JOINED = dAY_JOINED;
            DESIGNATION = dESIGNATION;
            DEPARTMENT = dEPARTMENT;
            JOIN_DATE = jOIN_DATE;
            TMS_MANAGER = tMS_MANAGER;
            TEAM = tEAM;
            LEADER = lEADER;
            RESIGNATION_DATE = rESIGNATION_DATE;
            LAST_WORKING_DATE = lAST_WORKING_DATE;
            BIRTH_DATE = bIRTH_DATE;
            EMP_PHOTO = eMP_PHOTO;
            EMP_STATUS = eMP_STATUS;
            GENDER = gENDER;
            RACE = rACE;
            MARITAL_STATUS = mARITAL_STATUS;
            HIGH_EDUCATION = hIGH_EDUCATION;
            LEAVE_ANUAL = lEAVE_ANUAL;
            FORECAST_TOTAL_DAYS = fORECAST_TOTAL_DAYS;
            SURVEY_SUBMISSION = sURVEY_SUBMISSION;
        }
        public string EMP_ID { get; set; }
        public string EMP_NAME { get; set; }
        public string EMP_EMAIL { get; set; }
        public string EMP_NO { get; set; }
        public int DAY_JOINED { get; set; }
        public string DESIGNATION { get; set; }
        public string DEPARTMENT { get; set; }
        public DateTime? JOIN_DATE { get; set; }
        public string TMS_MANAGER { get; set; }
        public string TEAM { get; set; }
        public string LEADER { get; set; }
        public DateTime? RESIGNATION_DATE { get; set; }
        public DateTime? LAST_WORKING_DATE { get; set; }
        public DateTime? BIRTH_DATE { get; set; }
        public string EMP_PHOTO { get; set; }
        public string EMP_STATUS { get; set; }
        public string GENDER { get; set; }
        public string RACE { get; set; }
        public string MARITAL_STATUS { get; set; }
        public string HIGH_EDUCATION { get; set; }
        public double LEAVE_ANUAL { get; set; }
        public double FORECAST_TOTAL_DAYS { get; set; }
        public bool SURVEY_SUBMISSION { get; set; }

    }
}
