# ADR 0001 — Discord 라이브러리 선택

- **상태:** Accepted (2026-09-23)
- **맥락:** Forum 온디맨드 워커는 Gateway 없이 REST 읽기 + Webhook 쓰기가 필요하다.

## 결정

`Discord.Net.Rest` 3.20.1 + `Discord.Net.Webhook` 3.20.1 만 참조한다. `Discord.Net.WebSocket` / 메타 패키지 `Discord.Net` 은 참조하지 않는다.

## 근거

1. Gateway OFF가 설정이 아니라 패키지 그래프 부재로 강제된다.
2. 읽기/쓰기 경계가 `.Rest` / `.Webhook` 에 1:1 매핑된다.
3. Forum `threadName` / `threadId` / `appliedTags` 시그니처가 실존한다.
4. stable이며 LLM 구현(Weld) 환각률이 낮다.

## 기각

| 대안 | 기각 사유 |
|---|---|
| NetCord | 1.0 stable 없음(전부 프리릴리스). Auto 구현이 breaking 흡수 부담. |
| DSharpPlus 5 | 4.x/5.x 자료 혼재 → LLM 오답. Gateway 결합 강함. |
| Remora.Discord | `Result<T>` 관용구를 스펙이 완벽히 못 쓰면 구현이 붕괴. |

## 재평가 트리거

NetCord **1.0 GA** 출시 시 이 ADR을 재검토한다. 교체 시 영향 범위는 `Forum.Discord` 프로젝트 1개로 한정한다.
