open System
open CleanCodeTodoApp.Domain
open CleanCodeTodoApp.Domain.TodoOperations
open CleanCodeTodoApp.Domain.TodoListOperations
open CleanCodeTodoApp.Infrastructure

/// CLI commands supported by the application
type CliCommand =
    | Create of text: string
    | List
    | Toggle of todoId: string
    | Remove of todoId: string
    | Save
    | Load  
    | Status
    | Help
    | Quit
    | Invalid of message: string

/// State to maintain between CLI commands - now with storage
type CliState = {
    Storage: ITodoStorage
    IsRunning: bool
}

/// Parse user input into CLI command
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
    | [|"remove"; id|] | [|"delete"; id|] -> Remove id
    | [|"save"|] -> Save
    | [|"load"|] -> Load
    | [|"status"|] -> Status
    | [|"help"|] -> Help
    | [|"quit"|] | [|"exit"|] -> Quit
    | _ -> Invalid "Unknown command. Type 'help' for available commands."

/// Display help information
let showHelp () : unit =
    printfn ""
    printfn "=== Clean Code Todo App ==="
    printfn "A functional F# application demonstrating clean code practices"
    printfn ""
    printfn "COMMANDS:"
    printfn ""
    printfn "📝 TODO MANAGEMENT:"
    printfn "  create <text>  - Create a new todo item"
    printfn "                   Example: create Buy groceries for dinner"
    printfn ""
    printfn "  list           - Show all todos with completion status"
    printfn "                   Displays: [✓] for completed, [ ] for pending"
    printfn ""
    printfn "  toggle <id>    - Toggle completion status of a todo"
    printfn "                   Example: toggle a1b2c3 (use first few ID characters)"
    printfn ""
    printfn "  remove <id>    - Remove a todo from the list"
    printfn "                   Example: remove a1b2c3 (also accepts 'delete')"
    printfn ""
    printfn "💾 DATA MANAGEMENT:"
    printfn "  save           - Manually save todos to file (auto-save also active)"
    printfn ""
    printfn "  load           - Reload todos from file storage"
    printfn ""
    printfn "  status         - Check storage status and todo statistics"
    printfn ""
    printfn "ℹ️  INFORMATION:"
    printfn "  help           - Show this help information"
    printfn ""
    printfn "  quit           - Exit the application (also accepts 'exit')"
    printfn ""
    printfn "EXAMPLES:"
    printfn "  > create Learn F# functional programming"
    printfn "  ✓ Todo created: Learn F# functional programming"
    printfn ""
    printfn "  > list"
    printfn "  === Your Todos ==="
    printfn "  1. [ ] Learn F# functional programming (ID: a1b2c3d4)"
    printfn ""
    printfn "  > toggle a1b2"
    printfn "  ✓ Todo marked as completed: Learn F# functional programming"
    printfn ""
    printfn "  > status"
    printfn "  ✓ Storage is available and ready"
    printfn "    📊 Current todos: 1 total, 1 completed"
    printfn ""
    printfn "TIPS:"
    printfn "  • You can use partial IDs (first few characters) for toggle/remove"
    printfn "  • Todos are automatically saved to 'todos.json' in the current directory"
    printfn "  • Type 'quit' or 'exit' to close the application"
    printfn "  • All operations are type-safe with comprehensive error handling"
    printfn ""

/// Display todo list in a readable format
let displayTodoList (todoList: TodoList) : unit =
    let allTodos = getAllTodos todoList |> Seq.toList
    
    match allTodos with
    | [] -> 
        printfn "No todos found. Create one with: create <text>"
    | todos ->
        printfn ""
        printfn "=== Your Todos ==="
        todos
        |> List.iteri (fun index todo ->
            let status = if todo.IsCompleted then "[✓]" else "[ ]"
            let idShort = TodoCore.todoIdValue todo.Id |> fun id -> id.Substring(0, Math.Min(8, id.Length))
            let text = TodoCore.todoTextValue todo.Text
            printfn "%d. %s %s (ID: %s)" (index + 1) status text idShort)
        printfn ""

/// Execute a CLI command with storage operations
let executeCommandAsync (command: CliCommand) (state: CliState) : System.Threading.Tasks.Task<CliState> =
    async {
        match command with
        | Create text ->
            match createTodo text DateTime.UtcNow with
            | Ok todo ->
                let operation = fun todoList -> Ok (addTodo todo todoList)
                let! result = TodoStorageOperations.withStorageTransactionAsync state.Storage operation |> Async.AwaitTask
                match result with
                | Ok _ ->
                    printfn "✓ Todo created: %s" (TodoCore.todoTextValue todo.Text)
                    return state
                | Error err ->
                    printfn "✗ Storage error: %A" err
                    return state
            | Error (InvalidTodoText msg) ->
                printfn "✗ Error: %s" msg
                return state
            | Error other ->
                printfn "✗ Unexpected error: %A" other
                return state
        
        | List ->
            let! loadResult = TodoStorageOperations.loadTodosAsync state.Storage |> Async.AwaitTask
            match loadResult with
            | Ok todoList ->
                displayTodoList todoList
                return state
            | Error err ->
                printfn "✗ Failed to load todos: %A" err
                return state
        
        | Toggle todoId ->
            let! loadResult = TodoStorageOperations.loadTodosAsync state.Storage |> Async.AwaitTask
            match loadResult with
            | Ok todoList ->
                let allTodos = getAllTodos todoList |> Seq.toList
                let matchingTodo = 
                    allTodos 
                    |> List.tryFind (fun todo -> 
                        let fullId = TodoCore.todoIdValue todo.Id
                        fullId.StartsWith(todoId, StringComparison.OrdinalIgnoreCase))
                
                match matchingTodo with
                | Some todo ->
                    let operation = fun todoList ->
                        let updatedTodo = toggleTodoCompletion todo
                        let updateFn = fun _ -> updatedTodo
                        updateTodo todo.Id updateFn todoList
                    
                    let! result = TodoStorageOperations.withStorageTransactionAsync state.Storage operation |> Async.AwaitTask
                    match result with
                    | Ok _ ->
                        let status = if (toggleTodoCompletion todo).IsCompleted then "completed" else "incomplete"
                        printfn "✓ Todo marked as %s: %s" status (TodoCore.todoTextValue todo.Text)
                        return state
                    | Error err ->
                        printfn "✗ Storage error: %A" err
                        return state
                | None ->
                    printfn "✗ No todo found with ID starting with: %s" todoId
                    return state
            | Error err ->
                printfn "✗ Failed to load todos: %A" err
                return state
        
        | Remove todoId ->
            let! loadResult = TodoStorageOperations.loadTodosAsync state.Storage |> Async.AwaitTask
            match loadResult with
            | Ok todoList ->
                let allTodos = getAllTodos todoList |> Seq.toList
                let matchingTodo = 
                    allTodos 
                    |> List.tryFind (fun todo -> 
                        let fullId = TodoCore.todoIdValue todo.Id
                        fullId.StartsWith(todoId, StringComparison.OrdinalIgnoreCase))
                
                match matchingTodo with
                | Some todo ->
                    let operation = fun todoList -> Ok (removeTodo todo.Id todoList)
                    let! result = TodoStorageOperations.withStorageTransactionAsync state.Storage operation |> Async.AwaitTask
                    match result with
                    | Ok _ ->
                        printfn "✓ Todo removed: %s" (TodoCore.todoTextValue todo.Text)
                        return state
                    | Error err ->
                        printfn "✗ Storage error: %A" err
                        return state
                | None ->
                    printfn "✗ No todo found with ID starting with: %s" todoId
                    return state
            | Error err ->
                printfn "✗ Failed to load todos: %A" err
                return state
        
        | Save ->
            let! loadResult = TodoStorageOperations.loadTodosAsync state.Storage |> Async.AwaitTask
            match loadResult with
            | Ok todoList ->
                let! saveResult = TodoStorageOperations.saveTodosAsync state.Storage todoList |> Async.AwaitTask
                match saveResult with
                | Ok _ ->
                    printfn "✓ Todos saved to file successfully"
                    return state
                | Error err ->
                    printfn "✗ Failed to save todos: %A" err
                    return state
            | Error err ->
                printfn "✗ Failed to load current todos: %A" err
                return state
        
        | Load ->
            let! loadResult = TodoStorageOperations.loadTodosAsync state.Storage |> Async.AwaitTask
            match loadResult with
            | Ok todoList ->
                displayTodoList todoList
                printfn "✓ Todos loaded from file"
                return state
            | Error err ->
                printfn "✗ Failed to load todos: %A" err
                return state
        
        | Status ->
            let! isAvailable = TodoStorageOperations.isStorageAvailableAsync state.Storage |> Async.AwaitTask
            if isAvailable then
                printfn "✓ Storage is available and ready"
                let! loadResult = TodoStorageOperations.loadTodosAsync state.Storage |> Async.AwaitTask
                match loadResult with
                | Ok todoList ->
                    let (totalCount, completedCount) = countTodos todoList
                    printfn "  📊 Current todos: %d total, %d completed" totalCount completedCount
                    return state
                | Error err ->
                    printfn "  ⚠️  Storage available but failed to load: %A" err
                    return state
            else
                printfn "✗ Storage is not available"
                return state
        
        | Help ->
            showHelp ()
            return state
        
        | Quit ->
            printfn "Goodbye!"
            return { state with IsRunning = false }
        
        | Invalid message ->
            printfn "✗ %s" message
            return state
    } |> Async.StartAsTask

/// Main CLI loop with async operations
let rec cliLoopAsync (state: CliState) : System.Threading.Tasks.Task<unit> =
    async {
        if state.IsRunning then
            printf "> "
            let input = Console.ReadLine()
            let command = parseCommand input
            let! newState = executeCommandAsync command state |> Async.AwaitTask
            return! cliLoopAsync newState |> Async.AwaitTask
    } |> Async.StartAsTask

/// Application entry point
[<EntryPoint>]
let main argv =
    async {
        printfn "Welcome to Todo App!"
        printfn "Type 'help' for available commands."
        
        // Create storage instance (using JSON file storage)
        let fileSystem = RealFileSystem() :> IFileSystem
        let currentDir = System.Environment.CurrentDirectory
        let todoFilePath = System.IO.Path.Combine(currentDir, "todos.json")
        let jsonStorage = JsonTodoStorage(fileSystem, todoFilePath)
        let storage = jsonStorage :> ITodoStorage
        
        let initialState = {
            Storage = storage
            IsRunning = true
        }
        
        do! cliLoopAsync initialState |> Async.AwaitTask
        return 0 // Return success exit code
    } |> Async.RunSynchronously
