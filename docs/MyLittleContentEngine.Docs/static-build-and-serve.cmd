dotnet clean
dotnet run --environment ASPNETCORE_ENVIRONMENT=Production -- build "/mybase/"
dotnet serve -d output --path-base mybase --default-extensions:.html