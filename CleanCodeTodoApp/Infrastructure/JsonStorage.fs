namespace CleanCodeTodoApp.Infrastructure

open System
open System.Threading.Tasks
open Newtonsoft.Json
open CleanCodeTodoApp.Domain

/// <summary>
/// Data Transfer Object for Todo serialization.
/// Separate from domain types to isolate JSON concerns from business logic.
/// </summary>
[<CLIMutable>]
type TodoDto = {
    /// Todo ID as plain string for JSON
    [<JsonProperty("id")>]
    id: string
    /// Todo text content
    [<JsonProperty("text")>]
    text: string  
    /// Completion status
    [<JsonProperty("isCompleted")>]
    isCompleted: bool
    /// Creation timestamp in ISO 8601 format
    [<JsonProperty("createdAt")>]
    createdAt: string
}

/// <summary>
/// Data Transfer Object for TodoList serialization.
/// Contains array of TodoDto and metadata.
/// </summary>
[<CLIMutable>]
type TodoListDto = {
    /// Array of todo DTOs
    [<JsonProperty("todos")>]
    todos: TodoDto[]
    /// Last modification timestamp in ISO 8601 format
    [<JsonProperty("lastModified")>]
    lastModified: string
}

/// <summary>
/// Conversion functions between domain types and DTOs.
/// Pure functions that handle the mapping between representations.
/// </summary>
module DtoConversion =

    /// <summary>
    /// Converts a Todo domain object to TodoDto for serialization.
    /// Pure function with no side effects.
    /// </summary>
    /// <param name="todo">Domain Todo object</param>
    /// <returns>TodoDto ready for JSON serialization</returns>
    let todoToDto (todo: Todo) : TodoDto =
        {
            id = TodoCore.todoIdValue todo.Id
            text = TodoCore.todoTextValue todo.Text
            isCompleted = todo.IsCompleted
            createdAt = todo.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        }

    /// <summary>
    /// Converts a TodoDto from JSON to Todo domain object.
    /// Validates data and returns Result type for error handling.
    /// </summary>
    /// <param name="dto">TodoDto from JSON deserialization</param>
    /// <returns>Result containing Todo or TodoError</returns>
    let todoFromDto (dto: TodoDto) : TodoResult<Todo> =
        match TodoCore.createTodoId dto.id with
        | Error e -> Error e
        | Ok todoId ->
            match TodoCore.createTodoText dto.text with
            | Error e -> Error e
            | Ok todoText ->
                match DateTime.TryParse(dto.createdAt) with
                | (false, _) -> Error (StorageError $"Invalid date format: {dto.createdAt}")
                | (true, createdAt) -> 
                    Ok {
                        Id = todoId
                        Text = todoText
                        IsCompleted = dto.isCompleted
                        CreatedAt = createdAt
                    }

    /// <summary>
    /// Converts a TodoList domain object to TodoListDto for serialization.
    /// Pure function with no side effects.
    /// </summary>
    /// <param name="todoList">Domain TodoList object</param>
    /// <returns>TodoListDto ready for JSON serialization</returns>
    let todoListToDto (todoList: TodoList) : TodoListDto =
        let todoDtos = 
            todoList.Todos
            |> Map.values
            |> Seq.map todoToDto
            |> Seq.toArray
        
        {
            todos = todoDtos
            lastModified = todoList.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        }

    /// <summary>
    /// Converts a TodoListDto from JSON to TodoList domain object.
    /// Validates all data and returns Result type for error handling.
    /// </summary>
    /// <param name="dto">TodoListDto from JSON deserialization</param>
    /// <returns>Result containing TodoList or TodoError</returns>
    let todoListFromDto (dto: TodoListDto) : TodoResult<TodoList> =
        match DateTime.TryParse(dto.lastModified) with
        | (false, _) -> Error (StorageError $"Invalid last modified date format: {dto.lastModified}")
        | (true, lastModified) ->
            let convertedTodos = 
                dto.todos
                |> Array.map todoFromDto
                |> Array.fold (fun acc todoResult ->
                    match acc, todoResult with
                    | Ok todoList, Ok todo -> Ok (todo :: todoList)
                    | Error e, _ -> Error e
                    | _, Error e -> Error e) (Ok [])
            
            match convertedTodos with
            | Error e -> Error e
            | Ok todos ->
                let todoMap = 
                    todos
                    |> List.map (fun todo -> (todo.Id, todo))
                    |> Map.ofList
                
                Ok {
                    Todos = todoMap
                    LastModified = lastModified
                }

    /// <summary>
    /// Deserializes JSON string to TodoListDto.
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Result containing TodoListDto or error</returns>
    let deserializeTodoListDto (json: string) : Result<TodoListDto, string> =
        try
            let dto = JsonConvert.DeserializeObject<TodoListDto>(json)
            Ok dto
        with
        | ex -> Error ex.Message

    /// <summary>
    /// Serializes TodoListDto to JSON string synchronously.
    /// </summary>
    /// <param name="dto">The TodoListDto to serialize</param>
    /// <returns>Result containing JSON string or error</returns>
    let serializeTodoListDto (dto: TodoListDto) : Result<string, string> =
        try
            let json = JsonConvert.SerializeObject(dto, Formatting.Indented)
            Ok json
        with
        | ex -> Error ex.Message

    /// <summary>
    /// Serializes TodoListDto to JSON string asynchronously.
    /// </summary>
    /// <param name="dto">The TodoListDto to serialize</param>
    /// <returns>Task with Result containing JSON string or error</returns>
    let serializeTodoListDtoAsync (dto: TodoListDto) : Task<Result<string, string>> = 
        task {
            try
                let json = JsonConvert.SerializeObject(dto, Formatting.Indented)
                return Ok json
            with
            | ex -> return Error ex.Message
        }