namespace CleanCodeTodoApp.Infrastructure

open System
open System.Threading.Tasks
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoListOperations
open CleanCodeTodoApp.Infrastructure

/// <summary>
/// Storage interface for todo operations.
/// Abstracts persistence concerns from domain logic.
/// </summary>
type ITodoStorage =
    /// Load todos from storage
    abstract member LoadTodosAsync: unit -> Task<TodoResult<TodoList>>
    /// Save todos to storage 
    abstract member SaveTodosAsync: TodoList -> Task<TodoResult<unit>>
    /// Check if storage is available
    abstract member IsAvailableAsync: unit -> Task<bool>

/// <summary>
/// JSON file-based todo storage implementation.
/// Demonstrates dependency injection and clean architecture.
/// </summary>
type JsonTodoStorage(fileSystem: IFileSystem, filePath: string) =

    /// <summary>
    /// Creates storage with default file path if none provided.
    /// </summary>
    /// <param name="fileSystem">File system abstraction</param>
    new(fileSystem: IFileSystem) = 
        JsonTodoStorage(fileSystem, "todos.json")

    interface ITodoStorage with

        member _.LoadTodosAsync() = 
            async {
                try
                    let exists = FileSystemOperations.fileExists fileSystem filePath
                    if not exists then
                        // Return empty list if file doesn't exist
                        let emptyList = createEmptyTodoList DateTime.UtcNow
                        return Ok emptyList
                    else
                        let! contentResult = FileSystemOperations.readFileAsync fileSystem filePath
                        match contentResult with
                        | Ok content ->
                            try
                                let dto = DtoConversion.deserializeTodoListDto content
                                match dto with
                                | Ok todoListDto ->
                                    let todoListResult = DtoConversion.todoListFromDto todoListDto
                                    match todoListResult with
                                    | Ok todoList -> return Ok todoList
                                    | Error err -> return Error err
                                | Error err -> 
                                    return Error (InvalidTodoText $"Failed to deserialize todos: {err}")
                            with
                            | ex -> 
                                return Error (InvalidTodoText $"JSON parsing failed: {ex.Message}")
                        | Error err -> 
                            return Error err
                with
                | ex -> 
                    return Error (InvalidTodoText $"Storage error: {ex.Message}")
            } |> Async.StartAsTask

        member _.SaveTodosAsync(todoList: TodoList) = 
            async {
                try
                    let dto = DtoConversion.todoListToDto todoList
                    let jsonResult = DtoConversion.serializeTodoListDto dto
                    match jsonResult with
                    | Ok json ->
                        let! writeResult = FileSystemOperations.writeFileAsync fileSystem filePath json
                        match writeResult with
                        | Ok _ -> return Ok ()
                        | Error err -> return Error err
                    | Error err ->
                        return Error (InvalidTodoText $"Failed to serialize todos: {err}")
                with
                | ex ->
                    return Error (InvalidTodoText $"Storage error: {ex.Message}")
            } |> Async.StartAsTask

        member _.IsAvailableAsync() = 
            async {
                try
                    // Try to create directory if it doesn't exist
                    let directory = System.IO.Path.GetDirectoryName(filePath)
                    if not (String.IsNullOrEmpty(directory)) then
                        fileSystem.CreateDirectory(directory)
                        return true
                    else
                        return true
                with
                | _ -> return false
            } |> Async.StartAsTask

/// <summary>
/// In-memory storage for testing purposes.
/// Demonstrates how abstractions enable different implementations.
/// </summary>
type InMemoryTodoStorage() =
    let mutable storedTodoList : TodoList option = None

    interface ITodoStorage with
        
        member _.LoadTodosAsync() = 
            async {
                match storedTodoList with
                | Some todoList -> return Ok todoList
                | None -> 
                    let emptyList = createEmptyTodoList DateTime.UtcNow
                    storedTodoList <- Some emptyList
                    return Ok emptyList
            } |> Async.StartAsTask

        member _.SaveTodosAsync(todoList: TodoList) = 
            async {
                storedTodoList <- Some todoList
                return Ok ()
            } |> Async.StartAsTask

        member _.IsAvailableAsync() = 
            async {
                return true
            } |> Async.StartAsTask

/// <summary>
/// Storage operations module with dependency injection pattern.
/// Provides functional wrappers around storage interface.
/// </summary>
module TodoStorageOperations =

    /// Load todos from storage with error handling
    let loadTodosAsync (storage: ITodoStorage) : Task<TodoResult<TodoList>> =
        storage.LoadTodosAsync()

    /// Save todos to storage with error handling  
    let saveTodosAsync (storage: ITodoStorage) (todoList: TodoList) : Task<TodoResult<unit>> =
        storage.SaveTodosAsync(todoList)

    /// Check if storage is ready for operations
    let isStorageAvailableAsync (storage: ITodoStorage) : Task<bool> =
        storage.IsAvailableAsync()

    /// Load, apply function, then save - transactional pattern
    let withStorageTransactionAsync (storage: ITodoStorage) (operation: TodoList -> TodoResult<TodoList>) : Task<TodoResult<TodoList>> = 
        async {
            let! loadResult = storage.LoadTodosAsync() |> Async.AwaitTask
            match loadResult with
            | Ok todoList ->
                match operation todoList with
                | Ok updatedList ->
                    let! saveResult = storage.SaveTodosAsync(updatedList) |> Async.AwaitTask
                    match saveResult with
                    | Ok _ -> return Ok updatedList
                    | Error err -> return Error err
                | Error err -> return Error err
            | Error err -> return Error err
        } |> Async.StartAsTask