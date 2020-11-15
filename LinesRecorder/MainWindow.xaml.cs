using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
		private readonly RecordingManager _recording;
		private readonly FileSystemWatcher _fileWatcher = new FileSystemWatcher();

		private string _root;
		private string[] _lines;

		public event PropertyChangedEventHandler PropertyChanged;

		public int LineIndex { get; private set; }

		public int AudioLevel { get; private set; }
		public string LineText => WaveIn.DeviceCount == 0 ? "No audio input device detected. Restart app after connecting" : _lines == null ? "Please load a lines file" : _lines[LineIndex];
		public string RecordText => _recording.Recording ? "Stop Recording" : "Record";
		public string PlayText => _recording.Playing ? "Stop" : "Play";
		public string IndexText => _lines == null ? "" : (LineIndex + 1) + " / " + _lines.Length;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

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

			_fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
			_fileWatcher.Changed += FileWatcher_Changed;


			waveFormControl.SetRecordingManager(_recording);

			RefreshDeviceMenu();
		}

		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			for (var i = 0; i < 50; i++)
			{
				try
				{
					//Can fail if the file is still being written to
					LoadFile(e.FullPath, false);
					return;
				}
				catch (Exception)
				{
					Thread.Sleep(1);
				}
			}
		}

		private void LoadFile(string filename, bool resetToFirstLine = true)
		{
			_root = System.IO.Path.GetDirectoryName(filename);
			_lines = File.ReadAllLines(filename).Select(l => l.Trim()).ToArray();
			SetIndex(resetToFirstLine ? 0 : LineIndex);

			_fileWatcher.Path = _root;
			_fileWatcher.Filter = System.IO.Path.GetFileName(filename);
		}

		#region Menu
		private void MenuOpen_Click(object sender, RoutedEventArgs e)
		{
			_fileWatcher.EnableRaisingEvents = false;

			var openDialog = new OpenFileDialog();
			openDialog.DefaultExt = "txt";

			if (openDialog.ShowDialog() == true)
			{
				var filename = openDialog.FileName;

				LoadFile(filename);
				
				_fileWatcher.EnableRaisingEvents = true;
			}
		}

		private void MenuExit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void RefreshDeviceMenu()
		{
			DeviceMenu.Items.Clear();
			var devices = RecordingManager.GetDeviceNames();
			for (var i = 0; i < devices.Length; i++)
			{
				var localI = i;
				var item = new MenuItem
				{
					Header = devices[i],
				};
				item.Click += (object sender, RoutedEventArgs e) => { _recording.DeviceIndex = localI; RefreshDeviceMenu(); };

				if (i == _recording.DeviceIndex)
					item.FontWeight = FontWeights.Bold;

				DeviceMenu.Items.Add(item);
			}
		}
		#endregion

		#region Buttons
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

			dir = System.IO.Path.Combine(_root, dir);

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
		#endregion

		#region Keyboard input
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
		#endregion
	}
}
