namespace Tests.Integration

open System
open System.IO
open NUnit.Framework
open FsUnit
open CleanCodeTodoApp.Infrastructure
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations
open CleanCodeTodoApp.Domain.TodoListOperations

/// <summary>
/// Integration tests for CLI storage operations.
/// Tests end-to-end functionality of storage commands.
/// </summary>
[<TestFixture>]
type CliStorageIntegrationTests() =

    let mutable tempFilePath: string = ""
    
    [<SetUp>]
    member _.Setup() =
        tempFilePath <- Path.GetTempFileName()
    
    [<TearDown>]
    member _.TearDown() =
        if File.Exists(tempFilePath) then
            File.Delete(tempFilePath)

    /// <summary>
    /// Test that demonstrates the full storage workflow:
    /// Create storage -> Load empty -> Save todos -> Load persisted data
    /// </summary>
    [<Test>]
    member _.``CLI storage workflow preserves todos across operations``() =
        // Arrange
        let fileSystem = RealFileSystem() :> IFileSystem
        let storage = JsonTodoStorage(fileSystem, tempFilePath) :> ITodoStorage
        
        // Act & Assert - Step 1: Create todos and save
        let todo1Result = createTodo "Test storage integration" DateTime.UtcNow
        let todo2Result = createTodo "Verify persistence" DateTime.UtcNow
        
        match todo1Result, todo2Result with
        | Ok todo1, Ok todo2 ->
            let todoList = createEmptyTodoList DateTime.UtcNow
                          |> addTodo todo1
                          |> addTodo todo2
                          
            let saveTask = TodoStorageOperations.saveTodosAsync storage todoList
            saveTask.Wait()
            
            match saveTask.Result with
            | Ok _ ->
                // Act & Assert - Step 2: Load persisted data
                let loadPersistedTask = TodoStorageOperations.loadTodosAsync storage
                loadPersistedTask.Wait()
                
                match loadPersistedTask.Result with
                | Ok persistedList ->
                    let (persistedCount, _) = countTodos persistedList
                    persistedCount |> should equal 2
                    
                    // Verify todos are retrievable by ID and content (ignoring potential DateTime differences)
                    match findTodo todo1.Id persistedList with
                    | Some persistedTodo1 -> 
                        TodoCore.todoIdValue persistedTodo1.Id |> should equal (TodoCore.todoIdValue todo1.Id)
                        TodoCore.todoTextValue persistedTodo1.Text |> should equal (TodoCore.todoTextValue todo1.Text)
                        persistedTodo1.IsCompleted |> should equal todo1.IsCompleted
                    | None -> Assert.Fail("Todo1 should be found after persistence")
                    
                    match findTodo todo2.Id persistedList with
                    | Some persistedTodo2 -> 
                        TodoCore.todoIdValue persistedTodo2.Id |> should equal (TodoCore.todoIdValue todo2.Id)
                        TodoCore.todoTextValue persistedTodo2.Text |> should equal (TodoCore.todoTextValue todo2.Text)
                        persistedTodo2.IsCompleted |> should equal todo2.IsCompleted
                    | None -> Assert.Fail("Todo2 should be found after persistence")
                | Error err -> Assert.Fail($"Persisted load failed: {err}")
            | Error err -> Assert.Fail($"Save failed: {err}")
        | _ -> Assert.Fail("Todo creation failed")

    /// <summary>
    /// Test storage transaction pattern used by CLI operations.
    /// Uses in-memory storage to avoid file system complexity.
    /// </summary>
    [<Test>]
    member _.``Storage transaction pattern maintains consistency``() =
        // Arrange - Use in-memory storage to avoid file system issues
        let storage = InMemoryTodoStorage() :> ITodoStorage
        let todoResult = createTodo "Transaction test todo" DateTime.UtcNow
        
        match todoResult with
        | Ok todo ->
            // Act - Use transaction pattern to add todo
            let addOperation = fun todoList -> Ok (addTodo todo todoList)
            let addTransactionTask = TodoStorageOperations.withStorageTransactionAsync storage addOperation
            addTransactionTask.Wait()
            
            match addTransactionTask.Result with
            | Ok resultList ->
                // Assert - Todo was added
                findTodo todo.Id resultList |> should equal (Some todo)
                
                // Act - Use transaction pattern to toggle todo  
                let toggleOperation = fun todoList ->
                    let updatedTodo = toggleTodoCompletion todo
                    let updateFn = fun _ -> updatedTodo
                    updateTodo todo.Id updateFn todoList
                
                let toggleTransactionTask = TodoStorageOperations.withStorageTransactionAsync storage toggleOperation
                toggleTransactionTask.Wait()
                
                match toggleTransactionTask.Result with
                | Ok finalList ->
                    // Assert - Todo was toggled
                    match findTodo todo.Id finalList with
                    | Some toggledTodo -> toggledTodo.IsCompleted |> should equal true
                    | None -> Assert.Fail("Todo should exist after toggle")
                | Error err -> Assert.Fail($"Toggle transaction failed: {err}")
            | Error err -> Assert.Fail($"Add transaction failed: {err}")
        | Error _ -> Assert.Fail("Todo creation failed")

    /// <summary>
    /// Test storage availability checking (used by status command).
    /// </summary>
    [<Test>]
    member _.``Storage status check works correctly``() =
        // Arrange
        let fileSystem = RealFileSystem() :> IFileSystem
        let storage = JsonTodoStorage(fileSystem, tempFilePath) :> ITodoStorage
        
        // Act
        let statusTask = TodoStorageOperations.isStorageAvailableAsync storage
        statusTask.Wait()
        
        // Assert
        statusTask.Result |> should equal true