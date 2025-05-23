# HandleIt

HandleIt is a Windows widget that monitors a process' GDI (Graphics Device Interface) handle count and alerts users when it approaches the Windows system limit. This widget is useful for users who need to track GDI handle leaks without sacrificing screen real estate to more comprehensive diagnostic tools.

## Purpose

Windows has a per-process limit on GDI handles, and when an application reaches this limit, the application can crash, exhibit unexpected behavior, or cause other applications to crash (in cases where the per-process limit was increased.)

HandleIt helps prevent these issues by:

- Monitoring the GDI handle count of a specified process in real time
- Providing visual alerts when handle count approaches critical levels
- Allowing customizable warning thresholds
- Running unobtrusively in a small, configurable widget

## Features

- Real-time (polled) GDI handle monitoring
- Customizable process selection
- Adjustable polling rate for handle count updates
- Configurable warning threshold (default 90%)
- Customizable UI appearance:
  - Light/Dark/System/Custom themes
  - Adjustable font settings
  - Configurable widget size
  - Always-on-top option
  - Position locking
  - Draggable widget

## Configuration

The application uses an INI file (`HandleIt.ini`) for storing settings, including:

### Widget Behavior

- Always on top
- Position locking
- Draggable widget

### Display Settings
- Theme mode (System/Light/Dark/Custom)
- Widget dimensions
- Font family and size
- Custom colors (for custom theme)

### Process Settings

- Target process name (or pid)
- Polling rate (milliseconds)
- Warning threshold (%)

## System Requirements

- Windows 10 22H2 (tested) / Windows 11 24H2 (tested)
- [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Administrative privileges may be required for monitoring certain processes

## Installation

1. Download the latest release
2. Extract the files to your preferred location
3. Run `HandleIt.exe`

## Basic Usage

1. Launch the widget. The widget appears above the taskbar clock by default.
2. Right click the widget and open the Settings window.
3. In the Process settings, change the Process Name to your process and click Apply & Close.
4. The widget will display the current GDI handle count.
5. When the warning threshold is reached, the widget will show a messagebox.

## License

MPL 2.0
