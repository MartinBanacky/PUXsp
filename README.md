# PUXsp

Repo for PUXdesign selection process.

## Planned Limitation

The analysis is a best-effort scan of the directory state as files are read. If a file is changed after it has already been analyzed but before the whole scan finishes, the final result may not fully reflect the very latest directory state until the next manual run.

## Versioning Note

The application does not use global versioning across all analyzed paths. Versions are tracked independently per analyzed root directory. A global versioning model would change the context of the app from root-based directory comparison to broader cross-root file history tracking, which adds complexity beyond the scope of this task.

## Additional Limitation

Deep directory trees are supported in principle, but very long Windows paths may still cause path access failures depending on the full path length and environment configuration.

## Prerequisites

- Windows environment
- .NET 8 runtime / SDK
- Python 3 for local test data scripts

## Run the App

1. Open [PUXsp.slnx](C:/dev/PUXsp/PUXsp.slnx) in Visual Studio.
2. Set `src/PUXsp.Web` as the startup project if needed.
3. Run the application.
4. Enter a local directory path in the UI.
5. Choose retry count `0..5`.
6. Click `Analyze`.

## Prepare Test Data

Example:

```powershell
python C:\dev\PUXsp\generate_test_data.py C:\dev\PUXspTEST --depth 4 --dirs-per-level 1 --files-per-dir 10 --file-size-kb 1024 --clean
```

This creates nested test directories and files under `C:\dev\PUXspTEST`.

## Edit Test Data

Example:

```powershell
python C:\dev\PUXsp\mutate_test_data.py C:\dev\PUXspTEST --modify-files 5 --delete-files 3 --add-files 4 --delete-dirs 1 --add-dirs 2
```

This modifies existing files, deletes some files and directories, and creates new files and directories for the next analysis run.
