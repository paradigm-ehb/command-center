using Google.Protobuf.WellKnownTypes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace paradigm_ehb.CommandCenter.WinUI.Components
{
    public sealed partial class ProgressBar : UserControl
    {
        public ProgressBar()
        {
            InitializeComponent();
            this.Loaded += ProgressBar_Loaded;
        }

        private void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateProgressBar(Progress);
        }

        private void UpdateProgressBar(double value)
        {
            if (FullWidth.ActualWidth > 0 && value > 0)
            {
                var onePiece = FullWidth.ActualWidth / 100.0;
                Usage.Width = onePiece * value;
            }else if (value > 100)
            {
                Usage.Width = 100;
            } else {
                Usage.Width = 0;
            }
        }

        private void Usage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProgressBar(Progress);
        }

        /// <summary>
        /// This function allows you to modify the value of the progress bar.
        /// </summary>
        public void modifyValue(double value)
        {
            UpdateProgressBar(value);
        }

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public static readonly DependencyProperty ProgressProperty = 
            DependencyProperty.Register(
                "Progress", 
                typeof(double), 
                typeof(ProgressBar), 
                new PropertyMetadata(0.0, OnProgressChanged));

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var progressBar = d as ProgressBar;
            progressBar?.UpdateProgressBar((double)e.NewValue);
        }
    }
}
