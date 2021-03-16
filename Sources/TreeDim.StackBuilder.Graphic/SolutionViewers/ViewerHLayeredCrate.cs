﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Sharp3D.Math.Core;

using treeDiM.StackBuilder.Basics;
#endregion

namespace treeDiM.StackBuilder.Graphics
{
    internal class ViewerHLayeredCrate : IDisposable
    {
        #region Constructor
        public ViewerHLayeredCrate(Vector3D dimOuter, Color crateColor)
        {
            Dimensions = dimOuter;
            CrateColor = crateColor;
        }
        #endregion
        #region Drawing
        public void Draw(Graphics3D graphics, IEnumerable<Box> boxes, bool selected, bool showOuterDimensions)
        {
            graphics.BackgroundColor = selected ? Color.LightBlue : Color.White;
            graphics.CameraPosition = Graphics3D.Corner_0;
            // draw case (inside)
            Case crate = new Case(0, Dimensions, Dimensions, CrateColor);
            crate.DrawInside(graphics, Transform3D.Identity);

            var bbox = BBox3D.Initial;
            foreach (var b in boxes)
            {
                graphics.AddBox(b);
                bbox.Extend(b.BBox);
            }
            crate.DrawEnd(graphics);
            if (showOuterDimensions)
                graphics.AddDimensions(new DimensionCube(Dimensions));

            graphics.Flush();
        }
        #endregion
        #region IDisposable
        public void Dispose() {}
        #endregion
        #region Data members
        private Vector3D Dimensions { get; set; }
        private Color CrateColor { get; set; }
        #endregion
    }

    internal class ViewerNonLayeredCrate : IDisposable
    {
        #region Constructor
        public ViewerNonLayeredCrate(Vector3D dimOuter, Color crateColor)
        {
            Dimensions = dimOuter;
            CrateColor = crateColor;
        }
        #endregion
        #region Drawing
        public void Draw(Graphics3D graphics, IEnumerable<BoxExplicitDir> boxes, bool selected, bool showOuterDimensions)
        {
            graphics.BackgroundColor = selected ? Color.LightBlue : Color.White;
            graphics.CameraPosition = Graphics3D.Corner_0;
            // draw case (inside)
            Case crate = new Case(0, Dimensions, Dimensions, CrateColor);
            crate.DrawInside(graphics, Transform3D.Identity);

            var tree = new BSPTree();
            foreach (var box in boxes)
                tree.InsertBox(box);
            tree.Draw(graphics);

            crate.DrawEnd(graphics);
            if (showOuterDimensions)
                graphics.AddDimensions(new DimensionCube(Dimensions));

            graphics.Flush();
        }
        #endregion
        #region IDisposable
        public void Dispose() { }
        #endregion
        #region Data members
        private Vector3D Dimensions { get; set; }
        private Color CrateColor { get; set; }
        #endregion
    }
}
