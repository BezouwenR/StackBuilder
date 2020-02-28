﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Represents an affine transform in 3D space,
    /// defined by a <see cref="Quaternion"/> rotation,
    /// a <see cref="Vector3"/> scale
    /// and a <see cref="Vector3"/> translation.
    /// </summary>
    /// <see href="https://github.com/KhronosGroup/glTF-Validator/issues/33"/>
    public struct AffineTransform
    {
        #region lifecycle

        public static AffineTransform Create(Matrix4x4 matrix)
        {
            return new AffineTransform(matrix, null, null, null);
        }

        public static AffineTransform Create(Vector3? scale, Quaternion? rotation, Vector3? translation)
        {
            return new AffineTransform(null, scale, rotation, translation);
        }

        internal AffineTransform(Matrix4x4? matrix, Vector3? scale, Quaternion? rotation, Vector3? translation)
        {
            if (matrix.HasValue)
            {
                Matrix4x4.Decompose(matrix.Value, out this.Scale, out this.Rotation, out this.Translation);
            }
            else
            {
                this.Scale = scale ?? Vector3.One;
                this.Rotation = rotation ?? Quaternion.Identity;
                this.Translation = translation ?? Vector3.Zero;
            }
        }

        public static implicit operator AffineTransform(Matrix4x4 matrix)
        {
            return new AffineTransform(matrix, null, null, null);
        }

        #endregion

        #region data

        /// <summary>
        /// Rotation
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Scale
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// Translation
        /// </summary>
        public Vector3 Translation;

        #endregion

        #region properties

        public static AffineTransform Identity => new AffineTransform { Rotation = Quaternion.Identity, Scale = Vector3.One, Translation = Vector3.Zero };

        /// <summary>
        /// Gets the <see cref="Matrix4x4"/> transform of the current <see cref="AffineTransform"/>
        /// </summary>
        public Matrix4x4 Matrix
        {
            get
            {
                return
                    Matrix4x4.CreateScale(Scale)
                    *
                    Matrix4x4.CreateFromQuaternion(Rotation.Sanitized())
                    *
                    Matrix4x4.CreateTranslation(Translation);
            }
        }

        public bool IsValid
        {
            get
            {
                if (!Scale._IsFinite()) return false;
                if (!Rotation._IsFinite()) return false;
                if (!Translation._IsFinite()) return false;

                return true;
            }
        }

        public bool IsIdentity
        {
            get
            {
                if (Scale != Vector3.One) return false;
                if (Rotation != Quaternion.Identity) return false;
                if (Translation != Vector3.Zero) return false;
                return true;
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Evaluates a <see cref="Matrix4x4"/> transform based on the available parameters.
        /// </summary>
        /// <param name="transform">A <see cref="Matrix4x4"/> instance, or null.</param>
        /// <param name="scale">A <see cref="Vector3"/> instance, or null.</param>
        /// <param name="rotation">A <see cref="Quaternion"/> instance, or null.</param>
        /// <param name="translation">A <see cref="Vector3"/> instance, or null.</param>
        /// <returns>A <see cref="Matrix4x4"/> transform.</returns>
        public static Matrix4x4 Evaluate(Matrix4x4? transform, Vector3? scale, Quaternion? rotation, Vector3? translation)
        {
            if (transform.HasValue) return transform.Value;

            return new AffineTransform(null, scale, rotation, translation).Matrix;
        }

        public static Matrix4x4 LocalToWorld(Matrix4x4 parentWorld, Matrix4x4 childLocal)
        {
            return childLocal * parentWorld;
        }

        public static Matrix4x4 WorldToLocal(Matrix4x4 parentWorld, Matrix4x4 childWorld)
        {
            return childWorld * parentWorld.Inverse();
        }

        public static AffineTransform Blend(ReadOnlySpan<AffineTransform> transforms, ReadOnlySpan<float> weights)
        {
            var s = Vector3.Zero;
            var r = default(Quaternion);
            var t = Vector3.Zero;

            for (int i = 0; i < transforms.Length; ++i)
            {
                var w = weights[i];

                s += transforms[i].Scale * w;
                r += transforms[i].Rotation * w;
                t += transforms[i].Translation * w;
            }

            r = Quaternion.Normalize(r);

            return AffineTransform.Create(s, r, t);
        }

        #endregion
    }
}
