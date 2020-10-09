﻿#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
#endregion

namespace treeDiM.Basics
{
    public partial class UCtrlTriInt : UserControl
    {
        #region Delegates
        public delegate void ValueChangedDelegate(object sender, EventArgs e);
        #endregion

        #region Events
        [Browsable(true)]
        public event ValueChangedDelegate ValueChanged;
        #endregion

        #region Constructor
        public UCtrlTriInt()
        {
            InitializeComponent();
        }
        #endregion

        #region Public properties
        [Browsable(true),
        EditorBrowsable(EditorBrowsableState.Always),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible),
        Category("Appearance")]
        public override string Text
        {
            get { return lbName.Text; }
            set { lbName.Text = value; }
        }
        [Browsable(true)]
        public int NoX
        {
            get { return (int)nudX.Value; }
            set { try { nudX.Value = (decimal)value; } catch (ArgumentOutOfRangeException) { } }
        }
        [Browsable(true)]
        public int NoY
        {
            get { return (int)nudY.Value; }
            set { try { nudY.Value = (decimal)value; } catch (ArgumentOutOfRangeException) { } }
        }
        [Browsable(true)]
        public int NoZ
        {
            get { return (int)nudZ.Value; }
            set { try { nudZ.Value = (decimal)value; } catch (ArgumentOutOfRangeException) { } }
        }
        #endregion

        #region Event handlers
        private void OnValueChangedLocal(object sender, EventArgs e) => ValueChanged?.Invoke(this, e);
        private void OnSizeChanged(object sender, EventArgs e)
        {
            // set nud location
            nudX.Location = new Point(Width - 3 * UCtrlDouble.stNudLength, 0);
            nudY.Location = new Point(Width - 2 * UCtrlDouble.stNudLength, 0);
            nudZ.Location = new Point(Width - 1 * UCtrlDouble.stNudLength, 0);
        }
        private void OnEnter(object sender, EventArgs e)
        {
            if (sender is NumericUpDown nud)
            {
                nud.Select(0, nud.Text.Length);
                if (MouseButtons == MouseButtons.Left)
                    selectByMouse = true;
            }
        }
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (selectByMouse && sender is NumericUpDown nud)
            {
                nud.Select(0, nud.Text.Length);
                selectByMouse = false;
            }
        }
        #endregion

        #region Data members
        private bool selectByMouse = false;
        #endregion
    }
}
