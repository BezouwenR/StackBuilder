﻿using System.Collections.Generic;

using Sharp3D.Math.Core;
using treeDiM.StackBuilder.Basics;

namespace treeDiM.StackBuilder.Engine
{
    public class PackPalletSolver : IPackPalletAnalysisSolver
    {
        public PackPalletSolver()
        {
        }

        public void ProcessAnalysis(PackPalletAnalysis analysis)
        {
            _packProperties = analysis.PackProperties;
            _palletProperties = analysis.PalletProperties;
            _interlayerProperties = analysis.InterlayerProperties;
            _constraintSet = analysis.ConstraintSet;
            analysis.Solutions = GenerateSolutions();
        }

        #region Non-Public Members
        private PackProperties _packProperties;
        private PalletProperties _palletProperties;
        private InterlayerProperties _interlayerProperties;
        private PackPalletConstraintSet _constraintSet;

        private List<PackPalletSolution> GenerateSolutions()
        {
            var solutions = new List<PackPalletSolution>();

            HalfAxis.HAxis[] axes = { HalfAxis.HAxis.AXIS_Z_N, HalfAxis.HAxis.AXIS_Z_P };
            // loop throught all patterns
            foreach (LayerPatternBox pattern in LayerPatternBox.All)
            {
                // loop throught all axes
                foreach (HalfAxis.HAxis axis in axes) // axis
                {
                    // loop through
                    Layer2D layer = BuildLayer(_packProperties, _palletProperties, _constraintSet
                        , pattern.Name, axis, false, false);
                    double actualLength = 0.0, actualWidth = 0.0;
                    if (!pattern.GetLayerDimensionsChecked(layer, out actualLength, out actualWidth))
                        continue;
                    pattern.GenerateLayer(layer, actualLength, actualWidth);

                    // filter by layer weight
                    if (_constraintSet.MaximumLayerWeight.Activated
                        && (layer.Count * _packProperties.Weight > _constraintSet.MaximumLayerWeight.Value))
                        continue;
                    // filter by maximum space
                    if (_constraintSet.MaximumSpaceAllowed.Activated
                        && layer.MaximumSpace > _constraintSet.MaximumSpaceAllowed.Value)
                        continue;
                    double layerHeight = layer.BoxHeight;

                    string title = string.Format("{0}-{1}", pattern.Name, axis);
                    double zLayer = 0.0;
                    var boxLayer = new Layer3DBox(zLayer, 0);
                    foreach (LayerPosition layerPos in layer)
                    {
                        LayerPosition layerPosTemp = AdjustLayerPosition(layerPos);
                        var boxPos = new BoxPosition(
                            layerPosTemp.Position
                                - (0.5 * _constraintSet.OverhangX) * Vector3D.XAxis
                                - (0.5 * _constraintSet.OverhangY) * Vector3D.YAxis
                                + zLayer * Vector3D.ZAxis
                            , layerPosTemp.LengthAxis
                            , layerPosTemp.WidthAxis
                            );
                        boxLayer.Add(boxPos);
                    }
                    boxLayer.MaximumSpace = layer.MaximumSpace;
                    BBox3D layerBBox = boxLayer.BoundingBox(_packProperties);
                    // filter by overhangX
                    if (_constraintSet.MinOverhangX.Activated
                        && (0.5 * (layerBBox.Length - _palletProperties.Length) < _constraintSet.MinOverhangX.Value))
                        continue;
                    // filter by overhangY
                    if (_constraintSet.MinOverhangY.Activated
                        && (0.5 * (layerBBox.Width - _palletProperties.Width) < _constraintSet.MinOverhangY.Value))
                        continue;

                    double interlayerThickness = null != _interlayerProperties ? _interlayerProperties.Thickness : 0;
                    double interlayerWeight = null != _interlayerProperties ? _interlayerProperties.Weight : 0;

                    var sol = new PackPalletSolution(null, title, boxLayer);
                    int noLayer = 1,
                        noInterlayer = (null != _interlayerProperties && _constraintSet.HasFirstInterlayer) ? 1 : 0;

                    bool maxHeightReached = _constraintSet.MaximumPalletHeight.Activated
                        && (_packProperties.Height
                        + noInterlayer * interlayerThickness
                        + noLayer * layer.BoxHeight) > _constraintSet.MaximumPalletHeight.Value;
                    bool maxWeightReached = _constraintSet.MaximumPalletWeight.Activated
                        && (_palletProperties.Weight
                        + noInterlayer * interlayerWeight
                        + noLayer * boxLayer.Count * _packProperties.Weight > _constraintSet.MaximumPalletWeight.Value);

                    noLayer = 0; noInterlayer = 0;
                    int iCountInterlayer = 0, iCountSwap = 1;
                    bool bSwap = false;
                    while (!maxHeightReached && !maxWeightReached)
                    {
                        bool bInterlayer = (0 == iCountInterlayer) && ((noLayer != 0) || _constraintSet.HasFirstInterlayer);
                        // actually insert new layer
                        sol.AddLayer(bSwap, bInterlayer);
                        // increment number of layers
                        noLayer++;
                        noInterlayer += (bInterlayer ? 1 : 0);
                        // update iCountInterlayer && iCountSwap
                        ++iCountInterlayer;
                        if (iCountInterlayer >= _constraintSet.InterlayerPeriod) iCountInterlayer = 0;
                        ++iCountSwap;
                        if (iCountSwap > _constraintSet.LayerSwapPeriod) { iCountSwap = 1; bSwap = !bSwap; }
                        // update maxHeightReached & maxWeightReached
                        maxHeightReached = _constraintSet.MaximumPalletHeight.Activated
                            && (_palletProperties.Height
                            + (noInterlayer + (iCountInterlayer == 0 ? 1 : 0)) * interlayerThickness
                            + (noLayer + 1) * layer.BoxHeight) > _constraintSet.MaximumPalletHeight.Value;
                        maxWeightReached = _constraintSet.MaximumPalletWeight.Activated
                            && (_palletProperties.Weight
                            + (noInterlayer + (iCountInterlayer == 0 ? 1 : 0)) * interlayerWeight
                            + (noLayer + 1) * boxLayer.Count * _packProperties.Weight > _constraintSet.MaximumPalletWeight.Value);
                    }

                    if (sol.PackCount > 0)
                        solutions.Add(sol);
                } // axis
            } // pattern
            solutions.Sort();
            return solutions;
        }

        /// <summary>
        /// if box bottom oriented to Z+, reverse box
        /// </summary>
        private LayerPosition AdjustLayerPosition(LayerPosition layerPos)
        {
            LayerPosition layerPosTemp = layerPos;
            if (layerPosTemp.HeightAxis == HalfAxis.HAxis.AXIS_Z_N)
            {
                if (layerPosTemp.LengthAxis == HalfAxis.HAxis.AXIS_X_P)
                {
                    layerPosTemp.WidthAxis = HalfAxis.HAxis.AXIS_Y_P;
                    layerPosTemp.Position += new Vector3D(0.0, -_packProperties.Width, -_packProperties.Height);
                }
                else if (layerPos.LengthAxis == HalfAxis.HAxis.AXIS_Y_P)
                {
                    layerPosTemp.WidthAxis = HalfAxis.HAxis.AXIS_X_N;
                    layerPosTemp.Position += new Vector3D(_packProperties.Width, 0.0, -_packProperties.Height);
                }
                else if (layerPos.LengthAxis == HalfAxis.HAxis.AXIS_X_N)
                {
                    layerPosTemp.LengthAxis = HalfAxis.HAxis.AXIS_X_P;
                    layerPosTemp.Position += new Vector3D(-_packProperties.Length, 0.0, -_packProperties.Height);
                }
                else if (layerPos.LengthAxis == HalfAxis.HAxis.AXIS_Y_N)
                {
                    layerPosTemp.WidthAxis = HalfAxis.HAxis.AXIS_X_P;
                    layerPosTemp.Position += new Vector3D(-_packProperties.Width, 0.0, -_packProperties.Height);
                }
            }
            return layerPosTemp;
        }

        private Layer2D BuildLayer(PackProperties packProperties, PalletProperties palletProperties, PackPalletConstraintSet constraintSet
            , string patternName, HalfAxis.HAxis axisOrtho, bool swapped, bool inversed)
        {
            double forcedSpace = constraintSet.MinimumSpace.Value;
            return new Layer2D(
                    new Vector3D(packProperties.Length + forcedSpace, packProperties.Width + forcedSpace, packProperties.Height)
                    , new Vector2D(
                        palletProperties.Length + constraintSet.OverhangX + forcedSpace
                        , _palletProperties.Width + constraintSet.OverhangY + forcedSpace)
                        , patternName, axisOrtho, swapped);
        }
        #endregion

    }
}
