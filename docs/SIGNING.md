# Code signing and releases

Releases are built and signed in GitHub Actions (`.github/workflows/release.yml`).
Signing uses [SignPath.io](https://signpath.io), which is free for open-source projects
through its Foundation program. The signing certificate lives at SignPath and never
touches a local machine; the workflow builds the MSI, submits it to SignPath, and
publishes the signed result.

Signing is what gets the installer past SmartScreen so `winget install` works for
everyone. An unsigned, brand-new binary has no reputation and Windows blocks it during
the download-vetting step, which is the failure we hit before adding this.

## One-time SignPath setup

You do this once in the SignPath dashboard. It is manual and the OSS application needs
their approval, which can take a few days.

1. Sign up at signpath.io and apply for the **Foundation (open source)** program. The
   project must be public and under an OSI-approved license (this repo is GPL-3.0).
2. Install the **SignPath GitHub app** on the `BigTimeRadio/COMTray` repository so
   SignPath can verify the CI build that produced the artifact.
3. Create a **Project** with slug `comtray`, linked to this repository.
4. Create an **Artifact configuration** with slug `msi` that signs the `.msi` inside the
   uploaded build artifact.
5. Create a **Signing policy** with slug `release-signing` that uses the Foundation
   certificate.
6. Create a CI user and an **API token**.

If you choose different slugs, update them in `.github/workflows/release.yml`.

## Repository secrets and variables

In the GitHub repo under Settings > Secrets and variables > Actions:

- Secret `SIGNPATH_API_TOKEN` - the SignPath CI token.
- Variable `SIGNPATH_ORGANIZATION_ID` - your SignPath organization id.

## Cutting a release

1. Bump the version in `src/ComTray/ComTray.csproj` and `installer/ComTray.wxs` to match.
2. Commit, then push a matching tag, for example `git tag v1.0.0 && git push origin v1.0.0`.
3. The workflow builds the MSI, signs it, attaches the signed MSI to a GitHub release for
   that tag, and uploads refreshed winget manifests as a build artifact.
4. Download the `winget-manifests` artifact and submit those files (which now carry the
   signed MSI's hash) to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs),
   or with `wingetcreate submit`.

The first signing run may sit waiting for manual approval in the SignPath dashboard until
you set the policy to sign CI builds automatically.
