namespace CleanCodeTodoApp.Domain

open System

/// <summary>
/// Unique identifier for a todo item.
/// Wraps a string to provide type safety and prevent confusion with other string values.
/// </summary>
type TodoId = TodoId of string

/// <summary>
/// Represents todo text with validation to prevent empty or invalid text.
/// Using discriminated union makes illegal states (empty text) unrepresentable.
/// </summary>
type TodoText = 
    | ValidText of string

/// <summary>
/// Represents all possible errors in the todo domain.
/// Using discriminated union for explicit error handling without exceptions.
/// </summary>
type TodoError = 
    | TodoNotFound of TodoId
    | InvalidTodoText of string
    | StorageError of string
    | UnknownError of exn

/// <summary>
/// Type alias for Result type with TodoError as the error case.
/// Enables clean composition of operations that may fail.
/// </summary>
type TodoResult<'T> = Result<'T, TodoError>

/// <summary>
/// Represents a single todo item with immutable properties.
/// All fields are immutable to support functional programming principles.
/// </summary>
type Todo = {
    /// Unique identifier for this todo item
    Id: TodoId
    /// The todo text content (validated to be non-empty)
    Text: TodoText  
    /// Whether this todo has been completed
    IsCompleted: bool
    /// When this todo was created (UTC)
    CreatedAt: DateTime
}

/// <summary>
/// Core domain module containing utilities and validation functions.
/// </summary>
module TodoCore =

    /// <summary>
    /// Creates a TodoId from a string with validation.
    /// Returns an error if the ID string is null, empty, or whitespace.
    /// </summary>
    /// <param name="id">The string to convert to TodoId</param>
    /// <returns>Result containing TodoId or TodoError</returns>
    let createTodoId (id: string) : TodoResult<TodoId> =
        if System.String.IsNullOrWhiteSpace(id) then
            Error (InvalidTodoText "Todo ID cannot be empty")
        else
            Ok (TodoId id)

    /// <summary>
    /// Creates ValidText from a string with validation.
    /// Returns an error if the text is null, empty, or whitespace.
    /// </summary>
    /// <param name="text">The string to convert to TodoText</param>
    /// <returns>Result containing TodoText or TodoError</returns>
    let createTodoText (text: string) : TodoResult<TodoText> =
        if System.String.IsNullOrWhiteSpace(text) then
            Error (InvalidTodoText "Todo text cannot be empty")
        else
            Ok (ValidText (text.Trim()))

    /// <summary>
    /// Extracts the string value from TodoId.
    /// Pure function for accessing wrapped value.
    /// </summary>
    /// <param name="todoId">The TodoId to unwrap</param>
    /// <returns>The underlying string value</returns>
    let todoIdValue (TodoId id) = id

    /// <summary>
    /// Extracts the string value from TodoText.
    /// Pure function for accessing wrapped value.
    /// </summary>
    /// <param name="todoText">The TodoText to unwrap</param>
    /// <returns>The underlying string value</returns>
    let todoTextValue (ValidText text) = text

/// <summary>
/// Operations on Todo items.
/// All functions are pure (no side effects) and composable.
/// </summary>
module TodoOperations =
    
    /// <summary>
    /// Creates a new todo with validation.
    /// Pure function that validates input and returns Result type.
    /// </summary>
    /// <param name="text">The todo text content</param>
    /// <param name="createdAt">When the todo was created</param>
    /// <returns>Result containing Todo or TodoError</returns>
    let createTodo (text: string) (createdAt: DateTime) : TodoResult<Todo> =
        let generateId = TodoId (System.Guid.NewGuid().ToString())
        
        TodoCore.createTodoText text
        |> Result.map (fun validText -> 
            { 
                Id = generateId
                Text = validText
                IsCompleted = false
                CreatedAt = createdAt 
            })

    /// <summary>
    /// Toggles the completion status of a todo.
    /// Pure function - returns new Todo with updated status.
    /// </summary>
    /// <param name="todo">The todo to toggle</param>
    /// <returns>Todo with toggled completion status</returns>
    let toggleTodoCompletion (todo: Todo) : Todo =
        { todo with IsCompleted = not todo.IsCompleted }

    /// <summary>
    /// Marks a todo as completed.
    /// Pure function - returns new Todo with completed status.
    /// </summary>
    /// <param name="todo">The todo to complete</param>
    /// <returns>Todo marked as completed</returns>
    let completeTodo (todo: Todo) : Todo =
        { todo with IsCompleted = true }

    /// <summary>
    /// Marks a todo as incomplete.
    /// Pure function - returns new Todo with incomplete status.
    /// </summary>
    /// <param name="todo">The todo to mark incomplete</param>
    /// <returns>Todo marked as incomplete</returns>
    let uncompleteTodo (todo: Todo) : Todo =
        { todo with IsCompleted = false }