# Changelog
All changes to the project are documented in this file.

## [V1.1.0] - 2024/12/02
### Added
- **Software Units**: A new action allows the creation of user-modeled interfaces for Software Units. To launch this action, right-click on the desired Software Unit and select "Create server interface for SW Unit".
- **Multi-dimensional arrays**: Support for arrays with multiple dimensions, valueRank >= 1.

    Example: `Array [0..5, 2..3, 1..10] of Bool`.

### Changed
- **Log message format**: Improved readability of messages displayed in the log file.

### Fixed
- Crash when processing arrays with variable limits. An exception is now added, and a message is displayed in the log file to inform the user that the array has not been included in the server interface.

    Example: `Array [*] of Bool`.

- Crash caused by unsupported data types inside structs. Unsupported data types were omitted in V1.0.0, but caused the add-in to crash when they were located inside a struct.