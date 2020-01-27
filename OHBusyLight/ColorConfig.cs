using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OHBusyLight
{
    public class ColorConfig
    {
        public string Name { get; set; }
        public int DefaultBlinkThreshold { get; set; }
        public List<ColorConfigItems> StatusTypes { get; set; }
        public List<ColorConfigMessageItems> StatusMessages { get; set; }

    }

    public class ColorConfigItems
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public int BlinkDelay { get; set; }
        public int BlinkRate { get; set; }

    }
    public class ColorConfigMessageItems
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public int BlinkDelay { get; set; }
        public int BlinkRate { get; set; }

    }

}
