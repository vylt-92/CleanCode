module CleanCodeTodoApp.Tests.Domain.TodoTests

open System
open NUnit.Framework
open FsUnit
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations

/// <summary>
/// Tests for Todo domain operations.
/// Following TDD principles - tests written first to define expected behavior.
/// </summary>
[<TestFixture>]
type TodoTests() =

    [<Test>]
    member _.``createTodo with valid text succeeds``() =
        // Arrange
        let validText = "Learn F# functional programming"
        let createdAt = DateTime(2025, 11, 9, 10, 0, 0, DateTimeKind.Utc)
        
        // Act
        let result = createTodo validText createdAt
        
        // Assert
        match result with
        | Ok todo ->
            TodoCore.todoTextValue todo.Text |> should equal validText
            todo.IsCompleted |> should be False
            todo.CreatedAt |> should equal createdAt
        | Error _ -> 
            Assert.Fail("Expected Ok result but got Error")

    [<Test>]
    member _.``createTodo with empty text fails``() =
        // Arrange
        let emptyText = ""
        let createdAt = DateTime.UtcNow
        
        // Act
        let result = createTodo emptyText createdAt
        
        // Assert
        match result with
        | Error (InvalidTodoText _) -> () // Expected error type
        | Error other -> Assert.Fail($"Expected InvalidTodoText but got {other}")
        | Ok _ -> Assert.Fail("Expected Error result but got Ok")

    [<Test>]
    member _.``createTodo with whitespace-only text fails``() =
        // Arrange
        let whitespaceText = "   "
        let createdAt = DateTime.UtcNow
        
        // Act
        let result = createTodo whitespaceText createdAt
        
        // Assert
        match result with
        | Error (InvalidTodoText _) -> () // Expected error type
        | Error other -> Assert.Fail($"Expected InvalidTodoText but got {other}")
        | Ok _ -> Assert.Fail("Expected Error result but got Ok")

    [<Test>]
    member _.``toggleTodoCompletion changes status``() =
        // Arrange
        let todo = {
            Id = TodoId "test-id"
            Text = ValidText "Test todo"
            IsCompleted = false
            CreatedAt = DateTime.UtcNow
        }
        
        // Act
        let toggledOnce = toggleTodoCompletion todo
        let toggledTwice = toggleTodoCompletion toggledOnce
        
        // Assert
        toggledOnce.IsCompleted |> should be True
        toggledTwice.IsCompleted |> should be False
        
        // Verify other properties remain unchanged
        toggledOnce.Id |> should equal todo.Id
        toggledOnce.Text |> should equal todo.Text
        toggledOnce.CreatedAt |> should equal todo.CreatedAt

    [<Test>]
    member _.``completeTodo marks todo as completed``() =
        // Arrange
        let todo = {
            Id = TodoId "test-id"
            Text = ValidText "Test todo"
            IsCompleted = false
            CreatedAt = DateTime.UtcNow
        }
        
        // Act
        let completed = completeTodo todo
        
        // Assert
        completed.IsCompleted |> should be True
        completed.Id |> should equal todo.Id
        completed.Text |> should equal todo.Text

    [<Test>]
    member _.``uncompleteTodo marks todo as incomplete``() =
        // Arrange
        let todo = {
            Id = TodoId "test-id"
            Text = ValidText "Test todo"
            IsCompleted = true
            CreatedAt = DateTime.UtcNow
        }
        
        // Act
        let uncompleted = uncompleteTodo todo
        
        // Assert
        uncompleted.IsCompleted |> should be False
        uncompleted.Id |> should equal todo.Id
        uncompleted.Text |> should equal todo.Text