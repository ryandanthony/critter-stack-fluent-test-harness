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
  dotnet test --solution {{solution}} --no-build --configuration Release --report-xunit-trx --results-directory TestResults

# Copy the harness into a throwaway service (no repo build props) and run that service's scenarios (requires Docker).
portability:
  ./scripts/check-portability.sh
