﻿#region Using directives
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using Excel = Microsoft.Office.Interop.Excel;

using Sharp3D.Math.Core;

using treeDiM.Basics;
using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Graphics;
using treeDiM.StackBuilder.Engine;

using treeDiM.StackBuilder.ExcelAddIn.Properties;
#endregion

namespace treeDiM.StackBuilder.ExcelAddIn
{
    public partial class UCtrlPerSheetAnalysis : UserControl
    {
        public UCtrlPerSheetAnalysis()
        {
            InitializeComponent();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                // show case in uCtrlCaseOrientation
                if (!(Globals.StackBuilderAddIn.Application.ActiveSheet is Excel.Worksheet xlSheet)) return;
                var caseLength = ReadDouble(xlSheet, Settings.Default.CellCaseLength, Resources.ID_CASE_LENGTH);
                var caseWidth = ReadDouble(xlSheet, Settings.Default.CellCaseWidth, Resources.ID_CASE_WIDTH);
                var caseHeight = ReadDouble(xlSheet, Settings.Default.CellCaseHeight, Resources.ID_CASE_HEIGHT);

                var bProperties = new BoxProperties(null, caseLength, caseWidth, caseHeight);
                bProperties.SetColor(Color.Chocolate);
                bProperties.TapeWidth = new OptDouble(true, UnitsManager.ConvertLengthFrom(50.0, UnitsManager.UnitSystem.UNIT_METRIC1));
                bProperties.TapeColor = Color.Beige;
                uCtrlCaseOrientation.BProperties = bProperties;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void OnCompute(object sender, EventArgs e)
        {
            try
            {
                if (Globals.StackBuilderAddIn.Application.ActiveSheet is Excel.Worksheet xlSheet)
                {
                    Console.WriteLine($"Sheet name = {xlSheet.Name}");

                    var caseLength = ReadDouble(xlSheet, Settings.Default.CellCaseLength, Resources.ID_CASE_LENGTH);
                    var caseWidth = ReadDouble(xlSheet, Settings.Default.CellCaseWidth, Resources.ID_CASE_WIDTH);
                    var caseHeight = ReadDouble(xlSheet, Settings.Default.CellCaseHeight, Resources.ID_CASE_HEIGHT);
                    var caseWeight = ReadDouble(xlSheet, Settings.Default.CellCaseWeight, Resources.ID_CASE_WEIGHT);

                    var palletLength = ReadDouble(xlSheet, Settings.Default.CellPalletLength, Resources.ID_PALLET_LENGTH);
                    var palletWidth = ReadDouble(xlSheet, Settings.Default.CellPalletWidth, Resources.ID_PALLET_WIDTH);
                    var palletHeight = ReadDouble(xlSheet, Settings.Default.CellPalletHeight, Resources.ID_PALLET_HEIGHT);
                    var palletWeight = ReadDouble(xlSheet, Settings.Default.CellPalletWeight, Resources.ID_PALLET_WEIGHT);

                    var palletMaximumHeight = ReadDouble(xlSheet, Settings.Default.CellMaxPalletHeight, Resources.ID_CONSTRAINTS_PALLETMAXIHEIGHT);
                    var palletMaximumWeight = ReadDouble(xlSheet, Settings.Default.CellMaxPalletWeight, Resources.ID_CONSTRAINTS_PALLETMAXIWEIGHT);

                    // ### actually compute result ###
                    // build a case
                    BoxProperties bProperties = new BoxProperties(null, caseLength, caseWidth, caseHeight);
                    bProperties.SetWeight(caseWeight);
                    bProperties.SetColor(Color.Chocolate);
                    bProperties.TapeWidth = new OptDouble(true, UnitsManager.ConvertLengthFrom(50.0, UnitsManager.UnitSystem.UNIT_METRIC1));
                    bProperties.TapeColor = Color.Beige;

                    // build a pallet
                    PalletProperties palletProperties = new PalletProperties(null, PalletTypeName, palletLength, palletWidth, palletHeight)
                    {
                        Weight = palletWeight,
                        Color = Color.Yellow
                    };

                    // build a constraint set
                    bool[] allowedOrientations = { true, true, true };
                    try { allowedOrientations = uCtrlCaseOrientation.AllowedOrientations; } catch (Exception ex) { Console.WriteLine(ex.Message); }

                    ConstraintSetCasePallet constraintSet = new ConstraintSetCasePallet();
                    constraintSet.SetAllowedOrientations(allowedOrientations);
                    constraintSet.SetMaxHeight(new OptDouble(true, palletMaximumHeight));
                    constraintSet.OptMaxWeight = new OptDouble(true, palletMaximumWeight);

                    // use a solver and get a list of sorted analyses + select the best one
                    var solver = new SolverCasePallet(bProperties, palletProperties, constraintSet);
                    var analyzes = solver.BuildAnalyses(true);
                    if (analyzes.Count > 0)
                    {
                        var analysis = analyzes[0];
                        int caseCount = analysis.Solution.ItemCount;      // <- your case count
                        var loadWeight = analysis.Solution.LoadWeight;
                        var totalWeight = analysis.Solution.Weight;   // <- your pallet weight

                        Graphics3DImage graphics = null;
                        // generate image path
                        var stackImagePath = Path.Combine(Path.ChangeExtension(Path.GetTempFileName(), "png"));
                        graphics = new Graphics3DImage(new Size(Settings.Default.ImageSize, Settings.Default.ImageSize))
                        {
                            FontSizeRatio = Settings.Default.FontSizeRatio,
                            CameraPosition = Graphics3D.Corner_0
                        };
                        ViewerSolution sv = new ViewerSolution(analysis.SolutionLay);
                        sv.Draw(graphics, Transform3D.Identity);
                        graphics.Flush();
                        Bitmap bmp = graphics.Bitmap;
                        bmp.Save(stackImagePath);


                        WriteInt(xlSheet, Settings.Default.CellNoCases, Resources.ID_RESULT_NOCASES, caseCount);
                        WriteDouble(xlSheet, Settings.Default.CellLoadWeight, Resources.ID_RESULT_LOADWEIGHT, loadWeight);
                        WriteDouble(xlSheet, Settings.Default.CellTotalPalletWeight, Resources.ID_RESULT_TOTALPALLETWEIGHT, totalWeight);

                        Globals.StackBuilderAddIn.Application.ActiveSheet.Shapes.AddPicture(
                            stackImagePath,
                            Microsoft.Office.Core.MsoTriState.msoFalse,
                            Microsoft.Office.Core.MsoTriState.msoCTrue,
                            Settings.Default.ImageLeft / 0.035, Settings.Default.ImageTop / 0.035,
                            Settings.Default.ImageWidth / 0.035, Settings.Default.ImageHeight / 0.035);
                    }
                    // ###
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        #region Static member functions
        internal static string TaskPaneTitle => "Stackbuilder Per Sheet Analysis";
        #endregion

        private string PalletTypeName => "EUR2";

        #region Helpers
        private double ReadDouble(Excel.Worksheet wSheet, string cellName, string vName)
        {
            try
            {
                var cell = wSheet.Range[cellName, Type.Missing];
                var content = cell.Value2;
                if (content is double)
                    return (double)content;
                else
                    throw new Exception("Not a double");
            }
            catch (Exception ex)
            {
                throw new ExceptionCellReading(vName, cellName, ex.Message);
            }
        }
        private void WriteInt(Excel.Worksheet wSheet, string cellName, string vName, int v)
        {
            try
            {
                var cell = wSheet.Range[cellName, Type.Missing];
                cell.Value2 = v;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void WriteDouble(Excel.Worksheet wSheet, string cellName, string vName, double v)
        {
            try
            {
                var cell = wSheet.Range[cellName, Type.Missing];
                cell.Value2 = v;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
