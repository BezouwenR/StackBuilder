﻿#region Using directives
// Docking
// log4net
using log4net;

using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Desktop.Properties;
#endregion

namespace treeDiM.StackBuilder.Desktop
{
    public partial class DockContentAnalysisCaseTruck : DockContentAnalysisEdit
    {
        #region Data members
        /// <summary>
        /// logger
        /// </summary>
        static readonly new ILog _log = LogManager.GetLogger(typeof(DockContentAnalysisCaseTruck));
        #endregion

        #region Constructor
        public DockContentAnalysisCaseTruck(IDocument doc, AnalysisCaseTruck analysis)
            : base(doc, analysis)
        {
            InitializeComponent();
        }
        #endregion

        #region Form override
        #endregion

        #region Override DockContentAnalysisEdit
        public override string GridCaption
        {   get { return Resources.ID_TRUCK; } }
        #endregion
    }
}
