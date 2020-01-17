﻿using System.Drawing;
using Sharp3D.Math.Core;


namespace treeDiM.StackBuilder.Basics
{
    public class InterlayerProperties : ItemBaseNamed
    {
        public InterlayerProperties(
            Document document, string name, string description
            , double length, double width, double thickness
            , double weight
            , Color color)
            : base(document, name, description)
        {
            Length = length;
            Width = width;
            Thickness = thickness;
            Weight = weight;
            Color = color;
        }

        public double Length { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public Vector3D Dimensions => new Vector3D(Length, Width, Thickness);
        public double Weight { get; set; }
        public Color Color { get; set; }

        public override string ToString()
        {
            var sBuilder = new System.Text.StringBuilder();
            sBuilder.Append(base.ToString());
            sBuilder.Append($" Length= {Length} Width = {Width} Thickness = {Thickness.ToString("0.##")} Weight = {Weight.ToString("0.##")}");
            return sBuilder.ToString();
        }
    }
}
