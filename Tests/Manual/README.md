# Manual Test Scripts

These are legacy manual test scripts from the development phase.

## ⚠️ Note

For automated testing, use the xUnit test suite:

```bash
dotnet test
```

## Available Manual Tests

| File | Purpose |
|------|---------|
| `TestScanEngine.cs` | Manual engine testing |
| `TestMultiThreaded.cs` | Threading performance tests |
| `TestPdfIntegration.cs` | PDF export validation |
| `TestFileHelper.cs` | Utility function tests |
| `TestConsoleUI.cs` | UI component tests |
| `TestDirectScan.cs` | Direct scan workflow |
| `TestPlatform.cs` | Platform compatibility |
| `TestWorkflow.cs` | End-to-end workflows |

## Usage

These scripts were used during development and are kept for reference.
They are **not** part of the automated test suite.

For current testing, see: `../FolderVision.Tests/`
