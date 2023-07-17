using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoDownloader
{
    public class DownloaderSetting
    {
        public int Count { get; set; }

        public int Parallelism { get; set; }

        public string SavePath { get; set; }

        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

    }
}
