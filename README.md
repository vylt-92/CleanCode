# Clean Code Todo App - F# Demonstration

A comprehensive F# console application demonstrating clean code practices, functional programming principles, and professional software architecture patterns.

## 🎯 Purpose

This project showcases clean code implementation in F# through a simple todo management application. It demonstrates:

- **Functional-First Design** with immutable data structures and pure functions
- **Type-Driven Development** with smart constructors and wrapper types  
- **Clean Architecture** with proper separation of concerns
- **Dependency Injection** patterns for testable, flexible code
- **Comprehensive Testing** with unit, integration, and property-based tests
- **Error Handling** using Result and Option types
- **Professional Code Organization** and documentation standards

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Compatible with Windows, macOS, and Linux

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/vylt-92/CleanCode.git
   cd CleanCode
   ```

2. **Build the application:**
   ```bash
   dotnet build
   ```

3. **Run tests:**
   ```bash
   dotnet test
   ```

4. **Start the application:**
   ```bash
   dotnet run --project CleanCodeTodoApp
   ```

## 💡 Usage

### Basic Commands

The application provides an interactive CLI with the following commands:

```bash
# Create a new todo
> create Buy groceries

# List all todos
> list

# Toggle todo completion status (use first few characters of ID)
> toggle a1b2c3

# Remove a todo
> remove a1b2c3

# Manual save (auto-save also works)
> save

# Reload from file
> load

# Check storage status
> status

# Show help
> help

# Exit application  
> quit
```

### Example Session

```bash
Welcome to Clean Code Todo App!
Type 'help' for available commands.

> create Learn F# functional programming
✓ Todo created: Learn F# functional programming

> create Build clean code demo
✓ Todo created: Build clean code demo

> list

=== Your Todos ===
1. [ ] Learn F# functional programming (ID: a1b2c3d4)
2. [ ] Build clean code demo (ID: e5f6g7h8)

> toggle a1b2
✓ Todo marked as completed: Learn F# functional programming

> status
✓ Storage is available and ready
  📊 Current todos: 2 total, 1 completed

> quit
Goodbye!
```

## 🏗️ Architecture

The application follows **Clean Architecture** principles with clear separation of concerns:

```
CleanCodeTodoApp/
├── Domain/           # Core business logic (pure functions)
│   ├── Todo.fs       # Todo entity with validation
│   └── TodoList.fs   # TodoList aggregate operations
├── Infrastructure/   # External concerns (I/O, persistence)
│   ├── FileSystem.fs # File operations abstraction
│   ├── JsonStorage.fs # JSON serialization with DTOs
│   └── TodoStorage.fs # Storage interface & implementations  
├── Application/      # Use case coordination
│   └── Commands.fs   # Command definitions and DTOs
└── Program.fs        # CLI interface & composition root

Tests/                # Comprehensive test suite
├── Domain/           # Unit tests for business logic
├── Infrastructure/   # Integration tests for I/O
└── Integration/      # End-to-end tests
```

### Dependency Flow

```
CLI/Program.fs
     ↓
Application/Commands.fs  
     ↓
Domain/Todo.fs ←── Infrastructure/TodoStorage.fs
Domain/TodoList.fs ←── Infrastructure/JsonStorage.fs
                  ←── Infrastructure/FileSystem.fs
```

**Key Design Decisions:**
- **Inward Dependencies**: Infrastructure depends on Domain, never the reverse
- **Interface Abstractions**: Core logic depends on interfaces, not concretions
- **Pure Domain Layer**: Business logic has no external dependencies
- **Testable by Design**: Dependency injection enables easy testing

## 🧪 Testing Strategy

The project demonstrates professional testing practices with **64 tests** covering:

### Test Types

- **Unit Tests**: Fast, focused tests for pure functions
- **Integration Tests**: Verify I/O boundaries and storage operations
- **Property-Based Tests**: Validate invariants with random inputs
- **End-to-End Tests**: Full CLI workflow testing

### Test Coverage

```bash
# Run all tests
dotnet test

# Run with coverage reporting  
dotnet test --collect:"XPlat Code Coverage"
```

**Test Results:** 64/64 tests passing (100% success rate)

### Testing Highlights

- **Domain Logic**: Extensively tested pure functions with predictable behavior
- **Storage Layer**: Integration tests for JSON serialization and file operations  
- **Error Handling**: Validation of error cases and edge conditions
- **CLI Integration**: End-to-end testing of user interaction workflows

## 📋 Clean Code Practices

This application demonstrates the following clean code principles:

### ✅ Functional Programming Excellence

- **Immutable Data Structures**: All types immutable by default
- **Pure Functions**: Core business logic without side effects
- **Function Composition**: Building complex operations from simple functions
- **Pipeline Operations**: Readable data transformations with `|>` operator

### ✅ Type Safety & Design

- **Smart Constructors**: Prevent invalid states at compile time
- **Wrapper Types**: Domain concepts wrapped in specific types
- **Result/Option Types**: Explicit error handling without exceptions
- **Type-Driven Development**: Making illegal states unrepresentable

### ✅ Architecture & Organization

- **Separation of Concerns**: Clear boundaries between layers
- **Dependency Injection**: Interface-based abstractions for flexibility
- **Single Responsibility**: Each module has one clear purpose
- **Clean Dependencies**: No circular references or tight coupling

### ✅ Code Quality

- **Descriptive Naming**: Self-documenting function and variable names
- **Consistent Conventions**: Uniform patterns throughout codebase
- **Comprehensive Documentation**: XML docs and inline comments
- **Error Messages**: User-friendly error reporting and guidance

## 📚 Learning Outcomes

By studying this codebase, you'll learn:

### F# Language Features
- Pattern matching and discriminated unions
- Record types and immutable data structures  
- Option and Result types for error handling
- Async workflows and task-based programming
- Module organization and function composition

### Software Architecture
- Clean Architecture implementation in functional languages
- Dependency injection patterns without heavy frameworks
- Domain-driven design with functional programming
- Test-driven development with property-based testing

### Clean Code Practices
- Writing self-documenting, maintainable code
- Organizing code for readability and modularity  
- Implementing robust error handling strategies
- Creating comprehensive test suites

## 🔍 Code Examples

### Domain Model (Immutable & Type-Safe)

```fsharp
// Smart constructor prevents invalid todos
let createTodo (text: string) (createdAt: DateTime) : TodoResult<Todo> =
    match createTodoText text with
    | Ok validText ->
        Ok {
            Id = generateTodoId ()
            Text = validText
            IsCompleted = false
            CreatedAt = createdAt
        }
    | Error err -> Error err

// Pure function - no side effects
let toggleTodoCompletion (todo: Todo) : Todo =
    { todo with IsCompleted = not todo.IsCompleted }
```

### Storage Abstraction (Dependency Injection)

```fsharp
// Interface for flexible storage implementations
type ITodoStorage =
    abstract member LoadTodosAsync: unit -> Task<TodoResult<TodoList>>
    abstract member SaveTodosAsync: TodoList -> Task<TodoResult<unit>>
    
// File-based implementation
type JsonTodoStorage(fileSystem: IFileSystem, filePath: string) =
    interface ITodoStorage with
        member _.LoadTodosAsync() = // Implementation...
        member _.SaveTodosAsync(todoList) = // Implementation...

// Test-friendly implementation  
type InMemoryTodoStorage() =
    interface ITodoStorage with
        member _.LoadTodosAsync() = // In-memory implementation...
```

### Pipeline Operations (Functional Composition)

```fsharp
// Readable data transformation pipelines
let getCompletedTodos (todoList: TodoList) : Todo seq =
    todoList
    |> getAllTodos 
    |> Seq.filter (fun todo -> todo.IsCompleted)

let countTodos (todoList: TodoList) : int * int =
    let allTodos = getAllTodos todoList |> Seq.toList
    let completedCount = 
        allTodos 
        |> List.filter (fun todo -> todo.IsCompleted) 
        |> List.length
    (allTodos.Length, completedCount)
```

## 📖 Documentation

- **[Clean Code Checklist](docs/CleanCodeChecklist.md)**: Comprehensive documentation of all implemented practices
- **[API Documentation](CleanCodeTodoApp/)**: XML documentation for all public functions
- **[Test Documentation](Tests/)**: Examples of unit, integration, and property-based testing

## 🛠️ Development

### Building from Source

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run application in development mode
dotnet run --project CleanCodeTodoApp
```

### Project Structure

- **Solution File**: `CleanCode.sln`
- **Main Application**: `CleanCodeTodoApp/CleanCodeTodoApp.fsproj`
- **Test Project**: `Tests/Tests.fsproj`
