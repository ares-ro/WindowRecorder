using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScreenRecorderLib;

namespace WindowRecorder
{
    public partial class MainViewModel : ObservableValidator
    {
        public MainViewModel()
        {
            GetWindowList();
            InitializeDefault();
            ValidateAllProperties();

            RecordSettingData.PropertyChanged += RecordSettingDataPropertyChanged;
        }

        //observableproperty 객체 영역
        [ObservableProperty]
        RecordSetting recordSettingData;

        [ObservableProperty]
        ObservableCollection<RecordableWindow> windows;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "공백일 수 없습니다.")]
        RecordableWindow? selectedWindow;

        [ObservableProperty]
        ResolutionSet selectedResolution;

        [ObservableProperty]
        BitmapSource bs;

        [ObservableProperty]
        bool isRecording;

        [ObservableProperty]
        bool canRecord;

        //이 클래스에서만 쓸 객체 영역
        Recorder rec;
        RecorderOptions options;

        [ObservableProperty]
        public List<ResolutionSet> resolutions = new()
        {
            new ResolutionSet(true),
            new ResolutionSet(960,540),
            new ResolutionSet(1280,720),
            new ResolutionSet(1920,1080),
            new ResolutionSet(2560,1440),
            new ResolutionSet(3840,2160)
        };

        //릴레이 커맨드 영역
        [RelayCommand]
        void LoadRecordSetting()
        {
            string savePath = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            if (!File.Exists(Path.Combine(savePath, "RecordSetting.json")))
            {
                RecordSettingData = new RecordSetting
                {
                    SavePath = PathConvert.FullToMasked(savePath),
                    IsIncludeSound = true,
                    Bitrate = 12000000,
                    Framerate = 60,
                };

                string rawJson = JsonConvert.SerializeObject(RecordSettingData, Formatting.Indented);
                File.WriteAllText(Path.Combine(savePath, "RecordSetting.json"), rawJson);
            }

            string json = File.ReadAllText(Path.Combine(savePath, "RecordSetting.json"));
            RecordSettingData = JsonConvert.DeserializeObject<RecordSetting>(json);
        }

        [RelayCommand]
        void SaveRecordSetting()
        {
            string savePath = AppDomain.CurrentDomain.BaseDirectory;

            RecordSetting buffer = new()
            {
                SavePath = RecordSettingData.SavePath,
                IsIncludeSound = RecordSettingData.IsIncludeSound,
                Bitrate = RecordSettingData.Bitrate,
                Framerate = RecordSettingData.Framerate
            };

            string rawJson = JsonConvert.SerializeObject(RecordSettingData, Formatting.Indented);
            File.WriteAllText(Path.Combine(savePath, "RecordSetting.json"), rawJson);
        }

        [RelayCommand]
        void GetWindowList()
        {
            Windows = new ObservableCollection<RecordableWindow>(Recorder.GetWindows());
        }

        [RelayCommand]
        void OpenFilePath()
        {
            Process.Start("explorer.exe", PathConvert.MaskedToFull(RecordSettingData.SavePath));
        }

        [RelayCommand]
        void SetFilePath()
        {
            OpenFolderDialog dialog = new();
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                RecordSettingData.SavePath = PathConvert.FullToMasked(dialog.FolderName);
            }
        }

        [RelayCommand]
        private void RecordButton()
        {
            if (IsRecording == false)
            {
                try
                {
                    StartRecording();
                    IsRecording = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                StopRecording();
                Bs = null;
                IsRecording = false;
            }
        }

        [RelayCommand]
        void ClosingProgram()
        {
            SaveRecordSetting();
        }

        //이 클래스에서만 쓸 메소드 영역
        void InitializeDefault()
        {
            LoadRecordSetting();
            SelectedResolution = Resolutions[0];
        }

        void StartRecording()
        {
            SetRecordOptions();
            rec = Recorder.CreateRecorder(options);
            rec.OnRecordingComplete += Rec_OnRecordingComplete;
            rec.OnRecordingFailed += Rec_OnRecordingFailed;
            rec.OnStatusChanged += Rec_OnStatusChanged;
            rec.OnFrameRecorded += Recorder_OnFrameRecorded;

            string filePath = Path.Combine(PathConvert.MaskedToFull(RecordSettingData.SavePath), $"{"video" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + " " + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second}.mp4");
            rec.Record(filePath);

            Debug.WriteLine("start");
        }

        void StopRecording()
        {
            rec.Stop();
            Debug.WriteLine("end");
        }

        void SetRecordOptions()
        {
            WindowRecordingSource selectedWrs = new WindowRecordingSource(SelectedWindow.Handle);
            bool isIncludeSound = RecordSettingData.IsIncludeSound;
            int bitrate = RecordSettingData.Bitrate;
            int framerate = RecordSettingData.Framerate;

            List<AudioDevice> inputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.InputDevices);
            List<AudioDevice> outputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.OutputDevices);
            AudioDevice selectedOutputDevice = outputDevices.FirstOrDefault();
            //AudioDevice selectedInputDevice = inputDevices.FirstOrDefault();

            options = new RecorderOptions();

            options.SourceOptions = new SourceOptions();
            options.SourceOptions.RecordingSources = new List<RecordingSourceBase> { selectedWrs };

            options.OutputOptions = new OutputOptions();
            options.OutputOptions.IsVideoFramePreviewEnabled = true;
            if (SelectedResolution.isWindowSize == false)
            {
                options.OutputOptions.OutputFrameSize = new ScreenSize(SelectedResolution.width, SelectedResolution.height);
            }

            options.AudioOptions = new AudioOptions();
            options.AudioOptions.IsAudioEnabled = isIncludeSound;
            options.AudioOptions.IsOutputDeviceEnabled = true;
            options.AudioOptions.AudioOutputDevice = selectedOutputDevice.DeviceName;
            options.AudioOptions.Bitrate = AudioBitrate.bitrate_192kbps;

            options.VideoEncoderOptions = new VideoEncoderOptions();
            options.VideoEncoderOptions.Bitrate = bitrate;
            options.VideoEncoderOptions.Framerate = framerate;
            options.VideoEncoderOptions.IsFixedFramerate = true;
            options.VideoEncoderOptions.Encoder = new H264VideoEncoder { BitrateMode = H264BitrateControlMode.CBR, EncoderProfile = H264Profile.Main };
            options.VideoEncoderOptions.IsFragmentedMp4Enabled = true;
            options.VideoEncoderOptions.IsThrottlingDisabled = false;
            options.VideoEncoderOptions.IsHardwareEncodingEnabled = true;
            options.VideoEncoderOptions.IsLowLatencyEnabled = false;
            options.VideoEncoderOptions.IsMp4FastStartEnabled = false;
        }

        void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {

        }
        void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            Debug.WriteLine(e.Error);
        }
        void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
        {

        }

        void Recorder_OnFrameRecorded(object sender, FrameRecordedEventArgs e)
        {
            int width = e.BitmapData.Width;
            int height = e.BitmapData.Height;
            int stride = e.BitmapData.Stride;

            PixelFormat pixelFormat = PixelFormats.Bgra32;

            var bitmapBuffer = BitmapSource.Create(
                width,
                height,
                96,
                96,
                pixelFormat,
                null,
                e.BitmapData.Data,
                stride * height,
                stride);
            bitmapBuffer.Freeze();

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                Bs = bitmapBuffer;
            });
        }

        partial void OnSelectedWindowChanged(RecordableWindow? oldValue, RecordableWindow? newValue)
        {
            CheckCanRecord();
        }

        partial void OnRecordSettingDataChanged(RecordSetting? oldValue, RecordSetting newValue)
        {
            CheckCanRecord();
        }

        partial void OnSelectedResolutionChanged(ResolutionSet? oldValue, ResolutionSet newValue)
        {
            CheckCanRecord();
        }

        void RecordSettingDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CheckCanRecord();
        }

        void CheckCanRecord()
        {
            if (RecordSettingData.HasErrors | (SelectedWindow == null))
            {
                CanRecord = false;
            }
            else
            {
                CanRecord = true;
            }
        }
    }
}