﻿#region Using directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using Sharp3D.Math.Core;
using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Exporters;
#endregion

namespace treeDiM.StackBuilder.TechnologyBSA_ASPNET
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                DropDownListFiles.DataSource = FtpHelpers.GetListOfFiles(ConfigSettings.FtpDirectory, ConfigSettings.FtpUsername, ConfigSettings.FtpPassword);
                DropDownListFiles.DataBind();

                DimCase = new Vector3D(300.0, 280.0, 275.0);
                WeightCase = 1.0;
                WeightPallet = 23.0;
                PalletIndex = 0;
                NumberOfLayers = 8;
                BoxPositions = new List<BoxPositionIndexed>();
            }
        }

        protected void OnNewProject(object sender, EventArgs e)
        {
            DimCase = new Vector3D(300.0, 280.0, 275.0);
            WeightCase = 1.0;
            WeightPallet = 23.0;
            PalletIndex = 0;
            NumberOfLayers = 5;
            LayersMirrorX = false; LayersMirrorY = false;
            Interlayers = "";
            FileName = "Untitled.csv";
            LayerDesignMode = LayerDesignModeCtrl;

            if (LayerDesignModeCtrl == 0)
                Response.Redirect("LayerDesignIntro.aspx");
            else
                Response.Redirect("LayerSelectionWebGL.aspx");
        }

        protected void OnOpenProject(object sender, EventArgs e)
        {
            // get selected file name
            var dimCase = Vector3D.Zero;
            double weightCase = 0.0;
            var dimPallet = Vector3D.Zero;
            double weightPallet = 0.0;
            bool MirrorX = false, MirrorY = false;
            int numberOfLayers = 0;
            List<BoxPositionIndexed> boxPositions = new List<BoxPositionIndexed>();
            List<bool> interlayers = new List<bool>();
            int layerDesignMode = 0;

            string filePath = DropDownListFiles.SelectedValue;
            byte[] fileContent = null;
            FtpHelpers.Download(ref fileContent, ConfigSettings.FtpDirectory, filePath, ConfigSettings.FtpUsername, ConfigSettings.FtpPassword);
            ExporterCSV_TechBSA.Import(new MemoryStream(fileContent),
                ref boxPositions,
                ref dimCase, ref weightCase,
                ref dimPallet, ref weightPallet,
                ref numberOfLayers,
                ref MirrorX, ref MirrorY,
                ref interlayers,
                ref layerDesignMode);

            DimCase = dimCase; WeightCase = weightCase;
            PalletIndex = 0; WeightPallet = weightPallet;
            NumberOfLayers = numberOfLayers;
            BoxPositions = boxPositions;
            LayersMirrorX = MirrorX;
            LayersMirrorY = MirrorY;
            LayerEdited = true;
            FileName = filePath;
            Interlayers = string.Concat(interlayers.Select(p => p ? "1" : "0").ToArray());
            LayerDesignMode = layerDesignMode;

            if (ConfigSettings.WebGLMode)
                Response.Redirect("ValidationWebGL.aspx");
            else
                Response.Redirect("Validation.aspx");
        }

        #region Private properties
        private Vector3D DimCase
        { set => Session[SessionVariables.DimCase] = value.ToString(); }
        private double WeightCase
        { set => Session[SessionVariables.WeightCase] = value; }
        private int PalletIndex
        { set => Session[SessionVariables.PalletIndex] = value; }
        private double WeightPallet
        { set => Session[SessionVariables.WeightPallet] = value; }
        private int NumberOfLayers
        { set => Session[SessionVariables.NumberOfLayers] = value; }
        private List<BoxPositionIndexed> BoxPositions
        { set => Session[SessionVariables.BoxPositions] = value; }
        private bool LayersMirrorX
        { set => Session[SessionVariables.LayersMirrorLength] = value; }
        private bool LayersMirrorY
        { set => Session[SessionVariables.LayersMirrorWidth] = value; }
        private string FileName
        { set => Session[SessionVariables.FileName] = value; }
        private string Interlayers
        { set => Session[SessionVariables.Interlayers] = value; }
        private bool LayerEdited
        { set => Session[SessionVariables.LayerEdited] = value; }
        private int LayerDesignModeCtrl => DropDownLayerDesignMode.SelectedIndex;
        private int LayerDesignMode
        { set => Session[SessionVariables.LayerDesignMode] = value; }
        #endregion
    }
}