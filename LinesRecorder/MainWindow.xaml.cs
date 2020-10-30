using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private readonly string[] _lines;
		private RecordingManager _recording;

		public event PropertyChangedEventHandler PropertyChanged;

		public int LineIndex { get; private set; }

		public int AudioLevel { get; private set; }
		public string LineText => _lines[LineIndex];
		public string RecordText => _recording.Recording ? "Stop Recording" : "Record";
		public string PlayText => _recording.Playing ? "Stop" : "Play";
		public string IndexText => (LineIndex + 1) + " / " + _lines.Length;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

			_lines = File.ReadAllLines("lines.txt").Select(l => l.Trim()).ToArray();

			_recording = new RecordingManager();

			_recording.AudioLevelChanged += l =>
			{
				AudioLevel = (int)(100 * l);
				OnPropertyChanged(nameof(AudioLevel));
			};
			_recording.RecordingStarted += () => OnPropertyChanged(nameof(RecordText));
			_recording.RecordingStopped += () => OnPropertyChanged(nameof(RecordText));

			_recording.PlaybackStarted += () => OnPropertyChanged(nameof(PlayText));
			_recording.PlaybackStopped += () => OnPropertyChanged(nameof(PlayText));

			waveFormControl.SetRecordingManager(_recording);

			SetIndex(0);
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void Prev_Click(object sender, RoutedEventArgs e)
		{
			SetIndex(LineIndex - 1);
		}

		private void Next_Click(object sender, RoutedEventArgs e)
		{
			SetIndex(LineIndex + 1);
		}
		
		private void SetIndex(int index)
		{
			LineIndex = (index + _lines.Length) % _lines.Length;

			//Tell the recorder where to save files
			var dir = LineIndex.ToString().PadLeft(4, '0') + " " + _lines[LineIndex];
			foreach (var c in System.IO.Path.GetInvalidFileNameChars())
				dir = dir.Replace(c.ToString(), "");

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			_recording.SetDirectory(dir);


			OnPropertyChanged(nameof(LineText));
			OnPropertyChanged(nameof(LineIndex));
			OnPropertyChanged(nameof(IndexText));
		}

		private void PlayToggle_Click(object sender, RoutedEventArgs e)
		{
			if (_recording.Playing)
				_recording.StopPlay();
			else
				_recording.Play();
		}

		private void RecordToggle_Click(object sender, RoutedEventArgs e)
		{
			if (_recording.Recording)
				_recording.StopRecording();
			else
				_recording.StartRecording();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Left:
					Prev_Click(this, null);
					break;
				case Key.Right:
					Next_Click(this, null);
					break;
				case Key.Down:
					if (!_recording.Recording)
						_recording.StartRecording();
					break;
				case Key.Up:
					_recording.Play();
					break;
				default:
					//Nothing
					break;
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Down:
					_recording.StopRecording();
					break;
				default:
					//Nothing
					break;
			}
		}
	}
}
