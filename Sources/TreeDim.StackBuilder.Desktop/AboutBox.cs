﻿#region Using directives
using System;
using System.Windows.Forms;
using System.Reflection;
using System.Resources;
using treeDiM.StackBuilder.Desktop.Properties;

using treeDiM.PLMPack.DBClient;

using log4net;
#endregion

namespace treeDiM.StackBuilder.Desktop
{
    partial class AboutBox : Form
    {
        #region Data members
        static readonly ILog _log = LogManager.GetLogger(typeof(AboutBox));
        #endregion

        #region Constructor
        public AboutBox()
        {
            InitializeComponent();

            //  Initialize the AboutBox to display the product information from the assembly information.
            //  Change assembly information settings for your application through either:
            //  - Project->Properties->Application->Assembly Information
            //  - AssemblyInfo.cs

            ResourceManager resourceManager = new ResourceManager("treeDiM.StackBuilder.Desktop",
                Assembly.GetExecutingAssembly());

            this.Text = String.Format(Resources.ID_ABOUT, AssemblyTitle);
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyVersion);
            this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
            this.textBoxDescription.Text = AssemblyDescription;
            this.labelRegisteredUserCount.Text = RegisteredUserCount;
        }
        #endregion

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                // Get all Title attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                // If there is at least one Title attribute
                if (attributes.Length > 0)
                {
                    // Select the first one
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    // If it is not an empty string, return it
                    if (titleAttribute.Title != "")
                        return titleAttribute.Title;
                }
                // If there was no Title attribute, or if the Title attribute was the empty string, return the .exe name
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                // Get all Description attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                // If there aren't any Description attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Description attribute, return its value
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                // Get all Product attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                // If there aren't any Product attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Product attribute, return its value
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                // Get all Copyright attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                // If there aren't any Copyright attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Copyright attribute, return its value
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                // Get all Company attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                // If there aren't any Company attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Company attribute, return its value
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }

        public string CompanyUrl
        {
            set { linkLabelUrl.Text = value; }
            get { return linkLabelUrl.Text; }
        }

        public string SupportEmail
        {
            set { this.linkLabelEmail.Text = value; }
            get { return this.linkLabelEmail.Text; }
        }

        public string RegisteredUserCount
        {
            get
            {
                try
                {
                    int userCount = 0;
                    using (WCFClient wcfClient = new WCFClient())
                    {
                        var client = wcfClient.Client;
                        userCount = null != client ? client.get_PLMPackRegisteredUserCount() : 0;
                    }
                    return string.Format(Resources.ID_REGISTEREDUSERCOUNT, userCount);
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                    return string.Empty;
                }
            }
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Web site url click handler
        /// </summary>
        private void OnLinkLabelUrlClick(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(this.linkLabelUrl.Text);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        /// <summary>
        /// Email address click hnadler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLinkLabelEmailClick(object sender, EventArgs e)
        { 
            try
            {
                System.Diagnostics.Process.Start(
                    "mailto:" + this.linkLabelEmail.Text
                    + "?subject="+ AssemblyTitle + " " + AssemblyVersion + " bug report");
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }        
        }
        #endregion

    }
}
