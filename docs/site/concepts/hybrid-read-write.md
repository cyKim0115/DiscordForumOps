# 하이브리드 읽기·쓰기

하이브리드는 읽기와 쓰기에 다른 경로를 쓴다는 뜻입니다. 읽기는 Bot REST 폴링, 쓰기는 Incoming Webhook 한 개입니다.

쓰기 때 메시지마다 persona를 바꿉니다. `username`과 `avatar_url`을 덮어쓰며, persona마다 웹훅을 따로 만들지 않습니다.

Gateway(WebSocket)는 쓰지 않습니다. 설정으로 끄는 것이 아니라 `Discord.Net.WebSocket`을 참조하지 않습니다.

라이브러리는 `Discord.Net.Rest`와 `Discord.Net.Webhook` 3.20.1입니다. 선택 이유는 [ADR 0001](../../adr/0001-discord-library.md)을 보세요.

TODO(본문): 읽기/쓰기 실패·429 동작.

전체 표와 범위는 [README](../../../README.md)와 [스펙 001](../../specs/001-forum-hybrid-mvp.md)에 있습니다.
