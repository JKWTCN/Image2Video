using Newtonsoft.Json;
using System.IO;
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
            try
            {
                Setting tmp = JsonConvert.DeserializeObject<Setting>(File.ReadAllText("setting.json"));
                this.ret_width = tmp.ret_width;
                this.ret_height = tmp.ret_height;
                this.duration = tmp.duration;
                this.fps = tmp.fps;
                this.long_image_start = tmp.long_image_start;
                this.long_image_end = tmp.long_image_end;
                this.long_image_velocity = tmp.long_image_velocity;
                this.is_all = tmp.is_all;
                this.has_start = tmp.has_start;
                this.has_end = tmp.has_end;
            }
            catch (Exception)
            {
                this.ret_width = 1080;
                this.ret_height = 1920;
                this.duration = 5;
                this.fps = 60;
                this.long_image_start = 250;
                this.long_image_end = 250;
                this.long_image_velocity = 500;
                this.is_all = false;
                this.has_start = true;
                this.has_end = true;
            }
        }

        public void UpdateSetting()
        {
            var _mainWindow = Application.Current.Windows
        .Cast<Window>()
        .FirstOrDefault(window => window is MainWindow) as MainWindow;
            this.ret_width = int.Parse(_mainWindow.ret_width.Text);
            this.ret_height = int.Parse(_mainWindow.ret_height.Text);
            this.duration = int.Parse(_mainWindow.duration.Text);
            this.fps = int.Parse(_mainWindow.fps.Text);
            this.long_image_start = int.Parse(_mainWindow.long_image_start.Text);
            this.long_image_end = int.Parse(_mainWindow.long_image_end.Text);
            this.long_image_velocity = int.Parse(_mainWindow.long_image_velocity.Text);
            this.is_all = (bool)_mainWindow.is_all.IsChecked;
            this.has_start = (bool)_mainWindow.has_start.IsChecked;
            this.has_end = (bool)_mainWindow.has_end.IsChecked;
        }

        public void SaveSetting()
        {
            File.WriteAllText("setting.json", JsonConvert.SerializeObject(this));
        }
    }


}
