dotnet clean
dotnet run --environment baseHref="/mybase/" -- build 
dotnet serve -d output --path-base mybase --default-extensions:.html