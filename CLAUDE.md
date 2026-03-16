# Hydra Framework — Project Instructions

## Architecture
This project uses the Hydra autonomous development pipeline:

```
Objective → Discovery (+ Classification) → Doc Generator → Planner → Implementer → Review Board → Loop → Post-Loop → Complete
```

## Key Files
- `hydra/config.json` — Runtime configuration (mode, iteration, reviewers)
- `hydra/plan.md` — Live task tracker with Recovery Pointer
- `hydra/tasks/` — Individual task manifests with full state
- `hydra/checkpoints/` — Per-iteration JSON snapshots
- `hydra/reviews/` — Review artifacts per task
- `hydra/context/` — Project context from Discovery
- `hydra/docs/` — Generated project documents (PRD, TRD, ADRs)

## Commands
- `/hydra-init` — First-time setup: run Discovery, generate reviewers, create runtime dirs
- `/hydra-start` — Auto-detects project state and continues or starts work (derives objective from context)
- `/hydra-start "objective"` — Override: start a specific new objective (flags: --afk, --yolo, --hitl, --max N)
- `/hydra-status` — Check loop progress, task counts, reviewer board
- `/hydra-task` — Task lifecycle: skip, unblock, add, prioritize, info, list
- `/hydra-pause` — Gracefully pause the loop at next iteration boundary
- `/hydra-log` — View run history: iterations, commits, reviews, blockers
- `/hydra-reset` — Archive current state and reset to clean slate
- `/hydra-clean` — Fully remove Hydra from this project
- `/hydra-review` — Manually trigger review for a task
- `/hydra-discover` — Re-run codebase discovery
- `/hydra-context` — Collect targeted context for an objective type
- `/hydra-simplify` — Post-loop cleanup pass on modified files
- `/hydra-docs` — View or regenerate project documents
- `/hydra-patch` — Lightweight task mode for bug fixes, config changes, and small features
- `/hydra-verify` — Human acceptance testing for completed tasks
- `/hydra-help` — Quick reference for all commands and modes

## The 10 Rules for the Hydra Loop

1. **Always read plan.md first.** The Recovery Pointer tells you exactly where you are.
2. **Files are truth, context is ephemeral.** Write state to files immediately. Never rely on conversational memory.
3. **Follow existing patterns exactly.** Read the pattern reference before implementing. Match the style.
4. **Tests are mandatory.** Every task includes tests. Run them after every change. Don't proceed if failing.
5. **Never skip the review gate.** ALL reviewers must approve. No exceptions.
6. **Address ALL blocking feedback.** When CHANGES_REQUESTED, fix every REJECT item.
7. **One task at a time.** Don't work on multiple tasks simultaneously (unless parallel mode with Agent Teams).
8. **Update Recovery Pointer on every state change.** This is how you survive compaction.
9. **Commit in AFK mode.** Every state transition gets a commit with the dynamic prefix from config (e.g., `feat(FEAT-003):`).
10. **HYDRA_COMPLETE means ALL tasks DONE and post-loop finished.** Not before.

## Git Convention
- The default branch is `main`. All Hydra branch operations (stacking, PRs, merges) target `main`.

## Safety
- Pre-tool guard blocks destructive commands (rm -rf /, DROP TABLE, etc.)
- Security rejections in AFK mode → BLOCKED (requires human review)
- Max retries per task: 3 (configurable in config.json)
- Max consecutive failures: 5 (auto-stops if no progress)

## Recovery
After any interruption (compaction, crash, timeout):
1. Read `hydra/plan.md` → Recovery Pointer
2. Read latest checkpoint in `hydra/checkpoints/`
3. Read active task manifest in `hydra/tasks/`
4. Resume from the Next Action
