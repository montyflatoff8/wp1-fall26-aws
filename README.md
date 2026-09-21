# wp1-fall26-aws

A small ASP.NET Core minimal Web API that accepts a file upload, validates it against a fixed policy, and writes it to Amazon S3 behind a storage interface.

The HTTP layer (`Wp1Fall26Aws.Api`) never touches the AWS SDK types in its handlers, and the storage layer (`Wp1Fall26Aws.Storage`) never touches `HttpContext`. A single narrow interface, `IS3ObjectClient`, sits between the store and the SDK, so the tests run offline against a fake.

## Layout

| Path | Purpose |
|---|---|
| `src/Wp1Fall26Aws.Api` | HTTP endpoints, configuration, DI wiring |
| `src/Wp1Fall26Aws.Storage` | `IDocumentStore`, validation, key/metadata rules, S3 adapter |
| `tests/Wp1Fall26Aws.Tests` | Offline unit tests (no AWS account, network, or credentials) |
| `samples/synthetic-document.txt` | Harmless file for trying the upload endpoint |
| `docs/evidence-template.md` | Redaction-safe checklist for submission evidence |

## Endpoints

| Method | Path | Result |
|---|---|---|
| `GET` | `/` | `200`, one-line text (Beanstalk's default health probe) |
| `GET` | `/health` | `200`, `{ "status": "healthy" }`. Makes no AWS call. |
| `POST` | `/documents` | Multipart upload, see below |

`POST /documents` reads the form field named `file`; if there is no such field it uses the first file in the form.

| Outcome | Status | Body |
|---|---|---|
| Stored | `201` | `{ "objectKey": "...", "metadata": { ... } }` |
| Validation failed | `400` | `{ "errors": [ "...", "..." ] }` |
| No file in request | `400` | `{ "errors": ["No file was provided."] }` |
| Storage failed | `500` | Generic message. The underlying error is logged server-side only. |

## Validation policy

Enforced before any S3 call. All failing rules are reported, not just the first.

| Rule | Default |
|---|---|
| Maximum size | 1,048,576 bytes (1 MiB); exactly 1 MiB passes |
| Empty file | Rejected |
| Allowed extensions | `.txt` `.pdf` `.png` `.jpg` `.jpeg` (case-insensitive) |
| Allowed content types | `text/plain`, `application/pdf`, `image/png`, `image/jpeg` |

## Storage behavior

**Object key:** `{KeyPrefix}/{yyyy}/{MM}/{dd}/{guid:N}{.ext}` using the UTC date, a new GUID, and the original extension lowercased. Example:

```
documents/uploads/2026/09/14/3f2a91c84d7b4e6f8a105c2d9b7e0431.pdf
```

The original filename is never part of the key.

**Object metadata:** exactly these four entries and nothing else.

| Key | Value |
|---|---|
| `original-filename` | as received |
| `content-type` | as validated |
| `uploaded-utc` | ISO 8601, e.g. `2026-09-14T18:22:07Z` |
| `content-length` | bytes, as a string |

## Configuration

Committed `appsettings.json` holds placeholders only:

| Key | Default | Notes |
|---|---|---|
| `S3Storage:BucketName` | *(empty)* | **Required.** Never commit a real value. |
| `S3Storage:KeyPrefix` | `documents/uploads` | |
| `S3Storage:Region` | `us-east-1` | Informational; the SDK's default chain resolves the region. |
| `S3Storage:MaxFileSizeBytes` | `1048576` | |

The same keys arrive from different places depending on where the app runs:

| Where | Source | Spelling |
|---|---|---|
| Laptop | .NET user secrets | `S3Storage:BucketName` |
| Beanstalk | environment property | `S3Storage__BucketName` |
| Tests | constructed in code | n/a |

**Credentials are never configured.** There is no profile name, key, or region override in code. The AWS SDK's default credential chain resolves everything: your SSO session locally, the instance profile on Elastic Beanstalk.

**A missing bucket name is not checked at startup.** The app boots, and the first upload that passes validation fails with a `500` and a server-side log line `S3Storage:BucketName is not configured.` If uploads work locally but fail on the server, check the `S3Storage__BucketName` environment property first.

## Run locally

Prerequisites: .NET 10 SDK, and for real uploads an AWS SSO session (`aws sso login`) with access to your bucket.

```bash
dotnet restore Wp1Fall26Aws.slnx
dotnet build   Wp1Fall26Aws.slnx --configuration Release
dotnet test    Wp1Fall26Aws.slnx --configuration Release
```

The tests need no AWS configuration. To try the API:

```bash
dotnet user-secrets --project src/Wp1Fall26Aws.Api set "S3Storage:BucketName" "<your-bucket>"
dotnet run --project src/Wp1Fall26Aws.Api
```

In a second terminal:

```bash
curl -F "file=@samples/synthetic-document.txt;type=text/plain" http://localhost:5000/documents
curl -i -F "file=@some-file.exe" http://localhost:5000/documents    # expect 400
```

`dotnet run` uses `Properties/launchSettings.json`, which listens on `http://localhost:5000` and sets the environment to `Development` (which is what makes user secrets load). The application never calls `UseUrls`, so a `--urls` argument is always honored.

To check that the published output starts the way Elastic Beanstalk starts it:

```bash
dotnet publish src/Wp1Fall26Aws.Api -c Release -o publish
cd publish && dotnet Wp1Fall26Aws.Api.dll --urls http://0.0.0.0:5000
```

## Continuous integration

`.github/workflows/ci.yml` restores, builds, and tests on pull requests to `main` and on pushes to any branch. It uses no AWS credentials.

## Out of scope

No database, authentication, download or delete endpoints, pre-signed URLs, virus scanning, multipart/resumable uploads, or front end.
