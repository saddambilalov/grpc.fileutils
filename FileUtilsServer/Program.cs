using System;
using System.IO;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using GrpcProto;
using Minio;

namespace FileUtilsServer
{
    class GrpcServiceIml : GrpcService.GrpcServiceBase
    {
        private static string BucketName => "myfiles-folder";

        public override async Task FileDownload(FileDownloadRequest request,
            IServerStreamWriter<DataChunk> responseStream,
            ServerCallContext context)
        {
            try
            {
                const int chunkSize = 512 * 1024;
                var client = GetMinioClinet();

                var stats = await client.StatObjectAsync(BucketName, request.FileName);

                long offset = 0, remaningSize = stats.Size;
                while (remaningSize > 0)
                {
                    long length = remaningSize > chunkSize ? chunkSize : remaningSize;

                    using var chunk = new MemoryStream();
                    await client.GetObjectAsync(BucketName, request.FileName, offset: offset, length: length,
                        (inputStream) =>
                    {
                        inputStream.CopyTo(chunk);
                    });

                    var data = ByteString.CopyFrom(chunk.ToArray());
                    await responseStream.WriteAsync(new DataChunk
                    {
                        Data = data,
                        Size = length
                    });

                    offset += chunkSize + 1;
                    remaningSize -= chunkSize;
                }

                //await client.GetObjectAsync(BucketName, request.FileName, (inputStream) =>
                //{
                //    byte[] bytes = new byte[chunkSize];

                //    int bufferSize;
                //    while (!context.CancellationToken.IsCancellationRequested
                //        && (bufferSize = inputStream.Read(bytes)) > 0)
                //    {
                //        var chunk = ByteString.CopyFrom(bytes, 0, bufferSize);

                //        responseStream.WriteAsync(new DataChunk
                //        {
                //            Data = chunk,
                //            Size = bufferSize
                //        }).GetAwaiter().GetResult();
                //    }
                //});
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override async Task<FileUploadResponse> FileUpLoad(IAsyncStreamReader<FileUploadRequest> requestStream,
            ServerCallContext context)
        {
            using var memoryStream = new MemoryStream();

            var fileName = Guid.NewGuid().ToString();
            while (!context.CancellationToken.IsCancellationRequested &&
                await requestStream.MoveNext())
            {
                var fileUploadRequest = requestStream.Current;
                if (fileUploadRequest.RequestCase == FileUploadRequest.RequestOneofCase.FileName)
                {
                    fileName = fileUploadRequest.FileName;
                    continue;
                }

                var chunk = fileUploadRequest.Chunk.ToByteArray();

                await memoryStream.WriteAsync(chunk);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);

            await UploadFileAsync(memoryStream, fileName);

            return new FileUploadResponse
            {
                Name = fileName
            };
        }

        private static MinioClient GetMinioClinet()
        {
            var endpoint = "127.0.0.1:9000";

            var accessKey = "Q3AM3UQ867SPQQA43P2F";
            var secretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";


            return new MinioClient(endpoint, accessKey, secretKey);
        }

        private static async Task UploadFileAsync(MemoryStream filestream, string fileName)
        {

            await GetMinioClinet().PutObjectAsync(BucketName,
                               fileName,
                                filestream,
                                filestream.Length,
                               "application/octet-stream");

            Console.WriteLine($"{fileName} is uploaded successfully");
        }
    }

    class Program
    {
        const int Port = 30051;

        public static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { GrpcService.BindService(new GrpcServiceIml()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("FileUtils server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
