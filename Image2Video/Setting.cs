using System.Windows;

namespace Image2Video
{

    public class Setting
    {
        public int ret_width { get; set; }
        public int ret_height { get; set; }
        public int duration { get; set; }
        public int fps { get; set; }
        public int long_image_start { get; set; }
        public int long_image_end { get; set; }
        public int long_image_velocity { get; set; }
        public bool is_all { get; set; }
        public bool has_start { get; set; }
        public bool has_end { get; set; }

        public void LoadSetting()
        {
            this.ret_width = Image2Video.Settings.Default.ret_width;
            this.ret_height = Image2Video.Settings.Default.ret_height;
            this.duration = Image2Video.Settings.Default.duration;
            this.fps = Image2Video.Settings.Default.fps;
            this.long_image_start = Image2Video.Settings.Default.long_image_start;
            this.long_image_end = Image2Video.Settings.Default.long_image_end;
            this.long_image_velocity = Image2Video.Settings.Default.long_image_velocity;
            this.is_all = Image2Video.Settings.Default.is_all;
            this.has_start = Image2Video.Settings.Default.has_start;
            this.has_end = Image2Video.Settings.Default.has_end;
        }

        static public void UpdateSetting()
        {
            var _mainWindow = Application.Current.Windows
        .Cast<Window>()
        .FirstOrDefault(window => window is MainWindow) as MainWindow;
            Image2Video.Settings.Default.ret_width = int.Parse(_mainWindow.ret_width.Text);
            Image2Video.Settings.Default.ret_height = int.Parse(_mainWindow.ret_height.Text);
            Image2Video.Settings.Default.duration = int.Parse(_mainWindow.duration.Text);
            Image2Video.Settings.Default.fps = int.Parse(_mainWindow.fps.Text);
            Image2Video.Settings.Default.long_image_start = int.Parse(_mainWindow.long_image_start.Text);
            Image2Video.Settings.Default.long_image_end = int.Parse(_mainWindow.long_image_end.Text);
            Image2Video.Settings.Default.long_image_velocity = int.Parse(_mainWindow.long_image_velocity.Text);
            Image2Video.Settings.Default.is_all = (bool)_mainWindow.is_all.IsChecked;
            Image2Video.Settings.Default.has_start = (bool)_mainWindow.has_start.IsChecked;
            Image2Video.Settings.Default.has_end = (bool)_mainWindow.has_end.IsChecked;
            Image2Video.Settings.Default.Save();
        }
    }


}
