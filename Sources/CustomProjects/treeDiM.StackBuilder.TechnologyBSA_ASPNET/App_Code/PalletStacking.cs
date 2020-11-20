﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Sharp3D.Math.Core;
using treeDiM.Basics;
using treeDiM.StackBuilder.Basics;
using treeDiM.StackBuilder.Engine;
using treeDiM.StackBuilder.Exporters;
using treeDiM.StackBuilder.Graphics;
#endregion

namespace treeDiM.StackBuilder.TechnologyBSA_ASPNET
{
    /// <summary>
    /// Summary description for PalletStacking
    /// </summary>
    public static class PalletStacking
    {
        public static void GetLayers(Vector3D caseDim, double caseWeight, Vector3D palletDim, double palletWeight, double maxPalletHeight, bool bestLayersOnly, ref List<LayerDetails> listLayers)
        {
            // case
            var boxProperties = new BoxProperties(null, caseDim.X, caseDim.Y, caseDim.Z)
            {
                TapeColor = Color.Tan,
                TapeWidth = new OptDouble(true, 50.0)
            };
            boxProperties.SetWeight(caseWeight);
            boxProperties.SetAllColors(Enumerable.Repeat(Color.Beige, 6).ToArray());
            // pallet
            var palletProperties = new PalletProperties(null, "EUR2", palletDim.X, palletDim.Y, palletDim.Z)
            {
                Weight = palletWeight,
                Color = Color.Yellow
            };
            // ### define a constraintset object
            var constraintSet = new ConstraintSetCasePallet()
            {
                OptMaxNumber = new OptInt(false, 0),
                OptMaxWeight = new OptDouble(true, 1000.0),
                Overhang = Vector2D.Zero,
            };
            constraintSet.SetAllowedOrientations(new bool[] { false, false, true });
            constraintSet.SetMaxHeight(new OptDouble(true, maxPalletHeight));
            Vector3D vPalletDim = palletProperties.GetStackingDimensions(constraintSet);
            // ###

            // get a list of all possible layers and fill ListView control
            ILayerSolver solver = new LayerSolver();
            var layers = solver.BuildLayers(boxProperties.OuterDimensions, new Vector2D(vPalletDim.X, vPalletDim.Y), 0.0, constraintSet, bestLayersOnly);
            foreach (var layer in layers)
                listLayers.Add(
                    new LayerDetails(
                        layer.Name,
                        layer.LayerDescriptor.ToString(),
                        layer.Count,
                        layer.NoLayers(caseDim.Z),
                        caseDim.X, caseDim.Y, caseDim.Z)
                );
        }

        public static void InitializeInterlayers(
            Vector3D caseDim, Vector3D palletDim, double maxPalletHeight,
            string initializer,
            ref List<InterlayerDetails> interlayers)
        {
            bool[] arrayBool = initializer.Select(c => c == '1').ToArray();

            var noLayers = (int)Math.Floor((maxPalletHeight - palletDim.Z) / caseDim.Z);
            for (var i = 0; i<noLayers; ++i)
                interlayers.Add(new InterlayerDetails($"# {i+1}", (i<arrayBool.Length) && arrayBool[i]));
            interlayers.Add(new InterlayerDetails("Top", (noLayers<arrayBool.Length) && arrayBool[noLayers]));
        }

        public static void GetSolution(
            Vector3D caseDim, double caseWeight, Bitmap bmpTexture,
            Vector3D palletDim, double palletWeight,
            double maxPalletHeight,
            List<BoxPositionIndexed> boxPositions,
            bool mirrorLength, bool mirrorWidth,
            List<bool> interlayers,
            double angle,
            Size sz,
            ref byte[] imageBytes,
            ref int caseCount, ref int layerCount,
            ref double weightLoad, ref double weightTotal,
            ref Vector3D bbLoad, ref Vector3D bbGlob
        )
        {
            SolutionLayered.SetSolver(new LayerSolver());
            // case
            var boxProperties = new BoxProperties(null, caseDim.X, caseDim.Y, caseDim.Z)
            {
                TapeColor = Color.Tan,
                TapeWidth = new OptDouble(true, 50.0)
            };
            if (null != bmpTexture)
            {
                double ratio = (double)bmpTexture.Height / bmpTexture.Width;

                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_X_N, TexturePosition(caseDim.Y, caseDim.Z, ratio), TextureSize(caseDim.Y, caseDim.Z, ratio), 0.0, bmpTexture);
                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_X_P, TexturePosition(caseDim.Y, caseDim.Z, ratio), TextureSize(caseDim.Y, caseDim.Z, ratio), 0.0, bmpTexture);
                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_Y_N, TexturePosition(caseDim.X, caseDim.Z, ratio), TextureSize(caseDim.X, caseDim.Z, ratio), 0.0, bmpTexture);
                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_Y_P, TexturePosition(caseDim.X, caseDim.Z, ratio), TextureSize(caseDim.X, caseDim.Z, ratio), 0.0, bmpTexture);

            }
            boxProperties.SetWeight(caseWeight);
            boxProperties.SetAllColors(Enumerable.Repeat(Color.Beige, 6).ToArray());
            // pallet
            var palletProperties = new PalletProperties(null, "EUR2", palletDim.X, palletDim.Y, palletDim.Z)
            {
                Weight = palletWeight,
                Color = Color.Yellow
            };
            // constraint set
            var constraintSet = new ConstraintSetCasePallet();
            constraintSet.SetAllowedOrientations(new bool[] { false, false, true });
            constraintSet.SetMaxHeight(new OptDouble(true, maxPalletHeight));
            // layer 2D
            var layer2D = new Layer2DBrickExpIndexed(caseDim, new Vector2D(), "", HalfAxis.HAxis.AXIS_Z_P);
            layer2D.SetPositions(boxPositions);
            // analysis
            var analysis = new AnalysisCasePallet(boxProperties, palletProperties, constraintSet);
            analysis.AddInterlayer(new InterlayerProperties(null, "interlayer", "", palletDim.X, palletDim.Y, 1.0, 0.0, Color.LightYellow));
            analysis.AddSolution(layer2D, mirrorLength, mirrorWidth);
            // solution
            SolutionLayered sol = analysis.SolutionLay;
            var solutionItems = sol.SolutionItems;
            int iCount = solutionItems.Count;
            for (int i = 0; i < iCount; ++i)
                solutionItems[i].InterlayerIndex = ((i < interlayers.Count) && interlayers[i]) ? 0 : -1;
            if (iCount < interlayers.Count && interlayers[iCount])
                analysis.PalletCapProperties = new PalletCapProperties(null, "palletcap", "", palletDim.X, palletDim.Y, 1, palletDim.X, palletDim.Y, 0.0, 0.0, Color.LightYellow);
            layerCount = analysis.SolutionLay.LayerCount;
            caseCount = analysis.Solution.ItemCount;
            weightLoad = analysis.Solution.LoadWeight;
            weightTotal = analysis.Solution.Weight;
            bbGlob = analysis.Solution.BBoxGlobal.DimensionsVec;
            bbLoad = analysis.Solution.BBoxLoad.DimensionsVec;

            // generate image path
            Graphics3DImage graphics = new Graphics3DImage(sz)
            {
                BackgroundColor = Color.Transparent,
                FontSizeRatio = ConfigSettings.FontSizeRatio,
                ShowDimensions = true
            };
            graphics.SetCameraPosition(10000.0, angle, 45.0);

            using (ViewerSolution sv = new ViewerSolution(analysis.SolutionLay))
                sv.Draw(graphics, Transform3D.Identity);
            graphics.Flush();
            Bitmap bmp = graphics.Bitmap;
            ImageConverter converter = new ImageConverter();
            imageBytes = (byte[])converter.ConvertTo(bmp, typeof(byte[]));
        }

        public static void GenerateExport(Vector3D caseDim, double caseWeight, Bitmap bmpTexture,
            Vector3D palletDim, double palletWeight,
            double maxPalletHeight,
            List<BoxPositionIndexed> boxPositions,
            bool mirrorLength, bool mirrorWidth,
            List<bool> interlayers,
            string filePath,
            ref int caseCount, ref int layerCount,
            ref double weightLoad, ref double weightTotal,
            ref Vector3D bbLoad, ref Vector3D bbGlob
        )
        {
            SolutionLayered.SetSolver(new LayerSolver());
            // case
            var boxProperties = new BoxProperties(null, caseDim.X, caseDim.Y, caseDim.Z)
            {
                TapeColor = Color.Tan,
                TapeWidth = new OptDouble(true, 50.0)
            };
            if (null != bmpTexture)
            {
                double ratio = (double)bmpTexture.Height / bmpTexture.Width;

                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_X_N, TexturePosition(caseDim.Y, caseDim.Z, ratio), TextureSize(caseDim.Y, caseDim.Z, ratio), 0.0, bmpTexture);
                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_X_P, TexturePosition(caseDim.Y, caseDim.Z, ratio), TextureSize(caseDim.Y, caseDim.Z, ratio), 0.0, bmpTexture);
                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_Y_N, TexturePosition(caseDim.X, caseDim.Z, ratio), TextureSize(caseDim.X, caseDim.Z, ratio), 0.0, bmpTexture);
                boxProperties.AddTexture(HalfAxis.HAxis.AXIS_Y_P, TexturePosition(caseDim.X, caseDim.Z, ratio), TextureSize(caseDim.X, caseDim.Z, ratio), 0.0, bmpTexture);

            }
            boxProperties.SetWeight(caseWeight);
            boxProperties.SetAllColors(Enumerable.Repeat(Color.Beige, 6).ToArray());
            // pallet
            var palletProperties = new PalletProperties(null, "EUR2", palletDim.X, palletDim.Y, palletDim.Z)
            {
                Weight = palletWeight,
                Color = Color.Yellow
            };
            // constraint set
            var constraintSet = new ConstraintSetCasePallet();
            constraintSet.SetAllowedOrientations(new bool[] { false, false, true });
            constraintSet.SetMaxHeight(new OptDouble(true, maxPalletHeight));
            // layer 2D
            var layer2D = new Layer2DBrickExpIndexed(caseDim, new Vector2D(), "", HalfAxis.HAxis.AXIS_Z_P);
            layer2D.SetPositions(boxPositions);
            // analysis
            var analysis = new AnalysisCasePallet(boxProperties, palletProperties, constraintSet);
            analysis.AddInterlayer(new InterlayerProperties(null, "interlayer", "", palletDim.X, palletDim.Y, 1.0, 0.0, Color.White));
            analysis.AddSolution(layer2D, mirrorLength, mirrorWidth);
            // solution
            SolutionLayered sol = analysis.SolutionLay;
            var solutionItems = sol.SolutionItems;
            int iCount = solutionItems.Count;
            for (int i = 0; i < iCount; ++i)
                solutionItems[i].InterlayerIndex = ((i < interlayers.Count) && interlayers[i]) ? 0 : -1;
            if (iCount < interlayers.Count && interlayers[iCount])
                analysis.PalletCapProperties = new PalletCapProperties(null, "palletcap", "", palletDim.X, palletDim.Y, 1, palletDim.X, palletDim.Y, 0.0, 0.0, Color.White);
            layerCount = analysis.SolutionLay.LayerCount;
            caseCount = analysis.Solution.ItemCount;
            weightLoad = analysis.Solution.LoadWeight;
            weightTotal = analysis.Solution.Weight;
            bbGlob = analysis.Solution.BBoxGlobal.DimensionsVec;
            bbLoad = analysis.Solution.BBoxLoad.DimensionsVec;
            // export file
            var exporter = new ExporterGLB();
            exporter.Export(analysis, filePath);
        }

        public static Vector2D TexturePosition(double W, double H, double szRatio)
        {
            double xL, yL;
            if (szRatio > H / W)
            {
                yL = H * 2.0/3.0;
                xL = yL / szRatio;
            }
            else
            {
                xL = W * 2.0 / 3.0;
                yL = xL * szRatio;
            }
            return new Vector2D(0.5 * (W - xL), 0.5 * (H - yL));
        }
        public static Vector2D TextureSize(double W, double H, double szRatio)
        {
            double xL, yL;
            if (szRatio > H / W)
            {
                yL = H * 2.0 / 3.0;
                xL = yL / szRatio;
            }
            else
            {
                xL = W * 2.0 / 3.0;
                yL = xL * szRatio;
            }
            return new Vector2D(xL, yL);
        }

        public static void Export(
            Vector3D caseDim, double caseWeight,
            Vector3D palletDim, double palletWeight,
            double maxPalletHeight,
            List<BoxPositionIndexed> listBoxPositionIndexed,
            bool mirrorLength, bool mirrorWidth,
            List<bool> interlayers,
            ref byte[] fileBytes,
            System.Drawing.Imaging.ImageFormat imageFormat,
            ref byte[] imageFileBytes)
        {
            SolutionLayered.SetSolver(new LayerSolver());

            // case
            var boxProperties = new BoxProperties(null, caseDim.X, caseDim.Y, caseDim.Z)
            {
                TapeColor = Color.LightGray,
                TapeWidth = new OptDouble(true, 50.0)
            };
            boxProperties.SetWeight(caseWeight);
            boxProperties.SetAllColors(Enumerable.Repeat(Color.Beige, 6).ToArray());
            // pallet
            var palletProperties = new PalletProperties(null, "EUR2", palletDim.X, palletDim.Y, palletDim.Z)
            {
                Weight = palletWeight,
                Color = Color.Yellow
            };
            // constraint set
            var constraintSet = new ConstraintSetCasePallet();
            constraintSet.SetAllowedOrientations(new bool[] { false, false, true });
            constraintSet.SetMaxHeight(new OptDouble(true, maxPalletHeight));

            var boxPositions = listBoxPositionIndexed.ConvertAll<BoxPosition>(bi => bi.BPos);

            // layer
            var layer2D = new Layer2DBrickExp(caseDim, new Vector2D(palletDim.X, palletDim.Y), "", HalfAxis.HAxis.AXIS_Z_P);
            layer2D.SetPositions(boxPositions);
            // analysis
            var analysis = new AnalysisCasePallet(boxProperties, palletProperties, constraintSet);
            analysis.AddInterlayer(new InterlayerProperties(null, "interlayer", "", palletDim.X, palletDim.Y, 1.0, 0.0, Color.LightYellow));
            analysis.AddSolution(layer2D, mirrorLength, mirrorWidth);
            

            SolutionLayered sol = analysis.SolutionLay;
            var solutionItems = sol.SolutionItems;
            int iCount = solutionItems.Count;
            for (int i = 0; i < iCount; ++i)
            {
                solutionItems[i].InterlayerIndex = ((i < interlayers.Count) && interlayers[i]) ? 0 : -1;
            }
            if (iCount < interlayers.Count && interlayers[iCount])
                analysis.PalletCapProperties = new PalletCapProperties(
                    null, "palletCap", "", palletDim.X, palletDim.Y, 1, palletDim.X, palletDim.Y, 0.0, 0.0, Color.LightYellow);

            // export
            var exporter = ExporterFactory.GetExporterByName("csv (TechnologyBSA)");
            exporter.PositionCoordinateMode = Exporter.CoordinateMode.CM_COG;
            Stream stream = new MemoryStream();
            exporter.ExportIndexed(analysis, listBoxPositionIndexed, ref stream);
            // save stream to file
            using (var br = new BinaryReader(stream))
                fileBytes = br.ReadBytes((int)stream.Length);

            // image
            var graphics = new Graphics3DImage(ConfigSettings.ExportImageSize)
            {
                CameraPosition = Graphics3D.Corner_0,
                ShowDimensions = ConfigSettings.ExportShowDimensions
            };
            using (ViewerSolution sv = new ViewerSolution(analysis.SolutionLay))
                sv.Draw(graphics, Transform3D.Identity);
            graphics.Flush();

            imageFileBytes = ImageToByteArray(graphics.Bitmap, imageFormat);
        }
        public static byte[] ImageToByteArray(Image img, System.Drawing.Imaging.ImageFormat imageFormat)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, imageFormat);
                return stream.ToArray();
            }
        }
    }
}