# Sims4 Support Roadmap (v1)

Policy: this file is a checklist mirror only; GitHub issues hold scope, implementation detail, and closure evidence.

This roadmap stays on the Sims support-home boundary. Shared contract wording follows `Api.GameServerInterop`, and the base mod remains outside this repo.

## Mirror Checklist

- [x] `Phase 1` contract hardening. Source: completed GitHub issue chain.
- [x] `Phase 2` game-side base mod exposure. Source: completed GitHub issue chain.
- [x] `Phase 3` smoke and test system. Source: completed GitHub issue chain.
- [x] `Phase 4` stream influence events. Source: completed GitHub issue chain.
- [x] `Phase 5` V3 capability and control truth. Source: completed GitHub issue chain.
- [x] `#10` V2 deployment prerequisite. Source: [GitHub issue](https://github.com/lancer1977/Sims4API/issues/10)

## Minimal Metadata

- repo: `Sims4-Support`
- support-home boundary: support home lives here; base mod stays separate
- local build: `dotnet build Api.Sims4.sln`
- local test: `dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal`
