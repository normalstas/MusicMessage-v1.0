using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicMessage.ClassHelp
{
	public class VoiceRecorder : IDisposable
	{
		private WaveInEvent _waveIn;
		private WaveFileWriter _writer;
		private MemoryStream _audioStream;
		private DateTime _recordingStartTime;
		private bool _isDisposed;
		public event Action<byte[]> AudioDataAvailable;

		public event Action<TimeSpan> RecordingStopped;
		public event Action<byte[]> RecordingDataAvailable;

		public bool IsRecording { get; private set; }
		public TimeSpan RecordingDuration => IsRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;

		public VoiceRecorder()
		{
			_waveIn = new WaveInEvent
			{
				WaveFormat = new WaveFormat(44100, 16, 1),
				BufferMilliseconds = 100
			};

			_waveIn.DataAvailable += OnDataAvailable;
			_waveIn.RecordingStopped += OnRecordingStopped;
		}

		public void StartRecording()
		{
			if (IsRecording || _isDisposed) return;

			_audioStream = new MemoryStream();
			_writer = new WaveFileWriter(new IgnoreDisposeStream(_audioStream), _waveIn.WaveFormat);
			_recordingStartTime = DateTime.Now;
			IsRecording = true;

			_waveIn.StartRecording();
		}

		public (byte[] AudioData, TimeSpan Duration) StopRecording()
		{
			if (!IsRecording || _isDisposed)
				return (null, TimeSpan.Zero);

			_waveIn.StopRecording();
			IsRecording = false;

			var duration = DateTime.Now - _recordingStartTime;
			return (_audioStream?.ToArray(), duration);
		}

		

		private void OnRecordingStopped(object sender, StoppedEventArgs e)
		{
			IsRecording = false;

			_writer?.Dispose();
			_writer = null;

			var duration = DateTime.Now - _recordingStartTime;
			RecordingStopped?.Invoke(duration);
		}

		public void PlayAudio(byte[] audioData)
		{
			if (audioData == null || audioData.Length == 0) return;

			try
			{
				using (var ms = new MemoryStream(audioData))
				using (var waveStream = new WaveFileReader(ms))
				using (var waveOut = new WaveOutEvent())
				{
					waveOut.Init(waveStream);
					waveOut.Play();

					while (waveOut.PlaybackState == PlaybackState.Playing)
					{
						System.Threading.Thread.Sleep(100);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка воспроизведения: {ex}");
			}
		}

		public void Dispose()
		{
			if (_isDisposed) return;

			_waveIn?.StopRecording();
			_writer?.Dispose();
			_audioStream?.Dispose();
			_waveIn?.Dispose();

			_isDisposed = true;
		}
		private void OnDataAvailable(object sender, WaveInEventArgs e)
		{
			if (!IsRecording) return;

			_writer.Write(e.Buffer, 0, e.BytesRecorded);
			_writer.Flush();

			AudioDataAvailable?.Invoke(e.Buffer);
		}

	}



	public class IgnoreDisposeStream : Stream
	{
		private readonly Stream _source;

		public IgnoreDisposeStream(Stream source) => _source = source;

		public override bool CanRead => _source.CanRead;
		public override bool CanSeek => _source.CanSeek;
		public override bool CanWrite => _source.CanWrite;
		public override long Length => _source.Length;

		public override long Position
		{
			get => _source.Position;
			set => _source.Position = value;
		}

		public override void Flush() => _source.Flush();
		public override int Read(byte[] buffer, int offset, int count) => _source.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => _source.Seek(offset, origin);
		public override void SetLength(long value) => _source.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => _source.Write(buffer, offset, count);

		protected override void Dispose(bool disposing)
		{
			// Не закрываем исходный поток
		}
	}
}