namespace CleanCodeTodoApp.Domain

open System
open CleanCodeTodoApp.Domain

/// <summary>
/// TodoList aggregate that manages a collection of todos.
/// Uses Map for efficient lookups and maintains immutability.
/// </summary>
type TodoList = {
    /// Collection of todos indexed by TodoId for O(log n) lookups
    Todos: Map<TodoId, Todo>
    /// When this todo list was last modified (UTC)
    LastModified: DateTime
}

/// <summary>
/// Operations on TodoList aggregate.
/// All functions are pure and return new instances rather than mutating existing ones.
/// </summary>
module TodoListOperations =

    /// <summary>
    /// Creates an empty todo list.
    /// Pure function that initializes empty state.
    /// </summary>
    /// <param name="createdAt">When the todo list was created</param>
    /// <returns>Empty TodoList</returns>
    let createEmptyTodoList (createdAt: DateTime) : TodoList =
        {
            Todos = Map.empty
            LastModified = createdAt
        }

    /// <summary>
    /// Adds a todo to the list.
    /// Pure function - returns new TodoList with added todo.
    /// </summary>
    /// <param name="todo">The todo to add</param>
    /// <param name="todoList">The todo list to add to</param>
    /// <returns>TodoList with todo added</returns>
    let addTodo (todo: Todo) (todoList: TodoList) : TodoList =
        {
            Todos = Map.add todo.Id todo todoList.Todos
            LastModified = DateTime.UtcNow
        }

    /// <summary>
    /// Updates a specific todo in the list using an update function.
    /// Pure function - returns Result with new TodoList or error if todo not found.
    /// </summary>
    /// <param name="todoId">The ID of the todo to update</param>
    /// <param name="updateFn">Function to apply to the todo</param>
    /// <param name="todoList">The todo list to update</param>
    /// <returns>Result containing updated TodoList or TodoError</returns>
    let updateTodo (todoId: TodoId) (updateFn: Todo -> Todo) (todoList: TodoList) : TodoResult<TodoList> =
        match Map.tryFind todoId todoList.Todos with
        | Some todo ->
            let updatedTodo = updateFn todo
            let updatedTodos = Map.add todoId updatedTodo todoList.Todos
            Ok { 
                Todos = updatedTodos
                LastModified = DateTime.UtcNow 
            }
        | None -> 
            Error (TodoNotFound todoId)

    /// <summary>
    /// Removes a todo from the list.
    /// Pure function - returns new TodoList with todo removed.
    /// If todo doesn't exist, returns unchanged list (idempotent).
    /// </summary>
    /// <param name="todoId">The ID of the todo to remove</param>
    /// <param name="todoList">The todo list to remove from</param>
    /// <returns>TodoList with todo removed</returns>
    let removeTodo (todoId: TodoId) (todoList: TodoList) : TodoList =
        {
            Todos = Map.remove todoId todoList.Todos
            LastModified = DateTime.UtcNow
        }

    /// <summary>
    /// Finds a todo by ID.
    /// Pure function that returns Option type for safe access.
    /// </summary>
    /// <param name="todoId">The ID to search for</param>
    /// <param name="todoList">The todo list to search in</param>
    /// <returns>Some Todo if found, None otherwise</returns>
    let findTodo (todoId: TodoId) (todoList: TodoList) : Todo option =
        Map.tryFind todoId todoList.Todos

    /// <summary>
    /// Filters todos by completion status.
    /// Pure function that returns sequence of matching todos.
    /// </summary>
    /// <param name="showCompleted">Whether to show completed (true) or incomplete (false) todos</param>
    /// <param name="todoList">The todo list to filter</param>
    /// <returns>Sequence of todos matching the filter</returns>
    let filterCompleted (showCompleted: bool) (todoList: TodoList) : Todo seq =
        todoList.Todos
        |> Map.values
        |> Seq.filter (fun todo -> todo.IsCompleted = showCompleted)

    /// <summary>
    /// Counts total and completed todos.
    /// Pure function that returns tuple of (total, completed).
    /// </summary>
    /// <param name="todoList">The todo list to count</param>
    /// <returns>Tuple of (total count, completed count)</returns>
    let countTodos (todoList: TodoList) : int * int =
        let allTodos = Map.values todoList.Todos |> Seq.toList
        let totalCount = List.length allTodos
        let completedCount = allTodos |> List.filter (fun t -> t.IsCompleted) |> List.length
        (totalCount, completedCount)

    /// <summary>
    /// Gets all todos as a sequence ordered by creation date.
    /// Pure function for iteration over todos.
    /// </summary>
    /// <param name="todoList">The todo list to get todos from</param>
    /// <returns>Sequence of all todos ordered by creation date</returns>
    let getAllTodos (todoList: TodoList) : Todo seq =
        todoList.Todos
        |> Map.values
        |> Seq.sortBy (fun todo -> todo.CreatedAt)