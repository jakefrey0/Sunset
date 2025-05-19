# Sunset

**Sunset** is a custom-built, Turing-complete programming language developed in C#. It features a fully self-written tokenizer, parser, and compiler capable of generating standalone executable files. Designed as both an educational tool and a serious compiler project, Sunset demonstrates deep knowledge of language design, compiler architecture, and low-level code generation.

---

## Features

- **Self-Written Compiler and Parser**: Built entirely in C#, Sunset tokenizes and parses source code without external libraries. It compiles to native assembly and produces runnable `.exe` files.
- **Turing-Complete Language**: Supports conditionals, loops, variables, arrays, structs, functions, and more.
- **Standalone Executable Generation**: Sunset can compile your `.sunset` files into native Windows executables — no runtime needed.
- **Cross-Platform Design**: Designed with cross-platform compatibility in mind; current builds and testing focused on Windows.
- **Self-Implemented Standard Library**: Sunset's standard functions are written in Sunset itself, proving the language is expressive enough to support real-world usage.
- **Assembly Output Logic**: The compiler emits clean, structured assembly to support logic like arrays, string handling, loops, functions, and classes.
- **Syntax Highlighting Support**: Comes with an EmEditor `.esy` file for syntax highlighting.

---

## Example Code

```sunset
let count = 5;
while (count > 0) {
    print(count);
    count = count - 1;
}
```

---

## Compiler Architecture

Sunset is composed of several compiler stages:

- **Tokenizer**: Parses source text character by character into tokens (identifiers, numbers, symbols, etc.)
- **Parser**: Converts tokens into abstract syntax trees, handling precedence and nesting
- **Code Generator**: Emits custom assembly logic for a stack-based execution model
- **Executable Builder**: Compiles final assembly into `.exe` files

Assembly logic covers:

- Array handling
- Class and struct logic
- Control flow (if/while/foreach)
- String manipulation
- Function parameters and local variables
- Stack instruction execution

All available under `/Assembly logic/`.

---

## Getting Started

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/jakefrey0/Sunset.git
   ```

2. **Open the Solution**:

   Open `Sunset.sln` in Visual Studio.

3. **Build the Project**:

   Build the solution to generate the Sunset compiler.

4. **Write & Compile Sunset Code**:

   Create `.sunset` source files and compile them:
   ```bash
   sunset your_file.sunset
   ```

---

## Syntax Highlighting

Use EmEditor? Import the syntax file for Sunset code coloring:

```
Sunset/bin/Debug/Sunset_Syntax.esy
```

---

## Example Project

Explore an early Sunset program:
- [Simple RPG in Sunset (2022/08/10)](https://github.com/cashsignsesh/Simple-RPG)

Preview:
![Syntax example](https://github.com/cashsignsesh/Sunset/blob/main/Sunset/bin/Debug/Capture.PNG)

---

## Roadmap

- [x] Custom compiler in C#  
- [x] Executable output (.exe)  
- [x] Core features: loops, arrays, structs, functions  
- [ ] Type system improvements  
- [ ] Runtime error reporting  
- [ ] Basic IDE or CLI debugger

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## Author

Created and maintained by [Jake Frey](https://github.com/jakefrey0)
