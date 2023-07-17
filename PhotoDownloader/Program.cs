using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoDownloader
{
    class Program
    {
        static event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        static async Task Main(string[] args)
        {
            string inputJson = File.ReadAllText("C:\\input.json");

            DownloaderSetting input = JsonConvert.DeserializeObject<DownloaderSetting>(inputJson);

            string sourceUrl = $"https://picsum.photos/{input.ImageWidth}/{input.ImageHeight}";

            int simultaneousDownloads = input.Parallelism;
            SemaphoreSlim semaphore = new SemaphoreSlim(simultaneousDownloads);

            List<Task> downloadTasks = new List<Task>();

            Console.WriteLine("$$$");
            Console.WriteLine($"Downloading {input.Count} images ({input.Parallelism} parallel downloads at most)");

            using (HttpClient client = new HttpClient())
            {
                int totalImages = input.Count;
                int downloadedImages = 0;
                object lockObject = new object();

                ProgressChanged += (sender, e) =>
                {
                    ShowProgress(e.Current, e.Total);
                };

                for (int i = 1; i <= input.Count; i++)
                {
                    await semaphore.WaitAsync();

                    int index = i;
                    Task downloadTask = Task.Run(async () =>
                    {
                        try
                        {
                            string outputFile = $"{input.SavePath}/{index}.jpg";

                            EnsureDirectoryExists(outputFile);

                            await DownloadPictureAsync(client, sourceUrl, outputFile);

                            //lock (lockObject)
                            //{
                            //    downloadedImages++;
                            //    ShowProgress(downloadedImages, totalImages);
                            //}

                            lock (lockObject)
                            {
                                Interlocked.Increment(ref downloadedImages);
                                OnProgressChanged(new ProgressChangedEventArgs(downloadedImages, totalImages));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error downloading picture {index}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    downloadTasks.Add(downloadTask);
                }

                //OnProgressChanged(new ProgressChangedEventArgs(downloadedImages, totalImages));

                await Task.WhenAll(downloadTasks);

            }
        }


        static void ShowProgress(int current, int total)
        {
            Console.CursorLeft = 0;
            Console.Write($"Progress: {current}/{total}");
            Console.WriteLine();
            Console.WriteLine("$$$");
            Console.CursorTop -= 2;
        }

        static async Task DownloadPictureAsync(HttpClient client, string imageUrl, string outputPath)
        {
            HttpResponseMessage response = await client.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }
        }

        static void EnsureDirectoryExists(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directoryPath);
        }

        static void OnProgressChanged(ProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(null, e);
        }
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        public int Current { get; }
        public int Total { get; }

        public ProgressChangedEventArgs(int current, int total)
        {
            Current = current;
            Total = total;
        }
    }
}
