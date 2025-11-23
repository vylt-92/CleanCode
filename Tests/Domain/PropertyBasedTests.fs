module CleanCodeTodoApp.Tests.Domain.PropertyBasedTests

open System
open NUnit.Framework
open FsUnit
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations
open CleanCodeTodoApp.Domain.TodoListOperations

/// <summary>
/// Property-based style tests for domain invariants.
/// Tests properties that should always hold true.
/// </summary>
[<TestFixture>]
type PropertyBasedTests() =

    let validTexts = ["a"; "test"; "todo item"; "Learn F#"; "Write tests"; "Special chars éñglîsh"]
    let invalidTexts = [null; ""; "   "; "\t"; "\n"]

    [<Test>]
    member _.``createTodo with valid texts always succeeds``() =
        // Act & Assert
        validTexts
        |> List.iter (fun text ->
            let createdAt = DateTime.UtcNow
            match createTodo text createdAt with
            | Ok todo ->
                TodoCore.todoTextValue todo.Text |> should equal (text.Trim())
                todo.IsCompleted |> should be False
                todo.CreatedAt |> should equal createdAt
            | Error _ -> Assert.Fail($"Expected success for valid text: {text}"))

    [<Test>]
    member _.``createTodo with invalid texts always fails``() =
        // Act & Assert
        invalidTexts
        |> List.iter (fun invalidText ->
            let createdAt = DateTime.UtcNow
            match createTodo invalidText createdAt with
            | Error (InvalidTodoText _) -> () // Expected
            | _ -> Assert.Fail($"Expected failure for invalid text: {invalidText}"))

    [<Test>]
    member _.``toggleTodoCompletion is involutive property``() =
        // Act & Assert
        validTexts
        |> List.iter (fun text ->
            match createTodo text DateTime.UtcNow with
            | Ok todo ->
                let toggledTwice = todo |> toggleTodoCompletion |> toggleTodoCompletion
                toggledTwice |> should equal todo
            | Error _ -> Assert.Fail($"Setup failed for text: {text}"))

    [<Test>]
    member _.``addTodo increases count by one property``() =
        // Act & Assert
        validTexts
        |> List.take 3 // Use first 3 to keep test fast
        |> List.iter (fun text ->
            let emptyList = createEmptyTodoList DateTime.UtcNow
            let (initialTotal, _) = countTodos emptyList
            
            match createTodo text DateTime.UtcNow with
            | Ok todo ->
                let updatedList = addTodo todo emptyList
                let (newTotal, _) = countTodos updatedList
                newTotal |> should equal (initialTotal + 1)
            | Error _ -> Assert.Fail($"Setup failed for text: {text}"))

    [<Test>]
    member _.``todo operations preserve identity property``() =
        // Act & Assert
        validTexts
        |> List.take 2 // Use first 2 to keep test fast
        |> List.iter (fun text ->
            let createdAt = DateTime.UtcNow
            match createTodo text createdAt with
            | Ok todo ->
                let completed = completeTodo todo
                let uncompleted = uncompleteTodo todo
                let toggled = toggleTodoCompletion todo
                
                // ID should never change
                completed.Id |> should equal todo.Id
                uncompleted.Id |> should equal todo.Id
                toggled.Id |> should equal todo.Id
                
                // Text should never change
                completed.Text |> should equal todo.Text
                uncompleted.Text |> should equal todo.Text
                toggled.Text |> should equal todo.Text
                
                // CreatedAt should never change
                completed.CreatedAt |> should equal todo.CreatedAt
                uncompleted.CreatedAt |> should equal todo.CreatedAt
                toggled.CreatedAt |> should equal todo.CreatedAt
            | Error _ -> Assert.Fail($"Setup failed for text: {text}"))

    [<Test>]
    member _.``findTodo after addTodo always succeeds property``() =
        // Act & Assert
        validTexts
        |> List.take 3
        |> List.iter (fun text ->
            let emptyList = createEmptyTodoList DateTime.UtcNow
            
            match createTodo text DateTime.UtcNow with
            | Ok todo ->
                let listWithTodo = addTodo todo emptyList
                match findTodo todo.Id listWithTodo with
                | Some foundTodo -> foundTodo |> should equal todo
                | None -> Assert.Fail("Todo should be found after adding")
            | Error _ -> Assert.Fail($"Setup failed for text: {text}"))

    [<Test>]
    member _.``getAllTodos count equals countTodos total property``() =
        // Arrange
        let mutable currentList = createEmptyTodoList DateTime.UtcNow
        let testTexts = validTexts |> List.take 3
        
        // Add todos
        testTexts
        |> List.iteri (fun i text ->
            match createTodo text (DateTime.UtcNow.AddMinutes(float i)) with
            | Ok todo ->
                currentList <- addTodo todo currentList
            | Error _ -> Assert.Fail($"Setup failed for text: {text}"))
        
        // Act & Assert
        let allTodos = getAllTodos currentList |> Seq.toList
        let (totalCount, _) = countTodos currentList
        
        allTodos.Length |> should equal totalCount
        totalCount |> should equal testTexts.Length

    [<Test>]
    member _.``filter properties - completed plus incomplete equals total``() =
        // Arrange
        let mutable currentList = createEmptyTodoList DateTime.UtcNow
        let testTexts = ["First todo"; "Second todo"; "Third todo"; "Fourth todo"]
        
        // Add todos with alternating completion status
        testTexts
        |> List.iteri (fun i text ->
            match createTodo text (DateTime.UtcNow.AddMinutes(float i)) with
            | Ok todo ->
                let finalTodo = if i % 2 = 0 then completeTodo todo else todo
                currentList <- addTodo finalTodo currentList
            | Error _ -> Assert.Fail($"Setup failed for text: {text}"))
        
        // Act
        let allTodos = getAllTodos currentList |> Seq.toList
        let completedTodos = filterCompleted true currentList |> Seq.toList
        let incompleteTodos = filterCompleted false currentList |> Seq.toList
        
        // Assert - All completed + incomplete should equal all todos
        let combinedCount = completedTodos.Length + incompleteTodos.Length
        combinedCount |> should equal allTodos.Length
        allTodos.Length |> should equal testTexts.Length

    [<Test>]
    member _.``immutability property - operations never modify original``() =
        // Arrange
        let text = "Test immutability"
        match createTodo text DateTime.UtcNow with
        | Ok originalTodo ->
            let originalCompleted = originalTodo.IsCompleted
            let originalText = originalTodo.Text
            let originalId = originalTodo.Id
            let originalCreatedAt = originalTodo.CreatedAt
            
            // Act - Perform various operations
            let _ = completeTodo originalTodo
            let _ = uncompleteTodo originalTodo
            let _ = toggleTodoCompletion originalTodo
            
            // Assert - Original todo is unchanged
            originalTodo.IsCompleted |> should equal originalCompleted
            originalTodo.Text |> should equal originalText
            originalTodo.Id |> should equal originalId
            originalTodo.CreatedAt |> should equal originalCreatedAt
        | Error err ->
            Assert.Fail($"Setup failed: {err}")

    [<Test>]
    member _.``associative property for multiple todo operations``() =
        // Arrange
        let texts = ["First"; "Second"; "Third"]
        let mutable list1 = createEmptyTodoList DateTime.UtcNow
        let mutable list2 = createEmptyTodoList DateTime.UtcNow
        
        // Create todos
        let todos = 
            texts 
            |> List.mapi (fun i text ->
                match createTodo text (DateTime.UtcNow.AddMinutes(float i)) with
                | Ok todo -> todo
                | Error _ -> failwith "Setup failed")
        
        // Act - Add todos in different orders
        // List 1: Add in original order
        todos |> List.iter (fun todo -> list1 <- addTodo todo list1)
        
        // List 2: Add in reverse order  
        todos |> List.rev |> List.iter (fun todo -> list2 <- addTodo todo list2)
        
        // Assert - Both lists should have same count and contain same todos
        let (count1, _) = countTodos list1
        let (count2, _) = countTodos list2
        count1 |> should equal count2
        count1 |> should equal todos.Length
        
        // All todos should be findable in both lists
        todos |> List.iter (fun todo ->
            findTodo todo.Id list1 |> should equal (Some todo)
            findTodo todo.Id list2 |> should equal (Some todo))