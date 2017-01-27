del .\bin\Release\SharpReverseProxy.*
dotnet pack -c release
nuget push .\bin\Release\SharpReverseProxy.* -Source https://www.nuget.org