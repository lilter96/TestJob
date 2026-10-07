# TestJob — focused ASP.NET Core assignment

A compact .NET 10 API exercise combining HTML parsing with AngleSharp, PostgreSQL access through Dapper, request validation and AES-based data handling. This is a supporting assignment, not the main engineering portfolio.

**Verification on 2026-10-07:** 19 tests passed locally.

```bash
dotnet test tests/TestJob.Api.Tests/TestJob.Api.Tests.csproj -c Release
```

Review [API source](src/TestJob.Api) and [tests](tests/TestJob.Api.Tests). Test encryption inputs are synthetic fixtures. Passing local tests do not constitute a cryptographic security audit or verification of external websites and deployment infrastructure.

For broader backend architecture and durable workflows, see [JobFinder](https://github.com/lilter96/jobfinder-showcase).
