using System;
using System.Drawing;

namespace Capture.Interface
{
    [Serializable]
    public class ScreenshotRequest : CrossAppDomainObject
    {
        public Guid RequestId { get; set; }
        public Rectangle RegionToCapture { get; set; }
        public Size? Resize { get; set; }
        public ImageFormat Format { get; set; }

        public ScreenshotRequest(Rectangle region, Size resize)
            : this(Guid.NewGuid(), region, resize) { }

        public ScreenshotRequest(Rectangle region)
            : this(Guid.NewGuid(), region, null) { }

        public ScreenshotRequest(Guid requestId, Rectangle region)
            : this(requestId, region, null) { }

        public ScreenshotRequest(Guid requestId, Rectangle region, Size? resize)
        {
            RequestId = requestId;
            RegionToCapture = region;
            Resize = resize;
        }

        public ScreenshotRequest Clone()
        {
            return new ScreenshotRequest(RequestId, RegionToCapture, Resize)
            {
                Format = Format
            };
        }
    }
}
