﻿using System;

namespace Capture.Interface
{
    [Serializable]
    public class DisplayTextEventArgs
    {
        public string Text { get; set; }
        public TimeSpan Duration { get; set; }

        public DisplayTextEventArgs(string text, TimeSpan duration)
        {
            Text = text;
            Duration = duration;
        }

        public override string ToString()
        {
            return string.Format("{0}", Text);
        }
    }
}
