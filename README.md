# DiscordForumOps

Discord **포럼 채널** 전용 온디맨드 봇을 .NET으로 만든다.
봇은 포럼 포스트에서 **자신이 멘션된 메시지만** 골라 처리한다. 답글은 웹훅으로 보내며, 메시지마다 persona(이름·아바타)를 바꿔 쓸 수 있다.

---

## 최종 목표

**MCP 서버처럼 도구·기능을 노출하는 포럼 중심 Discord 봇.**

다른 에이전트나 클라이언트가 "포스트 목록 조회", "새 메시지 읽기", "persona로 포스트 작성·답글" 같은 포럼 기능을 도구 호출 형태로 쓸 수 있게 하는 것이 목표다.
Core의 포트 인터페이스(`IForumReader` / `IPersonaPublisher` / `ICursorStore`)가 나중에 도구로 노출할 기능의 경계가 된다.

> 도구 노출 방식(MCP 서버 여부, 전송 방식, 도구 목록)은 아직 스펙이 없다. MVP 이후 별도 스펙에서 정한다.

---

## 현재 단계 — 하이브리드 포럼 MVP

스펙: [`docs/specs/001-forum-hybrid-mvp.md`](docs/specs/001-forum-hybrid-mvp.md)

"하이브리드"는 **읽기와 쓰기에 서로 다른 경로**를 쓴다는 뜻이다.

| 경로 | 방식 | 패키지 |
|---|---|---|
| 읽기 | Bot REST 폴링 (`after=` 커서) | `Discord.Net.Rest` 3.20.1 |
| 쓰기 | Incoming Webhook 1개 + 메시지별 persona override (`username` / `avatar_url`) | `Discord.Net.Webhook` 3.20.1 |
| 실시간 | **Gateway 없음** | `Discord.Net.WebSocket` 참조하지 않음 |

### 핵심 결정

- **Gateway OFF** — 설정으로 끄는 것이 아니라, WebSocket 패키지를 참조하지 않아서 **패키지 그래프 차원에서 막는다**. ([ADR 0001](docs/adr/0001-discord-library.md))
- **MESSAGE_CONTENT intent OFF** — 앱이 멘션된 메시지는 intent 없이도 본문이 온다. 이 예외를 이용해 **멘션된 메시지만** 처리한다. privileged intent 심사가 필요 없다.
  - 주의: MESSAGE_CONTENT는 Gateway에만 걸리는 제한이 아니라 REST에도 똑같이 걸린다.
- **웹훅 1개 + persona override** — persona마다 웹훅을 따로 만들지 않는다.
- **적응형 폴링** — 한가할 때는 긴 간격(기본 60초), 새 메시지가 오면 짧은 간격(기본 10초)으로 폴링한다. 간격 값은 설정에서 읽는다.
- **커서는 항상 앞으로만** — 멘션 여부와 관계없이 읽은 메시지까지 커서를 전진한다.
- **호스팅** — MVP는 집 PC에서 `Forum.Cli` 스모크와 `Forum.Worker` 수동 실행까지만 한다. 상시 가동은 후속 과제다.

### 라이브러리 선택 (ADR 0001 요약)

`Discord.Net.Rest` + `Discord.Net.Webhook` 3.20.1을 쓴다. stable 버전이고, 읽기/쓰기 경계가 두 패키지에 1:1로 맞고, 포럼 `threadName` / `threadId` / `appliedTags`를 지원하기 때문이다.

| 기각한 대안 | 사유 |
|---|---|
| NetCord | 1.0 stable 없음 (**1.0 GA 시 재검토**) |
| DSharpPlus 5 | 4.x/5.x 자료 혼재, Gateway 결합 강함 |
| Remora.Discord | `Result<T>` 관용구 부담 |

라이브러리를 바꾸게 되더라도 영향 범위는 `Forum.Discord` 프로젝트 하나다.

### MVP 범위 밖

Gateway·WebSocket, MESSAGE_CONTENT 의존 로직, 리액션·핀·아카이브·태그 변경, 멀티 길드·멀티 포럼, 상시 서비스·Docker·CI/CD.

### 진행 상황

| # | 단위 | 상태 |
|---|---|---|
| W1 | 솔루션 스캐폴딩 | ✅ |
| W2 | Forum.Core 커서·멘션 필터 + 테스트 | ✅ |
| W3 | Forum.Discord REST 읽기·웹훅 쓰기 어댑터 + WireMock 테스트 | ✅ |
| W4 | 시크릿 로더 + 로그 리댁션 | ✅ |
| W5 | Forum.Worker 적응형 폴링 루프 | ✅ |
| W6 | Forum.Cli 스모크 명령 | ✅ |
| W7 | handoff 문서 | ⬜ |

---

## 동작 흐름 (폴링 1틱)

```
posts = reader.ListActivePosts(forumId)
foreach post in posts:
  last = cursor.GetLastSeen(post)
  msgs = reader.GetMessagesAfter(post, last, limit: 100)
  foreach m in msgs (id 순):
    if m.MentionsBot: handler.Handle(m)      // 멘션된 메시지만 처리
    cursor.SetLastSeen(post, m.MessageId)    // 커서는 항상 전진
```

---

## 솔루션 구조

```
DiscordForumOps.sln        (net10.0, SDK 10.0.201 — global.json 고정)
├─ src/
│  ├─ Forum.Core/      도메인·포트 인터페이스. Discord.* 참조 금지
│  ├─ Forum.Discord/   Discord.Net.Rest/.Webhook을 쓰는 유일한 프로젝트
│  ├─ Forum.Worker/    BackgroundService 적응형 폴링
│  └─ Forum.Cli/       단발 스모크 명령 (whoami / smoke-post / smoke-reply / poll-once)
├─ tests/
│  ├─ Forum.Core.Tests/
│  └─ Forum.Discord.Tests/   WireMock 계약 테스트
└─ docs/
   ├─ adr/0001-discord-library.md
   └─ specs/001-forum-hybrid-mvp.md
```

**의존 규칙:** `Discord.*` 네임스페이스는 `Forum.Discord`에서만 `using`한다. Worker·Cli·Core는 `IForumReader` / `IPersonaPublisher` / `ICursorStore`만 바라본다.

---

## 시크릿 규칙

- 시크릿은 기존 **`Documents\Sensitive\mcp-secrets.env`** 한 곳에서만 읽는다. 새 시크릿 체계를 만들지 않는다.
- 코드·로그·문서·커밋에는 **키 이름만** 적는다. 값과 웹훅 URL은 어디에도 출력하지 않는다.

| 키 | 위치 | 용도 |
|---|---|---|
| `DISCORD_BOT_TOKEN` | mcp-secrets.env | REST 로그인 |
| `DISCORD_FORUM_WEBHOOK_URL` | mcp-secrets.env | 쓰기 |
| `DISCORD_FORUM_CHANNEL_ID` | appsettings.json 가능 | 대상 포럼 채널 |
| `DISCORD_BOT_USER_ID` | appsettings.json | 멘션 판정 |

- 로그 리댁션: 웹훅 경로는 `/api/webhooks/***/***`, 봇 토큰은 `Bot ***`로 가린다.
- `.gitignore`가 `*.env`, `appsettings.*.Local.json`, `secrets.json`을 제외한다.

---

## 빌드·테스트

```powershell
dotnet build   # TreatWarningsAsErrors
dotnet test
```

## Forum.Cli 스모크

`Documents\Sensitive\mcp-secrets.env`의 키 이름만 쓴다. 토큰·웹훅 URL은 출력하지 않는다.

```powershell
dotnet run --project src/Forum.Cli -- --help
dotnet run --project src/Forum.Cli -- smoke-post --dry-run
dotnet run --project src/Forum.Cli -- smoke-reply --dry-run --post-id 0
```

| 명령 | dry-run | live |
|---|---|---|
| `whoami` | 없음. REST identity. `DISCORD_BOT_TOKEN` + `DISCORD_BOT_USER_ID` 필요 | 봇 id·username. 시크릿 값 없음 |
| `smoke-post` | 전송 없이 채널·제목·본문·persona 배선만 출력. 시크릿 없어도 됨 | `IPersonaPublisher.CreatePostAsync`. 시크릿 + `DISCORD_FORUM_CHANNEL_ID` |
| `smoke-reply` | 위와 같음. `--post-id`는 live에서 필수 | `ReplyAsync`. 시크릿 + `--post-id` + 포럼 채널 id |
| `poll-once` | 없음. `MentionPollProcessor` 1틱 후 종료 | `DISCORD_BOT_TOKEN` + `DISCORD_BOT_USER_ID` + `DISCORD_FORUM_CHANNEL_ID` |

live 채널 오버라이드: `--forum-channel-id`. persona: `--persona-name`. 시크릿·채널 id가 없으면 키 이름만 알리고 종료한다.

---

## 공개 문서

<!-- GitBook public URL: TBD after site + Git Sync -->

사람용 GitBook 소스는 [`docs/site/`](docs/site/)입니다. 사이트 URL은 Git Sync 이후에 이 자리에 넣습니다.

## 문서

- [스펙 001 — Forum Hybrid MVP](docs/specs/001-forum-hybrid-mvp.md)
- [ADR 0001 — Discord 라이브러리 선택](docs/adr/0001-discord-library.md)
