using System;

namespace Capture.Interface
{
    [Serializable]
    public class ScreenshotReceivedEventArgs//: CrossAppDomainObject
    {
        public int ProcessId { get; set; }
        public Screenshot Screenshot { get; set; }

        public ScreenshotReceivedEventArgs(int processId, Screenshot screenshot)
        {
            ProcessId = processId;
            Screenshot = screenshot;
        }
    }
}
