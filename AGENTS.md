# Agent Code Generation Convention (.NET 8 ASP.NET Core MVC)

This file defines the code generation conventions for this repository. All generated code should follow these rules unless the user explicitly asks otherwise.

## 1. Architecture and Responsibilities

- Use `ASP.NET Core MVC` with thin controllers.
- Keep business logic out of controllers and views.
- Put directory scanning, snapshot comparison, retry handling, hashing, and JSON persistence into services.
- Keep models and view models focused on data shape and validation metadata.
- Use strongly typed Razor views with standard Tag Helpers.

## 2. Project-Specific Design Rules

- Persist state to local `JSON` files. Do not use a database.
- Detect file changes by content hash, not by timestamp alone.
- Store file and directory paths as paths relative to the analyzed root directory.
- Track deleted subdirectories explicitly, not only deleted files.
- Treat the first successful analysis as baseline creation.
- If analysis of a file is unstable because the file changes during reading, retry up to the configured limit and report a warning if it still cannot be read reliably.
- Do not try to fully detect post-scan changes for files already analyzed. That remains a documented limitation in `README.md`.
- Do not overwrite the saved snapshot for a root if the overall analysis fails in a way that would leave persisted state unreliable.

## 3. Naming and C# Style

- Use `PascalCase` for classes, methods, properties, interfaces, and namespaces.
- Use `camelCase` for local variables and method parameters.
- Use `_camelCase` for private readonly fields.
- Prefix interfaces with `I`.
- Use file-scoped namespaces.
- Prefer clear, explicit code over clever abstractions.
- Prefer LINQ extension syntax over query syntax unless query syntax is substantially clearer.
- Respect nullable reference types and use explicit null checks with pattern matching where appropriate.

## 4. Async and I/O

- Use asynchronous APIs for file I/O and persistence work where practical.
- Name asynchronous methods with the `Async` suffix.
- Accept and propagate `CancellationToken` in controller actions and I/O-heavy service methods where it materially improves cancellation behavior.

## 5. Dependency Injection

- Use constructor injection only.
- Register services directly in `Program.cs`.
- Do not use service location or resolve services manually inside methods.
- Keep service lifetimes simple and intentional.

## 6. Validation, Errors, and Security

- Validate user input as early as possible.
- For MVC form submissions, return the same view with validation messages when input is invalid.
- Use `[HttpPost]` and `[ValidateAntiForgeryToken]` for MVC form posts that modify or analyze state.
- Never use empty `catch` blocks.
- If exceptions are caught, log useful context and return a controlled user-facing result.

## 7. Comments and Documentation

- Do not add boilerplate comments.
- Inline comments should explain why a choice exists, not restate syntax.
- Add XML documentation only for public methods whose intent or contract is not obvious from the code.

## 8. Completion Standard

- Generate complete, compilable implementations.
- Do not leave placeholders such as `TODO`, stub methods, or partial files unless the user explicitly requests scaffolding only.
- Keep the codebase small and pragmatic for the scope of this selection task.
