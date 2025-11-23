# Module Contracts: Clean Code Todo Demonstration App

**Purpose**: Define public interfaces and contracts for all application modules
**Created**: 2025-11-09

## Domain Module Contract

### Public Types

```fsharp
// Core domain types
type TodoId = TodoId of string
type TodoText = ValidText of string | EmptyText
type Todo = { Id: TodoId; Text: TodoText; IsCompleted: bool; CreatedAt: DateTime }
type TodoList = { Todos: Map<TodoId, Todo>; LastModified: DateTime }

// Command types  
type CreateTodoCommand = { Text: string; RequestedAt: DateTime }
type CompleteTodoCommand = { TodoId: TodoId; RequestedAt: DateTime }
type ListTodosQuery = { ShowCompleted: bool; RequestedAt: DateTime }

// Error handling
type TodoError = TodoNotFound of TodoId | InvalidTodoText of string | StorageError of string | UnknownError of exn
type TodoResult<'T> = Result<'T, TodoError>
```

### Public Functions

```fsharp
module TodoOperations =
    /// Creates a new todo with validation
    val createTodo : string -> DateTime -> TodoResult<Todo>
    
    /// Toggles todo completion status
    val toggleTodoCompletion : Todo -> Todo
    
    /// Finds todo by ID in list
    val findTodo : TodoId -> TodoList -> Todo option
    
    /// Filters todos by completion status
    val filterCompleted : bool -> TodoList -> Todo seq
    
    /// Counts total and completed todos
    val countTodos : TodoList -> int * int
    
module TodoListOperations =
    /// Creates empty todo list
    val createEmptyTodoList : DateTime -> TodoList
    
    /// Adds todo to list (immutable update)
    val addTodo : Todo -> TodoList -> TodoList
    
    /// Updates specific todo in list
    val updateTodo : TodoId -> (Todo -> Todo) -> TodoList -> TodoResult<TodoList>
    
    /// Removes todo from list
    val removeTodo : TodoId -> TodoList -> TodoList
```

**Contract Guarantees**:
- All functions are pure (no side effects)
- Immutable data structures only
- Explicit error handling via Result types
- No exceptions thrown from public API

## Storage Module Contract

### Public Interface

```fsharp
module IStorage =
    /// Storage operation result
    type StorageResult<'T> = Result<'T, string>
    
    /// Loads todo list from persistent storage
    val loadTodoList : unit -> Async<StorageResult<TodoList>>
    
    /// Saves todo list to persistent storage  
    val saveTodoList : TodoList -> Async<StorageResult<unit>>
    
    /// Checks if storage file exists
    val storageExists : unit -> bool
    
    /// Creates default storage with sample data
    val createDefaultStorage : unit -> Async<StorageResult<unit>>
```

**Contract Guarantees**:
- Async operations for all I/O
- Result types for error handling
- No exceptions for normal error cases
- Idempotent operations (safe to retry)

## Application Service Contract

### Public Interface

```fsharp
module TodoService =
    /// Service operation result
    type ServiceResult<'T> = Result<'T, TodoError>
    
    /// Creates new todo and persists to storage
    val createTodo : CreateTodoCommand -> Async<ServiceResult<Todo>>
    
    /// Toggles todo completion and persists
    val toggleTodoCompletion : CompleteTodoCommand -> Async<ServiceResult<Todo>>
    
    /// Retrieves todos based on query
    val listTodos : ListTodosQuery -> Async<ServiceResult<Todo seq>>
    
    /// Gets count statistics
    val getTodoStatistics : unit -> Async<ServiceResult<int * int>>
    
    /// Initializes storage if needed
    val initializeStorage : unit -> Async<ServiceResult<unit>>
```

**Contract Guarantees**:
- Coordinates between domain and storage layers
- Handles cross-cutting concerns (logging, validation)
- Maintains transaction boundaries
- Async operations return quickly

## CLI Module Contract

### Command Parser Interface

```fsharp
module CommandParser =
    type CliCommand = 
        | Add of string
        | Complete of string  
        | List of bool // showCompleted
        | Stats
        | Help
        | Unknown of string
    
    /// Parses command line arguments into structured commands
    val parseArgs : string array -> CliCommand
    
    /// Validates command arguments
    val validateCommand : CliCommand -> Result<CliCommand, string>
```

### Display Interface

```fsharp
module Display =
    /// Formats todo for console display
    val formatTodo : Todo -> string
    
    /// Formats todo list for console display  
    val formatTodoList : Todo seq -> string
    
    /// Formats statistics for display
    val formatStatistics : int * int -> string
    
    /// Formats error messages for user
    val formatError : TodoError -> string
    
    /// Formats help text
    val formatHelp : unit -> string
```

### Workflow Interface

```fsharp
module Workflow =
    /// Executes CLI command and returns exit code
    val executeCommand : CliCommand -> Async<int>
    
    /// Main application entry point
    val runApplication : string array -> Async<int>
```

**Contract Guarantees**:
- Clear separation of parsing, display, and execution
- User-friendly error messages
- Consistent output formatting  
- Proper exit codes for shell integration

## Cross-Module Dependencies

### Dependency Flow

```
CLI -> Application Service -> Domain + Storage
```

**Rules**:
- CLI depends on Application Service only
- Application Service coordinates Domain + Storage
- Domain has no dependencies (pure)
- Storage depends on Domain for types only

### Dependency Injection Points

```fsharp
// Storage implementation injected into service
type TodoServiceConfig = {
    Storage: IStorage
    Logger: ILogger option
}

// Service injected into CLI workflow
type CliWorkflowConfig = {
    TodoService: TodoService
    Display: Display
}
```

## Error Handling Contract

### Error Propagation Strategy

```
Domain Errors -> Service Errors -> CLI Display
     |               |               |
   Result<T>      Result<T>      Exit Codes
```

**Rules**:
- Domain uses Result types for business errors
- Storage uses Result types for I/O errors  
- Services map between error types
- CLI converts errors to user messages and exit codes

### Logging Contract

```fsharp
module ILogger =
    type LogLevel = Debug | Info | Warning | Error
    type LogMessage = { Level: LogLevel; Message: string; Timestamp: DateTime }
    
    val log : LogLevel -> string -> unit
```

**Cross-Cutting Concern**:
- Optional dependency injected at service layer
- Structured logging with levels
- No logging in pure domain functions

## Testing Contracts

### Unit Test Requirements

```fsharp
// All modules must provide testable pure functions
module TodoTests =
    val ``createTodo with valid text succeeds`` : unit -> unit
    val ``createTodo with empty text fails`` : unit -> unit
    val ``toggleTodoCompletion changes status`` : unit -> unit
    
// Property-based tests for domain logic
module TodoPropertyTests =
    val ``adding and removing todo preserves list consistency`` : unit -> unit
    val ``todo operations maintain invariants`` : unit -> unit
```

### Integration Test Requirements

```fsharp  
module StorageIntegrationTests =
    val ``save and load roundtrip preserves data`` : unit -> unit
    val ``storage handles file system errors gracefully`` : unit -> unit
```

**Testing Guarantees**:
- All public functions have corresponding tests
- Property-based tests verify invariants
- Integration tests cover I/O boundaries
- Tests demonstrate clean code principles

## Contract Validation

✅ **Separation of Concerns**: Each module has single responsibility  
✅ **Dependency Inversion**: Abstractions defined, implementations injected
✅ **Pure Functions**: Domain layer has no side effects
✅ **Error Handling**: Explicit Result types throughout
✅ **Immutability**: All data structures immutable
✅ **Testability**: All functions easily unit testable

**Ready for Implementation**: All module contracts defined and validated.