# Changelog
All changes to the project are documented in this file.

## [V1.1.2] - 2025/03/20
### Fixed
- **Software Units**: Variables in Global and Instance DBs are now preceded by the namespace instead of the SW Unit name.

    Add-In versions < V1.1.2: `"SwUnitName.DbName"."Variable"`.
    Add-In versions >= V1.1.2: `"DbNamespace.DbName"."Variable"`.

## [V1.1.1] - 2024/12/09
### Added
- **Strings of length defined by Constant**: Strings where the length is defined by a constant are now supported. In previous versions, they were handled as an unknown data type.

    Example: `String [CONST]`.


## [V1.1.0] - 2024/12/02
### Added
- **Software Units**: A new action allows the creation of user-modeled interfaces for Software Units. To launch this action, right-click on the desired Software Unit and select "Create server interface for SW Unit".

    NOTE: This option can only be executed with the .dll from TIA Portal V19.
- **Multi-dimensional arrays**: Support for arrays with multiple dimensions, valueRank >= 1.

    Example: `Array [0..5, 2..3, 1..10] of Bool`.

### Changed
- **Log message format**: Improved readability of messages displayed in the log file.

### Fixed
- Crash when processing arrays with variable limits. An exception is now added, and a message is displayed in the log file to inform the user that the array has not been included in the server interface.

    Example: `Array [*] of Bool`.

- Crash caused by unsupported data types inside structs. Unsupported data types were omitted in V1.0.0, but caused the add-in to crash when they were located inside structs.