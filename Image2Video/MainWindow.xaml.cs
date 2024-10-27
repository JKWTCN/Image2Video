using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;
using MS.WindowsAPICodePack.Internal;
using OpenCvSharp;
using Path = System.IO.Path;
using Rect = OpenCvSharp.Rect;
using Window = System.Windows.Window;

namespace Image2Video
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Debug.WriteLine(System.Environment.CurrentDirectory);
            this.Closing += CleanCache;
            // 生成缓存文件夹
            if (!Directory.Exists("./Cache"))
                Directory.CreateDirectory("./Cache");
            else
            {
                Directory.Delete("./Cache", true);
                Directory.CreateDirectory("./Cache");
            }
            // 输出文件夹
            if (!Directory.Exists("./Res"))
                Directory.CreateDirectory("./Res");
        }

        /// <summary>
        /// 释放缓存
        /// </summary>
        private void CleanCache(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Directory.Exists("./Cache"))
                Directory.Delete("./Cache", true);
        }

        private void Open_files_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return;
            }
            string folderName = dialog.FileName;
            files_dir.Text = folderName;
        }

        private void All_do_Click(object sender, RoutedEventArgs e)
        {
            string[] filedirs;
            if (is_all.IsChecked == true)
                filedirs = Directory.GetFiles(files_dir.Text, "*", SearchOption.AllDirectories);
            else filedirs = Directory.GetFiles(files_dir.Text, "*", SearchOption.TopDirectoryOnly);
            //Debug.WriteLine(filedirs);
            var i = 0;
            progressBar.IsIndeterminate = false;
            Task.Run(() =>
            {
                bool btn_has_start = true, btn_has_end = false;
                Dispatcher.Invoke(() => btn_has_start = (bool)has_start.IsChecked);
                Dispatcher.Invoke(() => btn_has_end = (bool)has_end.IsChecked);
                int all = 0;
                if (btn_has_start == true)
                    all++;
                if (btn_has_end == true)
                    all++;
                all += filedirs.Length;
                if (btn_has_start == true)
                {
                    i++;
                    Debug.WriteLine("开始处理头图");
                    Mat start = Cv2.ImRead("Res/start.png");
                    var result = Standardize_images(ref start);
                    if (result)
                        Img2frame(start, i);
                    else LongImg2frame(start, i);
                    Dispatcher.Invoke(() => progressBar.Value = 100 * 1 / all);
                }
                foreach (var file in filedirs)
                {
                    i++;
                    Debug.WriteLine($"开始处理第{i}张");
                    string ext = Path.GetExtension(file);
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".PNG")
                    {
                        Mat mat = Cv2.ImRead(file);
                        var result = Standardize_images(ref mat);
                        if (result)
                            Img2frame(mat, i);
                        else LongImg2frame(mat, i);
                    }
                    Dispatcher.Invoke(() => progressBar.Value = 100 * (i + 1) / all);
                }
                if (btn_has_end == true)
                {
                    i++;
                    Debug.WriteLine("开始处理尾图");
                    Mat end = Cv2.ImRead("Res/end.png");
                    var result = Standardize_images(ref end);
                    if (result)
                        Img2frame(end, i);
                    else LongImg2frame(end, i);
                    Dispatcher.Invoke(() => progressBar.Value = 100);
                }
                Dispatcher.Invoke(() => progressBar.IsIndeterminate = true);
                string arg = "";
                bool gpu_check = true, cpu_check = false;
                string fps_text = "25", file_dir_text = "";
                Dispatcher.Invoke(() => fps_text = fps.Text);
                Dispatcher.Invoke(() => file_dir_text = files_dir.Text);
                string ffmpeg_path = System.Environment.CurrentDirectory + @"\ffmpeg\ffmpeg.exe";
                ProcessStartInfo info = new ProcessStartInfo(ffmpeg_path, $" -y -r {fps_text} -i {System.Environment.CurrentDirectory}\\Cache\\%d.jpg {System.Environment.CurrentDirectory}\\Cache\\output.mp4");
                info.UseShellExecute = true;
                info.RedirectStandardInput = false;
                info.RedirectStandardOutput = false;
                info.RedirectStandardError = false;
                info.CreateNoWindow = true;
                Process AppProcess = System.Diagnostics.Process.Start(info);
                AppProcess.WaitForExit();
                var files_dir_list = file_dir_text.Split("\\");
                File.Move("./Cache/output.mp4", $"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4");
                Dispatcher.Invoke(() => progressBar.IsIndeterminate = false);
                if (Directory.Exists("./Cache"))
                    Directory.Delete("./Cache", true);
            });


        }

        private Boolean Standardize_images(ref Mat mat)
        {
            int canvas_width = 1080, canvas_height = 1920;
            Dispatcher.Invoke(() => canvas_width = ret_width.Text.Length > 0 ? int.Parse(ret_width.Text) : 1080);
            Dispatcher.Invoke(() => canvas_height = ret_height.Text.Length > 0 ? int.Parse(ret_height.Text) : 1920);
            //Debug.WriteLine((double)canvas_width / mat.Width);
            Mat dst = new Mat();
            Cv2.Resize(mat, dst, new OpenCvSharp.Size(0, 0), (double)canvas_width / mat.Width, (double)canvas_width / mat.Width);
            //Cv2.ImWrite("Cache/tmp.jpg", dst);
            if (dst.Height <= canvas_height)
            {
                int top = (canvas_height - dst.Height) / 2;
                int bottom = (canvas_height - dst.Height) / 2;
                if (top + bottom + dst.Height < canvas_height)
                {
                    bottom = canvas_height - dst.Height - top;
                }
                Cv2.CopyMakeBorder(dst, dst, top, bottom, 0, 0, BorderTypes.Constant, new Scalar(0, 0, 0));
                //Cv2.ImWrite("Cache/tmp.jpg", dst);
                mat = dst;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void LongImg2frame(Mat mat, int num)
        {
            int canvas_width = 1080, canvas_height = 1920, sec = 5, f = 25;
            Dispatcher.Invoke(() => canvas_width = ret_width.Text.Length > 0 ? int.Parse(ret_width.Text) : 1080);
            Dispatcher.Invoke(() => canvas_height = ret_height.Text.Length > 0 ? int.Parse(ret_height.Text) : 1920);
            Dispatcher.Invoke(() => sec = duration.Text.Length > 0 ? int.Parse(duration.Text) : 5);
            Dispatcher.Invoke(() => f = fps.Text.Length > 0 ? int.Parse(fps.Text) : 25);
            Mat dst = new Mat();
            Cv2.Resize(mat, dst, new OpenCvSharp.Size(0, 0), (double)canvas_width / mat.Width, (double)canvas_width / mat.Width);
            //Cv2.ImWrite("Cache/tmp.jpg", dst);
            int ratio = (dst.Height - canvas_height) / (sec * f) + 1;
            for (int i = 0; i < f * sec; i++)
            {
                Mat tmp;
                if (i * ratio + canvas_height < dst.Height)
                    tmp = dst.Clone(new Rect(0, i * ratio, canvas_width, canvas_height));
                else
                    tmp = dst.Clone(new Rect(0, dst.Height - canvas_height, canvas_width, canvas_height));
                Cv2.ImWrite($"Cache/{i + (num - 1) * sec * f}.jpg", tmp);
            }
        }


        /// <summary>
        /// 短图片转帧
        /// </summary>
        private void Img2frame(Mat mat, int num)
        {
            int sec = 5, f = 25;
            Dispatcher.Invoke(() => sec = duration.Text.Length > 0 ? int.Parse(duration.Text) : 5);
            Dispatcher.Invoke(() => f = fps.Text.Length > 0 ? int.Parse(fps.Text) : 25);
            for (int i = 0; i < f * sec; i++)
            {
                Cv2.ImWrite($"Cache/{i + (num - 1) * sec * f}.jpg", mat);
            }

        }

    }
}