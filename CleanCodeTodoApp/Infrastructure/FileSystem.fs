namespace CleanCodeTodoApp.Infrastructure

open System.IO
open System.Threading.Tasks
open CleanCodeTodoApp.Domain

/// <summary>
/// File system abstraction interface.
/// Enables dependency injection and testing with mocked file operations.
/// </summary>
type IFileSystem =
    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The file path to check</param>
    /// <returns>True if file exists, false otherwise</returns>
    abstract member FileExists: path: string -> bool
    
    /// <summary>
    /// Reads all text from a file asynchronously.
    /// </summary>
    /// <param name="path">The file path to read</param>
    /// <returns>Task containing the file content as string</returns>
    abstract member ReadAllTextAsync: path: string -> Task<string>
    
    /// <summary>
    /// Writes all text to a file asynchronously.
    /// Creates directories if they don't exist.
    /// </summary>
    /// <param name="path">The file path to write to</param>
    /// <param name="content">The content to write</param>
    /// <returns>Task representing the completion of the operation</returns>
    abstract member WriteAllTextAsync: path: string * content: string -> Task<unit>
    
    /// <summary>
    /// Gets the directory name from a file path.
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>The directory path</returns>
    abstract member GetDirectoryName: path: string -> string
    
    /// <summary>
    /// Creates a directory if it doesn't exist.
    /// </summary>
    /// <param name="path">The directory path to create</param>
    /// <returns>Unit</returns>
    abstract member CreateDirectory: path: string -> unit

/// <summary>
/// Real file system implementation.
/// Uses actual System.IO operations for production use.
/// </summary>
type RealFileSystem() =
    interface IFileSystem with
        member _.FileExists(path: string) : bool =
            File.Exists(path)
        
        member _.ReadAllTextAsync(path: string) : Task<string> =
            File.ReadAllTextAsync(path)
        
        member _.WriteAllTextAsync(path: string, content: string) : Task<unit> =
            task {
                let dir = Path.GetDirectoryName(path)
                if not (Directory.Exists(dir)) then
                    Directory.CreateDirectory(dir) |> ignore
                
                do! File.WriteAllTextAsync(path, content)
            }
        
        member _.GetDirectoryName(path: string) : string =
            Path.GetDirectoryName(path)
        
        member _.CreateDirectory(path: string) : unit =
            if not (Directory.Exists(path)) then
                Directory.CreateDirectory(path) |> ignore

/// <summary>
/// File system operations with error handling.
/// Wraps IFileSystem operations with Result types for clean error handling.
/// </summary>
module FileSystemOperations =
    
    /// <summary>
    /// Safely reads a file with error handling.
    /// Wraps exceptions in TodoError.StorageError.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use</param>
    /// <param name="path">The file path to read</param>
    /// <returns>Async Result containing file content or error</returns>
    let readFileAsync (fileSystem: IFileSystem) (path: string) : Async<TodoResult<string>> =
        async {
            try
                let! content = fileSystem.ReadAllTextAsync(path) |> Async.AwaitTask
                return Ok content
            with
            | ex -> return Error (StorageError $"Failed to read file '{path}': {ex.Message}")
        }
    
    /// <summary>
    /// Safely writes a file with error handling.
    /// Wraps exceptions in TodoError.StorageError.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use</param>
    /// <param name="path">The file path to write to</param>
    /// <param name="content">The content to write</param>
    /// <returns>Async Result indicating success or error</returns>
    let writeFileAsync (fileSystem: IFileSystem) (path: string) (content: string) : Async<TodoResult<unit>> =
        async {
            try
                do! fileSystem.WriteAllTextAsync(path, content) |> Async.AwaitTask
                return Ok ()
            with
            | ex -> return Error (StorageError $"Failed to write file '{path}': {ex.Message}")
        }
    
    /// <summary>
    /// Checks if a file exists safely.
    /// Pure operation that doesn't throw exceptions.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use</param>
    /// <param name="path">The file path to check</param>
    /// <returns>True if file exists, false otherwise</returns>
    let fileExists (fileSystem: IFileSystem) (path: string) : bool =
        try
            fileSystem.FileExists(path)
        with
        | _ -> false