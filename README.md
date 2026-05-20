# White Knuckle Multiplayer Mod
### Made by Monksilly Team

## What is this?
This is a fully fledged multiplayer solution for **White Knuckle** as a C# BepInEx mod.

## Features
- Real-time player interactions
- Seamless game sessions
- Multi-platform support

## Installation
1. Clone the repository:
   ```sh
   git clone https://github.com/monksilly/white-knuckle-multiplayer.git
   ```
2. Navigate to the project directory:
   ```sh
   cd white-knuckle-multiplayer
   ```

## Building the Project
Before building, a `Directory.Build.props.user` file should be created in the root directory with the following content:

```xml
<Project>
  <PropertyGroup>
    <WhiteKnuckleDir>C:\Path\To\White Knuckle\</WhiteKnuckleDir>
    <CopyToBepInExPluginsDir>true</CopyToBepInExPluginsDir>
  </PropertyGroup>
</Project>
```

To build the mod, run:
```sh
dotnet publish -c Release
```

## Running the Project
1. Copy the built mod and all its DLL files to your BepInEx plugins directory.
2. Start **White Knuckle**.

## Contributing
Contributions are welcome! Please fork the repository, create your feature branch and create a pull request.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.