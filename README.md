# CanICore
Check .net framework nuget packages are .net core compatible

## How to use

### Usage

#### Command prompt

Download latest release [CanICore](https://github.com/adelinosousa/canicore/releases/download/1.0.1/CanICore-1.0.1.zip)

```bash
CanICore -p C:\Path\To\NetFramework\Project 
```
Options
| Parameter | Description |
|----------|-------------|
| -p        | Path to .net framework project directory |
| -o        | Outputs result to a csv file |

**NOTE*** Csv file will be created in the same directory as the command tool.

#### Visual studio

 - Download latest release of [CanICore](https://github.com/adelinosousa/canicore/releases/download/vsix-1.0.0/CanICore.vsix)
 - Close all open instances of Visual Studio, then double-click the [CanICore](https://github.com/adelinosousa/canicore/releases/download/vsix-1.0.0/CanICore.vsix) to install it
 - Open visual studio and solution you want to check .net core compatibility for
 - Select *Tools* > *Can I Core*. Please note that this process could take a while. Depends how many nuget packages it needs to check
 - Result can be seen in the **output** window - (CTRL+ALT+O)
 
![](vs-extension-demo.gif)
 
### Improvements
Loading indicator for vsix

## License
[LICENSE](LICENSE)
