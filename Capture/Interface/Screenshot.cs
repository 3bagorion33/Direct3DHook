using System;

namespace Capture.Interface
{
    public class Screenshot : CrossAppDomainObject
    {
        private Guid _requestId;
        public Guid RequestId => _requestId;
        public ImageFormat Format { get; set; }
        public System.Drawing.Imaging.PixelFormat PixelFormat { get; set; }
        public int Stride { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        private byte[] _data;
        public byte[] Data => _data;
        public Screenshot(Guid requestId, byte[] data)
        {
            _requestId = requestId;
            _data = data;
        }
    }
}
