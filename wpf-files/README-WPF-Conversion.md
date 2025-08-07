# WPF Conversion Instructions

To convert this console application to WPF on Windows:

## 1. Modify the project file

Replace the content of `SimpleSerialToApi.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.IO.Ports" Version="9.0.8" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.8" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.8" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.8" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.8" />
  </ItemGroup>

</Project>
```

## 2. Replace Program.cs

Delete `Program.cs` and copy the WPF files from the `wpf-files` folder:
- Copy `App.xaml` to the root of the project
- Copy `App.xaml.cs` to the root of the project  
- Copy `MainWindow.xaml` to the root of the project
- Copy `MainWindow.xaml.cs` to the root of the project

## 3. Build and run

```bash
dotnet build
dotnet run
```

The WPF application should now launch with a window displaying "SimpleSerialToApi - Step 01 Complete".