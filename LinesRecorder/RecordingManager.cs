﻿using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace LinesRecorder
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// https://channel9.msdn.com/coding4fun/articles/NET-Voice-Recorder
	/// </remarks>
	class RecordingManager : ISampleProvider
	{
		private WaveIn _waveIn;
		private WaveOut _waveOut;

		public bool Recording { get; set; }
		public bool Playing => _waveOut.PlaybackState == PlaybackState.Playing;
		public float AudioLevel { get; private set; }

		private readonly List<float> _audioBuffer = new List<float>();
		private int _audioBufferPlaybackPosition;

		private WaveFileWriter _waveWriter;
		private string _dir;

		public event Action<float> AudioLevelChanged;
		public event Action RecordingStarted;
		public event Action RecordingStopped;

		public event Action PlaybackStarted;
		public event Action PlaybackStopped;

		public RecordingManager()
		{
			_waveIn = new WaveIn();
			_waveIn.DataAvailable += _waveIn_DataAvailable;
			_waveIn.WaveFormat = new WaveFormat(44100, 1);

			if (WaveIn.DeviceCount > 0)
				_waveIn.StartRecording();

			_waveOut = new WaveOut();
			_waveOut.Init(this);
			_waveOut.PlaybackStopped += (s, e) => { PlaybackStopped?.Invoke(); };
		}

		public static string[] GetDeviceNames()
		{
			var res = new List<string>();
			int waveInDevices = WaveIn.DeviceCount;
			for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
			{
				WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
				res.Add(deviceInfo.ProductName);
				//Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
			}

			return res.ToArray();
		}

		public int DeviceIndex
		{
			get { return _waveIn.DeviceNumber; }
			set
			{
				//Can't do this on the main UI thread, otherwise for some reason we just stop and don't start again
				Task.Run(() =>
				{
					_waveIn.StopRecording();
					_waveIn.DeviceNumber = value;
					_waveIn.StartRecording();
				});
			}
		}

		private void _waveIn_DataAvailable(object sender, WaveInEventArgs e)
		{
			float max = 0;
			for (int index = 0; index < e.BytesRecorded; index += 2)
			{
				short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
				float sample32 = sample / 32768f;

				if (Recording)
				{
					_audioBuffer.Add(sample32);
				}

				max = Math.Max(max, Math.Abs(sample32));
			}

			if (Recording)
			{
				_waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
			}

			AudioLevel = max;
			AudioLevelChanged?.Invoke(max);
		}

		public void StartRecording()
		{
			StopPlay();

			_audioBuffer.Clear();
			_audioBufferPlaybackPosition = 0;

			var fileName = Path.Combine(_dir, Directory.GetFiles(_dir).Length.ToString().PadLeft(4, '0') + ".wav");

			_waveWriter = new WaveFileWriter(fileName, _waveIn.WaveFormat);

			Recording = true;
			RecordingStarted?.Invoke();
		}

		public void StopRecording()
		{
			Recording = false;
			RecordingStopped?.Invoke();

			_waveWriter.Close();
			_waveWriter = null;
		}

		internal void SetDirectory(string dir)
		{
			_dir = dir;
		}

		public void Play()
		{
			_waveOut.Stop();
			_audioBufferPlaybackPosition = 0;
			_waveOut.Play();

			PlaybackStarted?.Invoke();
		}

		public void StopPlay()
		{
			_waveOut.Stop();
		}

		#region ISampleProvider

		WaveFormat ISampleProvider.WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

		int ISampleProvider.Read(float[] buffer, int offset, int count)
		{
			int samples = 0;
			for (int i = 0; i < count && _audioBufferPlaybackPosition + i < _audioBuffer.Count; i++)
			{
				buffer[i + offset] = _audioBuffer[_audioBufferPlaybackPosition + i];
				samples++;
			}

			_audioBufferPlaybackPosition += samples;

			return samples;
		}

		#endregion
	}
}
