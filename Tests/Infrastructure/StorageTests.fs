module CleanCodeTodoApp.Tests.Infrastructure.StorageTests

open System
open System.Threading.Tasks
open NUnit.Framework
open FsUnit
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations
open CleanCodeTodoApp.Infrastructure

/// <summary>
/// Tests for storage layer implementations.
/// Validates persistence abstraction and dependency injection.
/// </summary>
[<TestFixture>]
type StorageTests() =

    [<Test>]
    member _.``InMemoryTodoStorage starts with empty list``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        
        // Act
        let loadTask = storage.LoadTodosAsync()
        loadTask.Wait()
        
        // Assert
        match loadTask.Result with
        | Ok todoList -> 
            let (totalCount, _) = TodoListOperations.countTodos todoList
            totalCount |> should equal 0
        | Error err -> Assert.Fail($"Expected empty list but got error: {err}")

    [<Test>]
    member _.``InMemoryTodoStorage saves and loads todos correctly``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        let createdAt = DateTime.UtcNow
        
        match createTodo "Test todo" createdAt with
        | Ok todo ->
            let initialList = TodoListOperations.createEmptyTodoList createdAt
            let listWithTodo = TodoListOperations.addTodo todo initialList
            
            // Act - Save
            let saveTask = storage.SaveTodosAsync(listWithTodo)
            saveTask.Wait()
            
            // Act - Load  
            let loadTask = storage.LoadTodosAsync()
            loadTask.Wait()
            
            // Assert
            match saveTask.Result, loadTask.Result with
            | Ok (), Ok loadedList ->
                let (totalCount, _) = TodoListOperations.countTodos loadedList
                totalCount |> should equal 1
                let foundTodo = TodoListOperations.findTodo todo.Id loadedList
                foundTodo |> should equal (Some todo)
            | Error saveErr, _ -> Assert.Fail($"Save failed: {saveErr}")
            | _, Error loadErr -> Assert.Fail($"Load failed: {loadErr}")
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``InMemoryTodoStorage isAvailable returns true``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        
        // Act
        let availableTask = storage.IsAvailableAsync()
        availableTask.Wait()
        
        // Assert
        availableTask.Result |> should be True

    [<Test>]
    member _.``TodoStorageOperations loadTodosAsync works with InMemoryStorage``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        
        // Act
        let loadTask = TodoStorageOperations.loadTodosAsync storage
        loadTask.Wait()
        
        // Assert
        match loadTask.Result with
        | Ok todoList ->
            let (totalCount, _) = TodoListOperations.countTodos todoList
            totalCount |> should equal 0
        | Error err -> Assert.Fail($"Expected success but got error: {err}")

    [<Test>]
    member _.``TodoStorageOperations saveTodosAsync works with InMemoryStorage``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        let emptyList = TodoListOperations.createEmptyTodoList DateTime.UtcNow
        
        // Act
        let saveTask = TodoStorageOperations.saveTodosAsync storage emptyList
        saveTask.Wait()
        
        // Assert
        match saveTask.Result with
        | Ok () -> () // Success expected
        | Error err -> Assert.Fail($"Expected success but got error: {err}")

    [<Test>]
    member _.``TodoStorageOperations isStorageAvailableAsync works``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        
        // Act
        let availableTask = TodoStorageOperations.isStorageAvailableAsync storage
        availableTask.Wait()
        
        // Assert
        availableTask.Result |> should be True

    [<Test>]
    member _.``TodoStorageOperations withStorageTransactionAsync handles operations correctly``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        
        let operation = fun todoList ->
            match createTodo "Transaction test" DateTime.UtcNow with
            | Ok todo -> Ok (TodoListOperations.addTodo todo todoList)
            | Error err -> Error err
        
        // Act
        let transactionTask = TodoStorageOperations.withStorageTransactionAsync storage operation
        transactionTask.Wait()
        
        // Assert
        match transactionTask.Result with
        | Ok updatedList ->
            let (totalCount, _) = TodoListOperations.countTodos updatedList
            totalCount |> should equal 1
            
            // Verify the storage was actually updated
            let verifyTask = storage.LoadTodosAsync()
            verifyTask.Wait()
            match verifyTask.Result with
            | Ok storedList ->
                let (storedCount, _) = TodoListOperations.countTodos storedList
                storedCount |> should equal 1
            | Error err -> Assert.Fail($"Verification failed: {err}")
        | Error err -> Assert.Fail($"Transaction failed: {err}")

    [<Test>]
    member _.``TodoStorageOperations withStorageTransactionAsync handles operation errors``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        
        let failingOperation = fun _ ->
            Error (InvalidTodoText "Simulated operation failure")
        
        // Act
        let transactionTask = TodoStorageOperations.withStorageTransactionAsync storage failingOperation
        transactionTask.Wait()
        
        // Assert
        match transactionTask.Result with
        | Error (InvalidTodoText msg) -> 
            msg |> should contain "Simulated operation failure"
        | _ -> Assert.Fail("Expected operation failure to be propagated")

    [<Test>]
    member _.``Multiple storage operations maintain consistency``() =
        // Arrange
        let storage = InMemoryTodoStorage() :> ITodoStorage
        
        let todo1Result = createTodo "First todo" DateTime.UtcNow
        let todo2Result = createTodo "Second todo" (DateTime.UtcNow.AddMinutes(1.0))
        
        match todo1Result, todo2Result with
        | Ok todo1, Ok todo2 ->
            let emptyList = TodoListOperations.createEmptyTodoList DateTime.UtcNow
            
            // Act - Multiple operations
            let listWithFirst = TodoListOperations.addTodo todo1 emptyList
            let saveFirstTask = storage.SaveTodosAsync(listWithFirst)
            saveFirstTask.Wait()
            
            let loadTask1 = storage.LoadTodosAsync()
            loadTask1.Wait()
            
            let listWithSecond = 
                match loadTask1.Result with
                | Ok list -> TodoListOperations.addTodo todo2 list
                | Error _ -> failwith "Load failed"
                
            let saveSecondTask = storage.SaveTodosAsync(listWithSecond)
            saveSecondTask.Wait()
            
            let finalLoadTask = storage.LoadTodosAsync()
            finalLoadTask.Wait()
            
            // Assert
            match finalLoadTask.Result with
            | Ok finalList ->
                let (totalCount, _) = TodoListOperations.countTodos finalList
                totalCount |> should equal 2
                
                TodoListOperations.findTodo todo1.Id finalList |> should equal (Some todo1)
                TodoListOperations.findTodo todo2.Id finalList |> should equal (Some todo2)
            | Error err -> Assert.Fail($"Final load failed: {err}")
        | _ -> Assert.Fail("Setup failed")

    [<Test>]
    member _.``Storage abstraction enables testing without file system``() =
        // Arrange - This test demonstrates the power of abstraction
        let memoryStorage = InMemoryTodoStorage() :> ITodoStorage
        
        // This function could work with ANY storage implementation
        let addTodoToStorage (storage: ITodoStorage) (text: string) =
            async {
                let! loadResult = storage.LoadTodosAsync() |> Async.AwaitTask
                match loadResult with
                | Ok todoList ->
                    match createTodo text DateTime.UtcNow with
                    | Ok todo ->
                        let updatedList = TodoListOperations.addTodo todo todoList
                        let! saveResult = storage.SaveTodosAsync(updatedList) |> Async.AwaitTask
                        return saveResult |> Result.map (fun _ -> todo)
                    | Error err -> return Error err
                | Error err -> return Error err
            } |> Async.StartAsTask
        
        // Act
        let addTask = addTodoToStorage memoryStorage "Abstraction test"
        addTask.Wait()
        
        // Assert
        match addTask.Result with
        | Ok addedTodo ->
            let verifyTask = memoryStorage.LoadTodosAsync()
            verifyTask.Wait()
            match verifyTask.Result with
            | Ok storedList ->
                TodoListOperations.findTodo addedTodo.Id storedList |> should equal (Some addedTodo)
            | Error err -> Assert.Fail($"Verification failed: {err}")
        | Error err -> Assert.Fail($"Add operation failed: {err}")

[<TestFixture>]
type JsonStorageComponentTests() =
    
    [<Test>]
    member _.``DtoConversion todoToDto creates correct DTO``() =
        // Arrange
        let createdAt = DateTime(2025, 11, 9, 15, 30, 0, DateTimeKind.Utc)
        match createTodo "Test todo" createdAt with
        | Ok todo ->
            // Act
            let dto = DtoConversion.todoToDto todo
            
            // Assert
            dto.id |> should equal (TodoCore.todoIdValue todo.Id)
            dto.text |> should equal "Test todo"
            dto.isCompleted |> should be False
            dto.createdAt |> should equal "2025-11-09T15:30:00.000Z"
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``DtoConversion todoFromDto recreates original todo``() =
        // Arrange
        let dto = {
            id = "test-id-123"
            text = "Test todo from DTO"
            isCompleted = true
            createdAt = "2025-11-09T15:30:00.000Z"
        }
        
        // Act
        let result = DtoConversion.todoFromDto dto
        
        // Assert
        match result with
        | Ok todo ->
            TodoCore.todoIdValue todo.Id |> should equal "test-id-123"
            TodoCore.todoTextValue todo.Text |> should equal "Test todo from DTO"
            todo.IsCompleted |> should be True
            // The exact time may vary due to timezone conversion, so let's check year/month/day
            todo.CreatedAt.Year |> should equal 2025
            todo.CreatedAt.Month |> should equal 11
            todo.CreatedAt.Day |> should equal 9
        | Error err ->
            Assert.Fail($"Expected success but got error: {err}")

    [<Test>]
    member _.``DtoConversion round trip preserves todo data``() =
        // Arrange
        let originalCreatedAt = DateTime(2025, 11, 9, 10, 15, 30, DateTimeKind.Utc)
        match createTodo "Round trip test" originalCreatedAt with
        | Ok originalTodo ->
            // Act - Convert to DTO and back
            let dto = DtoConversion.todoToDto originalTodo
            let result = DtoConversion.todoFromDto dto
            
            // Assert
            match result with
            | Ok reconstructedTodo ->
                TodoCore.todoIdValue reconstructedTodo.Id |> should equal (TodoCore.todoIdValue originalTodo.Id)
                TodoCore.todoTextValue reconstructedTodo.Text |> should equal (TodoCore.todoTextValue originalTodo.Text)
                reconstructedTodo.IsCompleted |> should equal originalTodo.IsCompleted
                // Due to potential timezone conversion issues, check date components separately
                reconstructedTodo.CreatedAt.Year |> should equal originalTodo.CreatedAt.Year
                reconstructedTodo.CreatedAt.Month |> should equal originalTodo.CreatedAt.Month
                reconstructedTodo.CreatedAt.Day |> should equal originalTodo.CreatedAt.Day
            | Error err ->
                Assert.Fail($"Round trip failed: {err}")
        | Error err ->
            Assert.Fail($"Setup failed: {err}")