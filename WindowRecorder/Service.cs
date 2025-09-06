using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace WindowRecorder
{
    public static class PathConvert
    {
        public static string FullToMasked(string path)
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string masked = "%USERPROFILE%";
            string maskedPath;

            if (path.StartsWith(userProfilePath))
            {
                maskedPath = path.Replace(userProfilePath, masked);
            }
            else
            {
                maskedPath = path;
            }

            return maskedPath;
        }
        public static string MaskedToFull(string path)
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string masked = "%USERPROFILE%";
            string fullPath;

            if (path.StartsWith(masked))
            {
                fullPath = path.Replace(masked, userProfilePath);
            }
            else
            {
                fullPath = path;
            }

            return fullPath;
        }
    }

    public class ResolutionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ResolutionSet rs)
            {
                if (rs.isWindowSize == true)
                {
                    return "선택된 윈도우에 맞춤";
                }
                else
                {
                    return rs.width + "×" + rs.height;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class SIConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<string> si = new() { "", "K", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" };
            int count = 0;

            if (double.TryParse(value.ToString(), out double data))
            {
                while (data / 1000 >= 1)
                {
                    count += 1;
                    data /= 1000;
                }
                double rounded = Math.Round((double)data, 1);
                return rounded.ToString() + si[count];
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}