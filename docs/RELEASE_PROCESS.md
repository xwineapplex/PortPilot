# Release Process

## Overview

PortPilot publishes Windows and Linux artifacts through the
`.github/workflows/publish.yml` GitHub Actions workflow. A pushed Git tag whose
name starts with `v` runs the release workflow. A manually dispatched workflow
builds the artifacts but does not create a GitHub Release.

Release notes are stored in `.github/release-notes/`. Keep one Markdown file per
release and name it after the Git tag, for example `v1.1.1.md`. The workflow
stops before building when the matching file is missing or empty, or when the tag
does not match the `Version` property in `PortPilot-Project.csproj`.

## Prepare a Release

1. Update the `Version` property in `PortPilot-Project.csproj`.
2. Restore packages and verify Debug and Release builds.
3. Review all changes since the previous release tag.
4. Create `.github/release-notes/vX.Y.Z.md` using the target tag name.
5. Describe user-visible changes, fixes, maintenance work, and known limitations.
6. Commit the version, implementation, documentation, workflow, and release notes.
7. Review the commit before creating the release tag.

Codex can prepare the version-specific release notes from the Git diff and commit
history. The workflow does not call an AI service; it publishes the reviewed
Markdown file committed to the repository.

## Publish a Release

After reviewing the release commit, push the branch and its matching tag:

```bash
git push origin master
git tag vX.Y.Z
git push origin vX.Y.Z
```

The tag must match the release-notes filename and should match the project version.
For version `1.1.1`, use tag `v1.1.1` and file
`.github/release-notes/v1.1.1.md`.

The workflow publishes four artifacts:

- Windows x64 standalone
- Windows x64 framework-dependent
- Linux x64 standalone
- Linux x64 framework-dependent

The GitHub Release is created as a draft. Review its body and attached artifacts
on GitHub before publishing it.

## Run a Build Without Releasing

Use **Run workflow** on the GitHub Actions page to validate all publish targets.
Manual runs do not execute the release-notes check and do not create a GitHub
Release because those steps require a tag reference.

## Correct a Failed Release

If the workflow fails because release notes are missing, add the correctly named
file and create a new release commit. Move or recreate a tag only after confirming
that it has not been used for a published release. Avoid rewriting published tags.
