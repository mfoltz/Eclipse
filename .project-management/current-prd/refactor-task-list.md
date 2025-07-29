# Task List: Refactor Continuation

These tasks break down the work described in `refactor-prd.md`.

- [x] **Compile Audit** - Run `dotnet build` and record all warnings and errors. _(Owner: Lead Developer, Due: 2025-07-31)_
  - Build output shows **9** warnings and **3** errors related to missing `Prefabs` references and unused fields.
- [ ] **Fix Build Errors** - Resolve the `Prefabs` references in `Recipes.cs` and any other compiler errors. _(Owner: Lead Developer, Due: 2025-08-03)_
- [ ] **Clean Warnings** - Enable `#nullable` in `DataParsers.cs` and handle unused fields in `CanvasService.cs`. _(Owner: Contributor, Due: 2025-08-03)_
- [ ] **Apply SRP & Documentation** - Review each service and utility for single responsibility and add XML comments to guide total refactor. _(Owner: Lead Developer, Due: 2025-08-10)_
- [ ] **Encapsulate Patterns & Responsibilities** - Act on comments from previous task and review AGENTS.md for other principles that should be adhered to and made manifest. _(Owner: Lead Developer, Due: 2025-08-10)_
- [ ] **Organize Directories** - Ensure files reside in logical folders such as `Services/` and `Utilities/`. _(Owner: Contributor, Due: 2025-08-10)_
- [ ] **Add Unit Tests** - Create a test project and cover major services and data parsers. _(Owner: Contributor, Due: 2025-08-15)_
- [ ] **Verify Clean Build** - Confirm the project builds without warnings or errors and all tests pass. _(Owner: Lead Developer, Due: 2025-08-15)_

