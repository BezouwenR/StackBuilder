﻿#region Using directives
using System;
using System.Linq;
using System.Collections.Generic;

using System.Xml;
using System.Xml.Xsl;
using System.Xml.Schema;
using System.IO;

using System.Drawing;
using System.Reflection;

using log4net;

using Sharp3D.Math.Core;

using treeDiM.Basics;
using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Graphics;
using treeDiM.StackBuilder.Reporting.Properties;
#endregion

namespace treeDiM.StackBuilder.Reporting
{
    #region MyXmlUrlResolver
    class MyXmlUrlResolver : XmlUrlResolver
    {
        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (baseUri != null)
                return base.ResolveUri(baseUri, relativeUri);
            else
                return base.ResolveUri(new Uri(@"D:\GitHub\StackBuilder\treeDiM.StackBuilder.Reporting\ReportTemplates"), relativeUri);
        }
    }
    #endregion

    #region Exceptions
    public class ReportExceptionInvalidAnalysis : Exception
    {
        public ReportExceptionInvalidAnalysis()
        { 
        }
    }

    public class ReportExceptionUnexpectedItem : Exception
    {
        #region Constructor
        public ReportExceptionUnexpectedItem(ItemBase item)
            : base(string.Format("Unexpected item : {0}", item.Name))
        {
        }
        #endregion
    }
    #endregion

    #region Reporter
    /// <summary>
    /// Generates pallet analyses reports
    /// </summary>
    abstract public class Reporter
    {
        #region Enums
        public enum EImageSize
        {
            IMAGESIZE_DEFAULT
            , IMAGESIZE_SMALL
        };
        #endregion

        #region Data members
        protected static readonly ILog _log = LogManager.GetLogger(typeof(Reporter));
        #endregion

        #region Abstract members
        abstract public bool WriteNamespace { get; }
        abstract public bool WriteImageFiles { get; }
        #endregion

        #region Public properties
        static public string CompanyLogo
        {
            get
            {
                string companyLogo = Settings.Default.CompanyLogoPath;
                if (!File.Exists(companyLogo))
                {
                    companyLogo = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                        , "ReportTemplates\\YourLogoHere.png");
                    Settings.Default.CompanyLogoPath = companyLogo;
                    Settings.Default.Save();

                    if (!File.Exists(companyLogo))
                        return string.Empty;
                }
                return companyLogo; 
            }
        }
        public static void SetFontSizeRatios(float fontSizeRatioDetail, float fontSizeRatioLarge)
        { FontSizeRatioDetail = fontSizeRatioDetail; FontSizeRatioLarge = fontSizeRatioLarge; }
        public static void SetImageSize(int imgSizeDetail, int imgSizeLarge)
        { ImageSizeDetail = imgSizeDetail; ImageSizeLarge = imgSizeLarge; }
        public static void SetImageHTMLSize(int imgHTMLDetail, int imgHTMLLarge)
        { ImageHTMLSizeDetail = imgHTMLDetail; ImageHTMLSizeLarge = imgHTMLLarge; }
        public static string TemplatePath
        {
            get
            {
                string templatePath = Settings.Default.TemplatePath;
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                {
                    templatePath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                        , "ReportTemplates\\ReportTemplateHtml.xsl");
                    Settings.Default.TemplatePath = templatePath;
                    Settings.Default.Save();
                    return templatePath;
                }
                return templatePath;
            }
        }
        static public bool ShowDimensions { get; set; } = true;
        #endregion

        #region Private properties
        private string ImageDirectory { set; get; }
        private static int ImageSizeDetail { get; set; } = 200;
        private static int ImageSizeLarge { get; set; } = 500;
        private static int ImageHTMLSizeDetail { get; set; } = 100;
        private static int ImageHTMLSizeLarge { get; set; } = 500;
        public static float FontSizeRatioDetail { get; set; } = 6.0f;
        public static float FontSizeRatioLarge { get; set; } = 10.0f;
        protected static bool ValidateAgainstSchema { get; set; } = false;
        protected static int ImageIndex { get; set; } = 0;
        #endregion

        #region Report generation
        public void BuildAnalysisReport(ReportData inputData, ref ReportNode rootNode, string reportTemplatePath, string outputFilePath)
        {
            // initialize image index
            // verify if inputData is a valid entry
            if (!inputData.IsValid)
                throw new Exception("Reporter.BuildAnalysisReport(): ReportData argument is invalid!");
            if (null == rootNode)
                throw new Exception("RootNode == null");
            // absolute path
            string absOutputFilePath = ToAbsolute(outputFilePath);
            string absReportTemplatePath = ToAbsolute(reportTemplatePath);
            // create directory if needed
            ImageDirectory = Path.Combine(Path.GetDirectoryName(absOutputFilePath), "Images");
            if (WriteImageFiles && !Directory.Exists(ImageDirectory))
                Directory.CreateDirectory(ImageDirectory); 
            // create xml data file + XmlTextReader
            string xmlFilePath = Path.ChangeExtension(Path.GetTempFileName(), "xml");
            if (inputData is ReportDataAnalysis inputDataAnalysis)
                CreateDataFileAnalysis(inputDataAnalysis, ref rootNode, xmlFilePath, WriteNamespace);
            if (inputData is ReportDataPackStress inputDataPackStress)
                CreateDataFilePackStress(inputDataPackStress, ref rootNode, xmlFilePath, WriteNamespace);
            XmlTextReader xmlData = new XmlTextReader(new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read));
            // validate against schema
            // note xml file validation against xml schema produces a large number of errors
            // For the moment, I can not remove all errors
            if (ValidateAgainstSchema)
                ValidateXmlDocument(xmlData, Path.Combine(Path.GetDirectoryName(absReportTemplatePath), "ReportSchema.xsd"));
            // check availibility of files
            if (!File.Exists(absReportTemplatePath))
                throw new FileNotFoundException(string.Format("Report template path ({0}) is invalid", absReportTemplatePath));
            // load generated xslt
            XmlTextReader xsltReader = new XmlTextReader(new FileStream(absReportTemplatePath, FileMode.Open, FileAccess.Read));
            // check for needed language file (e.g. ENU.xml)
            string threeLetterLanguageAbbrev = System.Globalization.CultureInfo.CurrentCulture.ThreeLetterWindowsLanguageName;
            if (!TryAndGetLanguageFile(absReportTemplatePath, threeLetterLanguageAbbrev))
            {
                _log.Warn(string.Format("Language file {0}.xml could not be found! Trying ENU.xml...", threeLetterLanguageAbbrev));
                threeLetterLanguageAbbrev = "ENU";

                if (!TryAndGetLanguageFile(absReportTemplatePath, threeLetterLanguageAbbrev))
                {
                    _log.Warn(string.Format("Language file {0}.xml could not be found! Giving up!", threeLetterLanguageAbbrev));
                    return;
                }
            }
            // generate word document
            byte[] wordDoc = GetReport(xmlData, xsltReader, Path.Combine(Path.GetDirectoryName(absReportTemplatePath), threeLetterLanguageAbbrev));
            // write resulting array to HDD, show process information
            using (FileStream fs = new FileStream(absOutputFilePath, FileMode.Create))
                fs.Write(wordDoc, 0, wordDoc.Length);
        }

        private bool TryAndGetLanguageFile(string absReportTemplatePath, string threeLetterLanguageAbbrev)
        {
            string pathLanguageFileExpected = Path.Combine(Path.GetDirectoryName(absReportTemplatePath), threeLetterLanguageAbbrev + ".xml");
            if (File.Exists(pathLanguageFileExpected))
                return true;
            else
            {
                string pathLanguageFileExec = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"ReportTemplates\" + threeLetterLanguageAbbrev + ".xml"
                    );
                if (!File.Exists(pathLanguageFileExec))
                    return false;
                else
                {
                    try
                    {
                        File.Copy(pathLanguageFileExec, pathLanguageFileExpected);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.Message);
                        return false;
                    }
                    return true;
                }
            }
        }
        #endregion

        #region Xml transformation using xsl template
        /// <summary>
        /// Creates document from XML using XSLT
        /// </summary>
        /// <param name="xmlData">Report data as XML</param>
        /// <param name="xsltReader">XSLT transformation as Stream, used for document creation</param>
        /// <returns>Resulting document as byte[]</returns>
        private static byte[] GetReport(XmlReader xmlData, XmlReader xsltReader, string threeLetterLanguageAbbrev)
        {
            // Initialize needed variables
            XslCompiledTransform xslt = new XslCompiledTransform();
            XsltArgumentList args = new XsltArgumentList();
            args.AddParam("lang", "", threeLetterLanguageAbbrev);

            XsltSettings xslt_set = new XsltSettings
            {
                EnableScript = true,
                EnableDocumentFunction = true
            };
            using (MemoryStream swResult = new MemoryStream())
            {
                // Load XSLT to reader and perform transformation
                xslt.Load(xsltReader, xslt_set, new MyXmlUrlResolver());
                xslt.Transform(xmlData, args, swResult);
                return swResult.ToArray();
            }
        }
        #endregion

        #region Static methods to build xml report
        public void CreateDataFileAnalysis(ReportDataAnalysis inputData, ref ReportNode rootNode, string xmlDataFilePath, bool writeNamespace)
        {
            // instantiate XmlDocument
            XmlDocument xmlDoc = new XmlDocument();
            // set declaration
            XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "no");
            xmlDoc.AppendChild(declaration);
            // report (root) element
            XmlElement elemDocument;
            if (writeNamespace)
                elemDocument = xmlDoc.CreateElement("report", "http://treeDim/StackBuilder/ReportSchema.xsd");
            else
                elemDocument = xmlDoc.CreateElement("report");
            xmlDoc.AppendChild(elemDocument);

            string ns = xmlDoc.DocumentElement.NamespaceURI;
            Document doc = inputData.Document;
            // name element
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = null != doc ? doc.Name : "";
            elemDocument.AppendChild(elemName);
            // description element
            if (rootNode.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = null != doc ? doc.Description : string.Empty;
                elemDocument.AppendChild(elemDescription);
            }
            // author element
            if (rootNode.GetChildByName(Resources.ID_RN_AUTHORS).Activated)
            {
                XmlElement elemAuthor = xmlDoc.CreateElement("author", ns);
                elemAuthor.InnerText = null != doc ? doc.Author : string.Empty;
                elemDocument.AppendChild(elemAuthor);
            }
            if (rootNode.GetChildByName(Resources.ID_RN_DATE).Activated)
            {
                // date of creation element
                XmlElement elemDateOfCreation = xmlDoc.CreateElement("dateOfCreation", ns);
                if (null != doc)
                    elemDateOfCreation.InnerText = doc.DateOfCreation.Year < 2000 ? DateTime.Now.ToShortDateString() : doc.DateOfCreation.ToShortDateString();
                else
                    elemDateOfCreation.InnerText = DateTime.Now.ToShortDateString();
                elemDocument.AppendChild(elemDateOfCreation);
            }

            // CompanyLogo
            ReportNode rnCompanyLogo = rootNode.GetChildByName(Resources.ID_RN_COMPANYLOGO);
            if (rnCompanyLogo.Activated && !string.IsNullOrEmpty(CompanyLogo))
            {
                Bitmap logoBitmap = new Bitmap(Image.FromFile(CompanyLogo));

                XmlElement elemCompanyLogo = xmlDoc.CreateElement("companyLogo", ns);
                elemDocument.AppendChild(elemCompanyLogo);
                XmlElement elemImagePath = xmlDoc.CreateElement("imagePath");
                elemCompanyLogo.AppendChild(elemImagePath);
                elemImagePath.InnerText = SaveImageAs(logoBitmap);
                XmlElement elemWidth = xmlDoc.CreateElement("width");
                elemCompanyLogo.AppendChild(elemWidth);
                elemWidth.InnerText = logoBitmap.Width.ToString();
                XmlElement elemHeight = xmlDoc.CreateElement("height");
                elemCompanyLogo.AppendChild(elemHeight);
                elemHeight.InnerText = logoBitmap.Height.ToString();
            }
            // main analysis
            ReportNode rnAnalysis = rootNode.GetChildByName(
                string.Format(Resources.ID_RN_ANALYSIS, inputData.MainAnalysis.Name));
            if (rnAnalysis.Activated)
                AppendAnalysisElement(inputData.MainAnalysis, rnAnalysis, elemDocument, xmlDoc);
            
            // finally save xml document
            _log.Debug(string.Format("Generating xml data file {0}", xmlDataFilePath));
            xmlDoc.Save(xmlDataFilePath);
        }
        public void CreateDataFilePackStress(ReportDataPackStress inputData, ref ReportNode rootNode, string xmlDataFilePath, bool writeNamespace)
        {
            // instantiate XmlDocument
            XmlDocument xmlDoc = new XmlDocument();
            // set declaration
            XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "no");
            xmlDoc.AppendChild(declaration);
            // report (root) element
            XmlElement elemDocument;
            if (writeNamespace)
                elemDocument = xmlDoc.CreateElement("report", "http://treeDim/StackBuilder/ReportSchema.xsd");
            else
                elemDocument = xmlDoc.CreateElement("report");
            xmlDoc.AppendChild(elemDocument);

            string ns = xmlDoc.DocumentElement.NamespaceURI;

            // author element
            if (rootNode.GetChildByName(Resources.ID_RN_AUTHORS).Activated)
            {
                XmlElement elemAuthor = xmlDoc.CreateElement("author", ns);
                elemAuthor.InnerText = inputData.Author;
                elemDocument.AppendChild(elemAuthor);
            }
            if (rootNode.GetChildByName(Resources.ID_RN_DATE).Activated)
            {
                DateTime dt = DateTime.Now;
                // date of creation element
                XmlElement elemDateOfCreation = xmlDoc.CreateElement("dateOfCreation", ns);
                elemDateOfCreation.InnerText = dt.ToShortDateString();
                elemDocument.AppendChild(elemDateOfCreation);
            }

            // CompanyLogo
            ReportNode rnCompanyLogo = rootNode.GetChildByName(Resources.ID_RN_COMPANYLOGO);
            if (rnCompanyLogo.Activated && !string.IsNullOrEmpty(CompanyLogo))
            {
                Bitmap logoBitmap = new Bitmap(Image.FromFile(CompanyLogo));

                XmlElement elemCompanyLogo = xmlDoc.CreateElement("companyLogo", ns);
                elemDocument.AppendChild(elemCompanyLogo);
                XmlElement elemImagePath = xmlDoc.CreateElement("imagePath");
                elemCompanyLogo.AppendChild(elemImagePath);
                elemImagePath.InnerText = SaveImageAs(logoBitmap);
                XmlElement elemWidth = xmlDoc.CreateElement("width");
                elemCompanyLogo.AppendChild(elemWidth);
                elemWidth.InnerText = logoBitmap.Width.ToString();
                XmlElement elemHeight = xmlDoc.CreateElement("height");
                elemCompanyLogo.AppendChild(elemHeight);
                elemHeight.InnerText = logoBitmap.Height.ToString();
            }

            // BEGIN PACKSTRESS SPECIFIC
            // PackStress
            ReportNode rnPackStress = rootNode.GetChildByName(Resources.ID_RN_PACKSTRESS);
            if (rnPackStress.Activated)
            {
                XmlElement elemPackStress = xmlDoc.CreateElement("packStress", ns);
                elemDocument.AppendChild(elemPackStress);

                // Case
                ReportNode rnCase = rnPackStress.GetChildByName(Resources.ID_RN_CASE);
                if (rnCase.Activated)
                {
                    XmlElement elemCase = xmlDoc.CreateElement("bctCase", ns);
                    elemPackStress.AppendChild(elemCase);

                    if (rnCase.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
                    {
                        AppendElementValue(xmlDoc, elemCase, "length", UnitsManager.UnitType.UT_LENGTH, inputData.Box.Length);
                        AppendElementValue(xmlDoc, elemCase, "width", UnitsManager.UnitType.UT_LENGTH, inputData.Box.Width);
                        AppendElementValue(xmlDoc, elemCase, "height", UnitsManager.UnitType.UT_LENGTH, inputData.Box.Height);
                    }
                    if (rnCase.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
                    {
                        AppendElementValue(xmlDoc, elemCase, "weight", UnitsManager.UnitType.UT_MASS, inputData.Box.Weight);
                    }
                    if (rnCase.GetChildByName(Resources.ID_RN_IMAGE).Activated)
                    {
                        Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                        {
                            FontSizeRatio = FontSizeRatioDetail,
                            CameraPosition = Graphics3D.Corner_0,
                            Target = Vector3D.Zero
                        };
                        graphics.AddBox(new Box(0, inputData.Box));
                        if (ShowDimensions)
                            graphics.AddDimensions(new DimensionCube(inputData.Box.Length, inputData.Box.Width, inputData.Box.Height));
                        graphics.Flush();
                        // ---
                        // imageThumb
                        AppendThumbnailElement(xmlDoc, elemCase, graphics.Bitmap);
                    }
                }
                // Material
                ReportNode rnMaterial = rnPackStress.GetChildByName(Resources.ID_RN_MATERIAL);
                if (rnMaterial.Activated)
                {
                    XmlElement elemMaterial = xmlDoc.CreateElement("material", ns);
                    elemPackStress.AppendChild(elemMaterial);
                    // name
                    XmlElement elemName = xmlDoc.CreateElement("name", ns);
                    elemName.InnerText = inputData.Mat.Name;
                    elemMaterial.AppendChild(elemName);
                    // profile
                    XmlElement elemProfile = xmlDoc.CreateElement("profile", ns);
                    elemProfile.InnerText = inputData.Mat.Profile;
                    elemMaterial.AppendChild(elemProfile);
                    // thickness
                    AppendElementValue(xmlDoc, elemMaterial, "thickness", UnitsManager.UnitType.UT_LENGTH, inputData.Mat.Thickness);
                    // ECT
                    AppendElementValue(xmlDoc, elemMaterial, "ect", UnitsManager.UnitType.UT_LINEARFORCE, inputData.Mat.ECT);
                    // Stiffness X
                    AppendElementValue(xmlDoc, elemMaterial, "stiffnessX", UnitsManager.UnitType.UT_STIFFNESS, inputData.Mat.RigidityDX);
                    // Stiffness Y
                    AppendElementValue(xmlDoc, elemMaterial, "stiffnessY", UnitsManager.UnitType.UT_STIFFNESS, inputData.Mat.RigidityDY);
                }

                // Static BCT
                ReportNode rnStaticBCT = rnPackStress.GetChildByName(Resources.ID_RN_STATICBCT);
                if (rnStaticBCT.Activated)
                {
                    XmlElement elemStaticBCT = xmlDoc.CreateElement("staticBCT", ns);
                    elemPackStress.AppendChild(elemStaticBCT);
                    // BCT
                    AppendElementValue(xmlDoc, elemStaticBCT, "bct", UnitsManager.UnitType.UT_FORCE, inputData.StaticBCT);
                }
                // Dynamic BCT
                ReportNode rnDynamicBCT = rnPackStress.GetChildByName(Resources.ID_RN_DYNAMICBCT);
                if (rnDynamicBCT.Activated)
                {
                    XmlElement elemDynamicBCT = xmlDoc.CreateElement("dynamicBCT", ns);
                    elemPackStress.AppendChild(elemDynamicBCT);

                    foreach (var bctRow in inputData.BCTRows)
                    {
                        XmlElement elemBctRow = xmlDoc.CreateElement("bctRow", ns);
                        elemDynamicBCT.AppendChild(elemBctRow);

                        var elemBctRowName = xmlDoc.CreateElement("name", ns);
                        elemBctRow.AppendChild(elemBctRowName);
                        elemBctRowName.InnerText = bctRow.Name;

                        foreach (var bctValue in bctRow.Values)
                        {
                            XmlElement elemBct = xmlDoc.CreateElement("bctValue", ns);
                            elemBctRow.AppendChild(elemBct);
                            elemBct.InnerText = string.Format("{0:0.#}", bctValue);
                        }
                    }
                }
                // Palletisation
                if (null != inputData.Analysis)
                { 
                    ReportNode rnPalletisation = rnPackStress.GetChildByName(Resources.ID_RN_PALLETISATION);
                    if (rnPalletisation.Activated)
                    {
                        XmlElement elemPalletisation = xmlDoc.CreateElement("palletisation", ns);
                        elemPackStress.AppendChild(elemPalletisation);

                        if (inputData.Analysis is AnalysisCasePallet analysis)
                        {
                            var constraintSet = analysis.ConstraintSet as ConstraintSetCasePallet;
                            ReportNode rnPallet = rnPalletisation.GetChildByName(Resources.ID_RN_PALLET);
                            if (rnPallet.Activated)
                                AppendBCTPallet(analysis.PalletProperties, constraintSet.Overhang, rnPallet, elemPalletisation, xmlDoc);
                            ReportNode rnPalletisationResults = rnPalletisation.GetChildByName(Resources.ID_RN_SOLUTION);
                            if (rnPalletisationResults.Activated)
                            {
                                SolutionLayered sol = analysis.SolutionLay;

                                XmlElement elemSolution = xmlDoc.CreateElement("bctSolution");
                                elemPalletisation.AppendChild(elemSolution);
                                // number of cases
                                if (rnPalletisationResults.GetChildByName(Resources.ID_RN_NOLAYERSBYNOCASES).Activated)
                                    AppendElementValue(xmlDoc, elemSolution, "noLayersAndNoCases", sol.NoLayersPerNoCasesString);
                                // pallet weight
                                if (sol.HasNetWeight)
                                    AppendElementValue(xmlDoc, elemSolution, "netWeight", UnitsManager.UnitType.UT_MASS, sol.NetWeight.Value);
                                AppendElementValue(xmlDoc, elemSolution, "weightLoad", UnitsManager.UnitType.UT_MASS, sol.LoadWeight);
                                AppendElementValue(xmlDoc, elemSolution, "weightTotal", UnitsManager.UnitType.UT_MASS, sol.Weight);
                                if (rnPalletisationResults.GetChildByName(Resources.ID_RN_LOADDIMENSIONS).Activated)
                                    AppendElementBB(xmlDoc, elemSolution, "bboxLoad", UnitsManager.UnitType.UT_LENGTH
                                        , new double[3] { sol.BBoxLoad.Length, sol.BBoxLoad.Width, sol.BBoxLoad.Height });
                                if (rnPalletisationResults.GetChildByName(Resources.ID_RN_OVERALLDIMENSIONS).Activated)
                                    AppendElementBB(xmlDoc, elemSolution, "bboxTotal", UnitsManager.UnitType.UT_LENGTH
                                        , new double[3] { sol.BBoxGlobal.Length, sol.BBoxGlobal.Width, sol.BBoxGlobal.Height });
                                // average load on first layer
                                AppendElementValue(xmlDoc, elemSolution, "loadOnLowestCase", UnitsManager.UnitType.UT_MASS, sol.LoadOnLowestCase);
                                // image
                                if (rnPalletisationResults.GetChildByName(Resources.ID_RN_IMAGE).Activated)
                                {
                                    // initialize drawing values
                                    string viewName = "view_solution_iso";
                                    int imageWidth = ImageSizeLarge;
                                    int imgHTMLWidth = ImageHTMLSizeLarge;
                                    // instantiate graphics
                                    Graphics3DImage graphics = new Graphics3DImage(new Size(imageWidth, imageWidth))
                                    {
                                        FontSizeRatio = FontSizeRatioLarge,
                                        CameraPosition = Graphics3D.Corner_0,
                                        ShowDimensions = ShowDimensions
                                    };
                                    try
                                    {
                                        // instantiate solution viewer
                                        using (ViewerSolution sv = new ViewerSolution(sol))
                                            sv.Draw(graphics, Transform3D.Identity);
                                        graphics.Flush();
                                    }
                                    catch (Exception ex)
                                    {
                                        _log.Error(ex.ToString());
                                    }
                                    // image path
                                    string imagePath = SaveImageAs(graphics.Bitmap);

                                    // ---
                                    XmlElement elemImage = xmlDoc.CreateElement(viewName, ns);
                                    elemSolution.AppendChild(elemImage);
                                    XmlElement elemImagePath = xmlDoc.CreateElement("imagePath");
                                    elemImage.AppendChild(elemImagePath);
                                    elemImagePath.InnerText = imagePath;
                                    XmlElement elemWidth = xmlDoc.CreateElement("width");
                                    elemImage.AppendChild(elemWidth);
                                    elemWidth.InnerText = imgHTMLWidth.ToString();
                                    XmlElement elemHeight = xmlDoc.CreateElement("height");
                                    elemImage.AppendChild(elemHeight);
                                    elemHeight.InnerText = imgHTMLWidth.ToString();
                                } // imahe
                            } // results
                        } // is AnalysisCasePallet
                    } // rnPalletisation.Activated
                } // analysis
            }
            // END  PACKSTRESS SPECIFIC

            // finally save xml document
            _log.Debug(string.Format("Generating xml data file {0}", xmlDataFilePath));
            xmlDoc.Save(xmlDataFilePath);

        }
        #endregion

        #region Analyses
        private void AppendAnalysisElement(Analysis analysis, ReportNode rnAnalysis, XmlElement elemDocument, XmlDocument xmlDoc)
        {
            if (analysis is AnalysisHomo analysisHomo)
                AppendAnalysisHomoElement(analysisHomo, rnAnalysis, elemDocument, xmlDoc);
            else if (analysis is AnalysisHetero analysisHetero)
                AppendAnalysisHeterogeneousElement(analysisHetero, rnAnalysis, elemDocument, xmlDoc);
        }
        private void AppendAnalysisHomoElement(AnalysisHomo analysis, ReportNode rnAnalysis, XmlElement elemDocument, XmlDocument xmlDoc)
        { 
            Packable content = analysis.Content;

            bool hasContent = false;
            // check for inner analysis
            AnalysisHomo innerAnalysis = null;
            if (content.InnerAnalysis(ref innerAnalysis))
            {
                // -> proceed recursively
                ReportNode rnInnerAnalysis = rnAnalysis.GetChildByName(string.Format(Resources.ID_RN_ANALYSIS, innerAnalysis.Name));
                if (rnInnerAnalysis.Activated)
                    AppendAnalysisHomoElement(innerAnalysis as AnalysisHomo, rnInnerAnalysis, elemDocument, xmlDoc);
            }
            else
                hasContent = true;
            
            // ### 0. ANALYSIS ELT
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            XmlElement eltAnalysis = xmlDoc.CreateElement("analysis", ns);
            elemDocument.AppendChild(eltAnalysis);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = analysis.Name;
            eltAnalysis.AppendChild(elemName);
            // description
            if (rnAnalysis.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = analysis.Description;
                eltAnalysis.AppendChild(elemDescription);
            }

            // ### 1. CONTENT
            if (hasContent)
            {
                ReportNode rnContent = rnAnalysis.GetChildByName(string.Format("{0} ({1})", PackableType(content), content.Name));
                if (rnContent.Activated)
                    AppendContentElement(content, rnContent, eltAnalysis, xmlDoc);
            }
  
            // ### 2. CONTAINER
            ReportNode rnContainer = rnAnalysis.GetChildByName(string.Format("{0} ({1})", PackableType(analysis.Container), analysis.Container.Name));
            if (rnContainer.Activated)
                AppendContainerElement(analysis.Container, rnContainer, eltAnalysis, xmlDoc);

            // ### 3. INTERLAYERS
            if (analysis is AnalysisLayered analysisLayered)
            {
                foreach (InterlayerProperties interlayer in analysisLayered.Interlayers)
                {
                    ReportNode rnInterlayer = rnAnalysis.GetChildByName(string.Format("{0} ({1})", PackableType(interlayer), interlayer.Name));
                    if (rnInterlayer.Activated)
                        AppendInterlayerElement(interlayer, rnInterlayer, eltAnalysis, xmlDoc);
                }
            }
            // ### 4. CONSTRAINTSET
            ReportNode rnConstraintSet = rnAnalysis.GetChildByName(Resources.ID_RN_CONSTRAINTSET);
            if (rnConstraintSet.Activated)
                AppendConstraintSet(analysis.ConstraintSet, rnConstraintSet, eltAnalysis, xmlDoc);

            // ### 5. SOLUTION
            ReportNode rnSolution = rnAnalysis.GetChildByName(Resources.ID_RN_SOLUTION);
            if (rnSolution.Activated)
                AppendSolutionElement(analysis.Solution, rnSolution, eltAnalysis, xmlDoc);
        }

        private void AppendAnalysisHeterogeneousElement(AnalysisHetero analysis, ReportNode rnAnalysis, XmlElement elemDocument, XmlDocument xmlDoc)
        {
            // ### 0. HANALYSIS ELT
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            XmlElement eltAnalysis = xmlDoc.CreateElement("hAnalysis", ns);
            elemDocument.AppendChild(eltAnalysis);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = analysis.Name;
            eltAnalysis.AppendChild(elemName);
            // description
            if (rnAnalysis.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = analysis.Description;
                eltAnalysis.AppendChild(elemDescription);
            }
            // 1. CONTENT
            // 2. CONTAINER
            ItemBase container = analysis.Containers.First();
            ReportNode rnContainer = rnAnalysis.GetChildByName(string.Format("{0}", PackableType(container)));
            if (rnContainer.Activated)
                AppendContainerElement(container, rnContainer, eltAnalysis, xmlDoc);
            // 3. CONSTRAINTSET
            ReportNode rnConstraintSet = rnAnalysis.GetChildByName(Resources.ID_RN_CONSTRAINTSET);
            if (rnConstraintSet.Activated)
                AppendConstraintSet(analysis.ConstraintSet, rnConstraintSet, eltAnalysis, xmlDoc);
            // 4. SOLUTION
            ReportNode rnSolution = rnAnalysis.GetChildByName(Resources.ID_RN_SOLUTION);
            if (rnSolution.Activated)
                AppendSolutionHeterogeneousElement(analysis.Solution, rnSolution, eltAnalysis, xmlDoc);

        }

        private string PackableType(ItemBase itemBase)
        {
            if (itemBase is BoxProperties)
            {
                BoxProperties bProperties = itemBase as BoxProperties;
                if (bProperties.HasInsideDimensions)
                    return Resources.ID_RN_CASE;
                else
                    return Resources.ID_RN_BOX;
            }
            else if (itemBase is BundleProperties)
                return Resources.ID_RN_BUNDLE;
            else if (itemBase is CylinderProperties)
                return Resources.ID_RN_CYLINDER;
            else if (itemBase is BottleProperties)
                return Resources.ID_RN_BOTTLE;
            else if (itemBase is PalletProperties)
                return Resources.ID_RN_PALLET;
            else if (itemBase is TruckProperties)
                return Resources.ID_RN_TRUCK;
            else
                return Resources.ID_RN_DEFAULT;            
        }

        private void AppendContentElement(Packable packable, ReportNode rnContent, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            if (packable is BoxProperties)
            {
                BoxProperties boxProperties = packable as BoxProperties;
                if (boxProperties.HasInsideDimensions)
                    AppendCaseElement(boxProperties, rnContent, elemAnalysis, xmlDoc);
                else
                    AppendBoxElement(boxProperties, rnContent, elemAnalysis, xmlDoc);
            }
            else if (packable is BundleProperties)
                AppendBundleElement(packable as BundleProperties, rnContent, elemAnalysis, xmlDoc);
            else if (packable is PackProperties)
                AppendPackElement(packable as PackProperties, rnContent, elemAnalysis, xmlDoc);
            else if (packable is RevSolidProperties)
                AppendCylinderElement(packable as RevSolidProperties, rnContent, elemAnalysis, xmlDoc);
            else if (packable is LoadedCase)
            {
            }
            else if (packable is LoadedPallet)
            {
            }
            else
                throw new ReportExceptionUnexpectedItem(packable);
        }
        private void AppendContainerElement(ItemBase container, ReportNode rnContainer, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            if (container is PalletProperties)
                AppendPalletElement(container as PalletProperties, rnContainer, elemAnalysis, xmlDoc);
            else if (container is BoxProperties)
                AppendCaseElement(container as BoxProperties, rnContainer, elemAnalysis, xmlDoc);
            else if (container is TruckProperties)
                AppendTruckElement(container as TruckProperties, rnContainer, elemAnalysis, xmlDoc);
        }
        private void AppendConstraintSet(ConstraintSetAbstract constraintSet, ReportNode rnConstraintSet, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            XmlElement elemConstraintSet = xmlDoc.CreateElement("constraintSet", ns);
            elemAnalysis.AppendChild(elemConstraintSet);

            if (constraintSet is ConstraintSetCasePallet && rnConstraintSet.GetChildByName(Resources.ID_RN_OVERHANG).Activated)
            {
                ConstraintSetCasePallet constraintSetCasePallet = constraintSet as ConstraintSetCasePallet;
                AppendElementValue(xmlDoc, elemConstraintSet, "overhangX", UnitsManager.UnitType.UT_LENGTH, constraintSetCasePallet.Overhang.X);
                AppendElementValue(xmlDoc, elemConstraintSet, "overhangY", UnitsManager.UnitType.UT_LENGTH, constraintSetCasePallet.Overhang.Y);
            }
            if (constraintSet is ConstraintSetCasePallet && constraintSet.OptMaxHeight.Activated && rnConstraintSet.GetChildByName(Resources.ID_RN_MAXIMUMHEIGHT).Activated)
                AppendElementValue(xmlDoc, elemConstraintSet, "maximumHeight", UnitsManager.UnitType.UT_LENGTH, constraintSet.OptMaxHeight.Value);
            if (constraintSet.OptMaxWeight.Activated && rnConstraintSet.GetChildByName(Resources.ID_RN_MAXIMUMWEIGHT).Activated)
                AppendElementValue(xmlDoc, elemConstraintSet, "maximumWeight", UnitsManager.UnitType.UT_MASS, constraintSet.OptMaxWeight.Value);
            if (constraintSet.OptMaxNumber.Activated && rnConstraintSet.GetChildByName(Resources.ID_RN_MAXIMUMNUMBER).Activated)
                AppendElementValue(xmlDoc, elemConstraintSet, "maximumNumber", constraintSet.OptMaxNumber.Value);
        }
        private void AppendConstraintSet(HConstraintSet constraintSet, ReportNode rnConstraintSet, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            XmlElement elemConstraintSet = xmlDoc.CreateElement("hConstraintSet", ns);
            elemAnalysis.AppendChild(elemConstraintSet);

            if (constraintSet is HConstraintSetPallet constraintSetPallet)
            { AppendElementValue(xmlDoc, elemConstraintSet, "maximumHeight", UnitsManager.UnitType.UT_LENGTH, constraintSetPallet.MaximumHeight); }
            if (constraintSet is HConstraintSetCase constraintSetCase)
            { }
        }
        private void AppendSolutionElement(SolutionHomo sol, ReportNode rnSolution, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            XmlElement elemSolution = xmlDoc.CreateElement("solution", ns);
            elemAnalysis.AppendChild(elemSolution);

            // *** Item # (Recursive count)
            AnalysisHomo analysis = sol.Analysis;
            Packable content = analysis.Content;
            int itemCount = sol.ItemCount;
            int number = 1;
            do
            {
                itemCount *= number;
                AppendContentItem(xmlDoc, elemSolution, content.DetailedName, itemCount);
            }
            while (null != content && content.InnerContent(ref content, ref number));

            if (sol is SolutionLayered solLayer)
            {
                // ***
                // Number of layers
                AppendElementValue(xmlDoc, elemSolution, "noLayers", solLayer.Layers.Count);
                // Number of cases per layer
                if (solLayer.HasConstantNoCasesPerLayer)
                    AppendElementValue(xmlDoc, elemSolution, "noCasesPerLayer", solLayer.Layers[0].BoxCount);
                // number layers x number cases
                if (rnSolution.GetChildByName(Resources.ID_RN_NOLAYERSBYNOCASES).Activated)
                    AppendElementValue(xmlDoc, elemSolution, "noLayersAndNoCases", solLayer.NoLayersPerNoCasesString);
                // Number of interlayers
                if (solLayer.InterlayerCount > 0 && rnSolution.GetChildByName(Resources.ID_RN_NOINTERLAYERS).Activated)
                    AppendElementValue(xmlDoc, elemSolution, "noInterlayers", solLayer.InterlayerCount);

            }
            // ***
            if (rnSolution.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
            {
                if (sol.HasNetWeight)
                    AppendElementValue(xmlDoc, elemSolution, "netWeight", UnitsManager.UnitType.UT_MASS, sol.NetWeight.Value);
                AppendElementValue(xmlDoc, elemSolution, "weightLoad", UnitsManager.UnitType.UT_MASS, sol.LoadWeight);
                AppendElementValue(xmlDoc, elemSolution, "weightTotal", UnitsManager.UnitType.UT_MASS, sol.Weight);

            }
            if (rnSolution.GetChildByName(Resources.ID_RN_LOADDIMENSIONS).Activated)
                AppendElementBB(xmlDoc, elemSolution, "bboxLoad", UnitsManager.UnitType.UT_LENGTH
                    , new double[3] { sol.BBoxLoad.Length, sol.BBoxLoad.Width, sol.BBoxLoad.Height });
            if (rnSolution.GetChildByName(Resources.ID_RN_OVERALLDIMENSIONS).Activated)
                AppendElementBB(xmlDoc, elemSolution, "bboxTotal", UnitsManager.UnitType.UT_LENGTH
                    , new double[3] { sol.BBoxGlobal.Length, sol.BBoxGlobal.Width, sol.BBoxGlobal.Height });
            if (rnSolution.GetChildByName(Resources.ID_RN_VOLUMEEFFICIENCY).Activated)
                AppendElementValue(xmlDoc, elemSolution, "efficiencyVolume", sol.VolumeEfficiency);

            ReportNode rnViews = rnSolution.GetChildByName(Resources.ID_RN_VIEWS);
            ReportNode rnViewIso = rnSolution.GetChildByName(Resources.ID_RN_VIEWISO);

            // --- case images
            for (int i = 0; i < 5; ++i)
            {
                if (i < 4 && !rnViews.Activated) continue;
                if (i == 4 && !rnViewIso.Activated) continue;

                // initialize drawing values
                string viewName = string.Empty;
                Vector3D cameraPos = Vector3D.Zero;
                int imageWidth = ImageSizeDetail, imgHTMLWidth = ImageHTMLSizeDetail;
                bool showDimLocal = false;
                switch (i)
                {
                    case 0: viewName = "view_solution_front"; cameraPos = Graphics3D.Front; break;
                    case 1: viewName = "view_solution_left"; cameraPos = Graphics3D.Left; break;
                    case 2: viewName = "view_solution_right"; cameraPos = Graphics3D.Right; break;
                    case 3: viewName = "view_solution_back"; cameraPos = Graphics3D.Back; break;
                    case 4: viewName = "view_solution_iso"; cameraPos = Graphics3D.Corner_0;
                        imageWidth = ImageSizeLarge;
                        imgHTMLWidth = ImageHTMLSizeLarge;
                        showDimLocal = true;
                        break;
                    default: break;
                }
                // instantiate graphics
                Graphics3DImage graphics = new Graphics3DImage(new Size(imageWidth, imageWidth))
                {
                    FontSizeRatio = FontSizeRatioLarge,
                    CameraPosition = cameraPos,
                    ShowDimensions = showDimLocal && ShowDimensions
                };

                try
                {
                    if (sol is SolutionLayered solLayer2)
                    {
                        // instantiate solution viewer
                        using (ViewerSolution sv = new ViewerSolution(solLayer2))
                            sv.Draw(graphics, Transform3D.Identity);
                    }
                    else if (sol is SolutionHCyl solHCyl)
                    {
                        using (ViewerSolutionHCyl sv = new ViewerSolutionHCyl(solHCyl))
                            sv.Draw(graphics, Transform3D.Identity);
                    }
                    graphics.Flush();
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                }
                // image path
                string imagePath = SaveImageAs(graphics.Bitmap);

                // ---
                XmlElement elemImage = xmlDoc.CreateElement(viewName, ns);
                elemSolution.AppendChild(elemImage);
                XmlElement elemImagePath = xmlDoc.CreateElement("imagePath");
                elemImage.AppendChild(elemImagePath);
                elemImagePath.InnerText = imagePath;
                XmlElement elemWidth = xmlDoc.CreateElement("width");
                elemImage.AppendChild(elemWidth);
                elemWidth.InnerText = imgHTMLWidth.ToString();
                XmlElement elemHeight = xmlDoc.CreateElement("height");
                elemImage.AppendChild(elemHeight);
                elemHeight.InnerText = imgHTMLWidth.ToString();
            }
            if (sol is SolutionLayered solLay3)
            { 
            ReportNode rnLayers = rnSolution.GetChildByName(Resources.ID_RN_LAYERS);
                if (rnLayers.Activated)
                {
                    // layers
                    XmlElement elemLayers = xmlDoc.CreateElement("layers", ns);
                    elemSolution.AppendChild(elemLayers);

                    List<LayerSummary> layerSummaries = solLay3.ListLayerSummary;
                    foreach (LayerSummary layerSum in layerSummaries)
                    {
                        // layer
                        XmlElement elemLayer = xmlDoc.CreateElement("layer", ns);
                        elemLayers.AppendChild(elemLayer);
                        // layerIndexes
                        XmlElement elemLayerIndexes = xmlDoc.CreateElement("layerIndexes", ns);
                        elemLayer.AppendChild(elemLayerIndexes);
                        elemLayerIndexes.InnerText = layerSum.LayerIndexesString;
                        // item count
                        content = sol.Analysis.Content;
                        itemCount = layerSum.ItemCount;
                        number = 1;
                        do
                        {
                            itemCount *= number;
                            AppendContentItem(xmlDoc, elemLayer, content.DetailedName, itemCount);
                        }
                        while (null != content && content.InnerContent(ref content, ref number));
                        // ***
                        if (rnLayers.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
                        {
                            // layerLength
                            AppendElementValue(xmlDoc, elemLayer, "layerLength", UnitsManager.UnitType.UT_LENGTH, layerSum.LayerDimensions.X);
                            // layerWidth
                            AppendElementValue(xmlDoc, elemLayer, "layerWidth", UnitsManager.UnitType.UT_LENGTH, layerSum.LayerDimensions.Y);
                            // layerHeight
                            AppendElementValue(xmlDoc, elemLayer, "layerHeight", UnitsManager.UnitType.UT_LENGTH, layerSum.LayerDimensions.Z);
                        }
                        if (rnLayers.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
                        {
                            // layerWeight
                            AppendElementValue(xmlDoc, elemLayer, "layerWeight", UnitsManager.UnitType.UT_MASS, layerSum.LayerWeight);
                            // layerNetWeight
                            if (sol.HasNetWeight)
                                AppendElementValue(xmlDoc, elemLayer, "layerNetWeight", UnitsManager.UnitType.UT_MASS, layerSum.LayerNetWeight);
                        }
                        // layer spaces
                        if (rnLayers.GetChildByName(Resources.ID_RN_SPACES).Activated)
                            AppendElementValue(xmlDoc, elemLayer, "layerSpaces", UnitsManager.UnitType.UT_LENGTH, layerSum.Space);
                        // layer image
                        if (rnLayers.GetChildByName(Resources.ID_RN_IMAGE).Activated)
                        {
                            Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                            {
                                FontSizeRatio = FontSizeRatioDetail
                            };
                            ViewerSolution.DrawILayer(graphics, layerSum.Layer3D, sol.Analysis.Content, Reporter.ShowDimensions);
                            graphics.Flush();
                            AppendThumbnailElement(xmlDoc, elemLayer, graphics.Bitmap);
                        }
                    }
                }
            }
        }

        private void AppendSolutionHeterogeneousElement(HSolution sol, ReportNode rnSolution, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            XmlElement elemSolution = xmlDoc.CreateElement("hSolution", ns);
            elemAnalysis.AppendChild(elemSolution);

            // *** Item # (Recursive count)
            AnalysisHetero analysis = sol.Analysis;

            for (int solItemIndex = 0; solItemIndex < sol.SolItemCount; ++solItemIndex)
            {
                XmlElement elemSolItem = xmlDoc.CreateElement("solItem", ns);
                elemSolution.AppendChild(elemSolItem);

                HSolItem hSolItem = sol.SolItem(solItemIndex);
                // index
                AppendElementValue(xmlDoc, elemSolItem, "index", solItemIndex);
                // itemQuantities
                ReportNode rnItemNumbers = rnSolution.GetChildByName(Resources.ID_RN_ITEMNUMBERS);
                if (rnItemNumbers.Activated)
                {
                    XmlElement elemItemNumbers = xmlDoc.CreateElement("itemQuantities", ns);
                    elemSolItem.AppendChild(elemItemNumbers);
                    // unit
                    XmlElement unitElement = xmlDoc.CreateElement("unitWeight", xmlDoc.DocumentElement.NamespaceURI);
                    unitElement.InnerText = UnitsManager.MassUnitString;
                    elemItemNumbers.AppendChild(unitElement);

                    // create a report node for all images
                    ReportNode rnItemImages = rnItemNumbers.GetChildByName(Resources.ID_RN_IMAGES);

                    var dictNameCount = hSolItem.SolutionItems;
                    foreach (int containedItemIndex in dictNameCount.Keys)
                    {
                        XmlElement elemItemNumber = xmlDoc.CreateElement("itemQuantity", ns);
                        elemItemNumbers.AppendChild(elemItemNumber);
                        if (analysis.ContentTypeByIndex(containedItemIndex) is Packable packable)
                        {
                            // name
                            AppendElementValue(xmlDoc, elemItemNumber, "name", packable.Name);
                            // count
                            AppendElementValue(xmlDoc, elemItemNumber, "count", dictNameCount[containedItemIndex]);
                            // weight
                            AppendElementValue(xmlDoc, elemItemNumber, "weight", packable.Weight * dictNameCount[containedItemIndex]);
                            // image
                            if (rnItemImages.Activated)
                            {
                                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                                {
                                    FontSizeRatio = FontSizeRatioDetail,
                                    CameraPosition = Graphics3D.Corner_0,
                                    Target = Vector3D.Zero
                                };
                                if (packable is BoxProperties caseProperties)
                                {
                                    Box box = new Box(0, caseProperties);
                                    graphics.AddBox(box);
                                }
                                graphics.Flush();
                                AppendThumbnailElement(xmlDoc, elemItemNumber, graphics.Bitmap);
                            }
                        }
                    }
                }
                ReportNode rnLoadWeight = rnSolution.GetChildByName(Resources.ID_RN_LOADWEIGHT);
                if (rnLoadWeight.Activated)
                    AppendElementValue(xmlDoc, elemSolItem, "weightLoad", UnitsManager.UnitType.UT_MASS, sol.LoadWeight(solItemIndex));
                ReportNode rnTotalWeight = rnSolution.GetChildByName(Resources.ID_RN_TOTALWEIGHT);
                if (rnTotalWeight.Activated)
                    AppendElementValue(xmlDoc, elemSolItem, "weightTotal", UnitsManager.UnitType.UT_MASS, sol.Weight(solItemIndex));
                if (rnSolution.GetChildByName(Resources.ID_RN_OVERALLDIMENSIONS).Activated)
                    AppendElementBB(xmlDoc, elemSolution, "bboxLoad", UnitsManager.UnitType.UT_LENGTH
                        , new double[3] { sol.BBoxGlobal(solItemIndex).Length, sol.BBoxGlobal(solItemIndex).Width, sol.BBoxGlobal(solItemIndex).Height });
                if (rnSolution.GetChildByName(Resources.ID_RN_LOADDIMENSIONS).Activated)
                    AppendElementBB(xmlDoc, elemSolution, "bboxTotal", UnitsManager.UnitType.UT_LENGTH
                        , new double[3] { sol.BBoxLoad(solItemIndex).Length, sol.BBoxLoad(solItemIndex).Width, sol.BBoxLoad(solItemIndex).Height });

                ReportNode rnViews = rnSolution.GetChildByName(Resources.ID_RN_VIEWS);
                ReportNode rnViewIso = rnSolution.GetChildByName(Resources.ID_RN_VIEWISO);

                // --- case images
                for (int i = 0; i < 5; ++i)
                {
                    if (i < 4 && !rnViews.Activated) continue;
                    if (i == 4 && !rnViewIso.Activated) continue;

                    // initialize drawing values
                    string viewName = string.Empty;
                    Vector3D cameraPos = Vector3D.Zero;
                    int imageWidth = ImageSizeDetail, imgHTMLWidth = ImageHTMLSizeDetail;
                    bool showDimLocal = false;
                    switch (i)
                    {
                        case 0: viewName = "view_solution_front"; cameraPos = Graphics3D.Front; break;
                        case 1: viewName = "view_solution_left"; cameraPos = Graphics3D.Left; break;
                        case 2: viewName = "view_solution_right"; cameraPos = Graphics3D.Right; break;
                        case 3: viewName = "view_solution_back"; cameraPos = Graphics3D.Back; break;
                        case 4:
                            viewName = "view_solution_iso"; cameraPos = Graphics3D.Corner_0;
                            imageWidth = ImageSizeLarge;
                            imgHTMLWidth = ImageHTMLSizeLarge;
                            showDimLocal = true;
                            break;
                        default: break;
                    }
                    // instantiate graphics
                    Graphics3DImage graphics = new Graphics3DImage(new Size(imageWidth, imageWidth))
                    {
                        FontSizeRatio = FontSizeRatioLarge,
                        // set camera position 
                        CameraPosition = cameraPos,
                        ShowDimensions = showDimLocal && ShowDimensions
                    };

                    // instantiate solution viewer
                    var sv = new ViewerHSolution(sol, solItemIndex);
                    sv.Draw(graphics, Transform3D.Identity);
                    graphics.Flush();
                    // image path
                    string imagePath = SaveImageAs(graphics.Bitmap);

                    // ---
                    XmlElement elemImage = xmlDoc.CreateElement(viewName, ns);
                    elemSolItem.AppendChild(elemImage);
                    XmlElement elemImagePath = xmlDoc.CreateElement("imagePath");
                    elemImage.AppendChild(elemImagePath);
                    elemImagePath.InnerText = imagePath;
                    XmlElement elemWidth = xmlDoc.CreateElement("width");
                    elemImage.AppendChild(elemWidth);
                    elemWidth.InnerText = imgHTMLWidth.ToString();
                    XmlElement elemHeight = xmlDoc.CreateElement("height");
                    elemImage.AppendChild(elemHeight);
                    elemHeight.InnerText = imgHTMLWidth.ToString();
                }
            }
        }
        private void AppendPalletElement(PalletProperties palletProp, ReportNode rnPallet, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            if (null == palletProp) return;
            // pallet
            XmlElement elemPallet = xmlDoc.CreateElement("pallet", ns);
            elemAnalysis.AppendChild(elemPallet);
            // name
            if (rnPallet.GetChildByName(Resources.ID_RN_NAME).Activated)
            {
                XmlElement elemName = xmlDoc.CreateElement("name", ns);
                elemName.InnerText = palletProp.Name;
                elemPallet.AppendChild(elemName);
            }
            // description
            if (rnPallet.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = palletProp.Description;
                elemPallet.AppendChild(elemDescription);
            }
            if (rnPallet.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
            {
                AppendElementValue(xmlDoc, elemPallet, "length", UnitsManager.UnitType.UT_LENGTH, palletProp.Length);
                AppendElementValue(xmlDoc, elemPallet, "width", UnitsManager.UnitType.UT_LENGTH, palletProp.Width);
                AppendElementValue(xmlDoc, elemPallet, "height", UnitsManager.UnitType.UT_LENGTH, palletProp.Height);
            }
            if (rnPallet.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
            {
                AppendElementValue(xmlDoc, elemPallet, "weight", UnitsManager.UnitType.UT_MASS, palletProp.Weight);
                AppendElementValue(xmlDoc, elemPallet, "admissibleLoad", UnitsManager.UnitType.UT_MASS, palletProp.AdmissibleLoadWeight);
            }
            // type
            XmlElement elemType = xmlDoc.CreateElement("type", ns);
            elemType.InnerText = palletProp.TypeName;
            elemPallet.AppendChild(elemType);
            if (rnPallet.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                // --- build image
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                {
                    FontSizeRatio = FontSizeRatioDetail,
                    CameraPosition = Graphics3D.Corner_0,
                    Target = Vector3D.Zero
                };
                Pallet pallet = new Pallet(palletProp);
                pallet.Draw(graphics, Transform3D.Identity);
                if (ShowDimensions)
                    graphics.AddDimensions(new DimensionCube(palletProp.Length, palletProp.Width, palletProp.Height));
                graphics.Flush();
                // ---
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemPallet, graphics.Bitmap);
            }
        }
        private void AppendBCTPallet(PalletProperties palletProp, Vector2D vOverhang, ReportNode rnPallet, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            if (null == palletProp) return;
            // pallet
            XmlElement elemPallet = xmlDoc.CreateElement("bctPallet", ns);
            elemAnalysis.AppendChild(elemPallet);
            
            if (rnPallet.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
            {
                AppendElementValue(xmlDoc, elemPallet, "length", UnitsManager.UnitType.UT_LENGTH, palletProp.Length);
                AppendElementValue(xmlDoc, elemPallet, "width", UnitsManager.UnitType.UT_LENGTH, palletProp.Width);
                AppendElementValue(xmlDoc, elemPallet, "height", UnitsManager.UnitType.UT_LENGTH, palletProp.Height);
            }
            if (rnPallet.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
                AppendElementValue(xmlDoc, elemPallet, "weight", UnitsManager.UnitType.UT_MASS, palletProp.Weight);
            if (rnPallet.GetChildByName(Resources.ID_RN_OVERHANG).Activated)
            {
                AppendElementValue(xmlDoc, elemPallet, "overhangX", UnitsManager.UnitType.UT_LENGTH, vOverhang.X);
                AppendElementValue(xmlDoc, elemPallet, "overhangY", UnitsManager.UnitType.UT_LENGTH, vOverhang.Y);
            }
            if (rnPallet.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                // --- build image
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                {
                    FontSizeRatio = FontSizeRatioDetail,
                    CameraPosition = Graphics3D.Corner_0,
                    Target = Vector3D.Zero
                };
                Pallet pallet = new Pallet(palletProp);
                pallet.Draw(graphics, Transform3D.Identity);
                if (ShowDimensions)
                    graphics.AddDimensions(new DimensionCube(palletProp.Length, palletProp.Width, palletProp.Height));
                graphics.Flush();
                // ---
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemPallet, graphics.Bitmap);
            }

        }

        private void AppendCylinderElement(RevSolidProperties cylProperties, ReportNode rnCylinder, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // get CylinderProperties
            XmlElement elemCylinder = xmlDoc.CreateElement("cylinder", ns);
            elemAnalysis.AppendChild(elemCylinder);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = cylProperties.Name;
            elemCylinder.AppendChild(elemName);
            // description
            if (rnCylinder.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = cylProperties.Description;
                elemCylinder.AppendChild(elemDescription);
            }
            if (rnCylinder.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
            {
                AppendElementValue(xmlDoc, elemCylinder, "diameter", UnitsManager.UnitType.UT_LENGTH, cylProperties.Diameter);
                AppendElementValue(xmlDoc, elemCylinder, "height", UnitsManager.UnitType.UT_LENGTH, cylProperties.Height);
                AppendElementValue(xmlDoc, elemCylinder, "weight", UnitsManager.UnitType.UT_MASS, cylProperties.Weight);
            }
            if (rnCylinder.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                // --- build image
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                {
                    FontSizeRatio = FontSizeRatioDetail,
                    CameraPosition = Graphics3D.Corner_0,
                    Target = Vector3D.Zero
                };
                Cyl cyl = null;
                if (cylProperties is CylinderProperties cylProp)
                    cyl = new Cylinder(0, cylProp);
                else if (cylProperties is BottleProperties bottleProp)
                    cyl = new Bottle(0, bottleProp);
                graphics.AddCylinder(cyl);
                if (ShowDimensions)
                {
                    graphics.AddDimensions( new DimensionCube(
                        new Vector3D(-cyl.RadiusOuter, -cyl.RadiusOuter, 0.0),
                        cyl.DiameterOuter, cyl.DiameterOuter, cyl.Height,
                        Color.Black, false));
                }
                graphics.Flush();
                // ---
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemCylinder, graphics.Bitmap);
            }
        }

        private void AppendBundleElement(BundleProperties bundleProp, ReportNode rnBundle, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            if (null == bundleProp) return;
            // bundle
            XmlElement elemBundle = CreateElement("bundle", null, elemAnalysis, xmlDoc, ns);
            elemAnalysis.AppendChild(elemBundle);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = bundleProp.Name;
            elemBundle.AppendChild(elemName);
            // description
            if (rnBundle.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = bundleProp.Description;
                elemBundle.AppendChild(elemDescription);
            }
            // length / width / number of flats / unit thickness / unit weight / total thickness / total weight
            if (rnBundle.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
            {
                AppendElementValue(xmlDoc, elemBundle, "length", UnitsManager.UnitType.UT_LENGTH, bundleProp.Length);
                AppendElementValue(xmlDoc, elemBundle, "width", UnitsManager.UnitType.UT_LENGTH, bundleProp.Width);
                AppendElementValue(xmlDoc, elemBundle, "unitThickness", UnitsManager.UnitType.UT_LENGTH, bundleProp.UnitThickness);
                AppendElementValue(xmlDoc, elemBundle, "totalThickness", UnitsManager.UnitType.UT_LENGTH, bundleProp.UnitThickness * bundleProp.NoFlats);
            }
            AppendElementValue(xmlDoc, elemBundle, "numberOfFlats", bundleProp.NoFlats);
            if (rnBundle.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
            {
                AppendElementValue(xmlDoc, elemBundle, "unitWeight", UnitsManager.UnitType.UT_MASS, bundleProp.UnitWeight);
                AppendElementValue(xmlDoc, elemBundle, "weightTotal", UnitsManager.UnitType.UT_MASS, bundleProp.UnitWeight * bundleProp.NoFlats);
            }
            if (rnBundle.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                // --- build image
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                {
                    FontSizeRatio = FontSizeRatioDetail,
                    CameraPosition = Graphics3D.Corner_0
                };
                Box box = new Box(0, bundleProp);
                graphics.AddBox(box);
                if (ShowDimensions)
                {
                    DimensionCube dc = new DimensionCube(bundleProp.Length, bundleProp.Width, bundleProp.Height);
                    graphics.AddDimensions(dc);
                }
                graphics.Flush();
                // ---
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemBundle, graphics.Bitmap);
            }
        }
        #endregion

        #region Dimensions
        private void AppendInterlayerElement(InterlayerProperties interlayerProp, ReportNode rnInterlayer, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            // sanity check
            if (null == interlayerProp) return;
            // namespace
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // interlayer
            XmlElement elemInterlayer = xmlDoc.CreateElement("interlayer", ns);
            elemAnalysis.AppendChild(elemInterlayer);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = interlayerProp.Name;
            elemInterlayer.AppendChild(elemName);
            // description
            if (rnInterlayer.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = interlayerProp.Description;
                elemInterlayer.AppendChild(elemDescription);
            }
            if (rnInterlayer.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
            {
                AppendElementValue(xmlDoc, elemInterlayer, "length", UnitsManager.UnitType.UT_LENGTH, interlayerProp.Length);
                AppendElementValue(xmlDoc, elemInterlayer, "width", UnitsManager.UnitType.UT_LENGTH, interlayerProp.Width);
                AppendElementValue(xmlDoc, elemInterlayer, "thickness", UnitsManager.UnitType.UT_LENGTH, interlayerProp.Thickness);
            }
            if (rnInterlayer.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
                AppendElementValue(xmlDoc, elemInterlayer, "weight", UnitsManager.UnitType.UT_MASS, interlayerProp.Weight);

            ReportNode rnImage = rnInterlayer.GetChildByName(Resources.ID_RN_IMAGE);
            if (rnImage.Activated)
            {
                // --- build image
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail));
                graphics.FontSizeRatio = FontSizeRatioDetail;
                graphics.CameraPosition = Graphics3D.Corner_0;
                Box box = new Box(0, interlayerProp);
                graphics.AddBox(box);
                if (ShowDimensions)
                {
                    DimensionCube dc = new DimensionCube(interlayerProp.Length, interlayerProp.Width, interlayerProp.Thickness); 
                    graphics.AddDimensions(dc);
                }
                graphics.Flush();
                // ---
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemInterlayer, graphics.Bitmap);
            }
        }

        private void AppendPalletCornerElement(PalletCornerProperties palletCornerProp, XmlElement elemPalletAnalysis, XmlDocument xmlDoc)
        {
            // sanity check
            if (null == palletCornerProp) return;
            // namespace
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // interlayer
            XmlElement elemPalletCorner = xmlDoc.CreateElement("palletCorner", ns);
            elemPalletAnalysis.AppendChild(elemPalletCorner);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = palletCornerProp.Name;
            elemPalletCorner.AppendChild(elemName);
            // description
            XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
            elemDescription.InnerText = palletCornerProp.Description;
            elemPalletCorner.AppendChild(elemDescription);

            AppendElementValue(xmlDoc, elemPalletCorner, "length", UnitsManager.UnitType.UT_LENGTH, palletCornerProp.Length);
            AppendElementValue(xmlDoc, elemPalletCorner, "width", UnitsManager.UnitType.UT_LENGTH, palletCornerProp.Width);
            AppendElementValue(xmlDoc, elemPalletCorner, "thickness", UnitsManager.UnitType.UT_LENGTH, palletCornerProp.Thickness);
            AppendElementValue(xmlDoc, elemPalletCorner, "weight", UnitsManager.UnitType.UT_LENGTH, palletCornerProp.Weight);
            // ---
            // view_palletCorner_iso
            // build image
            Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
            {
                CameraPosition = Graphics3D.Corner_0
            };
            Corner corner = new Corner(0, palletCornerProp);
            corner.Draw(graphics);
            graphics.Flush();
            // imageThumb
            AppendThumbnailElement(xmlDoc, elemPalletCorner, graphics.Bitmap);
        }

        private void AppendPalletCapElement(PalletCapProperties palletCapProp, XmlElement elemPalletAnalysis, XmlDocument xmlDoc)
        {
            // sanity check
            if (null == palletCapProp) return;
            // namespace
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // interlayer
            XmlElement elemPalletCap = xmlDoc.CreateElement("palletCap", ns);
            elemPalletAnalysis.AppendChild(elemPalletCap);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = palletCapProp.Name;
            elemPalletCap.AppendChild(elemName);
            // description
            XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
            elemDescription.InnerText = palletCapProp.Description;
            elemPalletCap.AppendChild(elemDescription);

            AppendElementValue(xmlDoc, elemPalletCap, "length", UnitsManager.UnitType.UT_LENGTH, palletCapProp.Length);
            AppendElementValue(xmlDoc, elemPalletCap, "width", UnitsManager.UnitType.UT_LENGTH, palletCapProp.Width);
            AppendElementValue(xmlDoc, elemPalletCap, "height", UnitsManager.UnitType.UT_LENGTH, palletCapProp.Height);
            AppendElementValue(xmlDoc, elemPalletCap, "innerLength", UnitsManager.UnitType.UT_LENGTH, palletCapProp.InsideLength);
            AppendElementValue(xmlDoc, elemPalletCap, "innerWidth", UnitsManager.UnitType.UT_LENGTH, palletCapProp.InsideWidth);
            AppendElementValue(xmlDoc, elemPalletCap, "innerHeight", UnitsManager.UnitType.UT_LENGTH, palletCapProp.InsideHeight);
            AppendElementValue(xmlDoc, elemPalletCap, "weight", UnitsManager.UnitType.UT_MASS, palletCapProp.Weight);
            // ---
            // view_palletCap_iso
            // build image
            Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail));
            graphics.FontSizeRatio = FontSizeRatioDetail;
            graphics.CameraPosition = Graphics3D.Corner_0;
            PalletCap palletCap = new PalletCap(0, palletCapProp, BoxPosition.Zero);
            palletCap.Draw(graphics);
            if (Reporter.ShowDimensions)
                graphics.AddDimensions(new DimensionCube(palletCapProp.Length, palletCapProp.Width, palletCapProp.Height));
            graphics.Flush();
            // imageThumb
            AppendThumbnailElement(xmlDoc, elemPalletCap, graphics.Bitmap);
        }

        private void AppendPalletFilmElement(PalletFilmProperties palletFilmProp, AnalysisLayered analyis, XmlElement elemPalletAnalysis, XmlDocument xmlDoc)
        {
            // sanity check
            if (null == palletFilmProp) return;
            // namespace
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // interlayer
            XmlElement elemPalletFilm = xmlDoc.CreateElement("palletFilm", ns);
            elemPalletAnalysis.AppendChild(elemPalletFilm);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = palletFilmProp.Name;
            elemPalletFilm.AppendChild(elemName);
            // description
            XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
            elemDescription.InnerText = palletFilmProp.Description;
            elemPalletFilm.AppendChild(elemDescription);
            // number of turns
            ConstraintSetCasePallet constaintSet = analyis.ConstraintSet as ConstraintSetCasePallet;
            XmlElement elemNumberOfTurns = xmlDoc.CreateElement("numberOfTurns", ns);
            elemNumberOfTurns.InnerText = constaintSet.PalletFilmTurns.ToString();
            elemPalletFilm.AppendChild(elemNumberOfTurns);
        }

        #region Helpers
        private void AppendThumbnailElement(XmlDocument xmlDoc, XmlElement parentElement, Bitmap bmp)
        {
            // get image path
            string imagePath = SaveImageAs(bmp);
            // namespace
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // imageThumb element
            XmlElement elemImage = xmlDoc.CreateElement("imageThumbSize", ns);
            parentElement.AppendChild(elemImage);
            // imagePath element
            XmlElement elemImagePath = xmlDoc.CreateElement("imagePath", ns);
            elemImage.AppendChild(elemImagePath);
            elemImagePath.InnerText = imagePath;
            // width
            XmlElement elemWidth = xmlDoc.CreateElement("width");
            elemImage.AppendChild(elemWidth);
            elemWidth.InnerText = ImageHTMLSizeDetail.ToString();
            // height
            XmlElement elemHeight = xmlDoc.CreateElement("height");
            elemImage.AppendChild(elemHeight);
            elemHeight.InnerText = ImageHTMLSizeDetail.ToString();
        }
        #endregion

        private void AppendTruckElement(TruckProperties truckProp, ReportNode rnTruck, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // get truckProperties
            if (null == truckProp) return;
            // create "truck" element
            XmlElement elemTruck = xmlDoc.CreateElement("truck", ns);
            elemAnalysis.AppendChild(elemTruck);
            // name
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = truckProp.Name;
            elemTruck.AppendChild(elemName);
            // description
            if (rnTruck.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
            {
                XmlElement elemDescription = xmlDoc.CreateElement("description", ns);
                elemDescription.InnerText = truckProp.Description;
                elemTruck.AppendChild(elemDescription);
            }
            // dimensions
            if (rnTruck.GetChildByName(Resources.ID_RN_DIMENSIONSINNER).Activated)
            {
                AppendElementValue(xmlDoc, elemTruck, "length", UnitsManager.UnitType.UT_LENGTH, truckProp.Length);
                AppendElementValue(xmlDoc, elemTruck, "width", UnitsManager.UnitType.UT_LENGTH, truckProp.Width);
                AppendElementValue(xmlDoc, elemTruck, "height", UnitsManager.UnitType.UT_LENGTH, truckProp.Height);
            }
            AppendElementValue(xmlDoc, elemTruck, "admissibleLoad", UnitsManager.UnitType.UT_MASS, truckProp.AdmissibleLoadWeight);

            // --- build image
            if (rnTruck.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                {
                    FontSizeRatio = FontSizeRatioDetail,
                    CameraPosition = Graphics3D.Corner_0
                };
                Truck truck = new Truck(truckProp);
                truck.DrawBegin(graphics);
                truck.DrawEnd(graphics);
                if (Reporter.ShowDimensions)
                    graphics.AddDimensions(new DimensionCube(truckProp.Length, truckProp.Width, truckProp.Height));
                graphics.Flush();
                AppendThumbnailElement(xmlDoc, elemTruck, graphics.Bitmap);
            }
        }
        #endregion

        #region BoxProperties / PackProperties
        private void AppendCaseElement(BoxProperties caseProperties, ReportNode rnCase, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // case element
            XmlElement elemCase = CreateElement("caseWithInnerDims", null, elemAnalysis, xmlDoc, ns);
            // name
            CreateElement("name", caseProperties.Name, elemCase, xmlDoc, ns);
            // description
            if (rnCase.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
                CreateElement("description", caseProperties.Description, elemCase, xmlDoc, ns);
            // length / width /height
            if (rnCase.GetChildByName(Resources.ID_RN_DIMENSIONSOUTER).Activated)
            {
                AppendElementValue(xmlDoc, elemCase, "length", UnitsManager.UnitType.UT_LENGTH, caseProperties.Length);
                AppendElementValue(xmlDoc, elemCase, "width", UnitsManager.UnitType.UT_LENGTH, caseProperties.Width);
                AppendElementValue(xmlDoc, elemCase, "height", UnitsManager.UnitType.UT_LENGTH, caseProperties.Height);
            }
            // innerLength / innerWidth / innerHeight
            if (rnCase.GetChildByName(Resources.ID_RN_DIMENSIONSINNER).Activated)
            {
                AppendElementValue(xmlDoc, elemCase, "innerLength", UnitsManager.UnitType.UT_LENGTH, caseProperties.InsideLength);
                AppendElementValue(xmlDoc, elemCase, "innerWidth", UnitsManager.UnitType.UT_LENGTH, caseProperties.InsideWidth);
                AppendElementValue(xmlDoc, elemCase, "innerHeight", UnitsManager.UnitType.UT_LENGTH, caseProperties.InsideHeight);
            }
            // weight
            if (rnCase.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
            {
                AppendElementValue(xmlDoc, elemCase, "weight", UnitsManager.UnitType.UT_MASS, caseProperties.Weight);
            }
            // image
            if (rnCase.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail))
                {
                    FontSizeRatio = FontSizeRatioDetail,
                    CameraPosition = Graphics3D.Corner_0,
                    Target = Vector3D.Zero
                };
                Box box = new Box(0, caseProperties);
                graphics.AddBox(box);
                if (Reporter.ShowDimensions)
                    graphics.AddDimensions(new DimensionCube(box.Length, box.Width, box.Height));
                graphics.Flush();
                // ---
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemCase, graphics.Bitmap);
            }
        }
        private void AppendPackElement(PackProperties packProperties, ReportNode rnPack, XmlElement elemPackAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // pack element
            XmlElement elemPack = CreateElement("pack", null, elemPackAnalysis, xmlDoc, ns);
            // name
            CreateElement("name", packProperties.Name, elemPack, xmlDoc, ns);
            // description
            if (rnPack.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
                CreateElement("description", packProperties.Description, elemPack, xmlDoc, ns);
            // arrangement
            if (rnPack.GetChildByName(Resources.ID_RN_ARRANGEMENT).Activated)
            {
                PackArrangement arrangement = packProperties.Arrangement;
                CreateElement(
                    "arrangement"
                    , string.Format("{0} * {1} * {2}", arrangement.Length, arrangement.Width, arrangement.Height)
                    , elemPack, xmlDoc, ns);
            }
            // length / width /height
            if (rnPack.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
            {
                AppendElementValue(xmlDoc, elemPack, "length", UnitsManager.UnitType.UT_LENGTH, packProperties.Length);
                AppendElementValue(xmlDoc, elemPack, "width", UnitsManager.UnitType.UT_LENGTH, packProperties.Width);
                AppendElementValue(xmlDoc, elemPack, "height", UnitsManager.UnitType.UT_LENGTH, packProperties.Height);
            }
            // weight
            if (rnPack.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
            {
                AppendElementValue(xmlDoc, elemPack, "netWeight", UnitsManager.UnitType.UT_MASS, packProperties.NetWeight);
                if (null != packProperties.Wrap)
                    AppendElementValue(xmlDoc, elemPack, "wrapperWeight", UnitsManager.UnitType.UT_MASS, packProperties.Wrap.Weight);
                if (null != packProperties.Tray)
                    AppendElementValue(xmlDoc, elemPack, "trayWeight", UnitsManager.UnitType.UT_MASS, packProperties.Tray.Weight);
                AppendElementValue(xmlDoc, elemPack, "weight", UnitsManager.UnitType.UT_MASS, packProperties.Weight);
            }
            if (rnPack.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                // --- build image
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail));
                graphics.FontSizeRatio = FontSizeRatioDetail;
                graphics.CameraPosition = Graphics3D.Corner_0;
                graphics.Target = Vector3D.Zero;
                Pack pack = new Pack(0, packProperties);
                pack.ForceTransparency = true;
                graphics.AddBox(pack);
                if (Reporter.ShowDimensions)
                    graphics.AddDimensions(new DimensionCube(pack.Length, pack.Width, pack.Height));
                graphics.Flush();
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemPack, graphics.Bitmap);
            }
        }

        private void AppendBoxElement(BoxProperties boxProperties, ReportNode rnBox, XmlElement elemAnalysis, XmlDocument xmlDoc)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            // box element
            XmlElement elemBox = CreateElement("box", null, elemAnalysis, xmlDoc, ns);
            // name
            CreateElement("name", boxProperties.Name, elemBox, xmlDoc, ns);
            // description
            if (rnBox.GetChildByName(Resources.ID_RN_DESCRIPTION).Activated)
                CreateElement("description", boxProperties.Description, elemBox, xmlDoc, ns);
            // dimensions
            if (rnBox.GetChildByName(Resources.ID_RN_DIMENSIONS).Activated)
            {
                AppendElementValue(xmlDoc, elemBox, "length", UnitsManager.UnitType.UT_LENGTH, boxProperties.Length);
                AppendElementValue(xmlDoc, elemBox, "width", UnitsManager.UnitType.UT_LENGTH, boxProperties.Width);
                AppendElementValue(xmlDoc, elemBox, "height", UnitsManager.UnitType.UT_LENGTH, boxProperties.Height);
            }
            // weight
            if (rnBox.GetChildByName(Resources.ID_RN_WEIGHT).Activated)
                AppendElementValue(xmlDoc, elemBox, "weight", UnitsManager.UnitType.UT_MASS, boxProperties.Weight);
            // image
            if (rnBox.GetChildByName(Resources.ID_RN_IMAGE).Activated)
            {
                // --- build image
                Graphics3DImage graphics = new Graphics3DImage(new Size(ImageSizeDetail, ImageSizeDetail));
                graphics.FontSizeRatio = FontSizeRatioDetail;
                graphics.CameraPosition = Graphics3D.Corner_0;
                graphics.Target = Vector3D.Zero;
                Box box = new Box(0, boxProperties);
                graphics.AddBox(box);
                if (Reporter.ShowDimensions)
                    graphics.AddDimensions(new DimensionCube(box.Length, box.Width, box.Height));
                graphics.Flush();
                // ---
                // imageThumb
                AppendThumbnailElement(xmlDoc, elemBox, graphics.Bitmap);
            }
        }
        #endregion

        #region Helpers
        private static XmlElement CreateElement(string eltName, string innerValue, XmlElement parentElt, XmlDocument xmlDoc, string ns)
        {
            XmlElement elt = xmlDoc.CreateElement(eltName, ns);
            if (!string.IsNullOrEmpty(innerValue))
                elt.InnerText = innerValue;
            parentElt.AppendChild(elt);
            return elt;
        }
        private static XmlElement CreateElement(string eltName, double v, XmlElement parentElt, XmlDocument xmlDoc, string ns)
        {
            XmlElement elt = xmlDoc.CreateElement(eltName, ns);
            elt.InnerText = string.Format("{0:F}", v);
            parentElt.AppendChild(elt);
            return elt;
        }
        private static XmlElement CreateElement(string eltName, int v, XmlElement parentElt, XmlDocument xmlDoc, string ns)
        {
            XmlElement elt = xmlDoc.CreateElement(eltName, ns);
            elt.InnerText = string.Format("{0}", v);
            parentElt.AppendChild(elt);
            return elt;
        }
        private static void AppendElementValue(XmlDocument xmlDoc, XmlElement parent, string eltName, UnitsManager.UnitType unitType, OptDouble optValue)
        {
            if (optValue.Activated)
            {
                // eltName
                XmlElement createdElement = xmlDoc.CreateElement(eltName, xmlDoc.DocumentElement.NamespaceURI);
                parent.AppendChild(createdElement);
                // unit
                XmlElement unitElement = xmlDoc.CreateElement("unit", xmlDoc.DocumentElement.NamespaceURI);
                unitElement.InnerText = UnitsManager.UnitString(unitType);
                createdElement.AppendChild(unitElement);
                // value
                XmlElement valueElement = xmlDoc.CreateElement("value", xmlDoc.DocumentElement.NamespaceURI);
                valueElement.InnerText = string.Format(UnitsManager.UnitFormat(unitType), optValue.Value);
                createdElement.AppendChild(valueElement);
            }
        }
        private static void AppendElementBB(XmlDocument xmlDoc, XmlElement parent, string eltName, UnitsManager.UnitType unitType, double[] eltValue)
        {
            // eltName
            XmlElement createdElement = xmlDoc.CreateElement(eltName, xmlDoc.DocumentElement.NamespaceURI);
            parent.AppendChild(createdElement);
            // unit
            XmlElement unitElement = xmlDoc.CreateElement("unit", xmlDoc.DocumentElement.NamespaceURI);
            unitElement.InnerText = UnitsManager.UnitString(unitType);
            createdElement.AppendChild(unitElement);
            // v0
            XmlElement valueElement0 = xmlDoc.CreateElement("v0", xmlDoc.DocumentElement.NamespaceURI);
            valueElement0.InnerText = string.Format(UnitsManager.UnitFormat(unitType), eltValue[0]);
            createdElement.AppendChild(valueElement0);
            // v1
            XmlElement valueElement1 = xmlDoc.CreateElement("v1", xmlDoc.DocumentElement.NamespaceURI);
            valueElement1.InnerText = string.Format(UnitsManager.UnitFormat(unitType), eltValue[1]);
            createdElement.AppendChild(valueElement1);
            // v2
            XmlElement valueElement2 = xmlDoc.CreateElement("v2", xmlDoc.DocumentElement.NamespaceURI);
            valueElement2.InnerText = string.Format(UnitsManager.UnitFormat(unitType), eltValue[2]);
            createdElement.AppendChild(valueElement2);
        }
        private static void AppendElementValue(XmlDocument xmlDoc, XmlElement parent, string eltName, UnitsManager.UnitType unitType, double eltValue)
        {
            // eltName
            XmlElement createdElement = xmlDoc.CreateElement(eltName, xmlDoc.DocumentElement.NamespaceURI);
            parent.AppendChild(createdElement);
            // unit
            XmlElement unitElement = xmlDoc.CreateElement("unit", xmlDoc.DocumentElement.NamespaceURI);
            unitElement.InnerText = UnitsManager.UnitString(unitType);
            createdElement.AppendChild(unitElement);
            // value
            XmlElement valueElement = xmlDoc.CreateElement("value", xmlDoc.DocumentElement.NamespaceURI);
            valueElement.InnerText = string.Format(UnitsManager.UnitFormat(unitType), eltValue);
            createdElement.AppendChild(valueElement);
        }
        private static void AppendElementValue(XmlDocument xmlDoc, XmlElement parent, string eltName, double eltValue)
        {
            XmlElement createdElement = xmlDoc.CreateElement(eltName, xmlDoc.DocumentElement.NamespaceURI);
            createdElement.InnerText = string.Format("{0:0.#}", eltValue);
            parent.AppendChild(createdElement);
        }
        private static void AppendElementValue(XmlDocument xmlDoc, XmlElement parent, string eltName, int eltValue)
        {
            XmlElement createdElement = xmlDoc.CreateElement(eltName, xmlDoc.DocumentElement.NamespaceURI);
            createdElement.InnerText = string.Format("{0}", eltValue);
            parent.AppendChild(createdElement);
        }
        private static void AppendElementValue(XmlDocument xmlDoc, XmlElement parent, string eltName, string eltValue)
        {
            XmlElement createdElement = xmlDoc.CreateElement(eltName, xmlDoc.DocumentElement.NamespaceURI);
            createdElement.InnerText = string.Format("{0}", eltValue);
            parent.AppendChild(createdElement);
        }
        private static void AppendContentItem(XmlDocument xmlDoc, XmlElement parent, string itemName, int itemQuantity)
        {
            string ns = xmlDoc.DocumentElement.NamespaceURI;
            XmlElement elemItem = xmlDoc.CreateElement("item", ns);
            parent.AppendChild(elemItem);
            // itemName
            XmlElement elemName = xmlDoc.CreateElement("name", ns);
            elemName.InnerText = itemName;
            elemItem.AppendChild(elemName);
            // itemQuantity
            XmlElement elemValue = xmlDoc.CreateElement("value", ns);
            elemValue.InnerText = itemQuantity.ToString();
            elemItem.AppendChild(elemValue);
        }
        private string SaveImageAs(Bitmap bmp)
        {
            if (!WriteImageFiles) return string.Empty;
            string fileName = string.Format("image_{0}.png", ++ImageIndex);
            try { bmp.Save(Path.Combine(ImageDirectory, fileName), System.Drawing.Imaging.ImageFormat.Png); }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
            return @"images\" + fileName;
        }
        public string ToAbsolute(string pathString)
        {
            if (Path.IsPathRooted(pathString))
                return pathString;
            else
                return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), pathString));
        }
        #endregion

        #region Deleting methods
        public void DeleteImageDirectory()
        {
            try { Directory.Delete(ImageDirectory, true); }
            catch (Exception ex) { _log.Error(ex.Message); }
        }
        #endregion

        #region XML validator
        // Validation Error Count
        static int ErrorsCount = 0;
        // Validation Error Message
        static string ErrorMessage = "";

        private static void ValidateXmlDocument(XmlReader documentToValidate, string schemaPath)
        {
            XmlSchema schema;
            using (var schemaReader = XmlReader.Create(schemaPath))
            {
                schema = XmlSchema.Read(schemaReader, ValidationEventHandler);
            }

            var schemas = new XmlSchemaSet();
            schemas.Add(schema);
            var settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = schemas;
            settings.ValidationFlags =
                XmlSchemaValidationFlags.ProcessIdentityConstraints |
                XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += ValidationEventHandler;

            using (var validationReader = XmlReader.Create(documentToValidate, settings))
            { while (validationReader.Read()) { } }

            if (ErrorsCount > 0)
                throw new Exception(ErrorMessage);
        }

        private static void ValidationEventHandler(
            object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                ErrorMessage = ErrorMessage + args.Message + "\r\n";
                ErrorsCount++;
            }
        }
        #endregion
    }
    #endregion
}
