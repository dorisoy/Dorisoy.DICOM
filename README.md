# Sinol DICOM Viewer

<div align="center">

** .NET下首款开源专业级 DICOM 医学影像查看器与 PACS 管理系统**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows-0078D6)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![fo-dicom](https://img.shields.io/badge/fo--dicom-5.x-green)](https://github.com/fo-dicom/fo-dicom)
[![License](https://img.shields.io/badge/License-Proprietary-red)]()

</div>

---

## 项目概述

DICOM Viewer 是一套基于 .NET 8 开发的专业级 DICOM 医学影像解决方案，包含桌面影像查看器和 Web API 服务器两大核心组件。支持 CT、MRI、US 等多种模态的 DICOM 图像加载、浏览、测量和导出，并提供完整的 PACS 集成能力和远程影像管理功能。

---

## 屏幕

<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/1.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/2.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/3.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/4.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/5.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/6.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/7.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/8.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/9.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/10.png?raw=true"/>
<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/11.png?raw=true"/>

## 系统架构

```
┌───────────────────────────────────────────────────────────────┐
│                         Sinol DICOM Platform                       │
├───────────────────────────────┬───────────────────────────────┤
│    Sinol.DicomViewer           │      Sinol.PACS.Server         │
│    (WPF 桌面应用)               │      (Web API 服务器)            │
│                               │                               │
│  ┌─────────────────────────┐  │  ┌─────────────────────────┐  │
│  │  Views/Pages         │  │  │  REST API Controllers  │  │
│  │  - MainPage          │  │  │  - Patients            │  │
│  │  - MprWindow         │  │  │  - Studies             │  │
│  │  - PacsQueryWindow   │  │  │  - Series/Instances    │  │
│  │  - SettingsPage      │  │  │  - WADO (Image Access) │  │
│  └─────────────────────────┘  │  └─────────────────────────┘  │
│  ┌─────────────────────────┐  │  ┌─────────────────────────┐  │
│  │  Services             │  │  │  Services              │  │
│  │  - DicomRendering    │  │  │  - DicomIndexService   │  │
│  │  - MprService        │  │  │  - DicomImageService   │  │
│  │  - PacsApiService    │  │  └─────────────────────────┘  │
│  └─────────────────────────┘  │                               │
└─────────────┬─────────────────┴───────────────────────────────┘
              │
              ▼
┌───────────────────────────────────────────────────────────────┐
│                   Sinol.DicomViewer.Core                         │
│                     (共享核心库)                                   │
│                                                                   │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌───────────┐  │
│  │  Data Models  │  │  Services     │  │  Repositories │  │  Database │  │
│  │  - Patient    │  │  - DicomLoader│  │  - Patient    │  │  - SQLite │  │
│  │  - Study      │  │  - PacsService│  │  - Report     │  │  - MySQL  │  │
│  │  - Series     │  │  - ReportSvc  │  │  - Exam       │  │  - MSSQL  │  │
│  └───────────────┘  └───────────────┘  └───────────────┘  └───────────┘  │
└───────────────────────────────────────────────────────────────┘
```

---

## 解决方案组成

### 📁 项目结构

```
Sinol.DicomViewer/
├── Sinol.DicomViewer.sln              # 主解决方案文件
├── src/
│   ├── Sinol.DicomViewer/              # WPF 桌面应用程序
│   ├── Sinol.DicomViewer.Core/         # 共享核心库
│   └── Sinol.PACS.Server/              # Web API 服务器
├── UI/
│   ├── Wpf.Ui/                         # WPF UI 控件库
│   ├── Wpf.Ui.Abstractions/            # UI 抽象层
│   └── Wpf.Ui.DependencyInjection/     # 依赖注入扩展
└── Directory.Packages.props           # 中央包管理
```

---

## 🖥️ Sinol.DicomViewer (桌面应用)

**技术栈**: .NET 8 | WPF | MVVM | Wpf.Ui | fo-dicom

### 功能特性

#### 🖼️ 影像查看
| 功能 | 说明 |
|------|------|
| 多模态支持 | CT、MRI、US、CR、DX 等多种 DICOM 模态 |
| 平滑滚动 | 鼠标滚轮快速浏览图像堆栈 |
| 窗宽窗位 | 左键拖拽交互式调节 |
| 缩放平移 | Ctrl+滚轮缩放，右键拖拽平移 |
| Cine 播放 | 基于 DICOM 元数据的动态电影播放 |
| 多帧支持 | 支持多帧 DICOM 文件播放 |

#### 📏 测量工具
| 工具 | 说明 |
|------|------|
| 距离测量 | 支持像素间距自动计算实际距离 (mm) |
| 角度测量 | 三点定义精确角度测量 |
| ROI 统计 | 感兴趣区域的 HU 值统计分析 |

#### 🌐 PACS 集成
| 功能 | 说明 |
|------|------|
| C-ECHO | DICOM 服务器连接测试 |
| C-FIND | 远程检查查询 |
| C-MOVE | 检查检索下载 |
| C-STORE | 影像上传发送 |
| Web API | RESTful API 影像访问 |

#### 📄 报告与导出
- PNG 图像导出
- PDF 报告生成 (QuestPDF)
- CSV 数据导出

### 目录结构

```
Sinol.DicomViewer/
├── Views/                      # 视图层
│   ├── MainWindow.xaml          # 主窗口
│   ├── PacsQueryWindow.xaml     # PACS 查询窗口
│   ├── MprWindow.xaml           # MPR 多平面重建窗口
│   ├── DicomTagsWindow.xaml     # DICOM 标签查看器
│   └── Pages/                   # 导航页面
│       ├── MainPage.xaml        # 影像查看主页
│       ├── PatientPage.xaml     # 患者管理
│       ├── ReportPage.xaml      # 报告管理
│       └── SettingsPage.xaml    # 系统设置
├── ViewModels/                 # 视图模型 (MVVM)
├── Services/                   # 应用服务
│   ├── DicomRenderingService   # DICOM 渲染服务
│   ├── MprService              # MPR 重建服务
│   ├── PacsApiService          # Web API 客户端
│   └── ConfigService           # 配置管理
├── Converters/                 # 值转换器
├── Helpers/                    # 工具类
└── Models/                     # 应用模型
```

---

## 🌐 Sinol.PACS.Server (Web API)

**技术栈**: .NET 8 | ASP.NET Core | fo-dicom | SixLabors.ImageSharp

### 功能特性

#### 🗃️ 影像索引
- 自动扫描指定目录的 DICOM 文件
- 内存索引快速检索
- 支持实时索引重建

#### 🖥️ REST API
| 模块 | 端点 | 说明 |
|------|------|------|
| **患者** | `GET /api/patients` | 分页查询患者列表 |
| | `GET /api/patients/{id}/studies` | 获取患者检查 |
| **检查** | `GET /api/studies` | 分页查询检查列表 |
| | `GET /api/studies/{uid}` | 获取检查详情 |
| | `GET /api/studies/{uid}/series` | 获取检查系列 |
| **系列** | `GET /api/series/{uid}` | 获取系列详情 |
| | `GET /api/series/{uid}/instances` | 获取系列实例 |
| **WADO** | `GET /api/wado/image/{uid}` | 获取渲染图像 (JPEG) |
| | `GET /api/wado/image/{uid}/png` | 获取渲染图像 (PNG) |
| | `GET /api/wado/thumbnail/{uid}` | 获取缩略图 |
| | `GET /api/wado/dicom/{uid}` | 下载原始 DICOM |
| | `GET /api/wado/metadata/{uid}` | 获取 DICOM 标签 |
| **索引** | `GET /api/index/statistics` | 索引统计信息 |
| | `POST /api/index/rebuild` | 重建索引 |

#### 🖼️ 图像处理
- 窗宽窗位参数支持
- 多帧图像访问
- 自动缩略图生成与缓存
- JPEG/PNG 格式转换

### 目录结构

```
Sinol.PACS.Server/
├── Controllers/                # API 控制器
│   ├── PatientsController.cs   # 患者 API
│   ├── StudiesController.cs    # 检查 API
│   ├── SeriesInstancesController.cs  # 系列/实例 API
│   └── WadoController.cs       # WADO 图像访问 API
├── Services/                   # 业务服务
│   ├── DicomIndexService.cs    # DICOM 文件索引服务
│   └── DicomImageService.cs    # 图像渲染服务
├── Models/                     # 数据模型
├── Program.cs                  # 应用程序入口
└── appsettings.json            # 配置文件
```

### 配置说明

```json
{
  "DicomStorage": {
    "RootPath": "E:\\Sinol\\3D.Scan\\CBCT\\DataSetes",
    "ThumbnailCachePath": "thumbnails",
    "ThumbnailSize": 128
  }
}
```

---

## 📦 Sinol.DicomViewer.Core (核心库)

**技术栈**: .NET 8 | fo-dicom | Dapper | 多数据库支持

### 模块组成

#### Data Models (数据模型)
| 模型 | 说明 |
|------|------|
| `Patient` | 患者信息 |
| `Examination` | 检查记录 |
| `Report` | 诊断报告 |
| `DicomSeries` | DICOM 系列 |
| `DicomFrame` | DICOM 帧数据 |
| `PacsNode` | PACS 节点配置 |
| `QueryResult` | 查询结果 |

#### Services (服务层)
| 服务 | 说明 |
|------|------|
| `DicomLoader` | DICOM 文件加载与解析 |
| `PacsService` | PACS 协议实现 (C-ECHO/FIND/MOVE/STORE) |
| `PatientDbService` | 患者数据管理 |
| `ReportService` | 报告生成与管理 |

#### Repositories (数据访问层)
| 仓储 | 说明 |
|------|------|
| `PatientRepository` | 患者数据 CRUD |
| `ExaminationRepository` | 检查数据 CRUD |
| `ReportRepository` | 报告数据 CRUD |

#### Database (数据库支持)
- **SQLite** - 默认本地存储
- **MySQL** - 生产环境
- **SQL Server** - 企业级部署

---

## 🚀 快速开始

### 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (WPF 应用)
- Visual Studio 2022 或 Rider (推荐)

### 安装与运行

```bash
# 1. 克隆仓库
git clone <repository-url>
cd Sinol.DicomViewer

# 2. 恢复依赖
dotnet restore

# 3. 启动 PACS Web API 服务器
cd src/Sinol.PACS.Server
dotnet run
# 服务器将在 http://localhost:5180 启动
# Swagger 文档: http://localhost:5180/swagger

# 4. 启动桌面应用 (新终端)
cd src/Sinol.DicomViewer
dotnet run
```

### 使用流程

1. **本地文件查看**
   - 点击 "打开文件夹" 选择包含 DICOM 文件的目录

2. **PACS 远程查询**
   - 菜单 → PACS 查询
   - 选择服务器类型:
     - **Sinol PACS Web API** - 本地 Web API 服务器
     - **DICOM 服务器** - 标准 PACS (C-FIND/C-MOVE)
   - 点击"测试连接"验证服务器状态
   - 输入查询条件并点击"查询"
   - 选择检查后点击"下载"

---

## 操作指南

### 鼠标操作

| 操作 | 功能 |
|------|------|
| 左键拖拽 | 调节窗宽窗位 |
| 右键拖拽 | 平移图像 |
| 滚轮 | 切换切片 |
| Ctrl + 滚轮 | 缩放图像 |
| 双击 | 重置视图 |

### 快捷键

| 按键 | 功能 |
|------|------|
| `Ctrl+O` | 打开文件夹 |
| `Space` | 播放/暂停 Cine |
| `R` | 重置窗宽窗位 |
| `M` | 测量工具 |
| `Esc` | 取消当前操作 |

---

## API 示例

### 获取检查列表

```bash
curl "http://localhost:5180/api/studies?pageIndex=0&pageSize=10"
```

### 获取渲染图像

```bash
# 获取 JPEG 图像
curl "http://localhost:5180/api/wado/image/{sopInstanceUid}" -o image.jpg

# 指定窗宽窗位
curl "http://localhost:5180/api/wado/image/{sopInstanceUid}?windowCenter=40&windowWidth=400"

# 获取缩略图
curl "http://localhost:5180/api/wado/thumbnail/{seriesInstanceUid}?size=256"
```

---

## 技术栈

| 组件 | 技术 |
|------|------|
| 运行时 | .NET 8 |
| 桌面 UI | WPF + Wpf.Ui (Fluent Design) |
| DICOM 库 | fo-dicom 5.x |
| Web 框架 | ASP.NET Core |
| 图像处理 | SixLabors.ImageSharp |
| ORM | Dapper |
| PDF 生成 | QuestPDF |
| 依赖注入 | Microsoft.Extensions.DependencyInjection |

---

## 未来计划


## 许可证

本项目为专有软件，未经授权禁止使用、复制或分发。

---

## 联系方式

<img src="https://github.com/dorisoy/Dorisoy.DICOM/blob/main/Screen/wx.jpg?raw=true"/>
