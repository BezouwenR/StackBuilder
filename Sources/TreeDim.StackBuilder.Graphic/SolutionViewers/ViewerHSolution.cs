﻿#region Using directives
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

using Sharp3D.Math.Core;

using treeDiM.StackBuilder.Basics;
#endregion

namespace treeDiM.StackBuilder.Graphics
{
    public class ViewerHSolution : Viewer
    {
        #region Constructor
        public ViewerHSolution(HSolution solution, int index)
        {
            Solution = solution;
            SolItemIndex = (index >= 0 && index < solution.SolItemCount) ? index : 0;
        }
        #endregion

        #region Abstract methods
        public override void Draw(Graphics3D graphics, Transform3D transform)
        {
            if (null == Solution)
                return;
            // draw pallet
            if (Solution.Analysis.Containers.First() is PalletProperties palletProperties)
            {
                Pallet p = new Pallet(palletProperties);
                p.Draw(graphics, transform);
            }
            TruckProperties truckProperties = Solution.Analysis.Containers.First() as TruckProperties;
            if (null != truckProperties)
            {
                Truck t = new Truck(truckProperties);
                t.DrawBegin(graphics);
            }

            // check validity
            if (!(SolItemIndex < Solution.SolItemCount))
                return;
            // get list of boxes
            List<Box> boxes = new List<Box>();
            HSolItem solItem = Solution.SolItem(SolItemIndex);
            uint pickId = 0;
            foreach (HSolElement solElt in solItem.ContainedElements)
            {
                if (Analysis.ContentTypeByIndex(solElt.ContentType) is BoxProperties bProperties)
                    boxes.Add(new Box(pickId++, bProperties, solElt.Position));
            }
            // draw boxes as triangles using BSPTree
            BSPTree bspTree = new BSPTree();
            foreach (Box b in boxes)
                bspTree.InsertBox(b);
            bspTree.Draw(graphics);
            // draw truck end
            if (null != truckProperties)
            {
                Truck t = new Truck(truckProperties);
                t.DrawEnd(graphics);
            }

            // ### dimensions
            if (graphics.ShowDimensions)
            {
                graphics.AddDimensions(new DimensionCube(BoundingBoxDim(DimCasePalletSol1), Color.Black, false));
                graphics.AddDimensions(new DimensionCube(BoundingBoxDim(DimCasePalletSol2), Color.Red, true));
            }
            // ###
        }
        public override void Draw(Graphics2D graphics)  {}
        #endregion

        #region Static properties
        public static int DimCasePalletSol1 { get; set; } = 0;
        public static int DimCasePalletSol2 { get; set; } = 1;
        #endregion

        #region Helpers
        private BBox3D BoundingBoxDim(int index)
        {
            PalletProperties palletProperties = null;
            switch (index)
            {
                case 0: return Solution.BBoxGlobal(SolItemIndex);
                case 1: return Solution.BBoxLoad(SolItemIndex);
                case 2: return palletProperties.BoundingBox;
                default: return Solution.BBoxGlobal(SolItemIndex);
            }
        }
        #endregion

        #region Data
        public HSolution Solution { get; set; }
        public AnalysisHetero Analysis => Solution.Analysis;
        public int SolItemIndex { get; set; } = 0;
        #endregion
    }
}
