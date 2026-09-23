# 빌드·테스트

레포 루트에서 `dotnet build`와 `dotnet test`를 실행합니다. 경고는 오류로 취급합니다(`TreatWarningsAsErrors`).

TFM은 `net10.0`입니다. SDK는 `global.json`의 `10.0.201`로 고정합니다.

테스트는 `Forum.Core.Tests`와 `Forum.Discord.Tests`에 있습니다. Discord 어댑터 계약은 WireMock으로 검증합니다.

시크릿 파일 경로와 키 값, 웹훅 URL은 이 페이지에 쓰지 않습니다. 키 이름만 [README 시크릿 규칙](../../../README.md)을 따르세요.

CLI 스모크(dry-run / live)는 루트 [README — Forum.Cli 스모크](../../../README.md#forumcli-스모크)를 따른다.

스펙의 구현 단위 목록은 [스펙 001](../../specs/001-forum-hybrid-mvp.md)에 있습니다.
