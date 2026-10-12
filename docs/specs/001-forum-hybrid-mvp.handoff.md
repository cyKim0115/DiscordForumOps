# 001 handoff — Forum Hybrid MVP

- **기준:** `cursor/persona-avatar-map` (origin/main `8f5581f` + 페르소나 아바타 맵 / 시크릿 ID 폴백 / 페르소나별 웹훅)
- **작성:** 2026-10-12
- **범위:** 웹훅 persona 아바타 리졸버, CLI `--avatar-url` / `--persona-name`, 맵 `forum_webhook_url`, `DISCORD_BOT_USER_ID`·`DISCORD_FORUM_CHANNEL_ID` 시크릿 폴백, 테스트, 본 문서.

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

`dotnet test DiscordForumOps.sln` — **33 passed / 0 failed**, 경고 0.

| 프로젝트 | 통과 | 실패 |
|---|---|---|
| Forum.Core.Tests | 26 | 0 |
| Forum.Discord.Tests | 7 | 0 |

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

추가:

- 아바타 리졸버 우선순위(임시 JSON 픽스처, fake URL) / 파일·항목 부재 시 경고(URL 미출력)
- 시크릿 파일에서 `DISCORD_BOT_USER_ID`·`DISCORD_FORUM_CHANNEL_ID` 폴백, env/config가 우선
- 페르소나 맵 `forum_webhook_url` → 공유 `DISCORD_FORUM_WEBHOOK_URL` 폴백. dry-run `webhook=persona|shared`

Weld 추가분(적응형 간격·Worker 틱)도 green.

---

## Smoke

선호 경로: `dotnet run --project src/Forum.Cli -- smoke-post --dry-run`

| 항목 | 결과 |
|---|---|
| dry-run | **pass** (exit 0). publisher 미호출 |
| dry-run `--persona-name Weld` + 임시 아바타 맵 | **pass**. `persona=Weld`, `avatarConfigured=true`. URL 미출력 |
| dry-run 웹훅 소스 | `webhook=persona` (맵 `forum_webhook_url` 있음) 또는 `webhook=shared` (없으면 공유 시크릿). URL 미출력 |
| live smoke-post (이 세션) | **하지 않음** |
| R2 (`wait=true` 응답 `id` == `channel_id`) | **pass** — 2026-10-12. post id `1559080898476122125`, starter message id == post id, author persona Weld |

값·웹훅 URL은 기록하지 않음.

---

## 새 옵션·설정

웹훅 쓰기(새 포스트·스레드 답글)는 같은 persona 이름/아바타를 쓴다.

| 항목 | 설명 |
|---|---|
| `--persona-name` | `smoke-post` / `smoke-reply` 웹훅 username 오버라이드 |
| `--avatar-url` | `smoke-post` / `smoke-reply` 웹훅 avatar 오버라이드 |
| `DISCORD_AVATAR_MAP_PATH` | 아바타 맵 JSON 경로. 기본 `%USERPROFILE%\Documents\Sensitive\avatar-cdn-map.json` |
| 맵 형식 | `name` / `agent_id` / `cdn_url` / 선택 `forum_webhook_url`. 이름 대소문자 무시 |
| 아바타 우선순위 | `--avatar-url` > 맵의 `--persona-name` 항목 > 설정 `Persona:AvatarUrl` > 없음 |
| 웹훅 우선순위 | 맵 `forum_webhook_url` > 공유 `DISCORD_FORUM_WEBHOOK_URL`. CLI `--webhook-url` 없음. 퍼블리셔는 URL당 1개 캐시 |
| 맵 부재 | 크래시 없음. 아바타는 경고만. 웹훅은 shared로 폴백. URL은 출력하지 않음 |
| dry-run `webhook=` | `persona` 또는 `shared` |
| `DISCORD_BOT_USER_ID` / `DISCORD_FORUM_CHANNEL_ID` | env/config가 있으면 그걸 쓰고, 없으면 `Documents\Sensitive\mcp-secrets.env` (`SecretsLoader`) |

---

## §8 구현 체크리스트

설계 항목은 스펙 본문에 이미 [x].

- [x] `dotnet build` TreatWarningsAsErrors 성공 (경고 0 / 오류 0)
- [x] §5 테스트 전부 green (Core 26 + Discord 7)
- [x] 스모크 dry-run 성공. R2 live smoke는 2026-10-12 통과
- [x] 이 handoff에 빌드·테스트·스모크·후속 요약

W1–W6 (`main@8ce42ef`) + W7 handoff (`main@8f5581f`) + 본 변경:

- [x] W1 솔루션 스캐폴딩 + Directory.* + docs
- [x] W2 Forum.Core + Core.Tests
- [x] W3 Forum.Discord Reader/Publisher + WireMock
- [x] W4 시크릿 로더 + 로그 스크러버
- [x] W5 Forum.Worker 적응형 폴링
- [x] W6 Forum.Cli whoami / smoke-post / smoke-reply / poll-once
- [x] W7 본 문서
- [x] 페르소나 아바타 맵 리졸버 + 시크릿 ID 폴백
- [x] 페르소나별 `forum_webhook_url` + URL당 퍼블리셔 캐시

---

## BLOCKED

없음. 설계 변경이 필요한 스펙 충돌은 없음.

---

## 후속 (상태만)

| ID | 상태 |
|---|---|
| R2 | **pass** (2026-10-12). post id `1559080898476122125`, starter message id == post id, author persona Weld |
| R4 | 미결. 상시 가동·스케줄러는 002 |
| R5 | 미결. knowledge-rag MCP (Lore/Reed) |
| R6 | 미결. 웹훅 로테이션 1줄 (Reed) |
