BACKGROUND
-------------
This is a version of the file example using the dotnet SDK
tools to compile [file.proto][] in a common library, build the server
and the client, and run them.

PREREQUISITES
-------------

- The [.NET Core SDK 2.1+](https://www.microsoft.com/net/core)

You can also build the solution `FileUtils.sln` using Visual Studio 2019,
but it's not a requirement.

BUILD AND RUN
-------------

- Build and run the server

  ```
  > dotnet run -p FileUtilsServer
  ```

- Build and run the client

  ```
  > dotnet run -p FileUtilsClient -- --file-path=C:\Users\sadda\Downloads\csv.csv
  ```

Tutorial
--------

You can find a more detailed tutorial about Grpc in [gRPC Basics: C#][]

[gRPC Basics: C#]:https://grpc.io/docs/languages/csharp/basics
