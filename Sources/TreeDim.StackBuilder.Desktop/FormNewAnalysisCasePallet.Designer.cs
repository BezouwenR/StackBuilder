﻿namespace treeDiM.StackBuilder.Desktop
{
    partial class FormNewAnalysisCasePallet
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormNewAnalysisCasePallet));
            this.cbPallets = new treeDiM.StackBuilder.Graphics.Controls.CCtrlComboFiltered();
            this.cbCases = new treeDiM.StackBuilder.Graphics.Controls.CCtrlComboFiltered();
            this.lbBox = new System.Windows.Forms.Label();
            this.uCtrlOverhang = new treeDiM.StackBuilder.Basics.UCtrlDualDouble();
            this.lbPallet = new System.Windows.Forms.Label();
            this.uCtrlLayerList = new treeDiM.StackBuilder.Graphics.UCtrlLayerList();
            this.checkBoxBestLayersOnly = new System.Windows.Forms.CheckBox();
            this.bnBestCombination = new System.Windows.Forms.Button();
            this.uCtrlCaseOrientation = new treeDiM.StackBuilder.Graphics.uCtrlCaseOrientation();
            this.tabCtrlConstraints = new System.Windows.Forms.TabControl();
            this.tabPageStopCriterions = new System.Windows.Forms.TabPage();
            this.uCtrlOptMaxNumber = new treeDiM.StackBuilder.Basics.UCtrlOptInt();
            this.uCtrlOptMaximumWeight = new treeDiM.StackBuilder.Basics.UCtrlOptDouble();
            this.uCtrlMaximumHeight = new treeDiM.StackBuilder.Basics.UCtrlDouble();
            this.tabPageOverhang = new System.Windows.Forms.TabPage();
            this.tabPageSpaces = new System.Windows.Forms.TabPage();
            this.uCtrlOptSpace = new treeDiM.StackBuilder.Basics.UCtrlOptDouble();
            this.lbSelect = new System.Windows.Forms.Label();
            this.tabCtrlConstraints.SuspendLayout();
            this.tabPageStopCriterions.SuspendLayout();
            this.tabPageOverhang.SuspendLayout();
            this.tabPageSpaces.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbDescription
            // 
            resources.ApplyResources(this.tbDescription, "tbDescription");
            // 
            // cbPallets
            // 
            this.cbPallets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPallets.FormattingEnabled = true;
            resources.ApplyResources(this.cbPallets, "cbPallets");
            this.cbPallets.Name = "cbPallets";
            this.cbPallets.SelectedIndexChanged += new System.EventHandler(this.OnInputChanged);
            // 
            // cbCases
            // 
            this.cbCases.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCases.FormattingEnabled = true;
            resources.ApplyResources(this.cbCases, "cbCases");
            this.cbCases.Name = "cbCases";
            this.cbCases.SelectedIndexChanged += new System.EventHandler(this.OnCaseChanged);
            // 
            // lbBox
            // 
            resources.ApplyResources(this.lbBox, "lbBox");
            this.lbBox.Name = "lbBox";
            // 
            // uCtrlOverhang
            // 
            resources.ApplyResources(this.uCtrlOverhang, "uCtrlOverhang");
            this.uCtrlOverhang.MinValue = -10000D;
            this.uCtrlOverhang.Name = "uCtrlOverhang";
            this.uCtrlOverhang.Unit = treeDiM.StackBuilder.Basics.UnitsManager.UnitType.UT_LENGTH;
            this.uCtrlOverhang.ValueX = 0D;
            this.uCtrlOverhang.ValueY = 0D;
            this.uCtrlOverhang.ValueChanged += new treeDiM.StackBuilder.Basics.UCtrlDualDouble.ValueChangedDelegate(this.OnInputChanged);
            // 
            // lbPallet
            // 
            resources.ApplyResources(this.lbPallet, "lbPallet");
            this.lbPallet.Name = "lbPallet";
            // 
            // uCtrlLayerList
            // 
            resources.ApplyResources(this.uCtrlLayerList, "uCtrlLayerList");
            this.uCtrlLayerList.Name = "uCtrlLayerList";
            this.uCtrlLayerList.SingleSelection = false;
            // 
            // checkBoxBestLayersOnly
            // 
            resources.ApplyResources(this.checkBoxBestLayersOnly, "checkBoxBestLayersOnly");
            this.checkBoxBestLayersOnly.Name = "checkBoxBestLayersOnly";
            this.checkBoxBestLayersOnly.UseVisualStyleBackColor = true;
            this.checkBoxBestLayersOnly.CheckedChanged += new System.EventHandler(this.OnInputChanged);
            // 
            // bnBestCombination
            // 
            resources.ApplyResources(this.bnBestCombination, "bnBestCombination");
            this.bnBestCombination.Name = "bnBestCombination";
            this.bnBestCombination.UseVisualStyleBackColor = true;
            this.bnBestCombination.Click += new System.EventHandler(this.OnBestCombinationClicked);
            // 
            // uCtrlCaseOrientation
            // 
            this.uCtrlCaseOrientation.AllowedOrientations = new bool[] {
        false,
        false,
        true};
            resources.ApplyResources(this.uCtrlCaseOrientation, "uCtrlCaseOrientation");
            this.uCtrlCaseOrientation.Name = "uCtrlCaseOrientation";
            this.uCtrlCaseOrientation.CheckedChanged += new treeDiM.StackBuilder.Graphics.uCtrlCaseOrientation.CheckChanged(this.OnInputChanged);
            // 
            // tabCtrlConstraints
            // 
            this.tabCtrlConstraints.Controls.Add(this.tabPageStopCriterions);
            this.tabCtrlConstraints.Controls.Add(this.tabPageOverhang);
            this.tabCtrlConstraints.Controls.Add(this.tabPageSpaces);
            resources.ApplyResources(this.tabCtrlConstraints, "tabCtrlConstraints");
            this.tabCtrlConstraints.Name = "tabCtrlConstraints";
            this.tabCtrlConstraints.SelectedIndex = 0;
            // 
            // tabPageStopCriterions
            // 
            this.tabPageStopCriterions.Controls.Add(this.uCtrlOptMaxNumber);
            this.tabPageStopCriterions.Controls.Add(this.uCtrlOptMaximumWeight);
            this.tabPageStopCriterions.Controls.Add(this.uCtrlMaximumHeight);
            resources.ApplyResources(this.tabPageStopCriterions, "tabPageStopCriterions");
            this.tabPageStopCriterions.Name = "tabPageStopCriterions";
            this.tabPageStopCriterions.UseVisualStyleBackColor = true;
            // 
            // uCtrlOptMaxNumber
            // 
            resources.ApplyResources(this.uCtrlOptMaxNumber, "uCtrlOptMaxNumber");
            this.uCtrlOptMaxNumber.Minimum = 0;
            this.uCtrlOptMaxNumber.Name = "uCtrlOptMaxNumber";
            this.uCtrlOptMaxNumber.Value = ((treeDiM.StackBuilder.Basics.OptInt)(resources.GetObject("uCtrlOptMaxNumber.Value")));
            this.uCtrlOptMaxNumber.ValueChanged += new treeDiM.StackBuilder.Basics.UCtrlOptInt.ValueChangedDelegate(this.OnInputChanged);
            // 
            // uCtrlOptMaximumWeight
            // 
            resources.ApplyResources(this.uCtrlOptMaximumWeight, "uCtrlOptMaximumWeight");
            this.uCtrlOptMaximumWeight.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.uCtrlOptMaximumWeight.Name = "uCtrlOptMaximumWeight";
            this.uCtrlOptMaximumWeight.Unit = treeDiM.StackBuilder.Basics.UnitsManager.UnitType.UT_MASS;
            this.uCtrlOptMaximumWeight.Value = ((treeDiM.StackBuilder.Basics.OptDouble)(resources.GetObject("uCtrlOptMaximumWeight.Value")));
            this.uCtrlOptMaximumWeight.ValueChanged += new treeDiM.StackBuilder.Basics.UCtrlOptDouble.ValueChangedDelegate(this.OnInputChanged);
            // 
            // uCtrlMaximumHeight
            // 
            resources.ApplyResources(this.uCtrlMaximumHeight, "uCtrlMaximumHeight");
            this.uCtrlMaximumHeight.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.uCtrlMaximumHeight.Name = "uCtrlMaximumHeight";
            this.uCtrlMaximumHeight.Unit = treeDiM.StackBuilder.Basics.UnitsManager.UnitType.UT_LENGTH;
            this.uCtrlMaximumHeight.Value = 0D;
            this.uCtrlMaximumHeight.ValueChanged += new treeDiM.StackBuilder.Basics.UCtrlDouble.ValueChangedDelegate(this.OnInputChanged);
            // 
            // tabPageOverhang
            // 
            this.tabPageOverhang.Controls.Add(this.uCtrlOverhang);
            resources.ApplyResources(this.tabPageOverhang, "tabPageOverhang");
            this.tabPageOverhang.Name = "tabPageOverhang";
            this.tabPageOverhang.UseVisualStyleBackColor = true;
            // 
            // tabPageSpaces
            // 
            this.tabPageSpaces.Controls.Add(this.uCtrlOptSpace);
            resources.ApplyResources(this.tabPageSpaces, "tabPageSpaces");
            this.tabPageSpaces.Name = "tabPageSpaces";
            this.tabPageSpaces.UseVisualStyleBackColor = true;
            // 
            // uCtrlOptSpace
            // 
            resources.ApplyResources(this.uCtrlOptSpace, "uCtrlOptSpace");
            this.uCtrlOptSpace.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.uCtrlOptSpace.Name = "uCtrlOptSpace";
            this.uCtrlOptSpace.Unit = treeDiM.StackBuilder.Basics.UnitsManager.UnitType.UT_LENGTH;
            this.uCtrlOptSpace.Value = ((treeDiM.StackBuilder.Basics.OptDouble)(resources.GetObject("uCtrlOptSpace.Value")));
            this.uCtrlOptSpace.ValueChanged += new treeDiM.StackBuilder.Basics.UCtrlOptDouble.ValueChangedDelegate(this.OnInputChanged);
            // 
            // lbSelect
            // 
            resources.ApplyResources(this.lbSelect, "lbSelect");
            this.lbSelect.Name = "lbSelect";
            // 
            // FormNewAnalysisCasePallet
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lbSelect);
            this.Controls.Add(this.tabCtrlConstraints);
            this.Controls.Add(this.bnBestCombination);
            this.Controls.Add(this.checkBoxBestLayersOnly);
            this.Controls.Add(this.uCtrlLayerList);
            this.Controls.Add(this.uCtrlCaseOrientation);
            this.Controls.Add(this.cbPallets);
            this.Controls.Add(this.cbCases);
            this.Controls.Add(this.lbPallet);
            this.Controls.Add(this.lbBox);
            this.Name = "FormNewAnalysisCasePallet";
            this.TopMost = true;
            this.Controls.SetChildIndex(this.lbName, 0);
            this.Controls.SetChildIndex(this.lbDescription, 0);
            this.Controls.SetChildIndex(this.tbName, 0);
            this.Controls.SetChildIndex(this.tbDescription, 0);
            this.Controls.SetChildIndex(this.lbBox, 0);
            this.Controls.SetChildIndex(this.lbPallet, 0);
            this.Controls.SetChildIndex(this.cbCases, 0);
            this.Controls.SetChildIndex(this.cbPallets, 0);
            this.Controls.SetChildIndex(this.uCtrlCaseOrientation, 0);
            this.Controls.SetChildIndex(this.uCtrlLayerList, 0);
            this.Controls.SetChildIndex(this.checkBoxBestLayersOnly, 0);
            this.Controls.SetChildIndex(this.bnBestCombination, 0);
            this.Controls.SetChildIndex(this.tabCtrlConstraints, 0);
            this.Controls.SetChildIndex(this.lbSelect, 0);
            this.tabCtrlConstraints.ResumeLayout(false);
            this.tabPageStopCriterions.ResumeLayout(false);
            this.tabPageOverhang.ResumeLayout(false);
            this.tabPageSpaces.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private treeDiM.StackBuilder.Graphics.Controls.CCtrlComboFiltered cbPallets;
        private treeDiM.StackBuilder.Graphics.Controls.CCtrlComboFiltered cbCases;
        private Graphics.uCtrlCaseOrientation uCtrlCaseOrientation;
        private Graphics.UCtrlLayerList uCtrlLayerList;
        private Basics.UCtrlOptDouble uCtrlOptSpace;
        private Basics.UCtrlDualDouble uCtrlOverhang;
        private Basics.UCtrlOptDouble uCtrlOptMaximumWeight;
        private Basics.UCtrlDouble uCtrlMaximumHeight;
        private Basics.UCtrlOptInt uCtrlOptMaxNumber;
        private System.Windows.Forms.Label lbBox;
        private System.Windows.Forms.Label lbPallet;
        private System.Windows.Forms.CheckBox checkBoxBestLayersOnly;
        private System.Windows.Forms.Button bnBestCombination;
        private System.Windows.Forms.TabControl tabCtrlConstraints;
        private System.Windows.Forms.TabPage tabPageOverhang;
        private System.Windows.Forms.TabPage tabPageStopCriterions;
        private System.Windows.Forms.TabPage tabPageSpaces;
        private System.Windows.Forms.Label lbSelect;
    }
}