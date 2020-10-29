using System;
using System.Collections.Generic;
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

namespace LinesRecorder
{
	/// <summary>
	/// Interaction logic for WaveformControl.xaml
	/// </summary>
	public partial class WaveformControl : UserControl
	{
		readonly Polygon waveForm = new Polygon();

		private double XScale = 4;

		private RecordingManager _recording;

		public WaveformControl()
		{
			InitializeComponent();

			waveForm.Stroke = Foreground;
			waveForm.StrokeThickness = 1;
			waveForm.Fill = new SolidColorBrush(Colors.Bisque);

			canvas.Children.Add(waveForm);
		}

		internal void SetRecordingManager(RecordingManager recording)
		{
			_recording = recording;

			recording.RecordingStarted += ClearPolygon;
			recording.AudioLevelChanged += Recording_AudioLevelChanged;
		}

		private void ClearPolygon()
		{
			waveForm.Points.Clear();
		}

		private void Recording_AudioLevelChanged(float value)
		{
			if (!_recording.Recording)
				return;

			var middle = waveForm.Points.Count / 2;
			var xPos = middle * XScale;
			waveForm.Points.Insert(middle, new Point(xPos, Height * (1 - value)));
			waveForm.Points.Insert(middle, new Point(xPos, Height));
		}
	}
}
