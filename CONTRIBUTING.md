# Contributing

## Setting up the environment

NET 9.0 is required to build and run the project. You can download it from the official [.NET website](https://dotnet.microsoft.com/download/dotnet/9.0).

```bash
dotnet restore
dotnet build
```

## Adding and running examples

You can run, modify and add new examples in `examples/` directory.

```bash
cd examples/
dotnet restore
dotnet build
dotnet run <exampleClass>
```

## Formatting code

This project uses [`dotnet-format`](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-format) for code formatting.

```bash
dotnet format
```

## Running tests

```bash
dotnet test
```
