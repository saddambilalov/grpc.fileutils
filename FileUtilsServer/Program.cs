using System;
using System.IO;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcProto;

namespace FileUtilsServer
{
  class GrpcServiceIml : GrpcService.GrpcServiceBase
  {
    public override async Task<FileUploadResponse> FileUpLoad(IAsyncStreamReader<FileUploadRequest> requestStream,
        ServerCallContext context)
    {

      var fileName = string.Empty;
      uint numberOfChunks = 1;
      
      var dir = AppDomain.CurrentDomain.BaseDirectory;
      var tempFilePath = Path.Combine(dir, Guid.NewGuid().ToString());

      using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
      {
        while (await requestStream.MoveNext())
        {
          var fileUploadRequest = requestStream.Current;
          if (fileUploadRequest.RequestCase == FileUploadRequest.RequestOneofCase.FileName)
          {
            fileName = fileUploadRequest.FileName;
            continue;
          }

          await fs.WriteAsync(fileUploadRequest.Chunk.ToByteArray());
          numberOfChunks++;
        }
      }

      new FileInfo(tempFilePath).MoveTo(Path.Combine(dir, fileName), overwrite: true);
      Console.WriteLine($"The number of chunks received: {numberOfChunks}");

      return new FileUploadResponse
      {
        Name = fileName,
        NumberOfChunks = numberOfChunks
      };
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
