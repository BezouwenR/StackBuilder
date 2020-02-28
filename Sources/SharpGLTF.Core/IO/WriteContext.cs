﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

using BYTES = System.ArraySegment<byte>;
using SCHEMA2 = SharpGLTF.Schema2.ModelRoot;

namespace SharpGLTF.IO
{
    /// <summary>
    /// Callback used for saving associated files of the current model.
    /// </summary>
    /// <param name="assetName">The asset relative path.</param>
    /// <param name="assetData">The file contents as a <see cref="byte"/> array.</param>
    public delegate void FileWriterCallback(String assetName, BYTES assetData);

    /// <summary>
    /// Configuration settings for writing model files.
    /// </summary>
    public class WriteContext : WriteSettings
    {
        #region lifecycle

        public static WriteContext Create(FileWriterCallback callback)
        {
            Guard.NotNull(callback, nameof(callback));

            var context = new WriteContext(callback)
            {
                _UpdateSupportedExtensions = true
            };

            return context;
        }

        public static WriteContext CreateFromFile(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            return CreateFromDirectory(dir);
        }

        public static WriteContext CreateFromDirectory(string dirPath)
        {
            Guard.DirectoryPathMustExist(dirPath, nameof(dirPath));

            var context = Create((fn, d) => File.WriteAllBytes(Path.Combine(dirPath, fn), d.ToArray()));
            context.ImageWriting = ResourceWriteMode.SatelliteFile;
            context.JsonIndented = true;
            return context;
        }

        public static WriteContext CreateFromDictionary(IDictionary<string, BYTES> dict)
        {
            Guard.NotNull(dict, nameof(dict));

            var context = Create((fn, buff) => dict[fn] = buff);
            context.ImageWriting = ResourceWriteMode.SatelliteFile;
            context.MergeBuffers = false;
            context.JsonIndented = false;

            return context;
        }

        public static WriteContext CreateFromStream(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanWrite, nameof(stream));

            var context = Create((fn, d) => stream.Write(d.Array, d.Offset, d.Count));
            context.ImageWriting = ResourceWriteMode.Embedded;
            context.MergeBuffers = true;
            context.JsonIndented = false;

            return context.WithBinarySettings();
        }

        public WriteContext WithBinarySettings()
        {
            ImageWriting = ResourceWriteMode.BufferView;
            MergeBuffers = true;
            JsonIndented = false;

            return this;
        }

        /// <summary>
        /// These settings are used exclusively by <see cref="SCHEMA2.DeepClone"/>.
        /// </summary>
        /// <returns>A <see cref="WriteContext"/> instance to be used by <see cref="SCHEMA2.DeepClone()"/></returns>
        internal WriteContext WithDeepCloneSettings()
        {
            _UpdateSupportedExtensions = false;
            _NoCloneWatchdog = true;
            MergeBuffers = false;

            return this;
        }

        private WriteContext(FileWriterCallback callback)
        {
            _FileWriter = callback;
        }

        #endregion

        #region data

        private readonly FileWriterCallback _FileWriter;

        #endregion

        #region properties

        /// <summary>
        /// Gets a value indicating whether to scan the whole model for used extensions.
        /// </summary>
        internal Boolean _UpdateSupportedExtensions { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether creating a defensive copy before serialization is not allowed.
        /// </summary>
        internal bool _NoCloneWatchdog { get; private set; } = false;

        #endregion

        #region API

        public void WriteAllBytesToEnd(string fileName, BYTES data)
        {
            this._FileWriter(fileName, data);
        }

        /// <summary>
        /// Writes <paramref name="model"/> to this context.
        /// </summary>
        /// <param name="baseName">The base name to use for asset files, without extension.</param>
        /// <param name="model">The <see cref="SCHEMA2"/> to write.</param>
        public void WriteTextSchema2(string baseName, SCHEMA2 model)
        {
            Guard.NotNullOrEmpty(baseName, nameof(baseName));
            Guard.NotNull(model, nameof(model));

            model = this._PreprocessSchema2(model, this.ImageWriting == ResourceWriteMode.BufferView, this.MergeBuffers);
            Guard.NotNull(model, nameof(model));

            model._PrepareBuffersForSatelliteWriting(this, baseName);

            model._PrepareImagesForWriting(this, baseName, ResourceWriteMode.SatelliteFile);

            _ValidateBeforeWriting(model);

            using (var m = new MemoryStream())
            {
                model._WriteJSON(m, this.JsonIndented);

                WriteAllBytesToEnd($"{baseName}.gltf", m.ToArraySegment());
            }

            model._AfterWriting();
        }

        /// <summary>
        /// Writes <paramref name="model"/> to this context.
        /// </summary>
        /// <param name="baseName">The base name to use for asset files, without extension.</param>
        /// <param name="model">The <see cref="SCHEMA2"/> to write.</param>
        public void WriteBinarySchema2(string baseName, SCHEMA2 model)
        {
            Guard.NotNullOrEmpty(baseName, nameof(baseName));
            Guard.NotNull(model, nameof(model));

            model = this._PreprocessSchema2(model, this.ImageWriting == ResourceWriteMode.BufferView, true);
            Guard.NotNull(model, nameof(model));

            var ex = BinarySerialization.IsBinaryCompatible(model);
            if (ex != null) throw ex;

            model._PrepareBuffersForInternalWriting();

            model._PrepareImagesForWriting(this, baseName, ResourceWriteMode.Embedded);

            _ValidateBeforeWriting(model);

            using (var m = new MemoryStream())
            {
                using (var w = new BinaryWriter(m))
                {
                    BinarySerialization.WriteBinaryModel(w, model);
                }

                WriteAllBytesToEnd($"{baseName}.glb", m.ToArraySegment());
            }

            model._AfterWriting();
        }

        #endregion

        #region core

        /// <summary>
        /// This needs to be called immediately before writing to json,
        /// but immediately after preprocessing and buffer setup, so we have a valid model.
        /// </summary>
        /// <param name="model"></param>
        private void _ValidateBeforeWriting(SCHEMA2 model)
        {
            if (_NoCloneWatchdog) return;

            var vcontext = new Validation.ValidationResult(model, Validation.ValidationMode.Strict);

            model.ValidateReferences(vcontext.GetContext());
            var ex = vcontext.Errors.FirstOrDefault();
            if (ex != null) throw ex;

            model.Validate(vcontext.GetContext());
            ex = vcontext.Errors.FirstOrDefault();
            if (ex != null) throw ex;
        }

        /// <summary>
        /// Prepares the model for writing with the appropiate settings, creating a defensive copy if neccesary.
        /// </summary>
        /// <param name="model">The source <see cref="SCHEMA2"/> instance.</param>
        /// <param name="imagesAsBufferViews">true if images should be stored as buffer views.</param>
        /// <param name="mergeBuffers">true if it's required the model must have a single buffer.</param>
        /// <returns>The source <see cref="SCHEMA2"/> instance, or a cloned and modified instance if current settings required it.</returns>
        private SCHEMA2 _PreprocessSchema2(SCHEMA2 model, bool imagesAsBufferViews, bool mergeBuffers)
        {
            Guard.NotNull(model, nameof(model));

            foreach (var img in model.LogicalImages) if (!img._HasContent) throw new Validation.DataException(img, "Image Content is missing.");

            // check if we need to modify the model before saving it,
            // in order to create a defensive copy.

            if (model.LogicalImages.Count == 0) imagesAsBufferViews = false;
            if (model.LogicalBuffers.Count <= 1 && !imagesAsBufferViews) mergeBuffers = false;

            if (mergeBuffers | imagesAsBufferViews)
            {
                // cloning check is done to prevent cloning from entering in an infinite loop where each clone attempt triggers another clone request.
                if (_NoCloneWatchdog) throw new InvalidOperationException($"Current settings require creating a densive copy before model modification, but calling {nameof(SCHEMA2.DeepClone)} is not allowed with the current settings.");

                model = model.DeepClone();
            }

            if (imagesAsBufferViews) model.MergeImages();
            if (mergeBuffers) model.MergeBuffers();

            if (this._UpdateSupportedExtensions) model.UpdateExtensionsSupport();

            return model;
        }

        #endregion
    }
}
