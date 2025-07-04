dotnet clean
dotnet run --environment ASPNETCORE_ENVIRONMENT=Production --environment BaseUrl="/mybase/" -- build 
dotnet serve -d output --path-base mybase --default-extensions:.html