namespace CleanCodeTodoApp.Application

open System
open CleanCodeTodoApp.Domain

/// <summary>
/// Command to create a new todo item.
/// Represents user intention with all necessary data.
/// </summary>
type CreateTodoCommand = {
    /// The text content for the new todo
    Text: string
    /// When the request was made (for auditing and timestamps)
    RequestedAt: DateTime
}

/// <summary>
/// Command to toggle completion status of a todo.
/// Uses TodoId for type safety.
/// </summary>
type CompleteTodoCommand = {
    /// The ID of the todo to toggle
    TodoId: TodoId
    /// When the request was made
    RequestedAt: DateTime
}

/// <summary>
/// Query to list todos with filtering options.
/// Separation of commands (mutations) and queries (reads).
/// </summary>
type ListTodosQuery = {
    /// Whether to include completed todos in results
    ShowCompleted: bool
    /// When the request was made
    RequestedAt: DateTime
}

/// <summary>
/// Command to remove a todo from the list.
/// Explicit command for delete operations.
/// </summary>
type RemoveTodoCommand = {
    /// The ID of the todo to remove
    TodoId: TodoId
    /// When the request was made
    RequestedAt: DateTime
}

/// <summary>
/// Query to get statistics about todos.
/// Pure query operation with no side effects.
/// </summary>
type GetStatisticsQuery = {
    /// When the request was made
    RequestedAt: DateTime
}

/// <summary>
/// Command validation module.
/// Pure functions for validating commands before processing.
/// </summary>
module CommandValidation =

    /// <summary>
    /// Validates a CreateTodoCommand.
    /// Ensures required fields are present and valid.
    /// </summary>
    /// <param name="command">The command to validate</param>
    /// <returns>Result indicating validation success or error</returns>
    let validateCreateTodoCommand (command: CreateTodoCommand) : TodoResult<CreateTodoCommand> =
        if System.String.IsNullOrWhiteSpace(command.Text) then
            Error (InvalidTodoText "Todo text cannot be empty")
        else
            Ok command

    /// <summary>
    /// Validates a CompleteTodoCommand.
    /// Ensures the command has all required fields.
    /// </summary>
    /// <param name="command">The command to validate</param>
    /// <returns>Result indicating validation success or error</returns>
    let validateCompleteTodoCommand (command: CompleteTodoCommand) : TodoResult<CompleteTodoCommand> =
        // TodoId is already type-safe, so just validate presence
        Ok command

    /// <summary>
    /// Validates a ListTodosQuery.
    /// Currently always valid but included for consistency.
    /// </summary>
    /// <param name="query">The query to validate</param>
    /// <returns>Result indicating validation success or error</returns>
    let validateListTodosQuery (query: ListTodosQuery) : TodoResult<ListTodosQuery> =
        Ok query

    /// <summary>
    /// Validates a RemoveTodoCommand.
    /// Ensures the command has all required fields.
    /// </summary>
    /// <param name="command">The command to validate</param>
    /// <returns>Result indicating validation success or error</returns>
    let validateRemoveTodoCommand (command: RemoveTodoCommand) : TodoResult<RemoveTodoCommand> =
        Ok command