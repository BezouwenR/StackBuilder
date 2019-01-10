﻿namespace treeDiM.StackBuilder.Reporting
{
    public class ReporterHtml : Reporter
    {
        #region Constructor
        /// <summary>
        /// ReportHtml : generate html report
        /// </summary>
        public ReporterHtml(ReportData inputData, ref ReportNode rnRoot, string templatePath, string outpuFilePath)
        {
            BuildAnalysisReport(inputData, ref rnRoot, templatePath, outpuFilePath);
        }
        #endregion

        #region Override Reporter
        public override bool WriteNamespace => false;
        public override bool WriteImageFiles => true;
        #endregion
    }
}
