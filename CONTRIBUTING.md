# Contributing to Prose — AI Grammar Corrector

Thank you for your interest in contributing to the Grammar Corrector project! We welcome contributions from the community.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/AI-Grammar-Correction-Tool.git
   cd GrammarCorrector
   ```
3. **Create a feature branch** for your work:
   ```bash
   git checkout -b feature/description-of-feature
   ```

## Development Setup

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Configure `appsettings.json` with your local settings (database, Stripe keys, etc.)
4. Create the database:
   ```bash
   dotnet ef database update
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

## Code Style Guidelines

- Follow standard C# naming conventions (PascalCase for classes/methods, camelCase for variables)
- Use meaningful variable and function names
- Add comments for complex logic
- Keep methods focused and reasonably sized
- Use async/await for I/O operations
- Ensure null safety with nullable reference types

## Submitting Changes

1. **Make your changes** with clear, descriptive commits:
   ```bash
   git commit -m "Add feature: description"
   ```
2. **Push to your fork**:
   ```bash
   git push origin feature/description-of-feature
   ```
3. **Create a Pull Request** on GitHub with a clear description of:
   - What problem does it solve?
   - How does it work?
   - Any breaking changes?
   - Testing recommendations

## Pull Request Guidelines

- Keep PRs focused on a single feature or bug fix
- Include relevant issue numbers (e.g., "Fixes #123")
- Update documentation if needed
- Add comments explaining non-obvious code
- Ensure the project builds without errors: `dotnet build`
- Test your changes thoroughly before submitting

## Reporting Issues

When reporting a bug, please include:
- Clear description of the issue
- Steps to reproduce
- Expected vs. actual behavior
- Your environment (OS, .NET version, etc.)
- Screenshots or error messages if applicable

## Areas for Contribution

- **Bug Fixes** – Help fix reported issues
- **Feature Enhancements** – Add new grammar rules or UI improvements
- **Documentation** – Improve or add documentation
- **Performance** – Optimize slow operations
- **Tests** – Add unit or integration tests
- **Translation** – Add support for new languages

## Code Review Process

All submissions undergo review:
- Maintainers will review your code for quality, style, and functionality
- You may be asked to make changes or improvements
- Once approved, your contribution will be merged

## Questions?

Feel free to:
- Open a GitHub Issue for questions or clarification
- Discuss ideas before submitting large changes
- Ask for help if you get stuck

---

Thank you for contributing to make Prose better! 🎉
