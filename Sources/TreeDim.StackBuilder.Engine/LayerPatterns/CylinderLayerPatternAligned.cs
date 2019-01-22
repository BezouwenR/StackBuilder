﻿using System;

using Sharp3D.Math.Core;

using treeDiM.StackBuilder.Basics;

namespace treeDiM.StackBuilder.Engine
{
    class CylinderLayerPatternAligned : LayerPatternCyl
    {
        public override string Name => "Aligned";
        public override bool CanBeSwapped => false;

        public override bool GetLayerDimensions(ILayer2D layer, out double actualLength, out double actualWidth)
        {
            double palletLength = GetPalletLength(layer);
            double palletWidth = GetPalletWidth(layer);
            double diameter = 2.0 * GetRadius(layer);

            actualLength = Math.Floor(palletLength / diameter) * diameter; ;
            actualWidth = Math.Floor(palletWidth / diameter) * diameter;

            return (palletLength > diameter) && (palletWidth > diameter);
        }
        public override void GenerateLayer(ILayer2D layer, double actualLength, double actualWidth)
        {
            layer.Clear();
            double palletLength = GetPalletLength(layer);
            double palletWidth = GetPalletWidth(layer);
            double radius = GetRadius(layer);
            double diameter = 2.0 * radius;

            int sizeX = (int)Math.Floor(palletLength / diameter);
            int sizeY = (int)Math.Floor(palletWidth / diameter);

            double offsetX = 0.5 * (palletLength - actualLength);
            double offsetY = 0.5 * (palletWidth - actualWidth);

            double spaceX = sizeX > 1 ? (actualLength - sizeX * diameter) / (sizeX - 1) : 0.0;
            double spaceY = sizeY > 1 ? (actualWidth - sizeY * diameter) / (sizeY - 1) : 0.0;

            for (int j=0; j<sizeY; ++j)
                for (int i=0; i<sizeX; ++i)
                    AddPosition(layer
                        , new Vector2D(
                            radius + offsetX + i * (diameter + spaceX)
                            , radius + offsetY + j * (diameter + spaceY)
                        ));
                     
        }
    }
}
