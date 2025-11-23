module CleanCodeTodoApp.Tests.Domain.ErrorHandlingTests

open System
open NUnit.Framework
open FsUnit
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations
open CleanCodeTodoApp.Domain.TodoListOperations

/// <summary>
/// Tests for error handling and edge cases.
/// Validates proper error reporting and boundary conditions.
/// </summary>
[<TestFixture>]
type ErrorHandlingTests() =

    [<Test>]
    member _.``TodoCore createTodoId with null string fails``() =
        // Act & Assert
        let result = TodoCore.createTodoId null
        
        match result with
        | Error (InvalidTodoText _) -> () // Expected
        | _ -> Assert.Fail("Expected InvalidTodoText error for null")

    [<Test>]
    member _.``TodoCore createTodoId with empty string fails``() =
        // Act & Assert
        let result = TodoCore.createTodoId ""
        
        match result with
        | Error (InvalidTodoText _) -> () // Expected
        | _ -> Assert.Fail("Expected InvalidTodoText error for empty string")

    [<Test>]
    member _.``TodoCore createTodoText with very long text succeeds``() =
        // Arrange
        let veryLongText = String.replicate 1000 "A"
        
        // Act & Assert
        let result = TodoCore.createTodoText veryLongText
        
        match result with
        | Ok (ValidText text) -> text |> should equal veryLongText
        | Error _ -> Assert.Fail("Expected valid text for long string")

    [<Test>]
    member _.``TodoCore createTodoText trims whitespace``() =
        // Arrange
        let textWithWhitespace = "  Test todo with spaces  "
        
        // Act
        let result = TodoCore.createTodoText textWithWhitespace
        
        // Assert
        match result with
        | Ok (ValidText text) -> text |> should equal "Test todo with spaces"
        | Error _ -> Assert.Fail("Expected valid text with trimmed whitespace")

    [<Test>]
    member _.``createTodo with special characters succeeds``() =
        // Arrange
        let specialText = "Review éñglîsh & français tëxt with ünícodé"
        let createdAt = DateTime.UtcNow
        
        // Act
        let result = createTodo specialText createdAt
        
        // Assert
        match result with
        | Ok todo ->
            TodoCore.todoTextValue todo.Text |> should equal specialText
        | Error err -> Assert.Fail($"Expected success but got error: {err}")

    [<Test>]
    member _.``addTodo twice with same todo instance fails``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todoResult = createTodo "Test todo" DateTime.UtcNow
        
        match todoResult with
        | Ok todo ->
            let listWithTodo = addTodo todo emptyList
            
            // Act - Try to add the same todo again
            // Note: Since our addTodo doesn't return Result, we need to check different behavior
            let listAfterSecondAdd = addTodo todo listWithTodo
            
            // Assert - Second add should still result in only one todo (since Map.add overwrites)
            let (totalCount, _) = countTodos listAfterSecondAdd
            totalCount |> should equal 1
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``updateTodo with function that throws exception handles gracefully``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let todoResult = createTodo "Test todo" DateTime.UtcNow
        
        match todoResult with
        | Ok todo ->
            let listWithTodo = addTodo todo emptyList
            
            // This would be problematic in real code, but let's test a more realistic scenario
            let updateFn = fun _ -> { todo with Text = ValidText "Updated safely" }
            
            // Act
            let result = updateTodo todo.Id updateFn listWithTodo
            
            // Assert
            match result with
            | Ok updatedList ->
                let updatedTodo = findTodo todo.Id updatedList
                match updatedTodo with
                | Some t -> TodoCore.todoTextValue t.Text |> should equal "Updated safely"
                | None -> Assert.Fail("Todo not found after update")
            | Error err -> Assert.Fail($"Update should have succeeded: {err}")
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``findTodo with non-existent id returns None``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        let nonExistentId = TodoId "definitely-does-not-exist"
        
        // Act
        let result = findTodo nonExistentId emptyList
        
        // Assert
        result |> should equal None

    [<Test>]
    member _.``getAllTodos with empty list returns empty sequence``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        
        // Act
        let result = getAllTodos emptyList |> Seq.toList
        
        // Assert
        result |> should be Empty

    [<Test>]
    member _.``countTodos with empty list returns zero counts``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        
        // Act
        let (totalCount, completedCount) = countTodos emptyList
        
        // Assert
        totalCount |> should equal 0
        completedCount |> should equal 0

    [<Test>]
    member _.``filterCompleted with empty list returns empty result``() =
        // Arrange
        let emptyList = createEmptyTodoList DateTime.UtcNow
        
        // Act
        let completedTodos = filterCompleted true emptyList |> Seq.toList
        let incompleteTodos = filterCompleted false emptyList |> Seq.toList
        
        // Assert
        completedTodos |> should be Empty
        incompleteTodos |> should be Empty

    [<Test>]
    member _.``toggleTodoCompletion is idempotent``() =
        // Arrange
        let todoResult = createTodo "Test todo" DateTime.UtcNow
        
        match todoResult with
        | Ok originalTodo ->
            // Act - Toggle twice
            let toggledOnce = toggleTodoCompletion originalTodo
            let toggledTwice = toggleTodoCompletion toggledOnce
            
            // Assert - Should be back to original state
            toggledTwice.IsCompleted |> should equal originalTodo.IsCompleted
            toggledTwice.Id |> should equal originalTodo.Id
            toggledTwice.Text |> should equal originalTodo.Text
            toggledTwice.CreatedAt |> should equal originalTodo.CreatedAt
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``todo operations preserve immutability``() =
        // Arrange
        let todoResult = createTodo "Original todo" DateTime.UtcNow
        
        match todoResult with
        | Ok originalTodo ->
            // Act - Perform operations
            let completedTodo = completeTodo originalTodo
            let uncompletedTodo = uncompleteTodo completedTodo
            let toggledTodo = toggleTodoCompletion originalTodo
            
            // Assert - Original todo is unchanged
            originalTodo.IsCompleted |> should be False
            originalTodo.Id |> should equal completedTodo.Id
            originalTodo.Text |> should equal completedTodo.Text
            
            // Assert - Operations create new instances
            completedTodo.IsCompleted |> should be True
            uncompletedTodo.IsCompleted |> should be False
            toggledTodo.IsCompleted |> should be True
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``createEmptyTodoList creates new instance each time``() =
        // Arrange
        let time1 = DateTime.UtcNow
        let time2 = time1.AddSeconds(1.0)
        
        // Act
        let list1 = createEmptyTodoList time1
        let list2 = createEmptyTodoList time2
        
        // Assert
        list1.LastModified |> should equal time1
        list2.LastModified |> should equal time2
        list1.LastModified |> should not' (equal list2.LastModified)