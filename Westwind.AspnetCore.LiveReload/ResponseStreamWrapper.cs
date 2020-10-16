using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspNetCore.LiveReload
{
    /// <summary>
    /// Wraps the Response Stream to inject the WebSocket HTML into 
    /// an HTML Page.
    /// </summary>
    public class ResponseStreamWrapper : Stream
    {
        private Stream _baseStream;
        private HttpContext _context;

        private bool _isContentLengthSet = false;


        public ResponseStreamWrapper(Stream baseStream, HttpContext context)
        {
            _baseStream = baseStream;
            _context = context;
            CanWrite = true;
        }

        public override void Flush() => _baseStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // this is called at the beginning of a request in 3.x and so
            // we have to set the ContentLength here as the flush/write locks headers
            if (!_isContentLengthSet && IsHtmlResponse())
            {
                _context.Response.Headers.ContentLength = null;
                _isContentLengthSet = true;
            }

            return _baseStream.FlushAsync(cancellationToken);
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

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

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _baseStream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            _baseStream.WriteByte(value);
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
            {
                _baseStream?.Write(buffer, offset, count);
            }
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
            {
                if (_baseStream != null)
                    await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }


        private bool? _isHtmlResponse = null;
        

        private bool IsHtmlResponse(bool forceReCheck = false)
        {
            if (!forceReCheck && _isHtmlResponse != null)
                return _isHtmlResponse.Value;

            // we need to check if the active request is still valid
            // this can fail if we're in the middle of an error response
            // or url rewrite in which case we can't intercept
            if (_context?.Response == null)
                return false;

            _isHtmlResponse =
                _context.Response?.Body != null &&
                (_context.Response.StatusCode == 200 || _context.Response.StatusCode == 500) &&
                _context.Response.ContentType != null &&
                _context.Response.ContentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) &&
                (_context.Response.ContentType.Contains("utf-8", StringComparison.OrdinalIgnoreCase) ||
                !_context.Response.ContentType.Contains("charset=", StringComparison.OrdinalIgnoreCase));

            if (!_isHtmlResponse.Value)
                return false;

            // Check for refresh exlusion rules
            RefreshInclusionModes refreshFile = RefreshInclusionModes.ContinueProcessing;
            if(LiveReloadConfiguration.Current.RefreshInclusionFilter != null)
            {
                refreshFile = LiveReloadConfiguration.Current.RefreshInclusionFilter.Invoke(_context.Request.Path.Value);
                if (refreshFile == RefreshInclusionModes.DontRefresh)
                    return false; // don't embed refresh script
            }

            
            // Make sure we force dynamic content type since we're
            // rewriting the content - static content will set the header explicitly
            // and fail when it doesn't matchif (_isHtmlResponse.Value)
            if (!_isContentLengthSet && _context.Response.ContentLength != null)
            {
                _context.Response.Headers.ContentLength = null;
                _isContentLengthSet = true;
            } 
                
            return _isHtmlResponse.Value;
        }

        public override  ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _baseStream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            //_baseStream?.Dispose();
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
