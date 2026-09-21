# Evidence template

Copy this file, fill it in, and submit the copy.

## Before you submit: redact

Remove or replace all of the following in everything you paste or screenshot:

- [ ] AWS account IDs (the 12-digit number), including inside ARNs
- [ ] Access key IDs, secret keys, session tokens, or anything that looks like one
- [ ] SSO start URLs and the contents of `~/.aws` files
- [ ] Bucket names, if your instructor asks for them to be masked (use `<bucket>`)
- [ ] Request IDs and other identifiers in error output

**Never paste credentials, tokens, or the output of `aws configure list` / environment dumps.** None of the items below need them. If a command's output contains one, do not include that output.

Replace an account ID like this: `arn:aws:iam::<ACCOUNT-ID>:role/<role-name>`.

## 1. Uploaded object

Key and metadata from a successful `201` response (paste the response body, not headers):

```json
{
  "objectKey": "",
  "metadata": {
    "original-filename": "",
    "content-type": "",
    "uploaded-utc": "",
    "content-length": ""
  }
}
```

- [ ] Key matches `documents/uploads/yyyy/MM/dd/<32 hex characters>.ext`
- [ ] Key does not contain the original filename
- [ ] Metadata has exactly four entries

Rejected upload (the `.exe` request):

- [ ] Status was `400`
- [ ] Body listed the validation error(s): _paste here_

## 2. Continuous integration

- Failing run URL (deliberately broken assertion): _paste here_
- Passing run URL (after the fix): _paste here_
- [ ] `ci.yml` contains no AWS credentials, no `permissions:` block, and no OIDC configuration

## 3. IAM denial analysis

Describe one request that was denied by IAM.

- What action was attempted: _
- Which resource: _ (redact account ID)
- Which policy statement, or missing statement, caused the denial: _
- What change would allow it, and why you would or would not make it: _

## 4. Deployment

- Environment health after deploy: _
- `GET /health` status code: _
- [ ] Upload through the deployed URL succeeded

## 5. Cleanup

- [ ] Elastic Beanstalk environment terminated
- [ ] Test objects deleted from the bucket
- [ ] Any resources created only for this assignment removed
- [ ] Confirmed in the console that no billable resources from this assignment remain

## 6. Final check

- [ ] I searched this document for 12-digit numbers, `AKIA`, `ASIA`, `aws_secret`, and `token`, and found nothing sensitive
