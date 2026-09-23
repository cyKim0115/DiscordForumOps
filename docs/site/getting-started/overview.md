# 개요

DiscordForumOps는 포럼 채널에서만 동작하는 온디맨드 봇입니다. 멘션된 메시지를 골라 처리하고, 웹훅으로 답글을 보냅니다.

현재 단계는 하이브리드 포럼 MVP입니다. 읽기는 Bot REST, 쓰기는 Webhook + persona, Gateway는 패키지 그래프에서 빠져 있습니다.

솔루션은 `Forum.Core`(포트), `Forum.Discord`(어댑터), `Forum.Worker`(폴링), `Forum.Cli`(스모크)로 나눕니다. `Discord.*` using은 `Forum.Discord`에만 둡니다.

시크릿 키 이름: `DISCORD_BOT_TOKEN`, `DISCORD_FORUM_WEBHOOK_URL`, `DISCORD_FORUM_CHANNEL_ID`, `DISCORD_BOT_USER_ID`. 값은 문서에 적지 않습니다.

TODO(본문): 권한·채널 준비 체크리스트.

자세한 범위와 결정은 [README](../../../README.md), [스펙 001](../../specs/001-forum-hybrid-mvp.md), [ADR 0001](../../adr/0001-discord-library.md)을 보세요.
