# Shader Decompiler

This project is a WIP utility to make RoR2 shaders decompiled by uTinyRipper usable in the Unity Editor

For now, only made to be functional on "hg" shaders, as well as "efficientblur", "fillcrop", "underwaterfog" and "ProjectorLight", because I'm not sure if those shaders come from plugins or not

## Compilation

### Debian/Ubuntu

```sh
sudo apt update && sudo apt install build-essential git clang 
git clone https://github.com/Xenos6666/ShaderDecompiler.git 
cd ShaderDecompiler 
make
```

## Usage

```sh
./shaderReconstructor <inputFile> <outputFile>
```
