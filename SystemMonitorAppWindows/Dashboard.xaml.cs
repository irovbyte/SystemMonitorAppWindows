using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Microsoft.VisualBasic.Devices;

namespace SystemMonitorAppWindows
{
    public partial class Dashboard : Window
    {
        private PerformanceCounter gpuCounter;
        private PerformanceCounter diskCounter;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramAvailableCounter;
        private DispatcherTimer timer;

        private double totalRamMb;

        public Dashboard()
        {
            InitializeComponent();

            // окно стартует прозрачным
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            // GPU (если доступно)
            gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "engtype_3D");

            // Disk
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

            // Total RAM as double
            var ci = new ComputerInfo();
            totalRamMb = ci.TotalPhysicalMemory / (1024.0 * 1024.0);

            // CPU & RAM counters
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramAvailableCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Timer — обновление каждые 300 мс
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Tick += UpdateStats;

            _ = cpuCounter.NextValue(); // warm-up
            timer.Start();
        }

        private void UpdateStats(object sender, EventArgs e)
        {
            // GPU
            double gpuUsage = SafeRead(gpuCounter);
            AnimateBar(GpuProgressBar, gpuUsage);
            GpuText.Text = $"{gpuUsage:F1}%";

            // Disk
            double diskUsage = SafeRead(diskCounter);
            AnimateBar(DiskProgressBar, diskUsage);
            DiskText.Text = $"{diskUsage:F1}%";

            // CPU
            double cpuUsage = SafeRead(cpuCounter);
            AnimateBar(CpuProgressBar, cpuUsage);
            CpuText.Text = $"{cpuUsage:F1}%";

            // RAM: used = 100 - (available/total * 100)
            double availableMb = SafeRead(ramAvailableCounter);
            double usedPercent = 100.0 - (availableMb / totalRamMb * 100.0);
            usedPercent = Clamp01To100(usedPercent);

            AnimateBar(RamProgressBar, usedPercent);
            RamText.Text = $"{usedPercent:F1}%";
        }

        private double SafeRead(PerformanceCounter counter)
        {
            try { return counter.NextValue(); }
            catch { return 0.0; }
        }

        private double Clamp01To100(double v)
        {
            if (v < 0.0) return 0.0;
            if (v > 100.0) return 100.0;
            return v;
        }

        private void AnimateBar(System.Windows.Controls.ProgressBar bar, double targetValue)
        {
            var anim = new DoubleAnimation
            {
                From = bar.Value,
                To = Clamp01To100(targetValue),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            bar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, anim);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation
            {
                From = this.Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, _) => this.Close(); // закрыть после анимации
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => this.WindowState = WindowState.Minimized;

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}