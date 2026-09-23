# MVP와 이후(MCP)

MVP는 하이브리드 포럼 워커입니다. 멘션된 메시지를 읽고, persona 웹훅으로 씁니다. Gateway·멀티 길드·상시 서비스는 범위 밖입니다.

최종 목표는 MCP처럼 도구를 노출하는 것입니다. 포스트 목록, 새 메시지 읽기, persona 작성·답글이 도구 후보입니다. 전송 방식과 도구 목록은 아직 스펙이 없습니다.

Core 포트가 나중에 도구로 나갈 경계입니다. `IForumReader`, `IPersonaPublisher`, `ICursorStore`를 그대로 유지하는 것이 전제입니다.

구현 단위 W1–W2는 끝났고, W3 이후(어댑터 테스트, 시크릿 로더, Worker, CLI, handoff)는 진행 중입니다. 진행 표는 [README](../../../README.md)를 보세요.

TODO(본문): MCP 스펙이 생기면 이 페이지에 링크만 추가.

원문 스펙은 복붙하지 않습니다. [스펙 001](../../specs/001-forum-hybrid-mvp.md), [ADR 0001](../../adr/0001-discord-library.md)을 링크하세요.
