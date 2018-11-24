using System.Collections.Concurrent;

namespace Server
{
    internal class BufferManager
    {
        private ConcurrentDictionary<string, string> _buffers = new ConcurrentDictionary<string, string>();

        public void UpdateBuffer(string documentPath, string text)
        {
            _buffers.AddOrUpdate(documentPath, text, (k, v) => text);
        }

        public string GetBuffer(string documentPath)
        {
            return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : string.Empty;
        }
    }
}
