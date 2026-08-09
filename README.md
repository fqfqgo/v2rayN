# v2rayN · fqfqgo fork

基于 [2dust/v2rayN](https://github.com/2dust/v2rayN) 的个人维护版本，支持 Windows、Linux 和 macOS，以及 [Xray](https://github.com/XTLS/Xray-core)、[sing-box](https://github.com/SagerNet/sing-box) 等核心。

[![Release](https://img.shields.io/github/v/release/fqfqgo/v2rayN?logo=github&label=Release)](https://github.com/fqfqgo/v2rayN/releases)
[![Downloads](https://img.shields.io/github/downloads/fqfqgo/v2rayN/latest/total?logo=github&label=Downloads)](https://github.com/fqfqgo/v2rayN/releases)
[![GPG Signed](https://img.shields.io/badge/GPG-signed-4B32C3?logo=gnuprivacyguard)](https://github.com/fqfqgo/v2rayN/releases)

> [!IMPORTANT]
> 当前维护基线固定为 **7.24.6**。这是紧急安全更新基线，请勿从本仓库的旧版构建降级；下载后应同时校验对应的 GPG 签名。

## 下载

仅从当前 fork 的发布页下载：

https://github.com/fqfqgo/v2rayN/releases

每个发布资产都应同时提供同名 `.sig` 文件，发布页还应包含 `v2rayN-public-key.asc`。不要只校验压缩包能否解压，也不要信任第三方镜像重新打包的文件。

## GPG 与资产校验

导入发布页中的公钥，并核对指纹：

```text
7694 5E9F 3E9A 168F 8070 F195 805D 661C
134D FAF6 8903 C199 463C 31E5 AE90 3AE0
```

然后校验下载资产，例如：

```shell
gpg --import v2rayN-public-key.asc
gpg --verify v2rayN-windows-64.zip.sig v2rayN-windows-64.zip
```

只有在签名有效且签名公钥指纹与上方一致时才应使用该资产。`.sig` 必须与被校验文件来自同一个发布版本。

## 支持平台

| 平台 | x64 | x86 | arm64 | riscv64 | loong64 |
| --- | --- | --- | --- | --- | --- |
| Windows | ✅ | ✅ | ✅ | - | - |
| Linux | ✅ | - | ✅ | ✅ | ✅ |
| macOS | ✅ | - | ✅ | - | - |

最低系统要求：[发布文件介绍](https://github.com/2dust/v2rayN/wiki/Release-files-introduction)
