using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Threading.Tasks;

namespace MusicMessage.ClassHelp
{
	public class VoiceRecorder
	{
		private SpeechRecognitionEngine recognizer;
		private MemoryStream audioStream;
		private bool isRecording;
		private DateTime recordingStartTime;

		public event Action<TimeSpan> RecordingStopped;

		public VoiceRecorder()
		{
			InitializeRecognizer();
		}

		private void InitializeRecognizer()
		{
			recognizer = new SpeechRecognitionEngine();
			recognizer.SetInputToDefaultAudioDevice();

			// Добавляем грамматику для команды остановки записи
			var grammarBuilder = new GrammarBuilder("стоп");
			var grammar = new Grammar(grammarBuilder);
			recognizer.LoadGrammar(grammar);

			recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
		}

		public async Task<MemoryStream> StartRecordingAsync()
		{
			if (isRecording) return null;

			audioStream = new MemoryStream();
			isRecording = true;
			recordingStartTime = DateTime.Now;

			recognizer.RecognizeAsync(RecognizeMode.Multiple);

			return audioStream;
		}

		public TimeSpan StopRecording()
		{
			if (!isRecording) return TimeSpan.Zero;

			recognizer.RecognizeAsyncStop();
			isRecording = false;

			var duration = DateTime.Now - recordingStartTime;
			RecordingStopped?.Invoke(duration);

			return duration;
		}

		private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			if (e.Result.Text == "стоп")
			{
				StopRecording();
			}
		}

		public void PlayAudio(Stream audioStream)
		{
			if (audioStream == null) return;

			audioStream.Position = 0;
			using (var soundPlayer = new SoundPlayer(audioStream))
			{
				soundPlayer.Play();
			}
		}
	}
}
