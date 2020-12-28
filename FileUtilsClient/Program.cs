using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cocona;
using Google.Protobuf;
using Grpc.Core;
using GrpcProto;

namespace FileUtilsClient
{
    class Program
    {
        static void Main(string[] args) => CoconaApp.Run<Program>(args);

        [Command(Description = "This is a sample GRPC file upload application")]
        public async Task FileUpLoadAsync([Option(Description = "Path for uploading the file")] string filePath)
        {
            Channel channel = new Channel("127.0.0.1:30051", ChannelCredentials.Insecure);
            var cts = new CancellationTokenSource();

            try
            {
                var fileName = Path.GetFileName(filePath);

                var client = new GrpcService.GrpcServiceClient(channel);

                //using (var stream = client.FileUpLoad(cancellationToken: cts.Token))
                //{
                //    await stream.RequestStream.WriteAsync(new FileUploadRequest
                //    {
                //        FileName = fileName
                //    });

                //    using FileStream inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                //    int chunkSize = 512 * 1024; //500 KB
                //    byte[] bytes = new byte[chunkSize];
                //    int numberOfChunks = 1;

                //    int size;
                //    while ((size = inputStream.Read(bytes)) > 0)
                //    {
                //        var chunk = ByteString.CopyFrom(bytes, 0, size);

                //        await stream.RequestStream.WriteAsync(new FileUploadRequest
                //        {
                //            Chunk = chunk
                //        });

                //        await Task.Delay(millisecondsDelay: 1);
                //        numberOfChunks++;
                //    }

                //    Console.WriteLine($"The number of chunks sent: {numberOfChunks}");
                //    await stream.RequestStream.CompleteAsync();
                //}

                using AsyncServerStreamingCall<DataChunk> streaming = client.FileDownload(new FileDownloadRequest
                {
                    FileName = fileName
                }, cancellationToken: cts.Token);
                try
                {
                    using var memoryStream = new MemoryStream();

                    while (await streaming.ResponseStream.MoveNext(cancellationToken: cts.Token))
                    {
                        var dataChunk = streaming.ResponseStream.Current;
                        var chunk = dataChunk.ToByteArray();
                        await memoryStream.WriteAsync(chunk);
                    }
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                    await memoryStream.CopyToAsync(fs);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine("Stream cancelled.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                cts.Cancel();
            }

            await channel.ShutdownAsync();
        }
    }
}
