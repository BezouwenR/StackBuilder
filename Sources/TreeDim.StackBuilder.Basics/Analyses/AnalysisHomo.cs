﻿using System.Collections.Generic;

using Sharp3D.Math.Core;

using log4net;

namespace treeDiM.StackBuilder.Basics
{

    public abstract class AnalysisHomo : Analysis
    {
        public Packable Content
        {
            get { return _packable; }
            set
            {
                if (value == _packable) return;
                _packable?.RemoveDependancy(this);
                _packable = value;
                if (null != ParentDocument)
                    _packable?.AddDependancy(this);
            }
        }
        public virtual Vector3D ContentDimensions       => _packable.OuterDimensions;
        public virtual double ContentVolume             => _packable.Volume;
        public virtual double ContentWeight             => _packable.Weight;

        public Solution Solution                        => _solution;
        public ConstraintSetAbstract ConstraintSet      { get; set; }
        public List<InterlayerProperties> Interlayers   => _interlayers;
        public virtual bool AlternateLayersPref         => true;

        public abstract ItemBase Container              { get; }
        public abstract Vector2D ContainerDimensions    { get; }
        public abstract Vector3D Offset                 { get; }
        public abstract double   ContainerWeight        { get; }
        public abstract double   ContainerLoadingVolume { get; }
        /// <summary>
        /// can analysis solution be reused in other analysis
        /// </summary>
        public abstract bool HasEquivalentPackable { get; }
        /// <summary>
        /// get equivalent packable
        /// </summary>
        public abstract PackableLoaded EquivalentPackable { get; }

        public virtual bool AllowInterlayer(InterlayerProperties interlayer)
        {
            if (null == interlayer)
                return false;
            return true;    
        }

        public void AddInterlayer(InterlayerProperties interlayer)
        {
            _interlayers.Add(interlayer);
            if (null != ParentDocument)
                interlayer?.AddDependancy(this);
        }

        public abstract BBox3D BBoxLoadWDeco(BBox3D loadBBox);
        public abstract BBox3D BBoxGlobal(BBox3D loadBBox);

        public void AddSolution(List<LayerDesc> layers)
        {
            _solution = new Solution(this, layers);
        }
        public void AddSolution(List<KeyValuePair<LayerDesc,int>> listLayers)
        {
            _solution = new Solution(this, listLayers);
        }
        public int GetInterlayerIndex(InterlayerProperties interlayer)
        {
            if (null == interlayer) return -1;
            if (!_interlayers.Contains(interlayer))
                _interlayers.Add(interlayer);
            return _interlayers.FindIndex(item => item == interlayer);
        }
        public virtual InterlayerProperties Interlayer(int index)
        {
            return _interlayers[index];
        }

        public override void OnEndUpdate(ItemBase updatedAttribute)
        {
            base.OnEndUpdate(updatedAttribute);

            KillListeners();
            // ---
            // recompute using same layer(s) when possible
            RecomputeSolution();
            // ---
            EndUpdate();
        }
        public virtual void RecomputeSolution()
        {
            // get best layers
            List<ILayer2D> bestLayers = Solution.Solver.BuildLayers(Content, ContainerDimensions, Offset.Z, ConstraintSet, true);
            List<LayerDesc> bestLayerDescs = bestLayers.ConvertAll(l => l.LayerDescriptor);

            bool allFound = true;
            foreach (LayerDesc l in Solution.LayerDescriptors)
            {
                if (null == bestLayerDescs.Find(bld => bld.Equals(l)))
                {
                    allFound = false;
                    break;
                }
            }
            if (allFound)
                Solution.RebuildLayers();
            else
            {
                // recomputes whole new solution
                _solution = new Solution(this, bestLayerDescs);
            }
        }
        public override bool HasValidSolution => null != _solution;
        #region Non-Public Members

        protected Solution _solution;
        protected List<InterlayerProperties> _interlayers;
        /// <summary>
        /// Content
        /// </summary>
        protected Packable _packable;
        static readonly ILog _log = LogManager.GetLogger(typeof(AnalysisHomo));

        protected AnalysisHomo(Document doc, Packable packable) : base(doc)
        {
            Content = packable;
            _interlayers = new List<InterlayerProperties>();
        }

        protected override void RemoveItselfFromDependancies()
        {
            base.RemoveItselfFromDependancies();
            _packable?.RemoveDependancy(this);
            foreach (InterlayerProperties interlayer in _interlayers)
                interlayer.RemoveDependancy(this);
        }
        #endregion
    }
}
