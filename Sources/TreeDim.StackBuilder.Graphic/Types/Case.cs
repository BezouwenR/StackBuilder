﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using Sharp3D.Math.Core;
using treeDiM.StackBuilder.Basics;
#endregion

namespace treeDiM.StackBuilder.Graphics
{
    /// <summary>
    /// drawable case (used to draw a case with a Graphics3D object)
    /// </summary>
    public class Case : Drawable
    {

        #region Data members
        private Vector3D[] _points;
        private Transform3D _transf = Transform3D.Identity;
        #endregion

        #region Constructor
        public Case(BoxProperties boxProperties)
            : base(0)
        {
            Length = boxProperties.Length;
            Width = boxProperties.Width;
            Height = boxProperties.Height;
            InsideLength = boxProperties.InsideLength;
            InsideWidth = boxProperties.InsideWidth;
            InsideHeight = boxProperties.InsideHeight;
            Colors = boxProperties.Colors;
            ColorPath = Color.Black;
        }
        public Case(BoxProperties boxProperties, Transform3D transf)
            : base(0)
        {
            Length = boxProperties.Length;
            Width = boxProperties.Width;
            Height = boxProperties.Height;
            InsideLength = boxProperties.InsideLength;
            InsideWidth = boxProperties.InsideWidth;
            InsideHeight = boxProperties.InsideHeight;
            Colors = boxProperties.Colors;
            ColorPath = Color.Black;
            _transf = transf;
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Points
        /// </summary>
        public override Vector3D[] Points
        {
            get
            {
                if (null == _points)
                {
                    _points = new Vector3D[8];
                    _points[0] = _transf.transform(new Vector3D(0.0,    0.0,    0.0));
                    _points[1] = _transf.transform(new Vector3D(Length, 0.0,    0.0));
                    _points[2] = _transf.transform(new Vector3D(Length, Width,  0.0));
                    _points[3] = _transf.transform(new Vector3D(0.0,    Width,  0.0));

                    _points[4] = _transf.transform(new Vector3D(0.0,    0.0,    Height));
                    _points[5] = _transf.transform(new Vector3D(Length, 0.0,    Height));
                    _points[6] = _transf.transform(new Vector3D(Length, Width,  Height));
                    _points[7] = _transf.transform(new Vector3D(0.0,    Width,  Height));
                }
                return _points;
            }
        }

        public Vector3D[] InsidePoints
        {
            get
            {
                double zThickness = Height > InsideHeight ? 0.5 * (Height - InsideHeight) : 0.0;
                if (null == _points)
                {
                    _points = new Vector3D[8];

                    _points[0] = _transf.transform(new Vector3D(0.0,            0.0,        zThickness));
                    _points[1] = _transf.transform(new Vector3D(InsideLength,   0.0,        zThickness));
                    _points[2] = _transf.transform(new Vector3D(InsideLength,   InsideWidth, zThickness));
                    _points[3] = _transf.transform(new Vector3D(0.0,            InsideWidth, zThickness));

                    _points[4] = _transf.transform(new Vector3D(0.0,            0.0,            InsideHeight + zThickness));
                    _points[5] = _transf.transform(new Vector3D(InsideLength,   0.0,            InsideHeight + zThickness));
                    _points[6] = _transf.transform(new Vector3D(InsideLength,   InsideWidth,    InsideHeight + zThickness));
                    _points[7] = _transf.transform(new Vector3D(0.0,            InsideWidth,    InsideHeight + zThickness));
                }
                return _points;
            }
        }
        /// <summary>
        /// Faces
        /// </summary>
        public Face[] Faces
        {
            get
            {
                var points = Points;
                var faces = new Face[6];
                faces[0] = new Face(PickId, new Vector3D[] { points[3], points[2], points[1], points[0] }, Colors[0], ColorPath, "CASE");    // AXIS_Z_P
                faces[1] = new Face(PickId, new Vector3D[] { points[4], points[5], points[6], points[7] }, Colors[1], ColorPath, "CASE");    // AXIS_Z_N
                faces[2] = new Face(PickId, new Vector3D[] { points[1], points[5], points[4], points[0] }, Colors[2], ColorPath, "CASE");    // AXIS_Y_P
                faces[3] = new Face(PickId, new Vector3D[] { points[3], points[7], points[6], points[2] }, Colors[3], ColorPath, "CASE");    // AXIS_Y_N
                faces[4] = new Face(PickId, new Vector3D[] { points[2], points[6], points[5], points[1] }, Colors[4], ColorPath, "CASE");    // AXIS_X_N
                faces[5] = new Face(PickId, new Vector3D[] { points[4], points[7], points[3], points[0] }, Colors[5], ColorPath, "CASE");    // AXIS_X_P
                return faces;
            }
        }
        public Face[] InsideFaces
        {
            get
            {
                var points = InsidePoints;
                var faces = new Face[6];
                faces[0] = new Face(PickId, new Vector3D[] { points[3], points[2], points[1], points[0] }, Colors[0], ColorPath, "CASE", false);    // AXIS_Z_P
                faces[1] = new Face(PickId, new Vector3D[] { points[4], points[5], points[6], points[7] }, Colors[1], ColorPath, "CASE", false);    // AXIS_Z_N
                faces[2] = new Face(PickId, new Vector3D[] { points[1], points[5], points[4], points[0] }, Colors[2], ColorPath, "CASE", false);    // AXIS_Y_P
                faces[3] = new Face(PickId, new Vector3D[] { points[3], points[7], points[6], points[2] }, Colors[3], ColorPath, "CASE", false);    // AXIS_Y_N
                faces[4] = new Face(PickId, new Vector3D[] { points[2], points[6], points[5], points[1] }, Colors[4], ColorPath, "CASE", false);    // AXIS_X_N
                faces[5] = new Face(PickId, new Vector3D[] { points[4], points[7], points[3], points[0] }, Colors[5], ColorPath, "CASE", false);    // AXIS_X_P
                return faces;
            }
        }

        public double Length { get; }
        public double Width { get; }
        public double Height { get; }
        public double InsideLength { get; }
        public double InsideWidth { get; }
        public double InsideHeight { get; }

        public Color[] Colors { get; } = new Color[6];
        public Color ColorPath { get; } = Color.Black;
        #endregion

        #region Overrides
        public override void DrawBegin(Graphics3D graphics)
        {
            foreach (Face face in Faces)
                graphics.AddFace(face);
        }
        public override void Draw(Graphics3D graphics)
        {
        }
        public override void DrawEnd(Graphics3D graphics)
        {
        }
        #endregion

        #region Inside drawing
        public void DrawInside(Graphics3D graphics, Transform3D transform)
        {
            Face[] faces = InsideFaces;
            for (int i = 0; i < 6; ++i)
                faces[i].IsSolid = false;
            foreach (Face face in faces)
                graphics.AddFace(face.Transform(transform));
        }
        #endregion
    }
}
