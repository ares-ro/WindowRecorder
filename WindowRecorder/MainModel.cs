using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowRecorder
{
    public partial class RecordSetting : ObservableValidator
    {
        [ObservableProperty]
        string savePath;

        [Range(1, 999999999, ErrorMessage = "1~999,999,999 사이의 값이여야 합니다.")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        int bitrate;

        [Range(1, 999, ErrorMessage = "1~999 사이의 값이여야 합니다.")]
        [NotifyDataErrorInfo]
        [ObservableProperty]
        int framerate;

        [ObservableProperty]
        bool isIncludeSound;
    }

    public class ResolutionSet : ObservableValidator
    {
        public bool isWindowSize;
        public int width;
        public int height;

        public ResolutionSet()
        {

        }
        public ResolutionSet(bool isWindowSize)
        {
            this.isWindowSize = isWindowSize;
        }
        public ResolutionSet(int width, int height)
        {
            this.isWindowSize = false;
            this.width = width;
            this.height = height;
        }
    }
}