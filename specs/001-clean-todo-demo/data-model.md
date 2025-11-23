# Data Model: Clean Code Todo Demonstration App

**Created**: 2025-11-09  
**Feature**: [Clean Code Todo Demo](spec.md)  
**Purpose**: Define domain entities, types, and data structures

## Core Domain Types

### Todo Entity

**Purpose**: Represents a single todo item with immutable properties and type safety

```fsharp
type TodoId = TodoId of string

type TodoText = 
    | ValidText of string
    | EmptyText
    
type Todo = {
    Id: TodoId
    Text: TodoText  
    IsCompleted: bool
    CreatedAt: DateTime
}
```

**Validation Rules**:
- TodoId must be non-empty string (enforced by type)
- TodoText cannot be empty string (illegal state unrepresentable)
- CreatedAt uses UTC DateTime for consistency
- IsCompleted defaults to false for new todos

**Type Safety Features**:
- TodoId prevents string confusion with other IDs
- TodoText discriminated union makes empty text unrepresentable
- Immutable record prevents accidental mutation

### TodoList Aggregate

**Purpose**: Contains collection of todos with aggregate operations

```fsharp
type TodoList = {
    Todos: Map<TodoId, Todo>
    LastModified: DateTime
}
```

**Aggregate Rules**:
- Uses Map for O(log n) lookups by ID
- LastModified tracks changes for persistence
- Immutable collection prevents external mutation

### Command Types

**Purpose**: Represent user intentions with type safety

```fsharp
type CreateTodoCommand = {
    Text: string
    RequestedAt: DateTime
}

type CompleteTodoCommand = {
    TodoId: TodoId
    RequestedAt: DateTime  
}

type ListTodosQuery = {
    ShowCompleted: bool
    RequestedAt: DateTime
}
```

### Result Types

**Purpose**: Handle operations results without exceptions

```fsharp
type TodoError = 
    | TodoNotFound of TodoId
    | InvalidTodoText of string
    | StorageError of string
    | UnknownError of exn

type TodoResult<'T> = Result<'T, TodoError>
```

**Error Handling**:
- Explicit error cases for all failure modes
- No exceptions in domain layer
- Composable with Result.bind and Result.map

## Persistence Model

### JSON Serialization Types

**Purpose**: Data transfer objects for JSON persistence

```fsharp
type TodoDto = {
    id: string
    text: string  
    isCompleted: bool
    createdAt: string // ISO 8601 format
}

type TodoListDto = {
    todos: TodoDto[]
    lastModified: string
}
```

**Mapping Rules**:
- DTOs use camelCase for JSON conventions
- DateTime serialized as ISO 8601 strings
- Separate from domain types to prevent JSON concerns leaking

## State Transitions

### Todo Lifecycle

```
[New] --create--> [Incomplete] --complete--> [Complete]
  |                    |                        |
  |                    |--uncomplete----------|
```

**Valid Transitions**:
- New → Incomplete (via CreateTodo)
- Incomplete → Complete (via CompleteTodo) 
- Complete → Incomplete (via UncompleteTodo)

**Invariants**:
- Todo text cannot be empty after creation
- CreatedAt never changes after creation
- ID is immutable and unique within TodoList

### TodoList State

```
[Empty] --add todo--> [Has Todos] --remove all--> [Empty]
   |                       |
   |--load from storage-----|
   |--save to storage-------|
```

**Persistence Rules**:
- LastModified updated on any todo change
- Map preserves insertion order for display
- Empty list is valid state

## Domain Operations

### Pure Functions (Side-Effect Free)

```fsharp
// Todo creation with validation
val createTodo : string -> DateTime -> TodoResult<Todo>

// Todo completion toggle
val toggleTodoCompletion : Todo -> Todo

// TodoList queries
val findTodo : TodoId -> TodoList -> Todo option
val filterCompleted : bool -> TodoList -> Todo seq
val countTodos : TodoList -> int * int // (total, completed)

// TodoList updates (returns new immutable list)
val addTodo : Todo -> TodoList -> TodoList
val updateTodo : TodoId -> (Todo -> Todo) -> TodoList -> TodoResult<TodoList>
val removeTodo : TodoId -> TodoList -> TodoList
```

**Purity Guarantees**:
- No I/O operations in domain functions
- No mutations of input parameters
- Deterministic outputs for same inputs
- Composable with pipeline operators

### Type Mapping Functions

```fsharp
// Domain <-> DTO conversion
val todoToDto : Todo -> TodoDto
val todoFromDto : TodoDto -> TodoResult<Todo>
val todoListToDto : TodoList -> TodoListDto  
val todoListFromDto : TodoListDto -> TodoResult<TodoList>
```

## Sample Data Structure

### Default JSON Format

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
    },
    {
      "id": "todo-4",
      "text": "Create best practices checklist",
      "isCompleted": false,
      "createdAt": "2025-11-09T11:45:00.000Z"
    }
  ],
  "lastModified": "2025-11-09T11:45:00.000Z"
}
```

**Data Characteristics**:
- Mix of completed/incomplete items for testing
- Realistic todo text content
- Chronological creation dates
- ISO 8601 timestamp format
- Unique incremental IDs

## Validation Summary

**Type Safety**: ✅ Illegal states unrepresentable through type design
**Immutability**: ✅ All domain types are immutable records/unions  
**Pure Functions**: ✅ No side effects in domain operations
**Error Handling**: ✅ Result types instead of exceptions
**Testability**: ✅ Deterministic functions easy to unit test

**Ready for Contract Generation**: Domain model complete and validated.