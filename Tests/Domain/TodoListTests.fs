module CleanCodeTodoApp.Tests.Domain.TodoListTests

open System
open NUnit.Framework
open FsUnit
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations
open CleanCodeTodoApp.Domain.TodoListOperations

/// <summary>
/// Tests for TodoList aggregate operations.
/// Validates immutable operations on todo collections.
/// </summary>
[<TestFixture>]
type TodoListTests() =

    [<Test>]
    member _.``addTodo to empty list succeeds``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todoResult = createTodo "Test todo" DateTime.UtcNow
        
        // Act & Assert
        match todoResult with
        | Ok todo ->
            let updatedList = addTodo todo emptyList
            let (totalCount, _) = countTodos updatedList
            totalCount |> should equal 1
            findTodo todo.Id updatedList |> should equal (Some todo)
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``updateTodo with existing ID succeeds``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todoResult = createTodo "Original text" DateTime.UtcNow
        
        match todoResult with
        | Ok originalTodo ->
            let listWithTodo = addTodo originalTodo emptyList
            let updateFn = fun todo -> { todo with Text = ValidText "Updated text"; IsCompleted = true }
            
            // Act
            let result = updateTodo originalTodo.Id updateFn listWithTodo
            
            // Assert
            match result with
            | Ok updatedList ->
                let retrievedTodo = findTodo originalTodo.Id updatedList
                match retrievedTodo with
                | Some todo ->
                    TodoCore.todoTextValue todo.Text |> should equal "Updated text"
                    todo.IsCompleted |> should be True
                | None -> Assert.Fail("Todo not found after update")
            | Error err ->
                Assert.Fail($"Expected success but got error: {err}")
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``updateTodo with non-existing ID fails``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let nonExistentId = TodoId "non-existent"
        let updateFn = fun todo -> { todo with IsCompleted = true }
        
        // Act
        let result = updateTodo nonExistentId updateFn emptyList
        
        // Assert
        match result with
        | Error (TodoNotFound _) -> () // Expected
        | Error other -> Assert.Fail($"Expected TodoNotFound but got {other}")
        | Ok _ -> Assert.Fail("Expected Error but got Ok")

    [<Test>]
    member _.``filterCompleted returns only completed todos``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todo1Result = createTodo "Completed task 1" (DateTime.UtcNow.AddDays(-1.0))
        let todo2Result = createTodo "Incomplete task" DateTime.UtcNow
        let todo3Result = createTodo "Completed task 2" (DateTime.UtcNow.AddHours(-2.0))
        
        match todo1Result, todo2Result, todo3Result with
        | Ok todo1, Ok todo2, Ok todo3 ->
            let completedTodo1 = completeTodo todo1
            let incompleteTodo = todo2  // Leave as is
            let completedTodo2 = completeTodo todo3
            
            let todoList = 
                emptyList
                |> addTodo completedTodo1
                |> addTodo incompleteTodo
                |> addTodo completedTodo2
            
            // Act
            let completedTodos = filterCompleted true todoList |> Seq.filter (fun t -> t.IsCompleted) |> Seq.toList
            
            // Assert
            completedTodos |> should haveLength 2
            completedTodos |> should contain completedTodo1
            completedTodos |> should contain completedTodo2
            completedTodos |> List.exists (fun t -> t.Id = incompleteTodo.Id) |> should be False
        | _ ->
            Assert.Fail("Setup failed")

    [<Test>]
    member _.``filterCompleted returns only incomplete todos when showCompleted is false``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todo1Result = createTodo "Completed task" (DateTime.UtcNow.AddDays(-1.0))
        let todo2Result = createTodo "Incomplete task 1" DateTime.UtcNow
        let todo3Result = createTodo "Incomplete task 2" (DateTime.UtcNow.AddHours(-1.0))
        
        match todo1Result, todo2Result, todo3Result with
        | Ok todo1, Ok todo2, Ok todo3 ->
            let completedTodo = completeTodo todo1
            let incompleteTodo1 = todo2  // Leave as is
            let incompleteTodo2 = todo3  // Leave as is
            
            let todoList = 
                emptyList
                |> addTodo completedTodo
                |> addTodo incompleteTodo1
                |> addTodo incompleteTodo2
            
            // Act
            let incompleteTodos = filterCompleted false todoList |> Seq.toList
            
            // Assert
            incompleteTodos |> should haveLength 2
            incompleteTodos |> should contain incompleteTodo1
            incompleteTodos |> should contain incompleteTodo2
            incompleteTodos |> List.exists (fun t -> t.Id = completedTodo.Id) |> should be False
        | _ ->
            Assert.Fail("Setup failed")

    [<Test>]
    member _.``getAllTodos returns all todos in order``() =
        // Arrange
        let firstDate = DateTime(2025, 11, 9, 10, 0, 0)
        let secondDate = DateTime(2025, 11, 9, 11, 0, 0)
        let thirdDate = DateTime(2025, 11, 9, 12, 0, 0)
        
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todo1Result = createTodo "First todo" firstDate
        let todo2Result = createTodo "Second todo" secondDate
        let todo3Result = createTodo "Third todo" thirdDate
        
        match todo1Result, todo2Result, todo3Result with
        | Ok todo1, Ok todo2, Ok todo3 ->
            let todoList = 
                emptyList
                |> addTodo todo1
                |> addTodo todo2
                |> addTodo todo3
            
            // Act
            let allTodos = getAllTodos todoList |> Seq.toList
            
            // Assert
            allTodos |> should haveLength 3
            // Should be ordered by creation date
            allTodos.[0].CreatedAt |> should equal firstDate
            allTodos.[1].CreatedAt |> should equal secondDate
            allTodos.[2].CreatedAt |> should equal thirdDate
        | _ ->
            Assert.Fail("Setup failed")

    [<Test>]
    member _.``countTodos returns correct counts``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todo1Result = createTodo "Completed task" DateTime.UtcNow
        let todo2Result = createTodo "Incomplete task" DateTime.UtcNow
        
        match todo1Result, todo2Result with
        | Ok todo1, Ok todo2 ->
            let completedTodo = completeTodo todo1
            let incompleteTodo = todo2
            
            let todoList = 
                emptyList
                |> addTodo completedTodo
                |> addTodo incompleteTodo
            
            // Act
            let (totalCount, completedCount) = countTodos todoList
            
            // Assert
            totalCount |> should equal 2
            completedCount |> should equal 1
        | _ ->
            Assert.Fail("Setup failed")