using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Json.Net;
using Newtonsoft.Json.Linq;
using System.Text;

namespace GrpcFileService.Services
{
    public class UploadService : Uploader.UploaderBase
    {

        public override async Task<UploadFileResponse> UploadFile(IAsyncStreamReader<UploadFileRequest> requestStream, ServerCallContext context)
        {
            var FilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            var uploadId = Path.GetRandomFileName();
            var uploadPath = Path.Combine(FilesDirectory, uploadId);
            Directory.CreateDirectory(uploadPath);
            await DownloadBinary(requestStream, uploadPath);
            ConvertBinaryToFile(FilesDirectory, uploadPath);
            return new UploadFileResponse { Id = uploadId };
        }

        private static void ConvertBinaryToFile(string FilesDirectory, string uploadPath)
        {
            var metadata_json = File.ReadAllText(Path.Combine(uploadPath, "metadata.json"));
            string file_name = JObject.Parse(metadata_json).GetValue("fileName").Value<string>();

            byte [] fileBytes = File.ReadAllBytes(Path.Combine(uploadPath, "data.bin"));

            File.WriteAllBytes(Path.Combine(FilesDirectory, file_name), fileBytes);
        }

        private static async Task DownloadBinary(IAsyncStreamReader<UploadFileRequest> requestStream, string uploadPath)
        {
            await using (var writeStream = File.Create(Path.Combine(uploadPath, "data.bin")))
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    if (message.Metadata != null)
                    {
                        await File.WriteAllTextAsync(Path.Combine(uploadPath, "metadata.json"), message.Metadata.ToString());
                    }
                    if (message.Data != null)
                    {
                        await writeStream.WriteAsync(message.Data.Memory);
                    }
                }
            }
        }
    }
}
