using Google.Protobuf;
using Grpc.Net.Client;
using GrpcFileService;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GrpcFileClient
{
    class Program
    {
        private const int ChunkSize = 1024 * 32;
        static async Task Main(string [] args)
        {
            bool works = true;
            while (works)
            {
                Console.Clear();
                Console.WriteLine("1. Pobieranie pliku");
                Console.WriteLine("2. Wysyłanie pliku");
                Console.WriteLine("0. Wyjście");
                Console.Write("Wybór: ");
                int choose; 
                if(!Int32.TryParse(Console.ReadLine(), out choose))
                {
                    Console.WriteLine("Błąd odczytu wyboru");
                    Console.ReadKey();
                    continue;
                }
                switch (choose)
                {
                    case 0:
                        works = false;
                        break;
                    case 1:
                        DownloadFileMenu();
                        break;
                    case 2:
                        await UploadFileMenuAsync();
                        break;
                    default:
                        Console.WriteLine("Podano złą liczbę");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void DownloadFileMenu()
        {
            throw new NotImplementedException();
        }

        private static async Task UploadFileMenuAsync()
        {
            var CurrentDirectory = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Files");
            string [] filePaths = Directory.GetFiles(CurrentDirectory, "*.txt", SearchOption.TopDirectoryOnly);
            for (int i = 1; i <= filePaths.Length; i++)
            {
                string file = filePaths [i-1];
                Console.WriteLine($"{i}. {file}");
            }
            Console.WriteLine("0. Anuluj");
            Console.Write("Wybór: ");

            int choose;
            if (!Int32.TryParse(Console.ReadLine(), out choose))
            {
                Console.WriteLine("Błąd odczytu wyboru");
                Console.ReadKey();
                return;
            }
            await UploadFileAsync(filePaths [choose-1]);
        }

        private static async Task UploadFileAsync(string file_path)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Uploader.UploaderClient(channel);

            Console.WriteLine("Nawiązywanie połączenia z serwerem");
            var call = client.UploadFile();

            Console.WriteLine("Wysyłanie metadanych pliku");
            await call.RequestStream.WriteAsync(new UploadFileRequest
            {
                Metadata = new FileMetadata
                {
                    FileName = Path.GetFileName(file_path)
                }
            });

            var buffer = new byte [ChunkSize];
            await using var readStream = File.OpenRead(file_path);

            while (true)
            {
                var count = await readStream.ReadAsync(buffer);
                if (count == 0)
                {
                    break;
                }

                Console.WriteLine("Wysyłanie paczki danych o długości " + count);
                await call.RequestStream.WriteAsync(new UploadFileRequest
                {
                    Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count))
                });
            }

            Console.WriteLine("Plik wysłany");
            await call.RequestStream.CompleteAsync();

            var response = await call;
            Console.WriteLine("Id wysyłania: " + response.Id);

            Console.WriteLine("Naciśnij dowolny klawisz aby powrócić do menu");
            Console.ReadKey();

        }
    }
}
