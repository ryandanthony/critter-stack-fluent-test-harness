set shell := ["bash", "-eu", "-o", "pipefail", "-c"]

solution := "src/CritterStack.TestHarness.slnx"

# Show all available recipes.
default:
  just --list

# Restore dependencies for the whole solution.
restore:
  dotnet restore {{solution}}

# Build all projects in Release mode.
build: restore
  dotnet build {{solution}} --no-restore --configuration Release

# Run all tests in Release mode (requires Docker for Testcontainers).
test: build
  dotnet test --solution {{solution}} --no-build --configuration Release --report-trx
