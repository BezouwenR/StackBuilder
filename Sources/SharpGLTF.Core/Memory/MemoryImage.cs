﻿using System;
using System.Collections.Generic;
using System.Text;

using BYTES = System.ArraySegment<System.Byte>;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Represents an image file stored as an in-memory byte array
    /// </summary>
    public readonly struct MemoryImage
    {
        #region constants

        const string EMBEDDED_OCTET_STREAM = "data:application/octet-stream;base64,";
        const string EMBEDDED_GLTF_BUFFER = "data:application/gltf-buffer;base64,";
        const string EMBEDDED_JPEG_BUFFER = "data:image/jpeg;base64,";
        const string EMBEDDED_PNG_BUFFER = "data:image/png;base64,";
        const string EMBEDDED_DDS_BUFFER = "data:image/vnd-ms.dds;base64,";
        const string EMBEDDED_WEBP_BUFFER = "data:image/webp;base64,";

        const string MIME_PNG = "image/png";
        const string MIME_JPG = "image/jpeg";
        const string MIME_DDS = "image/vnd-ms.dds";
        const string MIME_WEBP = "image/webp";

        /// <summary>
        /// Represents a 4x4 white PNG image.
        /// </summary>
        private const string DEFAULT_PNG_IMAGE = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAHXpUWHRUaXRsZQAACJlzSU1LLM0pCUmtKCktSgUAKVIFt/VCuZ8AAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAANElEQVQoz2O8cuUKAwxoa2vD2VevXsUqzsRAIqC9Bsb///8TdDey+CD0Awsx7h6NB5prAADPsx0VAB8VRQAAAABJRU5ErkJggg==";

        internal static Byte[] DefaultPngImage => Convert.FromBase64String(DEFAULT_PNG_IMAGE);

        internal static readonly string[] _EmbeddedHeaders =
                { EMBEDDED_OCTET_STREAM
                , EMBEDDED_GLTF_BUFFER
                , EMBEDDED_JPEG_BUFFER
                , EMBEDDED_PNG_BUFFER
                , EMBEDDED_DDS_BUFFER
                , EMBEDDED_WEBP_BUFFER };

        public static MemoryImage Empty => default;

        #endregion

        #region constructor

        public static implicit operator MemoryImage(BYTES image) { return new MemoryImage(image); }

        public static implicit operator MemoryImage(Byte[] image) { return new MemoryImage(image); }

        public MemoryImage(BYTES image) { _Image = image; }

        public MemoryImage(Byte[] image) { _Image = image == null ? default : new BYTES(image); }

        public MemoryImage(string filePath)
        {
            var data = System.IO.File.ReadAllBytes(filePath);
            _Image = new BYTES(data);
        }

        #endregion

        #region data

        private readonly BYTES _Image;

        #endregion

        #region properties

        public bool IsEmpty => _Image.Count == 0;

        /// <summary>
        /// Gets a value indicating whether this object represents a valid PNG image.
        /// </summary>
        public bool IsPng => _IsPngImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid JPG image.
        /// </summary>
        public bool IsJpg => _IsJpgImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid DDS image.
        /// </summary>
        public bool IsDds => _IsDdsImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid WEBP image.
        /// </summary>
        public bool IsWebp => _IsWebpImage(_Image);

        /// <summary>
        /// Gets a value indicating whether this object represents a valid image.
        /// </summary>
        public bool IsValid => _IsImage(_Image);

        /// <summary>
        /// Gets the most appropriate extension string for this image.
        /// </summary>
        public string FileExtension
        {
            get
            {
                if (IsPng) return "png";
                if (IsJpg) return "jpg";
                if (IsDds) return "dds";
                if (IsWebp) return "webp";
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the most appropriate Mime type string for this image.
        /// </summary>
        public string MimeType
        {
            get
            {
                if (IsPng) return MIME_PNG;
                if (IsJpg) return MIME_JPG;
                if (IsDds) return MIME_DDS;
                if (IsWebp) return MIME_WEBP;
                return "raw";
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Opens the image file for reading its contents
        /// </summary>
        /// <returns>A read only <see cref="System.IO.Stream"/>.</returns>
        public System.IO.Stream Open()
        {
            if (_Image.Count == 0) return null;
            return new System.IO.MemoryStream(_Image.Array, _Image.Offset, _Image.Count, false);
        }

        /// <summary>
        /// Returns this image file, enconded as a Mime64 string.
        /// </summary>
        /// <param name="withPrefix">true to prefix the string with a header.</param>
        /// <returns>A mime64 string.</returns>
        public string ToMime64(bool withPrefix = true)
        {
            if (!this.IsValid) return null;

            var mimeContent = string.Empty;
            if (withPrefix)
            {
                if (this.IsPng) mimeContent = EMBEDDED_PNG_BUFFER;
                if (this.IsJpg) mimeContent = EMBEDDED_JPEG_BUFFER;
                if (this.IsDds) mimeContent = EMBEDDED_DDS_BUFFER;
                if (this.IsWebp) mimeContent = EMBEDDED_WEBP_BUFFER;
            }

            return mimeContent + Convert.ToBase64String(_Image.Array, _Image.Offset, _Image.Count, Base64FormattingOptions.None);
        }

        /// <summary>
        /// Gets the internal buffer.
        /// </summary>
        /// <returns>An array buffer.</returns>
        public BYTES GetBuffer() { return _Image; }

        /// <summary>
        /// Tries to parse a Mime64 string to a Byte array.
        /// </summary>
        /// <param name="mime64content">The Mime64 string source.</param>
        /// <returns>A byte array representing an image file, or null if the image was not identified.</returns>
        public static Byte[] TryParseBytes(string mime64content)
        {
            return _TryParseBase64Unchecked(mime64content, EMBEDDED_GLTF_BUFFER)
                ?? _TryParseBase64Unchecked(mime64content, EMBEDDED_OCTET_STREAM)
                ?? _TryParseBase64Unchecked(mime64content, EMBEDDED_JPEG_BUFFER)
                ?? _TryParseBase64Unchecked(mime64content, EMBEDDED_PNG_BUFFER)
                ?? _TryParseBase64Unchecked(mime64content, EMBEDDED_DDS_BUFFER)
                ?? null;
        }

        /// <summary>
        /// identifies an image of a specific type.
        /// </summary>
        /// <param name="format">A string representing the format: png, jpg, dds...</param>
        /// <returns>True if this image is of the given type.</returns>
        public bool IsImageOfType(string format)
        {
            Guard.NotNullOrEmpty(format, nameof(format));

            if (!IsValid) return false;

            if (format.EndsWith("png", StringComparison.OrdinalIgnoreCase)) return IsPng;
            if (format.EndsWith("jpg", StringComparison.OrdinalIgnoreCase)) return IsJpg;
            if (format.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase)) return IsJpg;
            if (format.EndsWith("dds", StringComparison.OrdinalIgnoreCase)) return IsDds;
            if (format.EndsWith("webp", StringComparison.OrdinalIgnoreCase)) return IsWebp;

            return false;
        }

        #endregion

        #region internals

        private static Byte[] _TryParseBase64Unchecked(string uri, string prefix)
        {
            if (uri == null) return null;
            if (!uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

            var content = uri.Substring(prefix.Length);
            return Convert.FromBase64String(content);
        }

        private static bool _IsPngImage(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x89) return false;
            if (data[1] != 0x50) return false;
            if (data[2] != 0x4e) return false;
            if (data[3] != 0x47) return false;

            return true;
        }

        private static bool _IsJpgImage(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0xff) return false;
            if (data[1] != 0xd8) return false;

            return true;
        }

        private static bool _IsDdsImage(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x44) return false;
            if (data[1] != 0x44) return false;
            if (data[2] != 0x53) return false;
            if (data[3] != 0x20) return false;
            return true;
        }

        private static bool _IsWebpImage(IReadOnlyList<Byte> data)
        {
            // RIFF
            if (data[0] != 0x52) return false;
            if (data[1] != 0x49) return false;
            if (data[2] != 0x46) return false;
            if (data[3] != 0x46) return false;

            // WEBP
            if (data[8] != 0x57) return false;
            if (data[9] != 0x45) return false;
            if (data[10] != 0x42) return false;
            if (data[11] != 0x50) return false;

            return true;
        }

        private static bool _IsImage(IReadOnlyList<Byte> data)
        {
            if (data == null) return false;
            if (data.Count < 12) return false;

            if (_IsDdsImage(data)) return true;
            if (_IsJpgImage(data)) return true;
            if (_IsPngImage(data)) return true;
            if (_IsWebpImage(data)) return true;

            return false;
        }

        #endregion
    }
}
