# SignPath Foundation setup

This document describes the one-time steps required to activate the SignPath Foundation workflow prepared in `.github/workflows/sign-release.yml`.

The repository-side configuration is intentionally safe to merge before SignPath credentials exist: the signing workflow runs only when manually dispatched and fails early with a clear message if required settings are missing.

## 1. Confirm project eligibility

LegacySift should continue to satisfy the SignPath Foundation Open Source Code Signing conditions:

- public source code under the MIT License;
- no proprietary project component;
- active maintenance;
- a documented Windows release available for download;
- a public Code signing policy;
- a privacy policy;
- MFA enabled for the GitHub and SignPath accounts used for release signing;
- explicit author/reviewer/approver roles.

See [Code signing policy](CODE_SIGNING.md) and [Privacy](PRIVACY.md).

## 2. Publish an unsigned prerelease first

SignPath Foundation requires the project to already be released in the form that will later be signed.

Before applying, publish the current Windows portable ZIP as a GitHub **pre-release** (for example `v0.2.3-alpha`) from the validated `main` build.

The release/download page should link to the [Code signing policy](CODE_SIGNING.md).

Do not describe that prerelease as signed.

## 3. Apply to SignPath Foundation

Application page:

https://signpath.org/apply.html

Use the public LegacySift repository and release/download page in the application.

The project is requesting **Open Source Code Signing** with a certificate provided through SignPath Foundation.

## 4. SignPath organization/project setup

After acceptance:

1. Create/use the SignPath organization supplied for the OSS project.
2. Create a SignPath project for LegacySift.
3. Set the repository URL to:
   `https://github.com/glen000/LegacySift`
4. Add the predefined **GitHub.com** trusted build system.
5. Install/authorize the SignPath GitHub App for the LegacySift repository when requested.
6. Import `.signpath/artifact-configurations/default.xml` as the project's default artifact configuration.
7. Create/use a release signing policy with:
   - Authenticode certificate provided through SignPath Foundation;
   - trusted-build-system verification enabled;
   - origin verification enabled;
   - allowed release branch restricted to `main` (and only explicitly approved future `release/*` branches);
   - manual approval required for each release.

## 5. GitHub repository configuration

Configure these values in the LegacySift repository settings.

### Secret

- `SIGNPATH_API_TOKEN`

The token belongs to a SignPath user/service identity that is allowed to submit signing requests for the configured policy.

### Variables

- `SIGNPATH_ORGANIZATION_ID`
- `SIGNPATH_PROJECT_SLUG`
- `SIGNPATH_SIGNING_POLICY_SLUG`

The workflow reads these values but does not contain them in source control.

## 6. Artifact configuration

The repository artifact configuration signs exactly:

`LegacySift.exe`

inside the GitHub artifact ZIP.

It enforces:

- product name: `LegacySift`;
- product version: the executable's `FileVersion`;
- file version: the same `FileVersion`;
- original filename: `LegacySift.exe`.

The signing workflow automatically reads the version from the built executable and passes it to SignPath as the `version` parameter.

## 7. First signing test

After the SignPath project and GitHub values are configured:

1. Open **Actions** in GitHub.
2. Select **sign-release**.
3. Run the workflow from `main`.
4. Approve the signing request in SignPath when prompted.
5. Wait for the workflow to complete.
6. Download the `LegacySift-windows-signed` artifact.
7. Verify the included SHA-256 checksum.
8. Extract `LegacySift.exe` and verify its Windows **Digital Signatures** tab or run:

```powershell
Get-AuthenticodeSignature .\LegacySift.exe | Format-List
```

The workflow also performs this Authenticode validation automatically before packaging.

## 8. Release policy after activation

Once the first signing test has been validated:

- release artifacts intended for ordinary users should come from the signing workflow;
- pull-request and development artifacts may remain unsigned;
- generate the public release ZIP checksum only **after** signing;
- keep the unsigned GitHub build artifact for traceability, but clearly distinguish it from the signed release package;
- every signed release continues to require manual approval in SignPath.
