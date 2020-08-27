﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using Sharp3D.Math.Core;
#endregion

namespace treeDiM.StackBuilder.Basics
{
    public class PalletCornerProperties : ItemBaseNamed
    {
        #region Constructors
        /// <summary>
        /// Constructor 1
        /// </summary>
        /// <param name="document">Parent document</param>
        public PalletCornerProperties(Document document)
            : base(document)
        { 
        }
        /// <summary>
        /// Constructor 2
        /// </summary>
        /// <param name="document">Parent document</param>
        /// <param name="width">Width</param>
        /// <param name="color">Color</param>
        public PalletCornerProperties(Document document,
            string name, string description,
            double length, double width, double thickness,
            double weight,
            Color color)
            : base(document, name, description)
        {
            Length = length;
            Width = width;
            Thickness = thickness;
            Weight = weight;
            Color = color;
        }
        #endregion

        #region Properties
        public double Length  { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public double Weight { get; set; }
        public Color Color { get; set; }
        #endregion
    }
}
