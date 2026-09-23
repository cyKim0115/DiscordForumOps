# DiscordForumOps

Discord 포럼 채널 전용 온디맨드 봇입니다. .NET으로 만들고, 봇이 멘션된 메시지에만 반응합니다. 답글은 웹훅으로 보내며, 메시지마다 persona(이름·아바타)를 바꿀 수 있습니다.

## 최종 목표

MCP 서버처럼 포럼 기능을 도구로 노출하는 것이 목표입니다. 다른 에이전트나 클라이언트가 포스트 목록 조회, 새 메시지 읽기, persona로 작성·답글 같은 일을 도구 호출로 쓰게 합니다. 도구 목록과 전송 방식은 MVP 이후 별도 스펙에서 정합니다. Core 포트(`IForumReader` / `IPersonaPublisher` / `ICursorStore`)가 그 경계입니다.

## MVP — 하이브리드 포럼

읽기는 Bot REST 폴링, 쓰기는 Incoming Webhook + persona override, Gateway는 끕니다. `Discord.Net.WebSocket`을 참조하지 않습니다.

| 경로 | 방식 |
|---|---|
| 읽기 | Bot REST (`after=` 커서) |
| 쓰기 | Webhook 1개 + 메시지별 persona |
| 실시간 | Gateway OFF |

상세는 레포 [README](../../README.md), [스펙 001](../specs/001-forum-hybrid-mvp.md), [ADR 0001](../adr/0001-discord-library.md)을 보세요. 이 사이트는 목차와 짧은 안내만 둡니다.

시크릿은 키 이름만 적습니다. `DISCORD_BOT_TOKEN`, `DISCORD_FORUM_WEBHOOK_URL` 같은 값은 문서에 쓰지 않습니다.
