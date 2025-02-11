using Microsoft.WindowsAPICodePack.Dialogs;
using OpenCvSharp;
using Shell32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Path = System.IO.Path;
using Rect = OpenCvSharp.Rect;
using Window = System.Windows.Window;
namespace Image2Video
{

    public class BGM
    {
        public int ID { get; set; }
        public string? Name { get; set; }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private void BGM_LOAD()
        {
            List<BGM> list = new();
            string[] files = Directory.GetFiles("BGM", "*.mp3");
            foreach (var file in files)
            {
                list.Add(new BGM { ID = list.Count + 1, Name = file });
            }
            BGM_Combo.ItemsSource = list;

        }
        String uuid = Guid.NewGuid().ToString();

        int now_thread = 0;
        int all_thread = 0;
        public MainWindow()
        {
            InitializeComponent();
            //Debug.WriteLine(System.Environment.CurrentDirectory);
            this.Closing += CleanCache;
            //生成BGM文件夹
            if (!Directory.Exists("./BGM"))
                Directory.CreateDirectory("./BGM");
            if (!Directory.Exists("./Cache"))
                Directory.CreateDirectory("./Cache");
            // 生成缓存文件夹
            if (!Directory.Exists($"./Cache/{uuid}"))
                Directory.CreateDirectory($"./Cache/{uuid}");
            else
            {
                Directory.Delete($"./Cache/{uuid}", true);
                Directory.CreateDirectory($"./Cache/{uuid}");
            }
            // 输出文件夹
            if (!Directory.Exists("./Res"))
                Directory.CreateDirectory("./Res");
            Setting setting = new();
            setting.LoadSetting();
            UpdateUi(setting);
            BGM_LOAD();
        }

        /// <summary>
        /// 释放缓存
        /// </summary>
        private void CleanCache(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Directory.Exists($"./Cache/{uuid}"))
                Directory.Delete($"./Cache/{uuid}", true);
            Setting.UpdateSetting();
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
            now_thread = 0;
            all_thread = 0;
            if (!Directory.Exists($"./Cache/{uuid}"))
                Directory.CreateDirectory($"./Cache/{uuid}");
            else
            {
                Directory.Delete($"./Cache/{uuid}", true);
                Directory.CreateDirectory($"./Cache/{uuid}");
            }
            // 输出文件夹
            if (!Directory.Exists("./Res"))
                Directory.CreateDirectory("./Res");
            if (files_dir.Text == null || files_dir.Text == "")
            {
                return;
            }
            string[] filedirs;
            if (is_all.IsChecked == true)
                filedirs = Directory.GetFiles(files_dir.Text, "*", SearchOption.AllDirectories);
            else filedirs = Directory.GetFiles(files_dir.Text, "*", SearchOption.TopDirectoryOnly);
            //Debug.WriteLine(filedirs);
            var i = 0;
            var now_frame = 0;
            progressBar.IsIndeterminate = false;


            _ = Task.Run(() =>
            {
                bool btn_has_start = true, btn_has_end = false;
                _ = Dispatcher.Invoke(() => btn_has_start = (bool)has_start.IsChecked);
                _ = Dispatcher.Invoke(() => btn_has_end = (bool)has_end.IsChecked);
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
                    FrameInfo frameInfo = new FrameInfo { mat = start, now_frame = now_frame };
                    if (result)
                    {
                        int end_time = GetImg2frameOver(start, now_frame);
                        new Thread(new ParameterizedThreadStart(Img2frameThread)).Start(frameInfo);
                        now_thread++;
                        all_thread++;
                        now_frame = end_time;
                    }
                    else
                    {
                        int end_time = GetOverLongImg2frame(start, now_frame);
                        new Thread(new ParameterizedThreadStart(LongImg2frameThread)).Start(frameInfo);
                        now_thread++;
                        all_thread++;
                        now_frame = end_time;
                    }
                    Dispatcher.Invoke(() => thread_text.Text = $"活跃线程/总线程:{now_thread}/{all_thread}");
                    Dispatcher.Invoke(() => progressBar.Value = 100 * 1 / all);
                }
                foreach (var file in filedirs)
                {
                    Debug.WriteLine($"开始处理第{i}张");
                    string ext = Path.GetExtension(file);
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".PNG" || ext == ".JPEG" || ext == ".JPG")
                    {
                        i++;
                        Mat mat = Cv2.ImRead(file);
                        var result = Standardize_images(ref mat);
                        FrameInfo frameInfo = new FrameInfo { mat = mat, now_frame = now_frame };
                        if (result)
                        {
                            int end_time = GetImg2frameOver(mat, now_frame);
                            new Thread(new ParameterizedThreadStart(Img2frameThread)).Start(frameInfo);
                            now_thread++;
                            all_thread++;
                            now_frame = end_time;
                        }
                        else
                        {
                            int end_time = GetOverLongImg2frame(mat, now_frame);
                            new Thread(new ParameterizedThreadStart(LongImg2frameThread)).Start(frameInfo);
                            now_thread++;
                            all_thread++;
                            now_frame = end_time;
                        }
                        Dispatcher.Invoke(() => thread_text.Text = $"活跃线程/总线程:{now_thread}/{all_thread}");
                        Dispatcher.Invoke(() => progressBar.Value = 100 * (i + 1) / all);

                    }
                    else continue;
                }
                if (btn_has_end == true)
                {
                    i++;
                    Debug.WriteLine("开始处理尾图");
                    Mat end = Cv2.ImRead("Res/end.png");
                    var result = Standardize_images(ref end);
                    FrameInfo frameInfo = new FrameInfo { mat = end, now_frame = now_frame };
                    if (result)
                    {
                        int end_time = GetImg2frameOver(end, now_frame);
                        new Thread(new ParameterizedThreadStart(Img2frameThread)).Start(frameInfo);
                        now_thread++;
                        all_thread++;
                        now_frame = end_time;
                    }
                    else
                    {
                        int end_time = GetOverLongImg2frame(end, now_frame);
                        new Thread(new ParameterizedThreadStart(LongImg2frameThread)).Start(frameInfo);
                        now_thread++;
                        all_thread++;
                        now_frame = end_time;
                    }
                    Dispatcher.Invoke(() => thread_text.Text = $"活跃线程/总线程:{now_thread}/{all_thread}");
                    Dispatcher.Invoke(() => progressBar.Value = 100);
                }
                Dispatcher.Invoke(() => progressBar.IsIndeterminate = true);
                string fps_text = "60", file_dir_text = "", bgm_dir_text = "";
                Dispatcher.Invoke(() => fps_text = fps.Text);
                Dispatcher.Invoke(() => file_dir_text = files_dir.Text);
                Dispatcher.Invoke(() => bgm_dir_text = BGM_Combo.Text);
                string ffmpeg_path = System.Environment.CurrentDirectory + @"\ffmpeg\ffmpeg.exe";
                string cache_dir = $"Cache\\{uuid}";
                while (now_thread > 0)
                {
                    Dispatcher.Invoke(() => thread_text.Text = $"活跃线程/总线程:{now_thread}/{all_thread}");
                    Thread.Sleep(100);
                }


                //合成MP4（无声）
                Dispatcher.Invoke(() => thread_text.Text = $"合成无声视频");
                System.Diagnostics.Process.Start("ffmpeg.exe", $" -y -r {fps_text} -i {cache_dir}\\%d.jpg {cache_dir}\\output.mp4").WaitForExit();
                GC.Collect();

                // 处理MP3 生成和MP4同样长的MP3文件
                if (bgm_dir_text != "" && bgm_dir_text != null)
                {
                    int mp4sec = ReadMp4During(System.Environment.CurrentDirectory + $"\\Cache\\{uuid}\\output.mp4");
                    int mp3sec = ReadMp3During(bgm_dir_text);
                    FileInfo file = new FileInfo(bgm_dir_text);
                    if (file.Exists) //可以判断源文件是否存在
                    {
                        // 这里是true的话覆盖
                        file.CopyTo($"{System.Environment.CurrentDirectory}\\Cache\\{uuid}\\1.mp3", true);
                    }
                    string concat_str = $"concat:{cache_dir}\\1.mp3";
                    if (mp4sec > mp3sec)
                    {
                        //计算mp3长度
                        for (i = 0; i < Math.Ceiling((double)mp4sec / (double)mp3sec); i++)
                        {
                            concat_str += $"|{cache_dir}\\1.mp3";
                        }
                        Dispatcher.Invoke(() => thread_text.Text = $"拼接合适长度的BGM");
                        //拼接合适长度的mp3
                        System.Diagnostics.Process.Start("ffmpeg.exe", $" -i \"{concat_str}\" -y -acodec copy {cache_dir}\\b.mp3").WaitForExit();
                        Dispatcher.Invoke(() => thread_text.Text = $"截取BGM到视频长度");
                        //截取mp3到mp4长度
                        System.Diagnostics.Process.Start("ffmpeg.exe", $" -i {cache_dir}\\b.mp3 -t {mp4sec} -y -acodec copy {cache_dir}\\c.mp3").WaitForExit();
                    }
                    else if (mp4sec < mp3sec)
                    {
                        Dispatcher.Invoke(() => thread_text.Text = $"截取BGM到视频长度");
                        //截取mp3到mp4长度
                        System.Diagnostics.Process.Start("ffmpeg.exe", $" -i {cache_dir}\\1.mp3 -y -t {mp4sec} -acodec copy {cache_dir}\\c.mp3").WaitForExit();
                    }
                    Dispatcher.Invoke(() => thread_text.Text = $"合成BGM和视频");
                    //合成mp3和mp4
                    System.Diagnostics.Process.Start("ffmpeg.exe", $" -i {cache_dir}\\c.mp3 -i {cache_dir}\\output.mp4 -y {cache_dir}\\d.mp4").WaitForExit();

                    Dispatcher.Invoke(() => thread_text.Text = $"导出带BGM的视频");
                    //导出带BGM的mp4
                    var files_dir_list = file_dir_text.Split("\\");
                    if (File.Exists($"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4"))
                    {
                        File.Delete($"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4");
                    }
                    File.Move($"./Cache/{uuid}/d.mp4", $"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4");
                }
                else
                {
                    Dispatcher.Invoke(() => thread_text.Text = $"导出视频");
                    // 导出无声MP4
                    var files_dir_list = file_dir_text.Split("\\");
                    if (File.Exists($"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4"))
                    {
                        File.Delete($"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4");
                    }
                    File.Move($"./Cache/{uuid}/output.mp4", $"Res/{files_dir_list[files_dir_list.Length - 1]}.mp4");

                }
                Dispatcher.Invoke(() => progressBar.IsIndeterminate = false);
                Dispatcher.Invoke(() => thread_text.Text = $"开始清理缓存");
                //清理缓存
                if (Directory.Exists($"./Cache/{uuid}"))
                    Directory.Delete($"./Cache/{uuid}", true);
                GC.Collect();
                Dispatcher.Invoke(() => thread_text.Text = $"完成");
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

        /// <summary>
        /// 计算长图片结束帧
        /// </summary>
        private int GetOverLongImg2frame(Mat mat, int now_frame)
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
            //移动速率 
            int sec = (dst.Height - canvas_height) / velocity;
            if (sec <= _sec)
                sec = _sec;
            else if (sec > 60) sec = 60;
            int ratio = (dst.Height - canvas_height) / (sec * f) + 1;
            int end_frame = 0;
            //长图头停顿
            for (int i = 0; i < f * _long_image_start / 1000; i++)
            {
                now_frame++;
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
            }
            //长图尾停顿
            for (int i = 0; i < f * _long_image_end / 1000; i++)
            {
                now_frame++;
            }
            return now_frame;
        }




        /// <summary>
        /// 长图片转帧
        /// </summary>
        private void LongImg2frame(Mat mat, int now_frame)
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
            if (sec <= _sec)
                sec = _sec;
            else if (sec > 60) sec = 60;
            int ratio = (dst.Height - canvas_height) / (sec * f) + 1;
            int end_frame = 0;
            //长图头停顿
            for (int i = 0; i < f * _long_image_start / 1000; i++)
            {
                now_frame++;
                Mat tmp = dst.Clone(new Rect(0, 0, canvas_width, canvas_height));
                Cv2.ImWrite($"Cache/{uuid}/{now_frame}.jpg", tmp);
                GC.Collect();
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
                Cv2.ImWrite($"Cache/{uuid}/{now_frame}.jpg", tmp);
                GC.Collect();
            }
            //长图尾停顿
            for (int i = 0; i < f * _long_image_end / 1000; i++)
            {
                now_frame++;
                Mat tmp = dst.Clone(new Rect(0, end_frame, canvas_width, canvas_height));
                Cv2.ImWrite($"Cache/{uuid}/{now_frame}.jpg", tmp);
                GC.Collect();
            }
            now_thread--;
        }

        /// <summary>
        /// 计算短图片结束帧
        /// </summary>
        private int GetImg2frameOver(Mat mat, int start_frame)
        {
            int sec = 5, f = 60;
            Dispatcher.Invoke(() => sec = duration.Text.Length > 0 ? int.Parse(duration.Text) : 5);
            Dispatcher.Invoke(() => f = fps.Text.Length > 0 ? int.Parse(fps.Text) : 60);
            for (int i = 0; i < f * sec; i++)
            {
                start_frame++;
            }
            return start_frame;
        }

        /// <summary>
        /// 短图片转帧
        /// </summary>
        private void Img2frame(Mat mat, int now_frame)
        {
            int sec = 5, f = 60;
            Dispatcher.Invoke(() => sec = duration.Text.Length > 0 ? int.Parse(duration.Text) : 5);
            Dispatcher.Invoke(() => f = fps.Text.Length > 0 ? int.Parse(fps.Text) : 60);
            for (int i = 0; i < f * sec; i++)
            {
                now_frame++;
                Cv2.ImWrite($"Cache/{uuid}/{now_frame}.jpg", mat);
                GC.Collect();
            }
            now_thread--;

        }

        struct FrameInfo
        {
            public Mat mat;
            public int now_frame;
        };

        /// <summary>
        /// 长图片转帧线程
        /// </summary>
        private void LongImg2frameThread(object frame_info)
        {
            FrameInfo frame_info1 = (FrameInfo)frame_info;
            LongImg2frame(frame_info1.mat, frame_info1.now_frame);
        }

        /// <summary>
        /// 短图片转帧线程
        /// </summary>
        private void Img2frameThread(object frame_info)
        {
            FrameInfo frame_info1 = (FrameInfo)frame_info;
            Img2frame(frame_info1.mat, frame_info1.now_frame);
        }

        private void open_bgm_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "音频文件|*.mp3|所有文件|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog.Title = "打开BGM";
            openFileDialog.Multiselect = false;
            openFileDialog.InitialDirectory = "../";
            // 显示文件对话框
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // 获取用户选择的文件路径
                //bgm_dir.Text = openFileDialog.FileName;
            }

        }
        //获取MP4视频时长
        private int ReadMp4During(string filePath)
        {
            Debug.WriteLine(filePath);
            ShellClass sc = new ShellClass();
            Folder dir = sc.NameSpace(Path.GetDirectoryName(filePath));
            FolderItem item = dir.ParseName(Path.GetFileName(filePath));
            StringBuilder sb = new StringBuilder();
            string pattern = @"\d+";
            int i = 0, hour = 0, min = 0, sec = 0;
            foreach (Match match in Regex.Matches(dir.GetDetailsOf(item, 27), pattern))
            {
                if (i == 0) hour = int.Parse(match.Value);
                else if (i == 1) min = int.Parse(match.Value);
                else if (i == 2) sec = int.Parse(match.Value);
                i++;
            }
            int all_sec = hour * 3600 + min * 60 + sec;
            Debug.WriteLine($"{filePath}->{dir.GetDetailsOf(item, 27)}合{all_sec}秒");
            return all_sec;
        }

        //获取MP3时长
        private int ReadMp3During(string filePath)
        {
            filePath = Path.GetFullPath(filePath);
            ShellClass sc = new ShellClass();
            Folder dir = sc.NameSpace(Path.GetDirectoryName(filePath));
            FolderItem item = dir.ParseName(Path.GetFileName(filePath));
            StringBuilder sb = new StringBuilder();
            string pattern = @"\d+";
            int i = 0, hour = 0, min = 0, sec = 0;
            foreach (Match match in Regex.Matches(dir.GetDetailsOf(item, 27), pattern))
            {
                if (i == 0) hour = int.Parse(match.Value);
                else if (i == 1) min = int.Parse(match.Value);
                else if (i == 2) sec = int.Parse(match.Value);
                i++;
            }
            int all_sec = hour * 3600 + min * 60 + sec;
            Debug.WriteLine($"{filePath}->{dir.GetDetailsOf(item, 27)}合{all_sec}秒");
            return all_sec;
        }
    }
}