﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Drawing;

using log4net;

using treeDiM.Basics;
using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Graphics;
using Sharp3D.Math.Core;

using treeDiM.StackBuilder.Desktop.Properties;
#endregion

namespace treeDiM.StackBuilder.Desktop
{
    public partial class FormNewPack : FormNewBase, IDrawingContainer
    {
        #region Data members
        private PackProperties _packProperties;
        public StrapperSet StrapperSet { get; } = new StrapperSet();
        static readonly ILog _log = LogManager.GetLogger(typeof(FormNewPack));
        #endregion

        #region Constructor
        public FormNewPack(Document doc, PackProperties packProperties)
            : base(doc, packProperties)
        {
            InitializeComponent();
            _packProperties = packProperties;
            StrapperSet = null != _packProperties ? _packProperties.StrapperSet.Clone() : new StrapperSet();
        }
        #endregion

        #region FormNewBase overrides
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Graphics3DControl
            graphCtrl.DrawingContainer = this;
            // list of packs
            ComboBoxHelpers.FillCombo(Boxes.ToArray(), cbInnerBox, null != _packProperties ? _packProperties.Box : Boxes[0]);
            // arrangement
            if (null != _packProperties)
            {
                cbDir.SelectedIndex = (int)(_packProperties.BoxOrientation);
                Arrangement = _packProperties.Arrangement;
                Wrapper = _packProperties.Wrap;
                uCtrlOuterDimensions.Checked = _packProperties.HasForcedOuterDimensions;
                OuterDimensions = _packProperties.OuterDimensions;
            }
            else
            { 
                cbDir.SelectedIndex = 5; // HalfAxis.HAxis.AXIS_Z_P
                Arrangement = new PackArrangement(3, 2, 1);
                Wrapper = new WrapperPolyethilene(0.1, 0.010, Color.LightGray, true);

                uCtrlThickness.Value = UnitsManager.ConvertLengthFrom(0.1, UnitsManager.UnitSystem.UNIT_METRIC1);
                uCtrlHeight.Value = UnitsManager.ConvertLengthFrom(40, UnitsManager.UnitSystem.UNIT_METRIC1);
            }
            // set StrapperSet
            ctrlStrapperSet.StrapperSet = StrapperSet;

            // disable Ok button
            UpdateStatus(string.Empty);
        }

        public override string ItemDefaultName
        { get { return Resources.ID_PACK; } }

        public override void UpdateStatus(string message)
        {
            if (!DesignMode)
            {
                double length = 0.0, width = 0.0, height = 0.0;
                PackProperties.GetDimensions(
                    SelectedBox,
                    BoxOrientation,
                    Arrangement,
                    ref length, ref width, ref height);
                if (uCtrlOuterDimensions.Checked && (uCtrlOuterDimensions.X < length || uCtrlOuterDimensions.Y < width || uCtrlOuterDimensions.Z < height))
                    message = Resources.ID_INVALIDOUTERDIMENSION;
            }
            base.UpdateStatus(message);
        }
        #endregion

        #region Public properties
        public List<BoxProperties> Boxes { set; get; } = new List<BoxProperties>();
        public BoxProperties SelectedBox
        {
            get
            {
                if (Boxes.Count > 0 && -1 != cbInnerBox.SelectedIndex)
                    return Boxes[cbInnerBox.SelectedIndex];
                else
                    return null;
            }
        }
        public HalfAxis.HAxis BoxOrientation
        {
            get
            {
                HalfAxis.HAxis[] axes
                    = {
                           HalfAxis.HAxis.AXIS_X_N,
                           HalfAxis.HAxis.AXIS_X_P,
                           HalfAxis.HAxis.AXIS_Y_N,
                           HalfAxis.HAxis.AXIS_Y_P,
                           HalfAxis.HAxis.AXIS_Z_N,
                           HalfAxis.HAxis.AXIS_Z_P
                      };
                return axes[cbDir.SelectedIndex != -1 ? cbDir.SelectedIndex : 5];
            }
        }
        public PackArrangement Arrangement
        {
            get
            {
                return new PackArrangement(uCtrlLayout.NoX, uCtrlLayout.NoY, uCtrlLayout.NoZ);
            }
            set
            {
                uCtrlLayout.NoX = value.Length;
                uCtrlLayout.NoY = value.Width;
                uCtrlLayout.NoZ = value.Height;
            }
        }
        public PackWrapper Wrapper
        {
            get
            {
                PackWrapper wrapper = null;
                switch (cbType.SelectedIndex)
                {
                    case 0:
                        wrapper = new WrapperPolyethilene(
                            uCtrlThickness.Value, uCtrlWeight.Value, cbColor.Color, chkbTransparent.Checked);
                        break;
                    case 1:
                        wrapper = new WrapperPaper(
                            uCtrlThickness.Value, uCtrlWeight.Value, cbColor.Color
                            );
                        break;
                    case 2:
                        {
                            WrapperCardboard wrapperCardboard = new WrapperCardboard(
                                uCtrlThickness.Value, uCtrlWeight.Value, cbColor.Color
                                );
                            wrapperCardboard.SetNoWalls(uCtrlWalls.NoX, uCtrlWalls.NoY, uCtrlWalls.NoZ);
                            wrapper = wrapperCardboard;
                        }
                        break;
                    case 3:
                        {
                            WrapperTray wrapperTray = new WrapperTray(
                                uCtrlThickness.Value, uCtrlHeight.Value, cbColor.Color
                                );
                            wrapperTray.SetNoWalls(uCtrlWalls.NoX, uCtrlWalls.NoY, uCtrlWalls.NoZ);
                            wrapperTray.Height = uCtrlHeight.Value;
                            wrapperTray.Weight = uCtrlWeight.Value;
                            wrapper = wrapperTray;
                        }
                        break;
                    default:
                        break;
                }
                return wrapper;  
            }
            set
            {
                PackWrapper wrapper = value;
                if (null == wrapper) return;
                cbType.SelectedIndex = (int)wrapper.Type;
                OnWrapperTypeChanged(this, null);

                uCtrlThickness.Value = wrapper.UnitThickness;
                uCtrlWeight.Value = wrapper.Weight;
                cbColor.Color = wrapper.Color;

                switch (wrapper.Type)
                {
                    case PackWrapper.WType.WT_POLYETHILENE:
                        chkbTransparent.Checked = wrapper.Transparent;
                        break;
                    case PackWrapper.WType.WT_PAPER:
                        break;
                    case PackWrapper.WType.WT_CARDBOARD:
                        {
                            WrapperCardboard wrapperCardboard = wrapper as WrapperCardboard;
                            uCtrlWalls.NoX = wrapperCardboard.Wall(0);
                            uCtrlWalls.NoY = wrapperCardboard.Wall(1);
                            uCtrlWalls.NoZ = wrapperCardboard.Wall(2);
                        }
                        break;
                    case PackWrapper.WType.WT_TRAY:
                        {
                            WrapperTray wrapperTray = wrapper as WrapperTray;
                            uCtrlWalls.NoX = wrapperTray.Wall(0);
                            uCtrlWalls.NoY = wrapperTray.Wall(1);
                            uCtrlWalls.NoZ = wrapperTray.Wall(2);
                            uCtrlHeight.Value = wrapperTray.Height;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        public bool HasForcedOuterDimensions
        { get { return uCtrlOuterDimensions.Checked; } }
        public Vector3D OuterDimensions
        {
            get
            {
                return new Vector3D(
                    uCtrlOuterDimensions.X,
                    uCtrlOuterDimensions.Y,
                    uCtrlOuterDimensions.Z); 
            }
            set
            {
                uCtrlOuterDimensions.X = value.X;
                uCtrlOuterDimensions.Y = value.Y;
                uCtrlOuterDimensions.Z = value.Z;
            }
        }
        #endregion

        #region Event handlers
        private void FormNewPack_Load(object sender, EventArgs e)
        {
            graphCtrl.DrawingContainer = this;
            UpdateStatus(string.Empty);
        }
        private void OnPackChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender != uCtrlOuterDimensions && !this.DesignMode)
                {
                    double length = 0.0, width = 0.0, height = 0.0;
                    PackProperties.GetDimensions(SelectedBox, BoxOrientation, Arrangement, ref length, ref width, ref height);
                    uCtrlOuterDimensions.X = length;
                    uCtrlOuterDimensions.Y = width;
                    uCtrlOuterDimensions.Z = height;
                }
                graphCtrl.Invalidate();
                UpdateStatus(string.Empty);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        private void OnWrapperTypeChanged(object sender, EventArgs e)
        {

            bool showTransparent = false, showWalls = false, showHeight = false; 

            switch (cbType.SelectedIndex)
            {
                case 0: showTransparent = true; showWalls = false; showHeight = false; break;
                case 1: showTransparent = false; showWalls = false; showHeight = false; break;
                case 2: showTransparent = false; showWalls = true; showHeight = false; break;
                case 3: showTransparent = false; showWalls = true; showHeight = true; break;
                default: showTransparent = false; showWalls = false; showHeight = false; break;
            }

            chkbTransparent.Visible = showTransparent;
            uCtrlWalls.Visible = showWalls;
            uCtrlHeight.Visible = showHeight;

            graphCtrl.Invalidate();
        }
        #endregion

        #region IDrawingContainer
        public void Draw(Graphics3DControl ctrl, Graphics3D graphics)
        {
            StrapperSet.SetDimension(OuterDimensions);
            // build pack
            PackProperties packProperties = new PackProperties(
                null, SelectedBox, Arrangement, BoxOrientation, Wrapper)
            {
                StrapperSet = StrapperSet
            };
            if (uCtrlOuterDimensions.Checked)
                packProperties.ForceOuterDimensions(
                    new Vector3D(uCtrlOuterDimensions.X, uCtrlOuterDimensions.Y, uCtrlOuterDimensions.Z) );
            Pack pack = new Pack(0, packProperties)
            {
                ForceTransparency = true
            };
            graphics.AddBox(pack);
            graphics.AddDimensions(new DimensionCube(Vector3D.Zero, pack.Length, pack.Width, pack.Height, Color.Black, true));
            if (packProperties.Wrap.Transparent)
                graphics.AddDimensions(
                    new DimensionCube(
                        packProperties.InnerOffset
                        , packProperties.InnerLength, packProperties.InnerWidth, packProperties.InnerHeight
                        , Color.Red, false));
        }
        #endregion
    }
}
