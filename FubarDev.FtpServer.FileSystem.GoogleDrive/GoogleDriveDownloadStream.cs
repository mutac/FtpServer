﻿//-----------------------------------------------------------------------
// <copyright file="GoogleDriveDownloadStream.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FubarDev.FtpServer.FileSystem.GoogleDrive
{
    /// <summary>
    /// Encapsulation of a <see cref="HttpWebResponse"/> stream.
    /// </summary>
    internal class GoogleDriveDownloadStream : Stream
    {
        private readonly HttpWebResponse _response;

        private readonly long _startPosition;

        private readonly Stream _responseStream;

        private long _position;

        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleDriveDownloadStream"/> class.
        /// </summary>
        /// <param name="response">The response of the file to download</param>
        /// <param name="startPosition">The start position of the file to download</param>
        /// <param name="contentLength">The full (not remaining) length of the file to download</param>
        public GoogleDriveDownloadStream(HttpWebResponse response, long startPosition, long contentLength)
        {
            _startPosition = startPosition;
            _response = response;
            _responseStream = response.GetResponseStream();
            Length = contentLength;
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length
        { get; }

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                return _startPosition + _position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var readCount = _responseStream.Read(buffer, offset, count);
            _position += readCount;
            return readCount;
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var readCount = await _responseStream.ReadAsync(buffer, offset, count, cancellationToken);
            _position += readCount;
            return readCount;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _responseStream.Dispose();
                    _response?.Dispose();
                }
                _disposedValue = true;
            }
        }
    }
}
