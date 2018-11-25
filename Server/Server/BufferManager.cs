using Microsoft.Language.Xml;
using System.Collections.Concurrent;

namespace Server
{
    internal class BufferManager
    {
        private ConcurrentDictionary<string, Buffer> _buffers = new ConcurrentDictionary<string, Buffer>();

        public void UpdateBuffer(string documentPath, Buffer buffer)
        {
            _buffers.AddOrUpdate(documentPath, buffer, (k, v) => buffer);
        }

        public Buffer GetBuffer(string documentPath)
        {
            return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
        }
    }
}
