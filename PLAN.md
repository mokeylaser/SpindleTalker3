# SpindleTalker3 Modernization Plan — High Priority Changes

## Overview

Five high-priority tasks to modernize the SpindleTalker3 codebase, executed in dependency order.
The installed SDK is .NET 9.0.304; we target .NET 8.0 (current LTS).

---

## Phase 1: Upgrade to .NET 8 (LTS)

**Why first:** Everything else depends on a modern TFM — async Channels, updated NuGet packages, and test SDK all need it.

### Files to modify

| File | Change |
|------|--------|
| `VfdControl/VfdControl.csproj` | Multi-target: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`. Update `System.IO.Ports` to 8.0.0, `MinVer` to 5.0.0. |
| `SpindleTalkerDialog/SpindleTalker2.csproj` | TFM → `net8.0-windows`. Remove duplicate `Microsoft.VisualBasic` ref. Remove `UpgradeAssistant.Analyzers`. Remove `ImportWindowsDesktopTargets`. Update `Microsoft.Windows.Compatibility` → 8.0.0. |
| `ConsoleTest/ConsoleTest.csproj` | TFM → `net8.0`. Remove `UpgradeAssistant.Analyzers`. Remove unused `System.Data.DataSetExtensions`. |
| `PowerMeterMonitor/PowerMeterMonitor.csproj` | TFM → `net8.0-windows`. Remove `UpgradeAssistant.Analyzers`. Remove `ImportWindowsDesktopTargets`. Update `Microsoft.Windows.Compatibility` → 8.0.0. |
| `.github/workflows/build.yml` | `dotnet-version: 5.0.x` → `8.0.x` (lines 16, 47). Publish path `net5.0-windows` → `net8.0-windows` (line 53). |

### Verification
- `dotnet restore && dotnet build -c Release` — zero errors across all 4 projects
- `dotnet pack` on VfdControl produces nupkg containing both `netstandard2.0` and `net8.0` libs

---

## Phase 2: Replace Closed-Source DLLs (gTrackBar.dll + LBIndustrialCtrls.dll)

**Why second:** Validates .NET 8 WinForms compatibility before the deeper communication rewrite. Independent of the serial layer.

### 2A: Replace LBIndustrialCtrls.dll → Custom AnalogMeterControl

Create a new GDI+ `AnalogMeterControl` UserControl that draws a circular gauge with:
- Configurable `Value`, `MinValue`, `MaxValue`
- Scale arc with `ScaleDivisions` / `ScaleSubDivisions` tick marks and labels
- Rotating needle with configurable `NeedleColor`
- `BodyColor`, `ScaleColor` properties
- Double-buffered rendering to prevent flicker

| File | Change |
|------|--------|
| **NEW** `SpindleTalkerDialog/UserControls/AnalogMeterControl.cs` | Custom GDI+ circular gauge control (~200 lines) |
| `SpindleTalkerDialog/UserControls/GCSMeter.Designer.cs` | Replace `LBSoft.IndustrialCtrls.Meters.LBAnalogMeter` → `AnalogMeterControl` (lines 33, 67-83, 119). Remove `Renderer` and `ViewGlass` and `MeterStyle` properties. |
| `SpindleTalkerDialog/UserControls/GCSMeter.cs` | No changes needed — property names are kept identical on the new control. |
| `SpindleTalkerDialog/SpindleTalker2.csproj` | Remove `<Reference Include="LBIndustrialCtrls">` block. |
| Delete `SpindleTalkerDialog/LBIndustrialCtrls.dll` | |

### 2B: Replace gTrackBar.dll → Standard TrackBar

| File | Change |
|------|--------|
| `SpindleTalkerDialog/MainWindow.Designer.cs` | Replace `gTrackBar.gTrackBar` → `System.Windows.Forms.TrackBar`. Map: `MaxValue`→`Maximum`, `ChangeLarge`→`LargeChange`, `ChangeSmall`→`SmallChange`, `TickInterval`→`TickFrequency`. Remove gTrackBar-specific properties (`SliderCapEnd`, `SliderSize`, `SliderWidthHigh/Low`, `UpDownShow`, `ValueAdjusted`, `ValueDivisor`, `ValueStrFormat`, `Label`, `JumpToMouse`). Fix ValueChanged event signature. |
| `SpindleTalkerDialog/MainWindow.cs` | Replace `ChangeGtrackbarColours()` method body with simple `gTrackBarSpindleSpeed.Enabled = enabled;`. Remove `gTrackBar` using/references. |
| `SpindleTalkerDialog/SpindleTalker2.csproj` | Remove `<Reference Include="gTrackBar">` block. |
| Delete `SpindleTalkerDialog/gTrackBar.dll` | |

### Verification
- Solution builds cleanly
- Launch SpindleTalkerDialog — all 6 meters render with needles, scale labels, and digital readout
- Trackbar slides, fires ValueChanged, Enable/Disable works visually
- Screenshot comparison to confirm functional parity

---

## Phase 3: Replace Thread/ManualResetEvent with async/await

**Why third:** The core communication rewrite. Depends on .NET 8 for `System.Threading.Channels` (inbox).

### Design

Replace the blocking `DoWork()` thread loop with an async `Task`-based loop:

| Old Pattern | New Pattern |
|-------------|-------------|
| `new Thread(() => DoWork()).Start()` | `_workerTask = Task.Run(() => DoWorkAsync(_cts.Token))` |
| `Queue<byte[]>` + `lock()` | `Channel<byte[]>` (lock-free, async-ready) |
| `ManualResetEvent _spindleActive` | `Channel.Reader.ReadAsync()` (blocks until data) |
| `ManualResetEvent _dataReadyToRead` | `TaskCompletionSource<bool>` per send cycle |
| `Thread.Sleep(10)` polling in ReadData | `await Task.Delay(10, ct)` |
| `_dataReadyToRead.WaitOne(500)` | `await Task.WhenAny(tcs.Task, Task.Delay(500, ct))` |
| `Queue<int> _receivedQueue` + `Thread.Sleep(50)` polling | `Channel<int>` + `ReadAsync` with timeout |

### Files to modify

| File | Change |
|------|--------|
| `VfdControl/HYmodbus.cs` | Major rewrite: Replace fields, `DoWork`→`DoWorkAsync`, `ReadData`→`ReadDataAsync`, `GetData`→`GetDataAsync`, update `Connect()`, `Disconnect()`, `SendDataAsync()`, `SendData()`. Add `using System.Threading.Channels; using System.Threading.Tasks;` |
| `VfdControl/VfdControl.csproj` | Add conditional `System.Threading.Channels` v8.0.0 package for netstandard2.0 target (inbox on net8.0). |

### Public API preserved (no breaking changes)
- `void Connect()` — unchanged signature
- `void Disconnect()` — unchanged signature
- `void SendDataAsync(byte[])` — unchanged signature (name is misleading but kept for compatibility)
- `int SendData(byte[])` — unchanged synchronous signature, bridges via `Channel<int>.ReadAsync` internally
- All events: `OnProcessPollPacket`, `OnWriteTerminalForm`, `OnWriteLog` — unchanged delegates
- All properties: `ComOpen`, `PortName`, `BaudRate`, etc. — unchanged

### Verification
- Solution builds cleanly
- ConsoleTest connects to VFD, reads registers, sets RPM, polls, disconnects — no hangs
- SpindleTalkerDialog: connect/disconnect, meters update, command builder works
- Stress test: rapid connect/disconnect cycles — no deadlocks, no orphaned threads
- Verify `ComOpen` state transitions correctly

---

## Phase 4: Add Retry Logic with Exponential Backoff

**Why fourth:** Builds directly on Phase 3's async infrastructure. Adds resilience to the communication layer.

### Design

New method `SendAndReceiveWithRetryAsync()` wraps the write/wait/read cycle:
- On timeout or CRC failure: retry up to `MaxRetries` (default: 3)
- Exponential backoff: 100ms → 200ms → 400ms (capped at `RetryMaxDelayMs`)
- Flush stale serial buffer before each retry
- After all retries exhausted: set `VFDData.ReadError = true`, return empty, continue polling loop
- Log each retry attempt via `OnWriteLog`

### Files to modify

| File | Change |
|------|--------|
| `VfdControl/HYmodbus.cs` | Add `MaxRetries`, `RetryBaseDelayMs`, `RetryMaxDelayMs` properties. Add `SendAndReceiveWithRetryAsync()` method. Integrate into `DoWorkAsync` main loop replacing the direct write/read block. |

### Verification
- Normal operation: zero retries, no performance degradation
- Simulated failure (disconnect USB adapter briefly): log shows retry attempts with backoff delays
- After MaxRetries exhaustion: `ReadError` flag set, polling continues on next cycle
- No retry on the shutdown sentinel packet `0xFF 0xFF`

---

## Phase 5: Add Unit Tests for VfdControl

**Why last:** Tests the final, stable state of the library after all refactoring is complete.

### Test Infrastructure

| File | Change |
|------|--------|
| **NEW** `VfdControl.Tests/VfdControl.Tests.csproj` | xUnit + Moq test project targeting net8.0 |
| `SpindleTalker2.sln` | Add VfdControl.Tests project |
| `VfdControl/VfdControl.csproj` | Add `InternalsVisibleTo("VfdControl.Tests")` so tests can access `internal` members (e.g., `crc16byte`) |
| `VfdControl/HYmodbus.cs` | Change `private byte[] crc16byte(...)` → `internal`. Change `private bool CRCCheck(...)` → `internal`. |

### ISerialPort Interface (Testability)

| File | Change |
|------|--------|
| **NEW** `VfdControl/ISerialPort.cs` | Interface: `Open`, `Close`, `Write`, `Read`, `BytesToRead`, `IsOpen`, `DataReceived` event |
| **NEW** `VfdControl/SerialPortWrapper.cs` | Thin wrapper delegating to `System.IO.Ports.SerialPort` |
| `VfdControl/HYmodbus.cs` | Add constructor overload accepting `ISerialPort`. In `DoWorkAsync`, use injected port or create `SerialPortWrapper`. |

### Test Classes

| Test File | What It Covers |
|-----------|----------------|
| `CRC16Tests.cs` | CRC lookup table produces correct checksums for known Modbus test vectors. Round-trip: sign then verify. |
| `VFDdataTests.cs` | `OnChanged` fires on property set. `MinRPM` calculation. `InitDataOK()` logic. `Clear()` resets to -1. |
| `RegisterValueTests.cs` | Type/description mapping for known registers. `ToValue()` scaling (intX10, intX100, intX1000). `data0`/`data1` byte extraction. `CommandLength` returns correct value. |
| `MotorControlTests.cs` | Constructor validation (invalid parity/stopBits throws). **Bug fix**: line 43 `parity > 3` → `stopBits > 3`. SetRPM frequency calculation. |
| `HYmodbusTests.cs` | `GetResponseLength` returns correct values per command byte. `ProcessReceivedPacket` via mock serial port. Retry logic: mock timeout triggers retry, CRC failure triggers retry, max retries sets ReadError. Connect/Disconnect lifecycle. |
| `SettingsHandlerTests.cs` | CSV parsing with valid/invalid column counts. |

### Bug Fix Found During Analysis
`MotorControl.cs` line 43: `if (stopBits < 0 || parity > 3)` should be `if (stopBits < 0 || stopBits > 3)`. Will be fixed and covered by a unit test.

### Verification
- `dotnet test` — all tests pass
- CI/CD build.yml already runs `dotnet test` — will pick up new project automatically

---

## Execution Sequence Summary

```
Phase 1: .NET 8 Upgrade          ← Foundation for everything
    ↓
Phase 2: Replace DLLs            ← Validates .NET 8 WinForms, independent of serial layer
    ↓
Phase 3: Async/Await Rewrite     ← Core communication modernization
    ↓
Phase 4: Retry Logic             ← Builds on Phase 3 async infrastructure
    ↓
Phase 5: Unit Tests              ← Tests final state, adds ISerialPort for testability
```

Each phase will be verified with a full build before proceeding to the next.
