# Quickstart Guide: Clean Code Todo Demonstration App

**Created**: 2025-11-09  
**Purpose**: Development setup and getting started guide for F# clean code todo application

## Prerequisites

### Required Software
- .NET 8.0 SDK or later
- F# language support (included with .NET SDK)
- Your preferred editor (VS Code with Ionide, Visual Studio, JetBrains Rider)
- Git (for version control)

### Recommended Tools
- Fantomas (F# code formatter)
- FSharp.Analyzers (static analysis)
- Paket or NuGet (package management)

## Project Setup

### 1. Create Project Structure

```bash
# Create main application project
dotnet new console -lang F# -n CleanCodeTodoApp
cd CleanCodeTodoApp

# Create test project  
dotnet new nunit -lang F# -n Tests
dotnet add Tests reference CleanCodeTodoApp

# Create solution
dotnet new sln
dotnet sln add CleanCodeTodoApp Tests
```

### 2. Install Dependencies

Add to `CleanCodeTodoApp/CleanCodeTodoApp.fsproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Domain/Todo.fs" />
    <Compile Include="Domain/TodoList.fs" />
    <Compile Include="Infrastructure/JsonStorage.fs" />
    <Compile Include="Infrastructure/FileSystem.fs" />
    <Compile Include="Application/TodoService.fs" />
    <Compile Include="Application/Commands.fs" />
    <Compile Include="CLI/Parser.fs" />
    <Compile Include="CLI/Display.fs" />
    <Compile Include="CLI/Workflow.fs" />
    <Compile Include="Program.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
```

Add to `Tests/Tests.fsproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Domain/TodoTests.fs" />
    <Compile Include="Domain/TodoListTests.fs" />
    <Compile Include="Application/TodoServiceTests.fs" />
    <Compile Include="Infrastructure/JsonStorageTests.fs" />
    <Compile Include="CLI/ParserTests.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="FsUnit" Version="5.6.1" />
    <PackageReference Include="FsCheck" Version="2.16.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../CleanCodeTodoApp/CleanCodeTodoApp.fsproj" />
  </ItemGroup>
</Project>
```

### 3. Create Directory Structure

```bash
# Create application directories
mkdir -p CleanCodeTodoApp/{Domain,Infrastructure,Application,CLI,data}
mkdir -p Tests/{Domain,Application,Infrastructure,CLI}
mkdir -p docs
```

### 4. Create Sample Data

Create `CleanCodeTodoApp/data/todos.json`:

```json
{
  "todos": [
    {
      "id": "todo-1",
      "text": "Learn F# functional programming basics",
      "isCompleted": true,
      "createdAt": "2025-11-01T10:00:00.000Z"
    },
    {
      "id": "todo-2", 
      "text": "Implement clean code demonstration app",
      "isCompleted": false,
      "createdAt": "2025-11-08T14:30:00.000Z"
    },
    {
      "id": "todo-3",
      "text": "Write comprehensive documentation with examples",
      "isCompleted": false, 
      "createdAt": "2025-11-09T09:15:00.000Z"
    }
  ],
  "lastModified": "2025-11-09T11:45:00.000Z"
}
```

## Development Workflow

### Building and Testing

```bash
# Build the application
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test Tests

# Run tests with coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

### Running the Application

```bash
# Run with default data
dotnet run --project CleanCodeTodoApp

# Run with specific commands
dotnet run --project CleanCodeTodoApp -- add "New todo item"
dotnet run --project CleanCodeTodoApp -- list
dotnet run --project CleanCodeTodoApp -- complete todo-1
dotnet run --project CleanCodeTodoApp -- stats
dotnet run --project CleanCodeTodoApp -- help
```

### Code Formatting

```bash
# Install Fantomas globally
dotnet tool install -g fantomas

# Format all F# files
fantomas CleanCodeTodoApp Tests

# Check formatting without changes
fantomas --check CleanCodeTodoApp Tests
```

## Development Guidelines

### Clean Code Principles to Follow

1. **Functional-First Design**
   - Use immutable data structures (records, discriminated unions)
   - Write pure functions without side effects
   - Compose functions using pipeline operators

```fsharp
// Good: Pure function with pipeline
let processCommand command =
    command
    |> validateCommand
    |> Result.bind executeCommand
    |> Result.map formatResult
```

2. **Type-Driven Development**
   - Define domain types first
   - Make illegal states unrepresentable
   - Use explicit type signatures

```fsharp
// Good: Explicit types prevent errors
type TodoId = TodoId of string
type TodoText = ValidText of string | EmptyText
```

3. **Module-First Architecture**
   - One module per file
   - Clear public/private boundaries
   - Minimal public API surface

```fsharp
module Domain.Todo =
    // Private helpers
    let private validateText text = // ...
    
    // Public API
    let createTodo text createdAt = // ...
```

4. **Error Handling with Result Types**
   - No exceptions in business logic
   - Use Result<'T, 'Error> for operations that can fail
   - Compose with Result.bind and Result.map

```fsharp
// Good: Explicit error handling
type TodoResult<'T> = Result<'T, TodoError>

let addTodo text todoList =
    createTodo text DateTime.UtcNow
    |> Result.map (fun todo -> addTodoToList todo todoList)
```

### Testing Strategy

1. **Unit Tests for Pure Functions**
   ```fsharp
   [<Test>]
   let ``createTodo with valid text succeeds`` () =
       let result = createTodo "Valid todo" DateTime.UtcNow
       result |> should be (ofCase <@ Ok @>)
   ```

2. **Property-Based Tests for Invariants**
   ```fsharp
   [<Property>]
   let ``adding and removing todo preserves list consistency`` (text: string) =
       let todoList = createEmptyTodoList DateTime.UtcNow
       // Property test implementation
   ```

3. **Integration Tests for I/O**
   ```fsharp
   [<Test>]
   let ``save and load roundtrip preserves data`` () =
       // Test actual file I/O
   ```

### File Organization

```
CleanCodeTodoApp/
├── Domain/           # Pure business logic
│   ├── Todo.fs       # Todo entity and operations
│   └── TodoList.fs   # TodoList aggregate
├── Infrastructure/   # External dependencies
│   ├── JsonStorage.fs # File I/O implementation
│   └── FileSystem.fs  # File system abstractions
├── Application/      # Use case orchestration
│   ├── TodoService.fs # Service layer
│   └── Commands.fs    # Command handlers
└── CLI/              # User interface
    ├── Parser.fs     # Command parsing
    ├── Display.fs    # Output formatting
    └── Workflow.fs   # CLI orchestration
```

## Common Patterns

### Pipeline Composition
```fsharp
let handleCommand command =
    command
    |> parseCommand
    |> Result.bind validateCommand  
    |> Result.bind executeCommand
    |> Result.map formatOutput
    |> displayResult
```

### Result Type Chaining
```fsharp
let createAndSaveTodo text =
    result {
        let! todo = createTodo text DateTime.UtcNow
        let! todoList = loadTodoList()
        let updatedList = addTodo todo todoList
        do! saveTodoList updatedList
        return todo
    }
```

### Dependency Injection
```fsharp
type TodoServiceConfig = {
    Storage: IStorage
    TimeProvider: unit -> DateTime
}

let createTodoService config =
    // Service implementation using injected dependencies
```

## Debugging Tips

### Common Build Errors
- **FS0039**: Check module/namespace imports
- **FS0001**: Type signature mismatch - check function parameters
- **FS0025**: Incomplete pattern matching - handle all cases

### Testing Issues  
- Use `printfn "%A" value` for debugging F# values
- Check async test execution with proper `Async.RunSynchronously`
- Verify Result pattern matching covers all cases

### Performance Considerations
- Profile JSON serialization for large todo lists
- Use Map<> for O(log n) lookups by ID
- Consider lazy evaluation for display formatting

## Next Steps

1. Implement domain types and operations
2. Add storage layer with JSON serialization
3. Create application service layer
4. Build CLI interface
5. Add comprehensive tests
6. Generate clean code checklist

## Resources

- [F# Language Reference](https://docs.microsoft.com/en-us/dotnet/fsharp/)
- [F# Style Guide](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/)
- [Functional Programming Principles](https://fsharpforfunandprofit.com/)
- [Clean Architecture in F#](https://github.com/fsprojects/FsClean)

**Ready to Start**: Follow this guide to begin implementing the clean code todo demonstration application.