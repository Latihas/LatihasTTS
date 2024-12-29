# Latihas Export

## 简介

FF14的离线TTS工具，基于PaddleSpeech。ACT插件。

## 使用方法

### 配置

解压压缩包，文件结构不动，放入推荐路径`$(ActRoot)/Plugins/LatihasTTS/`中，看起来像这样：

```
$(ActRoot)/
│   ...
└── Plugins/
	│   ...
	└── LatihasTTS/
		├── TtsAssets/
		│   ├── a.ort
		│   ├── pinyin.txt
		│   ├── symbol.txt
		│   ├── v.ort
		│   └── vocab.txt
		├── libs/
		│   ├── onnxruntime.dll
		│   └── LatihasTTS.Core.dll
		└── LatihasTTS.dll
```

### 依赖

已验证的环境：`呆萌ACT`，`鲇鱼精1.3.5.3`

onnxruntime理论上会在Windows上自动安装，如果没有，请将`LatihasTTS/libs/onnxruntime.dll`复制到`$(ActRoot)/DLibs/onnxruntime.dll`

## 二次开发方法

Github有大小限制,a.ort和v.ort不在仓库中。
懒b了不想写了，就是正常的ONNX模型推理调用。
构建顺序没怎么写，可能有编译顺序错误，得多点两下构建。

## 参考文献

- https://github.com/Noisyfox/ACT.FoxTTS
- https://github.com/PaddlePaddle/PaddleSpeech