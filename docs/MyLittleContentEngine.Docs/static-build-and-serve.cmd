dotnet clean
dotnet run --environment BaseUrl="/mybase/" -- build 
dotnet serve -d output --path-base mybase --default-extensions:.html