﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Linq;

using Sharp3D.Math.Core;
using Sharp3DBinPacking;
using Sharp3D.Boxologic;
using log4net;

using treeDiM.StackBuilder.Basics;
#endregion

namespace treeDiM.StackBuilder.Engine
{
    public class HSolver : IHSolver
    {
        public List<HSolution> BuildSolutions(AnalysisHetero analysis)
        {
            // dim container + offset
            Vector3D dimContainer = analysis.DimContainer(0), offset = analysis.Offset(0);
            // content items
            List<ContentItem> contentItems = new List<ContentItem>(analysis.Content);
            // solutions
            List<HSolution> solutions = new List<HSolution>();

            // *** Sharp3DBinPacking : begin
            // create cuboid list
            List<Cuboid> listCuboids = new List<Cuboid>();
            bool bAllowAllOrientations = true;
            foreach (ContentItem ci in contentItems)
            {
                for (int i = 0; i < ci.Number; ++i)
                {
                    if (!ci.AllowOrientX && !ci.AllowOrientY && !ci.AllowOrientZ)
                        continue;
                    if (ci.Pack is BoxProperties b)
                        listCuboids.Add(
                            new Cuboid((decimal)b.Length, (decimal)b.Width, (decimal)b.Height)
                            {
                                Tag = BoxToID(ci.Pack as BoxProperties)
                                , AllowOrientX = ci.AllowOrientX
                                , AllowOrientY = ci.AllowOrientY
                                , AllowOrientZ = ci.AllowOrientZ
                                , PriorityLevel = ci.PriorityLevel
                            }
                        );
                }
                if (!ci.AllowOrientX || !ci.AllowOrientY || !ci.AllowOrientZ)
                    bAllowAllOrientations = false;
            }

            // Create a bin packer instance
            // The default bin packer will test all algorithms and try to find the best result
            // BinPackerVerifyOption is used to avoid bugs, it will check whether the result is correct
            var binPacker = BinPacker.GetDefault(BinPackerVerifyOption.BestOnly, bAllowAllOrientations);
            // The result contains bins which contains packed cuboids whith their coordinates
            var parameter = new BinPackParameter(
                (decimal)dimContainer.X, (decimal)dimContainer.Y, (decimal)dimContainer.Z,
                listCuboids.ToArray())
            { ShuffleCount = 0 };

            var binPackResult = binPacker.Pack(parameter);
            {
                HSolution sol = new HSolution("Sharp3DBinPacking") { Analysis = analysis };
                foreach (var bins in binPackResult.BestResult)
                {
                    HSolItem hSolItem = sol.CreateSolItem();
                    foreach (var cuboid in bins)
                    {
                        CuboidToSolItem(contentItems, offset, cuboid, out int index, out BoxPosition pos);
                        hSolItem.InsertContainedElt(index, pos);
                    }
                }
                solutions.Add(sol);
            }
            // *** Sharp3DBinPacking : end

            // *** BoxoLogic : begin
           
            bool singleSol = true;
            for (int variant = singleSol? 5 : 1; variant < 6; ++variant)
            {
                HSolution sol = new HSolution($"Boxologic - Variant {variant}") { Analysis = analysis };
                RunBoxologic(variant, sol, dimContainer, offset, contentItems);
                solutions.Add(sol);
            } // for
            // *** BoxoLogic : end
            return solutions;
        }

        private void RunBoxologic(int variant, HSolution hSol, Vector3D dimContainer, Vector3D offset, List<ContentItem> contentItems)
        {
            // ContentItem -> BoxItem
            List<BoxItem> boxItems = new List<BoxItem>();
            foreach (ContentItem ci in contentItems)
            {
                if (ci.Pack is BoxProperties b)
                    boxItems.Add(
                        new BoxItem()
                        {
                            ID = BoxToID(b),
                            Boxx = (decimal)b.Length,
                            Boxy = (decimal)b.Width,
                            Boxz = (decimal)b.Height,
                            AllowX = ci.AllowOrientX,
                            AllowY = ci.AllowOrientY,
                            AllowZ = ci.AllowOrientZ,
                            N = (int)ci.Number,
                            Order = ci.PriorityLevel
                        }
                );
            }

            // solve
            var bl = new Boxlogic() { OutputFilePath = string.Empty };
            var solArray = new SolutionArray();
            bl.Run(variant, boxItems.ToArray(), (decimal)dimContainer.X, (decimal)dimContainer.Y, (decimal)dimContainer.Z, ref solArray);
            var solution = solArray.Solutions[0];

            HSolItem hSolItem = hSol.CreateSolItem();
            Transform3D transform;
            switch (solution.Variant)
            {
                case 1: transform = Transform3D.Translation(new Vector3D(0.0, dimContainer.Y, 0.0)) * Transform3D.RotationX(90.0); break;
                case 2: transform = Transform3D.Translation(new Vector3D(dimContainer.X, 0.0, 0.0)) * Transform3D.RotationZ(90.0); break;
                case 3: transform = Transform3D.Translation(new Vector3D(dimContainer.X, 0.0, 0.0)) * Transform3D.RotationZ(90.0); break;
                case 4: transform = Transform3D.Translation(new Vector3D(dimContainer.X, 0.0, 0.0)) * Transform3D.RotationY(-90.0); break;
                case 5: transform = Transform3D.Translation(new Vector3D(0.0, dimContainer.Y, 0.0)) * Transform3D.RotationX(90.0); break;
                default: transform = Transform3D.Identity; break;
            }

            foreach (var item in solution.ItemsPacked)
            {
                BoxInfoToSolItem(contentItems, offset, item, transform, out int index, out BoxPosition pos);
                hSolItem.InsertContainedElt(index, pos.Adjusted(new Vector3D((double)item.DimX, (double)item.DimY, (double)item.DimZ)));
            }
            
            // remaining
            List<ContentItem> remainingBoxItems = new List<ContentItem>();
            foreach (var solItem in solution.ItemsUnpacked)
            {
                var ci = contentItems[(int)solItem.Id];
                ContentItem contentItem = new ContentItem(ci.Pack, 1)
                {
                    AllowOrientX = ci.AllowOrientX,
                    AllowOrientY = ci.AllowOrientY,
                    AllowOrientZ = ci.AllowOrientZ
                };
                remainingBoxItems.Add(contentItem);
            }
            if (remainingBoxItems.Count > 0)
                RunBoxologic(variant, hSol, dimContainer, offset, remainingBoxItems);            
        }

        private uint BoxToID(BoxProperties b)
        {
            if (!_dictionnary.ContainsKey(b))
                _dictionnary.Add(b, (uint)_dictionnary.Count);
            return _dictionnary[b];
        }
        private BoxProperties IDToBox(uint id)
        {
            if (!_dictionnary.ContainsValue(id))
                return null;
            return _dictionnary.FirstOrDefault(x => x.Value == id).Key;
        }

        private bool BoxInfoToSolItem(List<ContentItem> contentItems, Vector3D offset, SolItem solItem, Transform3D transform, out int index, out BoxPosition pos)
        {
            index = 0;
            pos = BoxPosition.Zero;

            BoxProperties b = IDToBox(solItem.Id);
            if (null != b)
            {
                try
                {
                    index = (int)solItem.Id;
                    pos = BoxPosition.FromPositionDimension(
                        new Vector3D((double)solItem.X, (double)solItem.Y, (double)solItem.Z),
                        new Vector3D((double)solItem.BX, (double)solItem.BY, (double)solItem.BZ),
                        new Vector3D((double)solItem.DimX, (double)solItem.DimY, (double)solItem.DimZ)
                        );
                    pos =  pos.Transform(Transform3D.Translation(offset) * transform);

                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            }
            return false;
        }

        private bool CuboidToSolItem(List<ContentItem> contentItems, Vector3D offset, Cuboid cuboid, out int index, out BoxPosition pos)
        {
            index = 0;
            pos = BoxPosition.Zero;
            try
            {
                index = int.Parse(cuboid.Tag.ToString());
                BoxProperties bProperties = IDToBox((uint)index);

                pos = BoxPosition.FromPositionDimension(
                        new Vector3D((double)cuboid.X, (double)cuboid.Y, (double)cuboid.Z) + offset,
                        new Vector3D((double)cuboid.Width, (double)cuboid.Height, (double)cuboid.Depth),
                        new Vector3D(bProperties.Length, bProperties.Width, bProperties.Height)
                        );
                return true;
            }
            catch (Exception ex)
                {
                    _log.Error(ex.Message);
                }
            return false;
        }

        private Dictionary<BoxProperties, uint> _dictionnary = new Dictionary<BoxProperties, uint>();
        private static ILog _log = LogManager.GetLogger(typeof(HSolver));
    }
}
