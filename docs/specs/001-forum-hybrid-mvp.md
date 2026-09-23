# 001 — Forum Hybrid MVP (Gateway OFF)

- **상태:** Ready for Weld
- **작성:** Keel · 2026-09-23 KST
- **선행:** `/workspace/dotnet-discord-bot-pair-research.md`, ADR `docs/adr/0001-discord-library.md`
- **구현:** Weld · **깃:** Trace · **RAG:** Lore · **조율:** Reed

---

## 0. TL;DR

온디맨드 Discord 포럼 워커 MVP. **Gateway 없음.** 읽기는 Bot REST(`after=` 커서), 쓰기는 Incoming Webhook + persona(`username`/`avatar_url`). 라이브러리는 Discord.Net.Rest + Discord.Net.Webhook 3.20.1만. **MESSAGE_CONTENT privileged intent는 켜지 않는다** — 멘션된 메시지/스레드만 처리한다.

---

## 1. 결정 사항과 기각된 대안

### D1. MESSAGE_CONTENT vs 멘션 온디맨드 → **멘션 온디맨드 (intent OFF)**

| 안 | 내용 | 결과 |
|---|---|---|
| A | Developer Portal에서 MESSAGE_CONTENT 켜고 모든 포럼 본문 폴링 | **기각** — privileged intent + 100+ 길드 심사. 이름에 Gateway가 있어 오해 유발. |
| B | 봇이 **멘션된** 메시지/포스트만 처리 (intent 예외 ③) | **채택** — 프로젝트명「온디맨드」와 일치. 권한·심사 최소화. |

**함정 기록:** MESSAGE_CONTENT는 Gateway 전용이 아니다. REST에도 동일 적용. intent 없이 `content`/`embeds`/`attachments`/`components`는 비지만, **앱이 멘션된 메시지**는 예외로 본문이 온다.

### D2. 라이브러리 → Discord.Net.Rest + .Webhook 3.20.1

ADR 0001. NetCord / DSharpPlus / Remora 기각 사유는 ADR 참고.

### D3. 쓰기 경로 → Webhook 1개 + 메시지별 persona override

페르소나마다 웹훅을 따로 두지 않는다. 차단·감사를 분리할 필요가 생기면 후속 스펙.

### D4. 솔루션 TFM → `net10.0`, SDK `10.0.201` (`global.json` 핀)

집 PC 실측 SDK와 Discord.Net.Rest TFM이 일치.

### D5. 시크릿 → 기존 `Documents\Sensitive\mcp-secrets.env` 단일 소스

새 체계 금지. 코드·로그·스펙에는 **키 이름만**.

### D6. MVP 호스팅 → 집 PC에서 `Forum.Cli` 스모크 + `Forum.Worker` 수동 기동

24시간 상시 가동·작업 스케줄러는 **후속**(R4 미결). MVP는 프로세스 수명 동안만 폴링.

---

## 2. 최소 MVP 범위

### In scope

1. 솔루션 스캐폴딩 + CPM + ADR/스펙 폴더
2. `Forum.Core` 도메인(Cursor, Persona, PostRef) + 단위 테스트
3. `Forum.Discord` 어댑터: `IForumReader` / `IPersonaPublisher` 구현 (Rest + Webhook)
4. `Forum.Worker`: 적응형 폴링 루프 — **멘션된 신규 메시지만** 처리 훅(콜백/핸들러 인터페이스)
5. `Forum.Cli`: `whoami` / `smoke-post` / `smoke-reply` / `poll-once`
6. 시크릿 로더(키 이름만) + 로그 리댁션 스크러버
7. WireMock 계약 테스트(읽기 커서·429 백오프 최소 1개)

### Out of scope (하지 말 것 / 후속)

- Gateway / WebSocket 패키지 참조
- MESSAGE_CONTENT intent 의존 로직
- 리액션·핀·아카이브·태그 변경(봇 MANAGE_* 경로)
- 멀티 길드·멀티 포럼
- 상시 서비스 설치 / Docker / CI CD
- NetCord 이전
- TeenipingTycoon 및 타 봇 영역

---

## 3. 솔루션 구조 (확정)

레포 권장 경로(Trace 생성): `C:\Users\76cha\repo\DiscordForumOps`

```
DiscordForumOps.sln
├─ global.json
├─ Directory.Build.props
├─ Directory.Packages.props
├─ .editorconfig  .gitignore  nuget.config
├─ docs/
│  ├─ adr/0001-discord-library.md
│  └─ specs/001-forum-hybrid-mvp.md   ← 본 문서
├─ src/
│  ├─ Forum.Core/          # 도메인. Discord.* using 금지
│  ├─ Forum.Discord/       # Discord.Net.Rest + .Webhook 유일한 SDK 접점
│  ├─ Forum.Worker/        # BackgroundService 폴링
│  └─ Forum.Cli/           # 단발 스모크 명령
└─ tests/
   ├─ Forum.Core.Tests/
   └─ Forum.Discord.Tests/ # WireMock.Net
```

**규칙:** `Discord.*` 네임스페이스는 `Forum.Discord`에서만 `using`. Worker/Cli/Core는 `IForumReader` / `IPersonaPublisher` / `ICursorStore` 만 본다.

### 생성/수정 파일 목록 (신규)

| 경로 | 비고 |
|---|---|
| `global.json` | SDK 10.0.201, rollForward latestFeature |
| `Directory.Build.props` | net10.0, nullable, TreatWarningsAsErrors |
| `Directory.Packages.props` | Discord.Net.Rest/Webhook 3.20.1, Hosting 10.0.*, WireMock |
| `.gitignore` | `*.env`, `appsettings.*.Local.json`, `secrets.json`, `bin/`, `obj/` |
| `docs/adr/0001-discord-library.md` | 본 스펙과 동봉 |
| `docs/specs/001-forum-hybrid-mvp.md` | 본 문서 |
| `src/Forum.Core/**` | 아래 시그니처 |
| `src/Forum.Discord/**` | Rest/Webhook 어댑터 |
| `src/Forum.Worker/**` | Hosting + PollingHostedService |
| `src/Forum.Cli/**` | System.CommandLine 또는 단순 args |
| `tests/Forum.Core.Tests/**` | 커서·중복 억제 |
| `tests/Forum.Discord.Tests/**` | WireMock |

스펙에 없는 파일은 만들지 않는다.

---

## 4. Public 시그니처 (Forum.Core)

```csharp
namespace Forum.Core;

public sealed record Persona(string Name, string? AvatarUrl);

public sealed record PostRef(ulong ForumChannelId, ulong PostId);

public sealed record MessageRef(ulong MessageId, ulong ChannelId, DateTimeOffset Timestamp);

public sealed record ForumMessage(
    MessageRef Ref,
    PostRef Post,
    string Content,
    bool MentionsBot,
    string AuthorId);

public interface ICursorStore
{
    Task<ulong?> GetLastSeenAsync(PostRef post, CancellationToken ct);
    Task SetLastSeenAsync(PostRef post, ulong messageId, CancellationToken ct);
}

public interface IForumReader
{
    Task<IReadOnlyList<PostRef>> ListActivePostsAsync(ulong forumChannelId, CancellationToken ct);
    Task<IReadOnlyList<ForumMessage>> GetMessagesAfterAsync(
        PostRef post, ulong? afterMessageId, int limit, CancellationToken ct);
}

public interface IPersonaPublisher
{
    /// <summary>새 포럼 포스트. 반환 = 스타터 메시지/포스트 id (스모크로 id vs channel_id 확인).</summary>
    Task<ulong> CreatePostAsync(
        ulong forumChannelId,
        string threadName,
        string content,
        Persona persona,
        IReadOnlyList<ulong>? appliedTagIds,
        CancellationToken ct);

    Task ReplyAsync(
        PostRef post,
        string content,
        Persona persona,
        CancellationToken ct);
}

public interface IMentionHandler
{
    Task HandleAsync(ForumMessage message, CancellationToken ct);
}
```

### Forum.Discord (구현체만, 시그니처 계약)

```csharp
namespace Forum.Discord;

public sealed class DiscordRestForumReader : IForumReader { /* DiscordRestClient */ }
public sealed class DiscordWebhookPublisher : IPersonaPublisher { /* DiscordWebhookClient 싱글턴 */ }
public sealed class FileCursorStore : ICursorStore { /* JSON 파일 또는 sqlite — MVP는 JSON */ }
```

의사코드 — 폴링 1틱:

```
posts = reader.ListActivePosts(forumId)
foreach post in posts:
  last = cursor.GetLastSeen(post)
  msgs = reader.GetMessagesAfter(post, last, limit:100)
  foreach m in msgs ordered by id:
    if m.MentionsBot: await handler.Handle(m)
    cursor.SetLastSeen(post, m.Ref.MessageId)  // 멘션 여부와 무관하게 커서 전진
```

의사코드 — 적응형 간격:

```
interval = idleSeconds (default 60)
on tick with any new message: interval = activeSeconds (default 10)
on N quiet ticks: interval = min(idleSeconds, interval * 2) 까지 확장
```

### 설정 키 (값 금지, 이름만)

| 키 | 위치 | 용도 |
|---|---|---|
| `DISCORD_BOT_TOKEN` | mcp-secrets.env | REST 로그인 |
| `DISCORD_FORUM_WEBHOOK_URL` | mcp-secrets.env | 쓰기 |
| `DISCORD_FORUM_CHANNEL_ID` | appsettings.json 가능 | 포럼 채널 |
| `DISCORD_BOT_USER_ID` | appsettings.json | 멘션 판정 |
| Persona Name / AvatarUrl | appsettings 또는 avatar-cdn-map **키만 참조** | 웹훅 override |

로그 리댁션 필수:

- `/api/webhooks/\d+/[\w-]+` → `/api/webhooks/***/***`
- Discord 봇 토큰 형태 → `Bot ***`

---

## 5. 테스트 목록 (삭제 금지, Weld는 추가만)

| 테스트 이름 | 검증 대상 |
|---|---|
| `Cursor_advances_only_forward` | 동일/과거 id로 Set 해도 lastSeen이 줄지 않음 |
| `Cursor_survives_roundtrip` | FileCursorStore 저장·재로드 |
| `Poll_skips_non_mention` | MentionsBot=false 는 handler 미호출, 커서는 전진 |
| `Poll_invokes_handler_on_mention` | MentionsBot=true 1회 호출 |
| `Reader_uses_after_cursor` | WireMock: `after=` 쿼리 존재, limit≤100 |
| `Publisher_create_post_sends_thread_name_and_persona` | WireMock: thread_name, username, avatar_url |
| `Publisher_reply_uses_thread_id` | WireMock: thread_id 쿼리/본문 |
| `RateLimit_429_retries_once` | WireMock 429 + Retry-After → 1회 재시도 |
| `Secrets_never_logged` | 스크러버가 웹훅 URL·토큰 패턴 마스킹 |

---

## 6. 하지 말 것

- `Discord.Net.WebSocket` / `DiscordSocketClient` / Gateway intent 코드 경로
- 시크릿·웹훅 URL을 로그·예외·콘솔·커밋·채팅·스크린샷에 출력
- 폴링 간격 매직넘버 하드코딩(설정으로)
- 메시지마다 `new DiscordWebhookClient` (HttpClient 고갈)
- 스펙 밖 프로젝트·파일 추가
- MESSAGE_CONTENT가 있다고 가정한 본문 파싱
- TeenipingTycoon / Iris / Bolt 경로 수정

---

## 7. Weld 작업 분해

| # | 단위 | 완료 신호 | 커밋(한글, Trace) |
|---|---|---|---|
| W1 | 솔루션 스캐폴딩 + Directory.* + .gitignore + docs 복사 | `dotnet build` 빈 프로젝트 green | `솔루션 - DiscordForumOps 스캐폴딩` |
| W2 | Forum.Core + Core.Tests (커서·멘션 필터 순수 로직) | Core.Tests green | `코어 - 커서와 멘션 필터` |
| W3 | Forum.Discord Reader/Publisher + WireMock 테스트 | Discord.Tests green | `디스코드 - REST 읽기·웹훅 쓰기 어댑터` |
| W4 | 시크릿 로더 + 로그 스크러버 | 스크러버 테스트 green | `설정 - 시크릿 로더와 로그 리댁션` |
| W5 | Forum.Worker 폴링 루프 + 적응형 간격 | Worker 빌드 + 단위(가짜 reader) | `워커 - 적응형 폴링 루프` |
| W6 | Forum.Cli whoami/smoke-post/smoke-reply/poll-once | Cli `--help` + 로컬 스모크 메모 | `CLI - 스모크 명령` |
| W7 | handoff.md 작성 | 아래 §8 체크 | (문서만) |

스펙과 현실이 어긋나면 **임의 판단 금지.** `docs/specs/001-forum-hybrid-mvp.handoff.md`에 `BLOCKED:` 로 적고 Keel로 반송.

---

## 8. 완료 판정 기준

설계(본 스펙) 체크리스트:

- [x] 결정·기각 대안
- [x] 파일 경로 목록
- [x] public 시그니처
- [x] 테스트 목록
- [x] 하지 말 것
- [x] 완료 판정

구현(Weld) 완료:

1. `dotnet build` TreatWarningsAsErrors로 성공
2. §5 테스트 **전부** green
3. 스모크(집 PC, 시크릿 파일 존재 시):  
   `dotnet run --project src/Forum.Cli -- smoke-post --dry-run` 또는 실전송 1회 후  
   **`?wait=true` 응답의 `id`와 `channel_id`를 handoff에 기록**(R2 검증). 값/URL은 적지 말고 일치 여부만.
4. `001-forum-hybrid-mvp.handoff.md`에 빌드·테스트·스모크·BLOCKED(있으면) 요약

---

## 9. 미결 → 후속 스펙

| ID | 항목 | 소유 |
|---|---|---|
| R2 | 스타터 메시지 id == 포스트 id 가정 검증 | Weld 스모크 → 결과 보고 |
| R4 | 집 PC 상시 가동 / 스케줄러 | Producer(찬영) 확인 후 002 |
| R5 | knowledge-rag MCP 복구 | Lore/Reed |
| R6 | 웹훅 로테이션 절차 1줄 | Reed 운영 문서 |

---

## 10. 핸드오프

- **박스 원본:** `/workspace/docs/specs/001-forum-hybrid-mvp.md`
- **ADR:** `/workspace/docs/adr/0001-discord-library.md`
- 레포 생성 후 Trace가 `docs/` 아래로 이식.
- Weld는 본 문서만 입력으로 구현 세션을 연다. 설계 대화 로그는 붙이지 않는다.
