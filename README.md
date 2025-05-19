# Sunset

**Sunset** is a custom, Turing-complete programming language developed in C#. It features a comprehensive parser and compiler built from the ground up, capable of generating standalone executable files. Designed with cross-platform compatibility in mind, Sunset can adapt to various operating systems, with primary testing conducted on Windows.

## Features

- **Custom Compiler and Parser**: Built entirely in C#, Sunset includes a self-developed parser and compiler, showcasing a deep understanding of language design and implementation.
- **Turing-Complete Language**: Sunset supports a full range of computational operations, affirming its Turing completeness.
- **Standalone Executable Generation**: The compiler can produce standalone executable files, allowing programs written in Sunset to run independently without additional dependencies.
- **Cross-Platform Design**: While development and testing were primarily on Windows, Sunset's architecture is adaptable, aiming for compatibility across different operating systems.
- **Syntax Highlighting Support**: An EmEditor syntax file is included to enhance code readability and editing efficiency.
- **Self-Implemented Libraries**: Sunset’s standard libraries are written in the Sunset language itself, demonstrating the language's expressive power and internal consistency.

## Getting Started

To explore the Sunset programming language:

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/jakefrey0/Sunset.git
   ```

2. **Open the Solution**:

   Open `Sunset.sln` in Visual Studio to examine the source code and understand the compiler's structure.

3. **Build the Project**:

   Build the solution to compile the Sunset compiler.

4. **Write and Compile Sunset Code**:

   Create your own `.sunset` files and use the compiler to generate standalone executables. In command line you would run `sunset main_file_path.sunset`

## Syntax Highlighting

For improved code editing, an EmEditor syntax file is available:

```
Sunset/bin/Debug/Sunset_Syntax.esy
```

Import this file into EmEditor to enable syntax highlighting for Sunset code.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## Example Project

[Example application written in Sunset (2022/08/10)](https://github.com/cashsignsesh/Simple-RPG)
![Syntax example](https://github.com/cashsignsesh/Sunset/blob/main/Sunset/bin/Debug/Capture.PNG)
