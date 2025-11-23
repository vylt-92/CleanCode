# Implementation Plan: Clean Code Todo Demonstration App

**Branch**: `001-clean-todo-demo` | **Date**: 2025-11-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-clean-todo-demo/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a simple F# todo application that demonstrates clean code best practices including functional-first design, module separation, pure functions, dependency injection, and comprehensive error handling using Result/Option types. The application will be a command-line tool that loads/saves data from JSON files and includes a comprehensive checklist documenting all demonstrated clean code practices.

## Technical Context

**Language/Version**: F# 8.0 with .NET 8.0  
**Primary Dependencies**: FSharp.Core, Newtonsoft.Json or System.Text.Json, FsUnit for testing  
**Storage**: Local JSON file persistence (no database)  
**Testing**: NUnit with FsUnit, FsCheck for property-based testing  
**Target Platform**: Cross-platform console application (.NET 8.0)
**Project Type**: Single console application with modular F# library structure  
**Performance Goals**: Sub-second response time for all operations, minimal memory footprint  
**Constraints**: No external database, terminal-only interface, demonstrate clean code practices  
**Scale/Scope**: Single-user todo list with basic CRUD operations, <1000 todos expected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Functional-First Design ✅
- Application will use immutable data structures for Todo items and TodoList
- All business logic will be implemented as pure functions
- Mutable state limited to I/O boundaries (file operations)

### Type-Driven Development ✅  
- Domain entities (Todo, TodoList) will be modeled with discriminated unions and records
- All functions will have explicit type signatures
- Illegal states (empty todo text, invalid IDs) will be unrepresentable through types

### Module-First Architecture ✅
- Clear module separation: Domain, Storage, CLI, Application
- Each module will have single responsibility
- Public APIs will be minimal and well-documented

### Pipeline-Oriented Programming ✅
- Data transformations will use |> and >> operators
- Complex operations broken into composable functions
- Left-to-right data flow for readability

### Comprehensive Testing ✅
- Unit tests for all pure functions
- Property-based tests for domain logic
- Integration tests for file I/O operations
- Target >90% coverage for business logic

**GATE RESULT**: ✅ PASS - All constitutional requirements can be met with this architecture

## Project Structure

### Documentation (this feature)

```text
specs/001-clean-todo-demo/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
CleanCodeTodoApp/
├── CleanCodeTodoApp.fsproj        # F# project file
├── Program.fs                     # Application entry point
├── Domain/
│   ├── Todo.fs                   # Todo domain model and pure functions
│   └── TodoList.fs               # TodoList aggregate and operations
├── Infrastructure/
│   ├── JsonStorage.fs            # JSON file persistence implementation
│   └── FileSystem.fs             # File system abstractions
├── Application/
│   ├── TodoService.fs            # Application service layer
│   └── Commands.fs               # Command handlers
├── CLI/
│   ├── Parser.fs                 # Command line argument parsing
│   ├── Display.fs                # Output formatting and display
│   └── Workflow.fs               # CLI workflow orchestration
└── data/
    └── todos.json                # Sample/default todo data

Tests/
├── Tests.fsproj                  # Test project file
├── Domain/
│   ├── TodoTests.fs              # Domain model tests
│   └── TodoListTests.fs          # TodoList operation tests
├── Application/
│   └── TodoServiceTests.fs       # Service layer tests
├── Infrastructure/
│   └── JsonStorageTests.fs       # Storage integration tests
└── CLI/
    └── ParserTests.fs            # CLI parsing tests

docs/
└── CleanCodeChecklist.md         # Generated clean code practices checklist
```

### Dependencies

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="NUnit" Version="3.14.0" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
<PackageReference Include="FsUnit" Version="5.6.1" />
<PackageReference Include="FsCheck" Version="2.16.5" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
```

## Implementation Phases

### Phase 0: Architecture Research ✅ COMPLETE
All technical decisions resolved. Ready for Phase 1.

### Phase 1: Core Design & Contracts
- Design domain models and data structures
- Define module interfaces and contracts  
- Create sample JSON data
- Generate development quickstart guide

### Phase 2: Implementation Planning
Will be handled by `/speckit.tasks` command after Phase 1 completion.

## Post-Design Constitution Re-evaluation

*GATE: Final check after Phase 1 design completion.*

### Functional-First Design ✅ CONFIRMED
- Domain types use immutable records and discriminated unions
- All business operations implemented as pure functions
- Pipeline operators used throughout for data flow
- No mutable state in domain layer

### Type-Driven Development ✅ CONFIRMED  
- TodoId, TodoText prevent illegal states
- Result types explicit in all function signatures
- Domain model makes invalid states unrepresentable
- All public functions have explicit type annotations

### Module-First Architecture ✅ CONFIRMED
- Clear separation: Domain, Infrastructure, Application, CLI
- Each module has single responsibility
- Minimal public APIs defined in contracts
- Dependencies flow inward to domain core

### Pipeline-Oriented Programming ✅ CONFIRMED
- Command processing uses pipeline composition
- Error handling chains with Result.bind
- Data transformations use |> operators
- Nested function calls avoided

### Comprehensive Testing ✅ CONFIRMED
- Unit tests for all pure functions planned
- Property-based tests for domain invariants
- Integration tests for I/O boundaries
- >90% coverage target for business logic

### Additional Requirements ✅ CONFIRMED
- Error handling uses Result/Option exclusively
- JSON serialization separate from domain types
- Dependency injection at application boundaries
- Comprehensive documentation with examples

**FINAL GATE RESULT**: ✅ PASS - All constitutional requirements satisfied by final design

## Ready for Implementation

✅ **Phase 0**: Research completed - All technical decisions resolved
✅ **Phase 1**: Design completed - Domain model, contracts, and quickstart ready
🔄 **Phase 2**: Ready for `/speckit.tasks` command to generate implementation tasks

**Artifacts Generated**:
- [research.md](research.md) - Technical decisions and rationale
- [data-model.md](data-model.md) - Domain types and validation rules
- [contracts/module-interfaces.md](contracts/module-interfaces.md) - Public APIs and interfaces
- [quickstart.md](quickstart.md) - Development setup and guidelines

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
