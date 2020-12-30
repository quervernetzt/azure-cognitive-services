using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;


namespace SpeechToTextConverter
{
    public class Program
    {
        private static readonly string PathToSln = @"xxx";
        private static readonly string SubscriptionKey = "xxx";
        private static readonly string SubscriptionRegion = "westeurope";
        private static readonly string ConverterFilePath = $@"{ PathToSln }\audio-converter\ffmpeg.exe";
        private static readonly string InputAudioFilePath = $@"{ PathToSln }\input\Recording.m4a";
        private static readonly string ConvertedAudioFilePath = $@"{ PathToSln }\output\Recording.wav";
        private static readonly string OutputPath = $@"{ PathToSln }\output\output.txt";
        private static readonly string InputLanguage = "de-DE";

        public static async Task Main()
        {
            Console.WriteLine("Starting...");
            ConvertAudioFile(InputAudioFilePath, ConvertedAudioFilePath);
            await RecognizeSpeechAsync();
            Console.WriteLine("Done...");
        }

        public static void ConvertAudioFile(string inputFilePath, string outputFilePath)
        {
            var ffmpegLibWin = ConverterFilePath;
            var process = Process.Start(ffmpegLibWin, $" -i { inputFilePath } -ac 1 -ar 16000 { outputFilePath }");
            process.WaitForExit();
        }

        public static async Task RecognizeSpeechAsync()
        {
            SpeechConfig speechConfig = SpeechConfig.FromSubscription(SubscriptionKey, SubscriptionRegion);
            speechConfig.SpeechRecognitionLanguage = InputLanguage;
            speechConfig.EnableDictation();

            using (AudioConfig audioConfig = AudioConfig.FromWavFileInput(ConvertedAudioFilePath))
            using (SpeechRecognizer recognizer = new SpeechRecognizer(speechConfig, audioConfig))
            {
                var stopRecognition = new TaskCompletionSource<int>();

                recognizer.Recognizing += (s, e) =>
                {
                    Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        File.AppendAllText(OutputPath, e.Result.Text + Environment.NewLine);
                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n    Session stopped event.");
                    stopRecognition.TrySetResult(0);
                };

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                await recognizer.StartContinuousRecognitionAsync();

                // Waits for completion. Use Task.WaitAny to keep the task rooted.
                Task.WaitAny(new[] { stopRecognition.Task });

                // Stops recognition.
                await recognizer.StopContinuousRecognitionAsync();
            }
        }
    }
}
