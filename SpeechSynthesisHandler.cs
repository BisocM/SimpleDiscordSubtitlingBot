using Microsoft.CognitiveServices.Speech;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using System.IO;
using NAudio.Wave;

namespace SimpleDiscordSubtitlingBot
{
    internal class SpeechSynthesisHandler
    {
        private readonly SpeechConfig speechConfig;
        private readonly VoiceNextConnection connection;

        public SpeechSynthesisHandler(string subscriptionKey, string region, string voiceName, VoiceNextConnection connection)
        {
            speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            speechConfig.SpeechSynthesisVoiceName = voiceName;
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw48Khz16BitMonoPcm);
            this.connection = connection;
        }

        //TODO: Cannot figure out why the audio cannot transmit into Discord properly...
        //It should be converting to the accurate S16LE format, but it's not working, and instead results in distorted audio.
        public async Task SpeakTextAsync(string text)
        {
            using var synthesizer = new SpeechSynthesizer(speechConfig);

            //Synthesize the speech to raw audio data
            using var result = await synthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                var audioData = result.AudioData;

                //Prepare the input format (Synthesizer's output format)
                var inputFormat = new WaveFormat(16000, 16, 1); //Mono, 16-bit, 16kHz
                using var memStream = new MemoryStream(audioData);
                using var inputStream = new RawSourceWaveStream(memStream, inputFormat);

                //Stereo, 16-bit, 48kHz
                var acmFormat = new WaveFormat(48000, 16, 2);
                using var resampler = new WaveFormatConversionStream(acmFormat, inputStream);

                //Send the resampled data to Discord
                using var discordStream = connection.GetTransmitSink();
                byte[] buffer = new byte[1920]; //Buffer size based on Discord's frame duration of 20ms for 48kHz
                int bytesRead;
                while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await discordStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new Exception($"Speech synthesis canceled: {cancellation.Reason}");
            }
        }

        internal async Task HandleMessage(DiscordClient sender, MessageCreateEventArgs args)
        {
            if (args.Message.Author.Id == BotUser.Instance.SelectedUser.Id)
                await SpeakTextAsync(args.Message.Content);
        }
    }
}