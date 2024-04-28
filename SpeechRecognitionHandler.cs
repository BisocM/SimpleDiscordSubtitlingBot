using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using System;

namespace SimpleDiscordSubtitlingBot
{
    public class SpeechRecognitionHandler
    {
        private SpeechRecognizer _recognizer;
        private PushAudioInputStream _pushStream;

        public event Action<string, string> OnTranscriptUpdated;
        public event Action<string> OnTranscriptCleared;

        public string Username { get; private set; }

        public SpeechRecognitionHandler(string subscriptionKey, string serviceRegion, string username)
        {
            var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.SetProfanity(ProfanityOption.Raw);

            Username = username;
            _pushStream = AudioInputStream.CreatePushStream();
            _recognizer = new SpeechRecognizer(speechConfig, AudioConfig.FromStreamInput(_pushStream));

            _recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Debug.WriteLine($"{Username}: {e.Result.Text}");
                    OnTranscriptUpdated?.Invoke(Username, e.Result.Text);
                }
            };

            _recognizer.Recognizing += (s, e) =>
            {
                OnTranscriptUpdated?.Invoke(Username, e.Result.Text);
            };
        }

        public void UpdateUsername(string newUsername)
        {
            Username = newUsername;
        }

        public void StartRecognition() => _recognizer.StartContinuousRecognitionAsync();

        public void StopRecognition()
        {
            _recognizer.StopContinuousRecognitionAsync().Wait();
            _pushStream.Close();
        }

        public void ProcessAudio(byte[] audioData) => _pushStream.Write(audioData);
    }
}