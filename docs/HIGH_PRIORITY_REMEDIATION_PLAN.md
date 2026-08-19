# High-Priority Remediation Plan

Status: In progress
Scope: Planning only; no production-code changes are included with this document.

## Objective

Resolve the release-blocking reliability issues identified in the August 2026 review while keeping
each change set small enough to implement, test, and revert independently.

The work is intentionally split into separate batches. Complete one batch at a time unless the next
batch explicitly depends on it.

## Priority and Delivery Order

| Order | Batch | Primary risk addressed | Size | Depends on |
|---|---|---|---|---|
| 0 | Regression baseline | Changes cannot be verified safely | S | None |
| 1 | Linux USB event and watcher lifecycle | Missed events and orphaned processes | M | Batch 0 |
| 2 | Configuration and Settings safety | Corrupt configuration and UI crashes | M | Batch 0 |
| 3 | Safe DDC process execution | Hung or accumulating `ddcutil` processes | M | Batch 1 |
| 4 | Stable monitor identity | Rules target stale or incorrect monitors | L | Batches 1–3 |
| 5 | Follow-up concurrency and performance | Reordering, memory growth, and UI stalls | M | Batches 1–3 |

Recommended first delivery: Batches 0 and 1. They remove the most immediate Linux correctness and
resource-lifecycle risks without changing the configuration schema.

## Rules for the Implementing Agent

- [ ] Read `.github/copilot-instructions.md`, `docs/COMMENT_STYLE_GUIDE.md`, and
      `docs/NAMING_CONVENTION.md` before editing code.
- [ ] Check `git status --short` and preserve unrelated user changes.
- [ ] Implement only one numbered batch per commit or pull request.
- [ ] Add or update automated tests before marking a batch complete.
- [ ] Run the verification commands listed in that batch.
- [ ] Update this checklist and record any intentional scope changes.
- [ ] Do not mark an item complete when only the happy path has been tested.

## Batch 0: Establish a Regression Baseline

Goal: Create a small test project that can exercise parsing, persistence, cancellation, and lifecycle
logic without launching the Avalonia UI or accessing physical hardware.

### Tasks

- [x] Add a test project to the solution.
- [x] Add test fixtures for representative `udevadm monitor --udev --property` output.
- [x] Add a fake `IUsbWatcher` that records `Start`, `Stop`, and `Dispose` calls.
- [x] Add a fake `IMonitorController` that can delay, fail, and record concurrent calls.
- [x] Add temporary-directory helpers for `ConfigStore` tests.
- [x] Confirm the existing project still builds without analyzer errors being promoted unexpectedly.

### Acceptance Criteria

- [x] Tests run without a display server, USB device, monitor, WMI, or `ddcutil`.
- [x] `dotnet test --no-restore` succeeds on Linux.
- [x] `dotnet build --no-restore` succeeds with zero compiler warnings.

## Batch 1: Fix Linux USB Events and Watcher Lifecycle

Goal: Deliver every Linux USB event exactly once and deterministically stop the watcher when
monitoring stops or the application exits.

Relevant files:

- `Linux/LinuxUsbWatcher.cs`
- `ViewModels/MainWindowViewModel.cs`
- `App.axaml.cs`
- `Abstractions/IUsbWatcher.cs`

### Event Parsing Tasks

- [x] Extract event-block parsing from the live process-reading loop so it can be tested separately.
- [x] Treat a blank line as the end of the current `udevadm` event.
- [x] Flush the current event when a new header arrives, but do not emit the same block twice.
- [x] Flush a complete final event when stdout reaches EOF.
- [x] Ignore the startup banner and malformed property lines safely.
- [x] Verify add and remove events preserve `DEVPATH`, VID, PID, serial, and device type filtering.

### Lifecycle Tasks

- [x] Make the application explicitly own and dispose `MainWindowViewModel` or its watcher.
- [x] Stop and dispose the watcher from the desktop lifetime exit path before dispatcher teardown.
- [x] Cancel and dispose `_monitoringToggleCts` during shutdown.
- [x] Prevent initialization from starting a watcher after shutdown has begun.
- [x] Make `LinuxUsbWatcher.Start` exception-safe: publish `_process`, `_cts`, and `_readTask` fields
      only after startup succeeds.
- [x] Make `Stop` safe when the child process never started or already exited.
- [x] Drain or redirect stderr so the child process cannot block on a full error pipe.
- [x] Clear stale `_knownDevices` state at the appropriate restart boundary.

### Required Tests

- [x] One add event followed by a blank line is emitted immediately.
- [x] Add and remove events are not delayed until the following header.
- [x] EOF flushes one complete event and ignores an incomplete event.
- [x] Calling `Start`, `Stop`, and `Dispose` repeatedly is safe.
- [x] A simulated process-start failure does not poison the next `Start` attempt.
- [x] Application exit disposes the watcher exactly once.
- [x] Shutdown racing with initialization does not restart monitoring.

### Acceptance Criteria

- [ ] No `udevadm monitor` process remains after a normal application exit.
- [ ] Plugging or unplugging one device triggers its matching rule without requiring another event.
- [ ] Monitoring can be enabled and disabled repeatedly without exceptions or stale events.
- [x] Batch 0 and Batch 1 tests pass.

## Batch 2: Make Configuration and Settings Failure-Safe

Goal: Preserve the previous valid configuration when a save is interrupted, and prevent Settings
I/O failures from escaping through the generated asynchronous command.

Relevant files:

- `Config/ConfigStore.cs`
- `ViewModels/SettingsWindowViewModel.cs`
- `Views/SettingsWindow.axaml.cs`
- `Properties/Resources.resx`
- `Properties/Resources.zh-Hant.resx`

### Atomic Save Tasks

- [ ] Serialize to a uniquely named temporary file in the same directory as `config.json`.
- [ ] Flush and close the temporary file before replacing the destination.
- [ ] Atomically replace or overwrite the destination only after serialization succeeds.
- [ ] Remove the temporary file after cancellation or failure.
- [ ] Preserve the previous valid file when serialization, flushing, or replacement fails.
- [ ] Serialize an immutable snapshot so caller mutations cannot alter an in-progress save.
- [ ] Document that the current `SemaphoreSlim` protects only one `ConfigStore` instance.
- [ ] Decide whether to enforce a single PortPilot process or add an interprocess save lock.

### Settings Tasks

- [ ] Disable Save until asynchronous Settings initialization completes.
- [ ] Catch JSON, I/O, permission, and cancellation failures in `SaveAsync`.
- [ ] Keep the Settings window open after a failed save.
- [ ] Show a localized failure message instead of allowing the exception to reach the UI context.
- [ ] Add matching English and Traditional Chinese resource keys.
- [ ] Avoid restarting the application unless the configuration save completed successfully.

### Required Tests

- [ ] A successful save produces valid JSON that can be loaded immediately.
- [ ] A canceled save leaves the previous file byte-for-byte unchanged.
- [ ] A failed replacement leaves the previous file loadable and removes the temporary file.
- [ ] Concurrent saves through one store produce a complete last-writer result.
- [ ] Settings reports a corrupt-file or permission failure without closing or throwing.
- [ ] English and Traditional Chinese resource files contain identical keys.

### Acceptance Criteria

- [ ] No failure path truncates the last known-good configuration.
- [ ] No Settings save exception reaches the dispatcher as an unhandled exception.
- [ ] Restart is offered only after a confirmed language save.
- [ ] Batch 0 and Batch 2 tests pass.

## Batch 3: Bound and Serialize DDC Process Execution

Goal: Ensure every Linux DDC command finishes, fails, or is terminated within a bounded period, and
ensure rule processing cannot launch conflicting commands for the same monitor.

Relevant files:

- `Linux/LinuxMonitorController.cs`
- `ViewModels/MainWindowViewModel.cs`
- `Abstractions/IMonitorController.cs`

### Process Tasks

- [ ] Build arguments with `ProcessStartInfo.ArgumentList` instead of a combined argument string.
- [ ] Validate Linux runtime bus addresses before passing them to `ddcutil`.
- [ ] Add a configurable, conservative timeout for detect and set operations.
- [ ] On cancellation or timeout, kill the entire child process tree when supported.
- [ ] Await process exit and both redirected stream readers during cleanup.
- [ ] Preserve stderr and exit code in a typed failure result or exception.
- [ ] Do not swallow `SetInputSourceAsync` failures inside `LinuxMonitorController`.
- [ ] Make the caller display failure rather than `Command sent` or `Rule applied`.

### Concurrency Tasks

- [ ] Serialize DDC operations per runtime monitor address.
- [ ] Decide and document whether a newer event cancels or queues behind an older event.
- [ ] Coalesce redundant requests that target the same monitor and input source.
- [ ] Ensure the synchronization primitive is disposed during application shutdown.

### Required Tests

- [ ] A successful fake process returns stdout and exit code zero.
- [ ] A nonzero exit returns stderr to the caller.
- [ ] A hung fake process is killed after timeout.
- [ ] Cancellation kills the child and leaves no process or reader task running.
- [ ] Two commands for one monitor never execute concurrently.
- [ ] Commands for different monitors follow the documented concurrency policy.
- [ ] UI status never reports success after a failed DDC command.

### Acceptance Criteria

- [ ] No test can leave a child process behind.
- [ ] Every DDC call has a bounded completion path.
- [ ] Rapid USB events cannot create an unbounded number of `ddcutil` processes.
- [ ] Batch 0, Batch 1, and Batch 3 tests pass.

## Batch 4: Introduce Stable Monitor Identity

Goal: Persist a hardware-oriented identity and resolve it to the current Windows handle or Linux bus
at execution time.

This is the largest batch and should be delivered separately because it changes the configuration
model and requires migration behavior.

Relevant files:

- `Abstractions/Models.cs`
- `Abstractions/IMonitorController.cs`
- `Config/AppConfig.cs`
- `Windows/WinMonitorController.cs`
- `Linux/LinuxMonitorController.cs`
- `ViewModels/MainWindowViewModel.cs`

### Design Tasks

- [ ] Separate stable identity from runtime address in `MonitorInfo`.
- [ ] Define a stable fingerprint based on the best available manufacturer, model, serial, and EDID
      data; do not use `HMONITOR`, physical index, or I²C bus as persisted identity.
- [ ] Define how duplicate monitors without unique serial numbers are represented and rebound.
- [ ] Resolve the stable identity to the current HMONITOR/physical index or I²C bus immediately
      before executing a command.
- [ ] Return an explicit not-found or ambiguous-monitor failure instead of silent success.
- [ ] Re-resolve monitors after Windows display changes and Linux topology changes.

### Migration Tasks

- [ ] Add an explicit configuration schema version.
- [ ] Continue reading legacy `monitorId` values without silently mapping them to a different device.
- [ ] Mark unresolved legacy rules as requiring user rebind.
- [ ] Preserve VID, PID, and input-source actions during migration.
- [ ] Save the new schema only after a user-confirmed or unambiguous migration.
- [ ] Add localized UI text for unresolved or ambiguous monitor bindings.

### Required Tests

- [ ] A runtime handle or bus change still resolves to the same stable monitor.
- [ ] A missing monitor returns a visible failure.
- [ ] Duplicate identical monitors do not bind arbitrarily.
- [ ] Legacy configuration loads without data loss.
- [ ] Unresolvable legacy rules remain visible and do not execute against another monitor.
- [ ] A migrated configuration round-trips through save and load.

### Acceptance Criteria

- [ ] No persisted rule contains an HMONITOR value, physical-monitor index, or Linux bus number as
      its sole identity.
- [ ] Topology changes cannot silently redirect a rule to another monitor.
- [ ] Existing users receive a safe migration or an explicit rebind request.
- [ ] All prior batch tests continue to pass.

## Batch 5: Follow-Up Concurrency and Performance

Goal: Complete the medium-priority work that interacts with the high-priority fixes.

### Tasks

- [ ] Replace monitoring toggle fire-and-forget callbacks with one serialized, cancelable state
      transition pipeline.
- [ ] Persist the actual final watcher state, not the requested state of an obsolete transition.
- [ ] Process USB rule events through a bounded `Channel` or equivalent single-consumer queue.
- [ ] Define overflow behavior and coalesce stale add/remove transitions.
- [ ] Cap or remove `RecentUsbEvents`; use a fixed-size buffer if it remains useful.
- [ ] Move Windows WMI and Linux `udevadm info --export-db` scans off the UI thread.
- [ ] Measure idle CPU before and after changing the one-second WMI polling queries.
- [ ] Remove the ineffective `AppDomain.UnhandledException` suppression and handle cancellation at
      the originating task or dispatcher boundary.
- [ ] Align all Avalonia package versions.
- [ ] Enable a focused CI analyzer set for disposal, async correctness, and process handling.

### Acceptance Criteria

- [ ] A rapid monitoring-toggle test always converges on the final requested state.
- [ ] USB event storms have bounded memory and task counts.
- [ ] Initial device scanning does not block the UI dispatcher.
- [ ] Idle CPU and memory remain stable during an extended monitoring run.

## Final Verification Checklist

- [x] Run `dotnet restore` when package or project files change.
- [x] Run `dotnet build --no-restore`.
- [x] Run `dotnet test --no-restore`.
- [ ] Publish and smoke-test Windows x64 standalone output.
- [ ] Publish and smoke-test Linux x64 standalone output.
- [ ] Verify add and remove rules on real hardware for both platforms.
- [ ] Verify normal exit, tray exit, restart, and OS-session shutdown paths.
- [ ] Verify configuration recovery after forced termination during a save.
- [ ] Confirm `git status --short` contains only the intended batch changes.
- [ ] Update this document, the developer guide, and release notes with completed behavior.

## Completion Record

Use this section to hand work between agents.

| Batch | Status | Commit or PR | Notes / remaining work |
|---|---|---|---|
| 0 | Complete | `11ff3ce` | Added a headless xUnit regression project with fixtures, fakes, and temp config support. |
| 1 | Implemented | This commit | Automated tests pass; Linux hardware smoke tests remain. Add release notes when the next version is selected. |
| 2 | Not started | | |
| 3 | Not started | | |
| 4 | Not started | | |
| 5 | Not started | | |
