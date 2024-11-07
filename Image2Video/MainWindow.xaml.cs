using Microsoft.WindowsAPICodePack.Dialogs;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
            Setting setting = new Setting();
            setting.LoadSetting();
            UpdateUi(setting);
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
            string[] filedirs;
            if (is_all.IsChecked == true)
                filedirs = Directory.GetFiles(files_dir.Text, "*", SearchOption.AllDirectories);
            else filedirs = Directory.GetFiles(files_dir.Text, "*", SearchOption.TopDirectoryOnly);
            //Debug.WriteLine(filedirs);
            var i = 0;
            var now_frame = 0;
            progressBar.IsIndeterminate = false;

            //保存设置
            Setting setting = new Setting();
            Dispatcher.Invoke(() => setting.UpdateSetting());
            setting.SaveSetting();

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
                        Img2frame(start, ref now_frame);
                    else LongImg2frame(start, ref now_frame);
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
                            Img2frame(mat, ref now_frame);
                        else LongImg2frame(mat, ref now_frame);
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
                        Img2frame(end, ref now_frame);
                    else LongImg2frame(end, ref now_frame);
                    Dispatcher.Invoke(() => progressBar.Value = 100);
                }
                Dispatcher.Invoke(() => progressBar.IsIndeterminate = true);
                string arg = "";
                bool gpu_check = true, cpu_check = false;
                string fps_text = "60", file_dir_text = "";
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
                if (File.Exists($"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4"))
                {
                    File.Delete($"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4");
                }
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

        private void UpdateUi(Setting setting)
        {
            if (setting == null)
                return;
            Dispatcher.Invoke(() =>
            {
                ret_width.Text = setting.ret_width.ToString();
                ret_height.Text = setting.ret_height.ToString();
                duration.Text = setting.duration.ToString();
                fps.Text = setting.fps.ToString();
                long_image_start.Text = setting.long_image_start.ToString();
                long_image_end.Text = setting.long_image_end.ToString();
                long_image_velocity.Text = setting.long_image_velocity.ToString();
                is_all.IsChecked = setting.is_all;
                has_start.IsChecked = setting.has_start;
                has_end.IsChecked = setting.has_end;

            });
        }
        private void LongImg2frame(Mat mat, ref int now_frame)
        {
            int canvas_width = 1080, canvas_height = 1920, f = 60, velocity = 1000, _long_image_start = 500, _sec = 5, _long_image_end = 500;
            Dispatcher.Invoke(() => canvas_width = ret_width.Text.Length > 0 ? int.Parse(ret_width.Text) : 1080);
            Dispatcher.Invoke(() => _sec = duration.Text.Length > 0 ? int.Parse(duration.Text) : 5);
            Dispatcher.Invoke(() => canvas_height = ret_height.Text.Length > 0 ? int.Parse(ret_height.Text) : 1920);
            Dispatcher.Invoke(() => f = fps.Text.Length > 0 ? int.Parse(fps.Text) : 60);
            Dispatcher.Invoke(() => velocity = long_image_velocity.Text.Length > 0 ? int.Parse(long_image_velocity.Text) : 500);
            Dispatcher.Invoke(() => _long_image_start = long_image_start.Text.Length > 0 ? int.Parse(long_image_start.Text) : 250);
            Dispatcher.Invoke(() => _long_image_end = long_image_end.Text.Length > 0 ? int.Parse(long_image_end.Text) : 250);
            Mat dst = new Mat();
            Cv2.Resize(mat, dst, new OpenCvSharp.Size(0, 0), (double)canvas_width / mat.Width, (double)canvas_width / mat.Width);
            //Cv2.ImWrite("Cache/tmp.jpg", dst);
            //移动速率 
            int sec = (dst.Height - canvas_height) / velocity;
            if (sec <= 0)
                sec = _sec;
            else if (sec > 30) sec = 30;
            int ratio = (dst.Height - canvas_height) / (sec * f) + 1;
            int end_frame = 0;
            //长图头停顿
            for (int i = 0; i < f * _long_image_start / 1000; i++)
            {
                now_frame++;
                Mat tmp = dst.Clone(new Rect(0, 0, canvas_width, canvas_height));
                Cv2.ImWrite($"Cache/{now_frame}.jpg", tmp);
            }
            for (int i = 0; i < f * sec; i++)
            {
                now_frame++;
                Mat tmp;
                if (i * ratio + canvas_height < dst.Height)
                    tmp = dst.Clone(new Rect(0, i * ratio, canvas_width, canvas_height));
                else
                {
                    end_frame = dst.Height - canvas_height;
                    tmp = dst.Clone(new Rect(0, dst.Height - canvas_height, canvas_width, canvas_height));
                }
                Cv2.ImWrite($"Cache/{now_frame}.jpg", tmp);
            }
            //长图尾停顿
            for (int i = 0; i < f * _long_image_end / 1000; i++)
            {
                now_frame++;
                Mat tmp = dst.Clone(new Rect(0, end_frame, canvas_width, canvas_height));
                Cv2.ImWrite($"Cache/{now_frame}.jpg", tmp);
            }
        }


        /// <summary>
        /// 短图片转帧
        /// </summary>
        private void Img2frame(Mat mat, ref int now_frame)
        {
            int sec = 5, f = 60;
            Dispatcher.Invoke(() => sec = duration.Text.Length > 0 ? int.Parse(duration.Text) : 5);
            Dispatcher.Invoke(() => f = fps.Text.Length > 0 ? int.Parse(fps.Text) : 60);
            for (int i = 0; i < f * sec; i++)
            {
                now_frame++;
                Cv2.ImWrite($"Cache/{now_frame}.jpg", mat);
            }

        }

    }
}