# 멘션 온디맨드

봇은 포럼 포스트에서 자신이 멘션된 메시지만 처리합니다. 모든 본문을 읽기 위해 privileged intent를 켜지 않습니다.

`MESSAGE_CONTENT` intent는 끕니다. 이 제한은 Gateway뿐 아니라 REST에도 같습니다. 앱이 멘션된 메시지는 예외로 본문이 옵니다.

이 예외를 쓰는 것이 「온디맨드」입니다. 길드 수가 늘어도 해당 intent 심사를 피할 수 있습니다.

멘션 판정에는 `DISCORD_BOT_USER_ID`를 씁니다. 토큰 값은 적지 않습니다.

TODO(본문): 멘션 형식과 필터 경계.

결정 기록은 [스펙 001](../../specs/001-forum-hybrid-mvp.md) D1을 보세요.
