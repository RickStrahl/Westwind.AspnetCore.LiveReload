using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspnetCore.LiveReload
{
    public class ResponseStreamWrapper : Stream
    {
        private Stream _baseStream;
        private HttpContext _context;

        public ResponseStreamWrapper(Stream baseStream, HttpContext context)
        {
            _baseStream = baseStream;
            _context = context;
            CanWrite = true;
        }

        public override void Flush() => _baseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            //return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
            IsHtmlResponse(forceReCheck: true);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (IsHtmlResponse())
            {
                string html = Encoding.UTF8.GetString(buffer.ToArray());
                html = LiveReloadMiddleware.InjectLiveReloadScript(html, _context);

                var bytes = Encoding.UTF8.GetBytes(html);
                _baseStream.Write(bytes, offset, bytes.Length);
            }
            else
                _baseStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (IsHtmlResponse())
            {
                string html = Encoding.UTF8.GetString(buffer.Take(count).ToArray());
                html = LiveReloadMiddleware.InjectLiveReloadScript(html, _context);

                var bytes = Encoding.UTF8.GetBytes(html);
                await _baseStream.WriteAsync(bytes, offset, bytes.Length, cancellationToken);
            }
            else
                await _baseStream.WriteAsync(buffer, offset, count,cancellationToken);
        }

        private bool? _isHtmlResponse = null;
        private bool IsHtmlResponse(bool forceReCheck = false)
        {
            if (!forceReCheck && _isHtmlResponse != null)
                return _isHtmlResponse.Value;

            _isHtmlResponse =
                _context.Response.StatusCode == 200 &&
                _context.Response.ContentType != null &&
                _context.Response.ContentType.Contains("text/html", StringComparison.InvariantCultureIgnoreCase) &&
                (_context.Response.ContentType.Contains("utf-8", StringComparison.InvariantCultureIgnoreCase) ||
                !_context.Response.ContentType.Contains("charset=", StringComparison.InvariantCultureIgnoreCase));


            return _isHtmlResponse.Value;
        }

        public override  ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _baseStream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            _baseStream?.Dispose();
            _baseStream = null;
            _context = null;

            base.Dispose(disposing);
        }

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }
}
