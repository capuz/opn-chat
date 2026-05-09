# Skill Registry

**Delegator use only.** Any agent that launches sub-agents reads this registry to resolve compact rules, then injects them directly into sub-agent prompts. Sub-agents do NOT read this registry or individual SKILL.md files.

See `_shared/skill-resolver.md` for the full resolution protocol.

## User Skills

| Trigger | Skill | Path |
|---------|-------|------|
| "judgment day", "judgment-day", "review adversarial", "dual review", "doble review", "juzgar", "que lo juzguen" | judgment-day | C:\Users\fcapuz\.claude\skills\judgment-day\SKILL.md |
| Writing Go tests, using teatest, adding test coverage, Bubbletea TUI testing | go-testing | C:\Users\fcapuz\.claude\skills\go-testing\SKILL.md |
| Creating a new skill, add agent instructions, document patterns for AI | skill-creator | C:\Users\fcapuz\.claude\skills\skill-creator\SKILL.md |
| Creating a pull request, opening a PR, preparing changes for review | branch-pr | C:\Users\fcapuz\.claude\skills\branch-pr\SKILL.md |
| Creating a GitHub issue, reporting a bug, requesting a feature | issue-creation | C:\Users\fcapuz\.claude\skills\issue-creation\SKILL.md |

## Compact Rules

Pre-digested rules per skill. Delegators copy matching blocks into sub-agent prompts as `## Project Standards (auto-resolved)`.

### judgment-day
- Launch TWO judges in parallel (async/delegate) — never sequential, never review yourself as orchestrator
- Each judge is blind — no cross-contamination, identical target, identical criteria
- Classify every WARNING: real (normal user can trigger it) or theoretical (contrived/edge case) — theoretical = INFO only, never fixed
- Confirmed = both judges agree; Suspect = one judge only; Contradiction = judges disagree on same issue
- Fix Agent is a SEPARATE delegation — never reuse a judge as the fixer
- After fixes, re-launch BOTH judges in parallel (Round 2)
- After 2 fix iterations, ASK the user before continuing — never escalate automatically
- APPROVED = 0 confirmed CRITICALs + 0 confirmed real WARNINGs (theoretical warnings and suggestions may remain)
- Before launching judges, resolve skills from registry and inject matching compact rules into ALL judge + fix-agent prompts
- NEVER push/commit after fixes until re-judgment completes

### go-testing
- Use table-driven tests: `tests := []struct{name, input, expected, wantErr}{...}` → `for _, tt := range tests { t.Run(tt.name, ...) }`
- Test Bubbletea Model state changes directly via `m.Update(tea.KeyMsg{...})`
- Use `teatest.NewTestModel` for full interactive TUI flows; send keys with `tm.Send()`
- Golden file testing: compare `m.View()` output against `testdata/TestName.golden`; update with `-update` flag
- Mock system-level deps by injecting structs, not patching globals
- Use `t.TempDir()` for file operations; `-short` flag skips integration tests
- One test file per source file: `model_test.go`, `update_test.go`, `view_test.go`

### skill-creator
- Skill lives in `skills/{skill-name}/SKILL.md` with required frontmatter: name, description (includes Trigger:), license: Apache-2.0, metadata.author, metadata.version
- Description MUST include `Trigger:` keywords — that's how the registry matches it
- Start SKILL.md with Critical Patterns — most important rules first, no lengthy intros
- Keep code examples minimal and focused; no troubleshooting sections
- Use `assets/` for templates/schemas; `references/` for local file paths only — no web URLs
- After creating, add entry to `AGENTS.md` registry table
- Do NOT create skills for one-off tasks or patterns already covered by existing docs

### branch-pr
- Every PR MUST link an approved issue via `Closes #N` in body — blank PRs are blocked by GitHub Actions
- Every PR MUST have exactly one `type:*` label
- Branch naming must match: `^(feat|fix|chore|docs|style|refactor|perf|test|build|ci|revert)\/[a-z0-9._-]+$`
- Conventional commits: `type(scope): description` — no `Co-Authored-By` trailers
- Run `shellcheck scripts/*.sh` before pushing
- PR body MUST include: linked issue, PR type checkbox, summary bullets, changes table, test plan, contributor checklist
- Linked issue MUST have `status:approved` label before PR can be opened
- Add the matching `type:*` label to the PR after creation

### issue-creation
- MUST use bug_report.yml or feature_request.yml template — blank issues are disabled
- Every issue gets `status:needs-review` automatically on creation; `status:approved` must be added by a maintainer before any PR can reference it
- Questions go to Discussions, NOT issues
- Search for duplicates first: `gh issue list --search "keyword"`
- Bug report required fields: description, steps to reproduce, expected vs actual behavior, OS, agent/client, shell
- Feature request required fields: problem description, proposed solution, affected area
- Commit title format: `type(scope): description` matching the issue type

## Project Conventions

| File | Path | Notes |
|------|------|-------|
| — | — | No project-level convention files found (no CLAUDE.md, AGENTS.md, .cursorrules, GEMINI.md in project root) |
