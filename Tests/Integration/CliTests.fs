module CleanCodeTodoApp.Tests.Integration.CliTests

open System
open System.IO
open System.Text
open NUnit.Framework
open FsUnit
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations
open CleanCodeTodoApp.Domain.TodoListOperations

/// <summary>
/// Integration tests for CLI commands.
/// Tests the full flow from command parsing to execution.
/// </summary>

/// CLI command types for testing  
type CliCommand =
    | Create of text: string
    | List
    | Toggle of todoId: string
    | Help
    | Quit
    | Invalid of message: string

/// CLI state for testing
type CliState = {
    TodoList: TodoList
    IsRunning: bool
}

/// Parse command helper function
let parseCommand (input: string) : CliCommand =
    let parts = input.Trim().Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
    
    match parts with
    | [||] -> Help
    | [|"create"|] -> Invalid "Please provide todo text: create <text>"
    | parts when parts.[0] = "create" && parts.Length > 1 ->
        let todoText = parts.[1..] |> String.concat " "
        Create todoText
    | [|"list"|] -> List
    | [|"toggle"; id|] -> Toggle id
    | [|"help"|] -> Help
    | [|"quit"|] | [|"exit"|] -> Quit
    | _ -> Invalid "Unknown command. Type 'help' for available commands."

/// Execute command helper function  
let executeCommand (command: CliCommand) (state: CliState) : CliState =
    match command with
    | Create text ->
        match createTodo text DateTime.UtcNow with
        | Ok todo ->
            let updatedList = addTodo todo state.TodoList
            { state with TodoList = updatedList }
        | Error _ ->
            state
    
    | List ->
        state
    
    | Toggle todoId ->
        let allTodos = getAllTodos state.TodoList |> Seq.toList
        let matchingTodo = 
            allTodos 
            |> List.tryFind (fun todo -> 
                let fullId = TodoCore.todoIdValue todo.Id
                fullId.StartsWith(todoId, StringComparison.OrdinalIgnoreCase))
        
        match matchingTodo with
        | Some todo ->
            let updatedTodo = toggleTodoCompletion todo
            let updateFn = fun _ -> updatedTodo
            match updateTodo todo.Id updateFn state.TodoList with
            | Ok updatedList ->
                { state with TodoList = updatedList }
            | Error _ ->
                state
        | None ->
            state
    
    | Help ->
        state
    
    | Quit ->
        { state with IsRunning = false }
    
    | Invalid _ ->
        state

[<TestFixture>]
type CliIntegrationTests() =

    /// Helper to create test state
    let createTestState () = 
        {
            TodoList = createEmptyTodoList DateTime.UtcNow
            IsRunning = true
        }

    [<Test>]
    member _.``parseCommand handles create with text correctly``() =
        // Arrange & Act
        let result = parseCommand "create Learn F# programming"
        
        // Assert
        match result with
        | Create text -> text |> should equal "Learn F# programming"
        | _ -> Assert.Fail("Expected Create command")

    [<Test>]
    member _.``parseCommand handles create without text``() =
        // Arrange & Act
        let result = parseCommand "create"
        
        // Assert
        match result with
        | Invalid msg -> msg |> should contain "Please provide todo text"
        | _ -> Assert.Fail("Expected Invalid command")

    [<Test>]
    member _.``parseCommand handles list command``() =
        // Arrange & Act
        let result = parseCommand "list"
        
        // Assert
        result |> should equal List

    [<Test>]
    member _.``parseCommand handles toggle with id``() =
        // Arrange & Act
        let result = parseCommand "toggle abc123"
        
        // Assert
        match result with
        | Toggle id -> id |> should equal "abc123"
        | _ -> Assert.Fail("Expected Toggle command")

    [<Test>]
    member _.``parseCommand handles help command``() =
        // Arrange & Act
        let result1 = parseCommand "help"
        let result2 = parseCommand ""
        
        // Assert
        result1 |> should equal Help
        result2 |> should equal Help

    [<Test>]
    member _.``parseCommand handles quit commands``() =
        // Arrange & Act
        let result1 = parseCommand "quit"
        let result2 = parseCommand "exit"
        
        // Assert
        result1 |> should equal Quit
        result2 |> should equal Quit

    [<Test>]
    member _.``parseCommand handles invalid commands``() =
        // Arrange & Act
        let result = parseCommand "unknown command"
        
        // Assert
        match result with
        | Invalid msg -> msg |> should contain "Unknown command"
        | _ -> Assert.Fail("Expected Invalid command")

    [<Test>]
    member _.``executeCommand create adds todo to list``() =
        // Arrange
        let initialState = createTestState()
        let command = Create "Test todo item"
        
        // Act
        let newState = executeCommand command initialState
        
        // Assert
        let (totalCount, _) = countTodos newState.TodoList
        totalCount |> should equal 1
        
        let allTodos = getAllTodos newState.TodoList |> Seq.toList
        allTodos |> should haveLength 1
        TodoCore.todoTextValue allTodos.Head.Text |> should equal "Test todo item"

    [<Test>]
    member _.``executeCommand toggle changes todo status``() =
        // Arrange
        let initialState = createTestState()
        
        // First create a todo
        let createResult = createTodo "Test todo" DateTime.UtcNow
        match createResult with
        | Ok todo ->
            let stateWithTodo = { initialState with TodoList = addTodo todo initialState.TodoList }
            let todoId = TodoCore.todoIdValue todo.Id
            let shortId = todoId.Substring(0, 8)  // Use first 8 characters
            
            // Act
            let toggleCommand = Toggle shortId
            let finalState = executeCommand toggleCommand stateWithTodo
            
            // Assert
            let updatedTodo = findTodo todo.Id finalState.TodoList
            match updatedTodo with
            | Some t -> t.IsCompleted |> should be True
            | None -> Assert.Fail("Todo not found after toggle")
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``executeCommand quit sets IsRunning to false``() =
        // Arrange
        let initialState = createTestState()
        let command = Quit
        
        // Act
        let newState = executeCommand command initialState
        
        // Assert
        newState.IsRunning |> should be False

    [<Test>]
    member _.``full workflow create and toggle todo``() =
        // Arrange
        let mutable state = createTestState()
        
        // Act - Create todo
        let createCommand = parseCommand "create Learn functional programming"
        state <- executeCommand createCommand state
        
        // Get the todo ID for toggling
        let allTodos = getAllTodos state.TodoList |> Seq.toList
        allTodos |> should haveLength 1
        
        let todoId = TodoCore.todoIdValue allTodos.Head.Id
        let shortId = todoId.Substring(0, 8)
        
        // Act - Toggle todo
        let toggleCommand = parseCommand $"toggle {shortId}"
        state <- executeCommand toggleCommand state
        
        // Assert - Todo should be completed
        let (totalCount, completedCount) = countTodos state.TodoList
        totalCount |> should equal 1
        completedCount |> should equal 1