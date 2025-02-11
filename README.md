# Image2Video

一个简单的图片转视频工具，支持将图片转换为带有过渡效果和背景音乐的视频。

## 功能特点

- 支持多种图片格式 (PNG, JPG, JPEG)
- 自定义视频分辨率
- 自定义帧率和切换间隔
- 支持长图片滚动效果
- 支持添加背景音乐(BGM)
- 支持递归遍历子文件夹中的图片
- 支持添加开头和结尾过渡图片
- 设置自动保存

## 系统要求

- Windows 7及以上系统
- .NET 8.0 Runtime
- FFmpeg (复制到与Image2Video.exe同目录)[[FFmpeg](https://ffmpeg.org/)]

## 安装说明

1. 从Release页面下载最新版本
2. 解压到任意目录
3. 运行Image2Video.exe

## 使用说明

1. 点击"打开文件夹"选择包含图片的文件夹
2. 自定义视频参数:
   - 分辨率
   - 帧率
   - 图片切换间隔
   - 长图滚动速率和停顿时间
3. 选择是否包含子文件夹内的图片
4. 选择是否添加开头和结尾图片
5. 将BGM文件(.mp3格式)放入BGM文件夹并从下拉列表选择
6. 点击"开始"生成视频
7. 生成的视频将保存在Res文件夹中

## 主要依赖

- OpenCvSharp4
- WindowsAPICodePack
- Newtonsoft.Json
- FFmpeg

## 开发环境

- Visual Studio 2022
- .NET 8.0
- WPF

## 反馈

如有问题或建议，欢迎提交Issue。
