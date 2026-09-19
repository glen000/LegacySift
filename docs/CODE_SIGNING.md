# Code signing policy

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Status

LegacySift is preparing SignPath Foundation Open Source Code Signing integration. Until the project is accepted and the signing configuration is activated, published alpha artifacts may remain unsigned.

## Repository and build origin

- Source repository: https://github.com/glen000/LegacySift
- Release-signing builds must originate from this repository.
- Release-signing artifacts must be built by GitHub Actions on GitHub-hosted runners from source-controlled build definitions.
- The intended release-signing branch is `main` (or a future explicitly documented `release/*` branch).
- Local binaries and manually rebuilt binaries are not eligible for release signing.

## Team roles

LegacySift is currently maintained by a single project maintainer.

- **Author / committer:** [@glen000](https://github.com/glen000)
- **Reviewer:** [@glen000](https://github.com/glen000). Contributions from non-committers must be reviewed before merge.
- **Code-signing approver:** [@glen000](https://github.com/glen000)

The GitHub and SignPath accounts used for release signing must use multi-factor authentication.

## Signing rules

For SignPath Foundation release signing:

1. The binary must be produced from this repository by the approved GitHub Actions workflow.
2. The signing policy must use SignPath origin verification and a GitHub trusted build system.
3. The signing policy should be restricted to the approved release branch(es).
4. The artifact configuration must enforce the LegacySift product name and the build's product/file version.
5. Every release-signing request requires manual approval in SignPath.
6. The signed executable is packaged only after SignPath returns the signed artifact.
7. SHA-256 checksums are generated after signing and packaging.

## Privacy policy

See [Privacy](PRIVACY.md).

LegacySift does not contain telemetry or an online account system. This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

## Verifying a signed Windows build

A signed release can be inspected in Windows through **Properties → Digital Signatures**.

PowerShell can also be used:

```powershell
Get-AuthenticodeSignature .\LegacySift.exe | Format-List
```

For a SignPath Foundation-signed release, the signature should validate successfully and the signer information should correspond to the certificate provided through SignPath Foundation.

## Unsigned development builds

Pull-request artifacts and ordinary development builds may remain unsigned. A missing signature on such a build does not mean that it came from a published release channel.
