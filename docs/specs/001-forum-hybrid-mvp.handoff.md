# 001 handoff — Forum Hybrid MVP

- **기준:** `origin/main` `8ce42ef` (W6 CLI 머지, PR #6)
- **작성:** Weld W7 · 2026-09-23 KST
- **범위:** 문서만. 제품 코드 변경 없음.

---

## Build

`dotnet build DiscordForumOps.sln` (`Directory.Build.props` `TreatWarningsAsErrors=true`)

| 항목 | 결과 |
|---|---|
| 결과 | **pass** |
| 경고 | 0 |
| 오류 | 0 |
| 산출 | Forum.Core / Discord / Worker / Cli + Core.Tests / Discord.Tests |

---

## Tests

`dotnet test DiscordForumOps.sln` — **20 passed / 0 failed**, 경고 0.

| 프로젝트 | 통과 | 실패 |
|---|---|---|
| Forum.Core.Tests | 14 | 0 |
| Forum.Discord.Tests | 6 | 0 |

§5 필수 이름 전부 green:

- `Cursor_advances_only_forward` (Core InMemory + Discord File)
- `Cursor_survives_roundtrip` (Core InMemory + Discord File)
- `Poll_skips_non_mention`
- `Poll_invokes_handler_on_mention`
- `Reader_uses_after_cursor`
- `Publisher_create_post_sends_thread_name_and_persona`
- `Publisher_reply_uses_thread_id`
- `RateLimit_429_retries_once`
- `Secrets_never_logged`

Weld 추가분(적응형 간격·Worker 틱)도 green.

---

## Smoke

선호 경로: `dotnet run --project src/Forum.Cli -- smoke-post --dry-run`

| 항목 | 결과 |
|---|---|
| dry-run | **pass** (exit 0). publisher 미호출. `secretsConfigured=false`, `readerConfigured=false`, `forumChannelId=unset` |
| live smoke-post | **skipped** |
| R2 (`wait=true` 응답 `id` == `channel_id`) | **skipped** — 일치 여부 미검증 |

`Documents\Sensitive\mcp-secrets.env` 파일은 있음. 스펙 키 `DISCORD_BOT_TOKEN` / `DISCORD_FORUM_WEBHOOK_URL` 은 없음. 다른 Discord 웹훅 키는 있으나 포럼 웹훅이 아니므로 대체 사용하지 않음. 값·URL 기록 없음.

CLI live `CreatePostAsync`는 메시지 id만 반환. 시크릿이 생겨도 R2 비교는 `?wait=true` JSON의 `id` vs `channel_id`가 필요.

---

## §8 구현 체크리스트

설계 항목은 스펙 본문에 이미 [x].

- [x] `dotnet build` TreatWarningsAsErrors 성공 (경고 0 / 오류 0)
- [x] §5 테스트 전부 green (Core 14 + Discord 6)
- [x] 스모크 dry-run 성공. live / R2는 스펙 시크릿 키 부재로 skipped
- [x] 이 handoff에 빌드·테스트·스모크·후속 요약

W1–W6 (`main@8ce42ef`):

- [x] W1 솔루션 스캐폴딩 + Directory.* + docs
- [x] W2 Forum.Core + Core.Tests
- [x] W3 Forum.Discord Reader/Publisher + WireMock
- [x] W4 시크릿 로더 + 로그 스크러버
- [x] W5 Forum.Worker 적응형 폴링
- [x] W6 Forum.Cli whoami / smoke-post / smoke-reply / poll-once
- [x] W7 본 문서

---

## BLOCKED

없음. 설계 변경이 필요한 스펙 충돌은 없음.

---

## 후속 (상태만)

| ID | 상태 |
|---|---|
| R2 | **skipped / 미검증.** 스펙 웹훅·봇 토큰 키 없음. live `id` vs `channel_id` 비교 못 함 |
| R4 | 미결. 상시 가동·스케줄러는 002 |
| R5 | 미결. knowledge-rag MCP (Lore/Reed) |
| R6 | 미결. 웹훅 로테이션 1줄 (Reed) |
