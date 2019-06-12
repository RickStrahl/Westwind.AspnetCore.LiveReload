using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Westwind.AspnetCore.LiveReload
{
    /// <summary>
    /// Wraps the Response Stream to inject the WebSocket HTML into 
    /// an HTML Page.
    /// </summary>
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
                WebsocketScriptInjectionHelper.InjectLiveReloadScriptAsync(buffer, offset, count, _context, _baseStream)
                                              .GetAwaiter()
                                              .GetResult();
            }
            else
                _baseStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count,
                                              CancellationToken cancellationToken)
        {
            if (IsHtmlResponse())
            {
                await WebsocketScriptInjectionHelper.InjectLiveReloadScriptAsync(
                    buffer, offset, count,
                    _context, _baseStream);
            }
            else
                await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
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


        #region Byte Helpers
        /// <summary>
        /// Tries to find a
        /// </summary>
        /// <param name="buffer">byte array to be searched</param>
        /// <param name="bufferToFind">byte to find</param>
        /// <returns></returns>
        public static int IndexOfByteArray(byte[] buffer, byte[] bufferToFind)
        {
            if (buffer.Length == 0 || bufferToFind.Length == 0)
                return -1;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == bufferToFind[0])
                {
                    bool innerMatch = true;
                    for (int j = 1; j < bufferToFind.Length; j++)
                    {
                        if (buffer[i + j] != bufferToFind[j])
                        {
                            innerMatch = false;
                            break;
                        }
                    }
                    if (innerMatch)
                        return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns an index into a byte array to find a string in the byte array.
        /// Exact match using the encoding provided or UTF-8 by default.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="stringToFind"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static int IndexOfByteArray(byte[] buffer, string stringToFind, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            if (buffer.Length == 0 || string.IsNullOrEmpty(stringToFind))
                return -1;

            var bytes = encoding.GetBytes(stringToFind);

            return IndexOfByteArray(buffer, bytes);
        }
        #endregion

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }
}
